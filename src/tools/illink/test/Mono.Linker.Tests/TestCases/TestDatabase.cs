// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Linker.Tests.Extensions;
using Mono.Linker.Tests.TestCasesRunner;

namespace Mono.Linker.Tests.TestCases
{
    public static class TestDatabase
    {
        private static TestCase[] _cachedAllCases;

        public static IEnumerable<object[]> AdvancedTests()
        {
            return TestCasesBySuiteName("Advanced");
        }

        public static IEnumerable<object[]> AttributeDebuggerTests()
        {
            return TestCasesBySuiteName("Attributes.Debugger");
        }

        public static IEnumerable<object[]> AttributeTests()
        {
            return TestCasesBySuiteName("Attributes");
        }

        public static IEnumerable<object[]> AttributesStructLayoutTests()
        {
            return TestCasesBySuiteName("Attributes.StructLayout");
        }

        public static IEnumerable<object[]> BCLFeaturesTests()
        {
            return TestCasesBySuiteName("BCLFeatures");
        }

        public static IEnumerable<object[]> BasicTests()
        {
            return TestCasesBySuiteName("Basic");
        }

        public static IEnumerable<object[]> CodegenAnnotationTests()
        {
            return TestCasesBySuiteName("CodegenAnnotation");
        }

        public static IEnumerable<object[]> CommandLineTests()
        {
            return TestCasesBySuiteName("CommandLine");
        }

        public static IEnumerable<object[]> ComponentModelTests()
        {
            return TestCasesBySuiteName("ComponentModel");
        }

        public static IEnumerable<object[]> CoreLinkTests()
        {
            return TestCasesBySuiteName("CoreLink");
        }

        public static IEnumerable<object[]> CppCLITests()
        {
            return TestCasesBySuiteName("CppCLI");
        }

        public static IEnumerable<object[]> DataFlowTests()
        {
            return TestCasesBySuiteName("DataFlow");
        }

        public static IEnumerable<object[]> DynamicDependenciesTests()
        {
            return TestCasesBySuiteName("DynamicDependencies");
        }

        public static IEnumerable<object[]> ExtensibilityTests()
        {
            return TestCasesBySuiteName("Extensibility");
        }

        public static IEnumerable<object[]> FeatureSettingsTests()
        {
            return TestCasesBySuiteName("FeatureSettings");
        }

        public static IEnumerable<object[]> FunctionPointersTests()
        {
            return TestCasesBySuiteName("FunctionPointers");
        }

        public static IEnumerable<object[]> GenericsTests()
        {
            return TestCasesBySuiteName("Generics");
        }

        public static IEnumerable<object[]> InheritanceAbstractClassTests()
        {
            return TestCasesBySuiteName("Inheritance.AbstractClasses");
        }

        public static IEnumerable<object[]> InheritanceComplexTests()
        {
            return TestCasesBySuiteName("Inheritance.Complex");
        }

        public static IEnumerable<object[]> InheritanceInterfaceTests()
        {
            return TestCasesBySuiteName("Inheritance.Interfaces");
        }

        public static IEnumerable<object[]> InheritanceVirtualMethodsTests()
        {
            return TestCasesBySuiteName("Inheritance.VirtualMethods");
        }

        public static IEnumerable<object[]> InlineArrayTests()
        {
            return TestCasesBySuiteName("InlineArrays");
        }

        public static IEnumerable<object[]> InteropTests()
        {
            return TestCasesBySuiteName("Interop");
        }

        public static IEnumerable<object[]> LibrariesTests()
        {
            return TestCasesBySuiteName("Libraries");
        }

        public static IEnumerable<object[]> LinkAttributesTests()
        {
            return TestCasesBySuiteName("LinkAttributes");
        }

        public static IEnumerable<object[]> LoggingTests()
        {
            return TestCasesBySuiteName("Logging");
        }

        public static IEnumerable<object[]> PreserveDependenciesTests()
        {
            return TestCasesBySuiteName("PreserveDependencies");
        }

        public static IEnumerable<object[]> ReferencesTests()
        {
            return TestCasesBySuiteName("References");
        }

        public static IEnumerable<object[]> ReflectionTests()
        {
            return TestCasesBySuiteName("Reflection");
        }

        public static IEnumerable<object[]> RequiresCapabilityTests()
        {
            return TestCasesBySuiteName("RequiresCapability");
        }

        public static IEnumerable<object[]> ResourcesTests()
        {
            return TestCasesBySuiteName("Resources");
        }

        public static IEnumerable<object[]> SealerTests()
        {
            return TestCasesBySuiteName("Sealer");
        }

        public static IEnumerable<object[]> SerializationTests()
        {
            return TestCasesBySuiteName("Serialization");
        }

        public static IEnumerable<object[]> SingleFileTests()
        {
            return TestCasesBySuiteName("SingleFile");
        }

        public static IEnumerable<object[]> StaticsTests()
        {
            return TestCasesBySuiteName("Statics");
        }

        public static IEnumerable<object[]> SubstitutionsTests()
        {
            return TestCasesBySuiteName("Substitutions");
        }

        public static IEnumerable<object[]> SymbolsTests()
        {
            return TestCasesBySuiteName("Symbols");
        }

        public static IEnumerable<object[]> TestFrameworkTests()
        {
            return TestCasesBySuiteName("TestFramework");
        }

        public static IEnumerable<object[]> TopLevelStatementsTests()
        {
            return TestCasesBySuiteName("TopLevelStatements");
        }

        public static IEnumerable<object[]> TracingTests()
        {
            return TestCasesBySuiteName("Tracing");
        }

        public static IEnumerable<object[]> TypeForwardingTests()
        {
            return TestCasesBySuiteName("TypeForwarding");
        }

        public static IEnumerable<object[]> UnreachableBlockTests()
        {
            return TestCasesBySuiteName("UnreachableBlock");
        }

        public static IEnumerable<object[]> UnreachableBodyTests()
        {
            return TestCasesBySuiteName("UnreachableBody");
        }

        public static IEnumerable<object[]> WarningsTests()
        {
            return TestCasesBySuiteName("Warnings");
        }

        public static IEnumerable<object[]> XmlTests()
        {
            return TestCasesBySuiteName("LinkXml");
        }

        public static IEnumerable<object[]> LinqExpressionsTests()
        {
            return TestCasesBySuiteName("LinqExpressions");
        }

        public static IEnumerable<object[]> MetadataTests()
        {
            return TestCasesBySuiteName("Metadata");
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

        static IEnumerable<object[]> TestCasesBySuiteName(string suiteName)
        {
            return AllCases()
                .Where(c => c.TestSuiteDirectory.FileName == suiteName)
                .Select(c => new object[] { c })
                .OrderBy(c => ((TestCase)c[0]).DisplayName);
        }

        static void GetDirectoryPaths(out string rootSourceDirectory, out string testCaseAssemblyRoot)
        {
            rootSourceDirectory = Path.GetFullPath(Path.Combine(PathUtilities.GetTestsSourceRootDirectory(), "Mono.Linker.Tests.Cases"));
            testCaseAssemblyRoot = PathUtilities.GetTestAssemblyRoot("Mono.Linker.Tests.Cases");
        }
    }
}
