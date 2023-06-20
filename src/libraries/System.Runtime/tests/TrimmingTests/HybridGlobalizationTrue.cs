// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Reflection;
using System.Linq;

/// <summary>
/// Ensures setting HybridGlobalization = true still works in a trimmed app.
/// </summary>
class Program
{
    static int Main(string[] args)
    {
        const BindingFlags allStatics = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        try
        {
            CompareInfo compInfo = new CultureInfo("tr-TR").CompareInfo;
            SortKey sk = compInfo.GetSortKey("string");
            return -1; // we expect GetSortKey to throw.
        }
        catch (PlatformNotSupportedException)
        {
        }

        // Ensure the internal GlobalizationMode class is trimmed correctly.
        Type globalizationMode = GetCoreLibType("System.Globalization.GlobalizationMode");

        var hybridMembers = globalizationMode.GetMembers(allStatics).Where(member =>
            (member is MemberInfo m && (m.Name.StartsWith("get_Hybrid") || m.Name.StartsWith("Hybrid"))));

        // expecting 'Hybrid' field and 'get_Hybrid' property
        if (hybridMembers.Count() != 2)
            return -2;

        return 100;
    }

    // The intention of this method is to ensure the trimmer doesn't preserve the Type.
    private static Type GetCoreLibType(string name) =>
        typeof(object).Assembly.GetType(name, throwOnError: false);
}
