// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests.Interop.PInvoke
{
    public class IndividualTests : LinkerTestBase
    {
        protected override string TestSuiteName => "Interop/PInvoke/Individual";

        [Fact]
        public Task CanOutputPInvokes()
        {
            return RunTest();
        }
    }
}
