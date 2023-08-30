// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.DotNet.XHarness.TestRunners.Common;
using Xunit.Abstractions;

#nullable enable
namespace Microsoft.DotNet.XHarness.TestRunners.Xunit;

internal class XUnitFilter
{
    public string? AssemblyName { get; private set; }
    public string? SelectorName { get; private set; }
    public string? SelectorValue { get; private set; }

    public bool Exclude { get; private set; }
    public XUnitFilterType FilterType { get; private set; }

    public static XUnitFilter CreateSingleFilter(string singleTestName, bool exclude, string? assemblyName = null)
    {
        if (string.IsNullOrEmpty(singleTestName))
        {
            throw new ArgumentException("must not be null or empty", nameof(singleTestName));
        }

        return new XUnitFilter
        {
            AssemblyName = assemblyName,
            SelectorValue = singleTestName,
            FilterType = XUnitFilterType.Single,
            Exclude = exclude
        };
    }

    public static XUnitFilter CreateAssemblyFilter(string assemblyName, bool exclude)
    {
        if (string.IsNullOrEmpty(assemblyName))
        {
            throw new ArgumentException("must not be null or empty", nameof(assemblyName));
        }

        // ensure that the assembly name does have one of the valid extensions
        var fileExtension = Path.GetExtension(assemblyName);
        if (fileExtension != ".dll" && fileExtension != ".exe")
        {
            throw new ArgumentException($"Assembly name must have .dll or .exe as extensions. Found extension {fileExtension}");
        }

        return new XUnitFilter
        {
            AssemblyName = assemblyName,
            FilterType = XUnitFilterType.Assembly,
            Exclude = exclude
        };
    }

    public static XUnitFilter CreateNamespaceFilter(string namespaceName, bool exclude, string? assemblyName = null)
    {
        if (string.IsNullOrEmpty(namespaceName))
        {
            throw new ArgumentException("must not be null or empty", nameof(namespaceName));
        }

        return new XUnitFilter
        {
            AssemblyName = assemblyName,
            SelectorValue = namespaceName,
            FilterType = XUnitFilterType.Namespace,
            Exclude = exclude
        };
    }

    public static XUnitFilter CreateClassFilter(string className, bool exclude, string? assemblyName = null)
    {
        if (string.IsNullOrEmpty(className))
        {
            throw new ArgumentException("must not be null or empty", nameof(className));
        }

        return new XUnitFilter
        {
            AssemblyName = assemblyName,
            SelectorValue = className,
            FilterType = XUnitFilterType.TypeName,
            Exclude = exclude
        };
    }

    public static XUnitFilter CreateTraitFilter(string traitName, string? traitValue, bool exclude)
    {
        if (string.IsNullOrEmpty(traitName))
        {
            throw new ArgumentException("must not be null or empty", nameof(traitName));
        }

        return new XUnitFilter
        {
            AssemblyName = null,
            SelectorName = traitName,
            SelectorValue = traitValue ?? string.Empty,
            FilterType = XUnitFilterType.Trait,
            Exclude = exclude
        };
    }

    private bool ApplyTraitFilter(ITestCase testCase, Func<bool, bool>? reportFilteredTest = null)
    {
        Func<bool, bool> log = (result) => reportFilteredTest?.Invoke(result) ?? result;

        if (!testCase.HasTraits())
        {
            return log(!Exclude);
        }

        if (testCase.TryGetTrait(SelectorName!, out var values))
        {
            if (values == null || values.Count == 0)
            {
                // We have no values and the filter doesn't specify one - that means we match on
                // the trait name only.
                if (string.IsNullOrEmpty(SelectorValue))
                {
                    return log(Exclude);
                }

                return log(!Exclude);
            }

            return values.Any(value => value.Equals(SelectorValue, StringComparison.InvariantCultureIgnoreCase)) ?
                log(Exclude) : log(!Exclude);
        }

        // no traits found, that means that we return the opposite of the setting of the filter
        return log(!Exclude);
    }

    private bool ApplyTypeNameFilter(ITestCase testCase, Func<bool, bool>? reportFilteredTest = null)
    {
        Func<bool, bool> log = (result) => reportFilteredTest?.Invoke(result) ?? result;
        var testClassName = testCase.GetTestClass();
        if (!string.IsNullOrEmpty(testClassName))
        {
            if (string.Equals(testClassName, SelectorValue, StringComparison.InvariantCulture))
            {
                return log(Exclude);
            }
        }

        return log(!Exclude);
    }

    private bool ApplySingleFilter(ITestCase testCase, Func<bool, bool>? reportFilteredTest = null)
    {
        Func<bool, bool> log = (result) => reportFilteredTest?.Invoke(result) ?? result;
        if (string.Equals(testCase.DisplayName, SelectorValue, StringComparison.InvariantCulture))
        {
            // if there is a match, return the exclude value
            return log(Exclude);
        }
        // if there is not match, return the opposite
        return log(!Exclude);
    }

    private bool ApplyNamespaceFilter(ITestCase testCase, Func<bool, bool>? reportFilteredTest = null)
    {
        Func<bool, bool> log = (result) => reportFilteredTest?.Invoke(result) ?? result;
        var testClassNamespace = testCase.GetNamespace();
        if (string.IsNullOrEmpty(testClassNamespace))
        {
            // if we exclude, since we have no namespace, we include the test
            return log(!Exclude);
        }

        if (string.Equals(testClassNamespace, SelectorValue, StringComparison.InvariantCultureIgnoreCase))
        {
            return log(Exclude);
        }

        // same logic as with no namespace
        return log(!Exclude);
    }

    public bool IsExcluded(TestAssemblyInfo assembly, Action<string>? reportFilteredAssembly = null)
    {
        if (FilterType != XUnitFilterType.Assembly)
        {
            throw new InvalidOperationException("Filter is not targeting assemblies.");
        }

        Func<bool, bool> log = (result) => ReportFilteredAssembly(assembly, result, reportFilteredAssembly);

        if (string.Equals(AssemblyName, assembly.FullPath, StringComparison.Ordinal))
        {
            return log(Exclude);
        }

        string fileName = Path.GetFileName(assembly.FullPath);
        if (string.Equals(fileName, AssemblyName, StringComparison.Ordinal))
        {
            return log(Exclude);
        }

        // No path of the name matched the filter, therefore return the opposite of the Exclude value
        return log(!Exclude);
    }

    public bool IsExcluded(ITestCase testCase, Action<string>? log = null)
    {
        Func<bool, bool>? reportFilteredTest = null;
        if (log != null)
        {
            reportFilteredTest = (result) => ReportFilteredTest(testCase, result, log);
        }

        return FilterType switch
        {
            XUnitFilterType.Trait => ApplyTraitFilter(testCase, reportFilteredTest),
            XUnitFilterType.TypeName => ApplyTypeNameFilter(testCase, reportFilteredTest),
            XUnitFilterType.Single => ApplySingleFilter(testCase, reportFilteredTest),
            XUnitFilterType.Namespace => ApplyNamespaceFilter(testCase, reportFilteredTest),
            _ => throw new InvalidOperationException($"Unsupported filter type {FilterType}")
        };
    }

    private bool ReportFilteredTest(ITestCase testCase, bool excluded, Action<string>? log = null)
    {
        const string includedText = "Included";
        const string excludedText = "Excluded";

        if (log == null)
        {
            return excluded;
        }

        var selector = FilterType == XUnitFilterType.Trait ?
            $"'{SelectorName}':'{SelectorValue}'" : $"'{SelectorValue}'";

        log($"[FILTER] {(excluded ? excludedText : includedText)} test (filtered by {FilterType}; {selector}): {testCase.DisplayName}");
        return excluded;
    }

    private static bool ReportFilteredAssembly(TestAssemblyInfo assemblyInfo, bool excluded, Action<string>? log = null)
    {
        if (log == null)
        {
            return excluded;
        }

        const string includedPrefix = "Included";
        const string excludedPrefix = "Excluded";

        log($"[FILTER] {(excluded ? excludedPrefix : includedPrefix)} assembly: {assemblyInfo.FullPath}");
        return excluded;
    }

    private static void AppendDesc(StringBuilder sb, string name, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        sb.Append($"; {name}: {value}");
    }

    public override string ToString()
    {
        var sb = new StringBuilder("XUnitFilter [");

        sb.Append($"Type: {FilterType}; ");
        sb.Append(Exclude ? "exclude" : "include");

        if (!string.IsNullOrEmpty(AssemblyName))
        {
            sb.Append($"; AssemblyName: {AssemblyName}");
        }

        switch (FilterType)
        {
            case XUnitFilterType.Assembly:
                break;

            case XUnitFilterType.Namespace:
                AppendDesc(sb, "Namespace", SelectorValue);
                break;

            case XUnitFilterType.Single:
                AppendDesc(sb, "Method", SelectorValue);
                break;

            case XUnitFilterType.Trait:
                AppendDesc(sb, "Trait name", SelectorName);
                AppendDesc(sb, "Trait value", SelectorValue);
                break;

            case XUnitFilterType.TypeName:
                AppendDesc(sb, "Class", SelectorValue);
                break;

            default:
                sb.Append("; Unknown filter type");
                break;
        }
        sb.Append(']');

        return sb.ToString();
    }
}
