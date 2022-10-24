// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests.Inheritance.Interfaces
{
    public sealed partial class StaticInterfaceMethodsTests : LinkerTestBase
    {
        protected override string TestSuiteName => "Inheritance.Interfaces.StaticInterfaceMethods";

        [Fact]
        public Task BaseProvidesInterfaceMethod()
        {
            return RunTest(allowMissingWarnings: false);
        }

        [Fact]
        public Task StaticAbstractInterfaceMethods()
        {
            return RunTest(allowMissingWarnings: false);
        }

        [Fact]
        public Task StaticAbstractInterfaceMethodsLibrary()
        {
            return RunTest(allowMissingWarnings: false);
        }

        [Fact]
        public Task StaticInterfaceMethodsInPreservedScope()
        {
            return RunTest(allowMissingWarnings: false);
        }

        [Fact]
        public Task StaticVirtualInterfaceMethodsInPreservedScope()
        {
            return RunTest(allowMissingWarnings: false);
        }

        [Fact]
        public Task StaticVirtualInterfaceMethodsInPreservedScopeLibrary()
        {
            return RunTest(allowMissingWarnings: false);
        }

        [Fact]
        public Task StaticVirtualInterfaceMethodsLibrary()
        {
            return RunTest(allowMissingWarnings: false);
        }

        [Fact]
        public Task UnusedInterfacesInPreservedScope()
        {
            return RunTest(allowMissingWarnings: false);
        }

        [Fact]
        public Task UnusedStaticInterfaceMethods()
        {
            return RunTest(allowMissingWarnings: false);
        }
    }
}
