// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace System.Net.Primitives.Functional.Tests
{
    public static partial class CookieCollectionTest
    {
        [Fact]
        public static void Clear_Success()
        {
            ICollection<Cookie> cc = CreateCookieCollection1();
            Assert.InRange(cc.Count, 1, int.MaxValue);
            cc.Clear();
            Assert.Equal(0, cc.Count);
        }

        [Fact]
        public static void Contains_Success()
        {
            ICollection<Cookie> cc = new CookieCollection();
            cc.Add(c1);
            Assert.True(cc.Contains(c1));
            Assert.False(cc.Contains(c2));
        }

        [Fact]
        public static void Remove_Success()
        {
            ICollection<Cookie> cc = CreateCookieCollection1();
            Assert.Equal(5, cc.Count);
            Assert.True(cc.Remove(c1));
            Assert.False(cc.Contains(c1));
            Assert.Equal(4, cc.Count);
        }

        [Fact]
        public static void Remove_NonExistantCookie_ReturnsFalse()
        {
            ICollection<Cookie> cc = CreateCookieCollection1();
            Assert.Equal(5, cc.Count);

            cc.Remove(c1);
            cc.Remove(c2);
            
            Assert.Equal(3, cc.Count);
            
            Assert.False(cc.Remove(c1));
            Assert.False(cc.Remove(c2));
            
            Assert.Equal(3, cc.Count);
        }
    }
}
