# Test Results Report

Generated: 2024-12-19

## Summary

**Total Tests:** 23
**Passed:** 23 ✅
**Failed:** 0 ❌
**Success Rate:** 100%

---

## DatabaseQueryTests.cs

**Total Tests:** 13 | **Passed:** 13 | **Failed:** 0

### Failed Tests (0)

✅ No failed tests

### Passed Tests (13)

- ✅ QueryUser_ByUserId_ReturnsCorrectUser
- ✅ QueryUser_ByEmail_ReturnsCorrectUser
- ✅ QueryAccounts_ByUserId_ReturnsAllUserAccounts
- ✅ QueryAccounts_WithAccountDto_ReturnsCorrectData
- ✅ QueryAccounts_NonExistentUser_ReturnsEmptyList
- ✅ QueryTransactions_ByUserId_ReturnsAllUserTransactions
- ✅ QueryTransactions_ByMonth_ReturnsFilteredTransactions
- ✅ QueryTransactions_WithTransactionDto_ReturnsCorrectData
- ✅ QueryTransactions_ByCategory_ReturnsFilteredTransactions
- ✅ QueryTransactions_GroupByCategory_ReturnsAggregatedSpending
- ✅ QueryTransactions_WithNullMonthYear_ReturnsAllTransactions
- ✅ QueryTransactions_PendingOnly_ReturnsPendingTransactions
- ✅ QueryUser_WithTransactionMonths_ReturnsCorrectMonths

---

## LLMServiceTests.cs

**Total Tests:** 10 | **Passed:** 10 | **Failed:** 0

### Failed Tests (0)

✅ No failed tests

### Passed Tests (10)

- ✅ GetFinancialAdvice_ValidRequest_ReturnsLLMResponse
- ✅ GetFinancialAdvice_ValidatesRequestPayload
- ✅ GetFinancialAdvice_HttpError_ReturnsErrorMessage
- ✅ GetFinancialAdvice_NetworkError_ReturnsErrorMessage
- ✅ GetFinancialAdvice_InvalidMonth_ReturnsError
- ✅ GetFinancialAdvice_EmptyResponse_ReturnsError
- ✅ GetSpendingData_ValidRequest_ReturnsSpendingContext
- ✅ GetSpendingData_AggregatesByCategory
- ✅ GetSpendingData_NoTransactions_ReturnsEmptySpending
- ✅ GetSpendingData_DefaultMonthYear_UsesCurrentMonth

