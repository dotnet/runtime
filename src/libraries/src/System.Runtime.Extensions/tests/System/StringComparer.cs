// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using Xunit;

namespace System.Tests
{
    public static class StringComparerTests
    {
        [Fact]
        public static void TestCurrent()
        {
            VerifyComparer(StringComparer.CurrentCulture, false);
            VerifyComparer(StringComparer.CurrentCultureIgnoreCase, true);
        }

        [Fact]
        public static void TestOrdinal()
        {
            VerifyComparer(StringComparer.Ordinal, false);
            VerifyComparer(StringComparer.OrdinalIgnoreCase, true);
        }

        private static void VerifyComparer(StringComparer sc, bool ignoreCase)
        {
            String s1 = "Hello";
            String s1a = "Hello";
            String s1b = "HELLO";
            String s2 = "There";

            Assert.True(sc.Equals(s1, s1a));
            Assert.True(sc.Equals(s1, s1a));

            Assert.Equal(0, sc.Compare(s1, s1a));
            Assert.Equal(0, ((IComparer)sc).Compare(s1, s1a));

            Assert.True(sc.Equals(s1, s1));
            Assert.True(((IEqualityComparer)sc).Equals(s1, s1));
            Assert.Equal(0, sc.Compare(s1, s1));
            Assert.Equal(0, ((IComparer)sc).Compare(s1, s1));

            Assert.False(sc.Equals(s1, s2));
            Assert.False(((IEqualityComparer)sc).Equals(s1, s2));
            Assert.True(sc.Compare(s1, s2) < 0);
            Assert.True(((IComparer)sc).Compare(s1, s2) < 0);

            Assert.Equal(ignoreCase, sc.Equals(s1, s1b));
            Assert.Equal(ignoreCase, ((IEqualityComparer)sc).Equals(s1, s1b));

            int result = sc.Compare(s1, s1b);
            if (ignoreCase)
                Assert.Equal(0, result);
            else
                Assert.NotEqual(0, result);

            result = ((IComparer)sc).Compare(s1, s1b);
            if (ignoreCase)
                Assert.Equal(0, result);
            else
                Assert.NotEqual(0, result);
        }
    }
}
