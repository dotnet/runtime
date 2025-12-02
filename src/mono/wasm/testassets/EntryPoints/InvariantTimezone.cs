using System;

// https://github.com/dotnet/runtime/blob/main/docs/design/features/timezone-invariant-mode.md

void WriteTestOutput(string output) => Console.WriteLine($"TestOutput -> {output}");

var timezonesCount = TimeZoneInfo.GetSystemTimeZones().Count;
WriteTestOutput($"Found {timezonesCount} timezones in the TZ database");

TimeZoneInfo utc = TimeZoneInfo.FindSystemTimeZoneById("UTC");
WriteTestOutput($"{utc.DisplayName} BaseUtcOffset is {utc.BaseUtcOffset}");

try
{
    TimeZoneInfo tst = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tokyo");
    WriteTestOutput($"{tst.DisplayName} BaseUtcOffset is {tst.BaseUtcOffset}");
}
catch (TimeZoneNotFoundException tznfe)
{
    WriteTestOutput($"Could not find Asia/Tokyo: {tznfe.Message}");
}

return 42;
