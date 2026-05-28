// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

internal static class TestHelpers
{
    internal static string FormatHResult(int hr)
    {
        string hex = $"0x{unchecked((uint)hr):X8}";
        foreach (Type type in new[] { typeof(HResults), typeof(CorDbgHResults) })
        {
            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            {
                if (field.FieldType == typeof(int) && (int)field.GetValue(null)! == hr)
                    return $"{field.Name} ({hex})";
            }
        }
        return hex;
    }

    internal static void AssertHResult(int expected, int actual)
    {
        Assert.True(expected == actual,
            $"Expected: {FormatHResult(expected)}, Actual: {FormatHResult(actual)}");
    }
}
