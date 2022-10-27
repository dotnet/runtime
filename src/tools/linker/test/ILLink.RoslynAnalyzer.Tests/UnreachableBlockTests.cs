// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
    public sealed partial class UnreachableBlockTests : LinkerTestBase
    {
        protected override string TestSuiteName => "UnreachableBlock";

        [Fact]
        public Task TryFilterBlocks()
        {
            return RunTest(allowMissingWarnings: true);
        }
    }
}
