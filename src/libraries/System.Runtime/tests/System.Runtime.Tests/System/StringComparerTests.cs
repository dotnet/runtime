// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Xunit;

namespace System.Tests
{
    public class StringComparerTests
    {
        [Fact]
        public void Create_InvalidArguments_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("culture", () => StringComparer.Create(null, ignoreCase: true));
        }

        [Fact]
        public void Create_CreatesValidComparer()
        {
            StringComparer c = StringComparer.Create(CultureInfo.InvariantCulture, ignoreCase: true);
            Assert.NotNull(c);
            Assert.True(c.Equals((object)"hello", (object)"HEllO"));
            Assert.True(c.Equals("hello", "HEllO"));
            Assert.False(c.Equals((object)"bello", (object)"HEllO"));
            Assert.False(c.Equals("bello", "HEllO"));

            c = StringComparer.Create(CultureInfo.InvariantCulture, ignoreCase: false);
            Assert.NotNull(c);
#pragma warning disable 0618 // suppress obsolete warning for String.Copy
            Assert.True(c.Equals((object)"hello", (object)string.Copy("hello")));
#pragma warning restore 0618 // restore warning when accessing obsolete members
            Assert.False(c.Equals((object)"hello", (object)"HEllO"));
            Assert.False(c.Equals("hello", "HEllO"));
            Assert.False(c.Equals((object)"bello", (object)"HEllO"));
            Assert.False(c.Equals("bello", "HEllO"));

            object obj = new object();
            Assert.Equal(c.GetHashCode((object)"hello"), c.GetHashCode((object)"hello"));
            Assert.Equal(c.GetHashCode("hello"), c.GetHashCode("hello"));
            Assert.Equal(c.GetHashCode("hello"), c.GetHashCode((object)"hello"));
            Assert.Equal(obj.GetHashCode(), c.GetHashCode(obj));
            Assert.Equal(42.CompareTo(84), c.Compare(42, 84));
            Assert.Throws<ArgumentException>(() => c.Compare("42", 84));
            Assert.Equal(1, c.Compare("42", null));
            Assert.Throws<ArgumentException>(() => c.Compare(42, "84"));
        }

        [Fact]
        public void Compare_InvalidArguments_Throws()
        {
            StringComparer c = StringComparer.Create(CultureInfo.InvariantCulture, ignoreCase: true);
            Assert.Throws<ArgumentException>(() => c.Compare(new object(), 42));
        }

        [Fact]
        public void GetHashCode_InvalidArguments_Throws()
        {
            StringComparer c = StringComparer.Create(CultureInfo.InvariantCulture, ignoreCase: true);
            AssertExtensions.Throws<ArgumentNullException>("obj", () => c.GetHashCode(null));
            AssertExtensions.Throws<ArgumentNullException>("obj", () => c.GetHashCode((object)null));
        }

        [Fact]
        public void Compare_ViaSort_SortsAsExpected()
        {
            string[] strings = new[] { "a", "b", "AB", "A", "cde", "abc", "f", "123", "ab" };

            Array.Sort(strings, StringComparer.OrdinalIgnoreCase);
            Assert.Equal(strings, new[] { "123", "a", "A", "AB", "ab", "abc", "b", "cde", "f" });

            Array.Sort(strings, StringComparer.Ordinal);
            Assert.Equal(strings, new[] { "123", "A", "AB", "a", "ab", "abc", "b", "cde", "f" });
        }

        [Fact]
        public void Compare_ExpectedResults()
        {
            StringComparer c = StringComparer.Ordinal;

            Assert.Equal(0, c.Compare((object)"hello", (object)"hello"));
#pragma warning disable 0618 // suppress obsolete warning for String.Copy
            Assert.Equal(0, c.Compare((object)"hello", (object)string.Copy("hello")));
#pragma warning restore 0618 // restore warning when accessing obsolete members
            Assert.Equal(-1, c.Compare(null, (object)"hello"));
            Assert.Equal(1, c.Compare((object)"hello", null));

            Assert.InRange(c.Compare((object)"hello", (object)"world"), int.MinValue, -1);
            Assert.InRange(c.Compare((object)"world", (object)"hello"), 1, int.MaxValue);
        }

        [Fact]
        public void Equals_ExpectedResults()
        {
            StringComparer c = StringComparer.Ordinal;

            Assert.True(c.Equals((object)null, (object)null));
            Assert.True(c.Equals(null, null));
            Assert.True(c.Equals((object)"hello", (object)"hello"));
            Assert.True(c.Equals("hello", "hello"));

            Assert.False(c.Equals((object)null, "hello"));
            Assert.False(c.Equals(null, "hello"));
            Assert.False(c.Equals("hello", (object)null));
            Assert.False(c.Equals("hello", null));

            Assert.True(c.Equals(42, 42));
            Assert.False(c.Equals(42, 84));
            Assert.False(c.Equals("42", 84));
            Assert.False(c.Equals(42, "84"));
        }

        [Fact]
        public void CreateCultureOptions_InvalidArguments_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => StringComparer.Create(null, CompareOptions.None));
        }

        [Fact]
        public void CreateCultureOptions_CreatesValidComparer()
        {
            StringComparer c = StringComparer.Create(CultureInfo.InvariantCulture, CompareOptions.IgnoreCase);
            Assert.NotNull(c);
            Assert.True(c.Equals((object)"hello", (object)"HEllO"));
            Assert.True(c.Equals("hello", "HEllO"));
            Assert.False(c.Equals((object)"bello", (object)"HEllO"));
            Assert.False(c.Equals("bello", "HEllO"));

            object obj = new object();
            Assert.Equal(c.GetHashCode((object)"hello"), c.GetHashCode((object)"hello"));
            Assert.Equal(c.GetHashCode("hello"), c.GetHashCode("hello"));
            Assert.Equal(c.GetHashCode("hello"), c.GetHashCode((object)"hello"));
            Assert.Equal(obj.GetHashCode(), c.GetHashCode(obj));
            Assert.Equal(42.CompareTo(84), c.Compare(42, 84));
            Assert.Throws<ArgumentException>(() => c.Compare("42", 84));
            Assert.Equal(1, c.Compare("42", null));
            Assert.Throws<ArgumentException>(() => c.Compare(42, "84"));
        }

        [Fact]
        public void IsWellKnownOrdinalComparer_TestCases()
        {
            CompareInfo ci_enUS = CompareInfo.GetCompareInfo("en-US");

            // First, instantiate and test the comparers directly

            RunTest(null, false, false);
            RunTest(EqualityComparer<string>.Default, true, false); // EC<string>.Default is Ordinal-equivalent
            RunTest(EqualityComparer<object>.Default, false, false); // EC<object>.Default isn't a string comparer
            RunTest(StringComparer.Ordinal, true, false);
            RunTest(StringComparer.OrdinalIgnoreCase, true, true);
            RunTest(StringComparer.InvariantCulture, false, false); // not ordinal
            RunTest(StringComparer.InvariantCultureIgnoreCase, false, false); // not ordinal
            RunTest(GetNonRandomizedComparer("WrappedAroundDefaultComparer"), true, false); // EC<string>.Default is Ordinal-equivalent
            RunTest(GetNonRandomizedComparer("WrappedAroundStringComparerOrdinal"), true, false);
            RunTest(GetNonRandomizedComparer("WrappedAroundStringComparerOrdinalIgnoreCase"), true, true);
            RunTest(new CustomStringComparer(), false, false); // not an inbox comparer
            RunTest(ci_enUS.GetStringComparer(CompareOptions.None), false, false); // linguistic
            RunTest(ci_enUS.GetStringComparer(CompareOptions.Ordinal), true, false);
            RunTest(ci_enUS.GetStringComparer(CompareOptions.OrdinalIgnoreCase), true, true);

            // Then, make sure that this API works with common collection types

            RunTest(new Dictionary<string, object>().Comparer, true, false);
            RunTest(new Dictionary<string, object>(StringComparer.Ordinal).Comparer, true, false);
            RunTest(new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase).Comparer, true, true);
            RunTest(new Dictionary<string, object>(StringComparer.InvariantCulture).Comparer, false, false);
            RunTest(new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase).Comparer, false, false);

            RunTest(new HashSet<string>().Comparer, true, false);
            RunTest(new HashSet<string>(StringComparer.Ordinal).Comparer, true, false);
            RunTest(new HashSet<string>(StringComparer.OrdinalIgnoreCase).Comparer, true, true);
            RunTest(new HashSet<string>(StringComparer.InvariantCulture).Comparer, false, false);
            RunTest(new HashSet<string>(StringComparer.InvariantCultureIgnoreCase).Comparer, false, false);

            static void RunTest(IEqualityComparer<string> comparer, bool expectedIsOrdinal, bool expectedIgnoreCase)
            {
                Assert.Equal(expectedIsOrdinal, StringComparer.IsWellKnownOrdinalComparer(comparer, out bool actualIgnoreCase));
                Assert.Equal(expectedIgnoreCase, actualIgnoreCase);
            }
        }

        [Fact]
        public void IsWellKnownCultureAwareComparer_TestCases()
        {
            CompareInfo ci_enUS = CompareInfo.GetCompareInfo("en-US");
            CompareInfo ci_inv = CultureInfo.InvariantCulture.CompareInfo;

            // First, instantiate and test the comparers directly

            RunTest(null, null, default);
            RunTest(EqualityComparer<string>.Default, null, default); // EC<string>.Default is not culture-aware
            RunTest(EqualityComparer<object>.Default, null, default); // EC<object>.Default isn't a string comparer
            RunTest(StringComparer.Ordinal, null, default);
            RunTest(StringComparer.OrdinalIgnoreCase, null, default);
            RunTest(StringComparer.InvariantCulture, ci_inv, CompareOptions.None);
            RunTest(StringComparer.InvariantCultureIgnoreCase, ci_inv, CompareOptions.IgnoreCase);
            RunTest(GetNonRandomizedComparer("WrappedAroundDefaultComparer"), null, default); // EC<string>.Default is Ordinal-equivalent
            RunTest(GetNonRandomizedComparer("WrappedAroundStringComparerOrdinal"), null, default);
            RunTest(GetNonRandomizedComparer("WrappedAroundStringComparerOrdinalIgnoreCase"), null, default);
            RunTest(new CustomStringComparer(), null, default); // not an inbox comparer
            RunTest(ci_enUS.GetStringComparer(CompareOptions.None), ci_enUS, CompareOptions.None);
            RunTest(ci_enUS.GetStringComparer(CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType), ci_enUS, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType);
            RunTest(ci_enUS.GetStringComparer(CompareOptions.Ordinal), null, default); // not linguistic
            RunTest(ci_enUS.GetStringComparer(CompareOptions.OrdinalIgnoreCase), null, default); // not linguistic
            RunTest(StringComparer.Create(CultureInfo.InvariantCulture, false), ci_inv, CompareOptions.None);
            RunTest(StringComparer.Create(CultureInfo.InvariantCulture, true), ci_inv, CompareOptions.IgnoreCase);
            RunTest(StringComparer.Create(CultureInfo.InvariantCulture, CompareOptions.IgnoreSymbols), ci_inv, CompareOptions.IgnoreSymbols);

            // Then, make sure that this API works with common collection types

            RunTest(new Dictionary<string, object>().Comparer, null, default);
            RunTest(new Dictionary<string, object>(StringComparer.Ordinal).Comparer, null, default);
            RunTest(new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase).Comparer, null, default);
            RunTest(new Dictionary<string, object>(StringComparer.InvariantCulture).Comparer, ci_inv, CompareOptions.None);
            RunTest(new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase).Comparer, ci_inv, CompareOptions.IgnoreCase);

            RunTest(new HashSet<string>().Comparer, null, default);
            RunTest(new HashSet<string>(StringComparer.Ordinal).Comparer, null, default);
            RunTest(new HashSet<string>(StringComparer.OrdinalIgnoreCase).Comparer, null, default);
            RunTest(new HashSet<string>(StringComparer.InvariantCulture).Comparer, ci_inv, CompareOptions.None);
            RunTest(new HashSet<string>(StringComparer.InvariantCultureIgnoreCase).Comparer, ci_inv, CompareOptions.IgnoreCase);

            static void RunTest(IEqualityComparer<string> comparer, CompareInfo expectedCompareInfo, CompareOptions expectedCompareOptions)
            {
                bool actualReturnValue = StringComparer.IsWellKnownCultureAwareComparer(comparer, out CompareInfo actualCompareInfo, out CompareOptions actualCompareOptions);
                Assert.Equal(expectedCompareInfo != null, actualReturnValue);
                Assert.Equal(expectedCompareInfo, actualCompareInfo);
                Assert.Equal(expectedCompareOptions, actualCompareOptions);
            }
        }

        private static IEqualityComparer<string> GetNonRandomizedComparer(string name)
        {
            Type nonRandomizedComparerType = typeof(StringComparer).Assembly.GetType("System.Collections.Generic.NonRandomizedStringEqualityComparer");
            Assert.NotNull(nonRandomizedComparerType);

            FieldInfo fi = nonRandomizedComparerType.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(fi);

            return (IEqualityComparer<string>)fi.GetValue(null);
        }

        private class CustomStringComparer : StringComparer
        {
            public override int Compare(string x, string y) => throw new NotImplementedException();
            public override bool Equals(string x, string y) => throw new NotImplementedException();
            public override int GetHashCode(string obj) => throw new NotImplementedException();
        }
    }
}
