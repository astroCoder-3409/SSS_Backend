
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Going.Plaid;
using Going.Plaid.Accounts;
using Going.Plaid.Entity;
using Going.Plaid.Item;
using Going.Plaid.Transactions;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SSS_Backend;
using System;
using System.Globalization;
using System.Net.Http;
using System.Security.Cryptography;
using static Google.Apis.Requests.BatchRequest;
using static SSS_Backend.DatabaseDTOs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        policy =>
        {
            policy.AllowAnyOrigin()  
                  .AllowAnyHeader()  
                  .AllowAnyMethod(); 
        });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=bankingInformation.db";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddHttpClient();
builder.Services.Configure<PlaidCredentials>(builder.Configuration.GetSection(PlaidOptions.SectionKey));
builder.Services.Configure<PlaidOptions>(builder.Configuration.GetSection(PlaidOptions.SectionKey));
builder.Services.AddSingleton<PlaidClient>();
builder.Services.AddSingleton<ContextContainer>(new ContextContainer() { RunningOnServer = true });
builder.Services.AddScoped<SyncService>();

var app = builder.Build();

var serviceAccountPath = builder.Configuration["Firebase:ServiceAccountPath"]; //TODO define path as env var

FirebaseApp.Create(new AppOptions()
{
    Credential = GoogleCredential.FromFile(serviceAccountPath),
    ProjectId = "soft-serve-software",
});

//Must go before endpoints
app.UseCors();


app.MapPost("/api/exchange_public_token", async (
    HttpContext httpContext,
    ApplicationDbContext db,
    [FromBody] ItemPublicTokenExchangeRequest request,
    [FromServices] PlaidClient plaidClient,
    [FromServices] IOptions<PlaidCredentials> plaidCredentials) =>
{
    var userId = httpContext.Items["UserId"] as string;
    try
    {
      var response = await plaidClient.ItemPublicTokenExchangeAsync(request);

        if (response.Error is not null)
        {
            return Results.BadRequest(response.Error);
        }

        User localUser = await db.Users
            .FirstOrDefaultAsync(u => u.UserId == userId);

        localUser.PlaidAccessToken = response.AccessToken;
        localUser.PlaidItemId = response.ItemId;

        db.Users.Update(localUser);
        await db.SaveChangesAsync();

    return Results.Ok(new { PublicTokenExchange = "complete" });
   
    }
    catch (Exception ex)
    {
        // Handle error
        Console.WriteLine($"[API] Error exchanging token: {ex.Message}");
        return Results.Problem(
            title: "An error occurred while exchanging the token.",
            detail: ex.Message,
            statusCode: 500
        );
    }
}).AddEndpointFilter<FirebaseAuthorizeFilter>();

app.MapGet("/api/accounts", async (
    HttpContext httpContext,
    ApplicationDbContext db,
    SyncService syncService,
    [FromServices] PlaidClient plaidClient) =>
{
    var userId = httpContext.Items["UserId"] as string;
    try
    {
        var accounts = await db.Accounts
                .Where(a => a.UserId == userId)
                .Select(a => new AccountDto
                {
                    AccountId = a.AccountId,
                    AccountType = a.AccountType,
                    AccountName = a.AccountName,
                    CurrentBalance = a.CurrentBalance,
                    PlaidMask = a.PlaidMask
                })
                .ToListAsync();

        if (accounts is null)
        {
            return Results.BadRequest("No accounts given user");
        }


        return Results.Ok(new { accounts });

    }
    catch (Exception ex)
    {
        // Handle error
        Console.WriteLine($"[API] Error getting accounts: {ex.Message}");
        return Results.Problem(
            title: "An error occurred while getting the accounts.",
            detail: ex.Message,
            statusCode: 500
        );
    }
}).AddEndpointFilter<FirebaseAuthorizeFilter>();

app.MapPost("/api/transactions", async (
    HttpContext httpContext,
    ApplicationDbContext db,
    [FromBody] MonthRequest request,
    SyncService syncService,
    [FromServices] PlaidClient plaidClient) =>
{
    var userId = httpContext.Items["UserId"] as string;
    DateTime startOfMonth = default;
    if (request?.MonthYear != null && !DateTime.TryParseExact(
        request.MonthYear,
        "MM/yyyy",
        System.Globalization.CultureInfo.InvariantCulture, // Use InvariantCulture for consistent parsing
        System.Globalization.DateTimeStyles.None,
        out startOfMonth))
    {
        return Results.BadRequest("Invalid month format. Please use the **MM/YYYY** format. Or, pass in null to get complete transaction history.");
    }

    var endOfMonthExclusive = startOfMonth.AddMonths(1);

    try
    {
        var transactions = await db.Transactions
            .Where(t => t.Account.UserId == userId) // Filter by UserId on the related Account
            .Where(t => request.MonthYear != null ? (t.TransactionDate >= startOfMonth.Date && t.TransactionDate < endOfMonthExclusive.Date) : true)
            .Include(t => t.Category) // Eagerly load the Category
            .Select(t => new TransactionDto
            {
                TransactionId = t.TransactionId,
                PlaidTransactionId = t.PlaidTransactionId,
                Amount = t.Amount,
                TransactionDate = t.TransactionDate,
                MerchantName = t.MerchantName,
                Description = t.Description,
                IsPending = t.IsPending,
                PlaidCategoryPrimary = t.PlaidCategoryPrimary,
                PlaidCategoryDetailed = t.PlaidCategoryDetailed,
                PlaidCategoryConfidenceLevel = t.PlaidCategoryConfidenceLevel
            })
            .ToListAsync();

        if (transactions is null)
        {
            return Results.BadRequest("No transactions for given user");
        }


        return Results.Ok(new { transactionCount = transactions.Count(), transactions =  transactions });

    }
    catch (Exception ex)
    {
        // Handle error
        Console.WriteLine($"[API] Error getting accounts: {ex.Message}");
        return Results.Problem(
            title: "An error occurred while getting the accounts.",
            detail: ex.Message,
            statusCode: 500
        );
    }
}).AddEndpointFilter<FirebaseAuthorizeFilter>();

app.MapGet("/api/user", async (
    HttpContext httpContext,
    ApplicationDbContext db,
    SyncService syncService,
    [FromServices] PlaidClient plaidClient) =>
{
    var userId = httpContext.Items["UserId"] as string;
    try
    {
        var userDetails = await db.Users
            .Where(u => u.UserId == userId) 
            .Select(u => new
            {
                Email = u.Email,
                FullName = u.FullName,
                DateOfBirth = u.DateOfBirth,
                LastSyncTime = u.LastSyncTime,
                RawTransactionMonths = u.TransactionMonths,
            }
            )
            .FirstOrDefaultAsync();

        if (userDetails != null)
        {
            var userDto = new UserDto
            {
                Email = userDetails.Email,
                FullName = userDetails.FullName,
                DateOfBirth = userDetails.DateOfBirth,
                LastSyncTime = userDetails.LastSyncTime,
                // The formatting logic is now safe to run client-side
                TransactionMonths = userDetails.RawTransactionMonths
                    .Select(t => t.ToString("MM/yyyy"))
                    .ToList(),
            };
            return Results.Ok(userDto);
        }

        else 
        {
            return Results.BadRequest("Unable to return a user.");
        }


        

    }
    catch (Exception ex)
    {
        // Handle error
        Console.WriteLine($"[API] Error getting user: {ex.Message}");
        return Results.Problem(
            title: "An error occurred while getting the user.",
            detail: ex.Message,
            statusCode: 500
        );
    }
}).AddEndpointFilter<FirebaseAuthorizeFilter>();


app.MapGet("/api/sync", async (
    HttpContext httpContext,
    ApplicationDbContext db,
    SyncService syncService,
    [FromServices] PlaidClient plaidClient) =>
{
    var userId = httpContext.Items["UserId"] as string;
    User localUser = await db.Users
    .FirstOrDefaultAsync(u => u.UserId == userId);
    try
    {
        var success = await syncService.SyncAllAsync(userId);
        if (!success)
        {
            Console.WriteLine($"[API] Error syncing user data.");
            return Results.Problem(
                title: "An error occurred while getting the accounts.",
                detail: "Unknown reason.",
                statusCode: 500
            );
        }


        return Results.NoContent(); //204 on success

    }
    catch (Exception ex)
    {
        // Handle error
        Console.WriteLine($"[API] Error getting accounts: {ex.Message}");
        return Results.Problem(
            title: "An error occurred while getting the accounts.",
            detail: ex.Message,
            statusCode: 500
        );
    }
}).AddEndpointFilter<FirebaseAuthorizeFilter>();


// Example of protected endpoint TODO Remove this later.
app.MapGet("/api/protected-data", (HttpContext httpContext) => {
    var userId = httpContext.Items["UserId"] as string;

    return Results.Ok(new
    {
        message = "Access granted!",
        user = $"Authenticated Firebase User ID: {userId}"
    });
})
.AddEndpointFilter<FirebaseAuthorizeFilter>(); // Apply the filter here

app.MapPost("/api/create_link_token", async (
    HttpContext httpContext,
    [FromServices] PlaidClient plaidClient,
    [FromServices] IOptions<PlaidCredentials> plaidCredentials) =>
{
    try
    {
        var userId = httpContext.Items["UserId"] as string;

        // Get the configured options
        var credentials = plaidCredentials.Value;

        // Safely parse products and country codes from options
        var productsList = credentials.Products?.Split(',') ?? Array.Empty<string>();
        var products = productsList
            .Select(p => Enum.Parse<Products>(p, true))
            .ToArray();

        var countryCodesList = credentials.CountryCodes?.Split(',') ?? Array.Empty<string>();
        var countryCodes = countryCodesList
            .Select(p => Enum.Parse<CountryCode>(p, true))
            .ToArray();

        var response = await plaidClient.LinkTokenCreateAsync(
            new()
            {
                AccessToken = null,
                User = new LinkTokenCreateRequestUser { ClientUserId = userId, },
                ClientName = "Quickstart for .NET", //TODO
                Products = products,
                Language = Language.English,
                CountryCodes = countryCodes,
            });

        if (response.Error is not null)
        {
            return Results.BadRequest(response.Error);
        }

        return Results.Ok(response.LinkToken);
    }
    catch (Exception ex)
    {
        // Return a more structured error
        return Results.Problem(ex.Message, statusCode: 500);
    }

})
.AddEndpointFilter<FirebaseAuthorizeFilter>();




app.Run();

public class FirebaseAuthorizeFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        string? authHeader = httpContext.Request.Headers["Authorization"];
        var db = httpContext.RequestServices.GetRequiredService<ApplicationDbContext>();

        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return Results.Unauthorized();
        }
        var idToken = authHeader.Substring("Bearer ".Length).Trim();
        try
        {
            FirebaseToken decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken);
            httpContext.Items["UserId"] = decodedToken.Uid;

            string uid = decodedToken.Uid;

            User localUser = await db.Users
                .FirstOrDefaultAsync(u => u.UserId == uid);

            if (localUser == null)
            {
                // User does not exist locally. Create them.
                Console.WriteLine($"New user syncing. Firebase UID: {uid}");
                localUser = new User
                {
                    UserId = uid,
                    Email = decodedToken.Claims.GetValueOrDefault("email", "").ToString(),
                    FullName = decodedToken.Claims.GetValueOrDefault("name", "").ToString(),
                };
                db.Users.Add(localUser);
            }
            else
            {
                // User exists. Update their info just in case it changed.
                Console.WriteLine($"Existing user logging in. Firebase UID: {uid}");
                localUser.Email = decodedToken.Claims.GetValueOrDefault("email", "").ToString();
                localUser.FullName = decodedToken.Claims.GetValueOrDefault("name", "").ToString();
                db.Users.Update(localUser);
            }

            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Firebase token verification failed: {ex.Message}");
            return Results.Unauthorized();
        }

        return await next(context);
    }
}

public class SyncService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly PlaidClient _plaidClient;

    public SyncService(ApplicationDbContext dbContext, [FromServices] PlaidClient plaidClient)
    {
        _dbContext = dbContext;
        _plaidClient = plaidClient;
    }

    public async Task<bool> SyncAllAsync(string userId)
    {
        try
        {
            var accountsResponse = await GetAccountsAsync(userId);
            await SyncAccountsAsync(userId, accountsResponse);
            var transactionsResponse = await GetTransactionsAsync(userId);
            await SyncTransactionsAsync(userId, transactionsResponse);
            return true;
        }
        catch (Exception ex) {
            return false;
        }
    }

    public async Task<AccountsGetResponse?> GetAccountsAsync(string userId)
    {
        
        try
        {
            User localUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            var plaidResponse = await _plaidClient.AccountsGetAsync(
                 new Going.Plaid.Accounts.AccountsGetRequest()
                 {
                     AccessToken = localUser.PlaidAccessToken
                 }
            );



            if (plaidResponse.Error is not null)
            {
                Console.WriteLine($"[API] Error getting accounts: {plaidResponse.Error.ErrorMessage}");
                throw new Exception(plaidResponse.Error.ErrorMessage);
            }


            return plaidResponse;

        }
        catch (Exception ex)
        {
            // Handle error
            Console.WriteLine($"[API] Error getting accounts: {ex.Message}");
            return null;
        }
    }

    public async Task<string> SyncAccountsAsync(string userId, AccountsGetResponse plaidData)
    {
        // 1. Validate the Plaid response
        if (plaidData == null || plaidData.Accounts == null)
        {
            return "Error: Invalid Plaid response data.";
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null)
        {
            // This shouldn't happen if the userId is valid, but it's a good safety check.
            return "Error: User not found.";
        }

        // Get all Plaid account IDs from the API response
        var plaidAccountIdsFromApi = plaidData.Accounts
            .Select(a => a.AccountId)
            .ToHashSet();

        // Get all existing accounts from our database for this user
        var existingDbAccounts = await _dbContext.Accounts
            .Where(a => a.UserId == userId)
            .ToListAsync();

        // Create a lookup map for fast access
        var dbAccountMap = existingDbAccounts.ToDictionary(a => a.PlaidAccountId);

        int addedCount = 0;
        int updatedCount = 0;
        
        foreach (var plaidAccount in plaidData.Accounts)
        {
            if (dbAccountMap.TryGetValue(plaidAccount.AccountId, out var dbAccount))
            {
                // --- UPDATE ---
                dbAccount.CurrentBalance = plaidAccount.Balances.Current ?? 0;
                dbAccount.AccountName = plaidAccount.Name;
                dbAccount.OfficialName = plaidAccount.OfficialName;
                dbAccount.AccountType = plaidAccount.Type.ToString();
                dbAccount.PlaidMask = plaidAccount.Mask;

                _dbContext.Accounts.Update(dbAccount);
                updatedCount++;
            }
            else
            {
                // --- CREATE ---
                var newAccount = new Account
                {
                    PlaidAccountId = plaidAccount.AccountId,
                    UserId = userId,
                    CurrentBalance = plaidAccount.Balances.Current ?? 0,
                    AccountName = plaidAccount.Name,
                    OfficialName = plaidAccount.OfficialName,
                    AccountType = plaidAccount.Type.ToString(),
                    PlaidMask = plaidAccount.Mask
                };

                await _dbContext.Accounts.AddAsync(newAccount);
                addedCount++;
            }
        }
        // --- DELETE ---
        var accountsToDelete = existingDbAccounts
            .Where(a => !plaidAccountIdsFromApi.Contains(a.PlaidAccountId))
            .ToList();

        int deletedCount = 0;
        if (accountsToDelete.Any())
            _dbContext.Accounts.RemoveRange(accountsToDelete);
        deletedCount = accountsToDelete.Count;

        // We use UtcNow for server-side timestamps to avoid timezone issues.
        user.LastSyncTime = DateTime.UtcNow;
        _dbContext.Users.Update(user);

        await _dbContext.SaveChangesAsync();

        return $"Sync complete. Added: {addedCount}, Updated: {updatedCount}, Removed: {deletedCount}.";
    }

    public async Task<TransactionsSyncResponse?> GetTransactionsAsync(string userId)
    {

        try
        {
            User localUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            var plaidResponse = await _plaidClient.TransactionsSyncAsync(
                 new Going.Plaid.Transactions.TransactionsSyncRequest()
                 {
                     AccessToken = localUser.PlaidAccessToken,
                     Cursor = localUser.PlaidTransactionsCursor,
                 }
            );



            if (plaidResponse.Error is not null)
            {
                Console.WriteLine($"[API] Error getting transactions: {plaidResponse.Error.ErrorMessage}");
                throw new Exception(plaidResponse.Error.ErrorMessage);
            }


            return plaidResponse;

        }
        catch (Exception ex)
        {
            // Handle error
            Console.WriteLine($"[API] Error getting transactions: {ex.Message}");
            return null;
        }
    }

    public async Task<string> SyncTransactionsAsync(string userId, TransactionsSyncResponse plaidData)
    {
        if (plaidData == null)
            return "Error: No Plaid data received.";


        // We need a map of [PlaidAccountId (string)] -> [Internal AccountId (int)]
        // to correctly link transactions.
        var userAccounts = await _dbContext.Accounts
            .Where(a => a.UserId == userId)
            .ToListAsync();

        var accountIdMap = userAccounts.ToDictionary(a => a.PlaidAccountId, a => a.AccountId);
        var accountEntityMap = userAccounts.ToDictionary(a => a.PlaidAccountId);

        int addedCount = 0, modifiedCount = 0, removedCount = 0;

        foreach (var plaidTx in plaidData.Added)
        {
            // Find our internal AccountId
            if (accountIdMap.TryGetValue(plaidTx.AccountId, out int internalAccountId))
            {
                var newTransaction = new Transaction();
                MapPlaidTransactionToEntity(plaidTx, newTransaction, internalAccountId);
                await _dbContext.Transactions.AddAsync(newTransaction);
                addedCount++;
            }
        }

        foreach (var plaidTx in plaidData.Modified)
        {
            var existingTx = await _dbContext.Transactions
                .FirstOrDefaultAsync(t => t.PlaidTransactionId == plaidTx.TransactionId);

            if (existingTx != null)
            {
                // Find our internal AccountId (in case it changed, though unlikely)
                if (accountIdMap.TryGetValue(plaidTx.AccountId, out int internalAccountId))
                {
                    MapPlaidTransactionToEntity(plaidTx, existingTx, internalAccountId);
                    _dbContext.Transactions.Update(existingTx);
                    modifiedCount++;
                }
            }
        }

        foreach (var removedTx in plaidData.Removed)
        {
            var txToDelete = await _dbContext.Transactions
                .FirstOrDefaultAsync(t => t.PlaidTransactionId == removedTx.TransactionId);

            if (txToDelete != null)
            {
                _dbContext.Transactions.Remove(txToDelete);
                removedCount++;
            }
        }


        var existingDates = await _dbContext.Transactions
            .Where(t => userAccounts.Select(a => a.AccountId).Contains(t.AccountId))
            .Select(t => t.TransactionDate)
            .ToListAsync();

        var allUniqueMonths = existingDates
            .Select(d => new DateOnly(d.Year, d.Month, 1)) // Normalize to the 1st of the month
            .Distinct()
            .OrderByDescending(d => d.Year) // Sort by year
            .ThenByDescending(d => d.Month) // Then by month
            .ToList();

        var user = await _dbContext.Users.FindAsync(userId);

        if (user != null)
        {
            // This is the MOST important step for the *next* sync.
            user.PlaidTransactionsCursor = plaidData.NextCursor;
            user.TransactionMonths = allUniqueMonths;
            _dbContext.Users.Update(user);
        }

        await _dbContext.SaveChangesAsync();

        return $"Transaction sync complete. Added: {addedCount}, Modified: {modifiedCount}, Removed: {removedCount}.";
    }

    private void MapPlaidTransactionToEntity(Going.Plaid.Entity.Transaction plaidTx, Transaction dbTx, int internalAccountId)
    {
        dbTx.PlaidTransactionId = plaidTx.TransactionId;
        dbTx.AccountId = internalAccountId;
        dbTx.Amount = plaidTx.Amount ?? 0;
        dbTx.TransactionDate = plaidTx.Date?.ToDateTime(TimeOnly.MinValue) ?? new DateTime();
        dbTx.Description = plaidTx.Name;
        dbTx.MerchantName = plaidTx.MerchantName ?? plaidTx.Counterparties?.FirstOrDefault()?.Name ?? "no name?"; //TODO: why are there no names sometimes... should this be optional?
        dbTx.IsPending = plaidTx.Pending;
        dbTx.PlaidCategoryPrimary = plaidTx.PersonalFinanceCategory.Primary;
        dbTx.PlaidCategoryDetailed = plaidTx.PersonalFinanceCategory.Detailed;
        dbTx.PlaidCategoryConfidenceLevel = plaidTx.PersonalFinanceCategory.ConfidenceLevel;
        // We will leave this null for now.
        dbTx.CategoryId = null;
    }
}


