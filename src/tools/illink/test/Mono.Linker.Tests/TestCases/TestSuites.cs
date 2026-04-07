// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mono.Linker.Tests.TestCasesRunner;

namespace Mono.Linker.Tests.TestCases
{
    [TestClass]
    public class All
    {
        [TestMethod]
        [DynamicData(nameof(TestDatabase.AdvancedTests), typeof(TestDatabase))]
        public void AdvancedTests(TestCase testCase)
        {
            Run(testCase);
        }

        [TestMethod]
        [DynamicData(nameof(TestDatabase.AttributeDebuggerTests), typeof(TestDatabase))]
        public void AttributesDebuggerTests(TestCase testCase)
        {
            Run(testCase);
        }

        [TestMethod]
        [DynamicData(nameof(TestDatabase.AttributesStructLayoutTests), typeof(TestDatabase))]
        public void AttributesStructLayoutTests(TestCase testCase)
        {
            Run(testCase);
        }

        [TestMethod]
        [DynamicData(nameof(TestDatabase.AttributeTests), typeof(TestDatabase))]
        public void AttributesTests(TestCase testCase)
        {
            Run(testCase);
        }

        [TestMethod]
        [DynamicData(nameof(TestDatabase.BCLFeaturesTests), typeof(TestDatabase))]
        public void BCLFeaturesTests(TestCase testCase)
        {
            Run(testCase);
        }

        [TestMethod]
        [DynamicData(nameof(TestDatabase.BasicTests), typeof(TestDatabase))]
        public void BasicTests(TestCase testCase)
        {
            Run(testCase);
        }

        [TestMethod]
        [DynamicData(nameof(TestDatabase.CodegenAnnotationTests), typeof(TestDatabase))]
        public void CodegenAnnotationTests(TestCase testCase)
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                Assert.Inconclusive("These tests are not valid when trimming .NET Framework");

#if NET
            Assert.Inconclusive("These tests are not valid when trimming .NET Core");
#endif
            Run(testCase);
        }

        [TestMethod]
        [DynamicData(nameof(TestDatabase.CommandLineTests), typeof(TestDatabase))]
        public void CommandLineTests(TestCase testCase)
        {
            Run(testCase);
        }

        [TestMethod]
        [DynamicData(nameof(TestDatabase.ComponentModelTests), typeof(TestDatabase))]
        public void ComponentModelTests(TestCase testCase)
        {
            Run(testCase);
        }

        [TestMethod]
        [DynamicData(nameof(TestDatabase.CoreLinkTests), typeof(TestDatabase))]
        public void CoreLinkTests(TestCase testCase)
        {
            Run(testCase);
        }

        [TestMethod]
        [DynamicData(nameof(TestDatabase.CppCLITests), typeof(TestDatabase))]
        public void CppCLITests(TestCase testCase)
        {
            Run(testCase);
        }

        [TestMethod]
        [DynamicData(nameof(TestDatabase.DataFlowTests), typeof(TestDatabase))]
        public void DataFlowTests(TestCase testCase)
        {
            Run(testCase);
        }

        [TestMethod]
        [DynamicData(nameof(TestDatabase.DynamicDependenciesTests), typeof(TestDatabase))]
        public void DynamicDependenciesTests(TestCase testCase)
        {
            Run(testCase);
        }

        [TestMethod]
        [DynamicData(nameof(TestDatabase.ExtensibilityTests), typeof(TestDatabase))]
        public void ExtensibilityTests(TestCase testCase)
        {
            Run(testCase);
        }

        [TestMethod]
        [DynamicData(nameof(TestDatabase.FeatureSettingsTests), typeof(TestDatabase))]
        public void FeatureSettingsTests(TestCase testCase)
        {
            Run(testCase);
        }

        [TestMethod]
        [DynamicData(nameof(TestDatabase.FunctionPointersTests), typeof(TestDatabase))]
        public void FunctionPointerTests(TestCase testCase)
        {
            Run(testCase);
        }

        [TestMethod]
        [DynamicData(nameof(TestDatabase.GenericsTests), typeof(TestDatabase))]
        public void GenericsTests(TestCase testCase)
        {
            Run(testCase);
        }

        [TestMethod]
        [DynamicData(nameof(TestDatabase.InheritanceAbstractClassTests), typeof(TestDatabase))]
        public void InheritanceAbstractClassTests(TestCase testCase)
        {
            Run(testCase);
        }

        [TestMethod]
        [DynamicData(nameof(TestDatabase.InheritanceComplexTests), typeof(TestDatabase))]
        public void InheritanceComplexTests(TestCase testCase)
        {
            Run(testCase);
        }

        [TestMethod]
        [DynamicData(nameof(TestDatabase.InheritanceInterfaceTests), typeof(TestDatabase))]
        public void InheritanceInterfaceTests(TestCase testCase)
        {
            Run(testCase);
        }

        [TestMethod]
        [DynamicData(nameof(TestDatabase.InheritanceVirtualMethodsTests), typeof(TestDatabase))]
        public void InheritanceVirtualMethodsTests(TestCase testCase)
        {
            Run(testCase);
        }

        [TestMethod]
        [DynamicData(nameof(TestDatabase.InlineArrayTests), typeof(TestDatabase))]
        public void InlineArrayTests(TestCase testCase)
        {
            Run(testCase);
        }

        [TestMethod]
        [DynamicData(nameof(TestDatabase.InteropTests), typeof(TestDatabase))]
        public void InteropTests(TestCase testCase)
        {
            Run(testCase);
        }

        [TestMethod]
        [DynamicData(nameof(TestDatabase.LibrariesTests), typeof(TestDatabase))]
        public void LibrariesTests(TestCase testCase)
        {
            Run(testCase);
        }

        [TestMethod]
        [DynamicData(nameof(TestDatabase.LinkAttributesTests), typeof(TestDatabase))]
        public void LinkAttributesTests(TestCase testCase)
        {
            Run(testCase);
        }

        [TestMethod]
        [DynamicData(nameof(TestDatabase.LoggingTests), typeof(TestDatabase))]
        public void LoggingTests(TestCase testCase)
        {
            Run(testCase);
        }

        [TestMethod]
        [DynamicData(nameof(TestDatabase.PreserveDependenciesTests), typeof(TestDatabase))]
        public void PreserveDependenciesTests(TestCase testCase)
        {
            Run(testCase);
        }

        [TestMethod]
        [DynamicData(nameof(TestDatabase.ReferencesTests), typeof(TestDatabase))]
        public void ReferencesTests(TestCase testCase)
        {
            Run(testCase);
        }

        [TestMethod]
        [DynamicData(nameof(TestDatabase.ReflectionTests), typeof(TestDatabase))]
        public void ReflectionTests(TestCase testCase)
        {
            Run(testCase);
        }

        [TestMethod]
        [DynamicData(nameof(TestDatabase.RequiresCapabilityTests), typeof(TestDatabase))]
        public void RequiresCapabilityTests(TestCase testCase)
        {
            Run(testCase);
        }

        [TestMethod]
        [DynamicData(nameof(TestDatabase.ResourcesTests), typeof(TestDatabase))]
        public void ResourcesTests(TestCase testCase)
        {
            Run(testCase);
        }

        [TestMethod]
        [DynamicData(nameof(TestDatabase.SealerTests), typeof(TestDatabase))]
        public void SealerTests(TestCase testCase)
        {
            Run(testCase);
        }

        [TestMethod]
        [DynamicData(nameof(TestDatabase.SerializationTests), typeof(TestDatabase))]
        public void SerializationTests(TestCase testCase)
        {
            Run(testCase);
        }

        [TestMethod]
        [DynamicData(nameof(TestDatabase.SingleFileTests), typeof(TestDatabase))]
        public void SingleFileTests(TestCase testCase)
        {
            Run(testCase);
        }

        [TestMethod]
        [DynamicData(nameof(TestDatabase.StaticsTests), typeof(TestDatabase))]
        public void StaticsTests(TestCase testCase)
        {
            Run(testCase);
        }

        [TestMethod]
        [DynamicData(nameof(TestDatabase.SubstitutionsTests), typeof(TestDatabase))]
        public void SubstitutionsTests(TestCase testCase)
        {
            Run(testCase);
        }

        [TestMethod]
        [DynamicData(nameof(TestDatabase.SymbolsTests), typeof(TestDatabase))]
        public void SymbolsTests(TestCase testCase)
        {
            Run(testCase);
        }

        [TestMethod]
        [DynamicData(nameof(TestDatabase.TestFrameworkTests), typeof(TestDatabase))]
        public void TestFrameworkTests(TestCase testCase)
        {
            Run(testCase);
        }

        [TestMethod]
        [DynamicData(nameof(TestDatabase.TopLevelStatementsTests), typeof(TestDatabase))]
        public void TopLevelStatementsTests(TestCase testCase)
        {
            Run(testCase);
        }

        [TestMethod]
        [DynamicData(nameof(TestDatabase.TracingTests), typeof(TestDatabase))]
        public void TracingTests(TestCase testCase)
        {
            Run(testCase);
        }

        [TestMethod]
        [DynamicData(nameof(TestDatabase.TypeForwardingTests), typeof(TestDatabase))]
        public void TypeForwardingTests(TestCase testCase)
        {
            Run(testCase);
        }

        [TestMethod]
        [DynamicData(nameof(TestDatabase.UnreachableBlockTests), typeof(TestDatabase))]
        public void UnreachableBlockTests(TestCase testCase)
        {
            Run(testCase);
        }

        [TestMethod]
        [DynamicData(nameof(TestDatabase.UnreachableBodyTests), typeof(TestDatabase))]
        public void UnreachableBodyTests(TestCase testCase)
        {
            Run(testCase);
        }

        [TestMethod]
        [DynamicData(nameof(TestDatabase.WarningsTests), typeof(TestDatabase))]
        public void WarningsTests(TestCase testCase)
        {
            Run(testCase);
        }

        [TestMethod]
        [DynamicData(nameof(TestDatabase.XmlTests), typeof(TestDatabase))]
        public void XmlTests(TestCase testCase)
        {
            Run(testCase);
        }

        [TestMethod]
        [DynamicData(nameof(TestDatabase.LinqExpressionsTests), typeof(TestDatabase))]
        public void LinqExpressionsTests(TestCase testCase)
        {
            Run(testCase);
        }

        [TestMethod]
        [DynamicData(nameof(TestDatabase.MetadataTests), typeof(TestDatabase))]
        public void MetadataTests(TestCase testCase)
        {
            Run(testCase);
        }

        protected virtual void Run(TestCase testCase)
        {
            var runner = new TestRunner(new ObjectFactory());
            var linkedResult = runner.Run(testCase);
            new ResultChecker().Check(linkedResult);
        }
    }
}
