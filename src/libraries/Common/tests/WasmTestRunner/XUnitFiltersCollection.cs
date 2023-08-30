// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.XHarness.TestRunners.Common;
using Xunit.Abstractions;

#nullable enable
namespace Microsoft.DotNet.XHarness.TestRunners.Xunit;

/// <summary>
/// Class that contains a collection of filters and can be used to decide if a test should be executed or not.
/// </summary>
internal class XUnitFiltersCollection : List<XUnitFilter>
{
    /// <summary>
    /// Return all the filters that are applied to assemblies.
    /// </summary>
    public IEnumerable<XUnitFilter> AssemblyFilters
        => Enumerable.Where(this, f => f.FilterType == XUnitFilterType.Assembly);

    /// <summary>
    /// Return all the filters that are applied to test cases.
    /// </summary>
    public IEnumerable<XUnitFilter> TestCaseFilters
        => Enumerable.Where(this, f => f.FilterType != XUnitFilterType.Assembly);

    // loop over all the filters, if we have conflicting filters, that is, one exclude and other one
    // includes, we will always include since it is better to run a test thant to skip it and think
    // you ran in.
    private bool IsExcludedInternal(IEnumerable<XUnitFilter> filters, Func<XUnitFilter, bool> isExcludedCb)
    {
        // No filters           : include by default
        // Any exclude filters  : include by default
        // Only include filters : exclude by default
        var isExcluded = filters.Any() && filters.All(f => !f.Exclude);
        foreach (var filter in filters)
        {
            var doesExclude = isExcludedCb(filter);
            if (filter.Exclude)
            {
                isExcluded |= doesExclude;
            }
            else
            {
                // filter does not exclude, that means that if it include, we should include and break the
                // loop, always include
                if (!doesExclude)
                {
                    return false;
                }
            }
        }

        return isExcluded;
    }

    public bool IsExcluded(TestAssemblyInfo assembly, Action<string>? log = null) =>
        IsExcludedInternal(AssemblyFilters, f => f.IsExcluded(assembly, log));

    public bool IsExcluded(ITestCase testCase, Action<string>? log = null)
    {
        // Check each type of filter separately. For conflicts within a type of filter, we want the inclusion
        // (the logic in IsExcludedInternal), but if all filters for a filter type exclude a test case, we want
        // the exclusion. For example, if a test class is included, but it contains tests that have excluded
        // traits, the behaviour should be to run all tests in that class without the excluded traits.
        foreach (IGrouping<XUnitFilterType, XUnitFilter> filterGroup in TestCaseFilters.GroupBy(f => f.FilterType))
        {
            if (IsExcludedInternal(filterGroup, f => f.IsExcluded(testCase, log)))
            {
                return true;
            }
        }

        return false;
    }
}
