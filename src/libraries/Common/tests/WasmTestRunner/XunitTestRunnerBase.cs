// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.XHarness.TestRunners.Common;

#nullable enable
namespace Microsoft.DotNet.XHarness.TestRunners.Xunit;
public abstract class MyXunitTestRunnerBase : TestRunner
{
    private protected XUnitFiltersCollection _filters = new();

    protected MyXunitTestRunnerBase(LogWriter logger) : base(logger)
    {
    }

    public override void SkipTests(IEnumerable<string> tests)
    {
        if (tests.Any())
        {
            // create a single filter per test
            foreach (var t in tests)
            {
                if (t.StartsWith("KLASS:", StringComparison.Ordinal))
                {
                    var klass = t.Replace("KLASS:", "");
                    _filters.Add(XUnitFilter.CreateClassFilter(klass, true));
                }
                else if (t.StartsWith("KLASS32:", StringComparison.Ordinal) && IntPtr.Size == 4)
                {
                    var klass = t.Replace("KLASS32:", "");
                    _filters.Add(XUnitFilter.CreateClassFilter(klass, true));
                }
                else if (t.StartsWith("KLASS64:", StringComparison.Ordinal) && IntPtr.Size == 8)
                {
                    var klass = t.Replace("KLASS32:", "");
                    _filters.Add(XUnitFilter.CreateClassFilter(klass, true));
                }
                else if (t.StartsWith("Platform32:", StringComparison.Ordinal) && IntPtr.Size == 4)
                {
                    var filter = t.Replace("Platform32:", "");
                    _filters.Add(XUnitFilter.CreateSingleFilter(filter, true));
                }
                else
                {
                    _filters.Add(XUnitFilter.CreateSingleFilter(t, true));
                }
            }
        }
    }

    public override void SkipCategories(IEnumerable<string> categories) => SkipCategories(categories, isExcluded: true);

    public virtual void SkipCategories(IEnumerable<string> categories, bool isExcluded)
    {
        if (categories == null)
        {
            throw new ArgumentNullException(nameof(categories));
        }

        foreach (var c in categories)
        {
            var traitInfo = c.Split('=');
            if (traitInfo.Length == 2)
            {
                _filters.Add(XUnitFilter.CreateTraitFilter(traitInfo[0], traitInfo[1], isExcluded));
            }
            else
            {
                _filters.Add(XUnitFilter.CreateTraitFilter(c, null, isExcluded));
            }
        }
    }

    public override void SkipMethod(string method, bool isExcluded)
        => _filters.Add(XUnitFilter.CreateSingleFilter(singleTestName: method, exclude: isExcluded));

    public override void SkipClass(string className, bool isExcluded)
        => _filters.Add(XUnitFilter.CreateClassFilter(className: className, exclude: isExcluded));

    public virtual void SkipNamespace(string namespaceName, bool isExcluded)
        => _filters.Add(XUnitFilter.CreateNamespaceFilter(namespaceName, exclude: isExcluded));
}
