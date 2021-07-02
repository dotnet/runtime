// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Runtime.Tests
{
    // NOTE: DependentHandle is already heavily tested indirectly through ConditionalWeakTable<,>.
    // This class contains some specific tests for APIs that are only relevant when used directly.
    public class JitInfoTests
    {
        // TODO(josalem): disable test on iOS/Android/browser
        [Fact]
        public void JitInfoIsPopulated()
        {
            Assert.True(System.Runtime.JitInfo.GetCompilationTime() > TimeSpan.Zero);
            Assert.True(System.Runtime.JitInfo.GetCompiledILBytes() > 0);
            Assert.True(System.Runtime.JitInfo.GetCompiledMethodCount() > 0);
        }
    }
}
