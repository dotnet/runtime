// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Extensions;
using Mono.Linker.Tests.TestCasesRunner;

namespace Mono.Linker.Tests.TestCases
{
    public static class TestDatabase
    {
        private static TestCase[]? _cachedAllCases;

        public static IEnumerable<object[]> Basic()
        {
            return TestNamesBySuiteName();
        }

        public static IEnumerable<object[]> MultiAssembly()
        {
            return TestNamesBySuiteName();
        }

        public static IEnumerable<object[]> LinkXml()
        {
            return TestNamesBySuiteName();
        }

        public static IEnumerable<object[]> FeatureSettings()
        {
            return TestNamesBySuiteName();
        }

        public static TestCaseCollector CreateCollector()
        {
            GetDirectoryPaths(out string rootSourceDirectory, out string testCaseAssemblyRoot);
            return new TestCaseCollector(rootSourceDirectory, testCaseAssemblyRoot);
        }

        public static NPath TestCasesRootDirectory
        {
            get
            {
                GetDirectoryPaths(out string rootSourceDirectory, out string _);
                return rootSourceDirectory.ToNPath();
            }
        }

        private static IEnumerable<TestCase> AllCases()
        {
            _cachedAllCases ??= CreateCollector()
                    .Collect()
                    .OrderBy(c => c.DisplayName)
                    .ToArray();

            return _cachedAllCases;
        }

        public static TestCase? GetTestCaseFromName(string name)
        {
            return AllCases().FirstOrDefault(c => c.DisplayName == name);
        }

        private static IEnumerable<object[]> TestNamesBySuiteName([CallerMemberName] string suiteName = "")
        {
            return AllCases()
                .Where(c => c.TestSuiteDirectory.FileName == suiteName)
                .Select(c => c.DisplayName)
                .OrderBy(c => c)
                .Select(c => new object[] { c });
        }

        private static void GetDirectoryPaths(out string rootSourceDirectory, out string testCaseAssemblyRoot)
        {
            rootSourceDirectory = Path.GetFullPath(Path.Combine(PathUtilities.GetTestsSourceRootDirectory(), "Mono.Linker.Tests.Cases"));
            testCaseAssemblyRoot = PathUtilities.GetTestAssemblyRoot("Mono.Linker.Tests.Cases");
        }
    }
}
