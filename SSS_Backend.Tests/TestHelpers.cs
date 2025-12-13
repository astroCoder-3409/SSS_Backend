using Microsoft.EntityFrameworkCore;
using SSS_Backend;

namespace SSS_Backend.Tests;

public static class TestHelpers
{
    /// <summary>
    /// Creates an in-memory database context for testing
    /// </summary>
    public static ApplicationDbContext CreateInMemoryDbContext(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    /// <summary>
    /// Creates a test user with default values
    /// </summary>
    public static User CreateTestUser(string? userId = null, string? email = null)
    {
        return new User
        {
            UserId = userId ?? "test-user-123",
            Email = email ?? "test@example.com",
            FullName = "Test User",
            DateOfBirth = new DateTime(1990, 1, 1),
            LastSyncTime = DateTime.UtcNow,
            TransactionMonths = new List<DateOnly> { new DateOnly(2024, 1, 1) }
        };
    }

    /// <summary>
    /// Creates a test account with default values
    /// </summary>
    public static Account CreateTestAccount(string userId, string? plaidAccountId = null)
    {
        return new Account
        {
            PlaidAccountId = plaidAccountId ?? "acc-123",
            UserId = userId,
            AccountType = "depository",
            AccountName = "Checking Account",
            PlaidMask = "0000",
            OfficialName = "Test Checking Account",
            CurrentBalance = 5000.00m
        };
    }

    /// <summary>
    /// Creates a test transaction with default values
    /// </summary>
    public static Transaction CreateTestTransaction(int accountId, decimal amount = 100.00m, DateTime? date = null)
    {
        return new Transaction
        {
            PlaidTransactionId = Guid.NewGuid().ToString(),
            AccountId = accountId,
            Amount = amount,
            TransactionDate = date ?? new DateTime(2024, 1, 15),
            MerchantName = "Test Merchant",
            Description = "Test Transaction",
            PlaidCategoryPrimary = "GENERAL_MERCHANDISE",
            PlaidCategoryDetailed = "GENERAL",
            IsPending = false
        };
    }
}
