// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

using var baseline = File.OpenRead("baseline.object");
using var compare = File.OpenRead("compare.object");

Console.WriteLine($"Baseline size: {baseline.Length}");
Console.WriteLine($"Compare size: {compare.Length}");

if (baseline.Length != compare.Length)
    throw new Exception("Different sizes");

long length = baseline.Length;
for (int i = 0; i < length; i++)
{
    if (baseline.ReadByte() != compare.ReadByte())
        throw new Exception($"Different at byte {i}");
}

// We're not interested in running this, we just want some junk to compile
if (Environment.GetEnvironmentVariable("Never") == "Ever")
{
    Delegates.Run();
    Devirtualization.Run();
    Generics.Run();
    Interfaces.Run();
}

return 100;
