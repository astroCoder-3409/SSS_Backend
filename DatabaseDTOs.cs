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
    }
}
