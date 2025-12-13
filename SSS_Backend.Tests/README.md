# SSS Backend Tests

This directory contains comprehensive test suites for the SSS Backend application.

## Test Structure

### 1. Database Query Tests (`DatabaseQueryTests.cs`)
Tests for all database query operations including:
- User queries (by UserId, Email)
- Account queries (by UserId, with DTOs)
- Transaction queries (by UserId, by month, by category)
- Transaction aggregation and grouping
- Edge cases (non-existent users, empty results, etc.)

**Key Features:**
- Uses in-memory database for fast, isolated tests
- Tests both entity queries and DTO projections
- Validates filtering, grouping, and aggregation logic

### 2. LLM Service Tests (`LLMServiceTests.cs`)
Tests for the LLM service integration including:
- HTTP client mocking for LLM API calls
- Request payload validation
- Response handling (success, errors, network failures)
- Spending data aggregation
- Error handling and edge cases

**Key Features:**
- Mocks HTTP client to avoid actual LLM server calls
- Validates request payload structure
- Tests error scenarios (HTTP errors, network failures, empty responses)
- Tests spending data aggregation logic

### 3. Endpoint Integration Tests (`EndpointTests.cs`)
Integration tests for all API endpoints:
- `/api/accounts` - Get user accounts
- `/api/user` - Get user details
- `/api/transactions` - Get transactions (with month filtering)
- `/api/sync` - Sync Plaid data
- `/api/llm/query` - Query LLM service
- `/api/llm/spending-data` - Get spending data
- `/api/create_link_token` - Create Plaid link token
- `/api/exchange_public_token` - Exchange Plaid token
- `/api/protected-data` - Protected endpoint test

**Note:** Some endpoint tests may require additional mocking for:
- Firebase authentication (currently requires valid Firebase tokens)
- Plaid client (requires valid Plaid credentials)
- LLM service (HTTP client is mocked in LLMServiceTests)

## Running Tests

### Using .NET CLI
```bash
# Run all tests
dotnet test

# Run specific test class
dotnet test --filter "FullyQualifiedName~DatabaseQueryTests"

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run with code coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

### Using Visual Studio
1. Open Test Explorer (Test → Test Explorer)
2. Build the solution
3. Run all tests or select specific tests to run

### Using Visual Studio Code
1. Install the .NET Test Explorer extension
2. Open the test project
3. Use the test explorer to run tests

## Test Dependencies

The test project uses:
- **xUnit** - Testing framework
- **Moq** - Mocking framework for HTTP clients and services
- **FluentAssertions** - Fluent assertion library for readable test assertions
- **Microsoft.AspNetCore.Mvc.Testing** - Integration testing for ASP.NET Core
- **Microsoft.EntityFrameworkCore.InMemory** - In-memory database for testing

## Setting Up Tests

### Prerequisites
1. .NET 8.0 SDK installed
2. All NuGet packages restored (`dotnet restore`)

### Configuration
- Database tests use in-memory databases (no configuration needed)
- LLM service tests mock HTTP clients (no LLM server needed)
- Endpoint tests may require Firebase configuration for full authentication testing

## Test Coverage

### Database Queries
- ✅ User queries (by ID, by email)
- ✅ Account queries (by user, with DTOs)
- ✅ Transaction queries (by user, by month, by category)
- ✅ Transaction aggregation and grouping
- ✅ Edge cases and error scenarios

### LLM Service
- ✅ Successful LLM API calls
- ✅ Request payload validation
- ✅ Error handling (HTTP errors, network failures)
- ✅ Spending data aggregation
- ✅ Month/year filtering

### Endpoints
- ✅ All major endpoints tested
- ✅ Authentication validation
- ✅ Request/response validation
- ⚠️ Some endpoints require additional mocking for external services

## Notes

### Firebase Authentication
The endpoint integration tests currently require valid Firebase tokens for authentication. To fully test endpoints without Firebase:
1. Create a test version of `FirebaseAuthorizeFilter` that bypasses Firebase verification in test mode
2. Use environment variables to conditionally use test authentication
3. Mock Firebase Admin SDK (more complex)

### Plaid Integration
Tests that interact with Plaid endpoints may return 500 errors if Plaid credentials are not configured. This is expected in a test environment. Consider:
1. Mocking the PlaidClient service
2. Using Plaid's sandbox environment for testing
3. Creating a test-specific PlaidClient implementation

### LLM Service
The LLM service tests fully mock the HTTP client, so no actual LLM server is needed. The endpoint tests may attempt to call the LLM service if not properly mocked.

## Adding New Tests

When adding new tests:
1. Follow the existing test structure
2. Use `TestHelpers.cs` for common test data creation
3. Use FluentAssertions for readable assertions
4. Ensure tests are isolated (use unique database names)
5. Clean up resources in `Dispose()` methods

## Example Test

```csharp
[Fact]
public async Task QueryUser_ByUserId_ReturnsCorrectUser()
{
    // Arrange
    var userId = "test-user-123";

    // Act
    var user = await _dbContext.Users
        .FirstOrDefaultAsync(u => u.UserId == userId);

    // Assert
    user.Should().NotBeNull();
    user!.Email.Should().Be("test@example.com");
}
```
