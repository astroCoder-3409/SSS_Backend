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
using System.Threading.Tasks;

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
    public async Task<string> GetFinancialAdvice(string userQuery, string userId)
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