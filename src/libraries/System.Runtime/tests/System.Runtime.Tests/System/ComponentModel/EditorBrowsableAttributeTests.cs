// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.ComponentModel.Tests
{
    public static class EditorBrowsableAttributeTests
    {
        [Theory]
        [InlineData(EditorBrowsableState.Advanced)]
        [InlineData(EditorBrowsableState.Always)]
        [InlineData(EditorBrowsableState.Never)]
        [InlineData((EditorBrowsableState)12345)]
        public static void Ctor_EditableBrowserState(EditorBrowsableState state)
        {
            Assert.Equal(state, new EditorBrowsableAttribute(state).State);
        }

        [Theory]
        [InlineData(EditorBrowsableState.Advanced, EditorBrowsableState.Advanced, true)]
        [InlineData(EditorBrowsableState.Always, EditorBrowsableState.Always, true)]
        [InlineData(EditorBrowsableState.Never, EditorBrowsableState.Never, true)]
        [InlineData((EditorBrowsableState)12345, (EditorBrowsableState)12345, true)]
        [InlineData(EditorBrowsableState.Advanced, EditorBrowsableState.Always, false)]
        [InlineData(EditorBrowsableState.Advanced, EditorBrowsableState.Never, false)]
        public static void EqualsTest(EditorBrowsableState state1, EditorBrowsableState state2, bool equal)
        {
            var attr1 = new EditorBrowsableAttribute(state1);
            var attr2 = new EditorBrowsableAttribute(state2);

            Assert.True(attr1.Equals(attr1));
            Assert.Equal(equal, attr1.Equals(attr2));
            Assert.Equal(equal, attr2.Equals(attr1));
            Assert.Equal(equal, attr1.GetHashCode().Equals(attr2.GetHashCode()));
        }

        [Fact]
        public static void Equals_Null()
        {
            Assert.False(new EditorBrowsableAttribute(EditorBrowsableState.Advanced).Equals(null));
        }

        [Fact]
        public static void EnumValues()
        {
            Assert.Equal(0, (int)EditorBrowsableState.Always);
            Assert.Equal(1, (int)EditorBrowsableState.Never);
            Assert.Equal(2, (int)EditorBrowsableState.Advanced);
        }

        [Fact]
        public static void DefaultCtor_EditableBrowserState()
        {
            Assert.Equal(EditorBrowsableState.Always, new EditorBrowsableAttribute().State);
        }
    }
}
