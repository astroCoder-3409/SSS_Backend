namespace SSS_Backend;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

// DTOs for LLM API communication
public class LLMRequest
{
    [JsonPropertyName("query")]
    public required string Query { get; set; }

    [JsonPropertyName("data_context")]
    public required string DataContext { get; set; }
}

public class LLMResponse
{
    [JsonPropertyName("response")]
    public required string Response { get; set; }
}

// DTO for spending aggregation
public class CategorySpending
{
    [JsonPropertyName("category")]
    public required string Category { get; set; }

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }
}

public class SpendingContext
{
    [JsonPropertyName("month")]
    public string? Month { get; set; } // e.g., "January"

    [JsonPropertyName("year")]
    public int? Year { get; set; } // e.g., 2024

    [JsonPropertyName("spending")]
    public required List<CategorySpending> Spending { get; set; }
}

public class LLMService
{
    private readonly HttpClient _httpClient;
    private readonly ApplicationDbContext _dbContext;
    private readonly JsonSerializerOptions _jsonOptions;

    // Use Dependency Injection to get the HttpClient and your DbContext
    public LLMService(IHttpClientFactory httpClientFactory, ApplicationDbContext dbContext)
    {
        _httpClient = httpClientFactory.CreateClient("LLMApiClient");
        _dbContext = dbContext;

        _jsonOptions = new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            WriteIndented = false
        };
    }

    /// <summary>
    /// Gathers spending data for a user and sends a query to the LLM service.
    /// </summary>
    /// <param name="userQuery">The user's question (e.g., "What are some ways I can save money?")</param>
    /// <param name="userId">The ID of the user asking the question.</param>
    /// <param name="month">Month (1-12). If null, uses current month.</param>
    /// <param name="year">Year (e.g., 2024). If null, uses current year.</param>
    /// <returns>The LLM's string response.</returns>
    public async Task<string> GetFinancialAdvice(string userQuery, string userId, int? month = null, int? year = null)
    {
        try
        {
            // 1. === DETERMINE THE MONTH/YEAR ===
            var now = DateTime.UtcNow;
            var targetMonth = month ?? now.Month;
            var targetYear = year ?? now.Year;

            // Validate month
            if (targetMonth < 1 || targetMonth > 12)
            {
                return "Error: Month must be between 1 and 12.";
            }

            // Calculate the start and end dates for the specified month
            var startDate = new DateTime(targetYear, targetMonth, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1); // Last day of the month

            Console.WriteLine($"[LLM] Fetching data for {startDate:MMMM yyyy} (from {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd})");

            // 2. === GATHER AND AGGREGATE DATA FROM DATABASE ===
            // Get transactions and aggregate by category for the specified month
            // Using PlaidCategoryPrimary as the category since Category table might not be populated yet
            var spendingByCategory = await _dbContext.Transactions
                .AsNoTracking()
                .Include(t => t.Account)
                .Where(t => t.Account.UserId == userId && 
                           t.TransactionDate >= startDate &&
                           t.TransactionDate <= endDate &&
                           t.Amount > 0) // Only expenses (positive amounts in Plaid)
                .GroupBy(t => t.PlaidCategoryPrimary ?? "Uncategorized")
                .Select(g => new CategorySpending
                {
                    Category = g.Key,
                    Amount = g.Sum(t => t.Amount)
                })
                .ToListAsync();
            
            // Sort on client-side to avoid SQLite decimal ordering issues
            spendingByCategory = spendingByCategory.OrderByDescending(c => c.Amount).ToList();

            // 3. === CREATE SPENDING CONTEXT WITH METADATA ===
            var spendingContext = new SpendingContext
            {
                Month = startDate.ToString("MMMM"), // e.g., "January"
                Year = targetYear,
                Spending = spendingByCategory
            };

            // Serialize the context to a JSON string
            string dataContextJson = JsonSerializer.Serialize(spendingContext, _jsonOptions);

            // 3. === PREPARE THE API REQUEST ===
            var requestPayload = new LLMRequest
            {
                Query = userQuery,
                DataContext = dataContextJson
            };

            Console.WriteLine($"[LLM] Sending request to LLM API...");
            Console.WriteLine($"[LLM] Query: {userQuery}");
            Console.WriteLine($"[LLM] Data Context: {dataContextJson}");

            // 4. === CALL THE LLM API ===
            HttpResponseMessage response = await _httpClient.PostAsJsonAsync("/analyze", requestPayload);
            response.EnsureSuccessStatusCode();

            // 5. === READ THE RESPONSE ===
            LLMResponse? llmResponse = await response.Content.ReadFromJsonAsync<LLMResponse>();
            
            if (llmResponse?.Response != null)
            {
                Console.WriteLine($"[LLM] Received response from LLM API");
                return llmResponse.Response;
            }
            else
            {
                Console.WriteLine($"[LLM] Error: Received empty response from LLM API");
                return "Error: Received an empty response from the AI service.";
            }
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"[LLM] HTTP Exception: {e.Message}");
            return $"Error connecting to AI service: {e.Message}";
        }
        catch (Exception e)
        {
            Console.WriteLine($"[LLM] Exception: {e.Message}");
            return $"Error processing request: {e.Message}";
        }
    }

    /// <summary>
    /// Gets aggregated spending data for a user without querying the LLM.
    /// Useful for debugging or providing raw data to the frontend.
    /// </summary>
    /// <param name="userId">The user's ID</param>
    /// <param name="month">Month (1-12). If null, uses current month.</param>
    /// <param name="year">Year (e.g., 2024). If null, uses current year.</param>
    public async Task<SpendingContext> GetSpendingData(string userId, int? month = null, int? year = null)
    {
        var now = DateTime.UtcNow;
        var targetMonth = month ?? now.Month;
        var targetYear = year ?? now.Year;

        // Validate month
        if (targetMonth < 1 || targetMonth > 12)
        {
            targetMonth = now.Month; // Fallback to current month
        }

        // Calculate the start and end dates for the specified month
        var startDate = new DateTime(targetYear, targetMonth, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var spendingByCategory = await _dbContext.Transactions
            .AsNoTracking()
            .Include(t => t.Account)
            .Where(t => t.Account.UserId == userId && 
                       t.TransactionDate >= startDate &&
                       t.TransactionDate <= endDate &&
                       t.Amount > 0)
            .GroupBy(t => t.PlaidCategoryPrimary ?? "Uncategorized")
            .Select(g => new CategorySpending
            {
                Category = g.Key,
                Amount = g.Sum(t => t.Amount)
            })
            .ToListAsync();
        
        // Sort on client-side to avoid SQLite decimal ordering issues
        spendingByCategory = spendingByCategory.OrderByDescending(c => c.Amount).ToList();

        return new SpendingContext
        {
            Month = startDate.ToString("MMMM"),
            Year = targetYear,
            Spending = spendingByCategory
        };
    }
}