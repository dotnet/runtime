using Mono.Linker.Tests.TestCasesRunner;
using NUnit.Framework;

namespace Mono.Linker.Tests.TestCases
{
	[TestFixture]
	public class All
	{
		[TestCaseSource (typeof (TestDatabase), nameof (TestDatabase.BasicTests))]
		public void BasicTests (TestCase testCase)
		{
			Run (testCase);
		}

		[TestCaseSource (typeof (TestDatabase), nameof (TestDatabase.AdvancedTests))]
		public void AdvancedTests (TestCase testCase)
		{
			Run (testCase);
		}

		[TestCaseSource (typeof (TestDatabase), nameof (TestDatabase.XmlTests))]
		public void XmlTests (TestCase testCase)
		{
			Run (testCase);
		}

		[TestCaseSource (typeof (TestDatabase), nameof (TestDatabase.AttributeTests))]
		public void AttributesTests (TestCase testCase)
		{
			Run (testCase);
		}
		
		[TestCaseSource (typeof (TestDatabase), nameof (TestDatabase.AttributeDebuggerTests))]
		public void AttributesDebuggerTests (TestCase testCase)
		{
			Run (testCase);
		}

		[TestCaseSource (typeof (TestDatabase), nameof (TestDatabase.GenericsTests))]
		public void GenericsTests (TestCase testCase)
		{
			Run (testCase);
		}

		[TestCaseSource (typeof (TestDatabase), nameof (TestDatabase.StaticsTests))]
		public void StaticsTests (TestCase testCase)
		{
			Run (testCase);
		}

		[TestCaseSource (typeof (TestDatabase), nameof (TestDatabase.CoreLinkTests))]
		public void CoreLinkTests (TestCase testCase)
		{
			Run (testCase);
		}

		[TestCaseSource (typeof (TestDatabase), nameof (TestDatabase.InteropTests))]
		public void InteropTests (TestCase testCase)
		{
			Run (testCase);
		}

		[TestCaseSource(typeof(TestDatabase), nameof(TestDatabase.ReferencesTests))]
		public void ReferencesTests(TestCase testCase)
		{
			Run(testCase);
		}

		[TestCaseSource (typeof (TestDatabase), nameof (TestDatabase.ResourcesTests))]
		public void ResourcesTests (TestCase testCase)
		{
			Run (testCase);
		}

		[TestCaseSource (typeof (TestDatabase), nameof (TestDatabase.TypeForwardingTests))]
		public void TypeForwardingTests (TestCase testCase)
		{
			Run (testCase);
		}

		[TestCaseSource(typeof (TestDatabase), nameof (TestDatabase.TestFrameworkTests))]
		public void TestFrameworkTests (TestCase testCase)
		{
			Run (testCase);
		}

		[TestCaseSource (typeof (TestDatabase), nameof (TestDatabase.ReflectionTests))]
		public void ReflectionTests (TestCase testCase)
		{
			Run (testCase);
		}
		
		[TestCaseSource (typeof (TestDatabase), nameof (TestDatabase.PreserveDependenciesTests))]
		public void PreserveDependenciesTests (TestCase testCase)
		{
			Run (testCase);
		}
		
		[TestCaseSource (typeof (TestDatabase), nameof (TestDatabase.SymbolsTests))]
		public void SymbolsTests (TestCase testCase)
		{
			Run (testCase);
		}
		
		[TestCaseSource (typeof (TestDatabase), nameof (TestDatabase.LibrariesTests))]
		public void LibrariesTests (TestCase testCase)
		{
			Run (testCase);
		}
		
		[TestCaseSource (typeof (TestDatabase), nameof (TestDatabase.InheritanceInterfaceTests))]
		public void InheritanceInterfaceTests (TestCase testCase)
		{
			Run (testCase);
		}
		
		[TestCaseSource (typeof (TestDatabase), nameof (TestDatabase.InheritanceAbstractClassTests))]
		public void InheritanceAbstractClassTests (TestCase testCase)
		{
			Run (testCase);
		}
		
		[TestCaseSource (typeof (TestDatabase), nameof (TestDatabase.InheritanceVirtualMethodsTests))]
		public void InheritanceVirtualMethodsTests (TestCase testCase)
		{
			Run (testCase);
		}
		
		[TestCaseSource (typeof (TestDatabase), nameof (TestDatabase.InheritanceComplexTests))]
		public void InheritanceComplexTests (TestCase testCase)
		{
			Run (testCase);
		}

		[TestCaseSource (typeof (TestDatabase), nameof (TestDatabase.BCLFeaturesTests))]
		public void BCLFeaturesTests (TestCase testCase)
		{
			Run (testCase);
		}

		[TestCaseSource (typeof (TestDatabase), nameof (TestDatabase.CommandLineTests))]
		public void CommandLineTests (TestCase testCase)
		{
			Run (testCase);
		}

		protected virtual void Run (TestCase testCase)
		{
			var runner = new TestRunner (new ObjectFactory ());
			var linkedResult = runner.Run (testCase);
			new ResultChecker ().Check (linkedResult);
		}
	}
}
