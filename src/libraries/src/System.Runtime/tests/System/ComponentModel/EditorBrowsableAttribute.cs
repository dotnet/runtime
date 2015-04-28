// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using Xunit;

namespace System.Runtime.Tests
{
    public static class EditorBrowsableAttributeTests
    {
        [Fact]
        public static void TestCtor()
        {
            Assert.Equal(EditorBrowsableState.Advanced, new EditorBrowsableAttribute(EditorBrowsableState.Advanced).State);
            Assert.Equal(EditorBrowsableState.Always, new EditorBrowsableAttribute(EditorBrowsableState.Always).State);
            Assert.Equal(EditorBrowsableState.Never, new EditorBrowsableAttribute(EditorBrowsableState.Never).State);
            Assert.Equal((EditorBrowsableState)12345, new EditorBrowsableAttribute((EditorBrowsableState)12345).State);
        }

        [Fact]
        public static void TestEqual()
        {
            var attr = new EditorBrowsableAttribute(EditorBrowsableState.Advanced);
            Assert.Equal(attr, attr);
            Assert.True(attr.Equals(attr));
            Assert.Equal(attr.GetHashCode(), attr.GetHashCode());

            Assert.Equal(new EditorBrowsableAttribute(EditorBrowsableState.Advanced), attr);
            Assert.Equal(new EditorBrowsableAttribute(EditorBrowsableState.Advanced).GetHashCode(), attr.GetHashCode());

            Assert.NotEqual(new EditorBrowsableAttribute(EditorBrowsableState.Always), attr);
            Assert.NotEqual(new EditorBrowsableAttribute(EditorBrowsableState.Never).GetHashCode(), attr.GetHashCode());
            Assert.False(attr.Equals(null));
        }

        [Fact]
        public static void TestEnumValues()
        {
            Assert.Equal(0, (int)EditorBrowsableState.Always);
            Assert.Equal(1, (int)EditorBrowsableState.Never);
            Assert.Equal(2, (int)EditorBrowsableState.Advanced);
        }
    }
}
