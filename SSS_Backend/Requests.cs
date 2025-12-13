using System;

using SSS_Backend;

public record MonthRequest
{
    // Expected format: "MM/YYYY"
    public string MonthYear { get; init; }
}
