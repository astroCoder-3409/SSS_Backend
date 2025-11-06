using System;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net.Http;
using System.Net.Http.Json; // For PostAsJsonAsync
using System.Text.Json;
using System.Linq; // For LINQ queries
using System.Threading.Tasks;
using System.Text.Json.Serialization;

public class ApplicationDbContext : DbContext
{
    // Constructor used for dependency injection
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Define a DbSet for each table you want to interact with
    public DbSet<User> Users { get; set; }
    public DbSet<Account> Accounts { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Category> Categories { get; set; }

    // You can also use the OnModelCreating method for more complex configurations,
    // but for this schema, data annotations are sufficient.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Example: To ensure emails are unique across all users
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();
    }
}

public class User
{
    [Key]
    public int UserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Email { get; set; }

    [Required]
    public string PasswordHash { get; set; } // Store a HASH, not the plain password

    [Required]
    [MaxLength(100)]
    public string FullName { get; set; }

    public DateTime DateOfBirth { get; set; }

    // Plaid access token - nullable since not all users may have connected Plaid
    [MaxLength(500)]
    public string? PlaidAccessToken { get; set; }

    // Navigation property: A User can have many Accounts
    public ICollection<Account> Accounts { get; set; }
}

public class Account
{
    [Key]
    public int AccountId { get; set; } // Renamed from "Account number" for convention

    [Required]
    [MaxLength(50)]
    public string AccountType { get; set; }

    [MaxLength(100)]
    public string AccountName { get; set; }

    [Required]
    [Column(TypeName = "decimal(18, 2)")] // Specifies the exact data type in the DB
    public decimal CurrentBalance { get; set; }

    // --- Foreign Key & Navigation Properties ---

    // 1. The Foreign Key property
    public int UserId { get; set; }
    
    // 2. The navigation property to the "one" side of the relationship (the User)
    public User User { get; set; }

    // 3. The navigation property to the "many" side of the relationship (Transactions)
    public ICollection<Transaction> Transactions { get; set; }
}

public class Transaction
{
    [Key]
    public int TransactionId { get; set; }

    [Required]
    [Column(TypeName = "decimal(18, 2)")]
    public decimal Amount { get; set; }

    public DateTime TransactionDate { get; set; }

    [MaxLength(100)]
    public string MerchantName { get; set; }

    [MaxLength(255)]
    public string? Description { get; set; } // The '?' makes the string nullable

    // --- Foreign Key & Navigation Properties ---

    public int AccountId { get; set; }
    public Account Account { get; set; }
    
    // CategoryId is nullable (int?) in case a transaction is uncategorized
    public int? CategoryId { get; set; }
    public Category? Category { get; set; }
}

public class Category
{
    [Key] // Marks this property as the Primary Key
    public int CategoryId { get; set; }

    [Required] // Makes this field NOT NULL in the database
    [MaxLength(50)] // Sets the max string length
    public string Name { get; set; }

    // Navigation property: A Category can have many Transactions
    public ICollection<Transaction> Transactions { get; set; }
}

public record AnalysisRequest(string Query, string DataContext);
public record AnalysisResponse(string Response);

public class PythonAiService
{
    private readonly HttpClient _httpClient;
    private readonly ApplicationDbContext _dbContext; // <-- Uses your DbContext
    private readonly JsonSerializerOptions _jsonOptions;

    // Use Dependency Injection to get the HttpClient and your DbContext
    public PythonAiService(IHttpClientFactory httpClientFactory, ApplicationDbContext dbContext)
    {
        _httpClient = httpClientFactory.CreateClient("PythonApiClient"); // Use a named client
        _dbContext = dbContext;

        // Configure JSON options to handle navigation properties (cycles)
        _jsonOptions = new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            WriteIndented = false // More compact for API calls
        };
    }

    /// <summary>
    /// Gathers relevant data for a specific user and sends it to the Python AI service.
    /// </summary>
    /// <param name="userQuery">The user's question (e.g., "How can I save money?")</param>
    /// <param name="userId">The ID of the user asking the question.</param>
    /// <returns>The AI's string response.</returns>
    public async Task<string> GetFinancialAdvice(string userQuery, int userId)
    {
        // 1. === GATHER DATA FROM SQLITE (using EF Core) ===
        // We get all transactions for the specified user from the last 90 days.
        // We MUST include Category data for the LLM to provide good analysis.
        var transactions = await _dbContext.Transactions
            .AsNoTracking()
            .Include(t => t.Category) // <-- IMPORTANT: Includes the category
            .Where(t => t.Account.UserId == userId && // Filter by user
                         t.TransactionDate > DateTime.Now.AddDays(-90))
            .OrderByDescending(t => t.TransactionDate)
            .Select(t => new // Project to a simpler object
            {
                t.Amount,
                t.TransactionDate,
                t.MerchantName,
                Category = t.Category.Name // <-- Get just the category name
            })
            .ToListAsync();

        // 2. === SERIALIZE THE DATA ===
        // We serialize our new anonymous object list, which is clean and safe.
        // Using the projection in step 1 avoids the need for _jsonOptions.
        string transactionJson = JsonSerializer.Serialize(transactions);

        // 3. === PREPARE THE API REQUEST ===
        var requestPayload = new AnalysisRequest(
            Query: userQuery,
            DataContext: transactionJson
        );

        Console.WriteLine("Sending request to Python API...");

        try
        {
            // 4. === CALL THE PYTHON API ===
            // Assumes the base URL (http://127.0.0.1:8000) is set in Program.cs
            HttpResponseMessage response = await _httpClient.PostAsJsonAsync("/analyze", requestPayload);
            response.EnsureSuccessStatusCode();

            // 5. === READ THE RESPONSE ===
            AnalysisResponse? aiResponse = await response.Content.ReadFromJsonAsync<AnalysisResponse>();
            return aiResponse?.Response ?? "Error: Received an empty response.";
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"\nException Caught! Message: {e.Message}");
            return $"Error connecting to AI service: {e.Message}";
        }
    }
}