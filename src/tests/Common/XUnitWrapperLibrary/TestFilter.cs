// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace XUnitWrapperLibrary;

using Interlocked = System.Threading.Interlocked;

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

        public override string ToString()
        {
            return $"{Kind}{(Substring ? "~" : "=")}{Filter}";
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

        public bool IsMatch(string fullyQualifiedName, string displayName, string[] traits) =>
            _left.IsMatch(fullyQualifiedName, displayName, traits)
            && _right.IsMatch(fullyQualifiedName, displayName, traits);

        public override string ToString()
        {
            return $"({_left}) && ({_right})";
        }
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

        public bool IsMatch(string fullyQualifiedName, string displayName, string[] traits) =>
            _left.IsMatch(fullyQualifiedName, displayName, traits)
            || _right.IsMatch(fullyQualifiedName, displayName, traits);

        public override string ToString()
        {
            return $"({_left}) || ({_right})";
        }
    }

    public sealed class NotClause : ISearchClause
    {
        private ISearchClause _inner;

        public NotClause(ISearchClause inner)
        {
            _inner = inner;
        }

        public bool IsMatch(string fullyQualifiedName, string displayName, string[] traits)
            => !_inner.IsMatch(fullyQualifiedName, displayName, traits);

        public override string ToString()
        {
            return $"!({_inner})";
        }
    }

    private readonly ISearchClause? _filter;

    private readonly int _stripe;
    private readonly int _stripeCount = 1;
    private int _shouldRunQuery = -1;

    public TestFilter(string? filterString) :
        this(filterString == null ? Array.Empty<string>() : new string[]{filterString})
    {
    }

    public TestFilter(string[] filterArgs)
    {
        string? filterString = null;

        for (int i = 0; i < filterArgs.Length; i++)
        {
            if (filterArgs[i].StartsWith("-stripe"))
            {
                _stripe = int.Parse(filterArgs[++i]);
                _stripeCount = int.Parse(filterArgs[++i]);
            }
            else
            {
                filterString ??= filterArgs[0];
            }
        }

        var stripeEnvironment = Environment.GetEnvironmentVariable("TEST_HARNESS_STRIPE_TO_EXECUTE");
        if (!string.IsNullOrEmpty(stripeEnvironment) && stripeEnvironment != ".0.1")
        {
            var stripes = stripeEnvironment.Split('.');
            if (stripes.Length == 3)
            {
                Console.WriteLine($"Test striping enabled via TEST_HARNESS_STRIPE_TO_EXECUTE environment"
                                  + $" variable set to '{stripeEnvironment}'");
                _stripe = int.Parse(stripes[1]);
                _stripeCount = int.Parse(stripes[2]);
            }
        }

        if (filterString is not null)
        {
            _filter = ParseFilter(filterString, nameof(filterArgs));
        }
    }

    public TestFilter(ISearchClause? filter)
    {
        _filter = filter;
    }

    public bool ShouldRunTest(string fullyQualifiedName, string displayName, string[]? traits = null)
    {
        bool shouldRun;
        if (_filter is null)
        {
            shouldRun = true;
        }
        else
        {
            shouldRun = _filter.IsMatch(fullyQualifiedName, displayName, traits ?? Array.Empty<string>());
        }

        if (shouldRun)
        {
            // Test stripe, if true, then report success
            return ((Interlocked.Increment(ref _shouldRunQuery)) % _stripeCount) == _stripe;
        }
        return false;
    }

    private static ISearchClause ParseFilter(string filterString, string argName)
    {
        ReadOnlySpan<string> unsupported = ["|", "&", "(", ")", "!=", "!~"];
        foreach (string s in unsupported)
        {
            if (filterString.Contains(s))
            {
                throw new ArgumentException("Test filtering with |, &, (, ), !=, !~ is not supported", argName);
            }
        }

        int delimiter = filterString.IndexOfAny(['=', '~']);
        if (delimiter == -1)
        {
            return new NameClause(TermKind.FullyQualifiedName, filterString, substring: true);
        }

        bool isSubstring = filterString[delimiter] == '~';
        string termName = filterString.Substring(0, delimiter);
        string testName = filterString.Substring(delimiter + 1);

        TermKind termKind = termName switch
        {
            "FullyQualifiedName" => TermKind.FullyQualifiedName,
            "DisplayName" => TermKind.DisplayName,
            _ => throw new ArgumentException("Test filtering not supported with property " + termName, argName),
        };

        return new NameClause(termKind, testName, substring: isSubstring);
    }
}
