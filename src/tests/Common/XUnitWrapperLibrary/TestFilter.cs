// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
namespace XUnitWrapperLibrary;

public class TestFilter
{
    public interface ISearchClause
    {
        bool IsMatch(string fullyQualifiedName, string displayName, string[] traits);
    }

    public enum TermKind
    {
        FullyQualifiedName,
        DisplayName
    }
    public sealed class NameClause : ISearchClause
    {
        public NameClause(TermKind kind, string filter, bool substring)
        {
            Kind = kind;
            Filter = filter;
            Substring = substring;
        }
        public TermKind Kind { get; }
        public string Filter { get; }
        public bool Substring { get; }

        public bool IsMatch(string fullyQualifiedName, string displayName, string[] traits)
        {
            string stringToSearch = Kind switch
            {
                TermKind.FullyQualifiedName => fullyQualifiedName,
                TermKind.DisplayName => displayName,
                _ => throw new InvalidOperationException()
            };

            if (Substring)
            {
                return stringToSearch.Contains(Filter);
            }
            return stringToSearch == Filter;
        }
    }

    public sealed class AndClause : ISearchClause
    {
        private ISearchClause _left;
        private ISearchClause _right;

        public AndClause(ISearchClause left, ISearchClause right)
        {
            _left = left;
            _right = right;
        }

        public bool IsMatch(string fullyQualifiedName, string displayName, string[] traits) => _left.IsMatch(fullyQualifiedName, displayName, traits) && _right.IsMatch(fullyQualifiedName, displayName, traits);
    }

    public sealed class OrClause : ISearchClause
    {
        private ISearchClause _left;
        private ISearchClause _right;

        public OrClause(ISearchClause left, ISearchClause right)
        {
            _left = left;
            _right = right;
        }

        public bool IsMatch(string fullyQualifiedName, string displayName, string[] traits) => _left.IsMatch(fullyQualifiedName, displayName, traits) || _right.IsMatch(fullyQualifiedName, displayName, traits);
    }

    public sealed class NotClause : ISearchClause
    {
        private ISearchClause _inner;

        public NotClause(ISearchClause inner)
        {
            _inner = inner;
        }

        public bool IsMatch(string fullyQualifiedName, string displayName, string[] traits) => !_inner.IsMatch(fullyQualifiedName, displayName, traits);
    }

    private readonly ISearchClause? _filter;

    // Test exclusion list is a compatibility measure allowing for a smooth migration
    // away from the legacy issues.targets issue tracking system. Before we migrate
    // all tests to the new model, it's easier to keep bug exclusions in the existing
    // issues.targets file as a split model would be very confusing for developers
    // and test monitors.
    private readonly HashSet<string>? _testExclusionList;

    public TestFilter(string? filterString, HashSet<string>? testExclusionList)
    {
        if (filterString is not null)
        {
            if (filterString.IndexOfAny(new[] { '!', '(', ')', '~', '=' }) != -1)
            {
                throw new ArgumentException("Complex test filter expressions are not supported today. The only filters supported today are the simple form supported in 'dotnet test --filter' (substrings of the test's fully qualified name). If further filtering options are desired, file an issue on dotnet/runtime for support.", nameof(filterString));
            }
            _filter = new NameClause(TermKind.FullyQualifiedName, filterString, substring: true);
        }
        _testExclusionList = testExclusionList;
    }

    public TestFilter(ISearchClause? filter, HashSet<string>? testExclusionList)
    {
        _filter = filter;
        _testExclusionList = testExclusionList;
    }

    public bool ShouldRunTest(string fullyQualifiedName, string displayName, string[]? traits = null)
    {
        if (_testExclusionList is not null && _testExclusionList.Contains(displayName))
        {
            return false;
        }
        if (_filter is null)
        {
            return true;
        }
        return _filter.IsMatch(fullyQualifiedName, displayName, traits ?? Array.Empty<string>());
    }
    
    public static HashSet<string> LoadTestExclusionList()
    {
        HashSet<string> output = new ();
        string? testExclusionListPath = Environment.GetEnvironmentVariable("TestExclusionListPath");
        if (!string.IsNullOrEmpty(testExclusionListPath))
        {
            output.UnionWith(File.ReadAllLines(testExclusionListPath));
        }
        return output;
    }
}
