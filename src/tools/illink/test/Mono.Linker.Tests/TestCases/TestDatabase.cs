// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Linker.Tests.Extensions;
using Mono.Linker.Tests.TestCasesRunner;
using NUnit.Framework;

namespace Mono.Linker.Tests.TestCases
{
	public static class TestDatabase
	{
		private static TestCase[] _cachedAllCases;

		public static IEnumerable<TestCaseData> AdvancedTests ()
		{
			return NUnitCasesBySuiteName ("Advanced");
		}

		public static IEnumerable<TestCaseData> AttributeDebuggerTests ()
		{
			return NUnitCasesBySuiteName ("Attributes.Debugger");
		}

		public static IEnumerable<TestCaseData> AttributeTests ()
		{
			return NUnitCasesBySuiteName ("Attributes");
		}

		public static IEnumerable<TestCaseData> AttributesStructLayoutTests ()
		{
			return NUnitCasesBySuiteName ("Attributes.StructLayout");
		}

		public static IEnumerable<TestCaseData> BCLFeaturesTests ()
		{
			return NUnitCasesBySuiteName ("BCLFeatures");
		}

		public static IEnumerable<TestCaseData> BasicTests ()
		{
			return NUnitCasesBySuiteName ("Basic");
		}

		public static IEnumerable<TestCaseData> CodegenAnnotationTests ()
		{
			return NUnitCasesBySuiteName ("CodegenAnnotation");
		}

		public static IEnumerable<TestCaseData> CommandLineTests ()
		{
			return NUnitCasesBySuiteName ("CommandLine");
		}

		public static IEnumerable<TestCaseData> ComponentModelTests ()
		{
			return NUnitCasesBySuiteName ("ComponentModel");
		}

		public static IEnumerable<TestCaseData> CoreLinkTests ()
		{
			return NUnitCasesBySuiteName ("CoreLink");
		}

		public static IEnumerable<TestCaseData> CppCLITests ()
		{
			return NUnitCasesBySuiteName ("CppCLI");
		}

		public static IEnumerable<TestCaseData> DataFlowTests ()
		{
			return NUnitCasesBySuiteName ("DataFlow");
		}

		public static IEnumerable<TestCaseData> DynamicDependenciesTests ()
		{
			return NUnitCasesBySuiteName ("DynamicDependencies");
		}

		public static IEnumerable<TestCaseData> ExtensibilityTests ()
		{
			return NUnitCasesBySuiteName ("Extensibility");
		}

		public static IEnumerable<TestCaseData> FeatureSettingsTests ()
		{
			return NUnitCasesBySuiteName ("FeatureSettings");
		}

		public static IEnumerable<TestCaseData> FunctionPointersTests ()
		{
			return NUnitCasesBySuiteName ("FunctionPointers");
		}

		public static IEnumerable<TestCaseData> GenericsTests ()
		{
			return NUnitCasesBySuiteName ("Generics");
		}

		public static IEnumerable<TestCaseData> InheritanceAbstractClassTests ()
		{
			return NUnitCasesBySuiteName ("Inheritance.AbstractClasses");
		}

		public static IEnumerable<TestCaseData> InheritanceComplexTests ()
		{
			return NUnitCasesBySuiteName ("Inheritance.Complex");
		}

		public static IEnumerable<TestCaseData> InheritanceInterfaceTests ()
		{
			return NUnitCasesBySuiteName ("Inheritance.Interfaces");
		}

		public static IEnumerable<TestCaseData> InheritanceVirtualMethodsTests ()
		{
			return NUnitCasesBySuiteName ("Inheritance.VirtualMethods");
		}

		public static IEnumerable<TestCaseData> InteropTests ()
		{
			return NUnitCasesBySuiteName ("Interop");
		}

		public static IEnumerable<TestCaseData> LibrariesTests ()
		{
			return NUnitCasesBySuiteName ("Libraries");
		}

		public static IEnumerable<TestCaseData> LinkAttributesTests ()
		{
			return NUnitCasesBySuiteName ("LinkAttributes");
		}

		public static IEnumerable<TestCaseData> LoggingTests ()
		{
			return NUnitCasesBySuiteName ("Logging");
		}

		public static IEnumerable<TestCaseData> PreserveDependenciesTests ()
		{
			return NUnitCasesBySuiteName ("PreserveDependencies");
		}

		public static IEnumerable<TestCaseData> ReferencesTests ()
		{
			return NUnitCasesBySuiteName ("References");
		}

		public static IEnumerable<TestCaseData> ReflectionTests ()
		{
			return NUnitCasesBySuiteName ("Reflection");
		}

		public static IEnumerable<TestCaseData> RequiresCapabilityTests ()
		{
			return NUnitCasesBySuiteName ("RequiresCapability");
		}

		public static IEnumerable<TestCaseData> ResourcesTests ()
		{
			return NUnitCasesBySuiteName ("Resources");
		}

		public static IEnumerable<TestCaseData> SealerTests ()
		{
			return NUnitCasesBySuiteName ("Sealer");
		}

		public static IEnumerable<TestCaseData> SerializationTests ()
		{
			return NUnitCasesBySuiteName ("Serialization");
		}

		public static IEnumerable<TestCaseData> SingleFileTests ()
		{
			return NUnitCasesBySuiteName ("SingleFile");
		}

		public static IEnumerable<TestCaseData> StaticsTests ()
		{
			return NUnitCasesBySuiteName ("Statics");
		}

		public static IEnumerable<TestCaseData> SubstitutionsTests ()
		{
			return NUnitCasesBySuiteName ("Substitutions");
		}

		public static IEnumerable<TestCaseData> SymbolsTests ()
		{
			return NUnitCasesBySuiteName ("Symbols");
		}

		public static IEnumerable<TestCaseData> TestFrameworkTests ()
		{
			return NUnitCasesBySuiteName ("TestFramework");
		}

		public static IEnumerable<TestCaseData> TracingTests ()
		{
			return NUnitCasesBySuiteName ("Tracing");
		}

		public static IEnumerable<TestCaseData> TypeForwardingTests ()
		{
			return NUnitCasesBySuiteName ("TypeForwarding");
		}

		public static IEnumerable<TestCaseData> UnreachableBlockTests ()
		{
			return NUnitCasesBySuiteName ("UnreachableBlock");
		}

		public static IEnumerable<TestCaseData> UnreachableBodyTests ()
		{
			return NUnitCasesBySuiteName ("UnreachableBody");
		}

		public static IEnumerable<TestCaseData> WarningsTests ()
		{
			return NUnitCasesBySuiteName ("Warnings");
		}

		public static IEnumerable<TestCaseData> XmlTests ()
		{
			return NUnitCasesBySuiteName ("LinkXml");
		}

		public static IEnumerable<TestCaseData> LinqExpressionsTests ()
		{
			return NUnitCasesBySuiteName ("LinqExpressions");
		}

		public static IEnumerable<TestCaseData> MetadataTests ()
		{
			return NUnitCasesBySuiteName ("Metadata");
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

		static IEnumerable<TestCase> AllCases ()
		{
			_cachedAllCases ??= CreateCollector ()
					.Collect ()
					.OrderBy (c => c.DisplayName)
					.ToArray ();

			return _cachedAllCases;
		}

		static IEnumerable<TestCaseData> NUnitCasesBySuiteName (string suiteName)
		{
			return AllCases ()
				.Where (c => c.TestSuiteDirectory.FileName == suiteName)
				.Select (c => CreateNUnitTestCase (c, c.DisplayName))
				.OrderBy (c => c.TestName);
		}

		static TestCaseData CreateNUnitTestCase (TestCase testCase, string displayName)
		{
			var data = new TestCaseData (testCase);
			data.SetName (displayName);
			return data;
		}

		static void GetDirectoryPaths (out string rootSourceDirectory, out string testCaseAssemblyPath)
		{
			rootSourceDirectory = Path.GetFullPath (Path.Combine (PathUtilities.GetTestsSourceRootDirectory (), "Mono.Linker.Tests.Cases"));
			testCaseAssemblyPath = PathUtilities.GetTestAssemblyPath ("Mono.Linker.Tests.Cases");
		}
	}
}
