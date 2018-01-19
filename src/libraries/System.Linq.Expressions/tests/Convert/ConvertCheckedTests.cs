// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class ConvertCheckedTests
    {
        #region Test methods

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedByteToByteTest(bool useInterpreter)
        {
            foreach (byte value in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyCheckedByteToByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedByteToNullableByteTest(bool useInterpreter)
        {
            foreach (byte value in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyCheckedByteToNullableByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedByteToCharTest(bool useInterpreter)
        {
            foreach (byte value in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyCheckedByteToChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedByteToNullableCharTest(bool useInterpreter)
        {
            foreach (byte value in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyCheckedByteToNullableChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedByteToDecimalTest(bool useInterpreter)
        {
            foreach (byte value in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyCheckedByteToDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedByteToNullableDecimalTest(bool useInterpreter)
        {
            foreach (byte value in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyCheckedByteToNullableDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedByteToDoubleTest(bool useInterpreter)
        {
            foreach (byte value in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyCheckedByteToDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedByteToNullableDoubleTest(bool useInterpreter)
        {
            foreach (byte value in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyCheckedByteToNullableDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedByteToEnumTest(bool useInterpreter)
        {
            foreach (byte value in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyCheckedByteToEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedByteToNullableEnumTest(bool useInterpreter)
        {
            foreach (byte value in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyCheckedByteToNullableEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedByteToEnumLongTest(bool useInterpreter)
        {
            foreach (byte value in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyCheckedByteToEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedByteToNullableEnumLongTest(bool useInterpreter)
        {
            foreach (byte value in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyCheckedByteToNullableEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedByteToFloatTest(bool useInterpreter)
        {
            foreach (byte value in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyCheckedByteToFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedByteToNullableFloatTest(bool useInterpreter)
        {
            foreach (byte value in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyCheckedByteToNullableFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedByteToIntTest(bool useInterpreter)
        {
            foreach (byte value in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyCheckedByteToInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedByteToNullableIntTest(bool useInterpreter)
        {
            foreach (byte value in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyCheckedByteToNullableInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedByteToLongTest(bool useInterpreter)
        {
            foreach (byte value in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyCheckedByteToLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedByteToNullableLongTest(bool useInterpreter)
        {
            foreach (byte value in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyCheckedByteToNullableLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedByteToSByteTest(bool useInterpreter)
        {
            foreach (byte value in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyCheckedByteToSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedByteToNullableSByteTest(bool useInterpreter)
        {
            foreach (byte value in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyCheckedByteToNullableSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedByteToShortTest(bool useInterpreter)
        {
            foreach (byte value in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyCheckedByteToShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedByteToNullableShortTest(bool useInterpreter)
        {
            foreach (byte value in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyCheckedByteToNullableShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedByteToUIntTest(bool useInterpreter)
        {
            foreach (byte value in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyCheckedByteToUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedByteToNullableUIntTest(bool useInterpreter)
        {
            foreach (byte value in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyCheckedByteToNullableUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedByteToULongTest(bool useInterpreter)
        {
            foreach (byte value in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyCheckedByteToULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedByteToNullableULongTest(bool useInterpreter)
        {
            foreach (byte value in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyCheckedByteToNullableULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedByteToUShortTest(bool useInterpreter)
        {
            foreach (byte value in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyCheckedByteToUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedByteToNullableUShortTest(bool useInterpreter)
        {
            foreach (byte value in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyCheckedByteToNullableUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableByteToByteTest(bool useInterpreter)
        {
            foreach (byte? value in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyCheckedNullableByteToByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableByteToNullableByteTest(bool useInterpreter)
        {
            foreach (byte? value in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyCheckedNullableByteToNullableByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableByteToCharTest(bool useInterpreter)
        {
            foreach (byte? value in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyCheckedNullableByteToChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableByteToNullableCharTest(bool useInterpreter)
        {
            foreach (byte? value in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyCheckedNullableByteToNullableChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableByteToDecimalTest(bool useInterpreter)
        {
            foreach (byte? value in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyCheckedNullableByteToDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableByteToNullableDecimalTest(bool useInterpreter)
        {
            foreach (byte? value in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyCheckedNullableByteToNullableDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableByteToDoubleTest(bool useInterpreter)
        {
            foreach (byte? value in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyCheckedNullableByteToDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableByteToNullableDoubleTest(bool useInterpreter)
        {
            foreach (byte? value in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyCheckedNullableByteToNullableDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableByteToEnumTest(bool useInterpreter)
        {
            foreach (byte? value in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyCheckedNullableByteToEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableByteToNullableEnumTest(bool useInterpreter)
        {
            foreach (byte? value in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyCheckedNullableByteToNullableEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableByteToEnumLongTest(bool useInterpreter)
        {
            foreach (byte? value in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyCheckedNullableByteToEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableByteToNullableEnumLongTest(bool useInterpreter)
        {
            foreach (byte? value in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyCheckedNullableByteToNullableEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableByteToFloatTest(bool useInterpreter)
        {
            foreach (byte? value in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyCheckedNullableByteToFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableByteToNullableFloatTest(bool useInterpreter)
        {
            foreach (byte? value in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyCheckedNullableByteToNullableFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableByteToIntTest(bool useInterpreter)
        {
            foreach (byte? value in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyCheckedNullableByteToInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableByteToNullableIntTest(bool useInterpreter)
        {
            foreach (byte? value in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyCheckedNullableByteToNullableInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableByteToLongTest(bool useInterpreter)
        {
            foreach (byte? value in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyCheckedNullableByteToLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableByteToNullableLongTest(bool useInterpreter)
        {
            foreach (byte? value in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyCheckedNullableByteToNullableLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableByteToSByteTest(bool useInterpreter)
        {
            foreach (byte? value in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyCheckedNullableByteToSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableByteToNullableSByteTest(bool useInterpreter)
        {
            foreach (byte? value in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyCheckedNullableByteToNullableSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableByteToShortTest(bool useInterpreter)
        {
            foreach (byte? value in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyCheckedNullableByteToShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableByteToNullableShortTest(bool useInterpreter)
        {
            foreach (byte? value in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyCheckedNullableByteToNullableShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableByteToUIntTest(bool useInterpreter)
        {
            foreach (byte? value in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyCheckedNullableByteToUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableByteToNullableUIntTest(bool useInterpreter)
        {
            foreach (byte? value in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyCheckedNullableByteToNullableUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableByteToULongTest(bool useInterpreter)
        {
            foreach (byte? value in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyCheckedNullableByteToULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableByteToNullableULongTest(bool useInterpreter)
        {
            foreach (byte? value in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyCheckedNullableByteToNullableULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableByteToUShortTest(bool useInterpreter)
        {
            foreach (byte? value in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyCheckedNullableByteToUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableByteToNullableUShortTest(bool useInterpreter)
        {
            foreach (byte? value in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyCheckedNullableByteToNullableUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedCharToByteTest(bool useInterpreter)
        {
            foreach (char value in new char[] { '\0', '\b', 'A', '\uffff' })
            {
                VerifyCheckedCharToByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedCharToNullableByteTest(bool useInterpreter)
        {
            foreach (char value in new char[] { '\0', '\b', 'A', '\uffff' })
            {
                VerifyCheckedCharToNullableByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedCharToCharTest(bool useInterpreter)
        {
            foreach (char value in new char[] { '\0', '\b', 'A', '\uffff' })
            {
                VerifyCheckedCharToChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedCharToNullableCharTest(bool useInterpreter)
        {
            foreach (char value in new char[] { '\0', '\b', 'A', '\uffff' })
            {
                VerifyCheckedCharToNullableChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedCharToDecimalTest(bool useInterpreter)
        {
            foreach (char value in new char[] { '\0', '\b', 'A', '\uffff' })
            {
                VerifyCheckedCharToDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedCharToNullableDecimalTest(bool useInterpreter)
        {
            foreach (char value in new char[] { '\0', '\b', 'A', '\uffff' })
            {
                VerifyCheckedCharToNullableDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedCharToDoubleTest(bool useInterpreter)
        {
            foreach (char value in new char[] { '\0', '\b', 'A', '\uffff' })
            {
                VerifyCheckedCharToDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedCharToNullableDoubleTest(bool useInterpreter)
        {
            foreach (char value in new char[] { '\0', '\b', 'A', '\uffff' })
            {
                VerifyCheckedCharToNullableDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedCharToEnumTest(bool useInterpreter)
        {
            foreach (char value in new char[] { '\0', '\b', 'A', '\uffff' })
            {
                VerifyCheckedCharToEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedCharToNullableEnumTest(bool useInterpreter)
        {
            foreach (char value in new char[] { '\0', '\b', 'A', '\uffff' })
            {
                VerifyCheckedCharToNullableEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedCharToEnumLongTest(bool useInterpreter)
        {
            foreach (char value in new char[] { '\0', '\b', 'A', '\uffff' })
            {
                VerifyCheckedCharToEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedCharToNullableEnumLongTest(bool useInterpreter)
        {
            foreach (char value in new char[] { '\0', '\b', 'A', '\uffff' })
            {
                VerifyCheckedCharToNullableEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedCharToFloatTest(bool useInterpreter)
        {
            foreach (char value in new char[] { '\0', '\b', 'A', '\uffff' })
            {
                VerifyCheckedCharToFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedCharToNullableFloatTest(bool useInterpreter)
        {
            foreach (char value in new char[] { '\0', '\b', 'A', '\uffff' })
            {
                VerifyCheckedCharToNullableFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedCharToIntTest(bool useInterpreter)
        {
            foreach (char value in new char[] { '\0', '\b', 'A', '\uffff' })
            {
                VerifyCheckedCharToInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedCharToNullableIntTest(bool useInterpreter)
        {
            foreach (char value in new char[] { '\0', '\b', 'A', '\uffff' })
            {
                VerifyCheckedCharToNullableInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedCharToLongTest(bool useInterpreter)
        {
            foreach (char value in new char[] { '\0', '\b', 'A', '\uffff' })
            {
                VerifyCheckedCharToLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedCharToNullableLongTest(bool useInterpreter)
        {
            foreach (char value in new char[] { '\0', '\b', 'A', '\uffff' })
            {
                VerifyCheckedCharToNullableLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedCharToSByteTest(bool useInterpreter)
        {
            foreach (char value in new char[] { '\0', '\b', 'A', '\uffff' })
            {
                VerifyCheckedCharToSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedCharToNullableSByteTest(bool useInterpreter)
        {
            foreach (char value in new char[] { '\0', '\b', 'A', '\uffff' })
            {
                VerifyCheckedCharToNullableSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedCharToShortTest(bool useInterpreter)
        {
            foreach (char value in new char[] { '\0', '\b', 'A', '\uffff' })
            {
                VerifyCheckedCharToShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedCharToNullableShortTest(bool useInterpreter)
        {
            foreach (char value in new char[] { '\0', '\b', 'A', '\uffff' })
            {
                VerifyCheckedCharToNullableShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedCharToUIntTest(bool useInterpreter)
        {
            foreach (char value in new char[] { '\0', '\b', 'A', '\uffff' })
            {
                VerifyCheckedCharToUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedCharToNullableUIntTest(bool useInterpreter)
        {
            foreach (char value in new char[] { '\0', '\b', 'A', '\uffff' })
            {
                VerifyCheckedCharToNullableUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedCharToULongTest(bool useInterpreter)
        {
            foreach (char value in new char[] { '\0', '\b', 'A', '\uffff' })
            {
                VerifyCheckedCharToULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedCharToNullableULongTest(bool useInterpreter)
        {
            foreach (char value in new char[] { '\0', '\b', 'A', '\uffff' })
            {
                VerifyCheckedCharToNullableULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedCharToUShortTest(bool useInterpreter)
        {
            foreach (char value in new char[] { '\0', '\b', 'A', '\uffff' })
            {
                VerifyCheckedCharToUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedCharToNullableUShortTest(bool useInterpreter)
        {
            foreach (char value in new char[] { '\0', '\b', 'A', '\uffff' })
            {
                VerifyCheckedCharToNullableUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableCharToByteTest(bool useInterpreter)
        {
            foreach (char? value in new char?[] { null, '\0', '\b', 'A', '\uffff' })
            {
                VerifyCheckedNullableCharToByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableCharToNullableByteTest(bool useInterpreter)
        {
            foreach (char? value in new char?[] { null, '\0', '\b', 'A', '\uffff' })
            {
                VerifyCheckedNullableCharToNullableByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableCharToCharTest(bool useInterpreter)
        {
            foreach (char? value in new char?[] { null, '\0', '\b', 'A', '\uffff' })
            {
                VerifyCheckedNullableCharToChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableCharToNullableCharTest(bool useInterpreter)
        {
            foreach (char? value in new char?[] { null, '\0', '\b', 'A', '\uffff' })
            {
                VerifyCheckedNullableCharToNullableChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableCharToDecimalTest(bool useInterpreter)
        {
            foreach (char? value in new char?[] { null, '\0', '\b', 'A', '\uffff' })
            {
                VerifyCheckedNullableCharToDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableCharToNullableDecimalTest(bool useInterpreter)
        {
            foreach (char? value in new char?[] { null, '\0', '\b', 'A', '\uffff' })
            {
                VerifyCheckedNullableCharToNullableDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableCharToDoubleTest(bool useInterpreter)
        {
            foreach (char? value in new char?[] { null, '\0', '\b', 'A', '\uffff' })
            {
                VerifyCheckedNullableCharToDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableCharToNullableDoubleTest(bool useInterpreter)
        {
            foreach (char? value in new char?[] { null, '\0', '\b', 'A', '\uffff' })
            {
                VerifyCheckedNullableCharToNullableDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableCharToEnumTest(bool useInterpreter)
        {
            foreach (char? value in new char?[] { null, '\0', '\b', 'A', '\uffff' })
            {
                VerifyCheckedNullableCharToEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableCharToNullableEnumTest(bool useInterpreter)
        {
            foreach (char? value in new char?[] { null, '\0', '\b', 'A', '\uffff' })
            {
                VerifyCheckedNullableCharToNullableEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableCharToEnumLongTest(bool useInterpreter)
        {
            foreach (char? value in new char?[] { null, '\0', '\b', 'A', '\uffff' })
            {
                VerifyCheckedNullableCharToEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableCharToNullableEnumLongTest(bool useInterpreter)
        {
            foreach (char? value in new char?[] { null, '\0', '\b', 'A', '\uffff' })
            {
                VerifyCheckedNullableCharToNullableEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableCharToFloatTest(bool useInterpreter)
        {
            foreach (char? value in new char?[] { null, '\0', '\b', 'A', '\uffff' })
            {
                VerifyCheckedNullableCharToFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableCharToNullableFloatTest(bool useInterpreter)
        {
            foreach (char? value in new char?[] { null, '\0', '\b', 'A', '\uffff' })
            {
                VerifyCheckedNullableCharToNullableFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableCharToIntTest(bool useInterpreter)
        {
            foreach (char? value in new char?[] { null, '\0', '\b', 'A', '\uffff' })
            {
                VerifyCheckedNullableCharToInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableCharToNullableIntTest(bool useInterpreter)
        {
            foreach (char? value in new char?[] { null, '\0', '\b', 'A', '\uffff' })
            {
                VerifyCheckedNullableCharToNullableInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableCharToLongTest(bool useInterpreter)
        {
            foreach (char? value in new char?[] { null, '\0', '\b', 'A', '\uffff' })
            {
                VerifyCheckedNullableCharToLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableCharToNullableLongTest(bool useInterpreter)
        {
            foreach (char? value in new char?[] { null, '\0', '\b', 'A', '\uffff' })
            {
                VerifyCheckedNullableCharToNullableLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableCharToSByteTest(bool useInterpreter)
        {
            foreach (char? value in new char?[] { null, '\0', '\b', 'A', '\uffff' })
            {
                VerifyCheckedNullableCharToSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableCharToNullableSByteTest(bool useInterpreter)
        {
            foreach (char? value in new char?[] { null, '\0', '\b', 'A', '\uffff' })
            {
                VerifyCheckedNullableCharToNullableSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableCharToShortTest(bool useInterpreter)
        {
            foreach (char? value in new char?[] { null, '\0', '\b', 'A', '\uffff' })
            {
                VerifyCheckedNullableCharToShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableCharToNullableShortTest(bool useInterpreter)
        {
            foreach (char? value in new char?[] { null, '\0', '\b', 'A', '\uffff' })
            {
                VerifyCheckedNullableCharToNullableShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableCharToUIntTest(bool useInterpreter)
        {
            foreach (char? value in new char?[] { null, '\0', '\b', 'A', '\uffff' })
            {
                VerifyCheckedNullableCharToUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableCharToNullableUIntTest(bool useInterpreter)
        {
            foreach (char? value in new char?[] { null, '\0', '\b', 'A', '\uffff' })
            {
                VerifyCheckedNullableCharToNullableUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableCharToULongTest(bool useInterpreter)
        {
            foreach (char? value in new char?[] { null, '\0', '\b', 'A', '\uffff' })
            {
                VerifyCheckedNullableCharToULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableCharToNullableULongTest(bool useInterpreter)
        {
            foreach (char? value in new char?[] { null, '\0', '\b', 'A', '\uffff' })
            {
                VerifyCheckedNullableCharToNullableULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableCharToUShortTest(bool useInterpreter)
        {
            foreach (char? value in new char?[] { null, '\0', '\b', 'A', '\uffff' })
            {
                VerifyCheckedNullableCharToUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableCharToNullableUShortTest(bool useInterpreter)
        {
            foreach (char? value in new char?[] { null, '\0', '\b', 'A', '\uffff' })
            {
                VerifyCheckedNullableCharToNullableUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedDecimalToByteTest(bool useInterpreter)
        {
            foreach (decimal value in new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyCheckedDecimalToByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedDecimalToNullableByteTest(bool useInterpreter)
        {
            foreach (decimal value in new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyCheckedDecimalToNullableByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedDecimalToCharTest(bool useInterpreter)
        {
            foreach (decimal value in new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyCheckedDecimalToChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedDecimalToNullableCharTest(bool useInterpreter)
        {
            foreach (decimal value in new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyCheckedDecimalToNullableChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedDecimalToDecimalTest(bool useInterpreter)
        {
            foreach (decimal value in new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyCheckedDecimalToDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedDecimalToNullableDecimalTest(bool useInterpreter)
        {
            foreach (decimal value in new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyCheckedDecimalToNullableDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedDecimalToDoubleTest(bool useInterpreter)
        {
            foreach (decimal value in new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyCheckedDecimalToDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedDecimalToNullableDoubleTest(bool useInterpreter)
        {
            foreach (decimal value in new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyCheckedDecimalToNullableDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedDecimalToFloatTest(bool useInterpreter)
        {
            foreach (decimal value in new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyCheckedDecimalToFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedDecimalToNullableFloatTest(bool useInterpreter)
        {
            foreach (decimal value in new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyCheckedDecimalToNullableFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedDecimalToIntTest(bool useInterpreter)
        {
            foreach (decimal value in new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyCheckedDecimalToInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedDecimalToNullableIntTest(bool useInterpreter)
        {
            foreach (decimal value in new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyCheckedDecimalToNullableInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedDecimalToLongTest(bool useInterpreter)
        {
            foreach (decimal value in new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyCheckedDecimalToLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedDecimalToNullableLongTest(bool useInterpreter)
        {
            foreach (decimal value in new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyCheckedDecimalToNullableLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedDecimalToSByteTest(bool useInterpreter)
        {
            foreach (decimal value in new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyCheckedDecimalToSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedDecimalToNullableSByteTest(bool useInterpreter)
        {
            foreach (decimal value in new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyCheckedDecimalToNullableSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedDecimalToShortTest(bool useInterpreter)
        {
            foreach (decimal value in new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyCheckedDecimalToShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedDecimalToNullableShortTest(bool useInterpreter)
        {
            foreach (decimal value in new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyCheckedDecimalToNullableShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedDecimalToUIntTest(bool useInterpreter)
        {
            foreach (decimal value in new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyCheckedDecimalToUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedDecimalToNullableUIntTest(bool useInterpreter)
        {
            foreach (decimal value in new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyCheckedDecimalToNullableUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedDecimalToULongTest(bool useInterpreter)
        {
            foreach (decimal value in new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyCheckedDecimalToULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedDecimalToNullableULongTest(bool useInterpreter)
        {
            foreach (decimal value in new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyCheckedDecimalToNullableULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedDecimalToUShortTest(bool useInterpreter)
        {
            foreach (decimal value in new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyCheckedDecimalToUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedDecimalToNullableUShortTest(bool useInterpreter)
        {
            foreach (decimal value in new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyCheckedDecimalToNullableUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableDecimalToByteTest(bool useInterpreter)
        {
            foreach (decimal? value in new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyCheckedNullableDecimalToByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableDecimalToNullableByteTest(bool useInterpreter)
        {
            foreach (decimal? value in new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyCheckedNullableDecimalToNullableByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableDecimalToCharTest(bool useInterpreter)
        {
            foreach (decimal? value in new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyCheckedNullableDecimalToChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableDecimalToNullableCharTest(bool useInterpreter)
        {
            foreach (decimal? value in new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyCheckedNullableDecimalToNullableChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableDecimalToDecimalTest(bool useInterpreter)
        {
            foreach (decimal? value in new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyCheckedNullableDecimalToDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableDecimalToNullableDecimalTest(bool useInterpreter)
        {
            foreach (decimal? value in new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyCheckedNullableDecimalToNullableDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableDecimalToDoubleTest(bool useInterpreter)
        {
            foreach (decimal? value in new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyCheckedNullableDecimalToDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableDecimalToNullableDoubleTest(bool useInterpreter)
        {
            foreach (decimal? value in new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyCheckedNullableDecimalToNullableDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableDecimalToFloatTest(bool useInterpreter)
        {
            foreach (decimal? value in new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyCheckedNullableDecimalToFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableDecimalToNullableFloatTest(bool useInterpreter)
        {
            foreach (decimal? value in new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyCheckedNullableDecimalToNullableFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableDecimalToIntTest(bool useInterpreter)
        {
            foreach (decimal? value in new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyCheckedNullableDecimalToInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableDecimalToNullableIntTest(bool useInterpreter)
        {
            foreach (decimal? value in new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyCheckedNullableDecimalToNullableInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableDecimalToLongTest(bool useInterpreter)
        {
            foreach (decimal? value in new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyCheckedNullableDecimalToLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableDecimalToNullableLongTest(bool useInterpreter)
        {
            foreach (decimal? value in new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyCheckedNullableDecimalToNullableLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableDecimalToSByteTest(bool useInterpreter)
        {
            foreach (decimal? value in new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyCheckedNullableDecimalToSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableDecimalToNullableSByteTest(bool useInterpreter)
        {
            foreach (decimal? value in new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyCheckedNullableDecimalToNullableSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableDecimalToShortTest(bool useInterpreter)
        {
            foreach (decimal? value in new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyCheckedNullableDecimalToShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableDecimalToNullableShortTest(bool useInterpreter)
        {
            foreach (decimal? value in new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyCheckedNullableDecimalToNullableShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableDecimalToUIntTest(bool useInterpreter)
        {
            foreach (decimal? value in new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyCheckedNullableDecimalToUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableDecimalToNullableUIntTest(bool useInterpreter)
        {
            foreach (decimal? value in new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyCheckedNullableDecimalToNullableUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableDecimalToULongTest(bool useInterpreter)
        {
            foreach (decimal? value in new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyCheckedNullableDecimalToULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableDecimalToNullableULongTest(bool useInterpreter)
        {
            foreach (decimal? value in new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyCheckedNullableDecimalToNullableULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableDecimalToUShortTest(bool useInterpreter)
        {
            foreach (decimal? value in new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyCheckedNullableDecimalToUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableDecimalToNullableUShortTest(bool useInterpreter)
        {
            foreach (decimal? value in new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyCheckedNullableDecimalToNullableUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedDoubleToByteTest(bool useInterpreter)
        {
            foreach (double value in new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyCheckedDoubleToByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedDoubleToNullableByteTest(bool useInterpreter)
        {
            foreach (double value in new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyCheckedDoubleToNullableByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedDoubleToCharTest(bool useInterpreter)
        {
            foreach (double value in new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyCheckedDoubleToChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedDoubleToNullableCharTest(bool useInterpreter)
        {
            foreach (double value in new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyCheckedDoubleToNullableChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedDoubleToDecimalTest(bool useInterpreter)
        {
            foreach (double value in new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyCheckedDoubleToDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedDoubleToNullableDecimalTest(bool useInterpreter)
        {
            foreach (double value in new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyCheckedDoubleToNullableDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedDoubleToDoubleTest(bool useInterpreter)
        {
            foreach (double value in new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyCheckedDoubleToDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedDoubleToNullableDoubleTest(bool useInterpreter)
        {
            foreach (double value in new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyCheckedDoubleToNullableDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedDoubleToEnumTest(bool useInterpreter)
        {
            foreach (double value in new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyCheckedDoubleToEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedDoubleToNullableEnumTest(bool useInterpreter)
        {
            foreach (double value in new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyCheckedDoubleToNullableEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedDoubleToEnumLongTest(bool useInterpreter)
        {
            foreach (double value in new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyCheckedDoubleToEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedDoubleToNullableEnumLongTest(bool useInterpreter)
        {
            foreach (double value in new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyCheckedDoubleToNullableEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedDoubleToFloatTest(bool useInterpreter)
        {
            foreach (double value in new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyCheckedDoubleToFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedDoubleToNullableFloatTest(bool useInterpreter)
        {
            foreach (double value in new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyCheckedDoubleToNullableFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedDoubleToIntTest(bool useInterpreter)
        {
            foreach (double value in new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyCheckedDoubleToInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedDoubleToNullableIntTest(bool useInterpreter)
        {
            foreach (double value in new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyCheckedDoubleToNullableInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedDoubleToLongTest(bool useInterpreter)
        {
            foreach (double value in new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyCheckedDoubleToLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedDoubleToNullableLongTest(bool useInterpreter)
        {
            foreach (double value in new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyCheckedDoubleToNullableLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedDoubleToSByteTest(bool useInterpreter)
        {
            foreach (double value in new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyCheckedDoubleToSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedDoubleToNullableSByteTest(bool useInterpreter)
        {
            foreach (double value in new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyCheckedDoubleToNullableSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedDoubleToShortTest(bool useInterpreter)
        {
            foreach (double value in new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyCheckedDoubleToShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedDoubleToNullableShortTest(bool useInterpreter)
        {
            foreach (double value in new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyCheckedDoubleToNullableShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedDoubleToUIntTest(bool useInterpreter)
        {
            foreach (double value in new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyCheckedDoubleToUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedDoubleToNullableUIntTest(bool useInterpreter)
        {
            foreach (double value in new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyCheckedDoubleToNullableUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedDoubleToULongTest(bool useInterpreter)
        {
            foreach (double value in new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyCheckedDoubleToULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedDoubleToNullableULongTest(bool useInterpreter)
        {
            foreach (double value in new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyCheckedDoubleToNullableULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedDoubleToUShortTest(bool useInterpreter)
        {
            foreach (double value in new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyCheckedDoubleToUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedDoubleToNullableUShortTest(bool useInterpreter)
        {
            foreach (double value in new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyCheckedDoubleToNullableUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableDoubleToByteTest(bool useInterpreter)
        {
            foreach (double? value in new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyCheckedNullableDoubleToByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableDoubleToNullableByteTest(bool useInterpreter)
        {
            foreach (double? value in new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyCheckedNullableDoubleToNullableByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableDoubleToCharTest(bool useInterpreter)
        {
            foreach (double? value in new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyCheckedNullableDoubleToChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableDoubleToNullableCharTest(bool useInterpreter)
        {
            foreach (double? value in new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyCheckedNullableDoubleToNullableChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableDoubleToDecimalTest(bool useInterpreter)
        {
            foreach (double? value in new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyCheckedNullableDoubleToDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableDoubleToNullableDecimalTest(bool useInterpreter)
        {
            foreach (double? value in new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyCheckedNullableDoubleToNullableDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableDoubleToDoubleTest(bool useInterpreter)
        {
            foreach (double? value in new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyCheckedNullableDoubleToDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableDoubleToNullableDoubleTest(bool useInterpreter)
        {
            foreach (double? value in new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyCheckedNullableDoubleToNullableDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableDoubleToEnumTest(bool useInterpreter)
        {
            foreach (double? value in new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyCheckedNullableDoubleToEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableDoubleToNullableEnumTest(bool useInterpreter)
        {
            foreach (double? value in new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyCheckedNullableDoubleToNullableEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableDoubleToEnumLongTest(bool useInterpreter)
        {
            foreach (double? value in new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyCheckedNullableDoubleToEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableDoubleToNullableEnumLongTest(bool useInterpreter)
        {
            foreach (double? value in new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyCheckedNullableDoubleToNullableEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableDoubleToFloatTest(bool useInterpreter)
        {
            foreach (double? value in new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyCheckedNullableDoubleToFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableDoubleToNullableFloatTest(bool useInterpreter)
        {
            foreach (double? value in new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyCheckedNullableDoubleToNullableFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableDoubleToIntTest(bool useInterpreter)
        {
            foreach (double? value in new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyCheckedNullableDoubleToInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableDoubleToNullableIntTest(bool useInterpreter)
        {
            foreach (double? value in new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyCheckedNullableDoubleToNullableInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableDoubleToLongTest(bool useInterpreter)
        {
            foreach (double? value in new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyCheckedNullableDoubleToLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableDoubleToNullableLongTest(bool useInterpreter)
        {
            foreach (double? value in new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyCheckedNullableDoubleToNullableLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableDoubleToSByteTest(bool useInterpreter)
        {
            foreach (double? value in new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyCheckedNullableDoubleToSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableDoubleToNullableSByteTest(bool useInterpreter)
        {
            foreach (double? value in new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyCheckedNullableDoubleToNullableSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableDoubleToShortTest(bool useInterpreter)
        {
            foreach (double? value in new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyCheckedNullableDoubleToShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableDoubleToNullableShortTest(bool useInterpreter)
        {
            foreach (double? value in new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyCheckedNullableDoubleToNullableShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableDoubleToUIntTest(bool useInterpreter)
        {
            foreach (double? value in new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyCheckedNullableDoubleToUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableDoubleToNullableUIntTest(bool useInterpreter)
        {
            foreach (double? value in new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyCheckedNullableDoubleToNullableUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableDoubleToULongTest(bool useInterpreter)
        {
            foreach (double? value in new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyCheckedNullableDoubleToULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableDoubleToNullableULongTest(bool useInterpreter)
        {
            foreach (double? value in new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyCheckedNullableDoubleToNullableULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableDoubleToUShortTest(bool useInterpreter)
        {
            foreach (double? value in new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyCheckedNullableDoubleToUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableDoubleToNullableUShortTest(bool useInterpreter)
        {
            foreach (double? value in new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyCheckedNullableDoubleToNullableUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedEnumToByteTest(bool useInterpreter)
        {
            foreach (E value in new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyCheckedEnumToByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedEnumToNullableByteTest(bool useInterpreter)
        {
            foreach (E value in new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyCheckedEnumToNullableByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedEnumToCharTest(bool useInterpreter)
        {
            foreach (E value in new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyCheckedEnumToChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedEnumToNullableCharTest(bool useInterpreter)
        {
            foreach (E value in new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyCheckedEnumToNullableChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedEnumToDoubleTest(bool useInterpreter)
        {
            foreach (E value in new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyCheckedEnumToDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedEnumToNullableDoubleTest(bool useInterpreter)
        {
            foreach (E value in new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyCheckedEnumToNullableDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedEnumToEnumTest(bool useInterpreter)
        {
            foreach (E value in new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyCheckedEnumToEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedEnumToNullableEnumTest(bool useInterpreter)
        {
            foreach (E value in new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyCheckedEnumToNullableEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedEnumToEnumLongTest(bool useInterpreter)
        {
            foreach (E value in new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyCheckedEnumToEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedEnumToNullableEnumLongTest(bool useInterpreter)
        {
            foreach (E value in new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyCheckedEnumToNullableEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedEnumToFloatTest(bool useInterpreter)
        {
            foreach (E value in new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyCheckedEnumToFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedEnumToNullableFloatTest(bool useInterpreter)
        {
            foreach (E value in new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyCheckedEnumToNullableFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedEnumToIntTest(bool useInterpreter)
        {
            foreach (E value in new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyCheckedEnumToInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedEnumToNullableIntTest(bool useInterpreter)
        {
            foreach (E value in new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyCheckedEnumToNullableInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedEnumToLongTest(bool useInterpreter)
        {
            foreach (E value in new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyCheckedEnumToLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedEnumToNullableLongTest(bool useInterpreter)
        {
            foreach (E value in new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyCheckedEnumToNullableLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedEnumToSByteTest(bool useInterpreter)
        {
            foreach (E value in new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyCheckedEnumToSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedEnumToNullableSByteTest(bool useInterpreter)
        {
            foreach (E value in new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyCheckedEnumToNullableSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedEnumToShortTest(bool useInterpreter)
        {
            foreach (E value in new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyCheckedEnumToShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedEnumToNullableShortTest(bool useInterpreter)
        {
            foreach (E value in new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyCheckedEnumToNullableShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedEnumToUIntTest(bool useInterpreter)
        {
            foreach (E value in new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyCheckedEnumToUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedEnumToNullableUIntTest(bool useInterpreter)
        {
            foreach (E value in new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyCheckedEnumToNullableUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedEnumToULongTest(bool useInterpreter)
        {
            foreach (E value in new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyCheckedEnumToULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedEnumToNullableULongTest(bool useInterpreter)
        {
            foreach (E value in new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyCheckedEnumToNullableULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedEnumToUShortTest(bool useInterpreter)
        {
            foreach (E value in new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyCheckedEnumToUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedEnumToNullableUShortTest(bool useInterpreter)
        {
            foreach (E value in new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyCheckedEnumToNullableUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableEnumToByteTest(bool useInterpreter)
        {
            foreach (E? value in new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyCheckedNullableEnumToByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableEnumToNullableByteTest(bool useInterpreter)
        {
            foreach (E? value in new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyCheckedNullableEnumToNullableByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableEnumToCharTest(bool useInterpreter)
        {
            foreach (E? value in new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyCheckedNullableEnumToChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableEnumToNullableCharTest(bool useInterpreter)
        {
            foreach (E? value in new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyCheckedNullableEnumToNullableChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableEnumToDoubleTest(bool useInterpreter)
        {
            foreach (E? value in new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyCheckedNullableEnumToDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableEnumToNullableDoubleTest(bool useInterpreter)
        {
            foreach (E? value in new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyCheckedNullableEnumToNullableDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableEnumToEnumTest(bool useInterpreter)
        {
            foreach (E? value in new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyCheckedNullableEnumToEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableEnumToNullableEnumTest(bool useInterpreter)
        {
            foreach (E? value in new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyCheckedNullableEnumToNullableEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableEnumToEnumLongTest(bool useInterpreter)
        {
            foreach (E? value in new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyCheckedNullableEnumToEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableEnumToNullableEnumLongTest(bool useInterpreter)
        {
            foreach (E? value in new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyCheckedNullableEnumToNullableEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableEnumToFloatTest(bool useInterpreter)
        {
            foreach (E? value in new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyCheckedNullableEnumToFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableEnumToNullableFloatTest(bool useInterpreter)
        {
            foreach (E? value in new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyCheckedNullableEnumToNullableFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableEnumToIntTest(bool useInterpreter)
        {
            foreach (E? value in new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyCheckedNullableEnumToInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableEnumToNullableIntTest(bool useInterpreter)
        {
            foreach (E? value in new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyCheckedNullableEnumToNullableInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableEnumToLongTest(bool useInterpreter)
        {
            foreach (E? value in new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyCheckedNullableEnumToLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableEnumToNullableLongTest(bool useInterpreter)
        {
            foreach (E? value in new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyCheckedNullableEnumToNullableLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableEnumToSByteTest(bool useInterpreter)
        {
            foreach (E? value in new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyCheckedNullableEnumToSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableEnumToNullableSByteTest(bool useInterpreter)
        {
            foreach (E? value in new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyCheckedNullableEnumToNullableSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableEnumToShortTest(bool useInterpreter)
        {
            foreach (E? value in new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyCheckedNullableEnumToShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableEnumToNullableShortTest(bool useInterpreter)
        {
            foreach (E? value in new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyCheckedNullableEnumToNullableShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableEnumToUIntTest(bool useInterpreter)
        {
            foreach (E? value in new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyCheckedNullableEnumToUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableEnumToNullableUIntTest(bool useInterpreter)
        {
            foreach (E? value in new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyCheckedNullableEnumToNullableUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableEnumToULongTest(bool useInterpreter)
        {
            foreach (E? value in new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyCheckedNullableEnumToULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableEnumToNullableULongTest(bool useInterpreter)
        {
            foreach (E? value in new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyCheckedNullableEnumToNullableULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableEnumToUShortTest(bool useInterpreter)
        {
            foreach (E? value in new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyCheckedNullableEnumToUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableEnumToNullableUShortTest(bool useInterpreter)
        {
            foreach (E? value in new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyCheckedNullableEnumToNullableUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedEnumLongToByteTest(bool useInterpreter)
        {
            foreach (El value in new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyCheckedEnumLongToByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedEnumLongToNullableByteTest(bool useInterpreter)
        {
            foreach (El value in new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyCheckedEnumLongToNullableByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedEnumLongToCharTest(bool useInterpreter)
        {
            foreach (El value in new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyCheckedEnumLongToChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedEnumLongToNullableCharTest(bool useInterpreter)
        {
            foreach (El value in new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyCheckedEnumLongToNullableChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedEnumLongToDoubleTest(bool useInterpreter)
        {
            foreach (El value in new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyCheckedEnumLongToDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedEnumLongToNullableDoubleTest(bool useInterpreter)
        {
            foreach (El value in new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyCheckedEnumLongToNullableDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedEnumLongToEnumTest(bool useInterpreter)
        {
            foreach (El value in new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyCheckedEnumLongToEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedEnumLongToNullableEnumTest(bool useInterpreter)
        {
            foreach (El value in new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyCheckedEnumLongToNullableEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedEnumLongToEnumLongTest(bool useInterpreter)
        {
            foreach (El value in new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyCheckedEnumLongToEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedEnumLongToNullableEnumLongTest(bool useInterpreter)
        {
            foreach (El value in new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyCheckedEnumLongToNullableEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedEnumLongToFloatTest(bool useInterpreter)
        {
            foreach (El value in new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyCheckedEnumLongToFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedEnumLongToNullableFloatTest(bool useInterpreter)
        {
            foreach (El value in new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyCheckedEnumLongToNullableFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedEnumLongToIntTest(bool useInterpreter)
        {
            foreach (El value in new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyCheckedEnumLongToInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedEnumLongToNullableIntTest(bool useInterpreter)
        {
            foreach (El value in new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyCheckedEnumLongToNullableInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedEnumLongToLongTest(bool useInterpreter)
        {
            foreach (El value in new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyCheckedEnumLongToLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedEnumLongToNullableLongTest(bool useInterpreter)
        {
            foreach (El value in new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyCheckedEnumLongToNullableLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedEnumLongToSByteTest(bool useInterpreter)
        {
            foreach (El value in new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyCheckedEnumLongToSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedEnumLongToNullableSByteTest(bool useInterpreter)
        {
            foreach (El value in new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyCheckedEnumLongToNullableSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedEnumLongToShortTest(bool useInterpreter)
        {
            foreach (El value in new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyCheckedEnumLongToShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedEnumLongToNullableShortTest(bool useInterpreter)
        {
            foreach (El value in new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyCheckedEnumLongToNullableShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedEnumLongToUIntTest(bool useInterpreter)
        {
            foreach (El value in new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyCheckedEnumLongToUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedEnumLongToNullableUIntTest(bool useInterpreter)
        {
            foreach (El value in new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyCheckedEnumLongToNullableUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedEnumLongToULongTest(bool useInterpreter)
        {
            foreach (El value in new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyCheckedEnumLongToULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedEnumLongToNullableULongTest(bool useInterpreter)
        {
            foreach (El value in new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyCheckedEnumLongToNullableULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedEnumLongToUShortTest(bool useInterpreter)
        {
            foreach (El value in new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyCheckedEnumLongToUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedEnumLongToNullableUShortTest(bool useInterpreter)
        {
            foreach (El value in new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyCheckedEnumLongToNullableUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableEnumLongToByteTest(bool useInterpreter)
        {
            foreach (El? value in new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyCheckedNullableEnumLongToByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableEnumLongToNullableByteTest(bool useInterpreter)
        {
            foreach (El? value in new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyCheckedNullableEnumLongToNullableByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableEnumLongToCharTest(bool useInterpreter)
        {
            foreach (El? value in new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyCheckedNullableEnumLongToChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableEnumLongToNullableCharTest(bool useInterpreter)
        {
            foreach (El? value in new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyCheckedNullableEnumLongToNullableChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableEnumLongToDoubleTest(bool useInterpreter)
        {
            foreach (El? value in new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyCheckedNullableEnumLongToDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableEnumLongToNullableDoubleTest(bool useInterpreter)
        {
            foreach (El? value in new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyCheckedNullableEnumLongToNullableDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableEnumLongToEnumTest(bool useInterpreter)
        {
            foreach (El? value in new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyCheckedNullableEnumLongToEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableEnumLongToNullableEnumTest(bool useInterpreter)
        {
            foreach (El? value in new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyCheckedNullableEnumLongToNullableEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableEnumLongToEnumLongTest(bool useInterpreter)
        {
            foreach (El? value in new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyCheckedNullableEnumLongToEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableEnumLongToNullableEnumLongTest(bool useInterpreter)
        {
            foreach (El? value in new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyCheckedNullableEnumLongToNullableEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableEnumLongToFloatTest(bool useInterpreter)
        {
            foreach (El? value in new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyCheckedNullableEnumLongToFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableEnumLongToNullableFloatTest(bool useInterpreter)
        {
            foreach (El? value in new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyCheckedNullableEnumLongToNullableFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableEnumLongToIntTest(bool useInterpreter)
        {
            foreach (El? value in new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyCheckedNullableEnumLongToInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableEnumLongToNullableIntTest(bool useInterpreter)
        {
            foreach (El? value in new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyCheckedNullableEnumLongToNullableInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableEnumLongToLongTest(bool useInterpreter)
        {
            foreach (El? value in new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyCheckedNullableEnumLongToLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableEnumLongToNullableLongTest(bool useInterpreter)
        {
            foreach (El? value in new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyCheckedNullableEnumLongToNullableLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableEnumLongToSByteTest(bool useInterpreter)
        {
            foreach (El? value in new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyCheckedNullableEnumLongToSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableEnumLongToNullableSByteTest(bool useInterpreter)
        {
            foreach (El? value in new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyCheckedNullableEnumLongToNullableSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableEnumLongToShortTest(bool useInterpreter)
        {
            foreach (El? value in new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyCheckedNullableEnumLongToShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableEnumLongToNullableShortTest(bool useInterpreter)
        {
            foreach (El? value in new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyCheckedNullableEnumLongToNullableShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableEnumLongToUIntTest(bool useInterpreter)
        {
            foreach (El? value in new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyCheckedNullableEnumLongToUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableEnumLongToNullableUIntTest(bool useInterpreter)
        {
            foreach (El? value in new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyCheckedNullableEnumLongToNullableUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableEnumLongToULongTest(bool useInterpreter)
        {
            foreach (El? value in new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyCheckedNullableEnumLongToULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableEnumLongToNullableULongTest(bool useInterpreter)
        {
            foreach (El? value in new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyCheckedNullableEnumLongToNullableULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableEnumLongToUShortTest(bool useInterpreter)
        {
            foreach (El? value in new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyCheckedNullableEnumLongToUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableEnumLongToNullableUShortTest(bool useInterpreter)
        {
            foreach (El? value in new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyCheckedNullableEnumLongToNullableUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedFloatToByteTest(bool useInterpreter)
        {
            foreach (float value in new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyCheckedFloatToByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedFloatToNullableByteTest(bool useInterpreter)
        {
            foreach (float value in new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyCheckedFloatToNullableByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedFloatToCharTest(bool useInterpreter)
        {
            foreach (float value in new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyCheckedFloatToChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedFloatToNullableCharTest(bool useInterpreter)
        {
            foreach (float value in new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyCheckedFloatToNullableChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedFloatToDecimalTest(bool useInterpreter)
        {
            foreach (float value in new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyCheckedFloatToDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedFloatToNullableDecimalTest(bool useInterpreter)
        {
            foreach (float value in new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyCheckedFloatToNullableDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedFloatToDoubleTest(bool useInterpreter)
        {
            foreach (float value in new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyCheckedFloatToDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedFloatToNullableDoubleTest(bool useInterpreter)
        {
            foreach (float value in new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyCheckedFloatToNullableDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedFloatToEnumTest(bool useInterpreter)
        {
            foreach (float value in new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyCheckedFloatToEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedFloatToNullableEnumTest(bool useInterpreter)
        {
            foreach (float value in new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyCheckedFloatToNullableEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedFloatToEnumLongTest(bool useInterpreter)
        {
            foreach (float value in new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyCheckedFloatToEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedFloatToNullableEnumLongTest(bool useInterpreter)
        {
            foreach (float value in new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyCheckedFloatToNullableEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedFloatToFloatTest(bool useInterpreter)
        {
            foreach (float value in new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyCheckedFloatToFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedFloatToNullableFloatTest(bool useInterpreter)
        {
            foreach (float value in new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyCheckedFloatToNullableFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedFloatToIntTest(bool useInterpreter)
        {
            foreach (float value in new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyCheckedFloatToInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedFloatToNullableIntTest(bool useInterpreter)
        {
            foreach (float value in new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyCheckedFloatToNullableInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedFloatToLongTest(bool useInterpreter)
        {
            foreach (float value in new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyCheckedFloatToLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedFloatToNullableLongTest(bool useInterpreter)
        {
            foreach (float value in new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyCheckedFloatToNullableLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedFloatToSByteTest(bool useInterpreter)
        {
            foreach (float value in new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyCheckedFloatToSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedFloatToNullableSByteTest(bool useInterpreter)
        {
            foreach (float value in new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyCheckedFloatToNullableSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedFloatToShortTest(bool useInterpreter)
        {
            foreach (float value in new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyCheckedFloatToShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedFloatToNullableShortTest(bool useInterpreter)
        {
            foreach (float value in new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyCheckedFloatToNullableShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedFloatToUIntTest(bool useInterpreter)
        {
            foreach (float value in new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyCheckedFloatToUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedFloatToNullableUIntTest(bool useInterpreter)
        {
            foreach (float value in new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyCheckedFloatToNullableUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedFloatToULongTest(bool useInterpreter)
        {
            foreach (float value in new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyCheckedFloatToULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedFloatToNullableULongTest(bool useInterpreter)
        {
            foreach (float value in new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyCheckedFloatToNullableULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedFloatToUShortTest(bool useInterpreter)
        {
            foreach (float value in new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyCheckedFloatToUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedFloatToNullableUShortTest(bool useInterpreter)
        {
            foreach (float value in new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyCheckedFloatToNullableUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableFloatToByteTest(bool useInterpreter)
        {
            foreach (float? value in new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyCheckedNullableFloatToByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableFloatToNullableByteTest(bool useInterpreter)
        {
            foreach (float? value in new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyCheckedNullableFloatToNullableByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableFloatToCharTest(bool useInterpreter)
        {
            foreach (float? value in new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyCheckedNullableFloatToChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableFloatToNullableCharTest(bool useInterpreter)
        {
            foreach (float? value in new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyCheckedNullableFloatToNullableChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableFloatToDecimalTest(bool useInterpreter)
        {
            foreach (float? value in new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyCheckedNullableFloatToDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableFloatToNullableDecimalTest(bool useInterpreter)
        {
            foreach (float? value in new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyCheckedNullableFloatToNullableDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableFloatToDoubleTest(bool useInterpreter)
        {
            foreach (float? value in new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyCheckedNullableFloatToDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableFloatToNullableDoubleTest(bool useInterpreter)
        {
            foreach (float? value in new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyCheckedNullableFloatToNullableDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableFloatToEnumTest(bool useInterpreter)
        {
            foreach (float? value in new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyCheckedNullableFloatToEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableFloatToNullableEnumTest(bool useInterpreter)
        {
            foreach (float? value in new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyCheckedNullableFloatToNullableEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableFloatToEnumLongTest(bool useInterpreter)
        {
            foreach (float? value in new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyCheckedNullableFloatToEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableFloatToNullableEnumLongTest(bool useInterpreter)
        {
            foreach (float? value in new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyCheckedNullableFloatToNullableEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableFloatToFloatTest(bool useInterpreter)
        {
            foreach (float? value in new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyCheckedNullableFloatToFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableFloatToNullableFloatTest(bool useInterpreter)
        {
            foreach (float? value in new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyCheckedNullableFloatToNullableFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableFloatToIntTest(bool useInterpreter)
        {
            foreach (float? value in new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyCheckedNullableFloatToInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableFloatToNullableIntTest(bool useInterpreter)
        {
            foreach (float? value in new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyCheckedNullableFloatToNullableInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableFloatToLongTest(bool useInterpreter)
        {
            foreach (float? value in new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyCheckedNullableFloatToLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableFloatToNullableLongTest(bool useInterpreter)
        {
            foreach (float? value in new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyCheckedNullableFloatToNullableLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableFloatToSByteTest(bool useInterpreter)
        {
            foreach (float? value in new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyCheckedNullableFloatToSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableFloatToNullableSByteTest(bool useInterpreter)
        {
            foreach (float? value in new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyCheckedNullableFloatToNullableSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableFloatToShortTest(bool useInterpreter)
        {
            foreach (float? value in new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyCheckedNullableFloatToShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableFloatToNullableShortTest(bool useInterpreter)
        {
            foreach (float? value in new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyCheckedNullableFloatToNullableShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableFloatToUIntTest(bool useInterpreter)
        {
            foreach (float? value in new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyCheckedNullableFloatToUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableFloatToNullableUIntTest(bool useInterpreter)
        {
            foreach (float? value in new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyCheckedNullableFloatToNullableUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableFloatToULongTest(bool useInterpreter)
        {
            foreach (float? value in new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyCheckedNullableFloatToULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableFloatToNullableULongTest(bool useInterpreter)
        {
            foreach (float? value in new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyCheckedNullableFloatToNullableULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableFloatToUShortTest(bool useInterpreter)
        {
            foreach (float? value in new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyCheckedNullableFloatToUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableFloatToNullableUShortTest(bool useInterpreter)
        {
            foreach (float? value in new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyCheckedNullableFloatToNullableUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedIntToByteTest(bool useInterpreter)
        {
            foreach (int value in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCheckedIntToByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedIntToNullableByteTest(bool useInterpreter)
        {
            foreach (int value in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCheckedIntToNullableByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedIntToCharTest(bool useInterpreter)
        {
            foreach (int value in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCheckedIntToChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedIntToNullableCharTest(bool useInterpreter)
        {
            foreach (int value in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCheckedIntToNullableChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedIntToDecimalTest(bool useInterpreter)
        {
            foreach (int value in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCheckedIntToDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedIntToNullableDecimalTest(bool useInterpreter)
        {
            foreach (int value in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCheckedIntToNullableDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedIntToDoubleTest(bool useInterpreter)
        {
            foreach (int value in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCheckedIntToDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedIntToNullableDoubleTest(bool useInterpreter)
        {
            foreach (int value in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCheckedIntToNullableDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedIntToEnumTest(bool useInterpreter)
        {
            foreach (int value in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCheckedIntToEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedIntToNullableEnumTest(bool useInterpreter)
        {
            foreach (int value in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCheckedIntToNullableEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedIntToEnumLongTest(bool useInterpreter)
        {
            foreach (int value in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCheckedIntToEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedIntToNullableEnumLongTest(bool useInterpreter)
        {
            foreach (int value in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCheckedIntToNullableEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedIntToFloatTest(bool useInterpreter)
        {
            foreach (int value in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCheckedIntToFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedIntToNullableFloatTest(bool useInterpreter)
        {
            foreach (int value in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCheckedIntToNullableFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedIntToIntTest(bool useInterpreter)
        {
            foreach (int value in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCheckedIntToInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedIntToNullableIntTest(bool useInterpreter)
        {
            foreach (int value in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCheckedIntToNullableInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedIntToLongTest(bool useInterpreter)
        {
            foreach (int value in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCheckedIntToLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedIntToNullableLongTest(bool useInterpreter)
        {
            foreach (int value in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCheckedIntToNullableLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedIntToSByteTest(bool useInterpreter)
        {
            foreach (int value in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCheckedIntToSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedIntToNullableSByteTest(bool useInterpreter)
        {
            foreach (int value in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCheckedIntToNullableSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedIntToShortTest(bool useInterpreter)
        {
            foreach (int value in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCheckedIntToShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedIntToNullableShortTest(bool useInterpreter)
        {
            foreach (int value in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCheckedIntToNullableShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedIntToUIntTest(bool useInterpreter)
        {
            foreach (int value in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCheckedIntToUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedIntToNullableUIntTest(bool useInterpreter)
        {
            foreach (int value in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCheckedIntToNullableUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedIntToULongTest(bool useInterpreter)
        {
            foreach (int value in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCheckedIntToULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedIntToNullableULongTest(bool useInterpreter)
        {
            foreach (int value in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCheckedIntToNullableULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedIntToUShortTest(bool useInterpreter)
        {
            foreach (int value in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCheckedIntToUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedIntToNullableUShortTest(bool useInterpreter)
        {
            foreach (int value in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCheckedIntToNullableUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableIntToByteTest(bool useInterpreter)
        {
            foreach (int? value in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCheckedNullableIntToByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableIntToNullableByteTest(bool useInterpreter)
        {
            foreach (int? value in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCheckedNullableIntToNullableByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableIntToCharTest(bool useInterpreter)
        {
            foreach (int? value in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCheckedNullableIntToChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableIntToNullableCharTest(bool useInterpreter)
        {
            foreach (int? value in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCheckedNullableIntToNullableChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableIntToDecimalTest(bool useInterpreter)
        {
            foreach (int? value in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCheckedNullableIntToDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableIntToNullableDecimalTest(bool useInterpreter)
        {
            foreach (int? value in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCheckedNullableIntToNullableDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableIntToDoubleTest(bool useInterpreter)
        {
            foreach (int? value in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCheckedNullableIntToDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableIntToNullableDoubleTest(bool useInterpreter)
        {
            foreach (int? value in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCheckedNullableIntToNullableDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableIntToEnumTest(bool useInterpreter)
        {
            foreach (int? value in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCheckedNullableIntToEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableIntToNullableEnumTest(bool useInterpreter)
        {
            foreach (int? value in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCheckedNullableIntToNullableEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableIntToEnumLongTest(bool useInterpreter)
        {
            foreach (int? value in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCheckedNullableIntToEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableIntToNullableEnumLongTest(bool useInterpreter)
        {
            foreach (int? value in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCheckedNullableIntToNullableEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableIntToFloatTest(bool useInterpreter)
        {
            foreach (int? value in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCheckedNullableIntToFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableIntToNullableFloatTest(bool useInterpreter)
        {
            foreach (int? value in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCheckedNullableIntToNullableFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableIntToIntTest(bool useInterpreter)
        {
            foreach (int? value in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCheckedNullableIntToInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableIntToNullableIntTest(bool useInterpreter)
        {
            foreach (int? value in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCheckedNullableIntToNullableInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableIntToLongTest(bool useInterpreter)
        {
            foreach (int? value in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCheckedNullableIntToLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableIntToNullableLongTest(bool useInterpreter)
        {
            foreach (int? value in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCheckedNullableIntToNullableLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableIntToSByteTest(bool useInterpreter)
        {
            foreach (int? value in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCheckedNullableIntToSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableIntToNullableSByteTest(bool useInterpreter)
        {
            foreach (int? value in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCheckedNullableIntToNullableSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableIntToShortTest(bool useInterpreter)
        {
            foreach (int? value in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCheckedNullableIntToShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableIntToNullableShortTest(bool useInterpreter)
        {
            foreach (int? value in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCheckedNullableIntToNullableShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableIntToUIntTest(bool useInterpreter)
        {
            foreach (int? value in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCheckedNullableIntToUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableIntToNullableUIntTest(bool useInterpreter)
        {
            foreach (int? value in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCheckedNullableIntToNullableUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableIntToULongTest(bool useInterpreter)
        {
            foreach (int? value in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCheckedNullableIntToULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableIntToNullableULongTest(bool useInterpreter)
        {
            foreach (int? value in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCheckedNullableIntToNullableULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableIntToUShortTest(bool useInterpreter)
        {
            foreach (int? value in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCheckedNullableIntToUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableIntToNullableUShortTest(bool useInterpreter)
        {
            foreach (int? value in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyCheckedNullableIntToNullableUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedLongToByteTest(bool useInterpreter)
        {
            foreach (long value in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCheckedLongToByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedLongToNullableByteTest(bool useInterpreter)
        {
            foreach (long value in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCheckedLongToNullableByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedLongToCharTest(bool useInterpreter)
        {
            foreach (long value in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCheckedLongToChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedLongToNullableCharTest(bool useInterpreter)
        {
            foreach (long value in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCheckedLongToNullableChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedLongToDecimalTest(bool useInterpreter)
        {
            foreach (long value in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCheckedLongToDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedLongToNullableDecimalTest(bool useInterpreter)
        {
            foreach (long value in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCheckedLongToNullableDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedLongToDoubleTest(bool useInterpreter)
        {
            foreach (long value in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCheckedLongToDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedLongToNullableDoubleTest(bool useInterpreter)
        {
            foreach (long value in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCheckedLongToNullableDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedLongToEnumTest(bool useInterpreter)
        {
            foreach (long value in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCheckedLongToEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedLongToNullableEnumTest(bool useInterpreter)
        {
            foreach (long value in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCheckedLongToNullableEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedLongToEnumLongTest(bool useInterpreter)
        {
            foreach (long value in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCheckedLongToEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedLongToNullableEnumLongTest(bool useInterpreter)
        {
            foreach (long value in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCheckedLongToNullableEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedLongToFloatTest(bool useInterpreter)
        {
            foreach (long value in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCheckedLongToFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedLongToNullableFloatTest(bool useInterpreter)
        {
            foreach (long value in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCheckedLongToNullableFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedLongToIntTest(bool useInterpreter)
        {
            foreach (long value in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCheckedLongToInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedLongToNullableIntTest(bool useInterpreter)
        {
            foreach (long value in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCheckedLongToNullableInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedLongToLongTest(bool useInterpreter)
        {
            foreach (long value in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCheckedLongToLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedLongToNullableLongTest(bool useInterpreter)
        {
            foreach (long value in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCheckedLongToNullableLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedLongToSByteTest(bool useInterpreter)
        {
            foreach (long value in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCheckedLongToSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedLongToNullableSByteTest(bool useInterpreter)
        {
            foreach (long value in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCheckedLongToNullableSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedLongToShortTest(bool useInterpreter)
        {
            foreach (long value in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCheckedLongToShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedLongToNullableShortTest(bool useInterpreter)
        {
            foreach (long value in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCheckedLongToNullableShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedLongToUIntTest(bool useInterpreter)
        {
            foreach (long value in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCheckedLongToUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedLongToNullableUIntTest(bool useInterpreter)
        {
            foreach (long value in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCheckedLongToNullableUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedLongToULongTest(bool useInterpreter)
        {
            foreach (long value in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCheckedLongToULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedLongToNullableULongTest(bool useInterpreter)
        {
            foreach (long value in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCheckedLongToNullableULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedLongToUShortTest(bool useInterpreter)
        {
            foreach (long value in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCheckedLongToUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedLongToNullableUShortTest(bool useInterpreter)
        {
            foreach (long value in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCheckedLongToNullableUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableLongToByteTest(bool useInterpreter)
        {
            foreach (long? value in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCheckedNullableLongToByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableLongToNullableByteTest(bool useInterpreter)
        {
            foreach (long? value in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCheckedNullableLongToNullableByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableLongToCharTest(bool useInterpreter)
        {
            foreach (long? value in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCheckedNullableLongToChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableLongToNullableCharTest(bool useInterpreter)
        {
            foreach (long? value in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCheckedNullableLongToNullableChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableLongToDecimalTest(bool useInterpreter)
        {
            foreach (long? value in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCheckedNullableLongToDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableLongToNullableDecimalTest(bool useInterpreter)
        {
            foreach (long? value in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCheckedNullableLongToNullableDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableLongToDoubleTest(bool useInterpreter)
        {
            foreach (long? value in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCheckedNullableLongToDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableLongToNullableDoubleTest(bool useInterpreter)
        {
            foreach (long? value in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCheckedNullableLongToNullableDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableLongToEnumTest(bool useInterpreter)
        {
            foreach (long? value in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCheckedNullableLongToEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableLongToNullableEnumTest(bool useInterpreter)
        {
            foreach (long? value in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCheckedNullableLongToNullableEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableLongToEnumLongTest(bool useInterpreter)
        {
            foreach (long? value in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCheckedNullableLongToEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableLongToNullableEnumLongTest(bool useInterpreter)
        {
            foreach (long? value in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCheckedNullableLongToNullableEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableLongToFloatTest(bool useInterpreter)
        {
            foreach (long? value in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCheckedNullableLongToFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableLongToNullableFloatTest(bool useInterpreter)
        {
            foreach (long? value in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCheckedNullableLongToNullableFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableLongToIntTest(bool useInterpreter)
        {
            foreach (long? value in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCheckedNullableLongToInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableLongToNullableIntTest(bool useInterpreter)
        {
            foreach (long? value in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCheckedNullableLongToNullableInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableLongToLongTest(bool useInterpreter)
        {
            foreach (long? value in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCheckedNullableLongToLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableLongToNullableLongTest(bool useInterpreter)
        {
            foreach (long? value in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCheckedNullableLongToNullableLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableLongToSByteTest(bool useInterpreter)
        {
            foreach (long? value in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCheckedNullableLongToSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableLongToNullableSByteTest(bool useInterpreter)
        {
            foreach (long? value in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCheckedNullableLongToNullableSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableLongToShortTest(bool useInterpreter)
        {
            foreach (long? value in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCheckedNullableLongToShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableLongToNullableShortTest(bool useInterpreter)
        {
            foreach (long? value in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCheckedNullableLongToNullableShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableLongToUIntTest(bool useInterpreter)
        {
            foreach (long? value in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCheckedNullableLongToUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableLongToNullableUIntTest(bool useInterpreter)
        {
            foreach (long? value in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCheckedNullableLongToNullableUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableLongToULongTest(bool useInterpreter)
        {
            foreach (long? value in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCheckedNullableLongToULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableLongToNullableULongTest(bool useInterpreter)
        {
            foreach (long? value in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCheckedNullableLongToNullableULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableLongToUShortTest(bool useInterpreter)
        {
            foreach (long? value in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCheckedNullableLongToUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableLongToNullableUShortTest(bool useInterpreter)
        {
            foreach (long? value in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyCheckedNullableLongToNullableUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedSByteToByteTest(bool useInterpreter)
        {
            foreach (sbyte value in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCheckedSByteToByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedSByteToNullableByteTest(bool useInterpreter)
        {
            foreach (sbyte value in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCheckedSByteToNullableByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedSByteToCharTest(bool useInterpreter)
        {
            foreach (sbyte value in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCheckedSByteToChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedSByteToNullableCharTest(bool useInterpreter)
        {
            foreach (sbyte value in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCheckedSByteToNullableChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedSByteToDecimalTest(bool useInterpreter)
        {
            foreach (sbyte value in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCheckedSByteToDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedSByteToNullableDecimalTest(bool useInterpreter)
        {
            foreach (sbyte value in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCheckedSByteToNullableDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedSByteToDoubleTest(bool useInterpreter)
        {
            foreach (sbyte value in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCheckedSByteToDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedSByteToNullableDoubleTest(bool useInterpreter)
        {
            foreach (sbyte value in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCheckedSByteToNullableDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedSByteToEnumTest(bool useInterpreter)
        {
            foreach (sbyte value in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCheckedSByteToEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedSByteToNullableEnumTest(bool useInterpreter)
        {
            foreach (sbyte value in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCheckedSByteToNullableEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedSByteToEnumLongTest(bool useInterpreter)
        {
            foreach (sbyte value in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCheckedSByteToEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedSByteToNullableEnumLongTest(bool useInterpreter)
        {
            foreach (sbyte value in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCheckedSByteToNullableEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedSByteToFloatTest(bool useInterpreter)
        {
            foreach (sbyte value in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCheckedSByteToFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedSByteToNullableFloatTest(bool useInterpreter)
        {
            foreach (sbyte value in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCheckedSByteToNullableFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedSByteToIntTest(bool useInterpreter)
        {
            foreach (sbyte value in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCheckedSByteToInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedSByteToNullableIntTest(bool useInterpreter)
        {
            foreach (sbyte value in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCheckedSByteToNullableInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedSByteToLongTest(bool useInterpreter)
        {
            foreach (sbyte value in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCheckedSByteToLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedSByteToNullableLongTest(bool useInterpreter)
        {
            foreach (sbyte value in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCheckedSByteToNullableLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedSByteToSByteTest(bool useInterpreter)
        {
            foreach (sbyte value in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCheckedSByteToSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedSByteToNullableSByteTest(bool useInterpreter)
        {
            foreach (sbyte value in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCheckedSByteToNullableSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedSByteToShortTest(bool useInterpreter)
        {
            foreach (sbyte value in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCheckedSByteToShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedSByteToNullableShortTest(bool useInterpreter)
        {
            foreach (sbyte value in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCheckedSByteToNullableShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedSByteToUIntTest(bool useInterpreter)
        {
            foreach (sbyte value in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCheckedSByteToUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedSByteToNullableUIntTest(bool useInterpreter)
        {
            foreach (sbyte value in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCheckedSByteToNullableUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedSByteToULongTest(bool useInterpreter)
        {
            foreach (sbyte value in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCheckedSByteToULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedSByteToNullableULongTest(bool useInterpreter)
        {
            foreach (sbyte value in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCheckedSByteToNullableULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedSByteToUShortTest(bool useInterpreter)
        {
            foreach (sbyte value in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCheckedSByteToUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedSByteToNullableUShortTest(bool useInterpreter)
        {
            foreach (sbyte value in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCheckedSByteToNullableUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableSByteToByteTest(bool useInterpreter)
        {
            foreach (sbyte? value in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCheckedNullableSByteToByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableSByteToNullableByteTest(bool useInterpreter)
        {
            foreach (sbyte? value in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCheckedNullableSByteToNullableByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableSByteToCharTest(bool useInterpreter)
        {
            foreach (sbyte? value in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCheckedNullableSByteToChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableSByteToNullableCharTest(bool useInterpreter)
        {
            foreach (sbyte? value in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCheckedNullableSByteToNullableChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableSByteToDecimalTest(bool useInterpreter)
        {
            foreach (sbyte? value in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCheckedNullableSByteToDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableSByteToNullableDecimalTest(bool useInterpreter)
        {
            foreach (sbyte? value in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCheckedNullableSByteToNullableDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableSByteToDoubleTest(bool useInterpreter)
        {
            foreach (sbyte? value in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCheckedNullableSByteToDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableSByteToNullableDoubleTest(bool useInterpreter)
        {
            foreach (sbyte? value in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCheckedNullableSByteToNullableDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableSByteToEnumTest(bool useInterpreter)
        {
            foreach (sbyte? value in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCheckedNullableSByteToEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableSByteToNullableEnumTest(bool useInterpreter)
        {
            foreach (sbyte? value in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCheckedNullableSByteToNullableEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableSByteToEnumLongTest(bool useInterpreter)
        {
            foreach (sbyte? value in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCheckedNullableSByteToEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableSByteToNullableEnumLongTest(bool useInterpreter)
        {
            foreach (sbyte? value in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCheckedNullableSByteToNullableEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableSByteToFloatTest(bool useInterpreter)
        {
            foreach (sbyte? value in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCheckedNullableSByteToFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableSByteToNullableFloatTest(bool useInterpreter)
        {
            foreach (sbyte? value in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCheckedNullableSByteToNullableFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableSByteToIntTest(bool useInterpreter)
        {
            foreach (sbyte? value in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCheckedNullableSByteToInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableSByteToNullableIntTest(bool useInterpreter)
        {
            foreach (sbyte? value in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCheckedNullableSByteToNullableInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableSByteToLongTest(bool useInterpreter)
        {
            foreach (sbyte? value in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCheckedNullableSByteToLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableSByteToNullableLongTest(bool useInterpreter)
        {
            foreach (sbyte? value in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCheckedNullableSByteToNullableLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableSByteToSByteTest(bool useInterpreter)
        {
            foreach (sbyte? value in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCheckedNullableSByteToSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableSByteToNullableSByteTest(bool useInterpreter)
        {
            foreach (sbyte? value in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCheckedNullableSByteToNullableSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableSByteToShortTest(bool useInterpreter)
        {
            foreach (sbyte? value in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCheckedNullableSByteToShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableSByteToNullableShortTest(bool useInterpreter)
        {
            foreach (sbyte? value in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCheckedNullableSByteToNullableShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableSByteToUIntTest(bool useInterpreter)
        {
            foreach (sbyte? value in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCheckedNullableSByteToUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableSByteToNullableUIntTest(bool useInterpreter)
        {
            foreach (sbyte? value in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCheckedNullableSByteToNullableUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableSByteToULongTest(bool useInterpreter)
        {
            foreach (sbyte? value in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCheckedNullableSByteToULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableSByteToNullableULongTest(bool useInterpreter)
        {
            foreach (sbyte? value in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCheckedNullableSByteToNullableULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableSByteToUShortTest(bool useInterpreter)
        {
            foreach (sbyte? value in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCheckedNullableSByteToUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableSByteToNullableUShortTest(bool useInterpreter)
        {
            foreach (sbyte? value in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyCheckedNullableSByteToNullableUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedShortToByteTest(bool useInterpreter)
        {
            foreach (short value in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCheckedShortToByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedShortToNullableByteTest(bool useInterpreter)
        {
            foreach (short value in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCheckedShortToNullableByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedShortToCharTest(bool useInterpreter)
        {
            foreach (short value in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCheckedShortToChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedShortToNullableCharTest(bool useInterpreter)
        {
            foreach (short value in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCheckedShortToNullableChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedShortToDecimalTest(bool useInterpreter)
        {
            foreach (short value in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCheckedShortToDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedShortToNullableDecimalTest(bool useInterpreter)
        {
            foreach (short value in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCheckedShortToNullableDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedShortToDoubleTest(bool useInterpreter)
        {
            foreach (short value in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCheckedShortToDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedShortToNullableDoubleTest(bool useInterpreter)
        {
            foreach (short value in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCheckedShortToNullableDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedShortToEnumTest(bool useInterpreter)
        {
            foreach (short value in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCheckedShortToEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedShortToNullableEnumTest(bool useInterpreter)
        {
            foreach (short value in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCheckedShortToNullableEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedShortToEnumLongTest(bool useInterpreter)
        {
            foreach (short value in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCheckedShortToEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedShortToNullableEnumLongTest(bool useInterpreter)
        {
            foreach (short value in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCheckedShortToNullableEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedShortToFloatTest(bool useInterpreter)
        {
            foreach (short value in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCheckedShortToFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedShortToNullableFloatTest(bool useInterpreter)
        {
            foreach (short value in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCheckedShortToNullableFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedShortToIntTest(bool useInterpreter)
        {
            foreach (short value in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCheckedShortToInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedShortToNullableIntTest(bool useInterpreter)
        {
            foreach (short value in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCheckedShortToNullableInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedShortToLongTest(bool useInterpreter)
        {
            foreach (short value in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCheckedShortToLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedShortToNullableLongTest(bool useInterpreter)
        {
            foreach (short value in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCheckedShortToNullableLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedShortToSByteTest(bool useInterpreter)
        {
            foreach (short value in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCheckedShortToSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedShortToNullableSByteTest(bool useInterpreter)
        {
            foreach (short value in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCheckedShortToNullableSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedShortToShortTest(bool useInterpreter)
        {
            foreach (short value in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCheckedShortToShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedShortToNullableShortTest(bool useInterpreter)
        {
            foreach (short value in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCheckedShortToNullableShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedShortToUIntTest(bool useInterpreter)
        {
            foreach (short value in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCheckedShortToUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedShortToNullableUIntTest(bool useInterpreter)
        {
            foreach (short value in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCheckedShortToNullableUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedShortToULongTest(bool useInterpreter)
        {
            foreach (short value in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCheckedShortToULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedShortToNullableULongTest(bool useInterpreter)
        {
            foreach (short value in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCheckedShortToNullableULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedShortToUShortTest(bool useInterpreter)
        {
            foreach (short value in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCheckedShortToUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedShortToNullableUShortTest(bool useInterpreter)
        {
            foreach (short value in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCheckedShortToNullableUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableShortToByteTest(bool useInterpreter)
        {
            foreach (short? value in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCheckedNullableShortToByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableShortToNullableByteTest(bool useInterpreter)
        {
            foreach (short? value in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCheckedNullableShortToNullableByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableShortToCharTest(bool useInterpreter)
        {
            foreach (short? value in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCheckedNullableShortToChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableShortToNullableCharTest(bool useInterpreter)
        {
            foreach (short? value in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCheckedNullableShortToNullableChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableShortToDecimalTest(bool useInterpreter)
        {
            foreach (short? value in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCheckedNullableShortToDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableShortToNullableDecimalTest(bool useInterpreter)
        {
            foreach (short? value in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCheckedNullableShortToNullableDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableShortToDoubleTest(bool useInterpreter)
        {
            foreach (short? value in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCheckedNullableShortToDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableShortToNullableDoubleTest(bool useInterpreter)
        {
            foreach (short? value in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCheckedNullableShortToNullableDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableShortToEnumTest(bool useInterpreter)
        {
            foreach (short? value in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCheckedNullableShortToEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableShortToNullableEnumTest(bool useInterpreter)
        {
            foreach (short? value in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCheckedNullableShortToNullableEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableShortToEnumLongTest(bool useInterpreter)
        {
            foreach (short? value in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCheckedNullableShortToEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableShortToNullableEnumLongTest(bool useInterpreter)
        {
            foreach (short? value in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCheckedNullableShortToNullableEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableShortToFloatTest(bool useInterpreter)
        {
            foreach (short? value in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCheckedNullableShortToFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableShortToNullableFloatTest(bool useInterpreter)
        {
            foreach (short? value in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCheckedNullableShortToNullableFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableShortToIntTest(bool useInterpreter)
        {
            foreach (short? value in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCheckedNullableShortToInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableShortToNullableIntTest(bool useInterpreter)
        {
            foreach (short? value in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCheckedNullableShortToNullableInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableShortToLongTest(bool useInterpreter)
        {
            foreach (short? value in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCheckedNullableShortToLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableShortToNullableLongTest(bool useInterpreter)
        {
            foreach (short? value in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCheckedNullableShortToNullableLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableShortToSByteTest(bool useInterpreter)
        {
            foreach (short? value in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCheckedNullableShortToSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableShortToNullableSByteTest(bool useInterpreter)
        {
            foreach (short? value in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCheckedNullableShortToNullableSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableShortToShortTest(bool useInterpreter)
        {
            foreach (short? value in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCheckedNullableShortToShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableShortToNullableShortTest(bool useInterpreter)
        {
            foreach (short? value in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCheckedNullableShortToNullableShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableShortToUIntTest(bool useInterpreter)
        {
            foreach (short? value in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCheckedNullableShortToUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableShortToNullableUIntTest(bool useInterpreter)
        {
            foreach (short? value in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCheckedNullableShortToNullableUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableShortToULongTest(bool useInterpreter)
        {
            foreach (short? value in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCheckedNullableShortToULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableShortToNullableULongTest(bool useInterpreter)
        {
            foreach (short? value in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCheckedNullableShortToNullableULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableShortToUShortTest(bool useInterpreter)
        {
            foreach (short? value in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCheckedNullableShortToUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableShortToNullableUShortTest(bool useInterpreter)
        {
            foreach (short? value in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyCheckedNullableShortToNullableUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedUIntToByteTest(bool useInterpreter)
        {
            foreach (uint value in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyCheckedUIntToByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedUIntToNullableByteTest(bool useInterpreter)
        {
            foreach (uint value in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyCheckedUIntToNullableByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedUIntToCharTest(bool useInterpreter)
        {
            foreach (uint value in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyCheckedUIntToChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedUIntToNullableCharTest(bool useInterpreter)
        {
            foreach (uint value in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyCheckedUIntToNullableChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedUIntToDecimalTest(bool useInterpreter)
        {
            foreach (uint value in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyCheckedUIntToDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedUIntToNullableDecimalTest(bool useInterpreter)
        {
            foreach (uint value in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyCheckedUIntToNullableDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedUIntToDoubleTest(bool useInterpreter)
        {
            foreach (uint value in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyCheckedUIntToDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedUIntToNullableDoubleTest(bool useInterpreter)
        {
            foreach (uint value in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyCheckedUIntToNullableDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedUIntToEnumTest(bool useInterpreter)
        {
            foreach (uint value in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyCheckedUIntToEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedUIntToNullableEnumTest(bool useInterpreter)
        {
            foreach (uint value in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyCheckedUIntToNullableEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedUIntToEnumLongTest(bool useInterpreter)
        {
            foreach (uint value in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyCheckedUIntToEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedUIntToNullableEnumLongTest(bool useInterpreter)
        {
            foreach (uint value in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyCheckedUIntToNullableEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedUIntToFloatTest(bool useInterpreter)
        {
            foreach (uint value in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyCheckedUIntToFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedUIntToNullableFloatTest(bool useInterpreter)
        {
            foreach (uint value in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyCheckedUIntToNullableFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedUIntToIntTest(bool useInterpreter)
        {
            foreach (uint value in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyCheckedUIntToInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedUIntToNullableIntTest(bool useInterpreter)
        {
            foreach (uint value in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyCheckedUIntToNullableInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedUIntToLongTest(bool useInterpreter)
        {
            foreach (uint value in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyCheckedUIntToLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedUIntToNullableLongTest(bool useInterpreter)
        {
            foreach (uint value in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyCheckedUIntToNullableLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedUIntToSByteTest(bool useInterpreter)
        {
            foreach (uint value in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyCheckedUIntToSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedUIntToNullableSByteTest(bool useInterpreter)
        {
            foreach (uint value in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyCheckedUIntToNullableSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedUIntToShortTest(bool useInterpreter)
        {
            foreach (uint value in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyCheckedUIntToShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedUIntToNullableShortTest(bool useInterpreter)
        {
            foreach (uint value in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyCheckedUIntToNullableShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedUIntToUIntTest(bool useInterpreter)
        {
            foreach (uint value in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyCheckedUIntToUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedUIntToNullableUIntTest(bool useInterpreter)
        {
            foreach (uint value in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyCheckedUIntToNullableUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedUIntToULongTest(bool useInterpreter)
        {
            foreach (uint value in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyCheckedUIntToULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedUIntToNullableULongTest(bool useInterpreter)
        {
            foreach (uint value in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyCheckedUIntToNullableULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedUIntToUShortTest(bool useInterpreter)
        {
            foreach (uint value in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyCheckedUIntToUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedUIntToNullableUShortTest(bool useInterpreter)
        {
            foreach (uint value in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyCheckedUIntToNullableUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableUIntToByteTest(bool useInterpreter)
        {
            foreach (uint? value in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyCheckedNullableUIntToByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableUIntToNullableByteTest(bool useInterpreter)
        {
            foreach (uint? value in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyCheckedNullableUIntToNullableByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableUIntToCharTest(bool useInterpreter)
        {
            foreach (uint? value in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyCheckedNullableUIntToChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableUIntToNullableCharTest(bool useInterpreter)
        {
            foreach (uint? value in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyCheckedNullableUIntToNullableChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableUIntToDecimalTest(bool useInterpreter)
        {
            foreach (uint? value in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyCheckedNullableUIntToDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableUIntToNullableDecimalTest(bool useInterpreter)
        {
            foreach (uint? value in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyCheckedNullableUIntToNullableDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableUIntToDoubleTest(bool useInterpreter)
        {
            foreach (uint? value in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyCheckedNullableUIntToDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableUIntToNullableDoubleTest(bool useInterpreter)
        {
            foreach (uint? value in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyCheckedNullableUIntToNullableDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableUIntToEnumTest(bool useInterpreter)
        {
            foreach (uint? value in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyCheckedNullableUIntToEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableUIntToNullableEnumTest(bool useInterpreter)
        {
            foreach (uint? value in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyCheckedNullableUIntToNullableEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableUIntToEnumLongTest(bool useInterpreter)
        {
            foreach (uint? value in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyCheckedNullableUIntToEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableUIntToNullableEnumLongTest(bool useInterpreter)
        {
            foreach (uint? value in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyCheckedNullableUIntToNullableEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableUIntToFloatTest(bool useInterpreter)
        {
            foreach (uint? value in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyCheckedNullableUIntToFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableUIntToNullableFloatTest(bool useInterpreter)
        {
            foreach (uint? value in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyCheckedNullableUIntToNullableFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableUIntToIntTest(bool useInterpreter)
        {
            foreach (uint? value in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyCheckedNullableUIntToInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableUIntToNullableIntTest(bool useInterpreter)
        {
            foreach (uint? value in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyCheckedNullableUIntToNullableInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableUIntToLongTest(bool useInterpreter)
        {
            foreach (uint? value in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyCheckedNullableUIntToLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableUIntToNullableLongTest(bool useInterpreter)
        {
            foreach (uint? value in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyCheckedNullableUIntToNullableLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableUIntToSByteTest(bool useInterpreter)
        {
            foreach (uint? value in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyCheckedNullableUIntToSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableUIntToNullableSByteTest(bool useInterpreter)
        {
            foreach (uint? value in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyCheckedNullableUIntToNullableSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableUIntToShortTest(bool useInterpreter)
        {
            foreach (uint? value in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyCheckedNullableUIntToShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableUIntToNullableShortTest(bool useInterpreter)
        {
            foreach (uint? value in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyCheckedNullableUIntToNullableShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableUIntToUIntTest(bool useInterpreter)
        {
            foreach (uint? value in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyCheckedNullableUIntToUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableUIntToNullableUIntTest(bool useInterpreter)
        {
            foreach (uint? value in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyCheckedNullableUIntToNullableUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableUIntToULongTest(bool useInterpreter)
        {
            foreach (uint? value in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyCheckedNullableUIntToULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableUIntToNullableULongTest(bool useInterpreter)
        {
            foreach (uint? value in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyCheckedNullableUIntToNullableULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableUIntToUShortTest(bool useInterpreter)
        {
            foreach (uint? value in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyCheckedNullableUIntToUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableUIntToNullableUShortTest(bool useInterpreter)
        {
            foreach (uint? value in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyCheckedNullableUIntToNullableUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedULongToByteTest(bool useInterpreter)
        {
            foreach (ulong value in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyCheckedULongToByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedULongToNullableByteTest(bool useInterpreter)
        {
            foreach (ulong value in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyCheckedULongToNullableByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedULongToCharTest(bool useInterpreter)
        {
            foreach (ulong value in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyCheckedULongToChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedULongToNullableCharTest(bool useInterpreter)
        {
            foreach (ulong value in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyCheckedULongToNullableChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedULongToDecimalTest(bool useInterpreter)
        {
            foreach (ulong value in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyCheckedULongToDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedULongToNullableDecimalTest(bool useInterpreter)
        {
            foreach (ulong value in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyCheckedULongToNullableDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedULongToDoubleTest(bool useInterpreter)
        {
            foreach (ulong value in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyCheckedULongToDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedULongToNullableDoubleTest(bool useInterpreter)
        {
            foreach (ulong value in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyCheckedULongToNullableDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedULongToEnumTest(bool useInterpreter)
        {
            foreach (ulong value in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyCheckedULongToEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedULongToNullableEnumTest(bool useInterpreter)
        {
            foreach (ulong value in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyCheckedULongToNullableEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedULongToEnumLongTest(bool useInterpreter)
        {
            foreach (ulong value in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyCheckedULongToEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedULongToNullableEnumLongTest(bool useInterpreter)
        {
            foreach (ulong value in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyCheckedULongToNullableEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedULongToFloatTest(bool useInterpreter)
        {
            foreach (ulong value in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyCheckedULongToFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedULongToNullableFloatTest(bool useInterpreter)
        {
            foreach (ulong value in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyCheckedULongToNullableFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedULongToIntTest(bool useInterpreter)
        {
            foreach (ulong value in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyCheckedULongToInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedULongToNullableIntTest(bool useInterpreter)
        {
            foreach (ulong value in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyCheckedULongToNullableInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedULongToLongTest(bool useInterpreter)
        {
            foreach (ulong value in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyCheckedULongToLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedULongToNullableLongTest(bool useInterpreter)
        {
            foreach (ulong value in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyCheckedULongToNullableLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedULongToSByteTest(bool useInterpreter)
        {
            foreach (ulong value in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyCheckedULongToSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedULongToNullableSByteTest(bool useInterpreter)
        {
            foreach (ulong value in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyCheckedULongToNullableSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedULongToShortTest(bool useInterpreter)
        {
            foreach (ulong value in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyCheckedULongToShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedULongToNullableShortTest(bool useInterpreter)
        {
            foreach (ulong value in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyCheckedULongToNullableShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedULongToUIntTest(bool useInterpreter)
        {
            foreach (ulong value in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyCheckedULongToUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedULongToNullableUIntTest(bool useInterpreter)
        {
            foreach (ulong value in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyCheckedULongToNullableUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedULongToULongTest(bool useInterpreter)
        {
            foreach (ulong value in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyCheckedULongToULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedULongToNullableULongTest(bool useInterpreter)
        {
            foreach (ulong value in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyCheckedULongToNullableULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedULongToUShortTest(bool useInterpreter)
        {
            foreach (ulong value in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyCheckedULongToUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedULongToNullableUShortTest(bool useInterpreter)
        {
            foreach (ulong value in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyCheckedULongToNullableUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableULongToByteTest(bool useInterpreter)
        {
            foreach (ulong? value in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyCheckedNullableULongToByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableULongToNullableByteTest(bool useInterpreter)
        {
            foreach (ulong? value in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyCheckedNullableULongToNullableByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableULongToCharTest(bool useInterpreter)
        {
            foreach (ulong? value in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyCheckedNullableULongToChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableULongToNullableCharTest(bool useInterpreter)
        {
            foreach (ulong? value in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyCheckedNullableULongToNullableChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableULongToDecimalTest(bool useInterpreter)
        {
            foreach (ulong? value in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyCheckedNullableULongToDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableULongToNullableDecimalTest(bool useInterpreter)
        {
            foreach (ulong? value in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyCheckedNullableULongToNullableDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableULongToDoubleTest(bool useInterpreter)
        {
            foreach (ulong? value in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyCheckedNullableULongToDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableULongToNullableDoubleTest(bool useInterpreter)
        {
            foreach (ulong? value in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyCheckedNullableULongToNullableDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableULongToEnumTest(bool useInterpreter)
        {
            foreach (ulong? value in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyCheckedNullableULongToEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableULongToNullableEnumTest(bool useInterpreter)
        {
            foreach (ulong? value in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyCheckedNullableULongToNullableEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableULongToEnumLongTest(bool useInterpreter)
        {
            foreach (ulong? value in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyCheckedNullableULongToEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableULongToNullableEnumLongTest(bool useInterpreter)
        {
            foreach (ulong? value in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyCheckedNullableULongToNullableEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableULongToFloatTest(bool useInterpreter)
        {
            foreach (ulong? value in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyCheckedNullableULongToFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableULongToNullableFloatTest(bool useInterpreter)
        {
            foreach (ulong? value in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyCheckedNullableULongToNullableFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableULongToIntTest(bool useInterpreter)
        {
            foreach (ulong? value in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyCheckedNullableULongToInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableULongToNullableIntTest(bool useInterpreter)
        {
            foreach (ulong? value in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyCheckedNullableULongToNullableInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableULongToLongTest(bool useInterpreter)
        {
            foreach (ulong? value in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyCheckedNullableULongToLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableULongToNullableLongTest(bool useInterpreter)
        {
            foreach (ulong? value in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyCheckedNullableULongToNullableLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableULongToSByteTest(bool useInterpreter)
        {
            foreach (ulong? value in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyCheckedNullableULongToSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableULongToNullableSByteTest(bool useInterpreter)
        {
            foreach (ulong? value in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyCheckedNullableULongToNullableSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableULongToShortTest(bool useInterpreter)
        {
            foreach (ulong? value in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyCheckedNullableULongToShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableULongToNullableShortTest(bool useInterpreter)
        {
            foreach (ulong? value in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyCheckedNullableULongToNullableShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableULongToUIntTest(bool useInterpreter)
        {
            foreach (ulong? value in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyCheckedNullableULongToUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableULongToNullableUIntTest(bool useInterpreter)
        {
            foreach (ulong? value in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyCheckedNullableULongToNullableUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableULongToULongTest(bool useInterpreter)
        {
            foreach (ulong? value in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyCheckedNullableULongToULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableULongToNullableULongTest(bool useInterpreter)
        {
            foreach (ulong? value in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyCheckedNullableULongToNullableULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableULongToUShortTest(bool useInterpreter)
        {
            foreach (ulong? value in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyCheckedNullableULongToUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableULongToNullableUShortTest(bool useInterpreter)
        {
            foreach (ulong? value in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyCheckedNullableULongToNullableUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedUShortToByteTest(bool useInterpreter)
        {
            foreach (ushort value in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyCheckedUShortToByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedUShortToNullableByteTest(bool useInterpreter)
        {
            foreach (ushort value in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyCheckedUShortToNullableByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedUShortToCharTest(bool useInterpreter)
        {
            foreach (ushort value in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyCheckedUShortToChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedUShortToNullableCharTest(bool useInterpreter)
        {
            foreach (ushort value in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyCheckedUShortToNullableChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedUShortToDecimalTest(bool useInterpreter)
        {
            foreach (ushort value in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyCheckedUShortToDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedUShortToNullableDecimalTest(bool useInterpreter)
        {
            foreach (ushort value in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyCheckedUShortToNullableDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedUShortToDoubleTest(bool useInterpreter)
        {
            foreach (ushort value in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyCheckedUShortToDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedUShortToNullableDoubleTest(bool useInterpreter)
        {
            foreach (ushort value in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyCheckedUShortToNullableDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedUShortToEnumTest(bool useInterpreter)
        {
            foreach (ushort value in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyCheckedUShortToEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedUShortToNullableEnumTest(bool useInterpreter)
        {
            foreach (ushort value in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyCheckedUShortToNullableEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedUShortToEnumLongTest(bool useInterpreter)
        {
            foreach (ushort value in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyCheckedUShortToEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedUShortToNullableEnumLongTest(bool useInterpreter)
        {
            foreach (ushort value in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyCheckedUShortToNullableEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedUShortToFloatTest(bool useInterpreter)
        {
            foreach (ushort value in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyCheckedUShortToFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedUShortToNullableFloatTest(bool useInterpreter)
        {
            foreach (ushort value in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyCheckedUShortToNullableFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedUShortToIntTest(bool useInterpreter)
        {
            foreach (ushort value in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyCheckedUShortToInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedUShortToNullableIntTest(bool useInterpreter)
        {
            foreach (ushort value in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyCheckedUShortToNullableInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedUShortToLongTest(bool useInterpreter)
        {
            foreach (ushort value in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyCheckedUShortToLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedUShortToNullableLongTest(bool useInterpreter)
        {
            foreach (ushort value in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyCheckedUShortToNullableLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedUShortToSByteTest(bool useInterpreter)
        {
            foreach (ushort value in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyCheckedUShortToSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedUShortToNullableSByteTest(bool useInterpreter)
        {
            foreach (ushort value in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyCheckedUShortToNullableSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedUShortToShortTest(bool useInterpreter)
        {
            foreach (ushort value in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyCheckedUShortToShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedUShortToNullableShortTest(bool useInterpreter)
        {
            foreach (ushort value in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyCheckedUShortToNullableShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedUShortToUIntTest(bool useInterpreter)
        {
            foreach (ushort value in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyCheckedUShortToUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedUShortToNullableUIntTest(bool useInterpreter)
        {
            foreach (ushort value in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyCheckedUShortToNullableUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedUShortToULongTest(bool useInterpreter)
        {
            foreach (ushort value in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyCheckedUShortToULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedUShortToNullableULongTest(bool useInterpreter)
        {
            foreach (ushort value in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyCheckedUShortToNullableULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedUShortToUShortTest(bool useInterpreter)
        {
            foreach (ushort value in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyCheckedUShortToUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedUShortToNullableUShortTest(bool useInterpreter)
        {
            foreach (ushort value in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyCheckedUShortToNullableUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableUShortToByteTest(bool useInterpreter)
        {
            foreach (ushort? value in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyCheckedNullableUShortToByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableUShortToNullableByteTest(bool useInterpreter)
        {
            foreach (ushort? value in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyCheckedNullableUShortToNullableByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableUShortToCharTest(bool useInterpreter)
        {
            foreach (ushort? value in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyCheckedNullableUShortToChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableUShortToNullableCharTest(bool useInterpreter)
        {
            foreach (ushort? value in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyCheckedNullableUShortToNullableChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableUShortToDecimalTest(bool useInterpreter)
        {
            foreach (ushort? value in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyCheckedNullableUShortToDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableUShortToNullableDecimalTest(bool useInterpreter)
        {
            foreach (ushort? value in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyCheckedNullableUShortToNullableDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableUShortToDoubleTest(bool useInterpreter)
        {
            foreach (ushort? value in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyCheckedNullableUShortToDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableUShortToNullableDoubleTest(bool useInterpreter)
        {
            foreach (ushort? value in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyCheckedNullableUShortToNullableDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableUShortToEnumTest(bool useInterpreter)
        {
            foreach (ushort? value in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyCheckedNullableUShortToEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableUShortToNullableEnumTest(bool useInterpreter)
        {
            foreach (ushort? value in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyCheckedNullableUShortToNullableEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableUShortToEnumLongTest(bool useInterpreter)
        {
            foreach (ushort? value in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyCheckedNullableUShortToEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableUShortToNullableEnumLongTest(bool useInterpreter)
        {
            foreach (ushort? value in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyCheckedNullableUShortToNullableEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableUShortToFloatTest(bool useInterpreter)
        {
            foreach (ushort? value in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyCheckedNullableUShortToFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableUShortToNullableFloatTest(bool useInterpreter)
        {
            foreach (ushort? value in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyCheckedNullableUShortToNullableFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableUShortToIntTest(bool useInterpreter)
        {
            foreach (ushort? value in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyCheckedNullableUShortToInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableUShortToNullableIntTest(bool useInterpreter)
        {
            foreach (ushort? value in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyCheckedNullableUShortToNullableInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableUShortToLongTest(bool useInterpreter)
        {
            foreach (ushort? value in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyCheckedNullableUShortToLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableUShortToNullableLongTest(bool useInterpreter)
        {
            foreach (ushort? value in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyCheckedNullableUShortToNullableLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableUShortToSByteTest(bool useInterpreter)
        {
            foreach (ushort? value in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyCheckedNullableUShortToSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableUShortToNullableSByteTest(bool useInterpreter)
        {
            foreach (ushort? value in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyCheckedNullableUShortToNullableSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableUShortToShortTest(bool useInterpreter)
        {
            foreach (ushort? value in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyCheckedNullableUShortToShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableUShortToNullableShortTest(bool useInterpreter)
        {
            foreach (ushort? value in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyCheckedNullableUShortToNullableShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableUShortToUIntTest(bool useInterpreter)
        {
            foreach (ushort? value in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyCheckedNullableUShortToUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableUShortToNullableUIntTest(bool useInterpreter)
        {
            foreach (ushort? value in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyCheckedNullableUShortToNullableUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableUShortToULongTest(bool useInterpreter)
        {
            foreach (ushort? value in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyCheckedNullableUShortToULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableUShortToNullableULongTest(bool useInterpreter)
        {
            foreach (ushort? value in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyCheckedNullableUShortToNullableULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableUShortToUShortTest(bool useInterpreter)
        {
            foreach (ushort? value in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyCheckedNullableUShortToUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCheckedNullableUShortToNullableUShortTest(bool useInterpreter)
        {
            foreach (ushort? value in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyCheckedNullableUShortToNullableUShort(value, useInterpreter);
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyCheckedByteToByte(byte value, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(byte)), typeof(byte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedByteToNullableByte(byte value, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(byte)), typeof(byte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedByteToChar(byte value, bool useInterpreter)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(byte)), typeof(char)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile(useInterpreter);

            Assert.Equal((char)value, f());
        }

        private static void VerifyCheckedByteToNullableChar(byte value, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(byte)), typeof(char?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile(useInterpreter);

            Assert.Equal((char)value, f());
        }

        private static void VerifyCheckedByteToDecimal(byte value, bool useInterpreter)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(byte)), typeof(decimal)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedByteToNullableDecimal(byte value, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(byte)), typeof(decimal?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedByteToDouble(byte value, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(byte)), typeof(double)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedByteToNullableDouble(byte value, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(byte)), typeof(double?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedByteToEnum(byte value, bool useInterpreter)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(byte)), typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile(useInterpreter);

            Assert.Equal((E)value, f());
        }

        private static void VerifyCheckedByteToNullableEnum(byte value, bool useInterpreter)
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(byte)), typeof(E?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile(useInterpreter);

            Assert.Equal((E)value, f());
        }

        private static void VerifyCheckedByteToEnumLong(byte value, bool useInterpreter)
        {
            Expression<Func<El>> e =
                Expression.Lambda<Func<El>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(byte)), typeof(El)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El> f = e.Compile(useInterpreter);

            Assert.Equal((El)value, f());
        }

        private static void VerifyCheckedByteToNullableEnumLong(byte value, bool useInterpreter)
        {
            Expression<Func<El?>> e =
                Expression.Lambda<Func<El?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(byte)), typeof(El?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?> f = e.Compile(useInterpreter);

            Assert.Equal((El)value, f());
        }

        private static void VerifyCheckedByteToFloat(byte value, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(byte)), typeof(float)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedByteToNullableFloat(byte value, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(byte)), typeof(float?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedByteToInt(byte value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(byte)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedByteToNullableInt(byte value, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(byte)), typeof(int?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedByteToLong(byte value, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(byte)), typeof(long)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedByteToNullableLong(byte value, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(byte)), typeof(long?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedByteToSByte(byte value, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(byte)), typeof(sbyte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile(useInterpreter);

            if (value > sbyte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((sbyte)value, f());
        }

        private static void VerifyCheckedByteToNullableSByte(byte value, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(byte)), typeof(sbyte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile(useInterpreter);

            if (value > sbyte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((sbyte)value, f());
        }

        private static void VerifyCheckedByteToShort(byte value, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(byte)), typeof(short)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedByteToNullableShort(byte value, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(byte)), typeof(short?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedByteToUInt(byte value, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(byte)), typeof(uint)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedByteToNullableUInt(byte value, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(byte)), typeof(uint?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedByteToULong(byte value, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(byte)), typeof(ulong)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedByteToNullableULong(byte value, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(byte)), typeof(ulong?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedByteToUShort(byte value, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(byte)), typeof(ushort)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedByteToNullableUShort(byte value, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(byte)), typeof(ushort?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableByteToByte(byte? value, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(byte?)), typeof(byte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableByteToNullableByte(byte? value, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(byte?)), typeof(byte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableByteToChar(byte? value, bool useInterpreter)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(byte?)), typeof(char)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal((char)value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableByteToNullableChar(byte? value, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(byte?)), typeof(char?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile(useInterpreter);

            Assert.Equal((char?)value, f());
        }

        private static void VerifyCheckedNullableByteToDecimal(byte? value, bool useInterpreter)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(byte?)), typeof(decimal)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableByteToNullableDecimal(byte? value, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(byte?)), typeof(decimal?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableByteToDouble(byte? value, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(byte?)), typeof(double)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableByteToNullableDouble(byte? value, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(byte?)), typeof(double?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableByteToEnum(byte? value, bool useInterpreter)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(byte?)), typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal((E)value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableByteToNullableEnum(byte? value, bool useInterpreter)
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(byte?)), typeof(E?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile(useInterpreter);

            Assert.Equal((E?)value, f());
        }

        private static void VerifyCheckedNullableByteToEnumLong(byte? value, bool useInterpreter)
        {
            Expression<Func<El>> e =
                Expression.Lambda<Func<El>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(byte?)), typeof(El)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal((El)value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableByteToNullableEnumLong(byte? value, bool useInterpreter)
        {
            Expression<Func<El?>> e =
                Expression.Lambda<Func<El?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(byte?)), typeof(El?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?> f = e.Compile(useInterpreter);

            Assert.Equal((El?)value, f());
        }

        private static void VerifyCheckedNullableByteToFloat(byte? value, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(byte?)), typeof(float)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableByteToNullableFloat(byte? value, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(byte?)), typeof(float?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableByteToInt(byte? value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(byte?)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableByteToNullableInt(byte? value, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(byte?)), typeof(int?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableByteToLong(byte? value, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(byte?)), typeof(long)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableByteToNullableLong(byte? value, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(byte?)), typeof(long?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableByteToSByte(byte? value, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(byte?)), typeof(sbyte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                byte unboxed = value.GetValueOrDefault();
                if (unboxed > sbyte.MaxValue)
                    Assert.Throws<OverflowException>(() => f());
                else
                    Assert.Equal((sbyte)unboxed, f());
            }
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableByteToNullableSByte(byte? value, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(byte?)), typeof(sbyte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Null(f());
            else if (value > sbyte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((sbyte)value, f());
        }

        private static void VerifyCheckedNullableByteToShort(byte? value, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(byte?)), typeof(short)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableByteToNullableShort(byte? value, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(byte?)), typeof(short?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableByteToUInt(byte? value, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(byte?)), typeof(uint)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableByteToNullableUInt(byte? value, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(byte?)), typeof(uint?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableByteToULong(byte? value, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(byte?)), typeof(ulong)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableByteToNullableULong(byte? value, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(byte?)), typeof(ulong?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableByteToUShort(byte? value, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(byte?)), typeof(ushort)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableByteToNullableUShort(byte? value, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(byte?)), typeof(ushort?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedCharToByte(char value, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(char)), typeof(byte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile(useInterpreter);

            if (value > byte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((byte)value, f());
        }

        private static void VerifyCheckedCharToNullableByte(char value, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(char)), typeof(byte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile(useInterpreter);

            if (value > byte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((byte)value, f());
        }

        private static void VerifyCheckedCharToChar(char value, bool useInterpreter)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(char)), typeof(char)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedCharToNullableChar(char value, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(char)), typeof(char?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedCharToDecimal(char value, bool useInterpreter)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(char)), typeof(decimal)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedCharToNullableDecimal(char value, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(char)), typeof(decimal?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedCharToDouble(char value, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(char)), typeof(double)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedCharToNullableDouble(char value, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(char)), typeof(double?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedCharToEnum(char value, bool useInterpreter)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(char)), typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile(useInterpreter);

            Assert.Equal((E)value, f());
        }

        private static void VerifyCheckedCharToNullableEnum(char value, bool useInterpreter)
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(char)), typeof(E?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile(useInterpreter);

            Assert.Equal((E)value, f());
        }

        private static void VerifyCheckedCharToEnumLong(char value, bool useInterpreter)
        {
            Expression<Func<El>> e =
                Expression.Lambda<Func<El>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(char)), typeof(El)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El> f = e.Compile(useInterpreter);

            Assert.Equal((El)value, f());
        }

        private static void VerifyCheckedCharToNullableEnumLong(char value, bool useInterpreter)
        {
            Expression<Func<El?>> e =
                Expression.Lambda<Func<El?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(char)), typeof(El?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?> f = e.Compile(useInterpreter);

            Assert.Equal((El)value, f());
        }

        private static void VerifyCheckedCharToFloat(char value, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(char)), typeof(float)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedCharToNullableFloat(char value, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(char)), typeof(float?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedCharToInt(char value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(char)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedCharToNullableInt(char value, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(char)), typeof(int?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedCharToLong(char value, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(char)), typeof(long)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedCharToNullableLong(char value, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(char)), typeof(long?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedCharToSByte(char value, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(char)), typeof(sbyte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile(useInterpreter);

            if (value < sbyte.MinValue | value > sbyte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((sbyte)value, f());
        }

        private static void VerifyCheckedCharToNullableSByte(char value, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(char)), typeof(sbyte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile(useInterpreter);

            if (value > sbyte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((sbyte)value, f());
        }

        private static void VerifyCheckedCharToShort(char value, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(char)), typeof(short)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);

            if (value > short.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((short)value, f());
        }

        private static void VerifyCheckedCharToNullableShort(char value, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(char)), typeof(short?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);

            if (value > short.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((short)value, f());
        }

        private static void VerifyCheckedCharToUInt(char value, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(char)), typeof(uint)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedCharToNullableUInt(char value, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(char)), typeof(uint?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedCharToULong(char value, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(char)), typeof(ulong)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedCharToNullableULong(char value, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(char)), typeof(ulong?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedCharToUShort(char value, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(char)), typeof(ushort)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedCharToNullableUShort(char value, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(char)), typeof(ushort?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableCharToByte(char? value, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(char?)), typeof(byte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Throws<InvalidOperationException>(() => f());
            else if (value > byte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((byte)value.GetValueOrDefault(), f());
        }

        private static void VerifyCheckedNullableCharToNullableByte(char? value, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(char?)), typeof(byte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Null(f());
            else if (value > byte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((byte)value, f());
        }

        private static void VerifyCheckedNullableCharToChar(char? value, bool useInterpreter)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(char?)), typeof(char)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableCharToNullableChar(char? value, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(char?)), typeof(char?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableCharToDecimal(char? value, bool useInterpreter)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(char?)), typeof(decimal)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableCharToNullableDecimal(char? value, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(char?)), typeof(decimal?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableCharToDouble(char? value, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(char?)), typeof(double)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableCharToNullableDouble(char? value, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(char?)), typeof(double?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableCharToEnum(char? value, bool useInterpreter)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(char?)), typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal((E)value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableCharToNullableEnum(char? value, bool useInterpreter)
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(char?)), typeof(E?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile(useInterpreter);

            Assert.Equal((E?)value, f());
        }

        private static void VerifyCheckedNullableCharToEnumLong(char? value, bool useInterpreter)
        {
            Expression<Func<El>> e =
                Expression.Lambda<Func<El>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(char?)), typeof(El)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal((El)value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableCharToNullableEnumLong(char? value, bool useInterpreter)
        {
            Expression<Func<El?>> e =
                Expression.Lambda<Func<El?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(char?)), typeof(El?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?> f = e.Compile(useInterpreter);

            Assert.Equal((El?)value, f());
        }

        private static void VerifyCheckedNullableCharToFloat(char? value, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(char?)), typeof(float)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableCharToNullableFloat(char? value, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(char?)), typeof(float?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableCharToInt(char? value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(char?)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableCharToNullableInt(char? value, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(char?)), typeof(int?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableCharToLong(char? value, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(char?)), typeof(long)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableCharToNullableLong(char? value, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(char?)), typeof(long?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableCharToSByte(char? value, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(char?)), typeof(sbyte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                char unboxed = value.GetValueOrDefault();
                if (unboxed > sbyte.MaxValue)
                    Assert.Throws<OverflowException>(() => f());
                else
                    Assert.Equal((sbyte)unboxed, f());
            }
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableCharToNullableSByte(char? value, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(char?)), typeof(sbyte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Null(f());
            else if (value < sbyte.MinValue | value > sbyte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((sbyte)value, f());
        }

        private static void VerifyCheckedNullableCharToShort(char? value, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(char?)), typeof(short)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Throws<InvalidOperationException>(() => f());
            else if (value > short.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((short)value.GetValueOrDefault(), f());
        }

        private static void VerifyCheckedNullableCharToNullableShort(char? value, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(char?)), typeof(short?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Null(f());
            else if (value > short.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((short)value.GetValueOrDefault(), f());
        }

        private static void VerifyCheckedNullableCharToUInt(char? value, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(char?)), typeof(uint)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableCharToNullableUInt(char? value, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(char?)), typeof(uint?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableCharToULong(char? value, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(char?)), typeof(ulong)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableCharToNullableULong(char? value, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(char?)), typeof(ulong?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableCharToUShort(char? value, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(char?)), typeof(ushort)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableCharToNullableUShort(char? value, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(char?)), typeof(ushort?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedDecimalToByte(decimal value, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(decimal)), typeof(byte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile(useInterpreter);

            byte expected = 0;
            try
            {
                expected = (byte)value;
            }
            catch(OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedDecimalToNullableByte(decimal value, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(decimal)), typeof(byte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile(useInterpreter);

            byte expected = 0;
            try
            {
                expected = (byte)value;
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedDecimalToChar(decimal value, bool useInterpreter)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(decimal)), typeof(char)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile(useInterpreter);

            char expected = '\0';
            try
            {
                expected = (char)value;
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedDecimalToNullableChar(decimal value, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(decimal)), typeof(char?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile(useInterpreter);

            char expected = '\0';
            try
            {
                expected = (char)value;
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedDecimalToDecimal(decimal value, bool useInterpreter)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(decimal)), typeof(decimal)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedDecimalToNullableDecimal(decimal value, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(decimal)), typeof(decimal?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedDecimalToDouble(decimal value, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(decimal)), typeof(double)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);

            Assert.Equal((double)value, f());
        }

        private static void VerifyCheckedDecimalToNullableDouble(decimal value, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(decimal)), typeof(double?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal((double)value, f());
        }

        private static void VerifyCheckedDecimalToFloat(decimal value, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(decimal)), typeof(float)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);

            Assert.Equal((float)value, f());
        }

        private static void VerifyCheckedDecimalToNullableFloat(decimal value, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(decimal)), typeof(float?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal((float)value, f());
        }

        private static void VerifyCheckedDecimalToInt(decimal value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(decimal)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            int expected = 0;
            try
            {
                expected = (int)value;
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedDecimalToNullableInt(decimal value, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(decimal)), typeof(int?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);

            int expected = 0;
            try
            {
                expected = (int)value;
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedDecimalToLong(decimal value, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(decimal)), typeof(long)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);

            long expected = 0;
            try
            {
                expected = (long)value;
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedDecimalToNullableLong(decimal value, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(decimal)), typeof(long?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);

            long expected = 0;
            try
            {
                expected = (long)value;
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedDecimalToSByte(decimal value, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(decimal)), typeof(sbyte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile(useInterpreter);

            sbyte expected = 0;
            try
            {
                expected = (sbyte)value;
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedDecimalToNullableSByte(decimal value, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(decimal)), typeof(sbyte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile(useInterpreter);

            sbyte expected = 0;
            try
            {
                expected = (sbyte)value;
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedDecimalToShort(decimal value, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(decimal)), typeof(short)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);

            short expected = 0;
            try
            {
                expected = (short)value;
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedDecimalToNullableShort(decimal value, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(decimal)), typeof(short?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);

            short expected = 0;
            try
            {
                expected = (short)value;
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedDecimalToUInt(decimal value, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(decimal)), typeof(uint)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            uint expected = 0;
            try
            {
                expected = (uint)value;
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedDecimalToNullableUInt(decimal value, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(decimal)), typeof(uint?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);

            uint expected = 0;
            try
            {
                expected = (uint)value;
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedDecimalToULong(decimal value, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(decimal)), typeof(ulong)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);

            ulong expected = 0;
            try
            {
                expected = (ulong)value;
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedDecimalToNullableULong(decimal value, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(decimal)), typeof(ulong?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);

            ulong expected = 0;
            try
            {
                expected = (ulong)value;
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedDecimalToUShort(decimal value, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(decimal)), typeof(ushort)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);

            ushort expected = 0;
            try
            {
                expected = (ushort)value;
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedDecimalToNullableUShort(decimal value, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(decimal)), typeof(ushort?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);

            ushort expected = 0;
            try
            {
                expected = (ushort)value;
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedNullableDecimalToByte(decimal? value, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(decimal?)), typeof(byte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                byte expected = 0;
                try
                {
                    expected = (byte)value.GetValueOrDefault();
                }
                catch (OverflowException)
                {
                    Assert.Throws<OverflowException>(() => f());
                    return;
                }

                Assert.Equal(expected, f());
            }
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableDecimalToNullableByte(decimal? value, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(decimal?)), typeof(byte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                byte expected = 0;
                try
                {
                    expected = (byte)value.GetValueOrDefault();
                }
                catch (OverflowException)
                {
                    Assert.Throws<OverflowException>(() => f());
                    return;
                }

                Assert.Equal(expected, f());
            }
            else
                Assert.Null(f());
        }

        private static void VerifyCheckedNullableDecimalToChar(decimal? value, bool useInterpreter)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(decimal?)), typeof(char)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                char expected = '\0';
                try
                {
                    expected = (char)value.GetValueOrDefault();
                }
                catch (OverflowException)
                {
                    Assert.Throws<OverflowException>(() => f());
                    return;
                }

                Assert.Equal(expected, f());
            }
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableDecimalToNullableChar(decimal? value, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(decimal?)), typeof(char?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                char expected = '\0';
                try
                {
                    expected = (char)value.GetValueOrDefault();
                }
                catch (OverflowException)
                {
                    Assert.Throws<OverflowException>(() => f());
                    return;
                }

                Assert.Equal(expected, f());
            }
            else
                Assert.Null(f());
        }

        private static void VerifyCheckedNullableDecimalToDecimal(decimal? value, bool useInterpreter)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(decimal?)), typeof(decimal)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableDecimalToNullableDecimal(decimal? value, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(decimal?)), typeof(decimal?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableDecimalToDouble(decimal? value, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(decimal?)), typeof(double)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal((double?)value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableDecimalToNullableDouble(decimal? value, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(decimal?)), typeof(double?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal((double?)value, f());
        }

        private static void VerifyCheckedNullableDecimalToFloat(decimal? value, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(decimal?)), typeof(float)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal((float)value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableDecimalToNullableFloat(decimal? value, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(decimal?)), typeof(float?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal((float?)value, f());
        }

        private static void VerifyCheckedNullableDecimalToInt(decimal? value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(decimal?)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                int expected = 0;
                try
                {
                    expected = (int)value.GetValueOrDefault();
                }
                catch (OverflowException)
                {
                    Assert.Throws<OverflowException>(() => f());
                    return;
                }

                Assert.Equal(expected, f());
            }
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableDecimalToNullableInt(decimal? value, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(decimal?)), typeof(int?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                int expected = 0;
                try
                {
                    expected = (int)value.GetValueOrDefault();
                }
                catch (OverflowException)
                {
                    Assert.Throws<OverflowException>(() => f());
                    return;
                }

                Assert.Equal(expected, f());
            }
            else
                Assert.Null(f());
        }

        private static void VerifyCheckedNullableDecimalToLong(decimal? value, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(decimal?)), typeof(long)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                long expected = 0;
                try
                {
                    expected = (long)value.GetValueOrDefault();
                }
                catch (OverflowException)
                {
                    Assert.Throws<OverflowException>(() => f());
                    return;
                }

                Assert.Equal(expected, f());
            }
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableDecimalToNullableLong(decimal? value, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(decimal?)), typeof(long?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                long expected = 0;
                try
                {
                    expected = (long)value.GetValueOrDefault();
                }
                catch (OverflowException)
                {
                    Assert.Throws<OverflowException>(() => f());
                    return;
                }

                Assert.Equal(expected, f());
            }
            else
                Assert.Null(f());
        }

        private static void VerifyCheckedNullableDecimalToSByte(decimal? value, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(decimal?)), typeof(sbyte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                sbyte expected = 0;
                try
                {
                    expected = (sbyte)value.GetValueOrDefault();
                }
                catch (OverflowException)
                {
                    Assert.Throws<OverflowException>(() => f());
                    return;
                }

                Assert.Equal(expected, f());
            }
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableDecimalToNullableSByte(decimal? value, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(decimal?)), typeof(sbyte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                sbyte expected = 0;
                try
                {
                    expected = (sbyte)value.GetValueOrDefault();
                }
                catch (OverflowException)
                {
                    Assert.Throws<OverflowException>(() => f());
                    return;
                }

                Assert.Equal(expected, f());
            }
            else
                Assert.Null(f());
        }

        private static void VerifyCheckedNullableDecimalToShort(decimal? value, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(decimal?)), typeof(short)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                short expected = 0;
                try
                {
                    expected = (short)value.GetValueOrDefault();
                }
                catch (OverflowException)
                {
                    Assert.Throws<OverflowException>(() => f());
                    return;
                }

                Assert.Equal(expected, f());
            }
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableDecimalToNullableShort(decimal? value, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(decimal?)), typeof(short?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                short expected = 0;
                try
                {
                    expected = (short)value.GetValueOrDefault();
                }
                catch (OverflowException)
                {
                    Assert.Throws<OverflowException>(() => f());
                    return;
                }

                Assert.Equal(expected, f());
            }
            else
                Assert.Null(f());
        }

        private static void VerifyCheckedNullableDecimalToUInt(decimal? value, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(decimal?)), typeof(uint)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                uint expected = 0;
                try
                {
                    expected = (uint)value.GetValueOrDefault();
                }
                catch (OverflowException)
                {
                    Assert.Throws<OverflowException>(() => f());
                    return;
                }

                Assert.Equal(expected, f());
            }
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableDecimalToNullableUInt(decimal? value, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(decimal?)), typeof(uint?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                uint expected = 0;
                try
                {
                    expected = (uint)value.GetValueOrDefault();
                }
                catch (OverflowException)
                {
                    Assert.Throws<OverflowException>(() => f());
                    return;
                }

                Assert.Equal(expected, f());
            }
            else
                Assert.Null(f());
        }

        private static void VerifyCheckedNullableDecimalToULong(decimal? value, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(decimal?)), typeof(ulong)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                ulong expected = 0;
                try
                {
                    expected = (ulong)value.GetValueOrDefault();
                }
                catch (OverflowException)
                {
                    Assert.Throws<OverflowException>(() => f());
                    return;
                }

                Assert.Equal(expected, f());
            }
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableDecimalToNullableULong(decimal? value, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(decimal?)), typeof(ulong?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                ulong expected = 0;
                try
                {
                    expected = (ulong)value.GetValueOrDefault();
                }
                catch (OverflowException)
                {
                    Assert.Throws<OverflowException>(() => f());
                    return;
                }

                Assert.Equal(expected, f());
            }
            else
                Assert.Null(f());
        }

        private static void VerifyCheckedNullableDecimalToUShort(decimal? value, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(decimal?)), typeof(ushort)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                ushort expected = 0;
                try
                {
                    expected = (ushort)value.GetValueOrDefault();
                }
                catch (OverflowException)
                {
                    Assert.Throws<OverflowException>(() => f());
                    return;
                }

                Assert.Equal(expected, f());
            }
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableDecimalToNullableUShort(decimal? value, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(decimal?)), typeof(ushort?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                ushort expected = 0;
                try
                {
                    expected = (ushort)value.GetValueOrDefault();
                }
                catch (OverflowException)
                {
                    Assert.Throws<OverflowException>(() => f());
                    return;
                }

                Assert.Equal(expected, f());
            }
            else
                Assert.Null(f());
        }

        private static void VerifyCheckedDoubleToByte(double value, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(double)), typeof(byte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile(useInterpreter);

            byte expected = 0;
            try
            {
                expected = checked((byte)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedDoubleToNullableByte(double value, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(double)), typeof(byte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile(useInterpreter);

            byte expected = 0;
            try
            {
                expected = checked((byte)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedDoubleToChar(double value, bool useInterpreter)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(double)), typeof(char)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile(useInterpreter);

            char expected = '\0';
            try
            {
                expected = checked((char)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedDoubleToNullableChar(double value, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(double)), typeof(char?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile(useInterpreter);

            char expected = '\0';
            try
            {
                expected = checked((char)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedDoubleToDecimal(double value, bool useInterpreter)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(double)), typeof(decimal)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal> f = e.Compile(useInterpreter);

            decimal expected = 0;
            try
            {
                expected = (decimal)value;
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedDoubleToNullableDecimal(double value, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(double)), typeof(decimal?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal?> f = e.Compile(useInterpreter);

            decimal expected = 0;
            try
            {
                expected = (decimal)value;
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedDoubleToDouble(double value, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(double)), typeof(double)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedDoubleToNullableDouble(double value, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(double)), typeof(double?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedDoubleToEnum(double value, bool useInterpreter)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(double)), typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile(useInterpreter);

            E expected = 0;
            try
            {
                expected = checked((E)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedDoubleToNullableEnum(double value, bool useInterpreter)
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(double)), typeof(E?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile(useInterpreter);

            E expected = 0;
            try
            {
                expected = checked((E)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedDoubleToEnumLong(double value, bool useInterpreter)
        {
            Expression<Func<El>> e =
                Expression.Lambda<Func<El>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(double)), typeof(El)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El> f = e.Compile(useInterpreter);

            El expected = 0;
            try
            {
                expected = checked((El)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedDoubleToNullableEnumLong(double value, bool useInterpreter)
        {
            Expression<Func<El?>> e =
                Expression.Lambda<Func<El?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(double)), typeof(El?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?> f = e.Compile(useInterpreter);

            El expected = 0;
            try
            {
                expected = checked((El)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedDoubleToFloat(double value, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(double)), typeof(float)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);

            Assert.Equal((float)value, f());
        }

        private static void VerifyCheckedDoubleToNullableFloat(double value, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(double)), typeof(float?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal((float)value, f());
        }

        private static void VerifyCheckedDoubleToInt(double value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(double)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            int expected = 0;
            try
            {
                expected = checked((int)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedDoubleToNullableInt(double value, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(double)), typeof(int?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);

            int expected = 0;
            try
            {
                expected = checked((int)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedDoubleToLong(double value, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(double)), typeof(long)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);

            long expected = 0;
            try
            {
                expected = checked((long)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedDoubleToNullableLong(double value, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(double)), typeof(long?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);

            long expected = 0;
            try
            {
                expected = checked((long)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedDoubleToSByte(double value, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(double)), typeof(sbyte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile(useInterpreter);

            sbyte expected = 0;
            try
            {
                expected = checked((sbyte)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedDoubleToNullableSByte(double value, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(double)), typeof(sbyte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile(useInterpreter);

            sbyte expected = 0;
            try
            {
                expected = checked((sbyte)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedDoubleToShort(double value, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(double)), typeof(short)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);

            short expected = 0;
            try
            {
                expected = checked((short)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedDoubleToNullableShort(double value, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(double)), typeof(short?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);

            short expected = 0;
            try
            {
                expected = checked((short)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedDoubleToUInt(double value, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(double)), typeof(uint)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            uint expected = 0;
            try
            {
                expected = checked((uint)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedDoubleToNullableUInt(double value, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(double)), typeof(uint?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);

            uint expected = 0;
            try
            {
                expected = checked((uint)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedDoubleToULong(double value, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(double)), typeof(ulong)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);

            ulong expected = 0;
            try
            {
                expected = checked((ulong)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedDoubleToNullableULong(double value, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(double)), typeof(ulong?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);

            ulong expected = 0;
            try
            {
                expected = checked((ulong)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedDoubleToUShort(double value, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(double)), typeof(ushort)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);

            ushort expected = 0;
            try
            {
                expected = checked((ushort)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedDoubleToNullableUShort(double value, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(double)), typeof(ushort?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);

            ushort expected = 0;
            try
            {
                expected = checked((ushort)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedNullableDoubleToByte(double? value, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(double?)), typeof(byte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                byte expected = 0;
                try
                {
                    expected = checked((byte)value.GetValueOrDefault());
                }
                catch (OverflowException)
                {
                    Assert.Throws<OverflowException>(() => f());
                    return;
                }

                Assert.Equal(expected, f());
            }
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableDoubleToNullableByte(double? value, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(double?)), typeof(byte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile(useInterpreter);

            byte? expected = null;
            try
            {
                expected = checked((byte?)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedNullableDoubleToChar(double? value, bool useInterpreter)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(double?)), typeof(char)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                char expected = '\0';
                try
                {
                    expected = checked((char)value.GetValueOrDefault());
                }
                catch (OverflowException)
                {
                    Assert.Throws<OverflowException>(() => f());
                    return;
                }

                Assert.Equal(expected, f());
            }
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableDoubleToNullableChar(double? value, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(double?)), typeof(char?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile(useInterpreter);

            char? expected = null;
            try
            {
                expected = checked((char?)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedNullableDoubleToDecimal(double? value, bool useInterpreter)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(double?)), typeof(decimal)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                decimal expected = 0;
                try
                {
                    expected = checked((decimal)value.GetValueOrDefault());
                }
                catch (OverflowException)
                {
                    Assert.Throws<OverflowException>(() => f());
                    return;
                }

                Assert.Equal(expected, f());
            }
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableDoubleToNullableDecimal(double? value, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(double?)), typeof(decimal?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal?> f = e.Compile(useInterpreter);

            decimal? expected = null;
            try
            {
                expected = (decimal?)value;
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedNullableDoubleToDouble(double? value, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(double?)), typeof(double)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableDoubleToNullableDouble(double? value, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(double?)), typeof(double?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableDoubleToEnum(double? value, bool useInterpreter)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(double?)), typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                E expected = 0;
                try
                {
                    expected = checked((E)value.GetValueOrDefault());
                }
                catch (OverflowException)
                {
                    Assert.Throws<OverflowException>(() => f());
                    return;
                }

                Assert.Equal(expected, f());
            }
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableDoubleToNullableEnum(double? value, bool useInterpreter)
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(double?)), typeof(E?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile(useInterpreter);

            E? expected = null;
            try
            {
                expected = checked((E?)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedNullableDoubleToEnumLong(double? value, bool useInterpreter)
        {
            Expression<Func<El>> e =
                Expression.Lambda<Func<El>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(double?)), typeof(El)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                El expected = 0;
                try
                {
                    expected = checked((El)value.GetValueOrDefault());
                }
                catch (OverflowException)
                {
                    Assert.Throws<OverflowException>(() => f());
                    return;
                }

                Assert.Equal(expected, f());
            }
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableDoubleToNullableEnumLong(double? value, bool useInterpreter)
        {
            Expression<Func<El?>> e =
                Expression.Lambda<Func<El?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(double?)), typeof(El?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?> f = e.Compile(useInterpreter);

            El? expected = null;
            try
            {
                expected = checked((El?)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedNullableDoubleToFloat(double? value, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(double?)), typeof(float)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal((float)value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableDoubleToNullableFloat(double? value, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(double?)), typeof(float?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal((float?)value, f());
        }

        private static void VerifyCheckedNullableDoubleToInt(double? value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(double?)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                int expected = 0;
                try
                {
                    expected = checked((int)value.GetValueOrDefault());
                }
                catch (OverflowException)
                {
                    Assert.Throws<OverflowException>(() => f());
                    return;
                }

                Assert.Equal(expected, f());
            }
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableDoubleToNullableInt(double? value, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(double?)), typeof(int?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);

            int? expected = null;
            try
            {
                expected = checked((int?)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedNullableDoubleToLong(double? value, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(double?)), typeof(long)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                long expected = 0;
                try
                {
                    expected = checked((long)value.GetValueOrDefault());
                }
                catch (OverflowException)
                {
                    Assert.Throws<OverflowException>(() => f());
                    return;
                }

                Assert.Equal(expected, f());
            }
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableDoubleToNullableLong(double? value, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(double?)), typeof(long?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);

            long? expected = null;
            try
            {
                expected = checked((long?)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedNullableDoubleToSByte(double? value, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(double?)), typeof(sbyte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                sbyte expected = 0;
                try
                {
                    expected = checked((sbyte)value.GetValueOrDefault());
                }
                catch (OverflowException)
                {
                    Assert.Throws<OverflowException>(() => f());
                    return;
                }

                Assert.Equal(expected, f());
            }
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableDoubleToNullableSByte(double? value, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(double?)), typeof(sbyte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile(useInterpreter);

            sbyte? expected = null;
            try
            {
                expected = checked((sbyte?)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedNullableDoubleToShort(double? value, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(double?)), typeof(short)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                short expected = 0;
                try
                {
                    expected = checked((short)value.GetValueOrDefault());
                }
                catch (OverflowException)
                {
                    Assert.Throws<OverflowException>(() => f());
                    return;
                }

                Assert.Equal(expected, f());
            }
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableDoubleToNullableShort(double? value, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(double?)), typeof(short?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);

            short? expected = null;
            try
            {
                expected = checked((short?)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedNullableDoubleToUInt(double? value, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(double?)), typeof(uint)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                uint expected = 0;
                try
                {
                    expected = checked((uint)value.GetValueOrDefault());
                }
                catch (OverflowException)
                {
                    Assert.Throws<OverflowException>(() => f());
                    return;
                }

                Assert.Equal(expected, f());
            }
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableDoubleToNullableUInt(double? value, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(double?)), typeof(uint?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);

            uint? expected = null;
            try
            {
                expected = checked((uint?)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedNullableDoubleToULong(double? value, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(double?)), typeof(ulong)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                ulong expected = 0;
                try
                {
                    expected = checked((ulong)value.GetValueOrDefault());
                }
                catch (OverflowException)
                {
                    Assert.Throws<OverflowException>(() => f());
                    return;
                }

                Assert.Equal(expected, f());
            }
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableDoubleToNullableULong(double? value, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(double?)), typeof(ulong?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);

            ulong? expected = null;
            try
            {
                expected = checked((ulong?)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedNullableDoubleToUShort(double? value, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(double?)), typeof(ushort)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                ushort expected = 0;
                try
                {
                    expected = checked((ushort)value.GetValueOrDefault());
                }
                catch (OverflowException)
                {
                    Assert.Throws<OverflowException>(() => f());
                    return;
                }

                Assert.Equal(expected, f());
            }
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableDoubleToNullableUShort(double? value, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(double?)), typeof(ushort?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);

            ushort? expected = null;
            try
            {
                expected = checked((ushort?)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedEnumToByte(E value, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(E)), typeof(byte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile(useInterpreter);

            if (value < 0 | (int)value > byte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((byte)value, f());
        }

        private static void VerifyCheckedEnumToNullableByte(E value, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(E)), typeof(byte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile(useInterpreter);

            if (value < 0 | (int)value > byte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((byte)value, f());
        }

        private static void VerifyCheckedEnumToChar(E value, bool useInterpreter)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(E)), typeof(char)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile(useInterpreter);

            if (value < 0 | (int)value > char.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((char)value, f());
        }

        private static void VerifyCheckedEnumToNullableChar(E value, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(E)), typeof(char?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile(useInterpreter);

            if (value < (E)char.MinValue | value > (E)char.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((char)value, f());
        }

        private static void VerifyCheckedEnumToDouble(E value, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(E)), typeof(double)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);

            Assert.Equal((double)value, f());
        }

        private static void VerifyCheckedEnumToNullableDouble(E value, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(E)), typeof(double?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal((double)value, f());
        }

        private static void VerifyCheckedEnumToEnum(E value, bool useInterpreter)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(E)), typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedEnumToNullableEnum(E value, bool useInterpreter)
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(E)), typeof(E?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedEnumToEnumLong(E value, bool useInterpreter)
        {
            Expression<Func<El>> e =
                Expression.Lambda<Func<El>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(E)), typeof(El)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El> f = e.Compile(useInterpreter);

            Assert.Equal((El)value, f());
        }

        private static void VerifyCheckedEnumToNullableEnumLong(E value, bool useInterpreter)
        {
            Expression<Func<El?>> e =
                Expression.Lambda<Func<El?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(E)), typeof(El?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?> f = e.Compile(useInterpreter);

            Assert.Equal((El)value, f());
        }

        private static void VerifyCheckedEnumToFloat(E value, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(E)), typeof(float)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);

            Assert.Equal((float)value, f());
        }

        private static void VerifyCheckedEnumToNullableFloat(E value, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(E)), typeof(float?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal((float)value, f());
        }

        private static void VerifyCheckedEnumToInt(E value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(E)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            Assert.Equal((int)value, f());
        }

        private static void VerifyCheckedEnumToNullableInt(E value, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(E)), typeof(int?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);

            Assert.Equal((int)value, f());
        }

        private static void VerifyCheckedEnumToLong(E value, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(E)), typeof(long)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);

            Assert.Equal((long)value, f());
        }

        private static void VerifyCheckedEnumToNullableLong(E value, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(E)), typeof(long?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);

            Assert.Equal((long)value, f());
        }

        private static void VerifyCheckedEnumToSByte(E value, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(E)), typeof(sbyte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile(useInterpreter);

            if ((int)value < sbyte.MinValue | (int)value > sbyte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((sbyte)value, f());
        }

        private static void VerifyCheckedEnumToNullableSByte(E value, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(E)), typeof(sbyte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile(useInterpreter);

            if (value < 0 | (int)value > sbyte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((sbyte)value, f());
        }

        private static void VerifyCheckedEnumToShort(E value, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(E)), typeof(short)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);

            if (value < (E)short.MinValue | value > (E)short.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((short)value, f());
        }

        private static void VerifyCheckedEnumToNullableShort(E value, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(E)), typeof(short?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);

            if (value < (E)short.MinValue | value > (E)short.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((short)value, f());
        }

        private static void VerifyCheckedEnumToUInt(E value, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(E)), typeof(uint)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            if (value < 0)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((uint)value, f());
        }

        private static void VerifyCheckedEnumToNullableUInt(E value, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(E)), typeof(uint)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            if (value < 0)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((uint)value, f());
        }

        private static void VerifyCheckedEnumToULong(E value, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(E)), typeof(ulong)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);

            if (value < 0)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((ulong)value, f());
        }

        private static void VerifyCheckedEnumToNullableULong(E value, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(E)), typeof(ulong?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);

            if (value < 0)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((ulong)value, f());
        }

        private static void VerifyCheckedEnumToUShort(E value, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(E)), typeof(ushort)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);

            if (value < 0 | (int)value > ushort.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((ushort)value, f());
        }

        private static void VerifyCheckedEnumToNullableUShort(E value, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(E)), typeof(ushort?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);

            if (value < 0 | (int)value > ushort.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((ushort)value, f());
        }

        private static void VerifyCheckedNullableEnumToByte(E? value, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(E?)), typeof(byte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Throws<InvalidOperationException>(() => f());
            else if (value < 0 | (int)value > byte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((byte)value, f());
        }

        private static void VerifyCheckedNullableEnumToNullableByte(E? value, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(E?)), typeof(byte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Null(f());
            else if (value < 0 | (int)value > byte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((byte)value, f());
        }

        private static void VerifyCheckedNullableEnumToChar(E? value, bool useInterpreter)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(E?)), typeof(char)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Throws<InvalidOperationException>(() => f());
            else if (value.GetValueOrDefault() < 0 | (int)value.GetValueOrDefault() > char.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((char)value, f());
        }

        private static void VerifyCheckedNullableEnumToNullableChar(E? value, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(E?)), typeof(char?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Null(f());
            else if (value.GetValueOrDefault() < 0 | (int)value.GetValueOrDefault() > char.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((char)value, f());
        }

        private static void VerifyCheckedNullableEnumToDouble(E? value, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(E?)), typeof(double)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal((double)value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableEnumToNullableDouble(E? value, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(E?)), typeof(double?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal((double?)value, f());
        }

        private static void VerifyCheckedNullableEnumToEnum(E? value, bool useInterpreter)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(E?)), typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableEnumToNullableEnum(E? value, bool useInterpreter)
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(E?)), typeof(E?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableEnumToEnumLong(E? value, bool useInterpreter)
        {
            Expression<Func<El>> e =
                Expression.Lambda<Func<El>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(E?)), typeof(El)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal((El)value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableEnumToNullableEnumLong(E? value, bool useInterpreter)
        {
            Expression<Func<El?>> e =
                Expression.Lambda<Func<El?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(E?)), typeof(El?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?> f = e.Compile(useInterpreter);

            Assert.Equal((El?)value, f());
        }

        private static void VerifyCheckedNullableEnumToFloat(E? value, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(E?)), typeof(float)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal((float)value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableEnumToNullableFloat(E? value, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(E?)), typeof(float?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal((float?)value, f());
        }

        private static void VerifyCheckedNullableEnumToInt(E? value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(E?)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal((int)value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableEnumToNullableInt(E? value, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(E?)), typeof(int?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);

            Assert.Equal((int?)value, f());
        }

        private static void VerifyCheckedNullableEnumToLong(E? value, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(E?)), typeof(long)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal((long)value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableEnumToNullableLong(E? value, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(E?)), typeof(long?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);

            Assert.Equal((long?)value, f());
        }

        private static void VerifyCheckedNullableEnumToSByte(E? value, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(E?)), typeof(sbyte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                E unboxed = value.GetValueOrDefault();
                if ((int)unboxed < sbyte.MinValue | (int)unboxed > sbyte.MaxValue)
                    Assert.Throws<OverflowException>(() => f());
                else
                    Assert.Equal((sbyte)unboxed, f());
            }
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableEnumToNullableSByte(E? value, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(E?)), typeof(sbyte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Null(f());
            else if (value < 0 | (int)value > sbyte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((sbyte)value, f());
        }

        private static void VerifyCheckedNullableEnumToShort(E? value, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(E?)), typeof(short)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Throws<InvalidOperationException>(() => f());
            else if ((int)value.GetValueOrDefault() < short.MinValue | (int)value.GetValueOrDefault() > short.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((short)value, f());
        }

        private static void VerifyCheckedNullableEnumToNullableShort(E? value, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(E?)), typeof(short?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Null(f());
            else if ((int)value.GetValueOrDefault() < short.MinValue | (int)value.GetValueOrDefault() > short.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((short)value, f());
        }

        private static void VerifyCheckedNullableEnumToUInt(E? value, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(E?)), typeof(uint)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Throws<InvalidOperationException>(() => f());
            else if (value < 0)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((uint)value, f());
        }

        private static void VerifyCheckedNullableEnumToNullableUInt(E? value, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(E?)), typeof(uint?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);

            if (value < 0)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((uint?)value, f());
        }

        private static void VerifyCheckedNullableEnumToULong(E? value, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(E?)), typeof(ulong)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Throws<InvalidOperationException>(() => f());
            else if (value < 0)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((ulong)value, f());
        }

        private static void VerifyCheckedNullableEnumToNullableULong(E? value, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(E?)), typeof(ulong?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Null(f());
            else if (value < 0)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((uint)value, f());
        }

        private static void VerifyCheckedNullableEnumToUShort(E? value, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(E?)), typeof(ushort)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Throws<InvalidOperationException>(() => f());
            else if (value.GetValueOrDefault() < 0 | (int)value.GetValueOrDefault() > ushort.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((ushort)value, f());
        }

        private static void VerifyCheckedNullableEnumToNullableUShort(E? value, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(E?)), typeof(ushort?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Null(f());
            else if (value.GetValueOrDefault() < 0 | (int)value.GetValueOrDefault() > ushort.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((ushort)value, f());
        }

        private static void VerifyCheckedEnumLongToByte(El value, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(El)), typeof(byte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile(useInterpreter);

            if (value < 0 | (long)value > byte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((byte)value, f());
        }

        private static void VerifyCheckedEnumLongToNullableByte(El value, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(El)), typeof(byte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile(useInterpreter);

            if (value < 0 | (long)value > byte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((byte)value, f());
        }

        private static void VerifyCheckedEnumLongToChar(El value, bool useInterpreter)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(El)), typeof(char)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile(useInterpreter);

            if (value < 0 | (long)value > char.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((char)value, f());
        }

        private static void VerifyCheckedEnumLongToNullableChar(El value, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(El)), typeof(char?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile(useInterpreter);

            if (value < 0 | (long)value > char.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((char)value, f());
        }

        private static void VerifyCheckedEnumLongToDouble(El value, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(El)), typeof(double)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);

            Assert.Equal((double)value, f());
        }

        private static void VerifyCheckedEnumLongToNullableDouble(El value, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(El)), typeof(double?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal((double)value, f());
        }

        private static void VerifyCheckedEnumLongToEnum(El value, bool useInterpreter)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(El)), typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile(useInterpreter);

            if ((long)value < int.MinValue | (long)value > int.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((E)value, f());
        }

        private static void VerifyCheckedEnumLongToNullableEnum(El value, bool useInterpreter)
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(El)), typeof(E?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile(useInterpreter);

            if ((long)value < int.MinValue | (long)value > int.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((E)value, f());
        }

        private static void VerifyCheckedEnumLongToEnumLong(El value, bool useInterpreter)
        {
            Expression<Func<El>> e =
                Expression.Lambda<Func<El>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(El)), typeof(El)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedEnumLongToNullableEnumLong(El value, bool useInterpreter)
        {
            Expression<Func<El?>> e =
                Expression.Lambda<Func<El?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(El)), typeof(El?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedEnumLongToFloat(El value, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(El)), typeof(float)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);

            Assert.Equal((float)value, f());
        }

        private static void VerifyCheckedEnumLongToNullableFloat(El value, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(El)), typeof(float?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal((float)value, f());
        }

        private static void VerifyCheckedEnumLongToInt(El value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(El)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            if ((long)value < int.MinValue | (long)value > int.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((int)value, f());
        }

        private static void VerifyCheckedEnumLongToNullableInt(El value, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(El)), typeof(int?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);

            if ((long)value < int.MinValue | (long)value > int.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((int)value, f());
        }

        private static void VerifyCheckedEnumLongToLong(El value, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(El)), typeof(long)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);

            Assert.Equal((long)value, f());
        }

        private static void VerifyCheckedEnumLongToNullableLong(El value, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(El)), typeof(long?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);

            Assert.Equal((long)value, f());
        }

        private static void VerifyCheckedEnumLongToSByte(El value, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(El)), typeof(sbyte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile(useInterpreter);

            if ((long)value < sbyte.MinValue | (long)value > sbyte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((sbyte)value, f());
        }

        private static void VerifyCheckedEnumLongToNullableSByte(El value, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(El)), typeof(sbyte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile(useInterpreter);

            if ((long)value < 0 | (long)value > sbyte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((sbyte)value, f());
        }

        private static void VerifyCheckedEnumLongToShort(El value, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(El)), typeof(short)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);

            if ((long)value < short.MinValue | (long)value > short.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((short)value, f());
        }

        private static void VerifyCheckedEnumLongToNullableShort(El value, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(El)), typeof(short?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);

            if ((long)value < short.MinValue | (long)value > short.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((short)value, f());
        }

        private static void VerifyCheckedEnumLongToUInt(El value, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(El)), typeof(uint)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            if (value < 0 | (long)value > uint.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((uint)value, f());
        }

        private static void VerifyCheckedEnumLongToNullableUInt(El value, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(El)), typeof(uint?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);

            if (value < 0 | (long)value > uint.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((uint)value, f());
        }

        private static void VerifyCheckedEnumLongToULong(El value, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(El)), typeof(ulong)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);

            if (value < 0)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((ulong)value, f());
        }

        private static void VerifyCheckedEnumLongToNullableULong(El value, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(El)), typeof(ulong?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);

            if (value < 0)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((ulong)value, f());
        }

        private static void VerifyCheckedEnumLongToUShort(El value, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(El)), typeof(ushort)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);

            if (value < 0 | (long)value > ushort.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((ushort)value, f());
        }

        private static void VerifyCheckedEnumLongToNullableUShort(El value, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(El)), typeof(ushort?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);

            if (value < 0 | (long)value > ushort.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((ushort)value, f());
        }

        private static void VerifyCheckedNullableEnumLongToByte(El? value, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(El?)), typeof(byte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Throws<InvalidOperationException>(() => f());
            else if (value < 0 | (long)value > byte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((byte)value, f());
        }

        private static void VerifyCheckedNullableEnumLongToNullableByte(El? value, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(El?)), typeof(byte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Null(f());
            else if (value < 0 | (long)value > byte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((byte)value, f());
        }

        private static void VerifyCheckedNullableEnumLongToChar(El? value, bool useInterpreter)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(El?)), typeof(char)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Throws<InvalidOperationException>(() => f());
            else if (value < 0 | (long)value > char.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((char)value, f());
        }

        private static void VerifyCheckedNullableEnumLongToNullableChar(El? value, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(El?)), typeof(char?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Null(f());
            else if (value < 0 | (long)value > char.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((char)value, f());
        }

        private static void VerifyCheckedNullableEnumLongToDouble(El? value, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(El?)), typeof(double)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal((double)value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableEnumLongToNullableDouble(El? value, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(El?)), typeof(double?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal((double?)value, f());
        }

        private static void VerifyCheckedNullableEnumLongToEnum(El? value, bool useInterpreter)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(El?)), typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                if ((long)value.GetValueOrDefault() < int.MinValue || (long)value.GetValueOrDefault() > int.MaxValue)
                    Assert.Throws<OverflowException>(() => f());
                else
                    Assert.Equal((E)value.GetValueOrDefault(), f());
            }
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableEnumLongToNullableEnum(El? value, bool useInterpreter)
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(El?)), typeof(E?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Null(f());
            else if ((long)value < int.MinValue | (long)value > int.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((E)value, f());
        }

        private static void VerifyCheckedNullableEnumLongToEnumLong(El? value, bool useInterpreter)
        {
            Expression<Func<El>> e =
                Expression.Lambda<Func<El>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(El?)), typeof(El)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableEnumLongToNullableEnumLong(El? value, bool useInterpreter)
        {
            Expression<Func<El?>> e =
                Expression.Lambda<Func<El?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(El?)), typeof(El?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableEnumLongToFloat(El? value, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(El?)), typeof(float)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal((float)value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableEnumLongToNullableFloat(El? value, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(El?)), typeof(float?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal((float?)value, f());
        }

        private static void VerifyCheckedNullableEnumLongToInt(El? value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(El?)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Throws<InvalidOperationException>(() => f());
            else if ((long)value < int.MinValue | (long)value > int.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((int)value, f());
        }

        private static void VerifyCheckedNullableEnumLongToNullableInt(El? value, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(El?)), typeof(int?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Null(f());
            else if ((long)value < int.MinValue | (long)value > int.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((int)value, f());
        }

        private static void VerifyCheckedNullableEnumLongToLong(El? value, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(El?)), typeof(long)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal((long)value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableEnumLongToNullableLong(El? value, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(El?)), typeof(long?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);

            Assert.Equal((long?)value, f());
        }

        private static void VerifyCheckedNullableEnumLongToSByte(El? value, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(El?)), typeof(sbyte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                El unboxed = value.GetValueOrDefault();
                if ((long)unboxed < sbyte.MinValue | (long)unboxed > sbyte.MaxValue)
                    Assert.Throws<OverflowException>(() => f());
                else
                    Assert.Equal((sbyte)unboxed, f());
            }
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableEnumLongToNullableSByte(El? value, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(El?)), typeof(sbyte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Null(f());
            else if (value < 0 | (long)value > sbyte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((sbyte)value, f());
        }

        private static void VerifyCheckedNullableEnumLongToShort(El? value, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(El?)), typeof(short)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Throws<InvalidOperationException>(() => f());
            else if ((long)value < short.MinValue | (long)value > short.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((short)value, f());
        }

        private static void VerifyCheckedNullableEnumLongToNullableShort(El? value, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(El?)), typeof(short?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Null(f());
            else if ((long)value < short.MinValue | (long)value > short.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((short)value, f());
        }

        private static void VerifyCheckedNullableEnumLongToUInt(El? value, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(El?)), typeof(uint)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Throws<InvalidOperationException>(() => f());
            else if (value < 0 | (long)value > uint.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((uint)value, f());
        }

        private static void VerifyCheckedNullableEnumLongToNullableUInt(El? value, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(El?)), typeof(uint?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Null(f());
            else if (value < 0 | (long)value > uint.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((uint)value, f());
        }

        private static void VerifyCheckedNullableEnumLongToULong(El? value, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(El?)), typeof(ulong)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Throws<InvalidOperationException>(() => f());
            else if (value < 0)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((ulong)value.GetValueOrDefault(), f());
        }

        private static void VerifyCheckedNullableEnumLongToNullableULong(El? value, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(El?)), typeof(ulong?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);

            if ((!value.HasValue))
                Assert.Null(f());
            else if (value.GetValueOrDefault() < 0)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((ulong?)value.GetValueOrDefault(), f());
        }

        private static void VerifyCheckedNullableEnumLongToUShort(El? value, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(El?)), typeof(ushort)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Throws<InvalidOperationException>(() => f());
            else if (value < 0 | (long)value > ushort.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((ushort)value, f());
        }

        private static void VerifyCheckedNullableEnumLongToNullableUShort(El? value, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(El?)), typeof(ushort?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Null(f());
            else if (value < 0 | (long)value > ushort.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((ushort)value, f());
        }

        private static void VerifyCheckedFloatToByte(float value, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(float)), typeof(byte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile(useInterpreter);

            byte expected = 0;
            try
            {
                expected = checked((byte)value);
            }
            catch(OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedFloatToNullableByte(float value, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(float)), typeof(byte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile(useInterpreter);

            byte? expected = null;
            try
            {
                expected = checked((byte?)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedFloatToChar(float value, bool useInterpreter)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(float)), typeof(char)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile(useInterpreter);

            char expected = '\0';
            try
            {
                expected = checked((char)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedFloatToNullableChar(float value, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(float)), typeof(char?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile(useInterpreter);

            char expected = '\0';
            try
            {
                expected = checked((char)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedFloatToDecimal(float value, bool useInterpreter)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(float)), typeof(decimal)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal> f = e.Compile(useInterpreter);

            decimal expected = 0;
            try
            {
                expected = (decimal)value;
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedFloatToNullableDecimal(float value, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(float)), typeof(decimal?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal?> f = e.Compile(useInterpreter);

            decimal expected = 0;
            try
            {
                expected = (decimal)value;
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedFloatToDouble(float value, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(float)), typeof(double)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedFloatToNullableDouble(float value, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(float)), typeof(double?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedFloatToEnum(float value, bool useInterpreter)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(float)), typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile(useInterpreter);

            E expected = 0;
            try
            {
                expected = checked((E)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedFloatToNullableEnum(float value, bool useInterpreter)
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(float)), typeof(E?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile(useInterpreter);

            E expected = 0;
            try
            {
                expected = checked((E)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedFloatToEnumLong(float value, bool useInterpreter)
        {
            Expression<Func<El>> e =
                Expression.Lambda<Func<El>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(float)), typeof(El)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El> f = e.Compile(useInterpreter);

            El expected = 0;
            try
            {
                expected = checked((El)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedFloatToNullableEnumLong(float value, bool useInterpreter)
        {
            Expression<Func<El?>> e =
                Expression.Lambda<Func<El?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(float)), typeof(El?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?> f = e.Compile(useInterpreter);

            El expected = 0;
            try
            {
                expected = checked((El)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedFloatToFloat(float value, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(float)), typeof(float)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedFloatToNullableFloat(float value, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(float)), typeof(float?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedFloatToInt(float value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(float)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            int expected = 0;
            try
            {
                expected = checked((int)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedFloatToNullableInt(float value, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(float)), typeof(int?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);

            int expected = 0;
            try
            {
                expected = checked((int)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedFloatToLong(float value, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(float)), typeof(long)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);

            long expected = 0;
            try
            {
                expected = checked((long)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedFloatToNullableLong(float value, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(float)), typeof(long?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);

            long expected = 0;
            try
            {
                expected = checked((long)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedFloatToSByte(float value, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(float)), typeof(sbyte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile(useInterpreter);

            sbyte expected = 0;
            try
            {
                expected = checked((sbyte)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedFloatToNullableSByte(float value, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(float)), typeof(sbyte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile(useInterpreter);

            sbyte expected = 0;
            try
            {
                expected = checked((sbyte)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedFloatToShort(float value, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(float)), typeof(short)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);

            short expected = 0;
            try
            {
                expected = checked((short)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedFloatToNullableShort(float value, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(float)), typeof(short?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);

            short expected = 0;
            try
            {
                expected = checked((short)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedFloatToUInt(float value, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(float)), typeof(uint)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            uint expected = 0;
            try
            {
                expected = checked((uint)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedFloatToNullableUInt(float value, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(float)), typeof(uint?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);

            uint expected = 0;
            try
            {
                expected = checked((uint)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedFloatToULong(float value, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(float)), typeof(ulong)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);

            ulong expected = 0;
            try
            {
                expected = checked((ulong)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedFloatToNullableULong(float value, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(float)), typeof(ulong?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);

            ulong expected = 0;
            try
            {
                expected = checked((ulong)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedFloatToUShort(float value, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(float)), typeof(ushort)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);

            ushort expected = 0;
            try
            {
                expected = checked((ushort)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedFloatToNullableUShort(float value, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(float)), typeof(ushort?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);

            ushort expected = 0;
            try
            {
                expected = checked((ushort)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedNullableFloatToByte(float? value, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(float?)), typeof(byte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                byte expected = 0;
                try
                {
                    expected = checked((byte)value.GetValueOrDefault());
                }
                catch (OverflowException)
                {
                    Assert.Throws<OverflowException>(() => f());
                    return;
                }

                Assert.Equal(expected, f());
            }
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableFloatToNullableByte(float? value, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(float?)), typeof(byte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile(useInterpreter);

            byte? expected = null;
            try
            {
                expected = checked((byte?)value);
            }
            catch(OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedNullableFloatToChar(float? value, bool useInterpreter)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(float?)), typeof(char)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                char expected = '\0';
                try
                {
                    expected = checked((char)value.GetValueOrDefault());
                }
                catch (OverflowException)
                {
                    Assert.Throws<OverflowException>(() => f());
                    return;
                }

                Assert.Equal(expected, f());
            }
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableFloatToNullableChar(float? value, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(float?)), typeof(char?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile(useInterpreter);

            char? expected = null;
            try
            {
                expected = checked((char?)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedNullableFloatToDecimal(float? value, bool useInterpreter)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(float?)), typeof(decimal)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                decimal expected = 0;
                try
                {
                    expected = checked((decimal)value.GetValueOrDefault());
                }
                catch (OverflowException)
                {
                    Assert.Throws<OverflowException>(() => f());
                    return;
                }

                Assert.Equal(expected, f());
            }
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableFloatToNullableDecimal(float? value, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(float?)), typeof(decimal?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal?> f = e.Compile(useInterpreter);

            decimal? expected = null;
            try
            {
                expected = (decimal?)value;
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedNullableFloatToDouble(float? value, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(float?)), typeof(double)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableFloatToNullableDouble(float? value, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(float?)), typeof(double?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableFloatToEnum(float? value, bool useInterpreter)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(float?)), typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                E expected = 0;
                try
                {
                    expected = checked((E)value.GetValueOrDefault());
                }
                catch (OverflowException)
                {
                    Assert.Throws<OverflowException>(() => f());
                    return;
                }

                Assert.Equal(expected, f());
            }
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableFloatToNullableEnum(float? value, bool useInterpreter)
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(float?)), typeof(E?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile(useInterpreter);

            E? expected = null;
            try
            {
                expected = checked((E?)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedNullableFloatToEnumLong(float? value, bool useInterpreter)
        {
            Expression<Func<El>> e =
                Expression.Lambda<Func<El>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(float?)), typeof(El)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                El expected = 0;
                try
                {
                    expected = checked((El)value.GetValueOrDefault());
                }
                catch (OverflowException)
                {
                    Assert.Throws<OverflowException>(() => f());
                    return;
                }

                Assert.Equal(expected, f());
            }
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableFloatToNullableEnumLong(float? value, bool useInterpreter)
        {
            Expression<Func<El?>> e =
                Expression.Lambda<Func<El?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(float?)), typeof(El?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?> f = e.Compile(useInterpreter);

            El? expected = null;
            try
            {
                expected = checked((El?)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedNullableFloatToFloat(float? value, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(float?)), typeof(float)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableFloatToNullableFloat(float? value, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(float?)), typeof(float?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableFloatToInt(float? value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(float?)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                int expected = 0;
                try
                {
                    expected = checked((int)value.GetValueOrDefault());
                }
                catch (OverflowException)
                {
                    Assert.Throws<OverflowException>(() => f());
                    return;
                }

                Assert.Equal(expected, f());
            }
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableFloatToNullableInt(float? value, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(float?)), typeof(int?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);

            int? expected = null;
            try
            {
                expected = checked((int?)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedNullableFloatToLong(float? value, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(float?)), typeof(long)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                long expected = 0;
                try
                {
                    expected = checked((long)value.GetValueOrDefault());
                }
                catch (OverflowException)
                {
                    Assert.Throws<OverflowException>(() => f());
                    return;
                }

                Assert.Equal(expected, f());
            }
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableFloatToNullableLong(float? value, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(float?)), typeof(long?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);

            long? expected = null;
            try
            {
                expected = checked((long?)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedNullableFloatToSByte(float? value, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(float?)), typeof(sbyte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                sbyte expected = 0;
                try
                {
                    expected = checked((sbyte)value.GetValueOrDefault());
                }
                catch (OverflowException)
                {
                    Assert.Throws<OverflowException>(() => f());
                    return;
                }

                Assert.Equal(expected, f());
            }
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableFloatToNullableSByte(float? value, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(float?)), typeof(sbyte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile(useInterpreter);

            sbyte? expected = null;
            try
            {
                expected = checked((sbyte?)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedNullableFloatToShort(float? value, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(float?)), typeof(short)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                short expected = 0;
                try
                {
                    expected = checked((short)value.GetValueOrDefault());
                }
                catch (OverflowException)
                {
                    Assert.Throws<OverflowException>(() => f());
                    return;
                }

                Assert.Equal(expected, f());
            }
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableFloatToNullableShort(float? value, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(float?)), typeof(short?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);

            short? expected = null;
            try
            {
                expected = checked((short?)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedNullableFloatToUInt(float? value, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(float?)), typeof(uint)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                uint expected = 0;
                try
                {
                    expected = checked((uint)value.GetValueOrDefault());
                }
                catch (OverflowException)
                {
                    Assert.Throws<OverflowException>(() => f());
                    return;
                }

                Assert.Equal(expected, f());
            }
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableFloatToNullableUInt(float? value, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(float?)), typeof(uint?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);

            uint? expected = null;
            try
            {
                expected = checked((uint?)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedNullableFloatToULong(float? value, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(float?)), typeof(ulong)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                ulong expected = 0;
                try
                {
                    expected = checked((ulong)value.GetValueOrDefault());
                }
                catch (OverflowException)
                {
                    Assert.Throws<OverflowException>(() => f());
                    return;
                }

                Assert.Equal(expected, f());
            }
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableFloatToNullableULong(float? value, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(float?)), typeof(ulong?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);

            ulong? expected = null;
            try
            {
                expected = checked((ulong?)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedNullableFloatToUShort(float? value, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(float?)), typeof(ushort)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                ushort expected = 0;
                try
                {
                    expected = checked((ushort)value.GetValueOrDefault());
                }
                catch (OverflowException)
                {
                    Assert.Throws<OverflowException>(() => f());
                    return;
                }

                Assert.Equal(expected, f());
            }
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableFloatToNullableUShort(float? value, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(float?)), typeof(ushort?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);

            ushort? expected = null;
            try
            {
                expected = checked((ushort?)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedIntToByte(int value, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(int)), typeof(byte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile(useInterpreter);

            if (value < 0 | value > byte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((byte)value, f());
        }

        private static void VerifyCheckedIntToNullableByte(int value, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(int)), typeof(byte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile(useInterpreter);

            byte expected = 0;
            try
            {
                expected = checked((byte)value);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCheckedIntToChar(int value, bool useInterpreter)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(int)), typeof(char)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile(useInterpreter);

            if (value < 0 | value > char.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((char)value, f());
        }

        private static void VerifyCheckedIntToNullableChar(int value, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(int)), typeof(char?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile(useInterpreter);

            if (value < 0 | value > char.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((char)value, f());
        }

        private static void VerifyCheckedIntToDecimal(int value, bool useInterpreter)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(int)), typeof(decimal)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedIntToNullableDecimal(int value, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(int)), typeof(decimal?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedIntToDouble(int value, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(int)), typeof(double)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedIntToNullableDouble(int value, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(int)), typeof(double?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedIntToEnum(int value, bool useInterpreter)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(int)), typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile(useInterpreter);

            Assert.Equal((E)value, f());
        }

        private static void VerifyCheckedIntToNullableEnum(int value, bool useInterpreter)
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(int)), typeof(E?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile(useInterpreter);

            Assert.Equal((E)value, f());
        }

        private static void VerifyCheckedIntToEnumLong(int value, bool useInterpreter)
        {
            Expression<Func<El>> e =
                Expression.Lambda<Func<El>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(int)), typeof(El)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El> f = e.Compile(useInterpreter);

            Assert.Equal((El)value, f());
        }

        private static void VerifyCheckedIntToNullableEnumLong(int value, bool useInterpreter)
        {
            Expression<Func<El?>> e =
                Expression.Lambda<Func<El?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(int)), typeof(El?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?> f = e.Compile(useInterpreter);

            Assert.Equal((El)value, f());
        }

        private static void VerifyCheckedIntToFloat(int value, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(int)), typeof(float)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedIntToNullableFloat(int value, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(int)), typeof(float?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedIntToInt(int value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(int)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedIntToNullableInt(int value, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(int)), typeof(int?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedIntToLong(int value, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(int)), typeof(long)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedIntToNullableLong(int value, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(int)), typeof(long?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedIntToSByte(int value, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(int)), typeof(sbyte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile(useInterpreter);

            if (value < sbyte.MinValue | value > sbyte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((sbyte)value, f());
        }

        private static void VerifyCheckedIntToNullableSByte(int value, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(int)), typeof(sbyte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile(useInterpreter);

            if (value < sbyte.MinValue | value > sbyte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((sbyte)value, f());
        }

        private static void VerifyCheckedIntToShort(int value, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(int)), typeof(short)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);

            if (value < short.MinValue | value > short.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((short)value, f());
        }

        private static void VerifyCheckedIntToNullableShort(int value, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(int)), typeof(short?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);

            if (value < short.MinValue | value > short.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((short)value, f());
        }

        private static void VerifyCheckedIntToUInt(int value, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(int)), typeof(uint)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            if (value < 0)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((uint)value, f());
        }

        private static void VerifyCheckedIntToNullableUInt(int value, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(int)), typeof(uint?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);

            if (value < 0)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((uint)value, f());
        }

        private static void VerifyCheckedIntToULong(int value, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(int)), typeof(ulong)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);

            if (value < 0)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((uint)value, f());
        }

        private static void VerifyCheckedIntToNullableULong(int value, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(int)), typeof(ulong?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);

            if (value < 0)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((uint)value, f());
        }

        private static void VerifyCheckedIntToUShort(int value, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(int)), typeof(ushort)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);

            if (value < 0 | value > ushort.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((ushort)value, f());
        }

        private static void VerifyCheckedIntToNullableUShort(int value, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(int)), typeof(ushort?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);

            if (value < 0 | value > ushort.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((ushort)value, f());
        }

        private static void VerifyCheckedNullableIntToByte(int? value, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(int?)), typeof(byte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Throws<InvalidOperationException>(() => f());
            else if (value < 0 | value > byte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((byte)value, f());
        }

        private static void VerifyCheckedNullableIntToNullableByte(int? value, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(int?)), typeof(byte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Null(f());
            else if (value < 0 | value > byte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((byte)value, f());
        }

        private static void VerifyCheckedNullableIntToChar(int? value, bool useInterpreter)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(int?)), typeof(char)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Throws<InvalidOperationException>(() => f());
            else if (value < 0 | value > char.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((char)value, f());
        }

        private static void VerifyCheckedNullableIntToNullableChar(int? value, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(int?)), typeof(char?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Null(f());
            else if (value < 0 | value > char.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((char)value, f());
        }

        private static void VerifyCheckedNullableIntToDecimal(int? value, bool useInterpreter)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(int?)), typeof(decimal)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableIntToNullableDecimal(int? value, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(int?)), typeof(decimal?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableIntToDouble(int? value, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(int?)), typeof(double)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableIntToNullableDouble(int? value, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(int?)), typeof(double?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableIntToEnum(int? value, bool useInterpreter)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(int?)), typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal((E)value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableIntToNullableEnum(int? value, bool useInterpreter)
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(int?)), typeof(E?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile(useInterpreter);

            Assert.Equal((E?)value, f());
        }

        private static void VerifyCheckedNullableIntToEnumLong(int? value, bool useInterpreter)
        {
            Expression<Func<El>> e =
                Expression.Lambda<Func<El>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(int?)), typeof(El)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal((El)value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableIntToNullableEnumLong(int? value, bool useInterpreter)
        {
            Expression<Func<El?>> e =
                Expression.Lambda<Func<El?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(int?)), typeof(El?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?> f = e.Compile(useInterpreter);

            Assert.Equal((El?)value, f());
        }

        private static void VerifyCheckedNullableIntToFloat(int? value, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(int?)), typeof(float)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableIntToNullableFloat(int? value, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(int?)), typeof(float?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableIntToInt(int? value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(int?)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableIntToNullableInt(int? value, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(int?)), typeof(int?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableIntToLong(int? value, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(int?)), typeof(long)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableIntToNullableLong(int? value, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(int?)), typeof(long?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableIntToSByte(int? value, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(int?)), typeof(sbyte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                int unboxed = value.GetValueOrDefault();
                if (unboxed < sbyte.MinValue | unboxed > sbyte.MaxValue)
                    Assert.Throws<OverflowException>(() => f());
                else
                    Assert.Equal((sbyte)unboxed, f());
            }
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableIntToNullableSByte(int? value, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(int?)), typeof(sbyte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Null(f());
            else if (value < sbyte.MinValue | value > sbyte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((sbyte)value, f());
        }

        private static void VerifyCheckedNullableIntToShort(int? value, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(int?)), typeof(short)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Throws<InvalidOperationException>(() => f());
            else if (value < short.MinValue | value > short.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((short)value, f());
        }

        private static void VerifyCheckedNullableIntToNullableShort(int? value, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(int?)), typeof(short?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Null(f());
            else if (value < short.MinValue | value > short.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((short)value, f());
        }

        private static void VerifyCheckedNullableIntToUInt(int? value, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(int?)), typeof(uint)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Throws<InvalidOperationException>(() => f());
            else if (value < 0)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((uint)value, f());
        }

        private static void VerifyCheckedNullableIntToNullableUInt(int? value, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(int?)), typeof(uint?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);

            if (value < 0)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((uint?)value, f());
        }

        private static void VerifyCheckedNullableIntToULong(int? value, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(int?)), typeof(ulong)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Throws<InvalidOperationException>(() => f());
            else if (value < 0)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((ulong)value, f());
        }

        private static void VerifyCheckedNullableIntToNullableULong(int? value, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(int?)), typeof(ulong?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);

            if (value < 0)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((uint?)value, f());
        }

        private static void VerifyCheckedNullableIntToUShort(int? value, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(int?)), typeof(ushort)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                if (value.GetValueOrDefault() < 0 | value.GetValueOrDefault() > ushort.MaxValue)
                    Assert.Throws<OverflowException>(() => f());
                else
                    Assert.Equal((ushort)value.GetValueOrDefault(), f());
            }
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableIntToNullableUShort(int? value, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(int?)), typeof(ushort?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);

            if (value.GetValueOrDefault() < 0 | value.GetValueOrDefault() > ushort.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((ushort?)value, f());
        }

        private static void VerifyCheckedLongToByte(long value, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(long)), typeof(byte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile(useInterpreter);

            if (value < 0 | value > byte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((byte)value, f());
        }

        private static void VerifyCheckedLongToNullableByte(long value, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(long)), typeof(byte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile(useInterpreter);

            if (value < 0 | value > byte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((byte)value, f());
        }

        private static void VerifyCheckedLongToChar(long value, bool useInterpreter)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(long)), typeof(char)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile(useInterpreter);

            if (value < 0 | value > char.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((char)value, f());
        }

        private static void VerifyCheckedLongToNullableChar(long value, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(long)), typeof(char?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile(useInterpreter);

            if (value < 0 | value > char.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((char)value, f());
        }

        private static void VerifyCheckedLongToDecimal(long value, bool useInterpreter)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(long)), typeof(decimal)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedLongToNullableDecimal(long value, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(long)), typeof(decimal?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedLongToDouble(long value, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(long)), typeof(double)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedLongToNullableDouble(long value, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(long)), typeof(double?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedLongToEnum(long value, bool useInterpreter)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(long)), typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile(useInterpreter);

            if (value < int.MinValue | value > int.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((E)value, f());
        }

        private static void VerifyCheckedLongToNullableEnum(long value, bool useInterpreter)
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(long)), typeof(E?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile(useInterpreter);

            if (value < int.MinValue | value > int.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((E)value, f());
        }

        private static void VerifyCheckedLongToEnumLong(long value, bool useInterpreter)
        {
            Expression<Func<El>> e =
                Expression.Lambda<Func<El>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(long)), typeof(El)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El> f = e.Compile(useInterpreter);

            Assert.Equal((El)value, f());
        }

        private static void VerifyCheckedLongToNullableEnumLong(long value, bool useInterpreter)
        {
            Expression<Func<El?>> e =
                Expression.Lambda<Func<El?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(long)), typeof(El?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?> f = e.Compile(useInterpreter);

            Assert.Equal((El)value, f());
        }

        private static void VerifyCheckedLongToFloat(long value, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(long)), typeof(float)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedLongToNullableFloat(long value, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(long)), typeof(float?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedLongToInt(long value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(long)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            if (value < int.MinValue | value > int.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal(value, f());
        }

        private static void VerifyCheckedLongToNullableInt(long value, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(long)), typeof(int?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);

            if (value < int.MinValue | value > int.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((int)value, f());
        }

        private static void VerifyCheckedLongToLong(long value, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(long)), typeof(long)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedLongToNullableLong(long value, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(long)), typeof(long?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedLongToSByte(long value, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(long)), typeof(sbyte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile(useInterpreter);

            if (value < sbyte.MinValue | value > sbyte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((sbyte)value, f());
        }

        private static void VerifyCheckedLongToNullableSByte(long value, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(long)), typeof(sbyte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile(useInterpreter);

            if (value < sbyte.MinValue | value > sbyte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((sbyte)value, f());
        }

        private static void VerifyCheckedLongToShort(long value, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(long)), typeof(short)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);

            if (value < short.MinValue | value > short.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal(value, f());
        }

        private static void VerifyCheckedLongToNullableShort(long value, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(long)), typeof(short?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);

            if (value < short.MinValue | value > short.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((short)value, f());
        }

        private static void VerifyCheckedLongToUInt(long value, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(long)), typeof(uint)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            if (value < 0 | value > uint.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal(value, f());
        }

        private static void VerifyCheckedLongToNullableUInt(long value, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(long)), typeof(uint?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);

            if (value < 0 | value > uint.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((uint)value, f());
        }

        private static void VerifyCheckedLongToULong(long value, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(long)), typeof(ulong)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);

            if (value < 0)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((ulong)value, f());
        }

        private static void VerifyCheckedLongToNullableULong(long value, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(long)), typeof(ulong?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);

            if (value < 0)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((ulong)value, f());
        }

        private static void VerifyCheckedLongToUShort(long value, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(long)), typeof(ushort)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);

            if (value < 0 | value > ushort.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal(value, f());
        }

        private static void VerifyCheckedLongToNullableUShort(long value, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(long)), typeof(ushort?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);

            if (value < 0 | value > ushort.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((ushort)value, f());
        }

        private static void VerifyCheckedNullableLongToByte(long? value, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(long?)), typeof(byte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Throws<InvalidOperationException>(() => f());
            else if (value < 0 | value > byte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal(value.GetValueOrDefault(), f());
        }

        private static void VerifyCheckedNullableLongToNullableByte(long? value, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(long?)), typeof(byte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Null(f());
            else if (value < 0 | value > byte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((byte)value, f());
        }

        private static void VerifyCheckedNullableLongToChar(long? value, bool useInterpreter)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(long?)), typeof(char)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Throws<InvalidOperationException>(() => f());
            else if (value < 0 | value > char.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal(value.GetValueOrDefault(), f());
        }

        private static void VerifyCheckedNullableLongToNullableChar(long? value, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(long?)), typeof(char?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Null(f());
            else if (value < 0 | value > byte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((char)value.GetValueOrDefault(), f());
        }

        private static void VerifyCheckedNullableLongToDecimal(long? value, bool useInterpreter)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(long?)), typeof(decimal)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableLongToNullableDecimal(long? value, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(long?)), typeof(decimal?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableLongToDouble(long? value, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(long?)), typeof(double)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableLongToNullableDouble(long? value, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(long?)), typeof(double?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableLongToEnum(long? value, bool useInterpreter)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(long?)), typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                if (value.GetValueOrDefault() < int.MinValue || value.GetValueOrDefault() > int.MaxValue)
                    Assert.Throws<OverflowException>(() => f());
                else
                    Assert.Equal((E)value.GetValueOrDefault(), f());
            }
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableLongToNullableEnum(long? value, bool useInterpreter)
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(long?)), typeof(E?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Null(f());
            else if (value < int.MinValue | value > int.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((E)value.GetValueOrDefault(), f());
        }

        private static void VerifyCheckedNullableLongToEnumLong(long? value, bool useInterpreter)
        {
            Expression<Func<El>> e =
                Expression.Lambda<Func<El>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(long?)), typeof(El)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal((El)value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableLongToNullableEnumLong(long? value, bool useInterpreter)
        {
            Expression<Func<El?>> e =
                Expression.Lambda<Func<El?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(long?)), typeof(El?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?> f = e.Compile(useInterpreter);

            Assert.Equal((El?)value, f());
        }

        private static void VerifyCheckedNullableLongToFloat(long? value, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(long?)), typeof(float)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableLongToNullableFloat(long? value, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(long?)), typeof(float?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableLongToInt(long? value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(long?)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Throws<InvalidOperationException>(() => f());
            else if (value < int.MinValue | value > int.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal(value.GetValueOrDefault(), f());
        }

        private static void VerifyCheckedNullableLongToNullableInt(long? value, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(long?)), typeof(int?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);

            if (value < int.MinValue | value > int.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableLongToLong(long? value, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(long?)), typeof(long)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableLongToNullableLong(long? value, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(long?)), typeof(long?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableLongToSByte(long? value, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(long?)), typeof(sbyte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                long unboxed = value.GetValueOrDefault();
                if (unboxed < sbyte.MinValue | unboxed > sbyte.MaxValue)
                    Assert.Throws<OverflowException>(() => f());
                else
                    Assert.Equal((sbyte)unboxed, f());
            }
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableLongToNullableSByte(long? value, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(long?)), typeof(sbyte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Null(f());
            else if (value < sbyte.MinValue | value > sbyte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((sbyte)value, f());
        }

        private static void VerifyCheckedNullableLongToShort(long? value, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(long?)), typeof(short)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Throws<InvalidOperationException>(() => f());
            else if (value < short.MinValue | value > short.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal(value.GetValueOrDefault(), f());
        }

        private static void VerifyCheckedNullableLongToNullableShort(long? value, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(long?)), typeof(short?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);

            if (value < short.MinValue | value > short.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableLongToUInt(long? value, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(long?)), typeof(uint)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Throws<InvalidOperationException>(() => f());
            else if (value < 0 | value > uint.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal(value.GetValueOrDefault(), f());
        }

        private static void VerifyCheckedNullableLongToNullableUInt(long? value, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(long?)), typeof(uint?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);

            if (value < 0 | value > uint.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableLongToULong(long? value, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(long?)), typeof(ulong)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Throws<InvalidOperationException>(() => f());
            else if (value < 0)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((ulong)value, f());
        }

        private static void VerifyCheckedNullableLongToNullableULong(long? value, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(long?)), typeof(ulong?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);

            if (value < 0)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((ulong?)value, f());
        }

        private static void VerifyCheckedNullableLongToUShort(long? value, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(long?)), typeof(ushort)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Throws<InvalidOperationException>(() => f());
            else if (value < 0 | value > ushort.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal(value.GetValueOrDefault(), f());
        }

        private static void VerifyCheckedNullableLongToNullableUShort(long? value, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(long?)), typeof(ushort?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);

            if (value < 0 | value > ushort.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal(value, f());
        }

        private static void VerifyCheckedSByteToByte(sbyte value, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(sbyte)), typeof(byte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile(useInterpreter);

            if (value < 0)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((byte)value, f());
        }

        private static void VerifyCheckedSByteToNullableByte(sbyte value, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(sbyte)), typeof(byte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile(useInterpreter);

            if (value < 0)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((byte)value, f());
        }

        private static void VerifyCheckedSByteToChar(sbyte value, bool useInterpreter)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(sbyte)), typeof(char)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile(useInterpreter);

            if (value < 0)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((char)value, f());
        }

        private static void VerifyCheckedSByteToNullableChar(sbyte value, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(sbyte)), typeof(char?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile(useInterpreter);

            if (value < 0)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((char)value, f());
        }

        private static void VerifyCheckedSByteToDecimal(sbyte value, bool useInterpreter)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(sbyte)), typeof(decimal)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedSByteToNullableDecimal(sbyte value, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(sbyte)), typeof(decimal?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedSByteToDouble(sbyte value, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(sbyte)), typeof(double)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedSByteToNullableDouble(sbyte value, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(sbyte)), typeof(double?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedSByteToEnum(sbyte value, bool useInterpreter)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(sbyte)), typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile(useInterpreter);

            Assert.Equal((E)value, f());
        }

        private static void VerifyCheckedSByteToNullableEnum(sbyte value, bool useInterpreter)
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(sbyte)), typeof(E?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile(useInterpreter);

            Assert.Equal((E)value, f());
        }

        private static void VerifyCheckedSByteToEnumLong(sbyte value, bool useInterpreter)
        {
            Expression<Func<El>> e =
                Expression.Lambda<Func<El>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(sbyte)), typeof(El)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El> f = e.Compile(useInterpreter);

            Assert.Equal((El)value, f());
        }

        private static void VerifyCheckedSByteToNullableEnumLong(sbyte value, bool useInterpreter)
        {
            Expression<Func<El?>> e =
                Expression.Lambda<Func<El?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(sbyte)), typeof(El?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?> f = e.Compile(useInterpreter);

            Assert.Equal((El)value, f());
        }

        private static void VerifyCheckedSByteToFloat(sbyte value, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(sbyte)), typeof(float)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedSByteToNullableFloat(sbyte value, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(sbyte)), typeof(float?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedSByteToInt(sbyte value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(sbyte)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedSByteToNullableInt(sbyte value, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(sbyte)), typeof(int?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedSByteToLong(sbyte value, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(sbyte)), typeof(long)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedSByteToNullableLong(sbyte value, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(sbyte)), typeof(long?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedSByteToSByte(sbyte value, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(sbyte)), typeof(sbyte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedSByteToNullableSByte(sbyte value, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(sbyte)), typeof(sbyte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedSByteToShort(sbyte value, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(sbyte)), typeof(short)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedSByteToNullableShort(sbyte value, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(sbyte)), typeof(short?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedSByteToUInt(sbyte value, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(sbyte)), typeof(uint)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            if (value < 0)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((uint)value, f());
        }

        private static void VerifyCheckedSByteToNullableUInt(sbyte value, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(sbyte)), typeof(uint?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);

            if (value < 0)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((uint)value, f());
        }

        private static void VerifyCheckedSByteToULong(sbyte value, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(sbyte)), typeof(ulong)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);

            if (value < 0)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((uint)value, f());
        }

        private static void VerifyCheckedSByteToNullableULong(sbyte value, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(sbyte)), typeof(ulong?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);

            if (value < 0)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((uint)value, f());
        }

        private static void VerifyCheckedSByteToUShort(sbyte value, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(sbyte)), typeof(ushort)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);

            if (value < 0)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((ushort)value, f());
        }

        private static void VerifyCheckedSByteToNullableUShort(sbyte value, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(sbyte)), typeof(ushort?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);

            if (value < 0)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((ushort)value, f());
        }

        private static void VerifyCheckedNullableSByteToByte(sbyte? value, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(sbyte?)), typeof(byte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Throws<InvalidOperationException>(() => f());
            else if (value < 0)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((byte)value, f());
        }

        private static void VerifyCheckedNullableSByteToNullableByte(sbyte? value, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(sbyte?)), typeof(byte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Null(f());
            else if (value < 0)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((byte)value, f());
        }

        private static void VerifyCheckedNullableSByteToChar(sbyte? value, bool useInterpreter)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(sbyte?)), typeof(char)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Throws<InvalidOperationException>(() => f());
            else if (value < 0)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((char)value, f());
        }

        private static void VerifyCheckedNullableSByteToNullableChar(sbyte? value, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(sbyte?)), typeof(char?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile(useInterpreter);

            if (value < 0)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((char?)value, f());
        }

        private static void VerifyCheckedNullableSByteToDecimal(sbyte? value, bool useInterpreter)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(sbyte?)), typeof(decimal)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableSByteToNullableDecimal(sbyte? value, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(sbyte?)), typeof(decimal?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableSByteToDouble(sbyte? value, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(sbyte?)), typeof(double)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableSByteToNullableDouble(sbyte? value, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(sbyte?)), typeof(double?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableSByteToEnum(sbyte? value, bool useInterpreter)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(sbyte?)), typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal((E)value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableSByteToNullableEnum(sbyte? value, bool useInterpreter)
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(sbyte?)), typeof(E?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile(useInterpreter);

            Assert.Equal((E?)value, f());
        }

        private static void VerifyCheckedNullableSByteToEnumLong(sbyte? value, bool useInterpreter)
        {
            Expression<Func<El>> e =
                Expression.Lambda<Func<El>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(sbyte?)), typeof(El)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal((El)value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableSByteToNullableEnumLong(sbyte? value, bool useInterpreter)
        {
            Expression<Func<El?>> e =
                Expression.Lambda<Func<El?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(sbyte?)), typeof(El?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?> f = e.Compile(useInterpreter);

            Assert.Equal((El?)value, f());
        }

        private static void VerifyCheckedNullableSByteToFloat(sbyte? value, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(sbyte?)), typeof(float)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableSByteToNullableFloat(sbyte? value, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(sbyte?)), typeof(float?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableSByteToInt(sbyte? value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(sbyte?)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableSByteToNullableInt(sbyte? value, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(sbyte?)), typeof(int?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableSByteToLong(sbyte? value, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(sbyte?)), typeof(long)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableSByteToNullableLong(sbyte? value, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(sbyte?)), typeof(long?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableSByteToSByte(sbyte? value, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(sbyte?)), typeof(sbyte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableSByteToNullableSByte(sbyte? value, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(sbyte?)), typeof(sbyte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableSByteToShort(sbyte? value, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(sbyte?)), typeof(short)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableSByteToNullableShort(sbyte? value, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(sbyte?)), typeof(short?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableSByteToUInt(sbyte? value, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(sbyte?)), typeof(uint)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Throws<InvalidOperationException>(() => f());
            else if (value < 0)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((uint)value.GetValueOrDefault(), f());
        }

        private static void VerifyCheckedNullableSByteToNullableUInt(sbyte? value, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(sbyte?)), typeof(uint?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);

            if (value < 0)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((uint?)value, f());
        }

        private static void VerifyCheckedNullableSByteToULong(sbyte? value, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(sbyte?)), typeof(ulong)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Throws<InvalidOperationException>(() => f());
            else if (value < 0)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((ulong)value.GetValueOrDefault(), f());
        }

        private static void VerifyCheckedNullableSByteToNullableULong(sbyte? value, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(sbyte?)), typeof(ulong?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);

            if (value < 0)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((ulong?)value, f());
        }

        private static void VerifyCheckedNullableSByteToUShort(sbyte? value, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(sbyte?)), typeof(ushort)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                if (value.GetValueOrDefault() < 0)
                    Assert.Throws<OverflowException>(() => f());
                else
                    Assert.Equal((ushort)value.GetValueOrDefault(), f());
            }
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableSByteToNullableUShort(sbyte? value, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(sbyte?)), typeof(ushort?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);

            if (value < 0)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((ushort?)value, f());
        }

        private static void VerifyCheckedShortToByte(short value, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(short)), typeof(byte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile(useInterpreter);

            if (value < 0 | value > byte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((byte)value, f());
        }

        private static void VerifyCheckedShortToNullableByte(short value, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(short)), typeof(byte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile(useInterpreter);

            if (value < 0 | value > byte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((byte)value, f());
        }

        private static void VerifyCheckedShortToChar(short value, bool useInterpreter)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(short)), typeof(char)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile(useInterpreter);

            if (value < 0)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((char)value, f());
        }

        private static void VerifyCheckedShortToNullableChar(short value, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(short)), typeof(char?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile(useInterpreter);

            if (value < 0)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((char)value, f());
        }

        private static void VerifyCheckedShortToDecimal(short value, bool useInterpreter)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(short)), typeof(decimal)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedShortToNullableDecimal(short value, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(short)), typeof(decimal?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedShortToDouble(short value, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(short)), typeof(double)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedShortToNullableDouble(short value, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(short)), typeof(double?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedShortToEnum(short value, bool useInterpreter)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(short)), typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile(useInterpreter);

            Assert.Equal((E)value, f());
        }

        private static void VerifyCheckedShortToNullableEnum(short value, bool useInterpreter)
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(short)), typeof(E?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile(useInterpreter);

            Assert.Equal((E)value, f());
        }

        private static void VerifyCheckedShortToEnumLong(short value, bool useInterpreter)
        {
            Expression<Func<El>> e =
                Expression.Lambda<Func<El>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(short)), typeof(El)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El> f = e.Compile(useInterpreter);

            Assert.Equal((El)value, f());
        }

        private static void VerifyCheckedShortToNullableEnumLong(short value, bool useInterpreter)
        {
            Expression<Func<El?>> e =
                Expression.Lambda<Func<El?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(short)), typeof(El?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?> f = e.Compile(useInterpreter);

            Assert.Equal((El)value, f());
        }

        private static void VerifyCheckedShortToFloat(short value, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(short)), typeof(float)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedShortToNullableFloat(short value, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(short)), typeof(float?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedShortToInt(short value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(short)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedShortToNullableInt(short value, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(short)), typeof(int?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedShortToLong(short value, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(short)), typeof(long)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedShortToNullableLong(short value, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(short)), typeof(long?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedShortToSByte(short value, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(short)), typeof(sbyte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile(useInterpreter);

            if (value < sbyte.MinValue | value > sbyte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((sbyte)value, f());
        }

        private static void VerifyCheckedShortToNullableSByte(short value, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(short)), typeof(sbyte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile(useInterpreter);

            if (value < sbyte.MinValue | value > sbyte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((sbyte)value, f());
        }

        private static void VerifyCheckedShortToShort(short value, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(short)), typeof(short)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedShortToNullableShort(short value, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(short)), typeof(short?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedShortToUInt(short value, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(short)), typeof(uint)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            if (value < 0)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((uint)value, f());
        }

        private static void VerifyCheckedShortToNullableUInt(short value, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(short)), typeof(uint?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);

            if (value < 0)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((uint)value, f());
        }

        private static void VerifyCheckedShortToULong(short value, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(short)), typeof(ulong)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);

            if (value < 0)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((ulong)value, f());
        }

        private static void VerifyCheckedShortToNullableULong(short value, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(short)), typeof(ulong?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);

            if (value < 0)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((ulong)value, f());
        }

        private static void VerifyCheckedShortToUShort(short value, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(short)), typeof(ushort)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);

            if (value < 0)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((ushort)value, f());
        }

        private static void VerifyCheckedShortToNullableUShort(short value, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(short)), typeof(ushort?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);

            if (value < 0)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((ushort)value, f());
        }

        private static void VerifyCheckedNullableShortToByte(short? value, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(short?)), typeof(byte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Throws<InvalidOperationException>(() => f());
            else if (value < 0 | value > byte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableShortToNullableByte(short? value, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(short?)), typeof(byte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Null(f());
            else if (value < 0 | value > byte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((byte)value, f());
        }

        private static void VerifyCheckedNullableShortToChar(short? value, bool useInterpreter)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(short?)), typeof(char)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Throws<InvalidOperationException>(() => f());
            else if (value < 0 | value > char.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((char)value.GetValueOrDefault(), f());
        }

        private static void VerifyCheckedNullableShortToNullableChar(short? value, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(short?)), typeof(char?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile(useInterpreter);

            if (value < 0 | value > char.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((char?)value, f());
        }

        private static void VerifyCheckedNullableShortToDecimal(short? value, bool useInterpreter)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(short?)), typeof(decimal)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableShortToNullableDecimal(short? value, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(short?)), typeof(decimal?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableShortToDouble(short? value, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(short?)), typeof(double)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableShortToNullableDouble(short? value, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(short?)), typeof(double?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableShortToEnum(short? value, bool useInterpreter)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(short?)), typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal((E)value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableShortToNullableEnum(short? value, bool useInterpreter)
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(short?)), typeof(E?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile(useInterpreter);

            Assert.Equal((E?)value, f());
        }

        private static void VerifyCheckedNullableShortToEnumLong(short? value, bool useInterpreter)
        {
            Expression<Func<El>> e =
                Expression.Lambda<Func<El>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(short?)), typeof(El)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal((El)value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableShortToNullableEnumLong(short? value, bool useInterpreter)
        {
            Expression<Func<El?>> e =
                Expression.Lambda<Func<El?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(short?)), typeof(El?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?> f = e.Compile(useInterpreter);

            Assert.Equal((El?)value, f());
        }

        private static void VerifyCheckedNullableShortToFloat(short? value, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(short?)), typeof(float)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableShortToNullableFloat(short? value, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(short?)), typeof(float?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableShortToInt(short? value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(short?)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableShortToNullableInt(short? value, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(short?)), typeof(int?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableShortToLong(short? value, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(short?)), typeof(long)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableShortToNullableLong(short? value, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(short?)), typeof(long?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableShortToSByte(short? value, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(short?)), typeof(sbyte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                short unboxed = value.GetValueOrDefault();
                if (unboxed < sbyte.MinValue | unboxed > sbyte.MaxValue)
                    Assert.Throws<OverflowException>(() => f());
                else
                    Assert.Equal((sbyte)unboxed, f());
            }
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableShortToNullableSByte(short? value, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(short?)), typeof(sbyte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Null(f());
            else if (value < sbyte.MinValue | value > sbyte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((sbyte)value, f());
        }

        private static void VerifyCheckedNullableShortToShort(short? value, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(short?)), typeof(short)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableShortToNullableShort(short? value, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(short?)), typeof(short?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableShortToUInt(short? value, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(short?)), typeof(uint)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Throws<InvalidOperationException>(() => f());
            else if (value < 0 | value > uint.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((uint)value.GetValueOrDefault(), f());
        }

        private static void VerifyCheckedNullableShortToNullableUInt(short? value, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(short?)), typeof(uint?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);

            if (value < 0 | value > uint.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((uint?)value, f());
        }

        private static void VerifyCheckedNullableShortToULong(short? value, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(short?)), typeof(ulong)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Throws<InvalidOperationException>(() => f());
            else if (value < 0)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((ulong)value.GetValueOrDefault(), f());
        }

        private static void VerifyCheckedNullableShortToNullableULong(short? value, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(short?)), typeof(ulong?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);

            if (value < 0)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((ulong?)value, f());
        }

        private static void VerifyCheckedNullableShortToUShort(short? value, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(short?)), typeof(ushort)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Throws<InvalidOperationException>(() => f());
            else if (value.GetValueOrDefault() < 0)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((ushort)value.GetValueOrDefault(), f());
        }

        private static void VerifyCheckedNullableShortToNullableUShort(short? value, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(short?)), typeof(ushort?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);

            if (value.GetValueOrDefault() < 0)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((ushort?)value, f());
        }

        private static void VerifyCheckedUIntToByte(uint value, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(uint)), typeof(byte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile(useInterpreter);

            if (value > byte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((byte)value, f());
        }

        private static void VerifyCheckedUIntToNullableByte(uint value, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(uint)), typeof(byte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile(useInterpreter);

            if (value > byte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((byte)value, f());
        }

        private static void VerifyCheckedUIntToChar(uint value, bool useInterpreter)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(uint)), typeof(char)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile(useInterpreter);

            if (value > char.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal(value, f());
        }

        private static void VerifyCheckedUIntToNullableChar(uint value, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(uint)), typeof(char?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile(useInterpreter);

            if (value > char.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((char)value, f());
        }

        private static void VerifyCheckedUIntToDecimal(uint value, bool useInterpreter)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(uint)), typeof(decimal)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedUIntToNullableDecimal(uint value, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(uint)), typeof(decimal?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedUIntToDouble(uint value, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(uint)), typeof(double)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedUIntToNullableDouble(uint value, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(uint)), typeof(double?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedUIntToEnum(uint value, bool useInterpreter)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(uint)), typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile(useInterpreter);

            if (value > int.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((E)value, f());
        }

        private static void VerifyCheckedUIntToNullableEnum(uint value, bool useInterpreter)
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(uint)), typeof(E?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile(useInterpreter);

            if (value > int.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((E)value, f());
        }

        private static void VerifyCheckedUIntToEnumLong(uint value, bool useInterpreter)
        {
            Expression<Func<El>> e =
                Expression.Lambda<Func<El>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(uint)), typeof(El)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El> f = e.Compile(useInterpreter);

            Assert.Equal((El)value, f());
        }

        private static void VerifyCheckedUIntToNullableEnumLong(uint value, bool useInterpreter)
        {
            Expression<Func<El?>> e =
                Expression.Lambda<Func<El?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(uint)), typeof(El?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?> f = e.Compile(useInterpreter);

            Assert.Equal((El)value, f());
        }

        private static void VerifyCheckedUIntToFloat(uint value, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(uint)), typeof(float)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedUIntToNullableFloat(uint value, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(uint)), typeof(float?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedUIntToInt(uint value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(uint)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            if (value > int.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((int)value, f());
        }

        private static void VerifyCheckedUIntToNullableInt(uint value, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(uint)), typeof(int?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);

            if (value > int.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((int)value, f());
        }

        private static void VerifyCheckedUIntToLong(uint value, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(uint)), typeof(long)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedUIntToNullableLong(uint value, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(uint)), typeof(long?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedUIntToSByte(uint value, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(uint)), typeof(sbyte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile(useInterpreter);

            if (value > sbyte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((sbyte)value, f());
        }

        private static void VerifyCheckedUIntToNullableSByte(uint value, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(uint)), typeof(sbyte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile(useInterpreter);

            if (value > sbyte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((sbyte)value, f());
        }

        private static void VerifyCheckedUIntToShort(uint value, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(uint)), typeof(short)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);

            if (value > short.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((short)value, f());
        }

        private static void VerifyCheckedUIntToNullableShort(uint value, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(uint)), typeof(short?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);

            if (value > short.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((short)value, f());
        }

        private static void VerifyCheckedUIntToUInt(uint value, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(uint)), typeof(uint)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedUIntToNullableUInt(uint value, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(uint)), typeof(uint?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedUIntToULong(uint value, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(uint)), typeof(ulong)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedUIntToNullableULong(uint value, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(uint)), typeof(ulong?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedUIntToUShort(uint value, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(uint)), typeof(ushort)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);

            if (value > ushort.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal(value, f());
        }

        private static void VerifyCheckedUIntToNullableUShort(uint value, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(uint)), typeof(ushort?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);

            if (value > ushort.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((ushort)value, f());
        }

        private static void VerifyCheckedNullableUIntToByte(uint? value, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(uint?)), typeof(byte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Throws<InvalidOperationException>(() => f());
            else if (value.GetValueOrDefault() > byte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal(value.GetValueOrDefault(), f());
        }

        private static void VerifyCheckedNullableUIntToNullableByte(uint? value, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(uint?)), typeof(byte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Null(f());
            else if (value > byte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((byte)value, f());
        }

        private static void VerifyCheckedNullableUIntToChar(uint? value, bool useInterpreter)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(uint?)), typeof(char)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Throws<InvalidOperationException>(() => f());
            else if (value.GetValueOrDefault() > char.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal(value.GetValueOrDefault(), f());
        }

        private static void VerifyCheckedNullableUIntToNullableChar(uint? value, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(uint?)), typeof(char?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile(useInterpreter);

            if (value.GetValueOrDefault() > char.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableUIntToDecimal(uint? value, bool useInterpreter)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(uint?)), typeof(decimal)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableUIntToNullableDecimal(uint? value, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(uint?)), typeof(decimal?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableUIntToDouble(uint? value, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(uint?)), typeof(double)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableUIntToNullableDouble(uint? value, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(uint?)), typeof(double?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableUIntToEnum(uint? value, bool useInterpreter)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(uint?)), typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                if (value.GetValueOrDefault() > int.MaxValue)
                    Assert.Throws<OverflowException>(() => f());
                else
                    Assert.Equal((E)value.GetValueOrDefault(), f());
            }
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableUIntToNullableEnum(uint? value, bool useInterpreter)
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(uint?)), typeof(E?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile(useInterpreter);

            if (value > int.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((E?)value, f());
        }

        private static void VerifyCheckedNullableUIntToEnumLong(uint? value, bool useInterpreter)
        {
            Expression<Func<El>> e =
                Expression.Lambda<Func<El>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(uint?)), typeof(El)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal((El)value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableUIntToNullableEnumLong(uint? value, bool useInterpreter)
        {
            Expression<Func<El?>> e =
                Expression.Lambda<Func<El?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(uint?)), typeof(El?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?> f = e.Compile(useInterpreter);

            Assert.Equal((El?)value, f());
        }

        private static void VerifyCheckedNullableUIntToFloat(uint? value, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(uint?)), typeof(float)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableUIntToNullableFloat(uint? value, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(uint?)), typeof(float?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableUIntToInt(uint? value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(uint?)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Throws<InvalidOperationException>(() => f());
            else if (value > int.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((int)value, f());
        }

        private static void VerifyCheckedNullableUIntToNullableInt(uint? value, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(uint?)), typeof(int?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);

            if (value > int.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((int?)value, f());
        }

        private static void VerifyCheckedNullableUIntToLong(uint? value, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(uint?)), typeof(long)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Throws<InvalidOperationException>(() => f());
            else
                Assert.Equal((long)value, f());
        }

        private static void VerifyCheckedNullableUIntToNullableLong(uint? value, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(uint?)), typeof(long?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableUIntToSByte(uint? value, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(uint?)), typeof(sbyte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                uint unboxed = value.GetValueOrDefault();
                if (unboxed > sbyte.MaxValue)
                    Assert.Throws<OverflowException>(() => f());
                else
                    Assert.Equal((sbyte)unboxed, f());
            }
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableUIntToNullableSByte(uint? value, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(uint?)), typeof(sbyte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Null(f());
            else if (value > sbyte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((sbyte)value, f());
        }

        private static void VerifyCheckedNullableUIntToShort(uint? value, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(uint?)), typeof(short)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Throws<InvalidOperationException>(() => f());
            else if (value.GetValueOrDefault() > short.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((short)value.GetValueOrDefault(), f());
        }

        private static void VerifyCheckedNullableUIntToNullableShort(uint? value, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(uint?)), typeof(short?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);

            if (value.GetValueOrDefault() > short.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((short?)value, f());
        }

        private static void VerifyCheckedNullableUIntToUInt(uint? value, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(uint?)), typeof(uint)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableUIntToNullableUInt(uint? value, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(uint?)), typeof(uint?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableUIntToULong(uint? value, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(uint?)), typeof(ulong)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableUIntToNullableULong(uint? value, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(uint?)), typeof(ulong?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableUIntToUShort(uint? value, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(uint?)), typeof(ushort)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Throws<InvalidOperationException>(() => f());
            else if (value.GetValueOrDefault() > ushort.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableUIntToNullableUShort(uint? value, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(uint?)), typeof(ushort?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);

            if (value.GetValueOrDefault() > ushort.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal(value, f());
        }

        private static void VerifyCheckedULongToByte(ulong value, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ulong)), typeof(byte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile(useInterpreter);

            if (value > byte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((byte)value, f());
        }

        private static void VerifyCheckedULongToNullableByte(ulong value, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ulong)), typeof(byte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile(useInterpreter);

            if (value > byte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((byte)value, f());
        }

        private static void VerifyCheckedULongToChar(ulong value, bool useInterpreter)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ulong)), typeof(char)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile(useInterpreter);

            if (value > char.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal(value, f());
        }

        private static void VerifyCheckedULongToNullableChar(ulong value, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ulong)), typeof(char?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile(useInterpreter);

            if (value > char.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((char)value, f());
        }

        private static void VerifyCheckedULongToDecimal(ulong value, bool useInterpreter)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ulong)), typeof(decimal)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedULongToNullableDecimal(ulong value, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ulong)), typeof(decimal?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedULongToDouble(ulong value, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ulong)), typeof(double)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedULongToNullableDouble(ulong value, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ulong)), typeof(double?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedULongToEnum(ulong value, bool useInterpreter)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ulong)), typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile(useInterpreter);

            if (value > int.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((E)value, f());
        }

        private static void VerifyCheckedULongToNullableEnum(ulong value, bool useInterpreter)
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ulong)), typeof(E?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile(useInterpreter);

            if (value > int.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((E)value, f());
        }

        private static void VerifyCheckedULongToEnumLong(ulong value, bool useInterpreter)
        {
            Expression<Func<El>> e =
                Expression.Lambda<Func<El>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ulong)), typeof(El)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El> f = e.Compile(useInterpreter);

            if (value > long.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((El)value, f());
        }

        private static void VerifyCheckedULongToNullableEnumLong(ulong value, bool useInterpreter)
        {
            Expression<Func<El?>> e =
                Expression.Lambda<Func<El?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ulong)), typeof(El?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?> f = e.Compile(useInterpreter);

            if (value > long.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((El)value, f());
        }

        private static void VerifyCheckedULongToFloat(ulong value, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ulong)), typeof(float)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedULongToNullableFloat(ulong value, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ulong)), typeof(float?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedULongToInt(ulong value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ulong)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            if (value > int.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((int)value, f());
        }

        private static void VerifyCheckedULongToNullableInt(ulong value, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ulong)), typeof(int?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);

            if (value > int.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((int)value, f());
        }

        private static void VerifyCheckedULongToLong(ulong value, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ulong)), typeof(long)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);

            if (value > long.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((long)value, f());
        }

        private static void VerifyCheckedULongToNullableLong(ulong value, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ulong)), typeof(long?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);

            if (value > long.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((long)value, f());
        }

        private static void VerifyCheckedULongToSByte(ulong value, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ulong)), typeof(sbyte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile(useInterpreter);

            if (value > (ulong)sbyte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((sbyte)value, f());
        }

        private static void VerifyCheckedULongToNullableSByte(ulong value, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ulong)), typeof(sbyte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile(useInterpreter);

            if (value > (ulong)sbyte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((sbyte)value, f());
        }

        private static void VerifyCheckedULongToShort(ulong value, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ulong)), typeof(short)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);

            if (value > (ulong)short.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((short)value, f());
        }

        private static void VerifyCheckedULongToNullableShort(ulong value, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ulong)), typeof(short?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);

            if (value > (ulong)short.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((short)value, f());
        }

        private static void VerifyCheckedULongToUInt(ulong value, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ulong)), typeof(uint)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            if (value > uint.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal(value, f());
        }

        private static void VerifyCheckedULongToNullableUInt(ulong value, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ulong)), typeof(uint?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);

            if (value > uint.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((uint)value, f());
        }

        private static void VerifyCheckedULongToULong(ulong value, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ulong)), typeof(ulong)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedULongToNullableULong(ulong value, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ulong)), typeof(ulong?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedULongToUShort(ulong value, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ulong)), typeof(ushort)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);

            if (value > ushort.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal(value, f());
        }

        private static void VerifyCheckedULongToNullableUShort(ulong value, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ulong)), typeof(ushort?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);

            if (value > ushort.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((ushort)value, f());
        }

        private static void VerifyCheckedNullableULongToByte(ulong? value, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ulong?)), typeof(byte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                if (value.GetValueOrDefault() > byte.MaxValue)
                    Assert.Throws<OverflowException>(() => f());
                else
                    Assert.Equal((byte)value.GetValueOrDefault(), f());
            }
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableULongToNullableByte(ulong? value, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ulong?)), typeof(byte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Null(f());
            else if (value > byte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((byte)value, f());
        }

        private static void VerifyCheckedNullableULongToChar(ulong? value, bool useInterpreter)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ulong?)), typeof(char)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Throws<InvalidOperationException>(() => f());
            else if (value.GetValueOrDefault() > char.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal(value.GetValueOrDefault(), f());
        }

        private static void VerifyCheckedNullableULongToNullableChar(ulong? value, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ulong?)), typeof(char?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile(useInterpreter);

            if (value.GetValueOrDefault() > char.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableULongToDecimal(ulong? value, bool useInterpreter)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ulong?)), typeof(decimal)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableULongToNullableDecimal(ulong? value, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ulong?)), typeof(decimal?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableULongToDouble(ulong? value, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ulong?)), typeof(double)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal((double)value, f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableULongToNullableDouble(ulong? value, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ulong?)), typeof(double?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableULongToEnum(ulong? value, bool useInterpreter)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ulong?)), typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                if (value.GetValueOrDefault() > int.MaxValue)
                    Assert.Throws<OverflowException>(() => f());
                else
                    Assert.Equal((E)value.GetValueOrDefault(), f());
            }
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableULongToNullableEnum(ulong? value, bool useInterpreter)
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ulong?)), typeof(E?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile(useInterpreter);

            if (value.GetValueOrDefault() > int.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((E?)value, f());
        }

        private static void VerifyCheckedNullableULongToEnumLong(ulong? value, bool useInterpreter)
        {
            Expression<Func<El>> e =
                Expression.Lambda<Func<El>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ulong?)), typeof(El)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                if (value > long.MaxValue)
                    Assert.Throws<OverflowException>(() => f());
                else
                    Assert.Equal((El)value.GetValueOrDefault(), f());
            }
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableULongToNullableEnumLong(ulong? value, bool useInterpreter)
        {
            Expression<Func<El?>> e =
                Expression.Lambda<Func<El?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ulong?)), typeof(El?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?> f = e.Compile(useInterpreter);

            if (value > long.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((El?)value, f());
        }

        private static void VerifyCheckedNullableULongToFloat(ulong? value, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ulong?)), typeof(float)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableULongToNullableFloat(ulong? value, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ulong?)), typeof(float?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableULongToInt(ulong? value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ulong?)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Throws<InvalidOperationException>(() => f());
            else if (value.GetValueOrDefault() > int.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((int)value.GetValueOrDefault(), f());
        }

        private static void VerifyCheckedNullableULongToNullableInt(ulong? value, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ulong?)), typeof(int?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);

            if (value > int.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((int?)value, f());
        }

        private static void VerifyCheckedNullableULongToLong(ulong? value, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ulong?)), typeof(long)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Throws<InvalidOperationException>(() => f());
            else if (value > long.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((long)value.GetValueOrDefault(), f());
        }

        private static void VerifyCheckedNullableULongToNullableLong(ulong? value, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ulong?)), typeof(long?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);

            if (value.GetValueOrDefault() > long.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((long?)value, f());
        }

        private static void VerifyCheckedNullableULongToSByte(ulong? value, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ulong?)), typeof(sbyte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                ulong unboxed = value.GetValueOrDefault();
                if (unboxed > (ulong)sbyte.MaxValue)
                    Assert.Throws<OverflowException>(() => f());
                else
                    Assert.Equal((sbyte)unboxed, f());
            }
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableULongToNullableSByte(ulong? value, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ulong?)), typeof(sbyte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Null(f());
            else if (value > (long)sbyte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((sbyte)value, f());
        }

        private static void VerifyCheckedNullableULongToShort(ulong? value, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ulong?)), typeof(short)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Throws<InvalidOperationException>(() => f());
            else if (value.GetValueOrDefault() > (ulong)short.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((short)value.GetValueOrDefault(), f());
        }

        private static void VerifyCheckedNullableULongToNullableShort(ulong? value, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ulong?)), typeof(short?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);

            if (value.GetValueOrDefault() > (ulong)short.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((short?)value, f());
        }

        private static void VerifyCheckedNullableULongToUInt(ulong? value, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ulong?)), typeof(uint)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Throws<InvalidOperationException>(() => f());
            else if (value.GetValueOrDefault() > uint.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal(value.GetValueOrDefault(), f());
        }

        private static void VerifyCheckedNullableULongToNullableUInt(ulong? value, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ulong?)), typeof(uint?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);

            if (value.GetValueOrDefault() > uint.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableULongToULong(ulong? value, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ulong?)), typeof(ulong)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableULongToNullableULong(ulong? value, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ulong?)), typeof(ulong?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableULongToUShort(ulong? value, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ulong?)), typeof(ushort)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Throws<InvalidOperationException>(() => f());
            else if (value.GetValueOrDefault() > ushort.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal(value.GetValueOrDefault(), f());
        }

        private static void VerifyCheckedNullableULongToNullableUShort(ulong? value, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ulong?)), typeof(ushort?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);

            if (value.GetValueOrDefault() > ushort.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal(value, f());
        }

        private static void VerifyCheckedUShortToByte(ushort value, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ushort)), typeof(byte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile(useInterpreter);

            if (value > byte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((byte)value, f());
        }

        private static void VerifyCheckedUShortToNullableByte(ushort value, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ushort)), typeof(byte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile(useInterpreter);

            if (value > byte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((byte)value, f());
        }

        private static void VerifyCheckedUShortToChar(ushort value, bool useInterpreter)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ushort)), typeof(char)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile(useInterpreter);

            Assert.Equal((char)value, f());
        }

        private static void VerifyCheckedUShortToNullableChar(ushort value, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ushort)), typeof(char?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile(useInterpreter);

            Assert.Equal((char?)value, f());
        }

        private static void VerifyCheckedUShortToDecimal(ushort value, bool useInterpreter)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ushort)), typeof(decimal)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedUShortToNullableDecimal(ushort value, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ushort)), typeof(decimal?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedUShortToDouble(ushort value, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ushort)), typeof(double)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedUShortToNullableDouble(ushort value, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ushort)), typeof(double?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedUShortToEnum(ushort value, bool useInterpreter)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ushort)), typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile(useInterpreter);

            Assert.Equal((E)value, f());
        }

        private static void VerifyCheckedUShortToNullableEnum(ushort value, bool useInterpreter)
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ushort)), typeof(E?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile(useInterpreter);

            Assert.Equal((E)value, f());
        }

        private static void VerifyCheckedUShortToEnumLong(ushort value, bool useInterpreter)
        {
            Expression<Func<El>> e =
                Expression.Lambda<Func<El>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ushort)), typeof(El)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El> f = e.Compile(useInterpreter);

            Assert.Equal((El)value, f());
        }

        private static void VerifyCheckedUShortToNullableEnumLong(ushort value, bool useInterpreter)
        {
            Expression<Func<El?>> e =
                Expression.Lambda<Func<El?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ushort)), typeof(El?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?> f = e.Compile(useInterpreter);

            Assert.Equal((El)value, f());
        }

        private static void VerifyCheckedUShortToFloat(ushort value, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ushort)), typeof(float)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedUShortToNullableFloat(ushort value, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ushort)), typeof(float?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedUShortToInt(ushort value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ushort)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedUShortToNullableInt(ushort value, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ushort)), typeof(int?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedUShortToLong(ushort value, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ushort)), typeof(long)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedUShortToNullableLong(ushort value, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ushort)), typeof(long?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedUShortToSByte(ushort value, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ushort)), typeof(sbyte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile(useInterpreter);

            if (value > sbyte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((sbyte)value, f());
        }

        private static void VerifyCheckedUShortToNullableSByte(ushort value, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ushort)), typeof(sbyte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile(useInterpreter);

            if (value > sbyte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((sbyte)value, f());
        }

        private static void VerifyCheckedUShortToShort(ushort value, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ushort)), typeof(short)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);

            if (value > short.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((short)value, f());
        }

        private static void VerifyCheckedUShortToNullableShort(ushort value, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ushort)), typeof(short?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);

            if (value > short.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((short)value, f());
        }

        private static void VerifyCheckedUShortToUInt(ushort value, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ushort)), typeof(uint)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedUShortToNullableUInt(ushort value, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ushort)), typeof(uint?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedUShortToULong(ushort value, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ushort)), typeof(ulong)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedUShortToNullableULong(ushort value, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ushort)), typeof(ulong?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedUShortToUShort(ushort value, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ushort)), typeof(ushort)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedUShortToNullableUShort(ushort value, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ushort)), typeof(ushort?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableUShortToByte(ushort? value, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ushort?)), typeof(byte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Throws<InvalidOperationException>(() => f());
            else if (value.GetValueOrDefault() > byte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableUShortToNullableByte(ushort? value, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ushort?)), typeof(byte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Null(f());
            else if (value > byte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((byte)value, f());
        }

        private static void VerifyCheckedNullableUShortToChar(ushort? value, bool useInterpreter)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ushort?)), typeof(char)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Throws<InvalidOperationException>(() => f());
            else if (value.GetValueOrDefault() > char.MaxValue)
                Assert.Throws<InvalidOperationException>(() => f());
            else
                Assert.Equal(value.GetValueOrDefault(), f());
        }

        private static void VerifyCheckedNullableUShortToNullableChar(ushort? value, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ushort?)), typeof(char?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile(useInterpreter);

            Assert.Equal((char?)value, f());
        }

        private static void VerifyCheckedNullableUShortToDecimal(ushort? value, bool useInterpreter)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ushort?)), typeof(decimal)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableUShortToNullableDecimal(ushort? value, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ushort?)), typeof(decimal?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableUShortToDouble(ushort? value, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ushort?)), typeof(double)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableUShortToNullableDouble(ushort? value, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ushort?)), typeof(double?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableUShortToEnum(ushort? value, bool useInterpreter)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ushort?)), typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal((E)value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableUShortToNullableEnum(ushort? value, bool useInterpreter)
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ushort?)), typeof(E?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile(useInterpreter);

            Assert.Equal((E?)value, f());
        }

        private static void VerifyCheckedNullableUShortToEnumLong(ushort? value, bool useInterpreter)
        {
            Expression<Func<El>> e =
                Expression.Lambda<Func<El>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ushort?)), typeof(El)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal((El)value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableUShortToNullableEnumLong(ushort? value, bool useInterpreter)
        {
            Expression<Func<El?>> e =
                Expression.Lambda<Func<El?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ushort?)), typeof(El?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?> f = e.Compile(useInterpreter);

            Assert.Equal((El?)value, f());
        }

        private static void VerifyCheckedNullableUShortToFloat(ushort? value, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ushort?)), typeof(float)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableUShortToNullableFloat(ushort? value, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ushort?)), typeof(float?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableUShortToInt(ushort? value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ushort?)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableUShortToNullableInt(ushort? value, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ushort?)), typeof(int?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableUShortToLong(ushort? value, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ushort?)), typeof(long)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableUShortToNullableLong(ushort? value, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ushort?)), typeof(long?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableUShortToSByte(ushort? value, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ushort?)), typeof(sbyte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                ushort unboxed = value.GetValueOrDefault();
                if (unboxed > sbyte.MaxValue)
                    Assert.Throws<OverflowException>(() => f());
                else
                    Assert.Equal((sbyte)unboxed, f());
            }
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableUShortToNullableSByte(ushort? value, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ushort?)), typeof(sbyte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Null(f());
            else if (value > sbyte.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((sbyte)value, f());
        }

        private static void VerifyCheckedNullableUShortToShort(ushort? value, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ushort?)), typeof(short)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);

            if (!value.HasValue)
                Assert.Throws<InvalidOperationException>(() => f());
            else if (value > short.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((short)value.GetValueOrDefault(), f());
        }

        private static void VerifyCheckedNullableUShortToNullableShort(ushort? value, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ushort?)), typeof(short?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);

            if (value.GetValueOrDefault() > short.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal((short?)value, f());
        }

        private static void VerifyCheckedNullableUShortToUInt(ushort? value, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ushort?)), typeof(uint)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableUShortToNullableUInt(ushort? value, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ushort?)), typeof(uint?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableUShortToULong(ushort? value, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ushort?)), typeof(ulong)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableUShortToNullableULong(ushort? value, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ushort?)), typeof(ulong?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCheckedNullableUShortToUShort(ushort? value, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ushort?)), typeof(ushort)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyCheckedNullableUShortToNullableUShort(ushort? value, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.ConvertChecked(Expression.Constant(value, typeof(ushort?)), typeof(ushort?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        #endregion

        [Fact]
        public static void OpenGenericnType()
        {
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.ConvertChecked(Expression.Constant(null), typeof(List<>)));
        }

        [Fact]
        public static void TypeContainingGenericParameters()
        {
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.ConvertChecked(Expression.Constant(null), typeof(List<>.Enumerator)));
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.ConvertChecked(Expression.Constant(null), typeof(List<>).MakeGenericType(typeof(List<>))));
        }

        [Fact]
        public static void ByRefType()
        {
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.ConvertChecked(Expression.Constant(null), typeof(object).MakeByRefType()));
        }

        [Fact]
        public static void PointerType()
        {
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.ConvertChecked(Expression.Constant(null), typeof(int*)));
        }

        public static IEnumerable<object[]> Conversions()
        {
            yield return new object[] { 3, 3 };
            yield return new object[] { (byte)3, 3 };
            yield return new object[] { 3, 3.0 };
            yield return new object[] { 3.0, 3 };
            yield return new object[] { 24910, (short)24910 };
        }

        [Theory, PerCompilationType(nameof(Conversions))]
        public static void ConvertCheckedMakeUnary(object source, object result, bool useInterpreter)
        {
            LambdaExpression lambda = Expression.Lambda(
                Expression.MakeUnary(ExpressionType.ConvertChecked, Expression.Constant(source), result.GetType())
                );
            Delegate del = lambda.Compile(useInterpreter);
            Assert.Equal(result, del.DynamicInvoke());
        }

        [Fact]
        public static void CannotConvertNonVoidToVoid()
        {
            Assert.Throws<InvalidOperationException>(() => Expression.ConvertChecked(Expression.Constant(1), typeof(void)));
            Assert.Throws<InvalidOperationException>(() => Expression.ConvertChecked(Expression.Constant("a"), typeof(void)));
            Assert.Throws<InvalidOperationException>(() => Expression.ConvertChecked(Expression.Constant(DateTime.MinValue), typeof(void)));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertVoidToVoid(bool useInterpreter)
        {
            Action act = Expression.Lambda<Action>(Expression.ConvertChecked(Expression.Empty(), typeof(void)))
                .Compile(useInterpreter);
            act();
        }

        interface IInterface
        {
        }

        class NonSealed
        {
        }

        class Derived : NonSealed, IInterface
        {
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void NonSealedArrayToIfaceArray(bool useInterpreter)
        {
            checked
            {
                Expression<Func<NonSealed[][], IInterface[][]>> e = a => (IInterface[][])a;
                Func<NonSealed[][], IInterface[][]> f = e.Compile(useInterpreter);
                Derived[][] arr = new[] {new[] {new Derived(), new Derived(), new Derived(), new Derived()}};
                Assert.Same(arr, f(arr));
                Assert.Null(f(null));
                Assert.Throws<InvalidCastException>(() => f(Array.Empty<NonSealed[]>()));
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void IfaceArrayToNonSealedArray(bool useInterpreter)
        {
            checked
            {
                Expression<Func<IInterface[][], NonSealed[][]>> e = a => (NonSealed[][])a;
                Func<IInterface[][], NonSealed[][]> f = e.Compile(useInterpreter);
                Derived[][] arr = new[] {new[] {new Derived(), new Derived(), new Derived(), new Derived()}};
                Assert.Same(arr, f(arr));
                Assert.Null(f(null));
                Assert.Throws<InvalidCastException>(() => f(Array.Empty<IInterface[]>()));
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void NonSealedICollectionToIfaceArray(bool useInterpreter)
        {
            checked
            {
                Expression<Func<ICollection<NonSealed[]>, IInterface[][]>> e = a => (IInterface[][])a;
                Func<ICollection<NonSealed[]>, IInterface[][]> f = e.Compile(useInterpreter);
                Derived[][] arr = new[] {new[] {new Derived(), new Derived(), new Derived(), new Derived()}};
                Assert.Same(arr, f(arr));
                Assert.Null(f(null));
                Assert.Throws<InvalidCastException>(() => f(Array.Empty<NonSealed[]>()));
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void IfaceArrayToNonSealedIList(bool useInterpreter)
        {
            checked
            {
                Expression<Func<IInterface[][], IList<NonSealed>[]>> e = a => (IList<NonSealed>[])a;
                Func<IInterface[][], IList<NonSealed>[]> f = e.Compile(useInterpreter);
                Derived[][] arr = new[] {new[] {new Derived(), new Derived(), new Derived(), new Derived()}};
                Assert.Same(arr, f(arr));
                Assert.Null(f(null));
                Assert.Throws<InvalidCastException>(() => f(Array.Empty<IInterface[]>()));
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void NonSealedArrayToIfaceIEnumerable(bool useInterpreter)
        {
            checked
            {
                Expression<Func<NonSealed[][], IEnumerable<IInterface>[]>> e = a => (IEnumerable<IInterface>[])a;
                Func<NonSealed[][], IEnumerable<IInterface>[]> f = e.Compile(useInterpreter);
                Derived[][] arr = new[] {new[] {new Derived(), new Derived(), new Derived(), new Derived()}};
                Assert.Same(arr, f(arr));
                Assert.Null(f(null));
                Assert.Throws<InvalidCastException>(() => f(Array.Empty<NonSealed[]>()));
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void IfaceIReadonlyCollectionToNonSealedArray(bool useInterpreter)
        {
            checked
            {
                Expression<Func<IReadOnlyCollection<IInterface>[], NonSealed[][]>> e = a => (NonSealed[][])a;
                Func<IReadOnlyCollection<IInterface>[], NonSealed[][]> f = e.Compile(useInterpreter);
                Derived[][] arr = new[] {new[] {new Derived(), new Derived(), new Derived(), new Derived()}};
                Assert.Same(arr, f(arr));
                Assert.Null(f(null));
                Assert.Throws<InvalidCastException>(() => f(Array.Empty<IInterface[]>()));
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void IFaceIListToObjectArray(bool useInterpreter)
        {
            checked
            {
                Expression<Func<IList<IInterface[]>, object[][]>> e = a => (object[][])a;
                Func<IList<IInterface[]>, object[][]> f = e.Compile(useInterpreter);
                Derived[][] arr = new[] {new[] {new Derived(), new Derived(), new Derived(), new Derived()}};
                Assert.Same(arr, f(arr));
                Assert.Null(f(null));
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ObjectIListToIFaceArray(bool useInterpreter)
        {
            checked
            {
                Expression<Func<IList<object[]>, IInterface[][]>> e = a => (IInterface[][])a;
                Func<IList<object[]>, IInterface[][]> f = e.Compile(useInterpreter);
                Derived[][] arr = new[] {new[] {new Derived(), new Derived(), new Derived(), new Derived()}};
                Assert.Same(arr, f(arr));
                Assert.Null(f(null));
                Assert.Throws<InvalidCastException>(() => f(Array.Empty<string[]>()));
            }
        }

        [Fact]
        public static void IfaceToNonSZArray()
        {
            Assert.Throws<InvalidOperationException>(
                () => Expression.ConvertChecked(Expression.Default(typeof(IList<NonSealed>[])), typeof(NonSealed[,][])));
        }

        [Fact]
        public static void NonSZArrayToIface()
        {
            Assert.Throws<InvalidOperationException>(
                () => Expression.ConvertChecked(Expression.Default(typeof(NonSealed[,][])), typeof(IList<NonSealed>[])));
        }

        [Fact]
        public static void ArrayToNonArrayCompatibleIFace()
        {
            Assert.Throws<InvalidOperationException>(
                () => Expression.ConvertChecked(Expression.Default(typeof(NonSealed[][])), typeof(IEquatable<NonSealed>[])));
            Assert.Throws<InvalidOperationException>(
                () => Expression.ConvertChecked(
                    Expression.Default(typeof(NonSealed[][])), typeof(IDictionary<NonSealed, NonSealed>[])));
        }

        [Fact]
        public static void NonArrayCompatibleIFaceToArray()
        {
            Assert.Throws<InvalidOperationException>(
                () => Expression.ConvertChecked(Expression.Default(typeof(IEquatable<NonSealed>[])), typeof(NonSealed[][])));
            Assert.Throws<InvalidOperationException>(
                () => Expression.ConvertChecked(
                    Expression.Default(typeof(IDictionary<NonSealed, NonSealed>[])), typeof(NonSealed[][])));
        }

        [Fact]
        public static void ArrayToNotRelated()
        {
            Assert.Throws<InvalidOperationException>(
                () => Expression.ConvertChecked(Expression.Default(typeof(NonSealed[][][])), typeof(string[][])));
        }
    }
}
