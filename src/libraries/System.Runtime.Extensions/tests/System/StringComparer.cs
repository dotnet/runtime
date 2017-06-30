// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Xunit;

namespace System.Tests
{
    public static partial class StringComparerTests
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

        public static IEnumerable<object[]> UpperLowerCasing_TestData()
        {
            //                          lower                upper          Culture
            yield return new object[] { "abcd",             "ABCD",         "en-US" };
            yield return new object[] { "latin i",          "LATIN I",      "en-US" };
            yield return new object[] { "turky \u0131",     "TURKY I",      "tr-TR" };
            yield return new object[] { "turky i",          "TURKY \u0130", "tr-TR" };
        }

        [Theory]
        [MemberData(nameof(UpperLowerCasing_TestData))]
        public static void CreateWithCulturesTest(string lowerForm, string upperForm, string cultureName)
        {
            CultureInfo ci = CultureInfo.GetCultureInfo(cultureName);
            StringComparer sc = StringComparer.Create(ci, false);
            Assert.False(sc.Equals(lowerForm, upperForm), "Not expected to have the lowercase equals the uppercase with ignore case is false");
            Assert.False(sc.Equals((object) lowerForm, (object) upperForm), "Not expected to have the lowercase object equals the uppercase with ignore case is false");
            Assert.NotEqual(sc.GetHashCode(lowerForm), sc.GetHashCode(upperForm));
            Assert.NotEqual(sc.GetHashCode((object) lowerForm), sc.GetHashCode((object) upperForm));

            sc = StringComparer.Create(ci, true);
            Assert.True(sc.Equals(lowerForm, upperForm), "It is expected to have the lowercase equals the uppercase with ignore case is true"); 
            Assert.True(sc.Equals((object) lowerForm, (object) upperForm), "It is expected to have the lowercase object equals the uppercase with ignore case is true"); 
            Assert.Equal(sc.GetHashCode(lowerForm), sc.GetHashCode(upperForm));
            Assert.Equal(sc.GetHashCode((object) lowerForm), sc.GetHashCode((object) upperForm));
        }

        [Fact]
        public static void InvariantTest()
        {
            Assert.True(StringComparer.InvariantCulture.Equals("test", "test"), "Same casing strings with StringComparer.InvariantCulture should be equal");
            Assert.True(StringComparer.InvariantCulture.Equals((object) "test", (object) "test"), "Same casing objects with StringComparer.InvariantCulture should be equal");
            Assert.Equal(StringComparer.InvariantCulture.GetHashCode("test"), StringComparer.InvariantCulture.GetHashCode("test"));
            Assert.Equal(0, StringComparer.InvariantCulture.Compare("test", "test"));
            
            Assert.False(StringComparer.InvariantCulture.Equals("test", "TEST"), "different casing strings with StringComparer.InvariantCulture should not be equal");
            Assert.False(StringComparer.InvariantCulture.Equals((object) "test", (object) "TEST"), "different casing objects with StringComparer.InvariantCulture should not be equal");
            Assert.NotEqual(StringComparer.InvariantCulture.GetHashCode("test"), StringComparer.InvariantCulture.GetHashCode("TEST"));
            Assert.NotEqual(0, StringComparer.InvariantCulture.Compare("test", "TEST"));

            Assert.True(StringComparer.InvariantCultureIgnoreCase.Equals("test", "test"), "Same casing strings with StringComparer.InvariantCultureIgnoreCase should be equal");
            Assert.True(StringComparer.InvariantCultureIgnoreCase.Equals((object) "test", (object) "test"), "Same casing objects with StringComparer.InvariantCultureIgnoreCase should be equal");
            Assert.Equal(0, StringComparer.InvariantCultureIgnoreCase.Compare("test", "test"));
            
            Assert.True(StringComparer.InvariantCultureIgnoreCase.Equals("test", "TEST"), "same strings with different casing with StringComparer.InvariantCultureIgnoreCase should be equal");
            Assert.True(StringComparer.InvariantCultureIgnoreCase.Equals((object) "test", (object) "TEST"), "same objects with different casing with StringComparer.InvariantCultureIgnoreCase should be equal");
            Assert.Equal(StringComparer.InvariantCultureIgnoreCase.GetHashCode("test"), StringComparer.InvariantCultureIgnoreCase.GetHashCode("TEST"));
            Assert.Equal(0, StringComparer.InvariantCultureIgnoreCase.Compare("test", "TEST"));
        }
    }
}
