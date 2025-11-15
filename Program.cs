
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Going.Plaid;
using Going.Plaid.Entity;
using Going.Plaid.Item;
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
    [FromServices] PlaidClient plaidClient) =>
{
    var userId = httpContext.Items["UserId"] as string;
    User localUser = await db.Users
    .FirstOrDefaultAsync(u => u.UserId == userId);
    try
    {
        var response = await plaidClient.AccountsGetAsync(
             new Going.Plaid.Accounts.AccountsGetRequest()
             {
                 AccessToken = localUser.PlaidAccessToken
             }
              );


        if (response.Error is not null)
        {
            return Results.BadRequest(response.Error);
        }


        return Results.Ok(new { response.Accounts });

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
                ClientName = "Quickstart for .NET",
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
