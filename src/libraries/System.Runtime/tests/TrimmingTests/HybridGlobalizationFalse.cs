// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Reflection;
using System.Linq;

/// <summary>
/// Ensures setting HybridGlobalization = false still works in a trimmed app.
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
        }
        catch (PlatformNotSupportedException)
        {
            return -1; // we do not expect GetSortKey to throw.
        }

        // Ensure the internal GlobalizationMode class is trimmed correctly.
        Type globalizationMode = GetCoreLibType("System.Globalization.GlobalizationMode");

        var hybridFields = globalizationMode.GetMembers(allStatics).Where(member => member is FieldInfo field && field.Name.Contains("Hybrid"));
        // expecting 'Hybrid' field to be trimmed
        if (hybridFields.Count() != 0)
            return -2;

        // expecting 'get_Hybrid' property to be trimmed
        var hybridProperties = globalizationMode.GetMembers(allStatics).Where(member => member is FieldInfo field && field.Name.Contains("get_Hybrid"));
        if (hybridProperties.Count() != 0)
            return -3;

        // Ensure the CompareInfo class is trimmed correctly.
        Type compareInfo = GetCoreLibType("System.Globalization.CompareInfo");

        const BindingFlags privateInstanceMethods = BindingFlags.NonPublic | BindingFlags.Instance;
        string[] expectedTrimmedInstatnceMethods = new string[] {
            "JsInit",
            "JsCompareString",
            "JsStartsWith",
            "JsEndsWith",
            "JsIndexOfCore"
        };
        if (compareInfo.GetMembers(privateInstanceMethods).Any(m =>
            expectedTrimmedInstatnceMethods.Any(trimmable =>
                trimmable == m.Name)))
            return -4;
        
        const BindingFlags privateStaticMethods = BindingFlags.NonPublic | BindingFlags.Static;
        string[] expectedTrimmedStaticeMethods = new string[] {
            "AssertHybridOnWasm",
            "AssertComparisonSupported",
            "AssertIndexingSupported",
            "IndexingOptionsNotSupported",
            "CompareOptionsNotSupported",
            "CompareOptionsNotSupportedForCulture",
            "GetPNSEForCulture"
        };
        if (compareInfo.GetMembers(privateStaticMethods).Any(m => 
            expectedTrimmedStaticeMethods.Any(trimmable => 
                trimmable == m.Name)))
            return -5;

        // Ensure the TextInfo class is trimmed correctly.
        Type textInfo = GetCoreLibType("System.Globalization.TextInfo");
        if (textInfo.GetMembers(privateInstanceMethods).Any(m =>
            m.Name == "JsChangeCase"))
            return -6;

        return 100;
    }

    // The intention of this method is to ensure the trimmer doesn't preserve the Type.
    private static Type GetCoreLibType(string name) =>
        typeof(object).Assembly.GetType(name, throwOnError: false);
}
