// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

Console.WriteLine("Hello, Wasi Console!");
for (int i = 0; i < args.Length; i ++)
    Console.WriteLine($"args[{i}] = {args[i]}");

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
