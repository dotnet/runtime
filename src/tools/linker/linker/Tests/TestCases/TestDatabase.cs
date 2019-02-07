using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using System.Runtime.CompilerServices;
using System.IO;
using Mono.Linker.Tests.TestCasesRunner;

namespace Mono.Linker.Tests.TestCases
{
	public static class TestDatabase
	{
		private static TestCase[] _cachedAllCases;
		
		public static IEnumerable<TestCaseData> XmlTests()
		{
			return NUnitCasesBySuiteName("LinkXml");
		}

		public static IEnumerable<TestCaseData> BasicTests()
		{
			return NUnitCasesBySuiteName("Basic");
		}

		public static IEnumerable<TestCaseData> AttributeTests()
		{
			return NUnitCasesBySuiteName("Attributes");
		}
		
		public static IEnumerable<TestCaseData> AttributeDebuggerTests ()
		{
			return NUnitCasesBySuiteName ("Attributes.Debugger");
		}

		public static IEnumerable<TestCaseData> GenericsTests()
		{
			return NUnitCasesBySuiteName("Generics");
		}

		public static IEnumerable<TestCaseData> CoreLinkTests()
		{
			return NUnitCasesBySuiteName("CoreLink");
		}

		public static IEnumerable<TestCaseData> StaticsTests()
		{
			return NUnitCasesBySuiteName("Statics");
		}

		public static IEnumerable<TestCaseData> InteropTests()
		{
			return NUnitCasesBySuiteName("Interop");
		}

		public static IEnumerable<TestCaseData> ReferencesTests()
		{
			return NUnitCasesBySuiteName("References");
		}

		public static IEnumerable<TestCaseData> ResourcesTests ()
		{
			return NUnitCasesBySuiteName ("Resources");
		}

		public static IEnumerable<TestCaseData> TypeForwardingTests ()
		{
			return NUnitCasesBySuiteName ("TypeForwarding");
		}

		public static IEnumerable<TestCaseData> TestFrameworkTests ()
		{
			return NUnitCasesBySuiteName ("TestFramework");
		}

		public static IEnumerable<TestCaseData> ReflectionTests ()
		{
			return NUnitCasesBySuiteName ("Reflection");
		}
		
		public static IEnumerable<TestCaseData> SymbolsTests ()
		{
			return NUnitCasesBySuiteName ("Symbols");
		}
		
		public static IEnumerable<TestCaseData> PreserveDependenciesTests ()
		{
			return NUnitCasesBySuiteName ("PreserveDependencies");
		}
		
		public static IEnumerable<TestCaseData> LibrariesTests ()
		{
			return NUnitCasesBySuiteName ("Libraries");
		}

		public static IEnumerable<TestCaseData> AdvancedTests ()
		{
			return NUnitCasesBySuiteName ("Advanced");
		}
		
		public static IEnumerable<TestCaseData> InheritanceInterfaceTests ()
		{
			return NUnitCasesBySuiteName ("Inheritance.Interfaces");
		}
		
		public static IEnumerable<TestCaseData> InheritanceAbstractClassTests ()
		{
			return NUnitCasesBySuiteName ("Inheritance.AbstractClasses");
		}
		
		public static IEnumerable<TestCaseData> InheritanceVirtualMethodsTests ()
		{
			return NUnitCasesBySuiteName ("Inheritance.VirtualMethods");
		}
		
		public static IEnumerable<TestCaseData> InheritanceComplexTests ()
		{
			return NUnitCasesBySuiteName ("Inheritance.Complex");
		}

		public static IEnumerable<TestCaseData> BCLFeaturesTests ()
		{
			return NUnitCasesBySuiteName ("BCLFeatures");
		}

		public static IEnumerable<TestCaseData> CommandLineTests ()
		{
			return NUnitCasesBySuiteName ("CommandLine");
		}

		public static TestCaseCollector CreateCollector ()
		{
			string rootSourceDirectory;
			string testCaseAssemblyPath;
			GetDirectoryPaths (out rootSourceDirectory, out testCaseAssemblyPath);
			return new TestCaseCollector (rootSourceDirectory, testCaseAssemblyPath);
		}

		static IEnumerable<TestCase> AllCases ()
		{
			if (_cachedAllCases == null)
				_cachedAllCases = CreateCollector ()
					.Collect ()
					.OrderBy (c => c.DisplayName)
					.ToArray ();

			return _cachedAllCases;
		}

		static IEnumerable<TestCaseData> NUnitCasesBySuiteName(string suiteName)
		{
			return AllCases()
				.Where(c => c.TestSuiteDirectory.FileName == suiteName)
				.Select(c => CreateNUnitTestCase(c, c.DisplayName.Substring(suiteName.Length + 1)))
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
#if DEBUG
			var configDirectoryName = "Debug";
#else
			var configDirectoryName = "Release";
#endif
			testCaseAssemblyPath = Path.GetFullPath(Path.Combine(rootSourceDirectory, "bin", configDirectoryName, "Mono.Linker.Tests.Cases.dll"));
		}
	}
}
