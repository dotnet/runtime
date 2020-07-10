// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Tests;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Collections.Tests
{
    public class ComparerTests
    {
        [Theory]
        [InlineData("b", "a", 1)]
        [InlineData("b", "b", 0)]
        [InlineData("a", "b", -1)]
        [InlineData(1, 0, 1)]
        [InlineData(1, 1, 0)]
        [InlineData(1, 2, -1)]
        [InlineData(1, null, 1)]
        [InlineData(null, null, 0)]
        [InlineData(null, 1, -1)]
        public void Ctor_CultureInfo(object a, object b, int expected)
        {
            var culture = new CultureInfo("en-US");
            var comparer = new Comparer(culture);

            Assert.Equal(expected, Math.Sign(comparer.Compare(a, b)));
        }

        [Fact]
        public void Ctor_CultureInfo_NullCulture_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("culture", () => new Comparer(null)); // Culture is null
        }

        [Fact]
        public void DefaultInvariant_Compare()
        {
            var cultureNames = Helpers.TestCultureNames;

            var string1 = new string[] { "Apple", "abc", };
            var string2 = new string[] { "\u00C6ble", "ABC" };

            foreach (string cultureName in cultureNames)
            {
                CultureInfo culture;
                try
                {
                    culture = new CultureInfo(cultureName);
                }
                catch (CultureNotFoundException)
                {
                    continue;
                }

                // All cultures should sort the same way, irrespective of the thread's culture
                using (new ThreadCultureChange(culture, culture))
                {
                    Comparer comp = Comparer.DefaultInvariant;
                    /* Comparing in invariant mode compars firstChar - secondChar (A(65) - \u00C6(198) */
                    Assert.Equal(PlatformDetection.IsInvariantGlobalization ? -1 : 1, Math.Sign(comp.Compare(string1[0], string2[0])));
                    Assert.Equal(PlatformDetection.IsInvariantGlobalization ? 1 : -1, Math.Sign(comp.Compare(string1[1], string2[1])));
                }
            }
        }

        [Fact]
        public void DefaultInvariant_Compare_Invalid()
        {
            Comparer comp = Comparer.Default;
            AssertExtensions.Throws<ArgumentException>(null, () => comp.Compare(new object(), 1)); // One object doesn't implement IComparable
            AssertExtensions.Throws<ArgumentException>(null, () => comp.Compare(1, new object())); // One object doesn't implement IComparable
            AssertExtensions.Throws<ArgumentException>(null, () => comp.Compare(new object(), new object())); // Both objects don't implement IComparable

            AssertExtensions.Throws<ArgumentException>(null, () => comp.Compare(1, 1L)); // Different types
        }

        public static IEnumerable<object[]> CompareTestData()
        {
            yield return new object[] { "hello", "hello", 0 };
            yield return new object[] { "HELLO", "HELLO", 0 };
            yield return new object[] { "hello", "HELLO", PlatformDetection.IsInvariantGlobalization ? 1 : -1 };
            yield return new object[] { "hello", "goodbye", 1 };
            yield return new object[] { 1, 2, -1 };
            yield return new object[] { 2, 1, 1 };
            yield return new object[] { 1, 1, 0 };
            yield return new object[] { 1, null, 1 };
            yield return new object[] { null, 1, -1 };
            yield return new object[] { null, null, 0 };

            yield return new object[] { new Foo(5), new Bar(5), 0 };
            yield return new object[] { new Bar(5), new Foo(5), 0 };

            yield return new object[] { new Foo(1), new Bar(2), -1 };
            yield return new object[] { new Bar(2), new Foo(1), 1 };
        }

        [Theory]
        [MemberData(nameof(CompareTestData))]
        public void Default_Compare(object a, object b, int expected)
        {
            Assert.Equal(expected, Math.Sign(Comparer.Default.Compare(a, b)));
        }

        [Fact]
        public void Default_Compare_Invalid()
        {
            Comparer comp = Comparer.Default;
            AssertExtensions.Throws<ArgumentException>(null, () => comp.Compare(new object(), 1)); // One object doesn't implement IComparable
            AssertExtensions.Throws<ArgumentException>(null, () => comp.Compare(1, new object())); // One object doesn't implement IComparable
            AssertExtensions.Throws<ArgumentException>(null, () => comp.Compare(new object(), new object())); // Both objects don't implement IComparable

            AssertExtensions.Throws<ArgumentException>(null, () => comp.Compare(1, 1L)); // Different types
        }

        private class Foo : IComparable
        {
            public int IntValue;

            public Foo(int intValue)
            {
                IntValue = intValue;
            }

            public int CompareTo(object o)
            {
                if (o is Foo)
                {
                    return IntValue.CompareTo(((Foo)o).IntValue);
                }
                else if (o is Bar)
                {
                    return IntValue.CompareTo(((Bar)o).IntValue);
                }

                throw new ArgumentException("Object is not a Foo or a Bar");
            }
        }

        private class Bar
        {
            public int IntValue;

            public Bar(int intValue)
            {
                IntValue = intValue;
            }
        }
    }
}
