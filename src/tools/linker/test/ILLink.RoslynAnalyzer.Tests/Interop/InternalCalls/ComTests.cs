// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests.Interop.InternalCalls
{
    public sealed class ComTests : LinkerTestBase
    {
        protected override string TestSuiteName => "Interop/InternalCalls/Com";

        [Fact]
        public Task DefaultConstructorOfParameterIsRemoved()
        {
            return RunTest();
        }

        [Fact]
        public Task DefaultConstructorOfReturnTypeIsRemoved()
        {
            return RunTest();
        }

        [Fact]
        public Task FieldsOfParameterAreRemoved()
        {
            return RunTest();
        }

        [Fact]
        public Task FieldsOfReturnTypeAreRemoved()
        {
            return RunTest();
        }

        [Fact]
        public Task FieldsOfThisAreRemoved()
        {
            return RunTest();
        }
    }
}
