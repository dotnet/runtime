// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

/// <summary>
/// Ensures setting InvariantGlobalization = true still works in a trimmed app.
/// </summary>
class Program
{
    static int Main(string[] args)
    {
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

        // The rest of this code depends on a property of IL level trimming that keeps reflection
        // metadata for anything that is statically reachable. It's not applicable if we're not doing that
        // kind of trimming. Approximate what kind of trimming are we doing.
        if (GetMethodSecretly(typeof(Program), nameof(GetCoreLibType)) == null)
        {
            // Sanity check: we only expect this for native AOT; IsDynamicCodeSupported approximates that.
            if (RuntimeFeature.IsDynamicCodeSupported)
                throw new Exception();

            return 100;
        }
        
        // Ensure the internal GlobalizationMode class is trimmed correctly.
        Type globalizationMode = GetCoreLibType("System.Globalization.GlobalizationMode");

        if (OperatingSystem.IsWindows())
        {
            foreach (MemberInfo member in globalizationMode.GetMembers(allStatics))
            {
                // properties and their backing getter methods are OK
                if (member is PropertyInfo || member.Name.StartsWith("get_"))
                {
                    continue;
                }

                // Windows still contains a static cctor and a backing field for UseNls.
                if (member is ConstructorInfo || (member is FieldInfo field && field.Name.Contains("UseNls")))
                {
                    continue;
                }

                // Some unexpected member was left on GlobalizationMode, fail
                Console.WriteLine($"Member '{member.Name}' was not trimmed from GlobalizationMode, but should have been.");
                return -4;
            }
        }
        // On non Windows platforms, the full type is trimmed.
        else if (globalizationMode is not null)
        {
            Console.WriteLine("It is expected to have System.Globalization.GlobalizationMode type trimmed in non-Windows platforms");
            return -5;
        }

        return 100;
    }

    // The intention of this method is to ensure the trimmer doesn't preserve the Type.
    private static Type GetCoreLibType(string name) =>
        typeof(object).Assembly.GetType(name, throwOnError: false);

    // The intention is to look for a method on a type in a way that trimming cannot detect.
    private static MethodBase GetMethodSecretly(Type type, string name) =>
        type.GetMethod(name, BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
}
