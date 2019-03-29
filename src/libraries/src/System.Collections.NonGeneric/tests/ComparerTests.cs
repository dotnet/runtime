// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
            RemoteExecutor.Invoke(() =>
            {
                var cultureNames = new string[]
{
                    "cs-CZ","da-DK","de-DE","el-GR","en-US",
                    "es-ES","fi-FI","fr-FR","hu-HU","it-IT",
                    "ja-JP","ko-KR","nb-NO","nl-NL","pl-PL",
                    "pt-BR","pt-PT","ru-RU","sv-SE","tr-TR",
                    "zh-CN","zh-HK","zh-TW"
};

                var string1 = new string[] { "Apple", "abc", };
                var string2 = new string[] { "Æble", "ABC" };

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

                    // Set current culture
                    CultureInfo.CurrentCulture = culture;
                    CultureInfo.CurrentUICulture = culture;

                    // All cultures should sort the same way, irrespective of the thread's culture
                    Comparer comp = Comparer.DefaultInvariant;
                    Assert.Equal(1, comp.Compare(string1[0], string2[0]));
                    Assert.Equal(-1, comp.Compare(string1[1], string2[1]));
                }
                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
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
            yield return new object[] { new Foo(5), new Bar(5), 0 };
            yield return new object[] { new Bar(5), new Foo(5), 0 };

            yield return new object[] { new Foo(1), new Bar(2), -1 };
            yield return new object[] { new Bar(2), new Foo(1), 1 };
        }

        [Theory]
        [InlineData("hello", "hello", 0)]
        [InlineData("HELLO", "HELLO", 0)]
        [InlineData("hello", "HELLO", -1)]
        [InlineData("hello", "goodbye", 1)]
        [InlineData(1, 2, -1)]
        [InlineData(2, 1, 1)]
        [InlineData(1, 1, 0)]
        [InlineData(1, null, 1)]
        [InlineData(null, 1, -1)]
        [InlineData(null, null, 0)]
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
