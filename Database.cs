using System;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
    public string UserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Email { get; set; }

    [Required]
    [MaxLength(100)]
    public string? FullName { get; set; }

    public DateTime? DateOfBirth { get; set; }

    public DateTime? LastSyncTime { get; set; }
    
    public string? PlaidTransactionsCursor { get; set; }


    // Plaid access token - nullable since not all users may have connected Plaid
    [MaxLength(500)]
    public string? PlaidAccessToken { get; set; }

    // Navigation property: A User can have many Accounts
    public ICollection<Account>? Accounts { get; set; }

    public string? PlaidItemId { get; set; }
} 

public class Account
{
    [Key]
    public int AccountId { get; set; } // Renamed from "Account number" for convention

    [Required]
    [MaxLength(100)]
    public string PlaidAccountId { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string AccountType { get; set; }

    [MaxLength(100)]
    public string AccountName { get; set; }

    public string PlaidMask { get; set; }
    
    [MaxLength(255)]
    public string OfficialName { get; set; }

    [Required]
    [Column(TypeName = "decimal(18, 2)")] // Specifies the exact data type in the DB
    public decimal CurrentBalance { get; set; }

    // --- Foreign Key & Navigation Properties ---

    // 1. The Foreign Key property
    public string UserId { get; set; }
    
    // 2. The navigation property to the "one" side of the relationship (the User)
    public User User { get; set; }

    // 3. The navigation property to the "many" side of the relationship (Transactions)
    public ICollection<Transaction> Transactions { get; set; }
}

public class Transaction
{
    [Key]
    public int TransactionId { get; set; }

    public string? PlaidTransactionId { get; set; }

    public string? PlaidCategoryPrimary { get; set; }

    public string? PlaidCategoryDetailed { get; set; }

    public string? PlaidCategoryConfidenceLevel { get; set; }

    public bool? IsPending { get; set; }


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