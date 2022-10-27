// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests.Interop
{
    public sealed class PInvokeTests : LinkerTestBase
    {
        protected override string TestSuiteName => "Interop/PInvoke";


        [Fact]
        public Task UnusedDefaultConstructorIsRemoved()
        {
            return RunTest();
        }

        [Fact]
        public Task UnusedFieldsOfTypesPassedByRefAreNotRemoved()
        {
            return RunTest();
        }

        [Fact]
        public Task DefaultConstructorOfReturnTypeIsNotRemoved()
        {
            return RunTest();
        }

        [Fact]
        public Task UnusedDefaultConstructorOfTypePassedByRefIsNotRemoved()
        {
            return RunTest();
        }

        [Fact]
        public Task UnusedFieldsOfTypesAreNotRemoved()
        {
            return RunTest();
        }

        [Fact]
        public Task UnusedPInvoke()
        {
            return RunTest();
        }
    }
}
