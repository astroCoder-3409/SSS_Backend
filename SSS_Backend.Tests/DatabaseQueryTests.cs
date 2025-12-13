using Microsoft.EntityFrameworkCore;
using Xunit;
using FluentAssertions;

namespace SSS_Backend.Tests;

public class DatabaseQueryTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;

    public DatabaseQueryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        SeedTestData();
    }

    private void SeedTestData()
    {
        // Create test user
        var testUser = new User
        {
            UserId = "test-user-123",
            Email = "test@example.com",
            FullName = "Test User",
            DateOfBirth = new DateTime(1990, 1, 1),
            LastSyncTime = DateTime.UtcNow,
            TransactionMonths = new List<DateOnly> { new DateOnly(2024, 1, 1), new DateOnly(2024, 2, 1) }
        };

        _dbContext.Users.Add(testUser);

        // Create test accounts
        var account1 = new Account
        {
            PlaidAccountId = "acc-123",
            UserId = "test-user-123",
            AccountType = "depository",
            AccountName = "Checking Account",
            PlaidMask = "0000",
            OfficialName = "Test Checking Account",
            CurrentBalance = 5000.00m
        };

        var account2 = new Account
        {
            PlaidAccountId = "acc-456",
            UserId = "test-user-123",
            AccountType = "credit",
            AccountName = "Credit Card",
            PlaidMask = "1234",
            OfficialName = "Test Credit Card",
            CurrentBalance = -500.00m
        };

        _dbContext.Accounts.AddRange(account1, account2);
        _dbContext.SaveChanges();

        // Get the account IDs after save
        var savedAccount1 = _dbContext.Accounts.First(a => a.PlaidAccountId == "acc-123");
        var savedAccount2 = _dbContext.Accounts.First(a => a.PlaidAccountId == "acc-456");

        // Create test transactions
        var transactions = new List<Transaction>
        {
            new Transaction
            {
                PlaidTransactionId = "txn-001",
                AccountId = savedAccount1.AccountId,
                Amount = 50.00m,
                TransactionDate = new DateTime(2024, 1, 15),
                MerchantName = "Grocery Store",
                Description = "Grocery shopping",
                PlaidCategoryPrimary = "FOOD_AND_DRINK",
                PlaidCategoryDetailed = "GROCERIES",
                IsPending = false
            },
            new Transaction
            {
                PlaidTransactionId = "txn-002",
                AccountId = savedAccount1.AccountId,
                Amount = 25.00m,
                TransactionDate = new DateTime(2024, 1, 20),
                MerchantName = "Gas Station",
                Description = "Gas purchase",
                PlaidCategoryPrimary = "TRANSPORTATION",
                PlaidCategoryDetailed = "GAS_STATIONS",
                IsPending = false
            },
            new Transaction
            {
                PlaidTransactionId = "txn-003",
                AccountId = savedAccount2.AccountId,
                Amount = 100.00m,
                TransactionDate = new DateTime(2024, 2, 5),
                MerchantName = "Online Store",
                Description = "Online purchase",
                PlaidCategoryPrimary = "GENERAL_MERCHANDISE",
                PlaidCategoryDetailed = "ONLINE_MARKETPLACES",
                IsPending = true
            },
            new Transaction
            {
                PlaidTransactionId = "txn-004",
                AccountId = savedAccount1.AccountId,
                Amount = 200.00m,
                TransactionDate = new DateTime(2024, 2, 10),
                MerchantName = "Restaurant",
                Description = "Dinner",
                PlaidCategoryPrimary = "FOOD_AND_DRINK",
                PlaidCategoryDetailed = "RESTAURANTS",
                IsPending = false
            }
        };

        _dbContext.Transactions.AddRange(transactions);
        _dbContext.SaveChanges();
    }

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
        user.FullName.Should().Be("Test User");
        user.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task QueryUser_ByEmail_ReturnsCorrectUser()
    {
        // Arrange
        var email = "test@example.com";

        // Act
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == email);

        // Assert
        user.Should().NotBeNull();
        user!.UserId.Should().Be("test-user-123");
    }

    [Fact]
    public async Task QueryAccounts_ByUserId_ReturnsAllUserAccounts()
    {
        // Arrange
        var userId = "test-user-123";

        // Act
        var accounts = await _dbContext.Accounts
            .Where(a => a.UserId == userId)
            .ToListAsync();

        // Assert
        accounts.Should().HaveCount(2);
        accounts.Should().Contain(a => a.PlaidAccountId == "acc-123");
        accounts.Should().Contain(a => a.PlaidAccountId == "acc-456");
    }

    [Fact]
    public async Task QueryAccounts_WithAccountDto_ReturnsCorrectData()
    {
        // Arrange
        var userId = "test-user-123";

        // Act
        var accounts = await _dbContext.Accounts
            .Where(a => a.UserId == userId)
            .Select(a => new DatabaseDTOs.AccountDto
            {
                AccountId = a.AccountId,
                AccountType = a.AccountType,
                AccountName = a.AccountName,
                CurrentBalance = a.CurrentBalance,
                PlaidMask = a.PlaidMask
            })
            .ToListAsync();

        // Assert
        accounts.Should().HaveCount(2);
        var checkingAccount = accounts.First(a => a.AccountName == "Checking Account");
        checkingAccount.CurrentBalance.Should().Be(5000.00m);
        checkingAccount.AccountType.Should().Be("depository");
    }

    [Fact]
    public async Task QueryTransactions_ByUserId_ReturnsAllUserTransactions()
    {
        // Arrange
        var userId = "test-user-123";

        // Act
        var transactions = await _dbContext.Transactions
            .Where(t => t.Account.UserId == userId)
            .ToListAsync();

        // Assert
        transactions.Should().HaveCount(4);
    }

    [Fact]
    public async Task QueryTransactions_ByMonth_ReturnsFilteredTransactions()
    {
        // Arrange
        var userId = "test-user-123";
        var startOfMonth = new DateTime(2024, 1, 1);
        var endOfMonthExclusive = new DateTime(2024, 2, 1);

        // Act
        var transactions = await _dbContext.Transactions
            .Where(t => t.Account.UserId == userId)
            .Where(t => t.TransactionDate >= startOfMonth.Date && t.TransactionDate < endOfMonthExclusive.Date)
            .ToListAsync();

        // Assert
        transactions.Should().HaveCount(2);
        transactions.Should().OnlyContain(t => t.TransactionDate.Month == 1);
    }

    [Fact]
    public async Task QueryTransactions_WithTransactionDto_ReturnsCorrectData()
    {
        // Arrange
        var userId = "test-user-123";

        // Act
        var transactions = await _dbContext.Transactions
            .Where(t => t.Account.UserId == userId)
            .Include(t => t.Category)
            .Select(t => new DatabaseDTOs.TransactionDto
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

        // Assert
        transactions.Should().HaveCount(4);
        var groceryTransaction = transactions.First(t => t.PlaidTransactionId == "txn-001");
        groceryTransaction.Amount.Should().Be(50.00m);
        groceryTransaction.MerchantName.Should().Be("Grocery Store");
        groceryTransaction.PlaidCategoryPrimary.Should().Be("FOOD_AND_DRINK");
    }

    [Fact]
    public async Task QueryTransactions_ByCategory_ReturnsFilteredTransactions()
    {
        // Arrange
        var userId = "test-user-123";
        var category = "FOOD_AND_DRINK";

        // Act
        var transactions = await _dbContext.Transactions
            .Where(t => t.Account.UserId == userId)
            .Where(t => t.PlaidCategoryPrimary == category)
            .ToListAsync();

        // Assert
        transactions.Should().HaveCount(2);
        transactions.Should().OnlyContain(t => t.PlaidCategoryPrimary == category);
    }

    [Fact]
    public async Task QueryTransactions_GroupByCategory_ReturnsAggregatedSpending()
    {
        // Arrange
        var userId = "test-user-123";
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 31);

        // Act
        var spendingByCategory = await _dbContext.Transactions
            .AsNoTracking()
            .Include(t => t.Account)
            .Where(t => t.Account.UserId == userId &&
                       t.TransactionDate >= startDate &&
                       t.TransactionDate <= endDate &&
                       t.Amount > 0)
            .GroupBy(t => t.PlaidCategoryPrimary ?? "Uncategorized")
            .Select(g => new
            {
                Category = g.Key,
                Amount = g.Sum(t => t.Amount)
            })
            .ToListAsync();

        // Assert
        spendingByCategory.Should().HaveCount(2);
        var foodSpending = spendingByCategory.First(s => s.Category == "FOOD_AND_DRINK");
        foodSpending.Amount.Should().Be(50.00m);
        var transportSpending = spendingByCategory.First(s => s.Category == "TRANSPORTATION");
        transportSpending.Amount.Should().Be(25.00m);
    }

    [Fact]
    public async Task QueryUser_WithTransactionMonths_ReturnsCorrectMonths()
    {
        // Arrange
        var userId = "test-user-123";

        // Act
        var user = await _dbContext.Users
            .Where(u => u.UserId == userId)
            .Select(u => new
            {
                RawTransactionMonths = u.TransactionMonths
            })
            .FirstOrDefaultAsync();

        // Assert
        user.Should().NotBeNull();
        user!.RawTransactionMonths.Should().HaveCount(2);
        user.RawTransactionMonths.Should().Contain(new DateOnly(2024, 1, 1));
        user.RawTransactionMonths.Should().Contain(new DateOnly(2024, 2, 1));
    }

    [Fact]
    public async Task QueryTransactions_WithNullMonthYear_ReturnsAllTransactions()
    {
        // Arrange
        var userId = "test-user-123";

        // Act
        var transactions = await _dbContext.Transactions
            .Where(t => t.Account.UserId == userId)
            .ToListAsync();

        // Assert
        transactions.Should().HaveCount(4);
    }

    [Fact]
    public async Task QueryAccounts_NonExistentUser_ReturnsEmptyList()
    {
        // Arrange
        var userId = "non-existent-user";

        // Act
        var accounts = await _dbContext.Accounts
            .Where(a => a.UserId == userId)
            .ToListAsync();

        // Assert
        accounts.Should().BeEmpty();
    }

    [Fact]
    public async Task QueryTransactions_PendingOnly_ReturnsPendingTransactions()
    {
        // Arrange
        var userId = "test-user-123";

        // Act
        var pendingTransactions = await _dbContext.Transactions
            .Where(t => t.Account.UserId == userId)
            .Where(t => t.IsPending == true)
            .ToListAsync();

        // Assert
        pendingTransactions.Should().HaveCount(1);
        pendingTransactions.First().PlaidTransactionId.Should().Be("txn-003");
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }
}
