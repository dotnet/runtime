// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mono.Linker.Tests.Extensions;
using Mono.Linker.Tests.TestCasesRunner;

namespace Mono.Linker.Tests.TestCases
{
    public static class TestDatabase
    {
        private static TestCase[] _cachedAllCases;

        public static IEnumerable<TestDataRow<TestCase>> AdvancedTests()
        {
            return MSTestCasesBySuiteName("Advanced");
        }

        public static IEnumerable<TestDataRow<TestCase>> AttributeDebuggerTests()
        {
            return MSTestCasesBySuiteName("Attributes.Debugger");
        }

        public static IEnumerable<TestDataRow<TestCase>> AttributeTests()
        {
            return MSTestCasesBySuiteName("Attributes");
        }

        public static IEnumerable<TestDataRow<TestCase>> AttributesStructLayoutTests()
        {
            return MSTestCasesBySuiteName("Attributes.StructLayout");
        }

        public static IEnumerable<TestDataRow<TestCase>> BCLFeaturesTests()
        {
            return MSTestCasesBySuiteName("BCLFeatures");
        }

        public static IEnumerable<TestDataRow<TestCase>> BasicTests()
        {
            return MSTestCasesBySuiteName("Basic");
        }

        public static IEnumerable<TestDataRow<TestCase>> CodegenAnnotationTests()
        {
            return MSTestCasesBySuiteName("CodegenAnnotation");
        }

        public static IEnumerable<TestDataRow<TestCase>> CommandLineTests()
        {
            return MSTestCasesBySuiteName("CommandLine");
        }

        public static IEnumerable<TestDataRow<TestCase>> ComponentModelTests()
        {
            return MSTestCasesBySuiteName("ComponentModel");
        }

        public static IEnumerable<TestDataRow<TestCase>> CoreLinkTests()
        {
            return MSTestCasesBySuiteName("CoreLink");
        }

        public static IEnumerable<TestDataRow<TestCase>> CppCLITests()
        {
            return MSTestCasesBySuiteName("CppCLI");
        }

        public static IEnumerable<TestDataRow<TestCase>> DataFlowTests()
        {
            return MSTestCasesBySuiteName("DataFlow");
        }

        public static IEnumerable<TestDataRow<TestCase>> DynamicDependenciesTests()
        {
            return MSTestCasesBySuiteName("DynamicDependencies");
        }

        public static IEnumerable<TestDataRow<TestCase>> ExtensibilityTests()
        {
            return MSTestCasesBySuiteName("Extensibility");
        }

        public static IEnumerable<TestDataRow<TestCase>> FeatureSettingsTests()
        {
            return MSTestCasesBySuiteName("FeatureSettings");
        }

        public static IEnumerable<TestDataRow<TestCase>> FunctionPointersTests()
        {
            return MSTestCasesBySuiteName("FunctionPointers");
        }

        public static IEnumerable<TestDataRow<TestCase>> GenericsTests()
        {
            return MSTestCasesBySuiteName("Generics");
        }

        public static IEnumerable<TestDataRow<TestCase>> InheritanceAbstractClassTests()
        {
            return MSTestCasesBySuiteName("Inheritance.AbstractClasses");
        }

        public static IEnumerable<TestDataRow<TestCase>> InheritanceComplexTests()
        {
            return MSTestCasesBySuiteName("Inheritance.Complex");
        }

        public static IEnumerable<TestDataRow<TestCase>> InheritanceInterfaceTests()
        {
            return MSTestCasesBySuiteName("Inheritance.Interfaces");
        }

        public static IEnumerable<TestDataRow<TestCase>> InheritanceVirtualMethodsTests()
        {
            return MSTestCasesBySuiteName("Inheritance.VirtualMethods");
        }

        public static IEnumerable<TestDataRow<TestCase>> InlineArrayTests()
        {
            return MSTestCasesBySuiteName("InlineArrays");
        }

        public static IEnumerable<TestDataRow<TestCase>> InteropTests()
        {
            return MSTestCasesBySuiteName("Interop");
        }

        public static IEnumerable<TestDataRow<TestCase>> LibrariesTests()
        {
            return MSTestCasesBySuiteName("Libraries");
        }

        public static IEnumerable<TestDataRow<TestCase>> LinkAttributesTests()
        {
            return MSTestCasesBySuiteName("LinkAttributes");
        }

        public static IEnumerable<TestDataRow<TestCase>> LoggingTests()
        {
            return MSTestCasesBySuiteName("Logging");
        }

        public static IEnumerable<TestDataRow<TestCase>> PreserveDependenciesTests()
        {
            return MSTestCasesBySuiteName("PreserveDependencies");
        }

        public static IEnumerable<TestDataRow<TestCase>> ReferencesTests()
        {
            return MSTestCasesBySuiteName("References");
        }

        public static IEnumerable<TestDataRow<TestCase>> ReflectionTests()
        {
            return MSTestCasesBySuiteName("Reflection");
        }

        public static IEnumerable<TestDataRow<TestCase>> RequiresCapabilityTests()
        {
            return MSTestCasesBySuiteName("RequiresCapability");
        }

        public static IEnumerable<TestDataRow<TestCase>> ResourcesTests()
        {
            return MSTestCasesBySuiteName("Resources");
        }

        public static IEnumerable<TestDataRow<TestCase>> SealerTests()
        {
            return MSTestCasesBySuiteName("Sealer");
        }

        public static IEnumerable<TestDataRow<TestCase>> SerializationTests()
        {
            return MSTestCasesBySuiteName("Serialization");
        }

        public static IEnumerable<TestDataRow<TestCase>> SingleFileTests()
        {
            return MSTestCasesBySuiteName("SingleFile");
        }

        public static IEnumerable<TestDataRow<TestCase>> StaticsTests()
        {
            return MSTestCasesBySuiteName("Statics");
        }

        public static IEnumerable<TestDataRow<TestCase>> SubstitutionsTests()
        {
            return MSTestCasesBySuiteName("Substitutions");
        }

        public static IEnumerable<TestDataRow<TestCase>> SymbolsTests()
        {
            return MSTestCasesBySuiteName("Symbols");
        }

        public static IEnumerable<TestDataRow<TestCase>> TestFrameworkTests()
        {
            return MSTestCasesBySuiteName("TestFramework");
        }

        public static IEnumerable<TestDataRow<TestCase>> TopLevelStatementsTests()
        {
            return MSTestCasesBySuiteName("TopLevelStatements");
        }

        public static IEnumerable<TestDataRow<TestCase>> TracingTests()
        {
            return MSTestCasesBySuiteName("Tracing");
        }

        public static IEnumerable<TestDataRow<TestCase>> TypeForwardingTests()
        {
            return MSTestCasesBySuiteName("TypeForwarding");
        }

        public static IEnumerable<TestDataRow<TestCase>> UnreachableBlockTests()
        {
            return MSTestCasesBySuiteName("UnreachableBlock");
        }

        public static IEnumerable<TestDataRow<TestCase>> UnreachableBodyTests()
        {
            return MSTestCasesBySuiteName("UnreachableBody");
        }

        public static IEnumerable<TestDataRow<TestCase>> WarningsTests()
        {
            return MSTestCasesBySuiteName("Warnings");
        }

        public static IEnumerable<TestDataRow<TestCase>> XmlTests()
        {
            return MSTestCasesBySuiteName("LinkXml");
        }

        public static IEnumerable<TestDataRow<TestCase>> LinqExpressionsTests()
        {
            return MSTestCasesBySuiteName("LinqExpressions");
        }

        public static IEnumerable<TestDataRow<TestCase>> MetadataTests()
        {
            return MSTestCasesBySuiteName("Metadata");
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

        static IEnumerable<TestCase> AllCases()
        {
            _cachedAllCases ??= CreateCollector()
                    .Collect()
                    .OrderBy(c => c.DisplayName)
                    .ToArray();

            return _cachedAllCases;
        }

        static IEnumerable<TestDataRow<TestCase>> MSTestCasesBySuiteName(string suiteName)
        {
            return AllCases()
                .Where(c => c.TestSuiteDirectory.FileName == suiteName)
                .Select(c => CreateMSTestTestCase(c, c.DisplayName))
                .OrderBy(c => c.DisplayName);
        }

        static TestDataRow<TestCase> CreateMSTestTestCase(TestCase testCase, string displayName)
        {
            return new TestDataRow<TestCase>(testCase)
            {
                DisplayName = displayName,
            };
        }

        static void GetDirectoryPaths(out string rootSourceDirectory, out string testCaseAssemblyRoot)
        {
            rootSourceDirectory = Path.GetFullPath(Path.Combine(PathUtilities.GetTestsSourceRootDirectory(), "Mono.Linker.Tests.Cases"));
            testCaseAssemblyRoot = PathUtilities.GetTestAssemblyRoot("Mono.Linker.Tests.Cases");
        }
    }
}
