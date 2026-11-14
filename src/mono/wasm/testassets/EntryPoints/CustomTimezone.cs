// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;

foreach (string id in new[] { "Custom/Test", "Custom/Added", "Asia/Tokyo", "UTC" })
{
    try
    {
        TimeZoneInfo timezone = TimeZoneInfo.FindSystemTimeZoneById(id);
        Console.WriteLine($"TestOutput -> {id}: {timezone.BaseUtcOffset}");
    }
    catch (TimeZoneNotFoundException)
    {
        Console.WriteLine($"TestOutput -> {id}: not found");
    }
}

foreach (string id in new[] { "Custom/Test", "Custom/Added" })
{
    Console.WriteLine($"TestOutput -> Enumerated {id}: {TimeZoneInfo.GetSystemTimeZones().Any(zone => zone.Id == id)}");
}

return 42;
