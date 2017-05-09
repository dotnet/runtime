﻿using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using System.Runtime.CompilerServices;
using System.IO;
using Mono.Linker.Tests.TestCasesRunner;

namespace Mono.Linker.Tests.TestCases
{
	static class TestDatabase
	{
		public static IEnumerable<TestCaseData> XmlTests()
		{
			return NUnitCasesByPrefix("LinkXml.");
		}

		public static IEnumerable<TestCaseData> BasicTests()
		{
			return NUnitCasesByPrefix("Basic.");
		}

		public static IEnumerable<TestCaseData> VirtualMethodsTests()
		{
			return NUnitCasesByPrefix("VirtualMethods.");
		}

		public static IEnumerable<TestCaseData> AttributeTests()
		{
			return NUnitCasesByPrefix("Attributes.");
		}

		public static IEnumerable<TestCaseData> GenericsTests()
		{
			return NUnitCasesByPrefix("Generics.");
		}

		public static IEnumerable<TestCaseData> CoreLinkTests()
		{
			return NUnitCasesByPrefix("CoreLink.");
		}

		public static IEnumerable<TestCaseData> StaticsTests()
		{
			return NUnitCasesByPrefix("Statics.");
		}

		public static IEnumerable<TestCaseData> InteropTests()
		{
			return NUnitCasesByPrefix("Interop.");
		}

		public static IEnumerable<TestCaseData> ReferencesTests()
		{
			return NUnitCasesByPrefix("References.");
		}

		public static IEnumerable<TestCaseData> OtherTests()
		{
			var allGroupedTestNames = new HashSet<string>(
				XmlTests()
					.Concat(BasicTests())
					.Concat(XmlTests())
					.Concat(VirtualMethodsTests())
					.Concat(AttributeTests())
					.Concat(GenericsTests())
					.Concat(CoreLinkTests())
					.Concat(StaticsTests())
					.Concat(InteropTests())
					.Concat(ReferencesTests ())
					.Select(c => ((TestCase)c.Arguments[0]).ReconstructedFullTypeName));

			return AllCases().Where(c => !allGroupedTestNames.Contains(c.ReconstructedFullTypeName)).Select(c => CreateNUnitTestCase(c, c.DisplayName));
		}

		static IEnumerable<TestCase> AllCases()
		{
			string rootSourceDirectory;
			string testCaseAssemblyPath;
			GetDirectoryPaths(out rootSourceDirectory, out testCaseAssemblyPath);
			return new TestCaseCollector(rootSourceDirectory, testCaseAssemblyPath)
				.Collect()
				.OrderBy(c => c.DisplayName)
				.ToArray();
		}

		static IEnumerable<TestCaseData> NUnitCasesByPrefix(string testNamePrefix)
		{
			return AllCases()
				.Where(c => c.DisplayName.StartsWith(testNamePrefix))
				.Select(c => CreateNUnitTestCase(c, c.DisplayName.Substring(testNamePrefix.Length)))
				.OrderBy(c => c.TestName);
		}

		static TestCaseData CreateNUnitTestCase(TestCase testCase, string displayName)
		{
			var data = new TestCaseData(testCase);
			data.SetName(displayName);
			return data;
		}

		static void GetDirectoryPaths(out string rootSourceDirectory, out string testCaseAssemblyPath, [CallerFilePath] string thisFile = null)
		{
			var thisDirectory = Path.GetDirectoryName(thisFile);
			rootSourceDirectory = Path.GetFullPath(Path.Combine(thisDirectory, "..", "Mono.Linker.Tests.Cases"));
			testCaseAssemblyPath = Path.GetFullPath(Path.Combine(rootSourceDirectory, "bin", "Debug", "Mono.Linker.Tests.Cases.dll"));
		}
	}
}
