// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Reflection;
using System.Threading;

/// <summary>
/// Ensures setting InvariantGlobalization = true still works in a trimmed app.
/// </summary>
class Program
{
    static int Main(string[] args)
    {
        // since we are using Invariant GlobalizationMode = true, setting the culture doesn't matter.
        // The app will always use Invariant mode, so even in the Turkish culture, 'i' ToUpper will be "I"
        Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");
        if ("i".ToUpper() != "I")
        {
            // 'i' ToUpper was not "I", so fail
            return -1;
        }

        // Ensure the internal GlobalizationMode class is trimmed correctly
        Type globalizationMode = GetCoreLibType("System.Globalization.GlobalizationMode");
        const BindingFlags allStatics = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
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
            return -2;
        }

        return 100;
    }

    private static Type GetCoreLibType(string name) =>
        typeof(object).Assembly.GetType(name, throwOnError: true);
}
