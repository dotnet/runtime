using System;

// https://github.com/dotnet/runtime/blob/main/docs/design/features/timezone-invariant-mode.md

var timezonesCount = TimeZoneInfo.GetSystemTimeZones().Count;
Console.WriteLine($"Found {timezonesCount} timezones in the TZ database");

TimeZoneInfo utc = TimeZoneInfo.FindSystemTimeZoneById("UTC");
Console.WriteLine($"{utc.DisplayName} BaseUtcOffset is {utc.BaseUtcOffset}");

try
{
    TimeZoneInfo tst = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tokyo");
    Console.WriteLine($"{tst.DisplayName} BaseUtcOffset is {tst.BaseUtcOffset}");
}
catch (TimeZoneNotFoundException tznfe)
{
    Console.WriteLine($"Could not find Asia/Tokyo: {tznfe.Message}");
}

return 42;
