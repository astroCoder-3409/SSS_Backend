using Microsoft.EntityFrameworkCore;

using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=bankingInformation.db";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
    
var app = builder.Build();

var serviceAccountPath = "C:/Users/rdeed/Downloads/soft-serve-software-firebase-adminsdk-fbsvc-3baf649e03.json"; //TODO define path as env var

FirebaseApp.Create(new AppOptions()
{
    Credential = GoogleCredential.FromFile(serviceAccountPath),
    ProjectId = "soft-serve-software",
});

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


app.MapGet("/", () => "Hello World!");

app.Run();

public class FirebaseAuthorizeFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        string? authHeader = httpContext.Request.Headers["Authorization"];
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return Results.Unauthorized();
        }
        var idToken = authHeader.Substring("Bearer ".Length).Trim();
        try
        {
            FirebaseToken decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken);
            httpContext.Items["UserId"] = decodedToken.Uid;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Firebase token verification failed: {ex.Message}");
            return Results.Unauthorized();
        }

        return await next(context);
    }
}
