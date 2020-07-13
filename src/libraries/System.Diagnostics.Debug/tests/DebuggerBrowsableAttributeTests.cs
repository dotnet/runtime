// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Diagnostics.Tests
{
    public class DebuggerBrowsableAttributeTests
    {
        [Theory]
        [InlineData(DebuggerBrowsableState.Never)]
        [InlineData(DebuggerBrowsableState.Collapsed)]
        [InlineData(DebuggerBrowsableState.RootHidden)]
        [InlineData((DebuggerBrowsableState)1)]
        public void Ctor_State(DebuggerBrowsableState state)
        {
            var attribute = new DebuggerBrowsableAttribute(state);
            Assert.Equal(state, attribute.State);
        }

        [Theory]
        [InlineData(DebuggerBrowsableState.Never - 1)]
        [InlineData(DebuggerBrowsableState.RootHidden + 1)]
        public void Ctor_InvalidState_ThrowsArgumentOutOfRangeException(DebuggerBrowsableState state)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("state", () => new DebuggerBrowsableAttribute(state));
        }
    }
}
