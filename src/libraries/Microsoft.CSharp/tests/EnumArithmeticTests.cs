// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.CSharp.RuntimeBinder.Tests
{
    public class EnumArithmeticTests
    {
        public enum ByteEnum : byte
        {
            A = 0,
            B = 1,
            C = 2
        }

        private static readonly ByteEnum[] ByteEnumValues = {ByteEnum.A, ByteEnum.B, ByteEnum.C,};
        private static readonly byte[] ByteValues = {0, 1, 2, byte.MaxValue};

        public static IEnumerable<object[]> ByteEnumValueArguments() => ByteEnumValues.Select(i => new object[] {i});

        public static IEnumerable<object[]> ByteEnumAdditions()
            => ByteEnumValues.SelectMany(en => ByteValues, (en, ad) => new object[] {en, ad, unchecked(en + ad)});

        public static IEnumerable<object[]> CheckedByteEnumAdditions()
            => ByteEnumValues.SelectMany(
                en => ByteValues, (en, ad) => new object[] {en, ad, unchecked(en + ad), (int)en + ad > byte.MaxValue});

        public static IEnumerable<object[]> ByteEnumSubtractions()
            => ByteEnumValues.SelectMany(
                en => ByteValues, (en, ad) => new object[] {en, ad, unchecked(en - ad), unchecked(ad - en)});

        public static IEnumerable<object[]> ByteEnumSelfSubtraction()
            => ByteEnumValues.SelectMany(x => ByteEnumValues, (x, y) => new object[] {x, y, unchecked(x - y), unchecked (x - y) > (byte)x});

        public enum SByteEnum : sbyte
        {
            A = 0,
            B = 1,
            C = 2
        }

        private static readonly SByteEnum[] SByteEnumValues = {SByteEnum.A, SByteEnum.B, SByteEnum.C,};
        private static readonly sbyte[] SByteValues = {0, 1, 2, sbyte.MinValue, sbyte.MaxValue};

        public static IEnumerable<object[]> SByteEnumValueArguments() => SByteEnumValues.Select(i => new object[] { i });

        public static IEnumerable<object[]> SByteEnumAdditions()
            => SByteEnumValues.SelectMany(en => SByteValues, (en, ad) => new object[] {en, ad, unchecked(en + ad)});

        public static IEnumerable<object[]> CheckedSByteEnumAdditions()
            => SByteEnumValues.SelectMany(
                en => SByteValues, (en, ad) => new object[] {en, ad, unchecked(en + ad), (int)en + ad > sbyte.MaxValue});

        public static IEnumerable<object[]> SByteEnumSubtractions()
            => SByteEnumValues.SelectMany(
                en => SByteValues, (en, ad) => new object[] {en, ad, unchecked(en - ad), unchecked(ad - en)});

        public static IEnumerable<object[]> SByteEnumSelfSubtraction()
            => SByteEnumValues.SelectMany(x => SByteEnumValues, (x, y) => new object[] { x, y, unchecked(x - y), unchecked(x - y) > (byte)x });

        public enum Int16Enum : short
        {
            A = 0,
            B = 1,
            C = 2
        }

        private static readonly Int16Enum[] Int16EnumValues = {Int16Enum.A, Int16Enum.B, Int16Enum.C,};
        private static readonly short[] Int16Values = {0, 1, 2, short.MinValue, short.MaxValue};

        public static IEnumerable<object[]> Int16EnumValueArguments() => Int16EnumValues.Select(i => new object[] { i });

        public static IEnumerable<object[]> Int16EnumAdditions()
            => Int16EnumValues.SelectMany(en => Int16Values, (en, ad) => new object[] {en, ad, unchecked(en + ad)});

        public static IEnumerable<object[]> CheckedInt16EnumAdditions()
            => Int16EnumValues.SelectMany(
                en => Int16Values, (en, ad) => new object[] {en, ad, unchecked(en + ad), (int)en + ad > short.MaxValue});

        public static IEnumerable<object[]> Int16EnumSubtractions()
            => Int16EnumValues.SelectMany(
                en => Int16Values, (en, ad) => new object[] {en, ad, unchecked(en - ad), unchecked(ad - en)});

        public static IEnumerable<object[]> Int16EnumSelfSubtraction()
            => Int16EnumValues.SelectMany(x => Int16EnumValues, (x, y) => new object[] { x, y, unchecked(x - y), unchecked(x - y) > (byte)x });

        public enum UInt16Enum : ushort
        {
            A = 0,
            B = 1,
            C = 2
        }

        private static readonly UInt16Enum[] UInt16EnumValues = {UInt16Enum.A, UInt16Enum.B, UInt16Enum.C,};
        private static readonly ushort[] UInt16Values = {0, 1, 2, ushort.MaxValue};

        public static IEnumerable<object[]> UInt16EnumValueArguments() => UInt16EnumValues.Select(i => new object[] { i });

        public static IEnumerable<object[]> UInt16EnumAdditions()
            => UInt16EnumValues.SelectMany(en => UInt16Values, (en, ad) => new object[] {en, ad, unchecked(en + ad)});

        public static IEnumerable<object[]> CheckedUInt16EnumAdditions()
            => UInt16EnumValues.SelectMany(
                en => UInt16Values, (en, ad) => new object[] {en, ad, unchecked(en + ad), (int)en + ad > ushort.MaxValue});

        public static IEnumerable<object[]> UInt16EnumSubtractions()
            => UInt16EnumValues.SelectMany(
                en => UInt16Values, (en, ad) => new object[] {en, ad, unchecked(en - ad), unchecked(ad - en)});

        public static IEnumerable<object[]> UInt16EnumSelfSubtraction()
            => UInt16EnumValues.SelectMany(x => UInt16EnumValues, (x, y) => new object[] { x, y, unchecked(x - y), unchecked(x - y) > (byte)x });

        public enum Int32Enum
        {
            A = 0,
            B = 1,
            C = 2
        }

        private static readonly Int32Enum[] Int32EnumValues = {Int32Enum.A, Int32Enum.B, Int32Enum.C,};
        private static readonly int[] Int32Values = {0, 1, 2, int.MinValue, int.MaxValue};

        public static IEnumerable<object[]> Int32EnumValueArguments() => Int32EnumValues.Select(i => new object[] { i });

        public static IEnumerable<object[]> Int32EnumAdditions()
            => Int32EnumValues.SelectMany(en => Int32Values, (en, ad) => new object[] {en, ad, unchecked(en + ad)});

        public static IEnumerable<object[]> CheckedInt32EnumAdditions()
            => Int32EnumValues.SelectMany(
                en => Int32Values, (en, ad) => new object[] {en, ad, unchecked(en + ad), (long)en + ad > int.MaxValue});

        public static IEnumerable<object[]> Int32EnumSubtractions()
            => Int32EnumValues.SelectMany(
                en => Int32Values, (en, ad) => new object[] {en, ad, unchecked(en - ad), unchecked(ad - en)});

        public static IEnumerable<object[]> Int32EnumSelfSubtraction()
            => Int32EnumValues.SelectMany(x => Int32EnumValues, (x, y) => new object[] { x, y, unchecked(x - y), unchecked(x - y) > (byte)x });

        public enum UInt32Enum : uint
        {
            A = 0,
            B = 1,
            C = 2
        }

        private static readonly UInt32Enum[] UInt32EnumValues = {UInt32Enum.A, UInt32Enum.B, UInt32Enum.C,};
        private static readonly uint[] UInt32Values = {0, 1, 2, uint.MaxValue};

        public static IEnumerable<object[]> UInt32EnumValueArguments() => UInt32EnumValues.Select(i => new object[] { i });

        public static IEnumerable<object[]> UInt32EnumAdditions()
            => UInt32EnumValues.SelectMany(en => UInt32Values, (en, ad) => new object[] {en, ad, unchecked(en + ad)});

        public static IEnumerable<object[]> CheckedUInt32EnumAdditions()
            => UInt32EnumValues.SelectMany(
                en => UInt32Values, (en, ad) => new object[] {en, ad, unchecked(en + ad), (ulong)en + ad > uint.MaxValue});

        public static IEnumerable<object[]> UInt32EnumSubtractions()
            => UInt32EnumValues.SelectMany(
                en => UInt32Values, (en, ad) => new object[] {en, ad, unchecked(en - ad), unchecked(ad - en)});

        public static IEnumerable<object[]> UInt32EnumSelfSubtraction()
            => UInt32EnumValues.SelectMany(x => UInt32EnumValues, (x, y) => new object[] { x, y, unchecked(x - y), unchecked(x - y) > (byte)x });

        public enum Int64Enum : long
        {
            A = 0,
            B = 1,
            C = 2
        }

        private static readonly Int64Enum[] Int64EnumValues = {Int64Enum.A, Int64Enum.B, Int64Enum.C,};
        private static readonly long[] Int64Values = {0, 1, 2, long.MinValue, long.MaxValue};

        public static IEnumerable<object[]> Int64EnumValueArguments() => Int64EnumValues.Select(i => new object[] { i });

        public static IEnumerable<object[]> Int64EnumAdditions()
            => Int64EnumValues.SelectMany(en => Int64Values, (en, ad) => new object[] {en, ad, unchecked(en + ad)});

        public static IEnumerable<object[]> CheckedInt64EnumAdditions()
            => Int64EnumValues.SelectMany(
                en => Int64Values, (en, ad) => new object[] {en, ad, unchecked(en + ad), unchecked((long)en + ad < ad)});

        public static IEnumerable<object[]> Int64EnumSubtractions()
            => Int64EnumValues.SelectMany(
                en => Int64Values, (en, ad) => new object[] {en, ad, unchecked(en - ad), unchecked(ad - en)});

        public static IEnumerable<object[]> Int64EnumSelfSubtraction()
            => Int64EnumValues.SelectMany(x => Int64EnumValues, (x, y) => new object[] { x, y, unchecked(x - y), unchecked(x - y) > (byte)x });

        public enum UInt64Enum : ulong
        {
            A = 0,
            B = 1,
            C = 2
        }

        private static readonly UInt64Enum[] UInt64EnumValues = {UInt64Enum.A, UInt64Enum.B, UInt64Enum.C,};
        private static readonly ulong[] UInt64Values = {0, 1, 2, ulong.MaxValue};

        public static IEnumerable<object[]> UInt64EnumValueArguments() => UInt64EnumValues.Select(i => new object[] { i });

        public static IEnumerable<object[]> UInt64EnumAdditions()
            => UInt64EnumValues.SelectMany(en => UInt64Values, (en, ad) => new object[] {en, ad, unchecked(en + ad)});

        public static IEnumerable<object[]> CheckedUInt64EnumAdditions()
            => UInt64EnumValues.SelectMany(
                en => UInt64Values, (en, ad) => new object[] {en, ad, unchecked(en + ad), unchecked((ulong)en + ad < ad)});

        public static IEnumerable<object[]> UInt64EnumSubtractions()
            => UInt64EnumValues.SelectMany(
                en => UInt64Values, (en, ad) => new object[] {en, ad, unchecked(en - ad), unchecked(ad - en)});

        public static IEnumerable<object[]> UInt64EnumSelfSubtraction()
            => UInt64EnumValues.SelectMany(x => UInt64EnumValues, (x, y) => new object[] { x, y, unchecked(x - y), unchecked(x - y) > (byte)x });

        [Theory]
        [MemberData(nameof(ByteEnumAdditions))]
        [MemberData(nameof(SByteEnumAdditions))]
        [MemberData(nameof(Int16EnumAdditions))]
        [MemberData(nameof(UInt16EnumAdditions))]
        [MemberData(nameof(Int32EnumAdditions))]
        [MemberData(nameof(UInt32EnumAdditions))]
        [MemberData(nameof(Int64EnumAdditions))]
        [MemberData(nameof(UInt64EnumAdditions))]
        public void EnumAddition(dynamic enumVal, dynamic integralVal, object expected)
        {
            object result = unchecked (enumVal + integralVal);
            Assert.Equal(expected, result);
            Assert.IsType(enumVal.GetType(), result);
            result = unchecked(integralVal + enumVal);
            Assert.Equal(expected, result);
            Assert.IsType(enumVal.GetType(), result);
        }

        [Theory, MemberData(nameof(ByteEnumAdditions))]
        [ActiveIssue(31032, TargetFrameworkMonikers.NetFramework)]
        public void LiftedEnumAddition(ByteEnum? enumVal, byte? integralVal, ByteEnum expected)
        {
            dynamic d = enumVal;
            object result = unchecked(d + integralVal);
            Assert.Equal(expected, result);
            Assert.IsType<ByteEnum>(result);
            result = unchecked(integralVal + d);
            Assert.Equal(expected, result);
            Assert.IsType<ByteEnum>(result);
            d = integralVal;
            result = unchecked(enumVal + d);
            Assert.Equal(expected, result);
            Assert.IsType<ByteEnum>(result);
            result = unchecked(d + enumVal);
            Assert.Equal(expected, result);
            Assert.IsType<ByteEnum>(result);
        }

        [Theory, MemberData(nameof(SByteEnumAdditions))]
        [ActiveIssue(31032, TargetFrameworkMonikers.NetFramework)]
        public void LiftedEnumAddition(SByteEnum? enumVal, sbyte? integralVal, SByteEnum expected)
        {
            dynamic d = enumVal;
            object result = unchecked(d + integralVal);
            Assert.Equal(expected, result);
            Assert.IsType<SByteEnum>(result);
            result = unchecked(integralVal + d);
            Assert.Equal(expected, result);
            Assert.IsType<SByteEnum>(result);
            d = integralVal;
            result = unchecked(enumVal + d);
            Assert.Equal(expected, result);
            Assert.IsType<SByteEnum>(result);
            result = unchecked(d + enumVal);
            Assert.Equal(expected, result);
            Assert.IsType<SByteEnum>(result);
        }

        [Theory, MemberData(nameof(Int16EnumAdditions))]
        public void LiftedEnumAddition(Int16Enum? enumVal, short? integralVal, Int16Enum expected)
        {
            dynamic d = enumVal;
            object result = unchecked(d + integralVal);
            Assert.Equal(expected, result);
            Assert.IsType<Int16Enum>(result);
            result = unchecked(integralVal + d);
            Assert.Equal(expected, result);
            Assert.IsType<Int16Enum>(result);
            d = integralVal;
            result = unchecked(enumVal + d);
            Assert.Equal(expected, result);
            Assert.IsType<Int16Enum>(result);
            result = unchecked(d + enumVal);
            Assert.Equal(expected, result);
            Assert.IsType<Int16Enum>(result);
        }

        [Theory, MemberData(nameof(UInt16EnumAdditions))]
        public void LiftedEnumAddition(UInt16Enum? enumVal, ushort? integralVal, UInt16Enum expected)
        {
            dynamic d = enumVal;
            object result = unchecked(d + integralVal);
            Assert.Equal(expected, result);
            Assert.IsType<UInt16Enum>(result);
            result = unchecked(integralVal + d);
            Assert.Equal(expected, result);
            Assert.IsType<UInt16Enum>(result);
            d = integralVal;
            result = unchecked(enumVal + d);
            Assert.Equal(expected, result);
            Assert.IsType<UInt16Enum>(result);
            result = unchecked(d + enumVal);
            Assert.Equal(expected, result);
            Assert.IsType<UInt16Enum>(result);
        }

        [Theory, MemberData(nameof(Int32EnumAdditions))]
        public void LiftedEnumAddition(Int32Enum? enumVal, int? integralVal, Int32Enum expected)
        {
            dynamic d = enumVal;
            object result = unchecked(d + integralVal);
            Assert.Equal(expected, result);
            Assert.IsType<Int32Enum>(result);
            result = unchecked(integralVal + d);
            Assert.Equal(expected, result);
            Assert.IsType<Int32Enum>(result);
            d = integralVal;
            result = unchecked(enumVal + d);
            Assert.Equal(expected, result);
            Assert.IsType<Int32Enum>(result);
            result = unchecked(d + enumVal);
            Assert.Equal(expected, result);
            Assert.IsType<Int32Enum>(result);
        }

        [Theory, MemberData(nameof(UInt32EnumAdditions))]
        public void LiftedEnumAddition(UInt32Enum? enumVal, uint? integralVal, UInt32Enum expected)
        {
            dynamic d = enumVal;
            object result = unchecked(d + integralVal);
            Assert.Equal(expected, result);
            Assert.IsType<UInt32Enum>(result);
            result = unchecked(integralVal + d);
            Assert.Equal(expected, result);
            Assert.IsType<UInt32Enum>(result);
            d = integralVal;
            result = unchecked(enumVal + d);
            Assert.Equal(expected, result);
            Assert.IsType<UInt32Enum>(result);
            result = unchecked(d + enumVal);
            Assert.Equal(expected, result);
            Assert.IsType<UInt32Enum>(result);
        }

        [Theory, MemberData(nameof(Int64EnumAdditions))]
        public void LiftedEnumAddition(Int64Enum? enumVal, long? integralVal, Int64Enum expected)
        {
            dynamic d = enumVal;
            object result = unchecked(d + integralVal);
            Assert.Equal(expected, result);
            Assert.IsType<Int64Enum>(result);
            result = unchecked(integralVal + d);
            Assert.Equal(expected, result);
            Assert.IsType<Int64Enum>(result);
            d = integralVal;
            result = unchecked(enumVal + d);
            Assert.Equal(expected, result);
            Assert.IsType<Int64Enum>(result);
            result = unchecked(d + enumVal);
            Assert.Equal(expected, result);
            Assert.IsType<Int64Enum>(result);
        }

        [Theory, MemberData(nameof(UInt64EnumAdditions))]
        public void LiftedEnumAddition(UInt64Enum? enumVal, ulong? integralVal, UInt64Enum expected)
        {
            dynamic d = enumVal;
            object result = unchecked(d + integralVal);
            Assert.Equal(expected, result);
            Assert.IsType<UInt64Enum>(result);
            result = unchecked(integralVal + d);
            Assert.Equal(expected, result);
            Assert.IsType<UInt64Enum>(result);
            d = integralVal;
            result = unchecked(enumVal + d);
            Assert.Equal(expected, result);
            Assert.IsType<UInt64Enum>(result);
            result = unchecked(d + enumVal);
            Assert.Equal(expected, result);
            Assert.IsType<UInt64Enum>(result);
        }

        [Theory]
        [MemberData(nameof(CheckedByteEnumAdditions))]
        [MemberData(nameof(CheckedSByteEnumAdditions))]
        [MemberData(nameof(CheckedInt16EnumAdditions))]
        [MemberData(nameof(CheckedUInt16EnumAdditions))]
        [MemberData(nameof(CheckedInt32EnumAdditions))]
        [MemberData(nameof(CheckedUInt32EnumAdditions))]
        [MemberData(nameof(CheckedInt64EnumAdditions))]
        [MemberData(nameof(CheckedUInt64EnumAdditions))]
        public void CheckedEnumAddition(dynamic enumVal, dynamic integralVal, object expected, bool shouldThrow)
        {
            if (shouldThrow)
            {
                Assert.Throws<OverflowException>(() => checked(enumVal + integralVal));
                Assert.Throws<OverflowException>(() => checked(integralVal + enumVal));
            }
            else
            {
                object result = unchecked(enumVal + integralVal);
                Assert.Equal(expected, result);
                Assert.IsType(enumVal.GetType(), result);
                result = checked(integralVal + enumVal);
                Assert.Equal(expected, result);
                Assert.IsType(enumVal.GetType(), result);
            }
        }

        [Theory]
        [MemberData(nameof(ByteEnumSubtractions))]
        [MemberData(nameof(SByteEnumSubtractions))]
        [MemberData(nameof(Int16EnumSubtractions))]
        [MemberData(nameof(UInt16EnumSubtractions))]
        [MemberData(nameof(Int32EnumSubtractions))]
        [MemberData(nameof(UInt32EnumSubtractions))]
        [MemberData(nameof(Int64EnumSubtractions))]
        [MemberData(nameof(UInt64EnumSubtractions))]
        [ActiveIssue(31032, TargetFrameworkMonikers.NetFramework)]
        public void EnumSubtraction(dynamic enumVal, dynamic integralVal, object enMinusIn, object inMinusEn)
        {
            object result = unchecked(enumVal - integralVal);
            Assert.Equal(enMinusIn, result);
            Assert.IsType(enumVal.GetType(), result);
            result = unchecked(integralVal - enumVal);
            Assert.Equal(inMinusEn, result);
            Assert.IsType(enumVal.GetType(), result);
        }

        [Theory, MemberData(nameof(ByteEnumSubtractions))]
        [ActiveIssue(31032, TargetFrameworkMonikers.NetFramework)]
        public void LiftedEnumSubtraction(ByteEnum? enumVal, byte? integralVal, ByteEnum? enMinusIn, ByteEnum? inMinusEn)
        {
            dynamic d = enumVal;
            object result = unchecked(d - integralVal);
            Assert.Equal(enMinusIn, result);
            Assert.IsType<ByteEnum>(result);
            result = unchecked(integralVal - d);
            Assert.Equal(inMinusEn, result);
            Assert.IsType<ByteEnum>(result);
            d = integralVal;
            result = unchecked(enumVal - d);
            Assert.Equal(enMinusIn, result);
            Assert.IsType<ByteEnum>(result);
            result = unchecked(d - enumVal);
            Assert.Equal(inMinusEn, result);
            Assert.IsType<ByteEnum>(result);
        }

        [Theory, MemberData(nameof(SByteEnumSubtractions))]
        [ActiveIssue(31032, TargetFrameworkMonikers.NetFramework)]
        public void LiftedEnumSubtraction(SByteEnum? enumVal, sbyte? integralVal, SByteEnum? enMinusIn, SByteEnum? inMinusEn)
        {
            dynamic d = enumVal;
            object result = unchecked(d - integralVal);
            Assert.Equal(enMinusIn, result);
            Assert.IsType<SByteEnum>(result);
            result = unchecked(integralVal - d);
            Assert.Equal(inMinusEn, result);
            Assert.IsType<SByteEnum>(result);
            d = integralVal;
            result = unchecked(enumVal - d);
            Assert.Equal(enMinusIn, result);
            Assert.IsType<SByteEnum>(result);
            result = unchecked(d - enumVal);
            Assert.Equal(inMinusEn, result);
            Assert.IsType<SByteEnum>(result);
        }

        [Theory, MemberData(nameof(Int16EnumSubtractions))]
        public void LiftedEnumSubtraction(Int16Enum? enumVal, short? integralVal, Int16Enum? enMinusIn, Int16Enum? inMinusEn)
        {
            dynamic d = enumVal;
            object result = unchecked(d - integralVal);
            Assert.Equal(enMinusIn, result);
            Assert.IsType<Int16Enum>(result);
            result = unchecked(integralVal - d);
            Assert.Equal(inMinusEn, result);
            Assert.IsType<Int16Enum>(result);
            d = integralVal;
            result = unchecked(enumVal - d);
            Assert.Equal(enMinusIn, result);
            Assert.IsType<Int16Enum>(result);
            result = unchecked(d - enumVal);
            Assert.Equal(inMinusEn, result);
            Assert.IsType<Int16Enum>(result);
        }

        [Theory, MemberData(nameof(UInt16EnumSubtractions))]
        public void LiftedEnumSubtraction(UInt16Enum? enumVal, ushort? integralVal, UInt16Enum? enMinusIn, UInt16Enum? inMinusEn)
        {
            dynamic d = enumVal;
            object result = unchecked(d - integralVal);
            Assert.Equal(enMinusIn, result);
            Assert.IsType<UInt16Enum>(result);
            result = unchecked(integralVal - d);
            Assert.Equal(inMinusEn, result);
            Assert.IsType<UInt16Enum>(result);
            d = integralVal;
            result = unchecked(enumVal - d);
            Assert.Equal(enMinusIn, result);
            Assert.IsType<UInt16Enum>(result);
            result = unchecked(d - enumVal);
            Assert.Equal(inMinusEn, result);
            Assert.IsType<UInt16Enum>(result);
        }

        [Theory, MemberData(nameof(Int32EnumSubtractions))]
        public void LiftedEnumSubtraction(Int32Enum? enumVal, int? integralVal, Int32Enum? enMinusIn, Int32Enum? inMinusEn)
        {
            dynamic d = enumVal;
            object result = unchecked(d - integralVal);
            Assert.Equal(enMinusIn, result);
            Assert.IsType<Int32Enum>(result);
            result = unchecked(integralVal - d);
            Assert.Equal(inMinusEn, result);
            Assert.IsType<Int32Enum>(result);
            d = integralVal;
            result = unchecked(enumVal - d);
            Assert.Equal(enMinusIn, result);
            Assert.IsType<Int32Enum>(result);
            result = unchecked(d - enumVal);
            Assert.Equal(inMinusEn, result);
            Assert.IsType<Int32Enum>(result);
        }

        [Theory, MemberData(nameof(UInt32EnumSubtractions))]
        public void LiftedEnumSubtraction(UInt32Enum? enumVal, uint? integralVal, UInt32Enum? enMinusIn, UInt32Enum? inMinusEn)
        {
            dynamic d = enumVal;
            object result = unchecked(d - integralVal);
            Assert.Equal(enMinusIn, result);
            Assert.IsType<UInt32Enum>(result);
            result = unchecked(integralVal - d);
            Assert.Equal(inMinusEn, result);
            Assert.IsType<UInt32Enum>(result);
            d = integralVal;
            result = unchecked(enumVal - d);
            Assert.Equal(enMinusIn, result);
            Assert.IsType<UInt32Enum>(result);
            result = unchecked(d - enumVal);
            Assert.Equal(inMinusEn, result);
            Assert.IsType<UInt32Enum>(result);
        }

        [Theory, MemberData(nameof(Int64EnumSubtractions))]
        public void LiftedEnumSubtraction(Int64Enum? enumVal, long? integralVal, Int64Enum? enMinusIn, Int64Enum? inMinusEn)
        {
            dynamic d = enumVal;
            object result = unchecked(d - integralVal);
            Assert.Equal(enMinusIn, result);
            Assert.IsType<Int64Enum>(result);
            result = unchecked(integralVal - d);
            Assert.Equal(inMinusEn, result);
            Assert.IsType<Int64Enum>(result);
            d = integralVal;
            result = unchecked(enumVal - d);
            Assert.Equal(enMinusIn, result);
            Assert.IsType<Int64Enum>(result);
            result = unchecked(d - enumVal);
            Assert.Equal(inMinusEn, result);
            Assert.IsType<Int64Enum>(result);
        }

        [Theory, MemberData(nameof(UInt64EnumSubtractions))]
        public void LiftedEnumSubtraction(UInt64Enum? enumVal, ulong? integralVal, UInt64Enum? enMinusIn, UInt64Enum? inMinusEn)
        {
            dynamic d = enumVal;
            object result = unchecked(d - integralVal);
            Assert.Equal(enMinusIn, result);
            Assert.IsType<UInt64Enum>(result);
            result = unchecked(integralVal - d);
            Assert.Equal(inMinusEn, result);
            Assert.IsType<UInt64Enum>(result);
            d = integralVal;
            result = unchecked(enumVal - d);
            Assert.Equal(enMinusIn, result);
            Assert.IsType<UInt64Enum>(result);
            result = unchecked(d - enumVal);
            Assert.Equal(inMinusEn, result);
            Assert.IsType<UInt64Enum>(result);
        }

        [Theory, MemberData(nameof(ByteEnumSelfSubtraction))]
        [ActiveIssue(31032, TargetFrameworkMonikers.NetFramework)]
        public void EnumSubtraction(ByteEnum? x, ByteEnum? y, byte expected, bool overflows)
        {
            dynamic d = x;
            object result = unchecked(d - y);
            Assert.Equal(expected, result);
            Assert.IsType<byte>(result);
            if (overflows)
            {
                Assert.Throws<OverflowException>(() => checked(d - y));
            }
            else
            {
                result = checked(d - y);
                Assert.Equal(expected, result);
                Assert.IsType<byte>(result);
            }
        }

        [Theory, MemberData(nameof(SByteEnumSelfSubtraction))]
        [ActiveIssue(31032, TargetFrameworkMonikers.NetFramework)]
        public void EnumSubtraction(SByteEnum? x, SByteEnum? y, sbyte expected, bool overflows)
        {
            dynamic d = x;
            object result = unchecked(d - y);
            Assert.Equal(expected, result);
            Assert.IsType<sbyte>(result);
            if (overflows)
            {
                Assert.Throws<OverflowException>(() => checked(d - y));
            }
            else
            {
                result = checked(d - y);
                Assert.Equal(expected, result);
                Assert.IsType<sbyte>(result);
            }
        }

        [Theory, MemberData(nameof(Int16EnumSelfSubtraction))]
        public void EnumSubtraction(Int16Enum? x, Int16Enum? y, short expected, bool overflows)
        {
            dynamic d = x;
            object result = unchecked(d - y);
            Assert.Equal(expected, result);
            Assert.IsType<short>(result);
            if (overflows)
            {
                Assert.Throws<OverflowException>(() => checked(d - y));
            }
            else
            {
                result = checked(d - y);
                Assert.Equal(expected, result);
                Assert.IsType<short>(result);
            }
        }

        [Theory, MemberData(nameof(UInt16EnumSelfSubtraction))]
        public void EnumSubtraction(UInt16Enum? x, UInt16Enum? y, ushort expected, bool overflows)
        {
            dynamic d = x;
            object result = unchecked(d - y);
            Assert.Equal(expected, result);
            Assert.IsType<ushort>(result);
            if (overflows)
            {
                Assert.Throws<OverflowException>(() => checked(d - y));
            }
            else
            {
                result = checked(d - y);
                Assert.Equal(expected, result);
                Assert.IsType<ushort>(result);
            }
        }

        [Theory, MemberData(nameof(Int32EnumSelfSubtraction))]
        public void EnumSubtraction(Int32Enum? x, Int32Enum? y, int expected, bool overflows)
        {
            dynamic d = x;
            object result = unchecked(d - y);
            Assert.Equal(expected, result);
            Assert.IsType<int>(result);
            if (overflows)
            {
                Assert.Throws<OverflowException>(() => checked(d - y));
            }
            else
            {
                result = checked(d - y);
                Assert.Equal(expected, result);
                Assert.IsType<int>(result);
            }
        }

        [Theory, MemberData(nameof(UInt32EnumSelfSubtraction))]
        public void EnumSubtraction(UInt32Enum? x, UInt32Enum? y, uint expected, bool overflows)
        {
            dynamic d = x;
            object result = unchecked(d - y);
            Assert.Equal(expected, result);
            Assert.IsType<uint>(result);
            if (overflows)
            {
                Assert.Throws<OverflowException>(() => checked(d - y));
            }
            else
            {
                result = checked(d - y);
                Assert.Equal(expected, result);
                Assert.IsType<uint>(result);
            }
        }

        [Theory, MemberData(nameof(Int64EnumSelfSubtraction))]
        public void EnumSubtraction(Int64Enum? x, Int64Enum? y, long expected, bool overflows)
        {
            dynamic d = x;
            object result = unchecked(d - y);
            Assert.Equal(expected, result);
            Assert.IsType<long>(result);
            if (overflows)
            {
                Assert.Throws<OverflowException>(() => checked(d - y));
            }
            else
            {
                result = checked(d - y);
                Assert.Equal(expected, result);
                Assert.IsType<long>(result);
            }
        }

        [Theory, MemberData(nameof(UInt64EnumSelfSubtraction))]
        public void EnumSubtraction(UInt64Enum? x, UInt64Enum? y, ulong expected, bool overflows)
        {
            dynamic d = x;
            object result = unchecked(d - y);
            Assert.Equal(expected, result);
            Assert.IsType<ulong>(result);
            if (overflows)
            {
                Assert.Throws<OverflowException>(() => checked(d - y));
            }
            else
            {
                result = checked(d - y);
                Assert.Equal(expected, result);
                Assert.IsType<ulong>(result);
            }
        }

        [Theory]
        [MemberData(nameof(ByteEnumValueArguments))]
        [MemberData(nameof(SByteEnumValueArguments))]
        [MemberData(nameof(Int16EnumValueArguments))]
        [MemberData(nameof(UInt16EnumValueArguments))]
        [MemberData(nameof(Int32EnumValueArguments))]
        [MemberData(nameof(UInt32EnumValueArguments))]
        [MemberData(nameof(Int64EnumValueArguments))]
        [MemberData(nameof(UInt64EnumValueArguments))]
        [ActiveIssue(31032, TargetFrameworkMonikers.NetFramework)]
        public void WithLiteralNull(dynamic value)
        {
            object result = value + null;
            Assert.Null(result);
            result = null + value;
            Assert.Null(result);
            result = value - null;
            Assert.Null(result);
            result = null - value;
            Assert.Null(result);
        }

        [Theory]
        [MemberData(nameof(ByteEnumValueArguments))]
        [MemberData(nameof(SByteEnumValueArguments))]
        [MemberData(nameof(Int16EnumValueArguments))]
        [MemberData(nameof(UInt16EnumValueArguments))]
        [MemberData(nameof(Int32EnumValueArguments))]
        [MemberData(nameof(UInt32EnumValueArguments))]
        [MemberData(nameof(Int64EnumValueArguments))]
        [MemberData(nameof(UInt64EnumValueArguments))]
        public void WithNonLiteralNull(dynamic value)
        {
            object nonLiteralNull = null;
            Assert.Throws<RuntimeBinderException>(() => value + nonLiteralNull);
            Assert.Throws<RuntimeBinderException>(() => nonLiteralNull + value);
            Assert.Throws<RuntimeBinderException>(() => value - nonLiteralNull);
            Assert.Throws<RuntimeBinderException>(() => nonLiteralNull - value);
        }

        [Theory, MemberData(nameof(ByteEnumValueArguments))]
        [ActiveIssue(31032, TargetFrameworkMonikers.NetFramework)]
        public void WithTypedNullNullableByte(dynamic value)
        {
            object result = value + (byte?)null;
            Assert.Null(result);
            result = (byte?)null + value;
            Assert.Null(result);
            result = value - (byte?)null;
            Assert.Null(result);
            result = (byte?)null - value;
            Assert.Null(result);
        }

        [Theory, MemberData(nameof(SByteEnumValueArguments))]
        [ActiveIssue(31032, TargetFrameworkMonikers.NetFramework)]
        public void WithTypedNullNullableSByte(dynamic value)
        {
            object result = value + (sbyte?)null;
            Assert.Null(result);
            result = (sbyte?)null + value;
            Assert.Null(result);
            result = value - (sbyte?)null;
            Assert.Null(result);
            result = (sbyte?)null - value;
            Assert.Null(result);
        }

        [Theory, MemberData(nameof(Int16EnumValueArguments))]
        public void WithTypedNullNullableInt16(dynamic value)
        {
            object result = value + (short?)null;
            Assert.Null(result);
            result = (short?)null + value;
            Assert.Null(result);
            result = value - (short?)null;
            Assert.Null(result);
            result = (short?)null - value;
            Assert.Null(result);
        }

        [Theory, MemberData(nameof(UInt16EnumValueArguments))]
        public void WithTypedNullNullableUInt16(dynamic value)
        {
            object result = value + (ushort?)null;
            Assert.Null(result);
            result = (ushort?)null + value;
            Assert.Null(result);
            result = value - (ushort?)null;
            Assert.Null(result);
            result = (ushort?)null - value;
            Assert.Null(result);
        }

        [Theory, MemberData(nameof(Int32EnumValueArguments))]
        public void WithTypedNullNullableInt32(dynamic value)
        {
            object result = value + (int?)null;
            Assert.Null(result);
            result = (int?)null + value;
            Assert.Null(result);
            result = value - (int?)null;
            Assert.Null(result);
            result = (int?)null - value;
            Assert.Null(result);
        }

        [Theory, MemberData(nameof(UInt32EnumValueArguments))]
        public void WithTypedNullNullableUInt32(dynamic value)
        {
            object result = value + (uint?)null;
            Assert.Null(result);
            result = (uint?)null + value;
            Assert.Null(result);
            result = value - (uint?)null;
            Assert.Null(result);
            result = (uint?)null - value;
            Assert.Null(result);
        }

        [Theory, MemberData(nameof(Int64EnumValueArguments))]
        public void WithTypedNullNullableInt64(dynamic value)
        {
            object result = value + (long?)null;
            Assert.Null(result);
            result = (long?)null + value;
            Assert.Null(result);
            result = value - (long?)null;
            Assert.Null(result);
            result = (long?)null - value;
            Assert.Null(result);
        }

        [Theory, MemberData(nameof(UInt64EnumValueArguments))]
        public void WithTypedNullNullableUInt64(dynamic value)
        {
            object result = value + (ulong?)null;
            Assert.Null(result);
            result = (ulong?)null + value;
            Assert.Null(result);
            result = value - (ulong?)null;
            Assert.Null(result);
            result = (ulong?)null - value;
            Assert.Null(result);
        }
    }
}
