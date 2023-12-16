// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.VisualBasic.CompilerServices.Tests
{
    public class ObjectTypeTests
    {
        [Theory]
        [MemberData(nameof(AddObj_TestData))]
        public void AddObj(object x, object y, object expected)
        {
            Assert.Equal(expected, ObjectType.AddObj(x, y));
        }

        public static IEnumerable<object[]> AddObj_TestData()
        {
            yield return new object[] { null, null, 0 };
            yield return new object[] { 0, 0, 0 };
            // Add more...
        }

        [Theory]
        [MemberData(nameof(BitAndObj_TestData))]
        public void BitAndObj(object x, object y, object expected)
        {
            Assert.Equal(expected, ObjectType.BitAndObj(x, y));
        }

        public static IEnumerable<object[]> BitAndObj_TestData()
        {
            yield return new object[] { null, null, 0 };
            yield return new object[] { 0, 0, 0 };
            // Add more...
        }

        [Theory]
        [MemberData(nameof(BitOrObj_TestData))]
        public void BitOrObj(object x, object y, object expected)
        {
            Assert.Equal(expected, ObjectType.BitOrObj(x, y));
        }

        public static IEnumerable<object[]> BitOrObj_TestData()
        {
            yield return new object[] { null, null, 0 };
            yield return new object[] { 0, 0, 0 };
            // Add more...
        }

        [Theory]
        [MemberData(nameof(BitXorObj_TestData))]
        public void BitXorObj(object x, object y, object expected)
        {
            Assert.Equal(expected, ObjectType.BitXorObj(x, y));
        }

        public static IEnumerable<object[]> BitXorObj_TestData()
        {
            yield return new object[] { null, null, 0 };
            yield return new object[] { 0, 0, 0 };
            // Add more...
        }

        [Theory]
        [MemberData(nameof(DivObj_TestData))]
        public void DivObj(object x, object y, object expected)
        {
            Assert.Equal(expected, ObjectType.DivObj(x, y));
        }

        public static IEnumerable<object[]> DivObj_TestData()
        {
            yield return new object[] { null, null, double.NaN };
            yield return new object[] { 0, 0, double.NaN };
            // Add more...
        }

        [Theory]
        [MemberData(nameof(IDivObj_TestData))]
        public void IDivObj(object x, object y, object expected)
        {
            Assert.Equal(expected, ObjectType.IDivObj(x, y));
        }

        public static IEnumerable<object[]> IDivObj_TestData()
        {
            yield return new object[] { 0, 1, 0 };
            // Add more...
        }

        [Theory]
        [MemberData(nameof(IDivObj_DivideByZero_TestData))]
        public void IDivObj_DivideByZero(object x, object y)
        {
            Assert.Throws<DivideByZeroException>(() => ObjectType.IDivObj(x, y));
        }

        public static IEnumerable<object[]> IDivObj_DivideByZero_TestData()
        {
            yield return new object[] { null, null };
            yield return new object[] { 0, 0 };
            // Add more...
        }

        [Theory]
        [MemberData(nameof(ModObj_TestData))]
        public void ModObj(object x, object y, object expected)
        {
            Assert.Equal(expected, ObjectType.ModObj(x, y));
        }

        public static IEnumerable<object[]> ModObj_TestData()
        {
            yield return new object[] { 0, 1, 0 };
            // Add more...
        }

        [Theory]
        [MemberData(nameof(ModObj_DivideByZero_TestData))]
        public void ModObj_DivideByZero(object x, object y)
        {
            Assert.Throws<DivideByZeroException>(() => ObjectType.ModObj(x, y));
        }

        public static IEnumerable<object[]> ModObj_DivideByZero_TestData()
        {
            yield return new object[] { null, null };
            yield return new object[] { 0, 0 };
            // Add more...
        }

        [Theory]
        [MemberData(nameof(MulObj_TestData))]
        public void MulObj(object x, object y, object expected)
        {
            Assert.Equal(expected, ObjectType.MulObj(x, y));
        }

        public static IEnumerable<object[]> MulObj_TestData()
        {
            yield return new object[] { null, null, 0 };
            yield return new object[] { 0, 0, 0 };
            // Add more...
        }

        [Theory]
        [MemberData(nameof(NegObj_TestData))]
        public void NegObj(object obj, object expected)
        {
            Assert.Equal(expected, ObjectType.NegObj(obj));
        }

        public static IEnumerable<object[]> NegObj_TestData()
        {
            yield return new object[] { null, 0 };
            yield return new object[] { 0, 0 };
            // Add more...
        }

        [Theory]
        [MemberData(nameof(NotObj_TestData))]
        public void NotObj(object obj, object expected)
        {
            Assert.Equal(expected, ObjectType.NotObj(obj));
        }

        public static IEnumerable<object[]> NotObj_TestData()
        {
            yield return new object[] { null, -1 };
            yield return new object[] { 0, -1 };
            // Add more...
        }

        [Theory]
        [MemberData(nameof(PlusObj_TestData))]
        public void PlusObj(object obj, object expected)
        {
            Assert.Equal(expected, ObjectType.PlusObj(obj));
        }

        public static IEnumerable<object[]> PlusObj_TestData()
        {
            yield return new object[] { null, 0 };
            yield return new object[] { 0, 0 };
            // Add more...
        }

        [Theory]
        [MemberData(nameof(PowObj_TestData))]
        public void PowObj(object x, object y, object expected)
        {
            Assert.Equal(expected, ObjectType.PowObj(x, y));
        }

        public static IEnumerable<object[]> PowObj_TestData()
        {
            yield return new object[] { null, null, 1.0 };
            yield return new object[] { 0, 0, 1.0 };
            // Add more...
        }

        [Theory]
        [MemberData(nameof(ShiftLeftObj_TestData))]
        public void ShiftLeftObj(object obj, int amount, object expected)
        {
            Assert.Equal(expected, ObjectType.ShiftLeftObj(obj, amount));
        }

        public static IEnumerable<object[]> ShiftLeftObj_TestData()
        {
            yield return new object[] { null, null, 0 };
            yield return new object[] { 0, 0, 0 };
            // Add more...
        }

        [Theory]
        [MemberData(nameof(ShiftRightObj_TestData))]
        public void ShiftRightObj(object obj, int amount, object expected)
        {
            Assert.Equal(expected, ObjectType.ShiftRightObj(obj, amount));
        }

        public static IEnumerable<object[]> ShiftRightObj_TestData()
        {
            yield return new object[] { null, null, 0 };
            yield return new object[] { 0, 0, 0 };
            // Add more...
        }

        [Theory]
        [MemberData(nameof(StrCatObj_TestData))]
        public void StrCatObj(object x, object y, object expected)
        {
            Assert.Equal(expected, ObjectType.StrCatObj(x, y));
        }

        public static IEnumerable<object[]> StrCatObj_TestData()
        {
            yield return new object[] { null, null, "" };
            yield return new object[] { 0, 0, "00" };
            // Add more...
        }

        [Theory]
        [MemberData(nameof(SubObj_TestData))]
        public void SubObj(object x, object y, object expected)
        {
            Assert.Equal(expected, ObjectType.SubObj(x, y));
        }

        public static IEnumerable<object[]> SubObj_TestData()
        {
            yield return new object[] { null, null, 0 };
            yield return new object[] { 0, 0, 0 };
            // Add more...
        }

        [Theory]
        [MemberData(nameof(XorObj_TestData))]
        public void XorObj(object x, object y, object expected)
        {
            Assert.Equal(expected, ObjectType.XorObj(x, y));
        }

        public static IEnumerable<object[]> XorObj_TestData()
        {
            yield return new object[] { null, null, false };
            yield return new object[] { 0, 0, false };
            // Add more...
        }

        [Theory]
        [MemberData(nameof(GetObjectValuePrimitive_TestData))]
        public void GetObjectValuePrimitive(object obj, object expected)
        {
            Assert.Equal(expected, ObjectType.GetObjectValuePrimitive(obj));
        }

        public static IEnumerable<object[]> GetObjectValuePrimitive_TestData()
        {
            yield return new object[] { null, null };
            yield return new object[] { 0, 0 };
            // Add more...
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotHybridGlobalizationOnBrowser))]
        [MemberData(nameof(LikeObj_TestData))]
        public void LikeObj(object left, object right, object expectedBinaryCompare, object expectedTextCompare)
        {
            Assert.Equal(expectedBinaryCompare, ObjectType.LikeObj(left, right, CompareMethod.Binary));
            Assert.Equal(expectedTextCompare, ObjectType.LikeObj(left, right, CompareMethod.Text));
        }

        public static IEnumerable<object[]> LikeObj_TestData()
        {
            yield return new object[] { null, null, true, true };
            yield return new object[] { new char[0], null, true, true };
            yield return new object[] { "", null, true, true };
            yield return new object[] { "a3", new[] { 'A', '#' }, false, true };
            yield return new object[] { new[] { 'A', '3' }, "a#", false, true };
            yield return new object[] { "", "*", true, true };
            yield return new object[] { "", "?", false, false };
            yield return new object[] { "a", "?", true, true };
            yield return new object[] { "a3", "[A-Z]#", false, true };
            yield return new object[] { "A3", "[a-a]#", false, true };
        }

        [Theory]
        [MemberData(nameof(LikeObj_NullReference_TestData))]
        public void LikeObj_NullReference(object left, object right)
        {
            Assert.Throws<NullReferenceException>(() => ObjectType.LikeObj(left, right, CompareMethod.Binary));
            Assert.Throws<NullReferenceException>(() => ObjectType.LikeObj(left, right, CompareMethod.Text));
        }

        public static IEnumerable<object[]> LikeObj_NullReference_TestData()
        {
            yield return new object[] { null, new[] { '*' } };
            yield return new object[] { null, "*" };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotHybridGlobalizationOnBrowser))]
        [MemberData(nameof(ObjTst_TestData))]
        public void ObjTst(object x, object y, bool textCompare, object expected)
        {
            Assert.Equal(expected, ObjectType.ObjTst(x, y, textCompare));
        }

        public static IEnumerable<object[]> ObjTst_TestData()
        {
            yield return new object[] { null, null, 0, 0 };
            yield return new object[] { null, "", 0, 0 };
            yield return new object[] { "", null, 0, 0 };
            yield return new object[] { null, "a", -1, -1 };
            yield return new object[] { "a", null, 1, 1 };
            yield return new object[] { "", "a", -97, -1 };
            yield return new object[] { "a", "", 97, 1 };
            yield return new object[] { "a", "a", 0, 0 };
            yield return new object[] { "a", "b", -1, -1 };
            yield return new object[] { "b", "a", 1, 1 };
            yield return new object[] { "a", "ABC", 32, PlatformDetection.IsInvariantGlobalization ? -2 : -1 };
            yield return new object[] { "ABC", "a", -32, PlatformDetection.IsInvariantGlobalization ? 2 : 1 };
            yield return new object[] { "abc", "ABC", 32, 0 };
        }
    }
}
