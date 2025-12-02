// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Text;
using Xunit;

namespace System.Globalization.Tests
{
    public class CompareInfoTests : CompareInfoTestsBase
    {
        [Theory]
        [InlineData("")]
        [InlineData("en-US")]
        [InlineData("fr-FR")]
        [InlineData("en")]
        [InlineData("zh-Hans")]
        [InlineData("zh-Hant")]
        public void GetCompareInfo(string name)
        {
            CompareInfo compare = CompareInfo.GetCompareInfo(name);
            Assert.Equal(name, compare.Name);
        }

        [Fact]
        public void GetCompareInfo_Null_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("name", () => CompareInfo.GetCompareInfo(null));
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { CultureInfo.InvariantCulture.CompareInfo, CultureInfo.InvariantCulture.CompareInfo, true };
            yield return new object[] { CultureInfo.InvariantCulture.CompareInfo, CompareInfo.GetCompareInfo(""), true };
            yield return new object[] { new CultureInfo("en-US").CompareInfo, CompareInfo.GetCompareInfo("en-US"), true };
            yield return new object[] { new CultureInfo("en-US").CompareInfo, CompareInfo.GetCompareInfo("fr-FR"), false };
            yield return new object[] { new CultureInfo("en-US").CompareInfo, new object(), false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void EqualsTest(CompareInfo compare1, object value, bool expected)
        {
            Assert.Equal(expected, compare1.Equals(value));
            if (value is CompareInfo)
            {
                Assert.Equal(expected, compare1.GetHashCode().Equals(value.GetHashCode()));
            }
        }

        [Theory]
        [InlineData("", "CompareInfo - ")]
        [InlineData("en-US", "CompareInfo - en-US")]
        [InlineData("EN-US", "CompareInfo - en-US")]
        public void ToStringTest(string name, string expected)
        {
            Assert.Equal(expected, new CultureInfo(name).CompareInfo.ToString());
        }

        public static IEnumerable<object[]> CompareInfo_TestData()
        {
            yield return new object[] { "en-US"  , 0x0409 };
            yield return new object[] { "ar-SA"  , 0x0401 };
            yield return new object[] { "ja-JP"  , 0x0411 };
            yield return new object[] { "zh-CN"  , 0x0804 };
            yield return new object[] { "en-GB"  , 0x0809 };
            yield return new object[] { "tr-TR"  , 0x041f };
        }

        public static IEnumerable<object[]> IndexOf_TestData()
        {
            yield return new object[] { s_invariantCompare, "foo", "", 0, 0, 1 };
            yield return new object[] { s_invariantCompare, "", "", 0, 0, 0 };
            yield return new object[] { s_invariantCompare, "Hello", "l", 0,  2, -1 };
            yield return new object[] { s_invariantCompare, "Hello", "l", 3,  3, 3 };
            yield return new object[] { s_invariantCompare, "Hello", "l", 2,  2, 2 };
            yield return new object[] { s_invariantCompare, "Hello", "L", 0, -1, -1 };
            yield return new object[] { s_invariantCompare, "Hello", "h", 0, -1, -1 };
        }

        public static IEnumerable<object[]> IsSortable_TestData()
        {
            yield return new object[] { "", false };
            yield return new object[] { "abcdefg", true };
            yield return new object[] { "\uD800\uDC00", true };

            // VS test runner for xunit doesn't handle ill-formed UTF-16 strings properly.
            // We'll send this one through as an array to avoid U+FFFD substitution.

            yield return new object[] { new char[] { '\uD800', '\uD800' }, false };
        }

        [Theory]
        [MemberData(nameof(CompareInfo_TestData))]
        public static void LcidTest(string cultureName, int lcid)
        {
            var ci = CompareInfo.GetCompareInfo(lcid);
            Assert.Equal(cultureName, ci.Name);
            Assert.Equal(lcid, ci.LCID);

            Assembly assembly = typeof(string).Assembly;

            ci = CompareInfo.GetCompareInfo(lcid, assembly);
            Assert.Equal(cultureName, ci.Name);
            Assert.Equal(lcid, ci.LCID);

            ci = CompareInfo.GetCompareInfo(cultureName, assembly);
            Assert.Equal(cultureName, ci.Name);
            Assert.Equal(lcid, ci.LCID);
        }

        [Theory]
        [MemberData(nameof(IndexOf_TestData))]
        public void IndexOfTest(CompareInfo compareInfo, string source, string value, int startIndex, int indexOfExpected, int lastIndexOfExpected)
        {
            Assert.Equal(indexOfExpected, compareInfo.IndexOf(source, value, startIndex));
            if (value.Length == 1)
            {
                Assert.Equal(indexOfExpected, compareInfo.IndexOf(source, value[0], startIndex));
            }

            Assert.Equal(lastIndexOfExpected, compareInfo.LastIndexOf(source, value, startIndex));
            if (value.Length == 1)
            {
                Assert.Equal(lastIndexOfExpected, compareInfo.LastIndexOf(source, value[0], startIndex));
            }
        }

        [Theory]
        [MemberData(nameof(IsSortable_TestData))]
        public void IsSortableTest(object sourceObj, bool expected)
        {
            string source = sourceObj as string ?? new string((char[])sourceObj);
            Assert.Equal(expected, CompareInfo.IsSortable(source));

            // Now test the span version - use BoundedMemory to detect buffer overruns

            using BoundedMemory<char> sourceBoundedMemory = BoundedMemory.AllocateFromExistingData<char>(source);
            sourceBoundedMemory.MakeReadonly();
            Assert.Equal(expected, CompareInfo.IsSortable(sourceBoundedMemory.Span));

            // If the string as a whole is sortable, then all chars which aren't standalone
            // surrogate halves must also be sortable.

            foreach (char c in source)
                Assert.Equal(expected && !char.IsSurrogate(c), CompareInfo.IsSortable(c));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotHybridGlobalization))]
        public void VersionTest()
        {
            SortVersion sv1 = CultureInfo.GetCultureInfo("en-US").CompareInfo.Version;
            SortVersion sv2 = CultureInfo.GetCultureInfo("ja-JP").CompareInfo.Version;
            SortVersion sv3 = CultureInfo.GetCultureInfo("en").CompareInfo.Version;

            Assert.Equal(sv1.FullVersion, sv3.FullVersion);
            Assert.NotEqual(sv1.SortId, sv2.SortId);
        }
    }
}
