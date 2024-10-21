// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Diagnostics.CodeAnalysis.Tests
{
    public class SetsRequiredMembersAttributeTests
    {
        [Fact]
        public void TestConstructor()
        {
            new SetsRequiredMembersAttribute();
        }
    }
}
