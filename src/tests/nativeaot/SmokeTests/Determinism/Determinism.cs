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

for (int i = 0; i < baseline.Length; i++)
{
    if (baseline.ReadByte() != compare.ReadByte())
        throw new Exception($"Different at byte {i}");
}

return 100;
