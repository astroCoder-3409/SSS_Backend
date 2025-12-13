using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SSS_Backend
{
    public class DatabaseDTOs
    {
        // A simple class to represent an account for your API
        public class AccountDto
        {
            public int AccountId { get; set; }
            public string AccountType { get; set; }
            public string AccountName { get; set; }
            public decimal CurrentBalance { get; set; }
            public string PlaidMask { get; set; }
        }

        public class TransactionDto
        {
            public int TransactionId { get; set; }
            public string? PlaidTransactionId { get; set; }
            public string? PlaidCategoryPrimary { get; set; }
            public string? PlaidCategoryDetailed { get; set; }
            public string? PlaidCategoryConfidenceLevel { get; set; }
            public bool? IsPending { get; set; }
            public decimal Amount { get; set; }
            public DateTime TransactionDate { get; set; }
            public string MerchantName { get; set; }
            public string? Description { get; set; }
        }

        public class UserDto
        {
            public string Email { get; set; }
            public string? FullName { get; set; }
            public DateTime? DateOfBirth { get; set; }
            public DateTime? LastSyncTime { get; set; }
            public List<string>? TransactionMonths { get; set; }

        }
    }
}
