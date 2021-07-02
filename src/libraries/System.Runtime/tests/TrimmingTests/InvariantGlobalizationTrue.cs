// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Reflection;

/// <summary>
/// Ensures setting InvariantGlobalization = true still works in a trimmed app.
/// </summary>
class Program
{
    static int Main(string[] args)
    {
        // Ensure the internal GlobalizationMode class is trimmed correctly
        Type globalizationMode = GetCoreLibType("System.Globalization.GlobalizationMode");
        const BindingFlags allStatics = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
            return -1; // we expect new CultureInfo("tr-TR") to throw.
        }
        catch (CultureNotFoundException)
        {
        }

        if ("i".ToUpper() != "I")
        {
            return -3;
        }

        foreach (MemberInfo member in globalizationMode.GetMembers(allStatics))
        {
            // properties and their backing getter methods are OK
            if (member is PropertyInfo || member.Name.StartsWith("get_"))
            {
                continue;
            }

            if (OperatingSystem.IsWindows())
            {
                // Windows still contains a static cctor and a backing field for UseNls
                if (member is ConstructorInfo || (member is FieldInfo field && field.Name.Contains("UseNls")))
                {
                    continue;
                }
            }

            // Some unexpected member was left on GlobalizationMode, fail
            Console.WriteLine($"Member '{member.Name}' was not trimmed from GlobalizationMode, but should have been.");
            return -4;
        }

        return 100;
    }

    private static Type GetCoreLibType(string name) =>
        typeof(object).Assembly.GetType(name, throwOnError: true);
}
