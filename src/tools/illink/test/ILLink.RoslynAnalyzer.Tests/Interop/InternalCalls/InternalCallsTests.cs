// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests.Interop
{
    public sealed class InternalCallsTests : LinkerTestBase
    {
        protected override string TestSuiteName => "Interop/InternalCalls";

        [Fact]
        public Task NoSpecialMarking ()
        {
            return RunTest ();
        }
    }
}