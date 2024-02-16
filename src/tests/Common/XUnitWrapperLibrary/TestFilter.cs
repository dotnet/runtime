// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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

    // Test exclusion list is a compatibility measure allowing for a smooth migration
    // away from the legacy issues.targets issue tracking system. Before we migrate
    // all tests to the new model, it's easier to keep bug exclusions in the existing
    // issues.targets file as a split model would be very confusing for developers
    // and test monitors.

    // Explanation on the Test Exclusion Table is detailed in method LoadTestExclusionTable()
    // later on in this file.
    private readonly Dictionary<string, string>? _testExclusionTable;

    private readonly int _stripe;
    private readonly int _stripeCount = 1;
    private int _shouldRunQuery = -1;

    public TestFilter(string? filterString, Dictionary<string, string>? testExclusionTable) :
        this(filterString == null ? Array.Empty<string>() : new string[]{filterString}, testExclusionTable)
    {
    }

    public TestFilter(string[] filterArgs, Dictionary<string, string>? testExclusionTable)
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
            if (filterString.IndexOfAny(new[] { '!', '(', ')', '~', '=' }) != -1)
            {
                throw new ArgumentException("Complex test filter expressions are not supported today."
                                          + " The only filters currently supported are the simple forms"
                                          + " supported in 'dotnet test --filter' (substrings of the"
                                          + " test's fully qualified name). If further filtering options"
                                          + " are desired, file an issue on dotnet/runtime for support.",
                                            nameof(filterArgs));
            }
            _filter = new NameClause(TermKind.FullyQualifiedName, filterString, substring: true);
        }
        _testExclusionTable = testExclusionTable;
    }

    public TestFilter(ISearchClause? filter, Dictionary<string, string>? testExclusionTable)
    {
        _filter = filter;
        _testExclusionTable = testExclusionTable;
    }

    public bool ShouldRunTest(string fullyQualifiedName, string displayName, string[]? traits = null)
    {
        bool shouldRun;
        if (_testExclusionTable is not null && _testExclusionTable.ContainsKey(displayName.Replace("\\", "/")))
        {
            shouldRun = false;
        }
        else if (_filter is null)
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

    public string GetTestExclusionReason(string testDisplayName)
    {
        if (_testExclusionTable is null)
            return string.Empty;

        string trueDisplayName = testDisplayName.Replace("\\", "/");

        return _testExclusionTable.ContainsKey(trueDisplayName)
               ? _testExclusionTable[trueDisplayName]
               : string.Empty;
    }

    // GH dotnet/runtime issue #91562: Some tests are purposefully not run for a number
    // of reasons. They are specified in src/tests/issues.targets, along with a brief
    // explanation on why they are skipped inside an '<Issue>' tag.
    //
    // When building any test or test subtree, if any of the tests built matches an entry
    // in issues.targets, then the exclusions list file ($CORE_ROOT/TestExclusions.txt is
    // the default) is updated by adding a new a comma-separated entry:
    //
    // 1) Test's Path (What is added to <ExcludeList> in issues.targets)
    // 2) Reason for Skipping (What is written in the <Issue> tag)
    //
    // When a test runner is executed (e.g. Methodical_d1.sh), it uses a compiler-generated
    // source file to actually run the tests (e.g. FullRunner.g.cs - This is detailed in
    // XUnitWrapperGenerator.cs). This generated source file is the one in charge of
    // reading the test exclusions list file, and stores the comma-separated values into a
    // table represented by the dictionary called _testExclusionTable in this file.

    public static Dictionary<string, string> LoadTestExclusionTable()
    {
        Dictionary<string, string> output = new Dictionary<string, string>();

        // Try reading the exclusion list as a base64-encoded semicolon-delimited string as a commmand-line arg.
        string[] arguments = Environment.GetCommandLineArgs();
        string? testExclusionListArg = arguments.FirstOrDefault(arg => arg.StartsWith("--exclusion-list="));

        if (!string.IsNullOrEmpty(testExclusionListArg))
        {
            string testExclusionListPathFromCommandLine = testExclusionListArg.Substring("--exclusion-list=".Length);
            ReadExclusionListToTable(testExclusionListPathFromCommandLine, output);
        }

        // Try reading the exclusion list as a line-delimited file.
        string? testExclusionListPath = Environment.GetEnvironmentVariable("TestExclusionListPath");

        if (!string.IsNullOrEmpty(testExclusionListPath))
        {
            ReadExclusionListToTable(testExclusionListPath, output);
        }
        return output;
    }

    private static void ReadExclusionListToTable(string exclusionListPath,
                                                 Dictionary<string, string> table)
    {
        IEnumerable<string[]> excludedTestsWithReasons = File.ReadAllLines(exclusionListPath)
                                                             .Select(t => t.Split(','));

        foreach (string[] testInfo in excludedTestsWithReasons)
        {
            // Each line read from the exclusion list file follows the following format:
            //
            // Test Path, Reason For Skipping
            //
            // This translates to the two-element arrays we are adding to the test
            // exclusions table here.

            string testPath = testInfo[0];
            string skipReason = testInfo.Length > 1 ? testInfo[1] : string.Empty;

            if (!table.ContainsKey(testPath))
            {
                table.Add(testPath, skipReason);
            }
        }
    }
}
