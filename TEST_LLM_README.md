# Testing the LLM Integration

This guide explains how to test the LLM functionality in your backend.

## Prerequisites

1. **Backend is running**: Start your ASP.NET backend server
2. **LLM Server is running**: Make sure your LLM server is running on `http://127.0.0.1:8000`
3. **Database has data** (optional): Required only for real data tests

## Two Testing Modes

### Mode 1: Dummy Data (Recommended for Initial Testing)
- **Endpoint**: `POST /api/test/llm-dummy`
- **No database required!** Uses hardcoded spending data
- Perfect for testing LLM integration without setting up the full system

### Mode 2: Real Data
- **Endpoint**: `POST /api/test/llm`
- Requires at least one user with synced transactions in the database
- Tests the full end-to-end flow

## Test Endpoints

Two special test endpoints have been created that **bypass authentication** for easy testing:

### 1. Dummy Data Endpoint (No Database)
**Endpoint**: `POST /api/test/llm-dummy`

This endpoint:
- Uses hardcoded dummy spending data
- **No database required!**
- Perfect for initial LLM server testing
- Shows how data is formatted and sent to the LLM

### 2. Real Data Endpoint (Requires Database)
**Endpoint**: `POST /api/test/llm`

This endpoint:
- Automatically uses the first user in your database
- Shows real spending data summary
- Calls your LLM server with actual user spending context
- Returns both the spending data and LLM response

## Testing Methods

### Method 1: PowerShell Script (Recommended for Windows)

Run the included PowerShell script:

```powershell
cd SSS_Backend
.\test_llm.ps1
```

This will run two tests:
1. Current month with default query
2. January 2024 with custom query

### Method 2: HTTP File (VS Code REST Client)

1. Install the "REST Client" extension in VS Code
2. Open `test_llm.http`
3. Click "Send Request" above any test case

### Method 3: cURL

```bash
# Test with DUMMY DATA (no database required)
curl -X POST http://localhost:5290/api/test/llm-dummy \
  -H "Content-Type: application/json" \
  -d "{\"query\":\"How can I cut down on expenses and save more?\"}"

# Test with REAL DATA (requires database)
curl -X POST http://localhost:5290/api/test/llm \
  -H "Content-Type: application/json" \
  -d "{\"query\":\"How can I cut down on expenses and save more?\"}"

# Test with specific month
curl -X POST http://localhost:5290/api/test/llm-dummy \
  -H "Content-Type: application/json" \
  -d "{\"query\":\"What were my biggest expenses?\",\"month\":1,\"year\":2024}"
```

### Method 4: Postman

1. Create a new POST request to `http://localhost:5290/api/test/llm-dummy` (for dummy data)
2. Set Headers: `Content-Type: application/json`
3. Set Body (raw JSON):
```json
{
  "query": "How can I cut down on expenses and save more?",
  "month": 11,
  "year": 2024
}
```

For real data testing, change the URL to `/api/test/llm`

## Request Parameters

```json
{
  "query": "Your question here",  // Optional: defaults to test query
  "month": 11,                    // Optional: 1-12, defaults to current month
  "year": 2024                    // Optional: defaults to current year
}
```

## Example Responses

### Dummy Data Response
```json
{
  "test_mode": "dummy_data",
  "query": "How can I cut down on expenses and save more?",
  "month": "November",
  "year": 2024,
  "spending_summary": [
    {
      "category": "FOOD_AND_DRINK",
      "amount": 850.50
    },
    {
      "category": "TRANSPORTATION",
      "amount": 320.00
    },
    {
      "category": "RENT_AND_UTILITIES",
      "amount": 1200.00
    }
  ],
  "total_categories": 7,
  "total_spending": 3102.24,
  "llm_response": "Based on your spending data... [LLM's advice here]"
}
```

### Real Data Response
```json
{
  "user": "user@example.com",
  "query": "How can I cut down on expenses and save more?",
  "month": "November",
  "year": 2024,
  "spending_summary": [
    {
      "category": "FOOD_AND_DRINK",
      "amount": 1250.50
    }
  ],
  "total_categories": 8,
  "total_spending": 4567.89,
  "llm_response": "Based on your spending data... [LLM's advice here]"
}
```

## What the LLM Server Receives

Your LLM server at `http://127.0.0.1:8000/analyze` will receive:

```json
{
  "query": "How can I cut down on expenses and save more?",
  "data_context": "{\"month\":\"November\",\"year\":2024,\"spending\":[{\"category\":\"FOOD_AND_DRINK\",\"amount\":850.50},{\"category\":\"TRANSPORTATION\",\"amount\":320.00},{\"category\":\"RENT_AND_UTILITIES\",\"amount\":1200.00}]}"
}
```

The `data_context` is a JSON string containing the spending data structured for the LLM to analyze.

## Troubleshooting

### "No users found in database"
- You need to authenticate a user and sync their data first
- Use the Plaid Link flow to connect a bank account

### "No transaction data found"
- Make sure you've called `/api/sync` to sync transactions
- Try a different month where you have data

### "Error connecting to AI service"
- Make sure your LLM server is running on `http://127.0.0.1:8000`
- Check that the `/query` endpoint exists on your LLM server
- Verify the LLM server is accepting POST requests

### Port Issues
- Default backend port is 5290 (configured in launchSettings.json)
- LLM server should be running on port 8000
- Update the port in test files if your backend runs on a different port

## Production Endpoint

For production use with Firebase authentication:

**Endpoint**: `POST /api/llm/query`

Requires:
- Firebase authentication token in header: `Authorization: Bearer <token>`
- Same request body as test endpoint

## Security Note

⚠️ **IMPORTANT**: Remove or disable `/api/test/llm-dummy` and `/api/test/llm` before deploying to production!

These test endpoints bypass all authentication and should only be used in development.

