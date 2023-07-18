// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

		public static IEnumerable<object[]> DataFlow ()
		{
			return TestNamesBySuiteName ();
		}

		public static IEnumerable<object[]> DynamicDependencies ()
		{
			return TestNamesBySuiteName ();
		}

		public static IEnumerable<object[]> Generics ()
		{
			return TestNamesBySuiteName ();
		}

		public static IEnumerable<object[]> LinkXml()
		{
			return TestNamesBySuiteName();
		}

		public static IEnumerable<object[]> Reflection ()
		{
			return TestNamesBySuiteName ();
		}

		public static IEnumerable<object[]> Repro ()
		{
			return TestNamesBySuiteName ();
		}

		public static IEnumerable<object[]> RequiresCapability ()
		{
			return TestNamesBySuiteName ();
		}

		public static IEnumerable<object[]> SingleFile ()
		{
			return TestNamesBySuiteName ();
		}

		public static IEnumerable<object[]> UnreachableBlock ()
		{
			return TestNamesBySuiteName ();
		}

		public static IEnumerable<object[]> Warnings ()
		{
			return TestNamesBySuiteName ();
		}

		public static TestCaseCollector CreateCollector ()
		{
			GetDirectoryPaths (out string rootSourceDirectory, out string testCaseAssemblyPath);
			return new TestCaseCollector (rootSourceDirectory, testCaseAssemblyPath);
		}

		public static NPath TestCasesRootDirectory {
			get {
				GetDirectoryPaths (out string rootSourceDirectory, out string _);
				return rootSourceDirectory.ToNPath ();
			}
		}

		private static IEnumerable<TestCase> AllCases ()
		{
			_cachedAllCases ??= CreateCollector ()
					.Collect ()
					.Where (c => c != null)
					.OrderBy (c => c.DisplayName)
					.ToArray ();

			return _cachedAllCases;
		}

		public static TestCase? GetTestCaseFromName (string name)
		{
			return AllCases ().FirstOrDefault (c => c.DisplayName == name);
		}

		private static IEnumerable<object[]> TestNamesBySuiteName ([CallerMemberName] string suiteName = "")
		{
			return AllCases ()
				.Where (c => c.TestSuiteDirectory.FileName == suiteName)
				.Select (c => c.DisplayName)
				.OrderBy (c => c)
				.Select (c => new object[] { c });
		}

		private static void GetDirectoryPaths (out string rootSourceDirectory, out string testCaseAssemblyPath)
		{
			rootSourceDirectory = Path.GetFullPath (Path.Combine (PathUtilities.GetTestsSourceRootDirectory (), "Mono.Linker.Tests.Cases"));
			testCaseAssemblyPath = PathUtilities.GetTestAssemblyPath ("Mono.Linker.Tests.Cases");
		}
	}
}
