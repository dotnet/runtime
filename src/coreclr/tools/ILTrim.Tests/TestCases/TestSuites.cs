// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;
using Mono.Linker.Tests.TestCasesRunner;

namespace Mono.Linker.Tests.TestCases
{
    public class All
    {
        static readonly HashSet<string> s_expectedFailures = LoadExpectedFailures();

        static HashSet<string> LoadExpectedFailures()
        {
            var resourcePath = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "ILTrimExpectedFailures.txt");
            if (!File.Exists(resourcePath))
                return new HashSet<string>();

            return new HashSet<string>(File.ReadAllLines(resourcePath), StringComparer.Ordinal);
        }

        [ConditionalTheory]
        [MemberData(nameof(TestDatabase.BasicTests), MemberType = typeof(TestDatabase))]
        public void BasicTests(TestCase testCase) => Run(testCase);

        [ConditionalTheory]
        [MemberData(nameof(TestDatabase.AdvancedTests), MemberType = typeof(TestDatabase))]
        public void AdvancedTests(TestCase testCase) => Run(testCase);

        [ConditionalTheory]
        [MemberData(nameof(TestDatabase.AttributeDebuggerTests), MemberType = typeof(TestDatabase))]
        public void AttributesDebuggerTests(TestCase testCase) => Run(testCase);

        [ConditionalTheory]
        [MemberData(nameof(TestDatabase.AttributesStructLayoutTests), MemberType = typeof(TestDatabase))]
        public void AttributesStructLayoutTests(TestCase testCase) => Run(testCase);

        [ConditionalTheory]
        [MemberData(nameof(TestDatabase.AttributeTests), MemberType = typeof(TestDatabase))]
        public void AttributesTests(TestCase testCase) => Run(testCase);

        [ConditionalTheory]
        [MemberData(nameof(TestDatabase.BCLFeaturesTests), MemberType = typeof(TestDatabase))]
        public void BCLFeaturesTests(TestCase testCase) => Run(testCase);

        [ConditionalTheory]
        [MemberData(nameof(TestDatabase.ComponentModelTests), MemberType = typeof(TestDatabase))]
        public void ComponentModelTests(TestCase testCase) => Run(testCase);

        [ConditionalTheory]
        [MemberData(nameof(TestDatabase.CoreLinkTests), MemberType = typeof(TestDatabase))]
        public void CoreLinkTests(TestCase testCase) => Run(testCase);

        [ConditionalTheory]
        [MemberData(nameof(TestDatabase.DataFlowTests), MemberType = typeof(TestDatabase))]
        public void DataFlowTests(TestCase testCase) => Run(testCase);

        [ConditionalTheory]
        [MemberData(nameof(TestDatabase.DynamicDependenciesTests), MemberType = typeof(TestDatabase))]
        public void DynamicDependenciesTests(TestCase testCase) => Run(testCase);

        [ConditionalTheory]
        [MemberData(nameof(TestDatabase.FeatureSettingsTests), MemberType = typeof(TestDatabase))]
        public void FeatureSettingsTests(TestCase testCase) => Run(testCase);

        [ConditionalTheory]
        [MemberData(nameof(TestDatabase.FunctionPointersTests), MemberType = typeof(TestDatabase))]
        public void FunctionPointerTests(TestCase testCase) => Run(testCase);

        [ConditionalTheory]
        [MemberData(nameof(TestDatabase.GenericsTests), MemberType = typeof(TestDatabase))]
        public void GenericsTests(TestCase testCase) => Run(testCase);

        [ConditionalTheory]
        [MemberData(nameof(TestDatabase.InheritanceAbstractClassTests), MemberType = typeof(TestDatabase))]
        public void InheritanceAbstractClassTests(TestCase testCase) => Run(testCase);

        [ConditionalTheory]
        [MemberData(nameof(TestDatabase.InheritanceComplexTests), MemberType = typeof(TestDatabase))]
        public void InheritanceComplexTests(TestCase testCase) => Run(testCase);

        [ConditionalTheory]
        [MemberData(nameof(TestDatabase.InheritanceInterfaceTests), MemberType = typeof(TestDatabase))]
        public void InheritanceInterfaceTests(TestCase testCase) => Run(testCase);

        [ConditionalTheory]
        [MemberData(nameof(TestDatabase.InheritanceVirtualMethodsTests), MemberType = typeof(TestDatabase))]
        public void InheritanceVirtualMethodsTests(TestCase testCase) => Run(testCase);

        [ConditionalTheory]
        [MemberData(nameof(TestDatabase.InlineArrayTests), MemberType = typeof(TestDatabase))]
        public void InlineArrayTests(TestCase testCase) => Run(testCase);

        [ConditionalTheory]
        [MemberData(nameof(TestDatabase.InteropTests), MemberType = typeof(TestDatabase))]
        public void InteropTests(TestCase testCase) => Run(testCase);

        [ConditionalTheory]
        [MemberData(nameof(TestDatabase.LibrariesTests), MemberType = typeof(TestDatabase))]
        public void LibrariesTests(TestCase testCase) => Run(testCase);

        [ConditionalTheory]
        [MemberData(nameof(TestDatabase.LinkAttributesTests), MemberType = typeof(TestDatabase))]
        public void LinkAttributesTests(TestCase testCase) => Run(testCase);

        [ConditionalTheory]
        [MemberData(nameof(TestDatabase.LoggingTests), MemberType = typeof(TestDatabase))]
        public void LoggingTests(TestCase testCase) => Run(testCase);

        [ConditionalTheory]
        [MemberData(nameof(TestDatabase.PreserveDependenciesTests), MemberType = typeof(TestDatabase))]
        public void PreserveDependenciesTests(TestCase testCase) => Run(testCase);

        [ConditionalTheory]
        [MemberData(nameof(TestDatabase.ReferencesTests), MemberType = typeof(TestDatabase))]
        public void ReferencesTests(TestCase testCase) => Run(testCase);

        [ConditionalTheory]
        [MemberData(nameof(TestDatabase.ReflectionTests), MemberType = typeof(TestDatabase))]
        public void ReflectionTests(TestCase testCase) => Run(testCase);

        [ConditionalTheory]
        [MemberData(nameof(TestDatabase.RequiresCapabilityTests), MemberType = typeof(TestDatabase))]
        public void RequiresCapabilityTests(TestCase testCase) => Run(testCase);

        [ConditionalTheory]
        [MemberData(nameof(TestDatabase.ResourcesTests), MemberType = typeof(TestDatabase))]
        public void ResourcesTests(TestCase testCase) => Run(testCase);

        [ConditionalTheory]
        [MemberData(nameof(TestDatabase.SealerTests), MemberType = typeof(TestDatabase))]
        public void SealerTests(TestCase testCase) => Run(testCase);

        [ConditionalTheory]
        [MemberData(nameof(TestDatabase.SerializationTests), MemberType = typeof(TestDatabase))]
        public void SerializationTests(TestCase testCase) => Run(testCase);

        [ConditionalTheory]
        [MemberData(nameof(TestDatabase.SingleFileTests), MemberType = typeof(TestDatabase))]
        public void SingleFileTests(TestCase testCase) => Run(testCase);

        [ConditionalTheory]
        [MemberData(nameof(TestDatabase.StaticsTests), MemberType = typeof(TestDatabase))]
        public void StaticsTests(TestCase testCase) => Run(testCase);

        [ConditionalTheory]
        [MemberData(nameof(TestDatabase.SubstitutionsTests), MemberType = typeof(TestDatabase))]
        public void SubstitutionsTests(TestCase testCase) => Run(testCase);

        [ConditionalTheory]
        [MemberData(nameof(TestDatabase.SymbolsTests), MemberType = typeof(TestDatabase))]
        public void SymbolsTests(TestCase testCase) => Run(testCase);

        [ConditionalTheory]
        [MemberData(nameof(TestDatabase.TestFrameworkTests), MemberType = typeof(TestDatabase))]
        public void TestFrameworkTests(TestCase testCase) => Run(testCase);

        [ConditionalTheory]
        [MemberData(nameof(TestDatabase.TopLevelStatementsTests), MemberType = typeof(TestDatabase))]
        public void TopLevelStatementsTests(TestCase testCase) => Run(testCase);

        [ConditionalTheory]
        [MemberData(nameof(TestDatabase.TracingTests), MemberType = typeof(TestDatabase))]
        public void TracingTests(TestCase testCase) => Run(testCase);

        [ConditionalTheory]
        [MemberData(nameof(TestDatabase.TypeForwardingTests), MemberType = typeof(TestDatabase))]
        public void TypeForwardingTests(TestCase testCase) => Run(testCase);

        [ConditionalTheory]
        [MemberData(nameof(TestDatabase.UnreachableBlockTests), MemberType = typeof(TestDatabase))]
        public void UnreachableBlockTests(TestCase testCase) => Run(testCase);

        [ConditionalTheory]
        [MemberData(nameof(TestDatabase.UnreachableBodyTests), MemberType = typeof(TestDatabase))]
        public void UnreachableBodyTests(TestCase testCase) => Run(testCase);

        [ConditionalTheory]
        [MemberData(nameof(TestDatabase.WarningsTests), MemberType = typeof(TestDatabase))]
        public void WarningsTests(TestCase testCase) => Run(testCase);

        [ConditionalTheory]
        [MemberData(nameof(TestDatabase.XmlTests), MemberType = typeof(TestDatabase))]
        public void XmlTests(TestCase testCase) => Run(testCase);

        [ConditionalTheory]
        [MemberData(nameof(TestDatabase.LinqExpressionsTests), MemberType = typeof(TestDatabase))]
        public void LinqExpressionsTests(TestCase testCase) => Run(testCase);

        [ConditionalTheory]
        [MemberData(nameof(TestDatabase.MetadataTests), MemberType = typeof(TestDatabase))]
        public void MetadataTests(TestCase testCase) => Run(testCase);

        protected virtual void Run(TestCase testCase)
        {
            bool isExpectedFailure = s_expectedFailures.Contains(testCase.DisplayName);

            try
            {
                var runner = new TestRunner(new ObjectFactory());
                var linkedResult = runner.Run(testCase);
                new ResultChecker().Check(linkedResult);
            }
            catch (Exception ex) when (isExpectedFailure)
            {
                throw new SkipTestException($"Known ILTrim limitation: {ex.Message}");
            }

            if (isExpectedFailure)
                Assert.Fail($"Test '{testCase.DisplayName}' is in the expected failures list but now passes. Remove it from ILTrimExpectedFailures.txt.");
        }
    }
}
