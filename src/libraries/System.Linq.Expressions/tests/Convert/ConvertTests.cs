// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class ConvertTests
    {
        #region Test methods

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertByteToByteTest(bool useInterpreter)
        {
            foreach (byte value in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyByteToByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertByteToNullableByteTest(bool useInterpreter)
        {
            foreach (byte value in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyByteToNullableByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertByteToCharTest(bool useInterpreter)
        {
            foreach (byte value in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyByteToChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertByteToNullableCharTest(bool useInterpreter)
        {
            foreach (byte value in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyByteToNullableChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertByteToDecimalTest(bool useInterpreter)
        {
            foreach (byte value in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyByteToDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertByteToNullableDecimalTest(bool useInterpreter)
        {
            foreach (byte value in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyByteToNullableDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertByteToDoubleTest(bool useInterpreter)
        {
            foreach (byte value in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyByteToDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertByteToNullableDoubleTest(bool useInterpreter)
        {
            foreach (byte value in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyByteToNullableDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertByteToEnumTest(bool useInterpreter)
        {
            foreach (byte value in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyByteToEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertByteToNullableEnumTest(bool useInterpreter)
        {
            foreach (byte value in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyByteToNullableEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertByteToEnumLongTest(bool useInterpreter)
        {
            foreach (byte value in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyByteToEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertByteToNullableEnumLongTest(bool useInterpreter)
        {
            foreach (byte value in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyByteToNullableEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertByteToFloatTest(bool useInterpreter)
        {
            foreach (byte value in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyByteToFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertByteToNullableFloatTest(bool useInterpreter)
        {
            foreach (byte value in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyByteToNullableFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertByteToIntTest(bool useInterpreter)
        {
            foreach (byte value in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyByteToInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertByteToNullableIntTest(bool useInterpreter)
        {
            foreach (byte value in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyByteToNullableInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertByteToLongTest(bool useInterpreter)
        {
            foreach (byte value in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyByteToLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertByteToNullableLongTest(bool useInterpreter)
        {
            foreach (byte value in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyByteToNullableLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertByteToSByteTest(bool useInterpreter)
        {
            foreach (byte value in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyByteToSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertByteToNullableSByteTest(bool useInterpreter)
        {
            foreach (byte value in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyByteToNullableSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertByteToShortTest(bool useInterpreter)
        {
            foreach (byte value in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyByteToShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertByteToNullableShortTest(bool useInterpreter)
        {
            foreach (byte value in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyByteToNullableShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertByteToUIntTest(bool useInterpreter)
        {
            foreach (byte value in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyByteToUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertByteToNullableUIntTest(bool useInterpreter)
        {
            foreach (byte value in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyByteToNullableUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertByteToULongTest(bool useInterpreter)
        {
            foreach (byte value in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyByteToULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertByteToNullableULongTest(bool useInterpreter)
        {
            foreach (byte value in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyByteToNullableULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertByteToUShortTest(bool useInterpreter)
        {
            foreach (byte value in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyByteToUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertByteToNullableUShortTest(bool useInterpreter)
        {
            foreach (byte value in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyByteToNullableUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableByteToByteTest(bool useInterpreter)
        {
            foreach (byte? value in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyNullableByteToByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableByteToNullableByteTest(bool useInterpreter)
        {
            foreach (byte? value in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyNullableByteToNullableByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableByteToCharTest(bool useInterpreter)
        {
            foreach (byte? value in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyNullableByteToChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableByteToNullableCharTest(bool useInterpreter)
        {
            foreach (byte? value in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyNullableByteToNullableChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableByteToDecimalTest(bool useInterpreter)
        {
            foreach (byte? value in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyNullableByteToDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableByteToNullableDecimalTest(bool useInterpreter)
        {
            foreach (byte? value in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyNullableByteToNullableDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableByteToDoubleTest(bool useInterpreter)
        {
            foreach (byte? value in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyNullableByteToDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableByteToNullableDoubleTest(bool useInterpreter)
        {
            foreach (byte? value in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyNullableByteToNullableDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableByteToEnumTest(bool useInterpreter)
        {
            foreach (byte? value in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyNullableByteToEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableByteToNullableEnumTest(bool useInterpreter)
        {
            foreach (byte? value in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyNullableByteToNullableEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableByteToEnumLongTest(bool useInterpreter)
        {
            foreach (byte? value in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyNullableByteToEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableByteToNullableEnumLongTest(bool useInterpreter)
        {
            foreach (byte? value in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyNullableByteToNullableEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableByteToFloatTest(bool useInterpreter)
        {
            foreach (byte? value in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyNullableByteToFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableByteToNullableFloatTest(bool useInterpreter)
        {
            foreach (byte? value in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyNullableByteToNullableFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableByteToIntTest(bool useInterpreter)
        {
            foreach (byte? value in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyNullableByteToInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableByteToNullableIntTest(bool useInterpreter)
        {
            foreach (byte? value in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyNullableByteToNullableInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableByteToLongTest(bool useInterpreter)
        {
            foreach (byte? value in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyNullableByteToLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableByteToNullableLongTest(bool useInterpreter)
        {
            foreach (byte? value in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyNullableByteToNullableLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableByteToSByteTest(bool useInterpreter)
        {
            foreach (byte? value in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyNullableByteToSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableByteToNullableSByteTest(bool useInterpreter)
        {
            foreach (byte? value in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyNullableByteToNullableSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableByteToShortTest(bool useInterpreter)
        {
            foreach (byte? value in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyNullableByteToShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableByteToNullableShortTest(bool useInterpreter)
        {
            foreach (byte? value in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyNullableByteToNullableShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableByteToUIntTest(bool useInterpreter)
        {
            foreach (byte? value in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyNullableByteToUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableByteToNullableUIntTest(bool useInterpreter)
        {
            foreach (byte? value in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyNullableByteToNullableUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableByteToULongTest(bool useInterpreter)
        {
            foreach (byte? value in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyNullableByteToULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableByteToNullableULongTest(bool useInterpreter)
        {
            foreach (byte? value in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyNullableByteToNullableULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableByteToUShortTest(bool useInterpreter)
        {
            foreach (byte? value in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyNullableByteToUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableByteToNullableUShortTest(bool useInterpreter)
        {
            foreach (byte? value in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyNullableByteToNullableUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCharToByteTest(bool useInterpreter)
        {
            foreach (char value in new char[] { '\0', '\b', 'A', '\uffff' })
            {
                VerifyCharToByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCharToNullableByteTest(bool useInterpreter)
        {
            foreach (char value in new char[] { '\0', '\b', 'A', '\uffff' })
            {
                VerifyCharToNullableByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCharToCharTest(bool useInterpreter)
        {
            foreach (char value in new char[] { '\0', '\b', 'A', '\uffff' })
            {
                VerifyCharToChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCharToNullableCharTest(bool useInterpreter)
        {
            foreach (char value in new char[] { '\0', '\b', 'A', '\uffff' })
            {
                VerifyCharToNullableChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCharToDecimalTest(bool useInterpreter)
        {
            foreach (char value in new char[] { '\0', '\b', 'A', '\uffff' })
            {
                VerifyCharToDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCharToNullableDecimalTest(bool useInterpreter)
        {
            foreach (char value in new char[] { '\0', '\b', 'A', '\uffff' })
            {
                VerifyCharToNullableDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCharToDoubleTest(bool useInterpreter)
        {
            foreach (char value in new char[] { '\0', '\b', 'A', '\uffff' })
            {
                VerifyCharToDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCharToNullableDoubleTest(bool useInterpreter)
        {
            foreach (char value in new char[] { '\0', '\b', 'A', '\uffff' })
            {
                VerifyCharToNullableDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCharToEnumTest(bool useInterpreter)
        {
            foreach (char value in new char[] { '\0', '\b', 'A', '\uffff' })
            {
                VerifyCharToEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCharToNullableEnumTest(bool useInterpreter)
        {
            foreach (char value in new char[] { '\0', '\b', 'A', '\uffff' })
            {
                VerifyCharToNullableEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCharToEnumLongTest(bool useInterpreter)
        {
            foreach (char value in new char[] { '\0', '\b', 'A', '\uffff' })
            {
                VerifyCharToEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCharToNullableEnumLongTest(bool useInterpreter)
        {
            foreach (char value in new char[] { '\0', '\b', 'A', '\uffff' })
            {
                VerifyCharToNullableEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCharToFloatTest(bool useInterpreter)
        {
            foreach (char value in new char[] { '\0', '\b', 'A', '\uffff' })
            {
                VerifyCharToFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCharToNullableFloatTest(bool useInterpreter)
        {
            foreach (char value in new char[] { '\0', '\b', 'A', '\uffff' })
            {
                VerifyCharToNullableFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCharToIntTest(bool useInterpreter)
        {
            foreach (char value in new char[] { '\0', '\b', 'A', '\uffff' })
            {
                VerifyCharToInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCharToNullableIntTest(bool useInterpreter)
        {
            foreach (char value in new char[] { '\0', '\b', 'A', '\uffff' })
            {
                VerifyCharToNullableInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCharToLongTest(bool useInterpreter)
        {
            foreach (char value in new char[] { '\0', '\b', 'A', '\uffff' })
            {
                VerifyCharToLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCharToNullableLongTest(bool useInterpreter)
        {
            foreach (char value in new char[] { '\0', '\b', 'A', '\uffff' })
            {
                VerifyCharToNullableLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCharToSByteTest(bool useInterpreter)
        {
            foreach (char value in new char[] { '\0', '\b', 'A', '\uffff' })
            {
                VerifyCharToSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCharToNullableSByteTest(bool useInterpreter)
        {
            foreach (char value in new char[] { '\0', '\b', 'A', '\uffff' })
            {
                VerifyCharToNullableSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCharToShortTest(bool useInterpreter)
        {
            foreach (char value in new char[] { '\0', '\b', 'A', '\uffff' })
            {
                VerifyCharToShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCharToNullableShortTest(bool useInterpreter)
        {
            foreach (char value in new char[] { '\0', '\b', 'A', '\uffff' })
            {
                VerifyCharToNullableShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCharToUIntTest(bool useInterpreter)
        {
            foreach (char value in new char[] { '\0', '\b', 'A', '\uffff' })
            {
                VerifyCharToUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCharToNullableUIntTest(bool useInterpreter)
        {
            foreach (char value in new char[] { '\0', '\b', 'A', '\uffff' })
            {
                VerifyCharToNullableUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCharToULongTest(bool useInterpreter)
        {
            foreach (char value in new char[] { '\0', '\b', 'A', '\uffff' })
            {
                VerifyCharToULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCharToNullableULongTest(bool useInterpreter)
        {
            foreach (char value in new char[] { '\0', '\b', 'A', '\uffff' })
            {
                VerifyCharToNullableULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCharToUShortTest(bool useInterpreter)
        {
            foreach (char value in new char[] { '\0', '\b', 'A', '\uffff' })
            {
                VerifyCharToUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertCharToNullableUShortTest(bool useInterpreter)
        {
            foreach (char value in new char[] { '\0', '\b', 'A', '\uffff' })
            {
                VerifyCharToNullableUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableCharToByteTest(bool useInterpreter)
        {
            foreach (char? value in new char?[] { null, '\0', '\b', 'A', '\uffff' })
            {
                VerifyNullableCharToByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableCharToNullableByteTest(bool useInterpreter)
        {
            foreach (char? value in new char?[] { null, '\0', '\b', 'A', '\uffff' })
            {
                VerifyNullableCharToNullableByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableCharToCharTest(bool useInterpreter)
        {
            foreach (char? value in new char?[] { null, '\0', '\b', 'A', '\uffff' })
            {
                VerifyNullableCharToChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableCharToNullableCharTest(bool useInterpreter)
        {
            foreach (char? value in new char?[] { null, '\0', '\b', 'A', '\uffff' })
            {
                VerifyNullableCharToNullableChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableCharToDecimalTest(bool useInterpreter)
        {
            foreach (char? value in new char?[] { null, '\0', '\b', 'A', '\uffff' })
            {
                VerifyNullableCharToDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableCharToNullableDecimalTest(bool useInterpreter)
        {
            foreach (char? value in new char?[] { null, '\0', '\b', 'A', '\uffff' })
            {
                VerifyNullableCharToNullableDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableCharToDoubleTest(bool useInterpreter)
        {
            foreach (char? value in new char?[] { null, '\0', '\b', 'A', '\uffff' })
            {
                VerifyNullableCharToDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableCharToNullableDoubleTest(bool useInterpreter)
        {
            foreach (char? value in new char?[] { null, '\0', '\b', 'A', '\uffff' })
            {
                VerifyNullableCharToNullableDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableCharToEnumTest(bool useInterpreter)
        {
            foreach (char? value in new char?[] { null, '\0', '\b', 'A', '\uffff' })
            {
                VerifyNullableCharToEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableCharToNullableEnumTest(bool useInterpreter)
        {
            foreach (char? value in new char?[] { null, '\0', '\b', 'A', '\uffff' })
            {
                VerifyNullableCharToNullableEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableCharToEnumLongTest(bool useInterpreter)
        {
            foreach (char? value in new char?[] { null, '\0', '\b', 'A', '\uffff' })
            {
                VerifyNullableCharToEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableCharToNullableEnumLongTest(bool useInterpreter)
        {
            foreach (char? value in new char?[] { null, '\0', '\b', 'A', '\uffff' })
            {
                VerifyNullableCharToNullableEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableCharToFloatTest(bool useInterpreter)
        {
            foreach (char? value in new char?[] { null, '\0', '\b', 'A', '\uffff' })
            {
                VerifyNullableCharToFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableCharToNullableFloatTest(bool useInterpreter)
        {
            foreach (char? value in new char?[] { null, '\0', '\b', 'A', '\uffff' })
            {
                VerifyNullableCharToNullableFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableCharToIntTest(bool useInterpreter)
        {
            foreach (char? value in new char?[] { null, '\0', '\b', 'A', '\uffff' })
            {
                VerifyNullableCharToInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableCharToNullableIntTest(bool useInterpreter)
        {
            foreach (char? value in new char?[] { null, '\0', '\b', 'A', '\uffff' })
            {
                VerifyNullableCharToNullableInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableCharToLongTest(bool useInterpreter)
        {
            foreach (char? value in new char?[] { null, '\0', '\b', 'A', '\uffff' })
            {
                VerifyNullableCharToLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableCharToNullableLongTest(bool useInterpreter)
        {
            foreach (char? value in new char?[] { null, '\0', '\b', 'A', '\uffff' })
            {
                VerifyNullableCharToNullableLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableCharToSByteTest(bool useInterpreter)
        {
            foreach (char? value in new char?[] { null, '\0', '\b', 'A', '\uffff' })
            {
                VerifyNullableCharToSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableCharToNullableSByteTest(bool useInterpreter)
        {
            foreach (char? value in new char?[] { null, '\0', '\b', 'A', '\uffff' })
            {
                VerifyNullableCharToNullableSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableCharToShortTest(bool useInterpreter)
        {
            foreach (char? value in new char?[] { null, '\0', '\b', 'A', '\uffff' })
            {
                VerifyNullableCharToShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableCharToNullableShortTest(bool useInterpreter)
        {
            foreach (char? value in new char?[] { null, '\0', '\b', 'A', '\uffff' })
            {
                VerifyNullableCharToNullableShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableCharToUIntTest(bool useInterpreter)
        {
            foreach (char? value in new char?[] { null, '\0', '\b', 'A', '\uffff' })
            {
                VerifyNullableCharToUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableCharToNullableUIntTest(bool useInterpreter)
        {
            foreach (char? value in new char?[] { null, '\0', '\b', 'A', '\uffff' })
            {
                VerifyNullableCharToNullableUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableCharToULongTest(bool useInterpreter)
        {
            foreach (char? value in new char?[] { null, '\0', '\b', 'A', '\uffff' })
            {
                VerifyNullableCharToULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableCharToNullableULongTest(bool useInterpreter)
        {
            foreach (char? value in new char?[] { null, '\0', '\b', 'A', '\uffff' })
            {
                VerifyNullableCharToNullableULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableCharToUShortTest(bool useInterpreter)
        {
            foreach (char? value in new char?[] { null, '\0', '\b', 'A', '\uffff' })
            {
                VerifyNullableCharToUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableCharToNullableUShortTest(bool useInterpreter)
        {
            foreach (char? value in new char?[] { null, '\0', '\b', 'A', '\uffff' })
            {
                VerifyNullableCharToNullableUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertDecimalToByteTest(bool useInterpreter)
        {
            foreach (decimal value in new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyDecimalToByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertDecimalToNullableByteTest(bool useInterpreter)
        {
            foreach (decimal value in new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyDecimalToNullableByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertDecimalToCharTest(bool useInterpreter)
        {
            foreach (decimal value in new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyDecimalToChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertDecimalToNullableCharTest(bool useInterpreter)
        {
            foreach (decimal value in new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyDecimalToNullableChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertDecimalToDecimalTest(bool useInterpreter)
        {
            foreach (decimal value in new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyDecimalToDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertDecimalToNullableDecimalTest(bool useInterpreter)
        {
            foreach (decimal value in new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyDecimalToNullableDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertDecimalToDoubleTest(bool useInterpreter)
        {
            foreach (decimal value in new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyDecimalToDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertDecimalToNullableDoubleTest(bool useInterpreter)
        {
            foreach (decimal value in new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyDecimalToNullableDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertDecimalToFloatTest(bool useInterpreter)
        {
            foreach (decimal value in new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyDecimalToFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertDecimalToNullableFloatTest(bool useInterpreter)
        {
            foreach (decimal value in new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyDecimalToNullableFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertDecimalToIntTest(bool useInterpreter)
        {
            foreach (decimal value in new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyDecimalToInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertDecimalToNullableIntTest(bool useInterpreter)
        {
            foreach (decimal value in new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyDecimalToNullableInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertDecimalToLongTest(bool useInterpreter)
        {
            foreach (decimal value in new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyDecimalToLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertDecimalToNullableLongTest(bool useInterpreter)
        {
            foreach (decimal value in new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyDecimalToNullableLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertDecimalToSByteTest(bool useInterpreter)
        {
            foreach (decimal value in new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyDecimalToSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertDecimalToNullableSByteTest(bool useInterpreter)
        {
            foreach (decimal value in new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyDecimalToNullableSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertDecimalToShortTest(bool useInterpreter)
        {
            foreach (decimal value in new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyDecimalToShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertDecimalToNullableShortTest(bool useInterpreter)
        {
            foreach (decimal value in new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyDecimalToNullableShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertDecimalToUIntTest(bool useInterpreter)
        {
            foreach (decimal value in new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyDecimalToUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertDecimalToNullableUIntTest(bool useInterpreter)
        {
            foreach (decimal value in new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyDecimalToNullableUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertDecimalToULongTest(bool useInterpreter)
        {
            foreach (decimal value in new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyDecimalToULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertDecimalToNullableULongTest(bool useInterpreter)
        {
            foreach (decimal value in new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyDecimalToNullableULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertDecimalToUShortTest(bool useInterpreter)
        {
            foreach (decimal value in new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyDecimalToUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertDecimalToNullableUShortTest(bool useInterpreter)
        {
            foreach (decimal value in new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyDecimalToNullableUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableDecimalToByteTest(bool useInterpreter)
        {
            foreach (decimal? value in new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyNullableDecimalToByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableDecimalToNullableByteTest(bool useInterpreter)
        {
            foreach (decimal? value in new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyNullableDecimalToNullableByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableDecimalToCharTest(bool useInterpreter)
        {
            foreach (decimal? value in new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyNullableDecimalToChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableDecimalToNullableCharTest(bool useInterpreter)
        {
            foreach (decimal? value in new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyNullableDecimalToNullableChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableDecimalToDecimalTest(bool useInterpreter)
        {
            foreach (decimal? value in new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyNullableDecimalToDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableDecimalToNullableDecimalTest(bool useInterpreter)
        {
            foreach (decimal? value in new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyNullableDecimalToNullableDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableDecimalToDoubleTest(bool useInterpreter)
        {
            foreach (decimal? value in new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyNullableDecimalToDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableDecimalToNullableDoubleTest(bool useInterpreter)
        {
            foreach (decimal? value in new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyNullableDecimalToNullableDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableDecimalToFloatTest(bool useInterpreter)
        {
            foreach (decimal? value in new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyNullableDecimalToFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableDecimalToNullableFloatTest(bool useInterpreter)
        {
            foreach (decimal? value in new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyNullableDecimalToNullableFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableDecimalToIntTest(bool useInterpreter)
        {
            foreach (decimal? value in new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyNullableDecimalToInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableDecimalToNullableIntTest(bool useInterpreter)
        {
            foreach (decimal? value in new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyNullableDecimalToNullableInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableDecimalToLongTest(bool useInterpreter)
        {
            foreach (decimal? value in new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyNullableDecimalToLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableDecimalToNullableLongTest(bool useInterpreter)
        {
            foreach (decimal? value in new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyNullableDecimalToNullableLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableDecimalToSByteTest(bool useInterpreter)
        {
            foreach (decimal? value in new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyNullableDecimalToSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableDecimalToNullableSByteTest(bool useInterpreter)
        {
            foreach (decimal? value in new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyNullableDecimalToNullableSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableDecimalToShortTest(bool useInterpreter)
        {
            foreach (decimal? value in new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyNullableDecimalToShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableDecimalToNullableShortTest(bool useInterpreter)
        {
            foreach (decimal? value in new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyNullableDecimalToNullableShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableDecimalToUIntTest(bool useInterpreter)
        {
            foreach (decimal? value in new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyNullableDecimalToUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableDecimalToNullableUIntTest(bool useInterpreter)
        {
            foreach (decimal? value in new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyNullableDecimalToNullableUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableDecimalToULongTest(bool useInterpreter)
        {
            foreach (decimal? value in new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyNullableDecimalToULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableDecimalToNullableULongTest(bool useInterpreter)
        {
            foreach (decimal? value in new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyNullableDecimalToNullableULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableDecimalToUShortTest(bool useInterpreter)
        {
            foreach (decimal? value in new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyNullableDecimalToUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableDecimalToNullableUShortTest(bool useInterpreter)
        {
            foreach (decimal? value in new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyNullableDecimalToNullableUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertDoubleToByteTest(bool useInterpreter)
        {
            foreach (double value in new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyDoubleToByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertDoubleToNullableByteTest(bool useInterpreter)
        {
            foreach (double value in new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyDoubleToNullableByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertDoubleToCharTest(bool useInterpreter)
        {
            foreach (double value in new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyDoubleToChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertDoubleToNullableCharTest(bool useInterpreter)
        {
            foreach (double value in new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyDoubleToNullableChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertDoubleToDecimalTest(bool useInterpreter)
        {
            foreach (double value in new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyDoubleToDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertDoubleToNullableDecimalTest(bool useInterpreter)
        {
            foreach (double value in new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyDoubleToNullableDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertDoubleToDoubleTest(bool useInterpreter)
        {
            foreach (double value in new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyDoubleToDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertDoubleToNullableDoubleTest(bool useInterpreter)
        {
            foreach (double value in new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyDoubleToNullableDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertDoubleToEnumTest(bool useInterpreter)
        {
            foreach (double value in new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyDoubleToEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertDoubleToNullableEnumTest(bool useInterpreter)
        {
            foreach (double value in new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyDoubleToNullableEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertDoubleToEnumLongTest(bool useInterpreter)
        {
            foreach (double value in new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyDoubleToEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertDoubleToNullableEnumLongTest(bool useInterpreter)
        {
            foreach (double value in new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyDoubleToNullableEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertDoubleToFloatTest(bool useInterpreter)
        {
            foreach (double value in new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyDoubleToFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertDoubleToNullableFloatTest(bool useInterpreter)
        {
            foreach (double value in new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyDoubleToNullableFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertDoubleToIntTest(bool useInterpreter)
        {
            foreach (double value in new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyDoubleToInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertDoubleToNullableIntTest(bool useInterpreter)
        {
            foreach (double value in new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyDoubleToNullableInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertDoubleToLongTest(bool useInterpreter)
        {
            foreach (double value in new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyDoubleToLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertDoubleToNullableLongTest(bool useInterpreter)
        {
            foreach (double value in new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyDoubleToNullableLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertDoubleToSByteTest(bool useInterpreter)
        {
            foreach (double value in new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyDoubleToSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertDoubleToNullableSByteTest(bool useInterpreter)
        {
            foreach (double value in new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyDoubleToNullableSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertDoubleToShortTest(bool useInterpreter)
        {
            foreach (double value in new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyDoubleToShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertDoubleToNullableShortTest(bool useInterpreter)
        {
            foreach (double value in new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyDoubleToNullableShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertDoubleToUIntTest(bool useInterpreter)
        {
            foreach (double value in new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyDoubleToUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertDoubleToNullableUIntTest(bool useInterpreter)
        {
            foreach (double value in new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyDoubleToNullableUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertDoubleToULongTest(bool useInterpreter)
        {
            foreach (double value in new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyDoubleToULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertDoubleToNullableULongTest(bool useInterpreter)
        {
            foreach (double value in new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyDoubleToNullableULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertDoubleToUShortTest(bool useInterpreter)
        {
            foreach (double value in new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyDoubleToUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertDoubleToNullableUShortTest(bool useInterpreter)
        {
            foreach (double value in new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyDoubleToNullableUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableDoubleToByteTest(bool useInterpreter)
        {
            foreach (double? value in new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyNullableDoubleToByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableDoubleToNullableByteTest(bool useInterpreter)
        {
            foreach (double? value in new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyNullableDoubleToNullableByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableDoubleToCharTest(bool useInterpreter)
        {
            foreach (double? value in new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyNullableDoubleToChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableDoubleToNullableCharTest(bool useInterpreter)
        {
            foreach (double? value in new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyNullableDoubleToNullableChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableDoubleToDecimalTest(bool useInterpreter)
        {
            foreach (double? value in new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyNullableDoubleToDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableDoubleToNullableDecimalTest(bool useInterpreter)
        {
            foreach (double? value in new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyNullableDoubleToNullableDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableDoubleToDoubleTest(bool useInterpreter)
        {
            foreach (double? value in new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyNullableDoubleToDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableDoubleToNullableDoubleTest(bool useInterpreter)
        {
            foreach (double? value in new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyNullableDoubleToNullableDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableDoubleToEnumTest(bool useInterpreter)
        {
            foreach (double? value in new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyNullableDoubleToEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableDoubleToNullableEnumTest(bool useInterpreter)
        {
            foreach (double? value in new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyNullableDoubleToNullableEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableDoubleToEnumLongTest(bool useInterpreter)
        {
            foreach (double? value in new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyNullableDoubleToEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableDoubleToNullableEnumLongTest(bool useInterpreter)
        {
            foreach (double? value in new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyNullableDoubleToNullableEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableDoubleToFloatTest(bool useInterpreter)
        {
            foreach (double? value in new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyNullableDoubleToFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableDoubleToNullableFloatTest(bool useInterpreter)
        {
            foreach (double? value in new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyNullableDoubleToNullableFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableDoubleToIntTest(bool useInterpreter)
        {
            foreach (double? value in new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyNullableDoubleToInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableDoubleToNullableIntTest(bool useInterpreter)
        {
            foreach (double? value in new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyNullableDoubleToNullableInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableDoubleToLongTest(bool useInterpreter)
        {
            foreach (double? value in new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyNullableDoubleToLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableDoubleToNullableLongTest(bool useInterpreter)
        {
            foreach (double? value in new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyNullableDoubleToNullableLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableDoubleToSByteTest(bool useInterpreter)
        {
            foreach (double? value in new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyNullableDoubleToSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableDoubleToNullableSByteTest(bool useInterpreter)
        {
            foreach (double? value in new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyNullableDoubleToNullableSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableDoubleToShortTest(bool useInterpreter)
        {
            foreach (double? value in new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyNullableDoubleToShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableDoubleToNullableShortTest(bool useInterpreter)
        {
            foreach (double? value in new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyNullableDoubleToNullableShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableDoubleToUIntTest(bool useInterpreter)
        {
            foreach (double? value in new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyNullableDoubleToUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableDoubleToNullableUIntTest(bool useInterpreter)
        {
            foreach (double? value in new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyNullableDoubleToNullableUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableDoubleToULongTest(bool useInterpreter)
        {
            foreach (double? value in new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyNullableDoubleToULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableDoubleToNullableULongTest(bool useInterpreter)
        {
            foreach (double? value in new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyNullableDoubleToNullableULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableDoubleToUShortTest(bool useInterpreter)
        {
            foreach (double? value in new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyNullableDoubleToUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableDoubleToNullableUShortTest(bool useInterpreter)
        {
            foreach (double? value in new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyNullableDoubleToNullableUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertEnumToByteTest(bool useInterpreter)
        {
            foreach (E value in new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyEnumToByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertEnumToNullableByteTest(bool useInterpreter)
        {
            foreach (E value in new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyEnumToNullableByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertEnumToCharTest(bool useInterpreter)
        {
            foreach (E value in new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyEnumToChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertEnumToNullableCharTest(bool useInterpreter)
        {
            foreach (E value in new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyEnumToNullableChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertEnumToDoubleTest(bool useInterpreter)
        {
            foreach (E value in new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyEnumToDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertEnumToNullableDoubleTest(bool useInterpreter)
        {
            foreach (E value in new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyEnumToNullableDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertEnumToEnumTest(bool useInterpreter)
        {
            foreach (E value in new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyEnumToEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertEnumToNullableEnumTest(bool useInterpreter)
        {
            foreach (E value in new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyEnumToNullableEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertEnumToEnumLongTest(bool useInterpreter)
        {
            foreach (E value in new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyEnumToEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertEnumToNullableEnumLongTest(bool useInterpreter)
        {
            foreach (E value in new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyEnumToNullableEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertEnumToFloatTest(bool useInterpreter)
        {
            foreach (E value in new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyEnumToFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertEnumToNullableFloatTest(bool useInterpreter)
        {
            foreach (E value in new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyEnumToNullableFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertEnumToIntTest(bool useInterpreter)
        {
            foreach (E value in new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyEnumToInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertEnumToNullableIntTest(bool useInterpreter)
        {
            foreach (E value in new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyEnumToNullableInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertEnumToLongTest(bool useInterpreter)
        {
            foreach (E value in new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyEnumToLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertEnumToNullableLongTest(bool useInterpreter)
        {
            foreach (E value in new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyEnumToNullableLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertEnumToSByteTest(bool useInterpreter)
        {
            foreach (E value in new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyEnumToSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertEnumToNullableSByteTest(bool useInterpreter)
        {
            foreach (E value in new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyEnumToNullableSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertEnumToShortTest(bool useInterpreter)
        {
            foreach (E value in new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyEnumToShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertEnumToNullableShortTest(bool useInterpreter)
        {
            foreach (E value in new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyEnumToNullableShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertEnumToUIntTest(bool useInterpreter)
        {
            foreach (E value in new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyEnumToUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertEnumToNullableUIntTest(bool useInterpreter)
        {
            foreach (E value in new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyEnumToNullableUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertEnumToULongTest(bool useInterpreter)
        {
            foreach (E value in new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyEnumToULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertEnumToNullableULongTest(bool useInterpreter)
        {
            foreach (E value in new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyEnumToNullableULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertEnumToUShortTest(bool useInterpreter)
        {
            foreach (E value in new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyEnumToUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertEnumToNullableUShortTest(bool useInterpreter)
        {
            foreach (E value in new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyEnumToNullableUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableEnumToByteTest(bool useInterpreter)
        {
            foreach (E? value in new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyNullableEnumToByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableEnumToNullableByteTest(bool useInterpreter)
        {
            foreach (E? value in new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyNullableEnumToNullableByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableEnumToCharTest(bool useInterpreter)
        {
            foreach (E? value in new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyNullableEnumToChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableEnumToNullableCharTest(bool useInterpreter)
        {
            foreach (E? value in new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyNullableEnumToNullableChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableEnumToDoubleTest(bool useInterpreter)
        {
            foreach (E? value in new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyNullableEnumToDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableEnumToNullableDoubleTest(bool useInterpreter)
        {
            foreach (E? value in new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyNullableEnumToNullableDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableEnumToEnumTest(bool useInterpreter)
        {
            foreach (E? value in new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyNullableEnumToEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableEnumToNullableEnumTest(bool useInterpreter)
        {
            foreach (E? value in new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyNullableEnumToNullableEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableEnumToEnumLongTest(bool useInterpreter)
        {
            foreach (E? value in new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyNullableEnumToEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableEnumToNullableEnumLongTest(bool useInterpreter)
        {
            foreach (E? value in new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyNullableEnumToNullableEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableEnumToFloatTest(bool useInterpreter)
        {
            foreach (E? value in new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyNullableEnumToFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableEnumToNullableFloatTest(bool useInterpreter)
        {
            foreach (E? value in new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyNullableEnumToNullableFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableEnumToIntTest(bool useInterpreter)
        {
            foreach (E? value in new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyNullableEnumToInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableEnumToNullableIntTest(bool useInterpreter)
        {
            foreach (E? value in new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyNullableEnumToNullableInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableEnumToLongTest(bool useInterpreter)
        {
            foreach (E? value in new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyNullableEnumToLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableEnumToNullableLongTest(bool useInterpreter)
        {
            foreach (E? value in new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyNullableEnumToNullableLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableEnumToSByteTest(bool useInterpreter)
        {
            foreach (E? value in new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyNullableEnumToSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableEnumToNullableSByteTest(bool useInterpreter)
        {
            foreach (E? value in new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyNullableEnumToNullableSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableEnumToShortTest(bool useInterpreter)
        {
            foreach (E? value in new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyNullableEnumToShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableEnumToNullableShortTest(bool useInterpreter)
        {
            foreach (E? value in new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyNullableEnumToNullableShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableEnumToUIntTest(bool useInterpreter)
        {
            foreach (E? value in new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyNullableEnumToUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableEnumToNullableUIntTest(bool useInterpreter)
        {
            foreach (E? value in new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyNullableEnumToNullableUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableEnumToULongTest(bool useInterpreter)
        {
            foreach (E? value in new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyNullableEnumToULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableEnumToNullableULongTest(bool useInterpreter)
        {
            foreach (E? value in new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyNullableEnumToNullableULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableEnumToUShortTest(bool useInterpreter)
        {
            foreach (E? value in new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyNullableEnumToUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableEnumToNullableUShortTest(bool useInterpreter)
        {
            foreach (E? value in new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyNullableEnumToNullableUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertEnumLongToByteTest(bool useInterpreter)
        {
            foreach (El value in new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyEnumLongToByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertEnumLongToNullableByteTest(bool useInterpreter)
        {
            foreach (El value in new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyEnumLongToNullableByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertEnumLongToCharTest(bool useInterpreter)
        {
            foreach (El value in new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyEnumLongToChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertEnumLongToNullableCharTest(bool useInterpreter)
        {
            foreach (El value in new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyEnumLongToNullableChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertEnumLongToDoubleTest(bool useInterpreter)
        {
            foreach (El value in new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyEnumLongToDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertEnumLongToNullableDoubleTest(bool useInterpreter)
        {
            foreach (El value in new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyEnumLongToNullableDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertEnumLongToEnumTest(bool useInterpreter)
        {
            foreach (El value in new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyEnumLongToEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertEnumLongToNullableEnumTest(bool useInterpreter)
        {
            foreach (El value in new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyEnumLongToNullableEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertEnumLongToEnumLongTest(bool useInterpreter)
        {
            foreach (El value in new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyEnumLongToEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertEnumLongToNullableEnumLongTest(bool useInterpreter)
        {
            foreach (El value in new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyEnumLongToNullableEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertEnumLongToFloatTest(bool useInterpreter)
        {
            foreach (El value in new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyEnumLongToFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertEnumLongToNullableFloatTest(bool useInterpreter)
        {
            foreach (El value in new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyEnumLongToNullableFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertEnumLongToIntTest(bool useInterpreter)
        {
            foreach (El value in new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyEnumLongToInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertEnumLongToNullableIntTest(bool useInterpreter)
        {
            foreach (El value in new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyEnumLongToNullableInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertEnumLongToLongTest(bool useInterpreter)
        {
            foreach (El value in new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyEnumLongToLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertEnumLongToNullableLongTest(bool useInterpreter)
        {
            foreach (El value in new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyEnumLongToNullableLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertEnumLongToSByteTest(bool useInterpreter)
        {
            foreach (El value in new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyEnumLongToSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertEnumLongToNullableSByteTest(bool useInterpreter)
        {
            foreach (El value in new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyEnumLongToNullableSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertEnumLongToShortTest(bool useInterpreter)
        {
            foreach (El value in new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyEnumLongToShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertEnumLongToNullableShortTest(bool useInterpreter)
        {
            foreach (El value in new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyEnumLongToNullableShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertEnumLongToUIntTest(bool useInterpreter)
        {
            foreach (El value in new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyEnumLongToUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertEnumLongToNullableUIntTest(bool useInterpreter)
        {
            foreach (El value in new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyEnumLongToNullableUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertEnumLongToULongTest(bool useInterpreter)
        {
            foreach (El value in new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyEnumLongToULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertEnumLongToNullableULongTest(bool useInterpreter)
        {
            foreach (El value in new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyEnumLongToNullableULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertEnumLongToUShortTest(bool useInterpreter)
        {
            foreach (El value in new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyEnumLongToUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertEnumLongToNullableUShortTest(bool useInterpreter)
        {
            foreach (El value in new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyEnumLongToNullableUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableEnumLongToByteTest(bool useInterpreter)
        {
            foreach (El? value in new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyNullableEnumLongToByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableEnumLongToNullableByteTest(bool useInterpreter)
        {
            foreach (El? value in new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyNullableEnumLongToNullableByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableEnumLongToCharTest(bool useInterpreter)
        {
            foreach (El? value in new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyNullableEnumLongToChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableEnumLongToNullableCharTest(bool useInterpreter)
        {
            foreach (El? value in new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyNullableEnumLongToNullableChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableEnumLongToDoubleTest(bool useInterpreter)
        {
            foreach (El? value in new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyNullableEnumLongToDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableEnumLongToNullableDoubleTest(bool useInterpreter)
        {
            foreach (El? value in new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyNullableEnumLongToNullableDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableEnumLongToEnumTest(bool useInterpreter)
        {
            foreach (El? value in new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyNullableEnumLongToEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableEnumLongToNullableEnumTest(bool useInterpreter)
        {
            foreach (El? value in new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyNullableEnumLongToNullableEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableEnumLongToEnumLongTest(bool useInterpreter)
        {
            foreach (El? value in new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyNullableEnumLongToEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableEnumLongToNullableEnumLongTest(bool useInterpreter)
        {
            foreach (El? value in new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyNullableEnumLongToNullableEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableEnumLongToFloatTest(bool useInterpreter)
        {
            foreach (El? value in new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyNullableEnumLongToFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableEnumLongToNullableFloatTest(bool useInterpreter)
        {
            foreach (El? value in new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyNullableEnumLongToNullableFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableEnumLongToIntTest(bool useInterpreter)
        {
            foreach (El? value in new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyNullableEnumLongToInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableEnumLongToNullableIntTest(bool useInterpreter)
        {
            foreach (El? value in new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyNullableEnumLongToNullableInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableEnumLongToLongTest(bool useInterpreter)
        {
            foreach (El? value in new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyNullableEnumLongToLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableEnumLongToNullableLongTest(bool useInterpreter)
        {
            foreach (El? value in new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyNullableEnumLongToNullableLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableEnumLongToSByteTest(bool useInterpreter)
        {
            foreach (El? value in new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyNullableEnumLongToSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableEnumLongToNullableSByteTest(bool useInterpreter)
        {
            foreach (El? value in new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyNullableEnumLongToNullableSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableEnumLongToShortTest(bool useInterpreter)
        {
            foreach (El? value in new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyNullableEnumLongToShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableEnumLongToNullableShortTest(bool useInterpreter)
        {
            foreach (El? value in new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyNullableEnumLongToNullableShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableEnumLongToUIntTest(bool useInterpreter)
        {
            foreach (El? value in new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyNullableEnumLongToUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableEnumLongToNullableUIntTest(bool useInterpreter)
        {
            foreach (El? value in new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyNullableEnumLongToNullableUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableEnumLongToULongTest(bool useInterpreter)
        {
            foreach (El? value in new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyNullableEnumLongToULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableEnumLongToNullableULongTest(bool useInterpreter)
        {
            foreach (El? value in new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyNullableEnumLongToNullableULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableEnumLongToUShortTest(bool useInterpreter)
        {
            foreach (El? value in new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyNullableEnumLongToUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableEnumLongToNullableUShortTest(bool useInterpreter)
        {
            foreach (El? value in new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyNullableEnumLongToNullableUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertFloatToByteTest(bool useInterpreter)
        {
            foreach (float value in new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyFloatToByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertFloatToNullableByteTest(bool useInterpreter)
        {
            foreach (float value in new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyFloatToNullableByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertFloatToCharTest(bool useInterpreter)
        {
            foreach (float value in new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyFloatToChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertFloatToNullableCharTest(bool useInterpreter)
        {
            foreach (float value in new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyFloatToNullableChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertFloatToDecimalTest(bool useInterpreter)
        {
            foreach (float value in new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyFloatToDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertFloatToNullableDecimalTest(bool useInterpreter)
        {
            foreach (float value in new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyFloatToNullableDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertFloatToDoubleTest(bool useInterpreter)
        {
            foreach (float value in new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyFloatToDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertFloatToNullableDoubleTest(bool useInterpreter)
        {
            foreach (float value in new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyFloatToNullableDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertFloatToEnumTest(bool useInterpreter)
        {
            foreach (float value in new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyFloatToEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertFloatToNullableEnumTest(bool useInterpreter)
        {
            foreach (float value in new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyFloatToNullableEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertFloatToEnumLongTest(bool useInterpreter)
        {
            foreach (float value in new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyFloatToEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertFloatToNullableEnumLongTest(bool useInterpreter)
        {
            foreach (float value in new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyFloatToNullableEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertFloatToFloatTest(bool useInterpreter)
        {
            foreach (float value in new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyFloatToFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertFloatToNullableFloatTest(bool useInterpreter)
        {
            foreach (float value in new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyFloatToNullableFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertFloatToIntTest(bool useInterpreter)
        {
            foreach (float value in new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyFloatToInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertFloatToNullableIntTest(bool useInterpreter)
        {
            foreach (float value in new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyFloatToNullableInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertFloatToLongTest(bool useInterpreter)
        {
            foreach (float value in new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyFloatToLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertFloatToNullableLongTest(bool useInterpreter)
        {
            foreach (float value in new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyFloatToNullableLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertFloatToSByteTest(bool useInterpreter)
        {
            foreach (float value in new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyFloatToSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertFloatToNullableSByteTest(bool useInterpreter)
        {
            foreach (float value in new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyFloatToNullableSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertFloatToShortTest(bool useInterpreter)
        {
            foreach (float value in new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyFloatToShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertFloatToNullableShortTest(bool useInterpreter)
        {
            foreach (float value in new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyFloatToNullableShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertFloatToUIntTest(bool useInterpreter)
        {
            foreach (float value in new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyFloatToUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertFloatToNullableUIntTest(bool useInterpreter)
        {
            foreach (float value in new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyFloatToNullableUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertFloatToULongTest(bool useInterpreter)
        {
            foreach (float value in new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyFloatToULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertFloatToNullableULongTest(bool useInterpreter)
        {
            foreach (float value in new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyFloatToNullableULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertFloatToUShortTest(bool useInterpreter)
        {
            foreach (float value in new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyFloatToUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertFloatToNullableUShortTest(bool useInterpreter)
        {
            foreach (float value in new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyFloatToNullableUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableFloatToByteTest(bool useInterpreter)
        {
            foreach (float? value in new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyNullableFloatToByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableFloatToNullableByteTest(bool useInterpreter)
        {
            foreach (float? value in new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyNullableFloatToNullableByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableFloatToCharTest(bool useInterpreter)
        {
            foreach (float? value in new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyNullableFloatToChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableFloatToNullableCharTest(bool useInterpreter)
        {
            foreach (float? value in new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyNullableFloatToNullableChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableFloatToDecimalTest(bool useInterpreter)
        {
            foreach (float? value in new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyNullableFloatToDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableFloatToNullableDecimalTest(bool useInterpreter)
        {
            foreach (float? value in new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyNullableFloatToNullableDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableFloatToDoubleTest(bool useInterpreter)
        {
            foreach (float? value in new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyNullableFloatToDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableFloatToNullableDoubleTest(bool useInterpreter)
        {
            foreach (float? value in new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyNullableFloatToNullableDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableFloatToEnumTest(bool useInterpreter)
        {
            foreach (float? value in new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyNullableFloatToEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableFloatToNullableEnumTest(bool useInterpreter)
        {
            foreach (float? value in new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyNullableFloatToNullableEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableFloatToEnumLongTest(bool useInterpreter)
        {
            foreach (float? value in new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyNullableFloatToEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableFloatToNullableEnumLongTest(bool useInterpreter)
        {
            foreach (float? value in new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyNullableFloatToNullableEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableFloatToFloatTest(bool useInterpreter)
        {
            foreach (float? value in new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyNullableFloatToFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableFloatToNullableFloatTest(bool useInterpreter)
        {
            foreach (float? value in new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyNullableFloatToNullableFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableFloatToIntTest(bool useInterpreter)
        {
            foreach (float? value in new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyNullableFloatToInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableFloatToNullableIntTest(bool useInterpreter)
        {
            foreach (float? value in new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyNullableFloatToNullableInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableFloatToLongTest(bool useInterpreter)
        {
            foreach (float? value in new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyNullableFloatToLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableFloatToNullableLongTest(bool useInterpreter)
        {
            foreach (float? value in new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyNullableFloatToNullableLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableFloatToSByteTest(bool useInterpreter)
        {
            foreach (float? value in new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyNullableFloatToSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableFloatToNullableSByteTest(bool useInterpreter)
        {
            foreach (float? value in new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyNullableFloatToNullableSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableFloatToShortTest(bool useInterpreter)
        {
            foreach (float? value in new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyNullableFloatToShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableFloatToNullableShortTest(bool useInterpreter)
        {
            foreach (float? value in new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyNullableFloatToNullableShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableFloatToUIntTest(bool useInterpreter)
        {
            foreach (float? value in new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyNullableFloatToUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableFloatToNullableUIntTest(bool useInterpreter)
        {
            foreach (float? value in new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyNullableFloatToNullableUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableFloatToULongTest(bool useInterpreter)
        {
            foreach (float? value in new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyNullableFloatToULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableFloatToNullableULongTest(bool useInterpreter)
        {
            foreach (float? value in new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyNullableFloatToNullableULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableFloatToUShortTest(bool useInterpreter)
        {
            foreach (float? value in new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyNullableFloatToUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableFloatToNullableUShortTest(bool useInterpreter)
        {
            foreach (float? value in new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyNullableFloatToNullableUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertIntToByteTest(bool useInterpreter)
        {
            foreach (int value in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyIntToByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertIntToNullableByteTest(bool useInterpreter)
        {
            foreach (int value in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyIntToNullableByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertIntToCharTest(bool useInterpreter)
        {
            foreach (int value in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyIntToChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertIntToNullableCharTest(bool useInterpreter)
        {
            foreach (int value in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyIntToNullableChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertIntToDecimalTest(bool useInterpreter)
        {
            foreach (int value in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyIntToDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertIntToNullableDecimalTest(bool useInterpreter)
        {
            foreach (int value in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyIntToNullableDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertIntToDoubleTest(bool useInterpreter)
        {
            foreach (int value in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyIntToDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertIntToNullableDoubleTest(bool useInterpreter)
        {
            foreach (int value in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyIntToNullableDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertIntToEnumTest(bool useInterpreter)
        {
            foreach (int value in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyIntToEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertIntToNullableEnumTest(bool useInterpreter)
        {
            foreach (int value in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyIntToNullableEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertIntToEnumLongTest(bool useInterpreter)
        {
            foreach (int value in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyIntToEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertIntToNullableEnumLongTest(bool useInterpreter)
        {
            foreach (int value in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyIntToNullableEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertIntToFloatTest(bool useInterpreter)
        {
            foreach (int value in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyIntToFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertIntToNullableFloatTest(bool useInterpreter)
        {
            foreach (int value in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyIntToNullableFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertIntToIntTest(bool useInterpreter)
        {
            foreach (int value in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyIntToInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertIntToNullableIntTest(bool useInterpreter)
        {
            foreach (int value in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyIntToNullableInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertIntToLongTest(bool useInterpreter)
        {
            foreach (int value in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyIntToLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertIntToNullableLongTest(bool useInterpreter)
        {
            foreach (int value in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyIntToNullableLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertIntToSByteTest(bool useInterpreter)
        {
            foreach (int value in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyIntToSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertIntToNullableSByteTest(bool useInterpreter)
        {
            foreach (int value in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyIntToNullableSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertIntToShortTest(bool useInterpreter)
        {
            foreach (int value in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyIntToShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertIntToNullableShortTest(bool useInterpreter)
        {
            foreach (int value in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyIntToNullableShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertIntToUIntTest(bool useInterpreter)
        {
            foreach (int value in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyIntToUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertIntToNullableUIntTest(bool useInterpreter)
        {
            foreach (int value in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyIntToNullableUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertIntToULongTest(bool useInterpreter)
        {
            foreach (int value in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyIntToULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertIntToNullableULongTest(bool useInterpreter)
        {
            foreach (int value in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyIntToNullableULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertIntToUShortTest(bool useInterpreter)
        {
            foreach (int value in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyIntToUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertIntToNullableUShortTest(bool useInterpreter)
        {
            foreach (int value in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyIntToNullableUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableIntToByteTest(bool useInterpreter)
        {
            foreach (int? value in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyNullableIntToByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableIntToNullableByteTest(bool useInterpreter)
        {
            foreach (int? value in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyNullableIntToNullableByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableIntToCharTest(bool useInterpreter)
        {
            foreach (int? value in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyNullableIntToChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableIntToNullableCharTest(bool useInterpreter)
        {
            foreach (int? value in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyNullableIntToNullableChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableIntToDecimalTest(bool useInterpreter)
        {
            foreach (int? value in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyNullableIntToDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableIntToNullableDecimalTest(bool useInterpreter)
        {
            foreach (int? value in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyNullableIntToNullableDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableIntToDoubleTest(bool useInterpreter)
        {
            foreach (int? value in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyNullableIntToDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableIntToNullableDoubleTest(bool useInterpreter)
        {
            foreach (int? value in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyNullableIntToNullableDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableIntToEnumTest(bool useInterpreter)
        {
            foreach (int? value in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyNullableIntToEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableIntToNullableEnumTest(bool useInterpreter)
        {
            foreach (int? value in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyNullableIntToNullableEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableIntToEnumLongTest(bool useInterpreter)
        {
            foreach (int? value in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyNullableIntToEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableIntToNullableEnumLongTest(bool useInterpreter)
        {
            foreach (int? value in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyNullableIntToNullableEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableIntToFloatTest(bool useInterpreter)
        {
            foreach (int? value in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyNullableIntToFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableIntToNullableFloatTest(bool useInterpreter)
        {
            foreach (int? value in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyNullableIntToNullableFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableIntToIntTest(bool useInterpreter)
        {
            foreach (int? value in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyNullableIntToInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableIntToNullableIntTest(bool useInterpreter)
        {
            foreach (int? value in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyNullableIntToNullableInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableIntToLongTest(bool useInterpreter)
        {
            foreach (int? value in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyNullableIntToLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableIntToNullableLongTest(bool useInterpreter)
        {
            foreach (int? value in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyNullableIntToNullableLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableIntToSByteTest(bool useInterpreter)
        {
            foreach (int? value in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyNullableIntToSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableIntToNullableSByteTest(bool useInterpreter)
        {
            foreach (int? value in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyNullableIntToNullableSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableIntToShortTest(bool useInterpreter)
        {
            foreach (int? value in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyNullableIntToShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableIntToNullableShortTest(bool useInterpreter)
        {
            foreach (int? value in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyNullableIntToNullableShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableIntToUIntTest(bool useInterpreter)
        {
            foreach (int? value in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyNullableIntToUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableIntToNullableUIntTest(bool useInterpreter)
        {
            foreach (int? value in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyNullableIntToNullableUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableIntToULongTest(bool useInterpreter)
        {
            foreach (int? value in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyNullableIntToULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableIntToNullableULongTest(bool useInterpreter)
        {
            foreach (int? value in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyNullableIntToNullableULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableIntToUShortTest(bool useInterpreter)
        {
            foreach (int? value in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyNullableIntToUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableIntToNullableUShortTest(bool useInterpreter)
        {
            foreach (int? value in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyNullableIntToNullableUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertLongToByteTest(bool useInterpreter)
        {
            foreach (long value in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyLongToByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertLongToNullableByteTest(bool useInterpreter)
        {
            foreach (long value in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyLongToNullableByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertLongToCharTest(bool useInterpreter)
        {
            foreach (long value in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyLongToChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertLongToNullableCharTest(bool useInterpreter)
        {
            foreach (long value in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyLongToNullableChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertLongToDecimalTest(bool useInterpreter)
        {
            foreach (long value in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyLongToDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertLongToNullableDecimalTest(bool useInterpreter)
        {
            foreach (long value in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyLongToNullableDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertLongToDoubleTest(bool useInterpreter)
        {
            foreach (long value in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyLongToDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertLongToNullableDoubleTest(bool useInterpreter)
        {
            foreach (long value in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyLongToNullableDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertLongToEnumTest(bool useInterpreter)
        {
            foreach (long value in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyLongToEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertLongToNullableEnumTest(bool useInterpreter)
        {
            foreach (long value in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyLongToNullableEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertLongToEnumLongTest(bool useInterpreter)
        {
            foreach (long value in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyLongToEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertLongToNullableEnumLongTest(bool useInterpreter)
        {
            foreach (long value in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyLongToNullableEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertLongToFloatTest(bool useInterpreter)
        {
            foreach (long value in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyLongToFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertLongToNullableFloatTest(bool useInterpreter)
        {
            foreach (long value in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyLongToNullableFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertLongToIntTest(bool useInterpreter)
        {
            foreach (long value in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyLongToInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertLongToNullableIntTest(bool useInterpreter)
        {
            foreach (long value in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyLongToNullableInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertLongToLongTest(bool useInterpreter)
        {
            foreach (long value in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyLongToLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertLongToNullableLongTest(bool useInterpreter)
        {
            foreach (long value in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyLongToNullableLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertLongToSByteTest(bool useInterpreter)
        {
            foreach (long value in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyLongToSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertLongToNullableSByteTest(bool useInterpreter)
        {
            foreach (long value in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyLongToNullableSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertLongToShortTest(bool useInterpreter)
        {
            foreach (long value in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyLongToShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertLongToNullableShortTest(bool useInterpreter)
        {
            foreach (long value in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyLongToNullableShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertLongToUIntTest(bool useInterpreter)
        {
            foreach (long value in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyLongToUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertLongToNullableUIntTest(bool useInterpreter)
        {
            foreach (long value in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyLongToNullableUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertLongToULongTest(bool useInterpreter)
        {
            foreach (long value in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyLongToULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertLongToNullableULongTest(bool useInterpreter)
        {
            foreach (long value in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyLongToNullableULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertLongToUShortTest(bool useInterpreter)
        {
            foreach (long value in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyLongToUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertLongToNullableUShortTest(bool useInterpreter)
        {
            foreach (long value in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyLongToNullableUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableLongToByteTest(bool useInterpreter)
        {
            foreach (long? value in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyNullableLongToByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableLongToNullableByteTest(bool useInterpreter)
        {
            foreach (long? value in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyNullableLongToNullableByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableLongToCharTest(bool useInterpreter)
        {
            foreach (long? value in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyNullableLongToChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableLongToNullableCharTest(bool useInterpreter)
        {
            foreach (long? value in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyNullableLongToNullableChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableLongToDecimalTest(bool useInterpreter)
        {
            foreach (long? value in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyNullableLongToDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableLongToNullableDecimalTest(bool useInterpreter)
        {
            foreach (long? value in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyNullableLongToNullableDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableLongToDoubleTest(bool useInterpreter)
        {
            foreach (long? value in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyNullableLongToDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableLongToNullableDoubleTest(bool useInterpreter)
        {
            foreach (long? value in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyNullableLongToNullableDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableLongToEnumTest(bool useInterpreter)
        {
            foreach (long? value in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyNullableLongToEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableLongToNullableEnumTest(bool useInterpreter)
        {
            foreach (long? value in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyNullableLongToNullableEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableLongToEnumLongTest(bool useInterpreter)
        {
            foreach (long? value in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyNullableLongToEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableLongToNullableEnumLongTest(bool useInterpreter)
        {
            foreach (long? value in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyNullableLongToNullableEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableLongToFloatTest(bool useInterpreter)
        {
            foreach (long? value in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyNullableLongToFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableLongToNullableFloatTest(bool useInterpreter)
        {
            foreach (long? value in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyNullableLongToNullableFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableLongToIntTest(bool useInterpreter)
        {
            foreach (long? value in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyNullableLongToInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableLongToNullableIntTest(bool useInterpreter)
        {
            foreach (long? value in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyNullableLongToNullableInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableLongToLongTest(bool useInterpreter)
        {
            foreach (long? value in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyNullableLongToLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableLongToNullableLongTest(bool useInterpreter)
        {
            foreach (long? value in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyNullableLongToNullableLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableLongToSByteTest(bool useInterpreter)
        {
            foreach (long? value in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyNullableLongToSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableLongToNullableSByteTest(bool useInterpreter)
        {
            foreach (long? value in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyNullableLongToNullableSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableLongToShortTest(bool useInterpreter)
        {
            foreach (long? value in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyNullableLongToShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableLongToNullableShortTest(bool useInterpreter)
        {
            foreach (long? value in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyNullableLongToNullableShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableLongToUIntTest(bool useInterpreter)
        {
            foreach (long? value in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyNullableLongToUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableLongToNullableUIntTest(bool useInterpreter)
        {
            foreach (long? value in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyNullableLongToNullableUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableLongToULongTest(bool useInterpreter)
        {
            foreach (long? value in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyNullableLongToULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableLongToNullableULongTest(bool useInterpreter)
        {
            foreach (long? value in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyNullableLongToNullableULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableLongToUShortTest(bool useInterpreter)
        {
            foreach (long? value in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyNullableLongToUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableLongToNullableUShortTest(bool useInterpreter)
        {
            foreach (long? value in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyNullableLongToNullableUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertSByteToByteTest(bool useInterpreter)
        {
            foreach (sbyte value in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifySByteToByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertSByteToNullableByteTest(bool useInterpreter)
        {
            foreach (sbyte value in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifySByteToNullableByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertSByteToCharTest(bool useInterpreter)
        {
            foreach (sbyte value in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifySByteToChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertSByteToNullableCharTest(bool useInterpreter)
        {
            foreach (sbyte value in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifySByteToNullableChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertSByteToDecimalTest(bool useInterpreter)
        {
            foreach (sbyte value in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifySByteToDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertSByteToNullableDecimalTest(bool useInterpreter)
        {
            foreach (sbyte value in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifySByteToNullableDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertSByteToDoubleTest(bool useInterpreter)
        {
            foreach (sbyte value in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifySByteToDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertSByteToNullableDoubleTest(bool useInterpreter)
        {
            foreach (sbyte value in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifySByteToNullableDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertSByteToEnumTest(bool useInterpreter)
        {
            foreach (sbyte value in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifySByteToEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertSByteToNullableEnumTest(bool useInterpreter)
        {
            foreach (sbyte value in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifySByteToNullableEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertSByteToEnumLongTest(bool useInterpreter)
        {
            foreach (sbyte value in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifySByteToEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertSByteToNullableEnumLongTest(bool useInterpreter)
        {
            foreach (sbyte value in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifySByteToNullableEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertSByteToFloatTest(bool useInterpreter)
        {
            foreach (sbyte value in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifySByteToFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertSByteToNullableFloatTest(bool useInterpreter)
        {
            foreach (sbyte value in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifySByteToNullableFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertSByteToIntTest(bool useInterpreter)
        {
            foreach (sbyte value in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifySByteToInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertSByteToNullableIntTest(bool useInterpreter)
        {
            foreach (sbyte value in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifySByteToNullableInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertSByteToLongTest(bool useInterpreter)
        {
            foreach (sbyte value in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifySByteToLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertSByteToNullableLongTest(bool useInterpreter)
        {
            foreach (sbyte value in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifySByteToNullableLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertSByteToSByteTest(bool useInterpreter)
        {
            foreach (sbyte value in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifySByteToSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertSByteToNullableSByteTest(bool useInterpreter)
        {
            foreach (sbyte value in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifySByteToNullableSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertSByteToShortTest(bool useInterpreter)
        {
            foreach (sbyte value in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifySByteToShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertSByteToNullableShortTest(bool useInterpreter)
        {
            foreach (sbyte value in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifySByteToNullableShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertSByteToUIntTest(bool useInterpreter)
        {
            foreach (sbyte value in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifySByteToUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertSByteToNullableUIntTest(bool useInterpreter)
        {
            foreach (sbyte value in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifySByteToNullableUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertSByteToULongTest(bool useInterpreter)
        {
            foreach (sbyte value in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifySByteToULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertSByteToNullableULongTest(bool useInterpreter)
        {
            foreach (sbyte value in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifySByteToNullableULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertSByteToUShortTest(bool useInterpreter)
        {
            foreach (sbyte value in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifySByteToUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertSByteToNullableUShortTest(bool useInterpreter)
        {
            foreach (sbyte value in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifySByteToNullableUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableSByteToByteTest(bool useInterpreter)
        {
            foreach (sbyte? value in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyNullableSByteToByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableSByteToNullableByteTest(bool useInterpreter)
        {
            foreach (sbyte? value in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyNullableSByteToNullableByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableSByteToCharTest(bool useInterpreter)
        {
            foreach (sbyte? value in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyNullableSByteToChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableSByteToNullableCharTest(bool useInterpreter)
        {
            foreach (sbyte? value in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyNullableSByteToNullableChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableSByteToDecimalTest(bool useInterpreter)
        {
            foreach (sbyte? value in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyNullableSByteToDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableSByteToNullableDecimalTest(bool useInterpreter)
        {
            foreach (sbyte? value in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyNullableSByteToNullableDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableSByteToDoubleTest(bool useInterpreter)
        {
            foreach (sbyte? value in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyNullableSByteToDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableSByteToNullableDoubleTest(bool useInterpreter)
        {
            foreach (sbyte? value in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyNullableSByteToNullableDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableSByteToEnumTest(bool useInterpreter)
        {
            foreach (sbyte? value in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyNullableSByteToEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableSByteToNullableEnumTest(bool useInterpreter)
        {
            foreach (sbyte? value in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyNullableSByteToNullableEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableSByteToEnumLongTest(bool useInterpreter)
        {
            foreach (sbyte? value in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyNullableSByteToEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableSByteToNullableEnumLongTest(bool useInterpreter)
        {
            foreach (sbyte? value in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyNullableSByteToNullableEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableSByteToFloatTest(bool useInterpreter)
        {
            foreach (sbyte? value in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyNullableSByteToFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableSByteToNullableFloatTest(bool useInterpreter)
        {
            foreach (sbyte? value in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyNullableSByteToNullableFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableSByteToIntTest(bool useInterpreter)
        {
            foreach (sbyte? value in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyNullableSByteToInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableSByteToNullableIntTest(bool useInterpreter)
        {
            foreach (sbyte? value in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyNullableSByteToNullableInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableSByteToLongTest(bool useInterpreter)
        {
            foreach (sbyte? value in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyNullableSByteToLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableSByteToNullableLongTest(bool useInterpreter)
        {
            foreach (sbyte? value in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyNullableSByteToNullableLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableSByteToSByteTest(bool useInterpreter)
        {
            foreach (sbyte? value in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyNullableSByteToSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableSByteToNullableSByteTest(bool useInterpreter)
        {
            foreach (sbyte? value in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyNullableSByteToNullableSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableSByteToShortTest(bool useInterpreter)
        {
            foreach (sbyte? value in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyNullableSByteToShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableSByteToNullableShortTest(bool useInterpreter)
        {
            foreach (sbyte? value in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyNullableSByteToNullableShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableSByteToUIntTest(bool useInterpreter)
        {
            foreach (sbyte? value in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyNullableSByteToUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableSByteToNullableUIntTest(bool useInterpreter)
        {
            foreach (sbyte? value in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyNullableSByteToNullableUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableSByteToULongTest(bool useInterpreter)
        {
            foreach (sbyte? value in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyNullableSByteToULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableSByteToNullableULongTest(bool useInterpreter)
        {
            foreach (sbyte? value in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyNullableSByteToNullableULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableSByteToUShortTest(bool useInterpreter)
        {
            foreach (sbyte? value in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyNullableSByteToUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableSByteToNullableUShortTest(bool useInterpreter)
        {
            foreach (sbyte? value in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyNullableSByteToNullableUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertShortToByteTest(bool useInterpreter)
        {
            foreach (short value in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyShortToByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertShortToNullableByteTest(bool useInterpreter)
        {
            foreach (short value in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyShortToNullableByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertShortToCharTest(bool useInterpreter)
        {
            foreach (short value in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyShortToChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertShortToNullableCharTest(bool useInterpreter)
        {
            foreach (short value in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyShortToNullableChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertShortToDecimalTest(bool useInterpreter)
        {
            foreach (short value in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyShortToDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertShortToNullableDecimalTest(bool useInterpreter)
        {
            foreach (short value in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyShortToNullableDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertShortToDoubleTest(bool useInterpreter)
        {
            foreach (short value in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyShortToDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertShortToNullableDoubleTest(bool useInterpreter)
        {
            foreach (short value in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyShortToNullableDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertShortToEnumTest(bool useInterpreter)
        {
            foreach (short value in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyShortToEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertShortToNullableEnumTest(bool useInterpreter)
        {
            foreach (short value in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyShortToNullableEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertShortToEnumLongTest(bool useInterpreter)
        {
            foreach (short value in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyShortToEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertShortToNullableEnumLongTest(bool useInterpreter)
        {
            foreach (short value in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyShortToNullableEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertShortToFloatTest(bool useInterpreter)
        {
            foreach (short value in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyShortToFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertShortToNullableFloatTest(bool useInterpreter)
        {
            foreach (short value in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyShortToNullableFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertShortToIntTest(bool useInterpreter)
        {
            foreach (short value in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyShortToInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertShortToNullableIntTest(bool useInterpreter)
        {
            foreach (short value in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyShortToNullableInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertShortToLongTest(bool useInterpreter)
        {
            foreach (short value in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyShortToLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertShortToNullableLongTest(bool useInterpreter)
        {
            foreach (short value in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyShortToNullableLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertShortToSByteTest(bool useInterpreter)
        {
            foreach (short value in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyShortToSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertShortToNullableSByteTest(bool useInterpreter)
        {
            foreach (short value in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyShortToNullableSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertShortToShortTest(bool useInterpreter)
        {
            foreach (short value in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyShortToShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertShortToNullableShortTest(bool useInterpreter)
        {
            foreach (short value in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyShortToNullableShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertShortToUIntTest(bool useInterpreter)
        {
            foreach (short value in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyShortToUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertShortToNullableUIntTest(bool useInterpreter)
        {
            foreach (short value in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyShortToNullableUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertShortToULongTest(bool useInterpreter)
        {
            foreach (short value in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyShortToULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertShortToNullableULongTest(bool useInterpreter)
        {
            foreach (short value in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyShortToNullableULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertShortToUShortTest(bool useInterpreter)
        {
            foreach (short value in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyShortToUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertShortToNullableUShortTest(bool useInterpreter)
        {
            foreach (short value in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyShortToNullableUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableShortToByteTest(bool useInterpreter)
        {
            foreach (short? value in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyNullableShortToByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableShortToNullableByteTest(bool useInterpreter)
        {
            foreach (short? value in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyNullableShortToNullableByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableShortToCharTest(bool useInterpreter)
        {
            foreach (short? value in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyNullableShortToChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableShortToNullableCharTest(bool useInterpreter)
        {
            foreach (short? value in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyNullableShortToNullableChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableShortToDecimalTest(bool useInterpreter)
        {
            foreach (short? value in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyNullableShortToDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableShortToNullableDecimalTest(bool useInterpreter)
        {
            foreach (short? value in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyNullableShortToNullableDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableShortToDoubleTest(bool useInterpreter)
        {
            foreach (short? value in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyNullableShortToDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableShortToNullableDoubleTest(bool useInterpreter)
        {
            foreach (short? value in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyNullableShortToNullableDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableShortToEnumTest(bool useInterpreter)
        {
            foreach (short? value in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyNullableShortToEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableShortToNullableEnumTest(bool useInterpreter)
        {
            foreach (short? value in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyNullableShortToNullableEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableShortToEnumLongTest(bool useInterpreter)
        {
            foreach (short? value in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyNullableShortToEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableShortToNullableEnumLongTest(bool useInterpreter)
        {
            foreach (short? value in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyNullableShortToNullableEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableShortToFloatTest(bool useInterpreter)
        {
            foreach (short? value in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyNullableShortToFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableShortToNullableFloatTest(bool useInterpreter)
        {
            foreach (short? value in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyNullableShortToNullableFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableShortToIntTest(bool useInterpreter)
        {
            foreach (short? value in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyNullableShortToInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableShortToNullableIntTest(bool useInterpreter)
        {
            foreach (short? value in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyNullableShortToNullableInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableShortToLongTest(bool useInterpreter)
        {
            foreach (short? value in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyNullableShortToLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableShortToNullableLongTest(bool useInterpreter)
        {
            foreach (short? value in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyNullableShortToNullableLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableShortToSByteTest(bool useInterpreter)
        {
            foreach (short? value in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyNullableShortToSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableShortToNullableSByteTest(bool useInterpreter)
        {
            foreach (short? value in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyNullableShortToNullableSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableShortToShortTest(bool useInterpreter)
        {
            foreach (short? value in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyNullableShortToShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableShortToNullableShortTest(bool useInterpreter)
        {
            foreach (short? value in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyNullableShortToNullableShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableShortToUIntTest(bool useInterpreter)
        {
            foreach (short? value in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyNullableShortToUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableShortToNullableUIntTest(bool useInterpreter)
        {
            foreach (short? value in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyNullableShortToNullableUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableShortToULongTest(bool useInterpreter)
        {
            foreach (short? value in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyNullableShortToULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableShortToNullableULongTest(bool useInterpreter)
        {
            foreach (short? value in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyNullableShortToNullableULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableShortToUShortTest(bool useInterpreter)
        {
            foreach (short? value in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyNullableShortToUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableShortToNullableUShortTest(bool useInterpreter)
        {
            foreach (short? value in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyNullableShortToNullableUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertUIntToByteTest(bool useInterpreter)
        {
            foreach (uint value in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyUIntToByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertUIntToNullableByteTest(bool useInterpreter)
        {
            foreach (uint value in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyUIntToNullableByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertUIntToCharTest(bool useInterpreter)
        {
            foreach (uint value in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyUIntToChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertUIntToNullableCharTest(bool useInterpreter)
        {
            foreach (uint value in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyUIntToNullableChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertUIntToDecimalTest(bool useInterpreter)
        {
            foreach (uint value in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyUIntToDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertUIntToNullableDecimalTest(bool useInterpreter)
        {
            foreach (uint value in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyUIntToNullableDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertUIntToDoubleTest(bool useInterpreter)
        {
            foreach (uint value in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyUIntToDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertUIntToNullableDoubleTest(bool useInterpreter)
        {
            foreach (uint value in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyUIntToNullableDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertUIntToEnumTest(bool useInterpreter)
        {
            foreach (uint value in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyUIntToEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertUIntToNullableEnumTest(bool useInterpreter)
        {
            foreach (uint value in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyUIntToNullableEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertUIntToEnumLongTest(bool useInterpreter)
        {
            foreach (uint value in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyUIntToEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertUIntToNullableEnumLongTest(bool useInterpreter)
        {
            foreach (uint value in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyUIntToNullableEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertUIntToFloatTest(bool useInterpreter)
        {
            foreach (uint value in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyUIntToFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertUIntToNullableFloatTest(bool useInterpreter)
        {
            foreach (uint value in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyUIntToNullableFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertUIntToIntTest(bool useInterpreter)
        {
            foreach (uint value in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyUIntToInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertUIntToNullableIntTest(bool useInterpreter)
        {
            foreach (uint value in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyUIntToNullableInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertUIntToLongTest(bool useInterpreter)
        {
            foreach (uint value in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyUIntToLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertUIntToNullableLongTest(bool useInterpreter)
        {
            foreach (uint value in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyUIntToNullableLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertUIntToSByteTest(bool useInterpreter)
        {
            foreach (uint value in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyUIntToSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertUIntToNullableSByteTest(bool useInterpreter)
        {
            foreach (uint value in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyUIntToNullableSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertUIntToShortTest(bool useInterpreter)
        {
            foreach (uint value in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyUIntToShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertUIntToNullableShortTest(bool useInterpreter)
        {
            foreach (uint value in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyUIntToNullableShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertUIntToUIntTest(bool useInterpreter)
        {
            foreach (uint value in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyUIntToUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertUIntToNullableUIntTest(bool useInterpreter)
        {
            foreach (uint value in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyUIntToNullableUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertUIntToULongTest(bool useInterpreter)
        {
            foreach (uint value in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyUIntToULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertUIntToNullableULongTest(bool useInterpreter)
        {
            foreach (uint value in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyUIntToNullableULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertUIntToUShortTest(bool useInterpreter)
        {
            foreach (uint value in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyUIntToUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertUIntToNullableUShortTest(bool useInterpreter)
        {
            foreach (uint value in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyUIntToNullableUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableUIntToByteTest(bool useInterpreter)
        {
            foreach (uint? value in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyNullableUIntToByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableUIntToNullableByteTest(bool useInterpreter)
        {
            foreach (uint? value in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyNullableUIntToNullableByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableUIntToCharTest(bool useInterpreter)
        {
            foreach (uint? value in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyNullableUIntToChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableUIntToNullableCharTest(bool useInterpreter)
        {
            foreach (uint? value in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyNullableUIntToNullableChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableUIntToDecimalTest(bool useInterpreter)
        {
            foreach (uint? value in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyNullableUIntToDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableUIntToNullableDecimalTest(bool useInterpreter)
        {
            foreach (uint? value in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyNullableUIntToNullableDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableUIntToDoubleTest(bool useInterpreter)
        {
            foreach (uint? value in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyNullableUIntToDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableUIntToNullableDoubleTest(bool useInterpreter)
        {
            foreach (uint? value in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyNullableUIntToNullableDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableUIntToEnumTest(bool useInterpreter)
        {
            foreach (uint? value in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyNullableUIntToEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableUIntToNullableEnumTest(bool useInterpreter)
        {
            foreach (uint? value in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyNullableUIntToNullableEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableUIntToEnumLongTest(bool useInterpreter)
        {
            foreach (uint? value in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyNullableUIntToEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableUIntToNullableEnumLongTest(bool useInterpreter)
        {
            foreach (uint? value in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyNullableUIntToNullableEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableUIntToFloatTest(bool useInterpreter)
        {
            foreach (uint? value in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyNullableUIntToFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableUIntToNullableFloatTest(bool useInterpreter)
        {
            foreach (uint? value in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyNullableUIntToNullableFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableUIntToIntTest(bool useInterpreter)
        {
            foreach (uint? value in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyNullableUIntToInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableUIntToNullableIntTest(bool useInterpreter)
        {
            foreach (uint? value in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyNullableUIntToNullableInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableUIntToLongTest(bool useInterpreter)
        {
            foreach (uint? value in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyNullableUIntToLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableUIntToNullableLongTest(bool useInterpreter)
        {
            foreach (uint? value in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyNullableUIntToNullableLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableUIntToSByteTest(bool useInterpreter)
        {
            foreach (uint? value in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyNullableUIntToSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableUIntToNullableSByteTest(bool useInterpreter)
        {
            foreach (uint? value in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyNullableUIntToNullableSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableUIntToShortTest(bool useInterpreter)
        {
            foreach (uint? value in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyNullableUIntToShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableUIntToNullableShortTest(bool useInterpreter)
        {
            foreach (uint? value in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyNullableUIntToNullableShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableUIntToUIntTest(bool useInterpreter)
        {
            foreach (uint? value in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyNullableUIntToUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableUIntToNullableUIntTest(bool useInterpreter)
        {
            foreach (uint? value in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyNullableUIntToNullableUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableUIntToULongTest(bool useInterpreter)
        {
            foreach (uint? value in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyNullableUIntToULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableUIntToNullableULongTest(bool useInterpreter)
        {
            foreach (uint? value in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyNullableUIntToNullableULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableUIntToUShortTest(bool useInterpreter)
        {
            foreach (uint? value in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyNullableUIntToUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableUIntToNullableUShortTest(bool useInterpreter)
        {
            foreach (uint? value in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyNullableUIntToNullableUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertULongToByteTest(bool useInterpreter)
        {
            foreach (ulong value in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyULongToByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertULongToNullableByteTest(bool useInterpreter)
        {
            foreach (ulong value in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyULongToNullableByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertULongToCharTest(bool useInterpreter)
        {
            foreach (ulong value in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyULongToChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertULongToNullableCharTest(bool useInterpreter)
        {
            foreach (ulong value in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyULongToNullableChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertULongToDecimalTest(bool useInterpreter)
        {
            foreach (ulong value in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyULongToDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertULongToNullableDecimalTest(bool useInterpreter)
        {
            foreach (ulong value in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyULongToNullableDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertULongToDoubleTest(bool useInterpreter)
        {
            foreach (ulong value in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyULongToDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertULongToNullableDoubleTest(bool useInterpreter)
        {
            foreach (ulong value in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyULongToNullableDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertULongToEnumTest(bool useInterpreter)
        {
            foreach (ulong value in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyULongToEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertULongToNullableEnumTest(bool useInterpreter)
        {
            foreach (ulong value in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyULongToNullableEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertULongToEnumLongTest(bool useInterpreter)
        {
            foreach (ulong value in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyULongToEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertULongToNullableEnumLongTest(bool useInterpreter)
        {
            foreach (ulong value in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyULongToNullableEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertULongToFloatTest(bool useInterpreter)
        {
            foreach (ulong value in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyULongToFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertULongToNullableFloatTest(bool useInterpreter)
        {
            foreach (ulong value in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyULongToNullableFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertULongToIntTest(bool useInterpreter)
        {
            foreach (ulong value in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyULongToInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertULongToNullableIntTest(bool useInterpreter)
        {
            foreach (ulong value in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyULongToNullableInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertULongToLongTest(bool useInterpreter)
        {
            foreach (ulong value in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyULongToLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertULongToNullableLongTest(bool useInterpreter)
        {
            foreach (ulong value in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyULongToNullableLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertULongToSByteTest(bool useInterpreter)
        {
            foreach (ulong value in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyULongToSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertULongToNullableSByteTest(bool useInterpreter)
        {
            foreach (ulong value in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyULongToNullableSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertULongToShortTest(bool useInterpreter)
        {
            foreach (ulong value in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyULongToShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertULongToNullableShortTest(bool useInterpreter)
        {
            foreach (ulong value in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyULongToNullableShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertULongToUIntTest(bool useInterpreter)
        {
            foreach (ulong value in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyULongToUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertULongToNullableUIntTest(bool useInterpreter)
        {
            foreach (ulong value in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyULongToNullableUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertULongToULongTest(bool useInterpreter)
        {
            foreach (ulong value in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyULongToULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertULongToNullableULongTest(bool useInterpreter)
        {
            foreach (ulong value in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyULongToNullableULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertULongToUShortTest(bool useInterpreter)
        {
            foreach (ulong value in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyULongToUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertULongToNullableUShortTest(bool useInterpreter)
        {
            foreach (ulong value in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyULongToNullableUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableULongToByteTest(bool useInterpreter)
        {
            foreach (ulong? value in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyNullableULongToByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableULongToNullableByteTest(bool useInterpreter)
        {
            foreach (ulong? value in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyNullableULongToNullableByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableULongToCharTest(bool useInterpreter)
        {
            foreach (ulong? value in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyNullableULongToChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableULongToNullableCharTest(bool useInterpreter)
        {
            foreach (ulong? value in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyNullableULongToNullableChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableULongToDecimalTest(bool useInterpreter)
        {
            foreach (ulong? value in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyNullableULongToDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableULongToNullableDecimalTest(bool useInterpreter)
        {
            foreach (ulong? value in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyNullableULongToNullableDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableULongToDoubleTest(bool useInterpreter)
        {
            foreach (ulong? value in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyNullableULongToDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableULongToNullableDoubleTest(bool useInterpreter)
        {
            foreach (ulong? value in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyNullableULongToNullableDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableULongToEnumTest(bool useInterpreter)
        {
            foreach (ulong? value in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyNullableULongToEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableULongToNullableEnumTest(bool useInterpreter)
        {
            foreach (ulong? value in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyNullableULongToNullableEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableULongToEnumLongTest(bool useInterpreter)
        {
            foreach (ulong? value in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyNullableULongToEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableULongToNullableEnumLongTest(bool useInterpreter)
        {
            foreach (ulong? value in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyNullableULongToNullableEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableULongToFloatTest(bool useInterpreter)
        {
            foreach (ulong? value in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyNullableULongToFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableULongToNullableFloatTest(bool useInterpreter)
        {
            foreach (ulong? value in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyNullableULongToNullableFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableULongToIntTest(bool useInterpreter)
        {
            foreach (ulong? value in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyNullableULongToInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableULongToNullableIntTest(bool useInterpreter)
        {
            foreach (ulong? value in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyNullableULongToNullableInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableULongToLongTest(bool useInterpreter)
        {
            foreach (ulong? value in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyNullableULongToLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableULongToNullableLongTest(bool useInterpreter)
        {
            foreach (ulong? value in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyNullableULongToNullableLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableULongToSByteTest(bool useInterpreter)
        {
            foreach (ulong? value in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyNullableULongToSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableULongToNullableSByteTest(bool useInterpreter)
        {
            foreach (ulong? value in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyNullableULongToNullableSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableULongToShortTest(bool useInterpreter)
        {
            foreach (ulong? value in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyNullableULongToShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableULongToNullableShortTest(bool useInterpreter)
        {
            foreach (ulong? value in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyNullableULongToNullableShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableULongToUIntTest(bool useInterpreter)
        {
            foreach (ulong? value in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyNullableULongToUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableULongToNullableUIntTest(bool useInterpreter)
        {
            foreach (ulong? value in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyNullableULongToNullableUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableULongToULongTest(bool useInterpreter)
        {
            foreach (ulong? value in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyNullableULongToULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableULongToNullableULongTest(bool useInterpreter)
        {
            foreach (ulong? value in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyNullableULongToNullableULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableULongToUShortTest(bool useInterpreter)
        {
            foreach (ulong? value in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyNullableULongToUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableULongToNullableUShortTest(bool useInterpreter)
        {
            foreach (ulong? value in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyNullableULongToNullableUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertUShortToByteTest(bool useInterpreter)
        {
            foreach (ushort value in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyUShortToByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertUShortToNullableByteTest(bool useInterpreter)
        {
            foreach (ushort value in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyUShortToNullableByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertUShortToCharTest(bool useInterpreter)
        {
            foreach (ushort value in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyUShortToChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertUShortToNullableCharTest(bool useInterpreter)
        {
            foreach (ushort value in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyUShortToNullableChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertUShortToDecimalTest(bool useInterpreter)
        {
            foreach (ushort value in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyUShortToDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertUShortToNullableDecimalTest(bool useInterpreter)
        {
            foreach (ushort value in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyUShortToNullableDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertUShortToDoubleTest(bool useInterpreter)
        {
            foreach (ushort value in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyUShortToDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertUShortToNullableDoubleTest(bool useInterpreter)
        {
            foreach (ushort value in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyUShortToNullableDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertUShortToEnumTest(bool useInterpreter)
        {
            foreach (ushort value in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyUShortToEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertUShortToNullableEnumTest(bool useInterpreter)
        {
            foreach (ushort value in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyUShortToNullableEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertUShortToEnumLongTest(bool useInterpreter)
        {
            foreach (ushort value in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyUShortToEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertUShortToNullableEnumLongTest(bool useInterpreter)
        {
            foreach (ushort value in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyUShortToNullableEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertUShortToFloatTest(bool useInterpreter)
        {
            foreach (ushort value in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyUShortToFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertUShortToNullableFloatTest(bool useInterpreter)
        {
            foreach (ushort value in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyUShortToNullableFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertUShortToIntTest(bool useInterpreter)
        {
            foreach (ushort value in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyUShortToInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertUShortToNullableIntTest(bool useInterpreter)
        {
            foreach (ushort value in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyUShortToNullableInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertUShortToLongTest(bool useInterpreter)
        {
            foreach (ushort value in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyUShortToLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertUShortToNullableLongTest(bool useInterpreter)
        {
            foreach (ushort value in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyUShortToNullableLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertUShortToSByteTest(bool useInterpreter)
        {
            foreach (ushort value in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyUShortToSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertUShortToNullableSByteTest(bool useInterpreter)
        {
            foreach (ushort value in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyUShortToNullableSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertUShortToShortTest(bool useInterpreter)
        {
            foreach (ushort value in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyUShortToShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertUShortToNullableShortTest(bool useInterpreter)
        {
            foreach (ushort value in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyUShortToNullableShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertUShortToUIntTest(bool useInterpreter)
        {
            foreach (ushort value in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyUShortToUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertUShortToNullableUIntTest(bool useInterpreter)
        {
            foreach (ushort value in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyUShortToNullableUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertUShortToULongTest(bool useInterpreter)
        {
            foreach (ushort value in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyUShortToULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertUShortToNullableULongTest(bool useInterpreter)
        {
            foreach (ushort value in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyUShortToNullableULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertUShortToUShortTest(bool useInterpreter)
        {
            foreach (ushort value in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyUShortToUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertUShortToNullableUShortTest(bool useInterpreter)
        {
            foreach (ushort value in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyUShortToNullableUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableUShortToByteTest(bool useInterpreter)
        {
            foreach (ushort? value in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyNullableUShortToByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableUShortToNullableByteTest(bool useInterpreter)
        {
            foreach (ushort? value in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyNullableUShortToNullableByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableUShortToCharTest(bool useInterpreter)
        {
            foreach (ushort? value in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyNullableUShortToChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableUShortToNullableCharTest(bool useInterpreter)
        {
            foreach (ushort? value in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyNullableUShortToNullableChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableUShortToDecimalTest(bool useInterpreter)
        {
            foreach (ushort? value in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyNullableUShortToDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableUShortToNullableDecimalTest(bool useInterpreter)
        {
            foreach (ushort? value in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyNullableUShortToNullableDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableUShortToDoubleTest(bool useInterpreter)
        {
            foreach (ushort? value in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyNullableUShortToDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableUShortToNullableDoubleTest(bool useInterpreter)
        {
            foreach (ushort? value in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyNullableUShortToNullableDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableUShortToEnumTest(bool useInterpreter)
        {
            foreach (ushort? value in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyNullableUShortToEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableUShortToNullableEnumTest(bool useInterpreter)
        {
            foreach (ushort? value in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyNullableUShortToNullableEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableUShortToEnumLongTest(bool useInterpreter)
        {
            foreach (ushort? value in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyNullableUShortToEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableUShortToNullableEnumLongTest(bool useInterpreter)
        {
            foreach (ushort? value in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyNullableUShortToNullableEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableUShortToFloatTest(bool useInterpreter)
        {
            foreach (ushort? value in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyNullableUShortToFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableUShortToNullableFloatTest(bool useInterpreter)
        {
            foreach (ushort? value in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyNullableUShortToNullableFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableUShortToIntTest(bool useInterpreter)
        {
            foreach (ushort? value in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyNullableUShortToInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableUShortToNullableIntTest(bool useInterpreter)
        {
            foreach (ushort? value in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyNullableUShortToNullableInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableUShortToLongTest(bool useInterpreter)
        {
            foreach (ushort? value in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyNullableUShortToLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableUShortToNullableLongTest(bool useInterpreter)
        {
            foreach (ushort? value in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyNullableUShortToNullableLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableUShortToSByteTest(bool useInterpreter)
        {
            foreach (ushort? value in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyNullableUShortToSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableUShortToNullableSByteTest(bool useInterpreter)
        {
            foreach (ushort? value in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyNullableUShortToNullableSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableUShortToShortTest(bool useInterpreter)
        {
            foreach (ushort? value in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyNullableUShortToShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableUShortToNullableShortTest(bool useInterpreter)
        {
            foreach (ushort? value in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyNullableUShortToNullableShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableUShortToUIntTest(bool useInterpreter)
        {
            foreach (ushort? value in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyNullableUShortToUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableUShortToNullableUIntTest(bool useInterpreter)
        {
            foreach (ushort? value in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyNullableUShortToNullableUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableUShortToULongTest(bool useInterpreter)
        {
            foreach (ushort? value in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyNullableUShortToULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableUShortToNullableULongTest(bool useInterpreter)
        {
            foreach (ushort? value in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyNullableUShortToNullableULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableUShortToUShortTest(bool useInterpreter)
        {
            foreach (ushort? value in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyNullableUShortToUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullableUShortToNullableUShortTest(bool useInterpreter)
        {
            foreach (ushort? value in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyNullableUShortToNullableUShort(value, useInterpreter);
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyByteToByte(byte value, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.Convert(Expression.Constant(value, typeof(byte)), typeof(byte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyByteToNullableByte(byte value, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.Convert(Expression.Constant(value, typeof(byte)), typeof(byte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyByteToChar(byte value, bool useInterpreter)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.Convert(Expression.Constant(value, typeof(byte)), typeof(char)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile(useInterpreter);

            Assert.Equal((char)value, f());
        }

        private static void VerifyByteToNullableChar(byte value, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.Convert(Expression.Constant(value, typeof(byte)), typeof(char?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile(useInterpreter);

            Assert.Equal((char)value, f());
        }

        private static void VerifyByteToDecimal(byte value, bool useInterpreter)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.Convert(Expression.Constant(value, typeof(byte)), typeof(decimal)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyByteToNullableDecimal(byte value, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.Convert(Expression.Constant(value, typeof(byte)), typeof(decimal?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyByteToDouble(byte value, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.Convert(Expression.Constant(value, typeof(byte)), typeof(double)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyByteToNullableDouble(byte value, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.Convert(Expression.Constant(value, typeof(byte)), typeof(double?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyByteToEnum(byte value, bool useInterpreter)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.Convert(Expression.Constant(value, typeof(byte)), typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile(useInterpreter);

            Assert.Equal((E)value, f());
        }

        private static void VerifyByteToNullableEnum(byte value, bool useInterpreter)
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.Convert(Expression.Constant(value, typeof(byte)), typeof(E?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile(useInterpreter);

            Assert.Equal((E)value, f());
        }

        private static void VerifyByteToEnumLong(byte value, bool useInterpreter)
        {
            Expression<Func<El>> e =
                Expression.Lambda<Func<El>>(
                    Expression.Convert(Expression.Constant(value, typeof(byte)), typeof(El)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El> f = e.Compile(useInterpreter);

            Assert.Equal((El)value, f());
        }

        private static void VerifyByteToNullableEnumLong(byte value, bool useInterpreter)
        {
            Expression<Func<El?>> e =
                Expression.Lambda<Func<El?>>(
                    Expression.Convert(Expression.Constant(value, typeof(byte)), typeof(El?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?> f = e.Compile(useInterpreter);

            Assert.Equal((El)value, f());
        }

        private static void VerifyByteToFloat(byte value, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.Convert(Expression.Constant(value, typeof(byte)), typeof(float)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyByteToNullableFloat(byte value, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.Convert(Expression.Constant(value, typeof(byte)), typeof(float?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyByteToInt(byte value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Convert(Expression.Constant(value, typeof(byte)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyByteToNullableInt(byte value, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.Convert(Expression.Constant(value, typeof(byte)), typeof(int?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyByteToLong(byte value, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.Convert(Expression.Constant(value, typeof(byte)), typeof(long)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyByteToNullableLong(byte value, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.Convert(Expression.Constant(value, typeof(byte)), typeof(long?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyByteToSByte(byte value, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.Convert(Expression.Constant(value, typeof(byte)), typeof(sbyte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((sbyte)value), f());
        }

        private static void VerifyByteToNullableSByte(byte value, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.Convert(Expression.Constant(value, typeof(byte)), typeof(sbyte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((sbyte)value), f());
        }

        private static void VerifyByteToShort(byte value, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.Convert(Expression.Constant(value, typeof(byte)), typeof(short)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyByteToNullableShort(byte value, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.Convert(Expression.Constant(value, typeof(byte)), typeof(short?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyByteToUInt(byte value, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.Convert(Expression.Constant(value, typeof(byte)), typeof(uint)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyByteToNullableUInt(byte value, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.Convert(Expression.Constant(value, typeof(byte)), typeof(uint?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyByteToULong(byte value, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.Convert(Expression.Constant(value, typeof(byte)), typeof(ulong)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyByteToNullableULong(byte value, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.Convert(Expression.Constant(value, typeof(byte)), typeof(ulong?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyByteToUShort(byte value, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.Convert(Expression.Constant(value, typeof(byte)), typeof(ushort)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyByteToNullableUShort(byte value, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.Convert(Expression.Constant(value, typeof(byte)), typeof(ushort?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableByteToByte(byte? value, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.Convert(Expression.Constant(value, typeof(byte?)), typeof(byte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableByteToNullableByte(byte? value, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.Convert(Expression.Constant(value, typeof(byte?)), typeof(byte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableByteToChar(byte? value, bool useInterpreter)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.Convert(Expression.Constant(value, typeof(byte?)), typeof(char)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal((char)value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableByteToNullableChar(byte? value, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.Convert(Expression.Constant(value, typeof(byte?)), typeof(char?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile(useInterpreter);

            Assert.Equal((char?)value, f());
        }

        private static void VerifyNullableByteToDecimal(byte? value, bool useInterpreter)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.Convert(Expression.Constant(value, typeof(byte?)), typeof(decimal)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableByteToNullableDecimal(byte? value, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.Convert(Expression.Constant(value, typeof(byte?)), typeof(decimal?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableByteToDouble(byte? value, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.Convert(Expression.Constant(value, typeof(byte?)), typeof(double)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableByteToNullableDouble(byte? value, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.Convert(Expression.Constant(value, typeof(byte?)), typeof(double?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableByteToEnum(byte? value, bool useInterpreter)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.Convert(Expression.Constant(value, typeof(byte?)), typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal((E)value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableByteToNullableEnum(byte? value, bool useInterpreter)
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.Convert(Expression.Constant(value, typeof(byte?)), typeof(E?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile(useInterpreter);

            Assert.Equal((E?)value, f());
        }

        private static void VerifyNullableByteToEnumLong(byte? value, bool useInterpreter)
        {
            Expression<Func<El>> e =
                Expression.Lambda<Func<El>>(
                    Expression.Convert(Expression.Constant(value, typeof(byte?)), typeof(El)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal((El)value, f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableByteToNullableEnumLong(byte? value, bool useInterpreter)
        {
            Expression<Func<El?>> e =
                Expression.Lambda<Func<El?>>(
                    Expression.Convert(Expression.Constant(value, typeof(byte?)), typeof(El?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?> f = e.Compile(useInterpreter);

            Assert.Equal((El?)value, f());
        }

        private static void VerifyNullableByteToFloat(byte? value, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.Convert(Expression.Constant(value, typeof(byte?)), typeof(float)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableByteToNullableFloat(byte? value, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.Convert(Expression.Constant(value, typeof(byte?)), typeof(float?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableByteToInt(byte? value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Convert(Expression.Constant(value, typeof(byte?)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableByteToNullableInt(byte? value, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.Convert(Expression.Constant(value, typeof(byte?)), typeof(int?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableByteToLong(byte? value, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.Convert(Expression.Constant(value, typeof(byte?)), typeof(long)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableByteToNullableLong(byte? value, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.Convert(Expression.Constant(value, typeof(byte?)), typeof(long?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableByteToSByte(byte? value, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.Convert(Expression.Constant(value, typeof(byte?)), typeof(sbyte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((sbyte)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableByteToNullableSByte(byte? value, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.Convert(Expression.Constant(value, typeof(byte?)), typeof(sbyte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((sbyte?)value), f());
        }

        private static void VerifyNullableByteToShort(byte? value, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.Convert(Expression.Constant(value, typeof(byte?)), typeof(short)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableByteToNullableShort(byte? value, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.Convert(Expression.Constant(value, typeof(byte?)), typeof(short?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableByteToUInt(byte? value, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.Convert(Expression.Constant(value, typeof(byte?)), typeof(uint)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableByteToNullableUInt(byte? value, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.Convert(Expression.Constant(value, typeof(byte?)), typeof(uint?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableByteToULong(byte? value, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.Convert(Expression.Constant(value, typeof(byte?)), typeof(ulong)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableByteToNullableULong(byte? value, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.Convert(Expression.Constant(value, typeof(byte?)), typeof(ulong?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableByteToUShort(byte? value, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.Convert(Expression.Constant(value, typeof(byte?)), typeof(ushort)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableByteToNullableUShort(byte? value, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.Convert(Expression.Constant(value, typeof(byte?)), typeof(ushort?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCharToByte(char value, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.Convert(Expression.Constant(value, typeof(char)), typeof(byte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((byte)value), f());
        }

        private static void VerifyCharToNullableByte(char value, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.Convert(Expression.Constant(value, typeof(char)), typeof(byte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((byte)value), f());
        }

        private static void VerifyCharToChar(char value, bool useInterpreter)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.Convert(Expression.Constant(value, typeof(char)), typeof(char)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCharToNullableChar(char value, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.Convert(Expression.Constant(value, typeof(char)), typeof(char?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCharToDecimal(char value, bool useInterpreter)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.Convert(Expression.Constant(value, typeof(char)), typeof(decimal)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCharToNullableDecimal(char value, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.Convert(Expression.Constant(value, typeof(char)), typeof(decimal?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCharToDouble(char value, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.Convert(Expression.Constant(value, typeof(char)), typeof(double)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCharToNullableDouble(char value, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.Convert(Expression.Constant(value, typeof(char)), typeof(double?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCharToEnum(char value, bool useInterpreter)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.Convert(Expression.Constant(value, typeof(char)), typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile(useInterpreter);

            Assert.Equal((E)value, f());
        }

        private static void VerifyCharToNullableEnum(char value, bool useInterpreter)
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.Convert(Expression.Constant(value, typeof(char)), typeof(E?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile(useInterpreter);

            Assert.Equal((E)value, f());
        }

        private static void VerifyCharToEnumLong(char value, bool useInterpreter)
        {
            Expression<Func<El>> e =
                Expression.Lambda<Func<El>>(
                    Expression.Convert(Expression.Constant(value, typeof(char)), typeof(El)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El> f = e.Compile(useInterpreter);

            Assert.Equal((El)value, f());
        }

        private static void VerifyCharToNullableEnumLong(char value, bool useInterpreter)
        {
            Expression<Func<El?>> e =
                Expression.Lambda<Func<El?>>(
                    Expression.Convert(Expression.Constant(value, typeof(char)), typeof(El?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?> f = e.Compile(useInterpreter);

            Assert.Equal((El)value, f());
        }

        private static void VerifyCharToFloat(char value, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.Convert(Expression.Constant(value, typeof(char)), typeof(float)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCharToNullableFloat(char value, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.Convert(Expression.Constant(value, typeof(char)), typeof(float?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCharToInt(char value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Convert(Expression.Constant(value, typeof(char)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCharToNullableInt(char value, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.Convert(Expression.Constant(value, typeof(char)), typeof(int?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCharToLong(char value, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.Convert(Expression.Constant(value, typeof(char)), typeof(long)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCharToNullableLong(char value, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.Convert(Expression.Constant(value, typeof(char)), typeof(long?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCharToSByte(char value, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.Convert(Expression.Constant(value, typeof(char)), typeof(sbyte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((sbyte)value), f());
        }

        private static void VerifyCharToNullableSByte(char value, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.Convert(Expression.Constant(value, typeof(char)), typeof(sbyte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((sbyte)value), f());
        }

        private static void VerifyCharToShort(char value, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.Convert(Expression.Constant(value, typeof(char)), typeof(short)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((short)value), f());
        }

        private static void VerifyCharToNullableShort(char value, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.Convert(Expression.Constant(value, typeof(char)), typeof(short?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((short)value), f());
        }

        private static void VerifyCharToUInt(char value, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.Convert(Expression.Constant(value, typeof(char)), typeof(uint)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCharToNullableUInt(char value, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.Convert(Expression.Constant(value, typeof(char)), typeof(uint?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCharToULong(char value, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.Convert(Expression.Constant(value, typeof(char)), typeof(ulong)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCharToNullableULong(char value, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.Convert(Expression.Constant(value, typeof(char)), typeof(ulong?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCharToUShort(char value, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.Convert(Expression.Constant(value, typeof(char)), typeof(ushort)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyCharToNullableUShort(char value, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.Convert(Expression.Constant(value, typeof(char)), typeof(ushort?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableCharToByte(char? value, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.Convert(Expression.Constant(value, typeof(char?)), typeof(byte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((byte)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableCharToNullableByte(char? value, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.Convert(Expression.Constant(value, typeof(char?)), typeof(byte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((byte?)value), f());
        }

        private static void VerifyNullableCharToChar(char? value, bool useInterpreter)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.Convert(Expression.Constant(value, typeof(char?)), typeof(char)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableCharToNullableChar(char? value, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.Convert(Expression.Constant(value, typeof(char?)), typeof(char?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableCharToDecimal(char? value, bool useInterpreter)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.Convert(Expression.Constant(value, typeof(char?)), typeof(decimal)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableCharToNullableDecimal(char? value, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.Convert(Expression.Constant(value, typeof(char?)), typeof(decimal?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableCharToDouble(char? value, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.Convert(Expression.Constant(value, typeof(char?)), typeof(double)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableCharToNullableDouble(char? value, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.Convert(Expression.Constant(value, typeof(char?)), typeof(double?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableCharToEnum(char? value, bool useInterpreter)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.Convert(Expression.Constant(value, typeof(char?)), typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal((E)value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableCharToNullableEnum(char? value, bool useInterpreter)
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.Convert(Expression.Constant(value, typeof(char?)), typeof(E?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile(useInterpreter);

            Assert.Equal((E?)value, f());
        }

        private static void VerifyNullableCharToEnumLong(char? value, bool useInterpreter)
        {
            Expression<Func<El>> e =
                Expression.Lambda<Func<El>>(
                    Expression.Convert(Expression.Constant(value, typeof(char?)), typeof(El)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal((El)value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableCharToNullableEnumLong(char? value, bool useInterpreter)
        {
            Expression<Func<El?>> e =
                Expression.Lambda<Func<El?>>(
                    Expression.Convert(Expression.Constant(value, typeof(char?)), typeof(El?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?> f = e.Compile(useInterpreter);

            Assert.Equal((El?)value, f());
        }

        private static void VerifyNullableCharToFloat(char? value, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.Convert(Expression.Constant(value, typeof(char?)), typeof(float)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableCharToNullableFloat(char? value, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.Convert(Expression.Constant(value, typeof(char?)), typeof(float?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableCharToInt(char? value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Convert(Expression.Constant(value, typeof(char?)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableCharToNullableInt(char? value, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.Convert(Expression.Constant(value, typeof(char?)), typeof(int?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableCharToLong(char? value, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.Convert(Expression.Constant(value, typeof(char?)), typeof(long)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableCharToNullableLong(char? value, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.Convert(Expression.Constant(value, typeof(char?)), typeof(long?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableCharToSByte(char? value, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.Convert(Expression.Constant(value, typeof(char?)), typeof(sbyte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((sbyte)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableCharToNullableSByte(char? value, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.Convert(Expression.Constant(value, typeof(char?)), typeof(sbyte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((sbyte?)value), f());
        }

        private static void VerifyNullableCharToShort(char? value, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.Convert(Expression.Constant(value, typeof(char?)), typeof(short)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((short)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableCharToNullableShort(char? value, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.Convert(Expression.Constant(value, typeof(char?)), typeof(short?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((short?)value), f());
        }

        private static void VerifyNullableCharToUInt(char? value, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.Convert(Expression.Constant(value, typeof(char?)), typeof(uint)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableCharToNullableUInt(char? value, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.Convert(Expression.Constant(value, typeof(char?)), typeof(uint?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableCharToULong(char? value, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.Convert(Expression.Constant(value, typeof(char?)), typeof(ulong)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableCharToNullableULong(char? value, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.Convert(Expression.Constant(value, typeof(char?)), typeof(ulong?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableCharToUShort(char? value, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.Convert(Expression.Constant(value, typeof(char?)), typeof(ushort)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableCharToNullableUShort(char? value, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.Convert(Expression.Constant(value, typeof(char?)), typeof(ushort?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyDecimalToByte(decimal value, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.Convert(Expression.Constant(value, typeof(decimal)), typeof(byte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile(useInterpreter);

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

        private static void VerifyDecimalToNullableByte(decimal value, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.Convert(Expression.Constant(value, typeof(decimal)), typeof(byte?)),
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

        private static void VerifyDecimalToChar(decimal value, bool useInterpreter)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.Convert(Expression.Constant(value, typeof(decimal)), typeof(char)),
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

        private static void VerifyDecimalToNullableChar(decimal value, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.Convert(Expression.Constant(value, typeof(decimal)), typeof(char?)),
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

        private static void VerifyDecimalToDecimal(decimal value, bool useInterpreter)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.Convert(Expression.Constant(value, typeof(decimal)), typeof(decimal)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyDecimalToNullableDecimal(decimal value, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.Convert(Expression.Constant(value, typeof(decimal)), typeof(decimal?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyDecimalToDouble(decimal value, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.Convert(Expression.Constant(value, typeof(decimal)), typeof(double)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);

            Assert.Equal((double)value, f());
        }

        private static void VerifyDecimalToNullableDouble(decimal value, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.Convert(Expression.Constant(value, typeof(decimal)), typeof(double?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal((double)value, f());
        }

        private static void VerifyDecimalToFloat(decimal value, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.Convert(Expression.Constant(value, typeof(decimal)), typeof(float)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);

            Assert.Equal((float)value, f());
        }

        private static void VerifyDecimalToNullableFloat(decimal value, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.Convert(Expression.Constant(value, typeof(decimal)), typeof(float?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal((float)value, f());
        }

        private static void VerifyDecimalToInt(decimal value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Convert(Expression.Constant(value, typeof(decimal)), typeof(int)),
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

        private static void VerifyDecimalToNullableInt(decimal value, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.Convert(Expression.Constant(value, typeof(decimal)), typeof(int?)),
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

        private static void VerifyDecimalToLong(decimal value, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.Convert(Expression.Constant(value, typeof(decimal)), typeof(long)),
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

        private static void VerifyDecimalToNullableLong(decimal value, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.Convert(Expression.Constant(value, typeof(decimal)), typeof(long?)),
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

        private static void VerifyDecimalToSByte(decimal value, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.Convert(Expression.Constant(value, typeof(decimal)), typeof(sbyte)),
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

        private static void VerifyDecimalToNullableSByte(decimal value, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.Convert(Expression.Constant(value, typeof(decimal)), typeof(sbyte?)),
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

        private static void VerifyDecimalToShort(decimal value, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.Convert(Expression.Constant(value, typeof(decimal)), typeof(short)),
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

        private static void VerifyDecimalToNullableShort(decimal value, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.Convert(Expression.Constant(value, typeof(decimal)), typeof(short?)),
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

        private static void VerifyDecimalToUInt(decimal value, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.Convert(Expression.Constant(value, typeof(decimal)), typeof(uint)),
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

        private static void VerifyDecimalToNullableUInt(decimal value, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.Convert(Expression.Constant(value, typeof(decimal)), typeof(uint?)),
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

        private static void VerifyDecimalToULong(decimal value, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.Convert(Expression.Constant(value, typeof(decimal)), typeof(ulong)),
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

        private static void VerifyDecimalToNullableULong(decimal value, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.Convert(Expression.Constant(value, typeof(decimal)), typeof(ulong?)),
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

        private static void VerifyDecimalToUShort(decimal value, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.Convert(Expression.Constant(value, typeof(decimal)), typeof(ushort)),
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

        private static void VerifyDecimalToNullableUShort(decimal value, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.Convert(Expression.Constant(value, typeof(decimal)), typeof(ushort?)),
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

        private static void VerifyNullableDecimalToByte(decimal? value, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.Convert(Expression.Constant(value, typeof(decimal?)), typeof(byte)),
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

        private static void VerifyNullableDecimalToNullableByte(decimal? value, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.Convert(Expression.Constant(value, typeof(decimal?)), typeof(byte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile(useInterpreter);

            byte? expected = null;
            try
            {
                expected = (byte?)value;
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyNullableDecimalToChar(decimal? value, bool useInterpreter)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.Convert(Expression.Constant(value, typeof(decimal?)), typeof(char)),
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

        private static void VerifyNullableDecimalToNullableChar(decimal? value, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.Convert(Expression.Constant(value, typeof(decimal?)), typeof(char?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile(useInterpreter);

            char? expected = null;
            try
            {
                expected = (char?)value;
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyNullableDecimalToDecimal(decimal? value, bool useInterpreter)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.Convert(Expression.Constant(value, typeof(decimal?)), typeof(decimal)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableDecimalToNullableDecimal(decimal? value, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.Convert(Expression.Constant(value, typeof(decimal?)), typeof(decimal?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableDecimalToDouble(decimal? value, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.Convert(Expression.Constant(value, typeof(decimal?)), typeof(double)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal((double)value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableDecimalToNullableDouble(decimal? value, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.Convert(Expression.Constant(value, typeof(decimal?)), typeof(double?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal((double?)value, f());
        }

        private static void VerifyNullableDecimalToFloat(decimal? value, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.Convert(Expression.Constant(value, typeof(decimal?)), typeof(float)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal((float)value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableDecimalToNullableFloat(decimal? value, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.Convert(Expression.Constant(value, typeof(decimal?)), typeof(float?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal((float?)value, f());
        }

        private static void VerifyNullableDecimalToInt(decimal? value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Convert(Expression.Constant(value, typeof(decimal?)), typeof(int)),
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

        private static void VerifyNullableDecimalToNullableInt(decimal? value, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.Convert(Expression.Constant(value, typeof(decimal?)), typeof(int?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);

            int? expected = null;
            try
            {
                expected = (int?)value;
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyNullableDecimalToLong(decimal? value, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.Convert(Expression.Constant(value, typeof(decimal?)), typeof(long)),
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

        private static void VerifyNullableDecimalToNullableLong(decimal? value, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.Convert(Expression.Constant(value, typeof(decimal?)), typeof(long?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);

            long? expected = null;
            try
            {
                expected = (long?)value;
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyNullableDecimalToSByte(decimal? value, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.Convert(Expression.Constant(value, typeof(decimal?)), typeof(sbyte)),
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

        private static void VerifyNullableDecimalToNullableSByte(decimal? value, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.Convert(Expression.Constant(value, typeof(decimal?)), typeof(sbyte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile(useInterpreter);

            sbyte? expected = null;
            try
            {
                expected = (sbyte?)value;
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyNullableDecimalToShort(decimal? value, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.Convert(Expression.Constant(value, typeof(decimal?)), typeof(short)),
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

        private static void VerifyNullableDecimalToNullableShort(decimal? value, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.Convert(Expression.Constant(value, typeof(decimal?)), typeof(short?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);

            short? expected = null;
            try
            {
                expected = (short?)value;
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyNullableDecimalToUInt(decimal? value, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.Convert(Expression.Constant(value, typeof(decimal?)), typeof(uint)),
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

        private static void VerifyNullableDecimalToNullableUInt(decimal? value, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.Convert(Expression.Constant(value, typeof(decimal?)), typeof(uint?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);

            uint? expected = null;
            try
            {
                expected = (uint?)value;
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyNullableDecimalToULong(decimal? value, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.Convert(Expression.Constant(value, typeof(decimal?)), typeof(ulong)),
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

        private static void VerifyNullableDecimalToNullableULong(decimal? value, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.Convert(Expression.Constant(value, typeof(decimal?)), typeof(ulong?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);

            ulong? expected = null;
            try
            {
                expected = (ulong?)value;
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyNullableDecimalToUShort(decimal? value, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.Convert(Expression.Constant(value, typeof(decimal?)), typeof(ushort)),
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

        private static void VerifyNullableDecimalToNullableUShort(decimal? value, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.Convert(Expression.Constant(value, typeof(decimal?)), typeof(ushort?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);

            ushort? expected = null;
            try
            {
                expected = (ushort?)value;
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyDoubleToByte(double value, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.Convert(Expression.Constant(value, typeof(double)), typeof(byte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((byte)value), f());
        }

        private static void VerifyDoubleToNullableByte(double value, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.Convert(Expression.Constant(value, typeof(double)), typeof(byte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((byte)value), f());
        }

        private static void VerifyDoubleToChar(double value, bool useInterpreter)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.Convert(Expression.Constant(value, typeof(double)), typeof(char)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((char)value), f());
        }

        private static void VerifyDoubleToNullableChar(double value, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.Convert(Expression.Constant(value, typeof(double)), typeof(char?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((char?)value), f());
        }

        private static void VerifyDoubleToDecimal(double value, bool useInterpreter)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.Convert(Expression.Constant(value, typeof(double)), typeof(decimal)),
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

        private static void VerifyDoubleToNullableDecimal(double value, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.Convert(Expression.Constant(value, typeof(double)), typeof(decimal?)),
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

        private static void VerifyDoubleToDouble(double value, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.Convert(Expression.Constant(value, typeof(double)), typeof(double)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyDoubleToNullableDouble(double value, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.Convert(Expression.Constant(value, typeof(double)), typeof(double?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyDoubleToEnum(double value, bool useInterpreter)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.Convert(Expression.Constant(value, typeof(double)), typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((E)value), f());
        }

        private static void VerifyDoubleToNullableEnum(double value, bool useInterpreter)
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.Convert(Expression.Constant(value, typeof(double)), typeof(E?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((E)value), f());
        }

        private static void VerifyDoubleToEnumLong(double value, bool useInterpreter)
        {
            Expression<Func<El>> e =
                Expression.Lambda<Func<El>>(
                    Expression.Convert(Expression.Constant(value, typeof(double)), typeof(El)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((El)value), f());
        }

        private static void VerifyDoubleToNullableEnumLong(double value, bool useInterpreter)
        {
            Expression<Func<El?>> e =
                Expression.Lambda<Func<El?>>(
                    Expression.Convert(Expression.Constant(value, typeof(double)), typeof(El?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((El)value), f());
        }

        private static void VerifyDoubleToFloat(double value, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.Convert(Expression.Constant(value, typeof(double)), typeof(float)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);

            Assert.Equal((float)value, f());
        }

        private static void VerifyDoubleToNullableFloat(double value, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.Convert(Expression.Constant(value, typeof(double)), typeof(float?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal((float)value, f());
        }

        private static void VerifyDoubleToInt(double value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Convert(Expression.Constant(value, typeof(double)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((int)value), f());
        }

        private static void VerifyDoubleToNullableInt(double value, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.Convert(Expression.Constant(value, typeof(double)), typeof(int?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((int)value), f());
        }

        private static void VerifyDoubleToLong(double value, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.Convert(Expression.Constant(value, typeof(double)), typeof(long)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((long)value), f());
        }

        private static void VerifyDoubleToNullableLong(double value, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.Convert(Expression.Constant(value, typeof(double)), typeof(long?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((long)value), f());
        }

        private static void VerifyDoubleToSByte(double value, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.Convert(Expression.Constant(value, typeof(double)), typeof(sbyte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((sbyte)value), f());
        }

        private static void VerifyDoubleToNullableSByte(double value, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.Convert(Expression.Constant(value, typeof(double)), typeof(sbyte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((sbyte)value), f());
        }

        private static void VerifyDoubleToShort(double value, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.Convert(Expression.Constant(value, typeof(double)), typeof(short)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((short)value), f());
        }

        private static void VerifyDoubleToNullableShort(double value, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.Convert(Expression.Constant(value, typeof(double)), typeof(short?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((short)value), f());
        }

        private static void VerifyDoubleToUInt(double value, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.Convert(Expression.Constant(value, typeof(double)), typeof(uint)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((uint)value), f());
        }

        private static void VerifyDoubleToNullableUInt(double value, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.Convert(Expression.Constant(value, typeof(double)), typeof(uint?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((uint)value), f());
        }

        private static void VerifyDoubleToULong(double value, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.Convert(Expression.Constant(value, typeof(double)), typeof(ulong)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((ulong)value), f());
        }

        private static void VerifyDoubleToNullableULong(double value, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.Convert(Expression.Constant(value, typeof(double)), typeof(ulong?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((ulong)value), f());
        }

        private static void VerifyDoubleToUShort(double value, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.Convert(Expression.Constant(value, typeof(double)), typeof(ushort)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((ushort)value), f());
        }

        private static void VerifyDoubleToNullableUShort(double value, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.Convert(Expression.Constant(value, typeof(double)), typeof(ushort?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((ushort)value), f());
        }

        private static void VerifyNullableDoubleToByte(double? value, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.Convert(Expression.Constant(value, typeof(double?)), typeof(byte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((byte)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableDoubleToNullableByte(double? value, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.Convert(Expression.Constant(value, typeof(double?)), typeof(byte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((byte?)value), f());
        }

        private static void VerifyNullableDoubleToChar(double? value, bool useInterpreter)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.Convert(Expression.Constant(value, typeof(double?)), typeof(char)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((char)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableDoubleToNullableChar(double? value, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.Convert(Expression.Constant(value, typeof(double?)), typeof(char?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((char?)value), f());
        }

        private static void VerifyNullableDoubleToDecimal(double? value, bool useInterpreter)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.Convert(Expression.Constant(value, typeof(double?)), typeof(decimal)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                decimal expected = 0;
                try
                {
                    expected = (decimal)value.GetValueOrDefault();
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

        private static void VerifyNullableDoubleToNullableDecimal(double? value, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.Convert(Expression.Constant(value, typeof(double?)), typeof(decimal?)),
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

        private static void VerifyNullableDoubleToDouble(double? value, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.Convert(Expression.Constant(value, typeof(double?)), typeof(double)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableDoubleToNullableDouble(double? value, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.Convert(Expression.Constant(value, typeof(double?)), typeof(double?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableDoubleToEnum(double? value, bool useInterpreter)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.Convert(Expression.Constant(value, typeof(double?)), typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((E)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableDoubleToNullableEnum(double? value, bool useInterpreter)
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.Convert(Expression.Constant(value, typeof(double?)), typeof(E?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((E?)value), f());
        }

        private static void VerifyNullableDoubleToEnumLong(double? value, bool useInterpreter)
        {
            Expression<Func<El>> e =
                Expression.Lambda<Func<El>>(
                    Expression.Convert(Expression.Constant(value, typeof(double?)), typeof(El)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((El)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableDoubleToNullableEnumLong(double? value, bool useInterpreter)
        {
            Expression<Func<El?>> e =
                Expression.Lambda<Func<El?>>(
                    Expression.Convert(Expression.Constant(value, typeof(double?)), typeof(El?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((El?)value), f());
        }

        private static void VerifyNullableDoubleToFloat(double? value, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.Convert(Expression.Constant(value, typeof(double?)), typeof(float)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal((float)value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableDoubleToNullableFloat(double? value, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.Convert(Expression.Constant(value, typeof(double?)), typeof(float?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal((float?)value, f());
        }

        private static void VerifyNullableDoubleToInt(double? value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Convert(Expression.Constant(value, typeof(double?)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((int)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableDoubleToNullableInt(double? value, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.Convert(Expression.Constant(value, typeof(double?)), typeof(int?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((int?)value), f());
        }

        private static void VerifyNullableDoubleToLong(double? value, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.Convert(Expression.Constant(value, typeof(double?)), typeof(long)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((long)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableDoubleToNullableLong(double? value, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.Convert(Expression.Constant(value, typeof(double?)), typeof(long?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((long?)value), f());
        }

        private static void VerifyNullableDoubleToSByte(double? value, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.Convert(Expression.Constant(value, typeof(double?)), typeof(sbyte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((sbyte)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableDoubleToNullableSByte(double? value, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.Convert(Expression.Constant(value, typeof(double?)), typeof(sbyte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((sbyte?)value), f());
        }

        private static void VerifyNullableDoubleToShort(double? value, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.Convert(Expression.Constant(value, typeof(double?)), typeof(short)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((short)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableDoubleToNullableShort(double? value, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.Convert(Expression.Constant(value, typeof(double?)), typeof(short?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((short?)value), f());
        }

        private static void VerifyNullableDoubleToUInt(double? value, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.Convert(Expression.Constant(value, typeof(double?)), typeof(uint)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((uint)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableDoubleToNullableUInt(double? value, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.Convert(Expression.Constant(value, typeof(double?)), typeof(uint?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((uint?)value), f());
        }

        private static void VerifyNullableDoubleToULong(double? value, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.Convert(Expression.Constant(value, typeof(double?)), typeof(ulong)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((ulong)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableDoubleToNullableULong(double? value, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.Convert(Expression.Constant(value, typeof(double?)), typeof(ulong?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((ulong?)value), f());
        }

        private static void VerifyNullableDoubleToUShort(double? value, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.Convert(Expression.Constant(value, typeof(double?)), typeof(ushort)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((ushort)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableDoubleToNullableUShort(double? value, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.Convert(Expression.Constant(value, typeof(double?)), typeof(ushort?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((ushort?)value), f());
        }

        private static void VerifyEnumToByte(E value, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.Convert(Expression.Constant(value, typeof(E)), typeof(byte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((byte)value), f());
        }

        private static void VerifyEnumToNullableByte(E value, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.Convert(Expression.Constant(value, typeof(E)), typeof(byte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((byte)value), f());
        }

        private static void VerifyEnumToChar(E value, bool useInterpreter)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.Convert(Expression.Constant(value, typeof(E)), typeof(char)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((char)value), f());
        }

        private static void VerifyEnumToNullableChar(E value, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.Convert(Expression.Constant(value, typeof(E)), typeof(char?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((char?)value), f());
        }

        private static void VerifyEnumToDouble(E value, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.Convert(Expression.Constant(value, typeof(E)), typeof(double)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);

            Assert.Equal((double)value, f());
        }

        private static void VerifyEnumToNullableDouble(E value, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.Convert(Expression.Constant(value, typeof(E)), typeof(double?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal((double)value, f());
        }

        private static void VerifyEnumToEnum(E value, bool useInterpreter)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.Convert(Expression.Constant(value, typeof(E)), typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyEnumToNullableEnum(E value, bool useInterpreter)
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.Convert(Expression.Constant(value, typeof(E)), typeof(E?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyEnumToEnumLong(E value, bool useInterpreter)
        {
            Expression<Func<El>> e =
                Expression.Lambda<Func<El>>(
                    Expression.Convert(Expression.Constant(value, typeof(E)), typeof(El)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El> f = e.Compile(useInterpreter);

            Assert.Equal((El)value, f());
        }

        private static void VerifyEnumToNullableEnumLong(E value, bool useInterpreter)
        {
            Expression<Func<El?>> e =
                Expression.Lambda<Func<El?>>(
                    Expression.Convert(Expression.Constant(value, typeof(E)), typeof(El?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?> f = e.Compile(useInterpreter);

            Assert.Equal((El)value, f());
        }

        private static void VerifyEnumToFloat(E value, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.Convert(Expression.Constant(value, typeof(E)), typeof(float)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);

            Assert.Equal((float)value, f());
        }

        private static void VerifyEnumToNullableFloat(E value, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.Convert(Expression.Constant(value, typeof(E)), typeof(float?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal((float)value, f());
        }

        private static void VerifyEnumToInt(E value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Convert(Expression.Constant(value, typeof(E)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            Assert.Equal((int)value, f());
        }

        private static void VerifyEnumToNullableInt(E value, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.Convert(Expression.Constant(value, typeof(E)), typeof(int?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);

            Assert.Equal((int)value, f());
        }

        private static void VerifyEnumToLong(E value, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.Convert(Expression.Constant(value, typeof(E)), typeof(long)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);

            Assert.Equal((long)value, f());
        }

        private static void VerifyEnumToNullableLong(E value, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.Convert(Expression.Constant(value, typeof(E)), typeof(long?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);

            Assert.Equal((long)value, f());
        }

        private static void VerifyEnumToSByte(E value, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.Convert(Expression.Constant(value, typeof(E)), typeof(sbyte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((sbyte)value), f());
        }

        private static void VerifyEnumToNullableSByte(E value, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.Convert(Expression.Constant(value, typeof(E)), typeof(sbyte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((sbyte)value), f());
        }

        private static void VerifyEnumToShort(E value, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.Convert(Expression.Constant(value, typeof(E)), typeof(short)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((short)value), f());
        }

        private static void VerifyEnumToNullableShort(E value, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.Convert(Expression.Constant(value, typeof(E)), typeof(short?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((short)value), f());
        }

        private static void VerifyEnumToUInt(E value, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.Convert(Expression.Constant(value, typeof(E)), typeof(uint)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((uint)value), f());
        }

        private static void VerifyEnumToNullableUInt(E value, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.Convert(Expression.Constant(value, typeof(E)), typeof(uint?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((uint?)value), f());
        }

        private static void VerifyEnumToULong(E value, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.Convert(Expression.Constant(value, typeof(E)), typeof(ulong)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((ulong)value), f());
        }

        private static void VerifyEnumToNullableULong(E value, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.Convert(Expression.Constant(value, typeof(E)), typeof(ulong?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((ulong)value), f());
        }

        private static void VerifyEnumToUShort(E value, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.Convert(Expression.Constant(value, typeof(E)), typeof(ushort)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((ushort)value), f());
        }

        private static void VerifyEnumToNullableUShort(E value, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.Convert(Expression.Constant(value, typeof(E)), typeof(ushort?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((ushort)value), f());
        }

        private static void VerifyNullableEnumToByte(E? value, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.Convert(Expression.Constant(value, typeof(E?)), typeof(byte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((byte)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableEnumToNullableByte(E? value, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.Convert(Expression.Constant(value, typeof(E?)), typeof(byte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((byte?)value), f());
        }

        private static void VerifyNullableEnumToChar(E? value, bool useInterpreter)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.Convert(Expression.Constant(value, typeof(E?)), typeof(char)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((char)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableEnumToNullableChar(E? value, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.Convert(Expression.Constant(value, typeof(E?)), typeof(char?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((char?)value), f());
        }

        private static void VerifyNullableEnumToDouble(E? value, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.Convert(Expression.Constant(value, typeof(E?)), typeof(double)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal((double)value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableEnumToNullableDouble(E? value, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.Convert(Expression.Constant(value, typeof(E?)), typeof(double?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal((double?)value, f());
        }

        private static void VerifyNullableEnumToEnum(E? value, bool useInterpreter)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.Convert(Expression.Constant(value, typeof(E?)), typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableEnumToNullableEnum(E? value, bool useInterpreter)
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.Convert(Expression.Constant(value, typeof(E?)), typeof(E?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableEnumToEnumLong(E? value, bool useInterpreter)
        {
            Expression<Func<El>> e =
                Expression.Lambda<Func<El>>(
                    Expression.Convert(Expression.Constant(value, typeof(E?)), typeof(El)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal((El)value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableEnumToNullableEnumLong(E? value, bool useInterpreter)
        {
            Expression<Func<El?>> e =
                Expression.Lambda<Func<El?>>(
                    Expression.Convert(Expression.Constant(value, typeof(E?)), typeof(El?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?> f = e.Compile(useInterpreter);

            Assert.Equal((El?)value, f());
        }

        private static void VerifyNullableEnumToFloat(E? value, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.Convert(Expression.Constant(value, typeof(E?)), typeof(float)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal((float)value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableEnumToNullableFloat(E? value, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.Convert(Expression.Constant(value, typeof(E?)), typeof(float?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal((float?)value, f());
        }

        private static void VerifyNullableEnumToInt(E? value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Convert(Expression.Constant(value, typeof(E?)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal((int)value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableEnumToNullableInt(E? value, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.Convert(Expression.Constant(value, typeof(E?)), typeof(int?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);

            Assert.Equal((int?)value, f());
        }

        private static void VerifyNullableEnumToLong(E? value, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.Convert(Expression.Constant(value, typeof(E?)), typeof(long)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal((long)value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableEnumToNullableLong(E? value, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.Convert(Expression.Constant(value, typeof(E?)), typeof(long?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);

            Assert.Equal((long?)value, f());
        }

        private static void VerifyNullableEnumToSByte(E? value, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.Convert(Expression.Constant(value, typeof(E?)), typeof(sbyte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((sbyte)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableEnumToNullableSByte(E? value, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.Convert(Expression.Constant(value, typeof(E?)), typeof(sbyte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((sbyte?)value), f());
        }

        private static void VerifyNullableEnumToShort(E? value, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.Convert(Expression.Constant(value, typeof(E?)), typeof(short)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((short)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableEnumToNullableShort(E? value, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.Convert(Expression.Constant(value, typeof(E?)), typeof(short?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((short?)value), f());
        }

        private static void VerifyNullableEnumToUInt(E? value, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.Convert(Expression.Constant(value, typeof(E?)), typeof(uint)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((uint)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableEnumToNullableUInt(E? value, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.Convert(Expression.Constant(value, typeof(E?)), typeof(uint?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((uint?)value), f());
        }

        private static void VerifyNullableEnumToULong(E? value, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.Convert(Expression.Constant(value, typeof(E?)), typeof(ulong)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((ulong)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableEnumToNullableULong(E? value, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.Convert(Expression.Constant(value, typeof(E?)), typeof(ulong?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((ulong?)value), f());
        }

        private static void VerifyNullableEnumToUShort(E? value, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.Convert(Expression.Constant(value, typeof(E?)), typeof(ushort)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((ushort)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableEnumToNullableUShort(E? value, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.Convert(Expression.Constant(value, typeof(E?)), typeof(ushort?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((ushort?)value), f());
        }

        private static void VerifyEnumLongToByte(El value, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.Convert(Expression.Constant(value, typeof(El)), typeof(byte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((byte)value), f());
        }

        private static void VerifyEnumLongToNullableByte(El value, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.Convert(Expression.Constant(value, typeof(El)), typeof(byte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((byte)value), f());
        }

        private static void VerifyEnumLongToChar(El value, bool useInterpreter)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.Convert(Expression.Constant(value, typeof(El)), typeof(char)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((char)value), f());
        }

        private static void VerifyEnumLongToNullableChar(El value, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.Convert(Expression.Constant(value, typeof(El)), typeof(char?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((char?)value), f());
        }

        private static void VerifyEnumLongToDouble(El value, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.Convert(Expression.Constant(value, typeof(El)), typeof(double)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);

            Assert.Equal((double)value, f());
        }

        private static void VerifyEnumLongToNullableDouble(El value, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.Convert(Expression.Constant(value, typeof(El)), typeof(double?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal((double)value, f());
        }

        private static void VerifyEnumLongToEnum(El value, bool useInterpreter)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.Convert(Expression.Constant(value, typeof(El)), typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((E)value), f());
        }

        private static void VerifyEnumLongToNullableEnum(El value, bool useInterpreter)
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.Convert(Expression.Constant(value, typeof(El)), typeof(E?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((E)value), f());
        }

        private static void VerifyEnumLongToEnumLong(El value, bool useInterpreter)
        {
            Expression<Func<El>> e =
                Expression.Lambda<Func<El>>(
                    Expression.Convert(Expression.Constant(value, typeof(El)), typeof(El)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyEnumLongToNullableEnumLong(El value, bool useInterpreter)
        {
            Expression<Func<El?>> e =
                Expression.Lambda<Func<El?>>(
                    Expression.Convert(Expression.Constant(value, typeof(El)), typeof(El?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyEnumLongToFloat(El value, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.Convert(Expression.Constant(value, typeof(El)), typeof(float)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);

            Assert.Equal((float)value, f());
        }

        private static void VerifyEnumLongToNullableFloat(El value, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.Convert(Expression.Constant(value, typeof(El)), typeof(float?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal((float)value, f());
        }

        private static void VerifyEnumLongToInt(El value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Convert(Expression.Constant(value, typeof(El)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((int)value), f());
        }

        private static void VerifyEnumLongToNullableInt(El value, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.Convert(Expression.Constant(value, typeof(El)), typeof(int?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((int)value), f());
        }

        private static void VerifyEnumLongToLong(El value, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.Convert(Expression.Constant(value, typeof(El)), typeof(long)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);

            Assert.Equal((long)value, f());
        }

        private static void VerifyEnumLongToNullableLong(El value, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.Convert(Expression.Constant(value, typeof(El)), typeof(long?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);

            Assert.Equal((long)value, f());
        }

        private static void VerifyEnumLongToSByte(El value, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.Convert(Expression.Constant(value, typeof(El)), typeof(sbyte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((sbyte)value), f());
        }

        private static void VerifyEnumLongToNullableSByte(El value, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.Convert(Expression.Constant(value, typeof(El)), typeof(sbyte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((sbyte)value), f());
        }

        private static void VerifyEnumLongToShort(El value, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.Convert(Expression.Constant(value, typeof(El)), typeof(short)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((short)value), f());
        }

        private static void VerifyEnumLongToNullableShort(El value, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.Convert(Expression.Constant(value, typeof(El)), typeof(short?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((short)value), f());
        }

        private static void VerifyEnumLongToUInt(El value, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.Convert(Expression.Constant(value, typeof(El)), typeof(uint)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((uint)value), f());
        }

        private static void VerifyEnumLongToNullableUInt(El value, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.Convert(Expression.Constant(value, typeof(El)), typeof(uint?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((uint)value), f());
        }

        private static void VerifyEnumLongToULong(El value, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.Convert(Expression.Constant(value, typeof(El)), typeof(ulong)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((ulong)value), f());
        }

        private static void VerifyEnumLongToNullableULong(El value, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.Convert(Expression.Constant(value, typeof(El)), typeof(ulong?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((ulong)value), f());
        }

        private static void VerifyEnumLongToUShort(El value, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.Convert(Expression.Constant(value, typeof(El)), typeof(ushort)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((ushort)value), f());
        }

        private static void VerifyEnumLongToNullableUShort(El value, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.Convert(Expression.Constant(value, typeof(El)), typeof(ushort?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((ushort)value), f());
        }

        private static void VerifyNullableEnumLongToByte(El? value, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.Convert(Expression.Constant(value, typeof(El?)), typeof(byte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((byte)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableEnumLongToNullableByte(El? value, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.Convert(Expression.Constant(value, typeof(El?)), typeof(byte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((byte?)value), f());
        }

        private static void VerifyNullableEnumLongToChar(El? value, bool useInterpreter)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.Convert(Expression.Constant(value, typeof(El?)), typeof(char)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((char)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableEnumLongToNullableChar(El? value, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.Convert(Expression.Constant(value, typeof(El?)), typeof(char?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((char?)value), f());
        }

        private static void VerifyNullableEnumLongToDouble(El? value, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.Convert(Expression.Constant(value, typeof(El?)), typeof(double)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal((double)value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableEnumLongToNullableDouble(El? value, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.Convert(Expression.Constant(value, typeof(El?)), typeof(double?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal((double?)value, f());
        }

        private static void VerifyNullableEnumLongToEnum(El? value, bool useInterpreter)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.Convert(Expression.Constant(value, typeof(El?)), typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((E)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableEnumLongToNullableEnum(El? value, bool useInterpreter)
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.Convert(Expression.Constant(value, typeof(El?)), typeof(E?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((E?)value), f());
        }

        private static void VerifyNullableEnumLongToEnumLong(El? value, bool useInterpreter)
        {
            Expression<Func<El>> e =
                Expression.Lambda<Func<El>>(
                    Expression.Convert(Expression.Constant(value, typeof(El?)), typeof(El)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableEnumLongToNullableEnumLong(El? value, bool useInterpreter)
        {
            Expression<Func<El?>> e =
                Expression.Lambda<Func<El?>>(
                    Expression.Convert(Expression.Constant(value, typeof(El?)), typeof(El?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableEnumLongToFloat(El? value, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.Convert(Expression.Constant(value, typeof(El?)), typeof(float)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal((float)value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableEnumLongToNullableFloat(El? value, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.Convert(Expression.Constant(value, typeof(El?)), typeof(float?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal((float?)value, f());
        }

        private static void VerifyNullableEnumLongToInt(El? value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Convert(Expression.Constant(value, typeof(El?)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((int)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableEnumLongToNullableInt(El? value, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.Convert(Expression.Constant(value, typeof(El?)), typeof(int?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((int?)value), f());
        }

        private static void VerifyNullableEnumLongToLong(El? value, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.Convert(Expression.Constant(value, typeof(El?)), typeof(long)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal((long)value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableEnumLongToNullableLong(El? value, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.Convert(Expression.Constant(value, typeof(El?)), typeof(long?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);

            Assert.Equal((long?)value, f());
        }

        private static void VerifyNullableEnumLongToSByte(El? value, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.Convert(Expression.Constant(value, typeof(El?)), typeof(sbyte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((sbyte)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableEnumLongToNullableSByte(El? value, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.Convert(Expression.Constant(value, typeof(El?)), typeof(sbyte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((sbyte?)value), f());
        }

        private static void VerifyNullableEnumLongToShort(El? value, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.Convert(Expression.Constant(value, typeof(El?)), typeof(short)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((short)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableEnumLongToNullableShort(El? value, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.Convert(Expression.Constant(value, typeof(El?)), typeof(short?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((short?)value), f());
        }

        private static void VerifyNullableEnumLongToUInt(El? value, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.Convert(Expression.Constant(value, typeof(El?)), typeof(uint)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((uint)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableEnumLongToNullableUInt(El? value, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.Convert(Expression.Constant(value, typeof(El?)), typeof(uint?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((uint?)value), f());
        }

        private static void VerifyNullableEnumLongToULong(El? value, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.Convert(Expression.Constant(value, typeof(El?)), typeof(ulong)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((ulong)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableEnumLongToNullableULong(El? value, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.Convert(Expression.Constant(value, typeof(El?)), typeof(ulong?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((ulong?)value), f());
        }

        private static void VerifyNullableEnumLongToUShort(El? value, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.Convert(Expression.Constant(value, typeof(El?)), typeof(ushort)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((ushort)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableEnumLongToNullableUShort(El? value, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.Convert(Expression.Constant(value, typeof(El?)), typeof(ushort?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((ushort?)value), f());
        }

        private static void VerifyFloatToByte(float value, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.Convert(Expression.Constant(value, typeof(float)), typeof(byte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((byte)value), f());
        }

        private static void VerifyFloatToNullableByte(float value, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.Convert(Expression.Constant(value, typeof(float)), typeof(byte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((byte)value), f());
        }

        private static void VerifyFloatToChar(float value, bool useInterpreter)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.Convert(Expression.Constant(value, typeof(float)), typeof(char)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((char)value), f());
        }

        private static void VerifyFloatToNullableChar(float value, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.Convert(Expression.Constant(value, typeof(float)), typeof(char?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((char?)value), f());
        }

        private static void VerifyFloatToDecimal(float value, bool useInterpreter)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.Convert(Expression.Constant(value, typeof(float)), typeof(decimal)),
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

        private static void VerifyFloatToNullableDecimal(float value, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.Convert(Expression.Constant(value, typeof(float)), typeof(decimal?)),
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

        private static void VerifyFloatToDouble(float value, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.Convert(Expression.Constant(value, typeof(float)), typeof(double)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);

            Assert.Equal((double)value, f());
        }

        private static void VerifyFloatToNullableDouble(float value, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.Convert(Expression.Constant(value, typeof(float)), typeof(double?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal((double)value, f());
        }

        private static void VerifyFloatToEnum(float value, bool useInterpreter)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.Convert(Expression.Constant(value, typeof(float)), typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((E)value), f());
        }

        private static void VerifyFloatToNullableEnum(float value, bool useInterpreter)
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.Convert(Expression.Constant(value, typeof(float)), typeof(E?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((E)value), f());
        }

        private static void VerifyFloatToEnumLong(float value, bool useInterpreter)
        {
            Expression<Func<El>> e =
                Expression.Lambda<Func<El>>(
                    Expression.Convert(Expression.Constant(value, typeof(float)), typeof(El)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((El)value), f());
        }

        private static void VerifyFloatToNullableEnumLong(float value, bool useInterpreter)
        {
            Expression<Func<El?>> e =
                Expression.Lambda<Func<El?>>(
                    Expression.Convert(Expression.Constant(value, typeof(float)), typeof(El?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((El)value), f());
        }

        private static void VerifyFloatToFloat(float value, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.Convert(Expression.Constant(value, typeof(float)), typeof(float)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyFloatToNullableFloat(float value, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.Convert(Expression.Constant(value, typeof(float)), typeof(float?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyFloatToInt(float value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Convert(Expression.Constant(value, typeof(float)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((int)value), f());
        }

        private static void VerifyFloatToNullableInt(float value, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.Convert(Expression.Constant(value, typeof(float)), typeof(int?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((int)value), f());
        }

        private static void VerifyFloatToLong(float value, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.Convert(Expression.Constant(value, typeof(float)), typeof(long)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((long)value), f());
        }

        private static void VerifyFloatToNullableLong(float value, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.Convert(Expression.Constant(value, typeof(float)), typeof(long?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((long)value), f());
        }

        private static void VerifyFloatToSByte(float value, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.Convert(Expression.Constant(value, typeof(float)), typeof(sbyte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((sbyte)value), f());
        }

        private static void VerifyFloatToNullableSByte(float value, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.Convert(Expression.Constant(value, typeof(float)), typeof(sbyte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((sbyte)value), f());
        }

        private static void VerifyFloatToShort(float value, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.Convert(Expression.Constant(value, typeof(float)), typeof(short)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((short)value), f());
        }

        private static void VerifyFloatToNullableShort(float value, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.Convert(Expression.Constant(value, typeof(float)), typeof(short?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((short)value), f());
        }

        private static void VerifyFloatToUInt(float value, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.Convert(Expression.Constant(value, typeof(float)), typeof(uint)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((uint)value), f());
        }

        private static void VerifyFloatToNullableUInt(float value, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.Convert(Expression.Constant(value, typeof(float)), typeof(uint?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((uint)value), f());
        }

        private static void VerifyFloatToULong(float value, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.Convert(Expression.Constant(value, typeof(float)), typeof(ulong)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((ulong)value), f());
        }

        private static void VerifyFloatToNullableULong(float value, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.Convert(Expression.Constant(value, typeof(float)), typeof(ulong?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((ulong)value), f());
        }

        private static void VerifyFloatToUShort(float value, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.Convert(Expression.Constant(value, typeof(float)), typeof(ushort)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((ushort)value), f());
        }

        private static void VerifyFloatToNullableUShort(float value, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.Convert(Expression.Constant(value, typeof(float)), typeof(ushort?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((ushort)value), f());
        }

        private static void VerifyNullableFloatToByte(float? value, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.Convert(Expression.Constant(value, typeof(float?)), typeof(byte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((byte)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableFloatToNullableByte(float? value, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.Convert(Expression.Constant(value, typeof(float?)), typeof(byte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((byte?)value), f());
        }

        private static void VerifyNullableFloatToChar(float? value, bool useInterpreter)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.Convert(Expression.Constant(value, typeof(float?)), typeof(char)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((char)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableFloatToNullableChar(float? value, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.Convert(Expression.Constant(value, typeof(float?)), typeof(char?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((char?)value), f());
        }

        private static void VerifyNullableFloatToDecimal(float? value, bool useInterpreter)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.Convert(Expression.Constant(value, typeof(float?)), typeof(decimal)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal> f = e.Compile(useInterpreter);

            if (value.HasValue)
            {
                decimal expected = 0;
                try
                {
                    expected = (decimal)value.GetValueOrDefault();
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

        private static void VerifyNullableFloatToNullableDecimal(float? value, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.Convert(Expression.Constant(value, typeof(float?)), typeof(decimal?)),
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

        private static void VerifyNullableFloatToDouble(float? value, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.Convert(Expression.Constant(value, typeof(float?)), typeof(double)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableFloatToNullableDouble(float? value, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.Convert(Expression.Constant(value, typeof(float?)), typeof(double?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableFloatToEnum(float? value, bool useInterpreter)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.Convert(Expression.Constant(value, typeof(float?)), typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((E)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableFloatToNullableEnum(float? value, bool useInterpreter)
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.Convert(Expression.Constant(value, typeof(float?)), typeof(E?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((E?)value), f());
        }

        private static void VerifyNullableFloatToEnumLong(float? value, bool useInterpreter)
        {
            Expression<Func<El>> e =
                Expression.Lambda<Func<El>>(
                    Expression.Convert(Expression.Constant(value, typeof(float?)), typeof(El)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((El)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableFloatToNullableEnumLong(float? value, bool useInterpreter)
        {
            Expression<Func<El?>> e =
                Expression.Lambda<Func<El?>>(
                    Expression.Convert(Expression.Constant(value, typeof(float?)), typeof(El?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((El?)value), f());
        }

        private static void VerifyNullableFloatToFloat(float? value, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.Convert(Expression.Constant(value, typeof(float?)), typeof(float)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableFloatToNullableFloat(float? value, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.Convert(Expression.Constant(value, typeof(float?)), typeof(float?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableFloatToInt(float? value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Convert(Expression.Constant(value, typeof(float?)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((int)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableFloatToNullableInt(float? value, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.Convert(Expression.Constant(value, typeof(float?)), typeof(int?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((int?)value), f());
        }

        private static void VerifyNullableFloatToLong(float? value, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.Convert(Expression.Constant(value, typeof(float?)), typeof(long)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((long)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableFloatToNullableLong(float? value, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.Convert(Expression.Constant(value, typeof(float?)), typeof(long?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((long?)value), f());
        }

        private static void VerifyNullableFloatToSByte(float? value, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.Convert(Expression.Constant(value, typeof(float?)), typeof(sbyte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((sbyte)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableFloatToNullableSByte(float? value, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.Convert(Expression.Constant(value, typeof(float?)), typeof(sbyte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((sbyte?)value), f());
        }

        private static void VerifyNullableFloatToShort(float? value, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.Convert(Expression.Constant(value, typeof(float?)), typeof(short)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((short)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableFloatToNullableShort(float? value, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.Convert(Expression.Constant(value, typeof(float?)), typeof(short?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((short?)value), f());
        }

        private static void VerifyNullableFloatToUInt(float? value, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.Convert(Expression.Constant(value, typeof(float?)), typeof(uint)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((uint)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableFloatToNullableUInt(float? value, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.Convert(Expression.Constant(value, typeof(float?)), typeof(uint?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((uint?)value), f());
        }

        private static void VerifyNullableFloatToULong(float? value, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.Convert(Expression.Constant(value, typeof(float?)), typeof(ulong)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((ulong)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableFloatToNullableULong(float? value, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.Convert(Expression.Constant(value, typeof(float?)), typeof(ulong?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((ulong?)value), f());
        }

        private static void VerifyNullableFloatToUShort(float? value, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.Convert(Expression.Constant(value, typeof(float?)), typeof(ushort)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((ushort)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableFloatToNullableUShort(float? value, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.Convert(Expression.Constant(value, typeof(float?)), typeof(ushort?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((ushort?)value), f());
        }

        private static void VerifyIntToByte(int value, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.Convert(Expression.Constant(value, typeof(int)), typeof(byte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((byte)value), f());
        }

        private static void VerifyIntToNullableByte(int value, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.Convert(Expression.Constant(value, typeof(int)), typeof(byte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((byte)value), f());
        }

        private static void VerifyIntToChar(int value, bool useInterpreter)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.Convert(Expression.Constant(value, typeof(int)), typeof(char)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((char)value), f());
        }

        private static void VerifyIntToNullableChar(int value, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.Convert(Expression.Constant(value, typeof(int)), typeof(char?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((char?)value), f());
        }

        private static void VerifyIntToDecimal(int value, bool useInterpreter)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.Convert(Expression.Constant(value, typeof(int)), typeof(decimal)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyIntToNullableDecimal(int value, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.Convert(Expression.Constant(value, typeof(int)), typeof(decimal?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyIntToDouble(int value, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.Convert(Expression.Constant(value, typeof(int)), typeof(double)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyIntToNullableDouble(int value, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.Convert(Expression.Constant(value, typeof(int)), typeof(double?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyIntToEnum(int value, bool useInterpreter)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.Convert(Expression.Constant(value, typeof(int)), typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile(useInterpreter);

            Assert.Equal((E)value, f());
        }

        private static void VerifyIntToNullableEnum(int value, bool useInterpreter)
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.Convert(Expression.Constant(value, typeof(int)), typeof(E?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile(useInterpreter);

            Assert.Equal((E)value, f());
        }

        private static void VerifyIntToEnumLong(int value, bool useInterpreter)
        {
            Expression<Func<El>> e =
                Expression.Lambda<Func<El>>(
                    Expression.Convert(Expression.Constant(value, typeof(int)), typeof(El)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El> f = e.Compile(useInterpreter);

            Assert.Equal((El)value, f());
        }

        private static void VerifyIntToNullableEnumLong(int value, bool useInterpreter)
        {
            Expression<Func<El?>> e =
                Expression.Lambda<Func<El?>>(
                    Expression.Convert(Expression.Constant(value, typeof(int)), typeof(El?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?> f = e.Compile(useInterpreter);

            Assert.Equal((El)value, f());
        }

        private static void VerifyIntToFloat(int value, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.Convert(Expression.Constant(value, typeof(int)), typeof(float)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyIntToNullableFloat(int value, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.Convert(Expression.Constant(value, typeof(int)), typeof(float?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyIntToInt(int value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Convert(Expression.Constant(value, typeof(int)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyIntToNullableInt(int value, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.Convert(Expression.Constant(value, typeof(int)), typeof(int?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyIntToLong(int value, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.Convert(Expression.Constant(value, typeof(int)), typeof(long)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyIntToNullableLong(int value, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.Convert(Expression.Constant(value, typeof(int)), typeof(long?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyIntToSByte(int value, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.Convert(Expression.Constant(value, typeof(int)), typeof(sbyte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((sbyte)value), f());
        }

        private static void VerifyIntToNullableSByte(int value, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.Convert(Expression.Constant(value, typeof(int)), typeof(sbyte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((sbyte)value), f());
        }

        private static void VerifyIntToShort(int value, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.Convert(Expression.Constant(value, typeof(int)), typeof(short)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((short)value), f());
        }

        private static void VerifyIntToNullableShort(int value, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.Convert(Expression.Constant(value, typeof(int)), typeof(short?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((short)value), f());
        }

        private static void VerifyIntToUInt(int value, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.Convert(Expression.Constant(value, typeof(int)), typeof(uint)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((uint)value), f());
        }

        private static void VerifyIntToNullableUInt(int value, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.Convert(Expression.Constant(value, typeof(int)), typeof(uint?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((uint)value), f());
        }

        private static void VerifyIntToULong(int value, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.Convert(Expression.Constant(value, typeof(int)), typeof(ulong)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((ulong)value), f());
        }

        private static void VerifyIntToNullableULong(int value, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.Convert(Expression.Constant(value, typeof(int)), typeof(ulong?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((ulong)value), f());
        }

        private static void VerifyIntToUShort(int value, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.Convert(Expression.Constant(value, typeof(int)), typeof(ushort)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((ushort)value), f());
        }

        private static void VerifyIntToNullableUShort(int value, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.Convert(Expression.Constant(value, typeof(int)), typeof(ushort?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((ushort)value), f());
        }

        private static void VerifyNullableIntToByte(int? value, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.Convert(Expression.Constant(value, typeof(int?)), typeof(byte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((byte)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableIntToNullableByte(int? value, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.Convert(Expression.Constant(value, typeof(int?)), typeof(byte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((byte?)value), f());
        }

        private static void VerifyNullableIntToChar(int? value, bool useInterpreter)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.Convert(Expression.Constant(value, typeof(int?)), typeof(char)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((char)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableIntToNullableChar(int? value, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.Convert(Expression.Constant(value, typeof(int?)), typeof(char?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((char?)value), f());
        }

        private static void VerifyNullableIntToDecimal(int? value, bool useInterpreter)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.Convert(Expression.Constant(value, typeof(int?)), typeof(decimal)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableIntToNullableDecimal(int? value, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.Convert(Expression.Constant(value, typeof(int?)), typeof(decimal?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableIntToDouble(int? value, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.Convert(Expression.Constant(value, typeof(int?)), typeof(double)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableIntToNullableDouble(int? value, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.Convert(Expression.Constant(value, typeof(int?)), typeof(double?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableIntToEnum(int? value, bool useInterpreter)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.Convert(Expression.Constant(value, typeof(int?)), typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal((E)value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableIntToNullableEnum(int? value, bool useInterpreter)
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.Convert(Expression.Constant(value, typeof(int?)), typeof(E?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile(useInterpreter);

            Assert.Equal((E?)value, f());
        }

        private static void VerifyNullableIntToEnumLong(int? value, bool useInterpreter)
        {
            Expression<Func<El>> e =
                Expression.Lambda<Func<El>>(
                    Expression.Convert(Expression.Constant(value, typeof(int?)), typeof(El)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal((El)value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableIntToNullableEnumLong(int? value, bool useInterpreter)
        {
            Expression<Func<El?>> e =
                Expression.Lambda<Func<El?>>(
                    Expression.Convert(Expression.Constant(value, typeof(int?)), typeof(El?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?> f = e.Compile(useInterpreter);

            Assert.Equal((El?)value, f());
        }

        private static void VerifyNullableIntToFloat(int? value, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.Convert(Expression.Constant(value, typeof(int?)), typeof(float)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableIntToNullableFloat(int? value, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.Convert(Expression.Constant(value, typeof(int?)), typeof(float?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableIntToInt(int? value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Convert(Expression.Constant(value, typeof(int?)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableIntToNullableInt(int? value, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.Convert(Expression.Constant(value, typeof(int?)), typeof(int?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableIntToLong(int? value, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.Convert(Expression.Constant(value, typeof(int?)), typeof(long)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableIntToNullableLong(int? value, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.Convert(Expression.Constant(value, typeof(int?)), typeof(long?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableIntToSByte(int? value, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.Convert(Expression.Constant(value, typeof(int?)), typeof(sbyte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((sbyte)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableIntToNullableSByte(int? value, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.Convert(Expression.Constant(value, typeof(int?)), typeof(sbyte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((sbyte?)value), f());
        }

        private static void VerifyNullableIntToShort(int? value, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.Convert(Expression.Constant(value, typeof(int?)), typeof(short)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((short)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableIntToNullableShort(int? value, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.Convert(Expression.Constant(value, typeof(int?)), typeof(short?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((short?)value), f());
        }

        private static void VerifyNullableIntToUInt(int? value, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.Convert(Expression.Constant(value, typeof(int?)), typeof(uint)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((uint)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableIntToNullableUInt(int? value, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.Convert(Expression.Constant(value, typeof(int?)), typeof(uint?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((uint?)value), f());
        }

        private static void VerifyNullableIntToULong(int? value, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.Convert(Expression.Constant(value, typeof(int?)), typeof(ulong)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((ulong)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableIntToNullableULong(int? value, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.Convert(Expression.Constant(value, typeof(int?)), typeof(ulong?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((ulong?)value), f());
        }

        private static void VerifyNullableIntToUShort(int? value, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.Convert(Expression.Constant(value, typeof(int?)), typeof(ushort)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((ushort)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableIntToNullableUShort(int? value, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.Convert(Expression.Constant(value, typeof(int?)), typeof(ushort?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((ushort?)value), f());
        }

        private static void VerifyLongToByte(long value, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.Convert(Expression.Constant(value, typeof(long)), typeof(byte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((byte)value), f());
        }

        private static void VerifyLongToNullableByte(long value, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.Convert(Expression.Constant(value, typeof(long)), typeof(byte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((byte)value), f());
        }

        private static void VerifyLongToChar(long value, bool useInterpreter)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.Convert(Expression.Constant(value, typeof(long)), typeof(char)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((char)value), f());
        }

        private static void VerifyLongToNullableChar(long value, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.Convert(Expression.Constant(value, typeof(long)), typeof(char?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((char?)value), f());
        }

        private static void VerifyLongToDecimal(long value, bool useInterpreter)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.Convert(Expression.Constant(value, typeof(long)), typeof(decimal)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyLongToNullableDecimal(long value, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.Convert(Expression.Constant(value, typeof(long)), typeof(decimal?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyLongToDouble(long value, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.Convert(Expression.Constant(value, typeof(long)), typeof(double)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyLongToNullableDouble(long value, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.Convert(Expression.Constant(value, typeof(long)), typeof(double?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyLongToEnum(long value, bool useInterpreter)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.Convert(Expression.Constant(value, typeof(long)), typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((E)value), f());
        }

        private static void VerifyLongToNullableEnum(long value, bool useInterpreter)
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.Convert(Expression.Constant(value, typeof(long)), typeof(E?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((E)value), f());
        }

        private static void VerifyLongToEnumLong(long value, bool useInterpreter)
        {
            Expression<Func<El>> e =
                Expression.Lambda<Func<El>>(
                    Expression.Convert(Expression.Constant(value, typeof(long)), typeof(El)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El> f = e.Compile(useInterpreter);

            Assert.Equal((El)value, f());
        }

        private static void VerifyLongToNullableEnumLong(long value, bool useInterpreter)
        {
            Expression<Func<El?>> e =
                Expression.Lambda<Func<El?>>(
                    Expression.Convert(Expression.Constant(value, typeof(long)), typeof(El?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?> f = e.Compile(useInterpreter);

            Assert.Equal((El)value, f());
        }

        private static void VerifyLongToFloat(long value, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.Convert(Expression.Constant(value, typeof(long)), typeof(float)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyLongToNullableFloat(long value, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.Convert(Expression.Constant(value, typeof(long)), typeof(float?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyLongToInt(long value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Convert(Expression.Constant(value, typeof(long)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((int)value), f());
        }

        private static void VerifyLongToNullableInt(long value, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.Convert(Expression.Constant(value, typeof(long)), typeof(int?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((int)value), f());
        }

        private static void VerifyLongToLong(long value, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.Convert(Expression.Constant(value, typeof(long)), typeof(long)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyLongToNullableLong(long value, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.Convert(Expression.Constant(value, typeof(long)), typeof(long?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyLongToSByte(long value, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.Convert(Expression.Constant(value, typeof(long)), typeof(sbyte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((sbyte)value), f());
        }

        private static void VerifyLongToNullableSByte(long value, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.Convert(Expression.Constant(value, typeof(long)), typeof(sbyte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((sbyte)value), f());
        }

        private static void VerifyLongToShort(long value, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.Convert(Expression.Constant(value, typeof(long)), typeof(short)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((short)value), f());
        }

        private static void VerifyLongToNullableShort(long value, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.Convert(Expression.Constant(value, typeof(long)), typeof(short?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((short)value), f());
        }

        private static void VerifyLongToUInt(long value, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.Convert(Expression.Constant(value, typeof(long)), typeof(uint)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((uint)value), f());
        }

        private static void VerifyLongToNullableUInt(long value, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.Convert(Expression.Constant(value, typeof(long)), typeof(uint?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((uint)value), f());
        }

        private static void VerifyLongToULong(long value, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.Convert(Expression.Constant(value, typeof(long)), typeof(ulong)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((ulong)value), f());
        }

        private static void VerifyLongToNullableULong(long value, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.Convert(Expression.Constant(value, typeof(long)), typeof(ulong?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((ulong)value), f());
        }

        private static void VerifyLongToUShort(long value, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.Convert(Expression.Constant(value, typeof(long)), typeof(ushort)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((ushort)value), f());
        }

        private static void VerifyLongToNullableUShort(long value, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.Convert(Expression.Constant(value, typeof(long)), typeof(ushort?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((ushort)value), f());
        }

        private static void VerifyNullableLongToByte(long? value, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.Convert(Expression.Constant(value, typeof(long?)), typeof(byte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((byte)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableLongToNullableByte(long? value, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.Convert(Expression.Constant(value, typeof(long?)), typeof(byte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((byte?)value), f());
        }

        private static void VerifyNullableLongToChar(long? value, bool useInterpreter)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.Convert(Expression.Constant(value, typeof(long?)), typeof(char)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((char)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableLongToNullableChar(long? value, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.Convert(Expression.Constant(value, typeof(long?)), typeof(char?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((char?)value), f());
        }

        private static void VerifyNullableLongToDecimal(long? value, bool useInterpreter)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.Convert(Expression.Constant(value, typeof(long?)), typeof(decimal)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableLongToNullableDecimal(long? value, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.Convert(Expression.Constant(value, typeof(long?)), typeof(decimal?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableLongToDouble(long? value, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.Convert(Expression.Constant(value, typeof(long?)), typeof(double)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableLongToNullableDouble(long? value, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.Convert(Expression.Constant(value, typeof(long?)), typeof(double?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableLongToEnum(long? value, bool useInterpreter)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.Convert(Expression.Constant(value, typeof(long?)), typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((E)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableLongToNullableEnum(long? value, bool useInterpreter)
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.Convert(Expression.Constant(value, typeof(long?)), typeof(E?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((E?)value), f());
        }

        private static void VerifyNullableLongToEnumLong(long? value, bool useInterpreter)
        {
            Expression<Func<El>> e =
                Expression.Lambda<Func<El>>(
                    Expression.Convert(Expression.Constant(value, typeof(long?)), typeof(El)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal((El)value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableLongToNullableEnumLong(long? value, bool useInterpreter)
        {
            Expression<Func<El?>> e =
                Expression.Lambda<Func<El?>>(
                    Expression.Convert(Expression.Constant(value, typeof(long?)), typeof(El?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?> f = e.Compile(useInterpreter);

            Assert.Equal((El?)value, f());
        }

        private static void VerifyNullableLongToFloat(long? value, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.Convert(Expression.Constant(value, typeof(long?)), typeof(float)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableLongToNullableFloat(long? value, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.Convert(Expression.Constant(value, typeof(long?)), typeof(float?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableLongToInt(long? value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Convert(Expression.Constant(value, typeof(long?)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((int)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableLongToNullableInt(long? value, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.Convert(Expression.Constant(value, typeof(long?)), typeof(int?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((int?)value), f());
        }

        private static void VerifyNullableLongToLong(long? value, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.Convert(Expression.Constant(value, typeof(long?)), typeof(long)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableLongToNullableLong(long? value, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.Convert(Expression.Constant(value, typeof(long?)), typeof(long?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableLongToSByte(long? value, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.Convert(Expression.Constant(value, typeof(long?)), typeof(sbyte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((sbyte)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableLongToNullableSByte(long? value, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.Convert(Expression.Constant(value, typeof(long?)), typeof(sbyte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((sbyte?)value), f());
        }

        private static void VerifyNullableLongToShort(long? value, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.Convert(Expression.Constant(value, typeof(long?)), typeof(short)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((short)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableLongToNullableShort(long? value, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.Convert(Expression.Constant(value, typeof(long?)), typeof(short?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((short?)value), f());
        }

        private static void VerifyNullableLongToUInt(long? value, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.Convert(Expression.Constant(value, typeof(long?)), typeof(uint)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((uint)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableLongToNullableUInt(long? value, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.Convert(Expression.Constant(value, typeof(long?)), typeof(uint?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((uint?)value), f());
        }

        private static void VerifyNullableLongToULong(long? value, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.Convert(Expression.Constant(value, typeof(long?)), typeof(ulong)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((ulong)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableLongToNullableULong(long? value, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.Convert(Expression.Constant(value, typeof(long?)), typeof(ulong?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((ulong?)value), f());
        }

        private static void VerifyNullableLongToUShort(long? value, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.Convert(Expression.Constant(value, typeof(long?)), typeof(ushort)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((ushort)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableLongToNullableUShort(long? value, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.Convert(Expression.Constant(value, typeof(long?)), typeof(ushort?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((ushort?)value), f());
        }

        private static void VerifySByteToByte(sbyte value, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.Convert(Expression.Constant(value, typeof(sbyte)), typeof(byte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((byte)value), f());
        }

        private static void VerifySByteToNullableByte(sbyte value, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.Convert(Expression.Constant(value, typeof(sbyte)), typeof(byte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((byte)value), f());
        }

        private static void VerifySByteToChar(sbyte value, bool useInterpreter)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.Convert(Expression.Constant(value, typeof(sbyte)), typeof(char)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((char)value), f());
        }

        private static void VerifySByteToNullableChar(sbyte value, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.Convert(Expression.Constant(value, typeof(sbyte)), typeof(char?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((char)value), f());
        }

        private static void VerifySByteToDecimal(sbyte value, bool useInterpreter)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.Convert(Expression.Constant(value, typeof(sbyte)), typeof(decimal)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifySByteToNullableDecimal(sbyte value, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.Convert(Expression.Constant(value, typeof(sbyte)), typeof(decimal?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifySByteToDouble(sbyte value, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.Convert(Expression.Constant(value, typeof(sbyte)), typeof(double)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifySByteToNullableDouble(sbyte value, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.Convert(Expression.Constant(value, typeof(sbyte)), typeof(double?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifySByteToEnum(sbyte value, bool useInterpreter)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.Convert(Expression.Constant(value, typeof(sbyte)), typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile(useInterpreter);

            Assert.Equal((E)value, f());
        }

        private static void VerifySByteToNullableEnum(sbyte value, bool useInterpreter)
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.Convert(Expression.Constant(value, typeof(sbyte)), typeof(E?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile(useInterpreter);

            Assert.Equal((E)value, f());
        }

        private static void VerifySByteToEnumLong(sbyte value, bool useInterpreter)
        {
            Expression<Func<El>> e =
                Expression.Lambda<Func<El>>(
                    Expression.Convert(Expression.Constant(value, typeof(sbyte)), typeof(El)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El> f = e.Compile(useInterpreter);

            Assert.Equal((El)value, f());
        }

        private static void VerifySByteToNullableEnumLong(sbyte value, bool useInterpreter)
        {
            Expression<Func<El?>> e =
                Expression.Lambda<Func<El?>>(
                    Expression.Convert(Expression.Constant(value, typeof(sbyte)), typeof(El?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?> f = e.Compile(useInterpreter);

            Assert.Equal((El)value, f());
        }

        private static void VerifySByteToFloat(sbyte value, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.Convert(Expression.Constant(value, typeof(sbyte)), typeof(float)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifySByteToNullableFloat(sbyte value, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.Convert(Expression.Constant(value, typeof(sbyte)), typeof(float?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifySByteToInt(sbyte value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Convert(Expression.Constant(value, typeof(sbyte)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifySByteToNullableInt(sbyte value, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.Convert(Expression.Constant(value, typeof(sbyte)), typeof(int?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifySByteToLong(sbyte value, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.Convert(Expression.Constant(value, typeof(sbyte)), typeof(long)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifySByteToNullableLong(sbyte value, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.Convert(Expression.Constant(value, typeof(sbyte)), typeof(long?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifySByteToSByte(sbyte value, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.Convert(Expression.Constant(value, typeof(sbyte)), typeof(sbyte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifySByteToNullableSByte(sbyte value, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.Convert(Expression.Constant(value, typeof(sbyte)), typeof(sbyte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifySByteToShort(sbyte value, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.Convert(Expression.Constant(value, typeof(sbyte)), typeof(short)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifySByteToNullableShort(sbyte value, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.Convert(Expression.Constant(value, typeof(sbyte)), typeof(short?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifySByteToUInt(sbyte value, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.Convert(Expression.Constant(value, typeof(sbyte)), typeof(uint)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((uint)value), f());
        }

        private static void VerifySByteToNullableUInt(sbyte value, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.Convert(Expression.Constant(value, typeof(sbyte)), typeof(uint?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((uint)value), f());
        }

        private static void VerifySByteToULong(sbyte value, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.Convert(Expression.Constant(value, typeof(sbyte)), typeof(ulong)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((ulong)value), f());
        }

        private static void VerifySByteToNullableULong(sbyte value, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.Convert(Expression.Constant(value, typeof(sbyte)), typeof(ulong?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((ulong)value), f());
        }

        private static void VerifySByteToUShort(sbyte value, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.Convert(Expression.Constant(value, typeof(sbyte)), typeof(ushort)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((ushort)value), f());
        }

        private static void VerifySByteToNullableUShort(sbyte value, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.Convert(Expression.Constant(value, typeof(sbyte)), typeof(ushort?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((ushort)value), f());
        }

        private static void VerifyNullableSByteToByte(sbyte? value, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.Convert(Expression.Constant(value, typeof(sbyte?)), typeof(byte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((byte)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableSByteToNullableByte(sbyte? value, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.Convert(Expression.Constant(value, typeof(sbyte?)), typeof(byte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((byte?)value), f());
        }

        private static void VerifyNullableSByteToChar(sbyte? value, bool useInterpreter)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.Convert(Expression.Constant(value, typeof(sbyte?)), typeof(char)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((char)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableSByteToNullableChar(sbyte? value, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.Convert(Expression.Constant(value, typeof(sbyte?)), typeof(char?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((char?)value), f());
        }

        private static void VerifyNullableSByteToDecimal(sbyte? value, bool useInterpreter)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.Convert(Expression.Constant(value, typeof(sbyte?)), typeof(decimal)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableSByteToNullableDecimal(sbyte? value, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.Convert(Expression.Constant(value, typeof(sbyte?)), typeof(decimal?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableSByteToDouble(sbyte? value, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.Convert(Expression.Constant(value, typeof(sbyte?)), typeof(double)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableSByteToNullableDouble(sbyte? value, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.Convert(Expression.Constant(value, typeof(sbyte?)), typeof(double?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableSByteToEnum(sbyte? value, bool useInterpreter)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.Convert(Expression.Constant(value, typeof(sbyte?)), typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal((E)value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableSByteToNullableEnum(sbyte? value, bool useInterpreter)
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.Convert(Expression.Constant(value, typeof(sbyte?)), typeof(E?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile(useInterpreter);

            Assert.Equal((E?)value, f());
        }

        private static void VerifyNullableSByteToEnumLong(sbyte? value, bool useInterpreter)
        {
            Expression<Func<El>> e =
                Expression.Lambda<Func<El>>(
                    Expression.Convert(Expression.Constant(value, typeof(sbyte?)), typeof(El)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal((El)value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableSByteToNullableEnumLong(sbyte? value, bool useInterpreter)
        {
            Expression<Func<El?>> e =
                Expression.Lambda<Func<El?>>(
                    Expression.Convert(Expression.Constant(value, typeof(sbyte?)), typeof(El?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?> f = e.Compile(useInterpreter);

            Assert.Equal((El?)value, f());
        }

        private static void VerifyNullableSByteToFloat(sbyte? value, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.Convert(Expression.Constant(value, typeof(sbyte?)), typeof(float)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableSByteToNullableFloat(sbyte? value, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.Convert(Expression.Constant(value, typeof(sbyte?)), typeof(float?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableSByteToInt(sbyte? value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Convert(Expression.Constant(value, typeof(sbyte?)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableSByteToNullableInt(sbyte? value, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.Convert(Expression.Constant(value, typeof(sbyte?)), typeof(int?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableSByteToLong(sbyte? value, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.Convert(Expression.Constant(value, typeof(sbyte?)), typeof(long)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableSByteToNullableLong(sbyte? value, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.Convert(Expression.Constant(value, typeof(sbyte?)), typeof(long?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableSByteToSByte(sbyte? value, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.Convert(Expression.Constant(value, typeof(sbyte?)), typeof(sbyte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableSByteToNullableSByte(sbyte? value, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.Convert(Expression.Constant(value, typeof(sbyte?)), typeof(sbyte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableSByteToShort(sbyte? value, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.Convert(Expression.Constant(value, typeof(sbyte?)), typeof(short)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableSByteToNullableShort(sbyte? value, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.Convert(Expression.Constant(value, typeof(sbyte?)), typeof(short?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableSByteToUInt(sbyte? value, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.Convert(Expression.Constant(value, typeof(sbyte?)), typeof(uint)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((uint)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableSByteToNullableUInt(sbyte? value, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.Convert(Expression.Constant(value, typeof(sbyte?)), typeof(uint?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((uint?)value), f());
        }

        private static void VerifyNullableSByteToULong(sbyte? value, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.Convert(Expression.Constant(value, typeof(sbyte?)), typeof(ulong)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((ulong)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableSByteToNullableULong(sbyte? value, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.Convert(Expression.Constant(value, typeof(sbyte?)), typeof(ulong?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((ulong?)value), f());
        }

        private static void VerifyNullableSByteToUShort(sbyte? value, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.Convert(Expression.Constant(value, typeof(sbyte?)), typeof(ushort)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((ushort)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableSByteToNullableUShort(sbyte? value, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.Convert(Expression.Constant(value, typeof(sbyte?)), typeof(ushort?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((ushort?)value), f());
        }

        private static void VerifyShortToByte(short value, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.Convert(Expression.Constant(value, typeof(short)), typeof(byte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((byte)value), f());
        }

        private static void VerifyShortToNullableByte(short value, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.Convert(Expression.Constant(value, typeof(short)), typeof(byte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((byte)value), f());
        }

        private static void VerifyShortToChar(short value, bool useInterpreter)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.Convert(Expression.Constant(value, typeof(short)), typeof(char)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((char)value), f());
        }

        private static void VerifyShortToNullableChar(short value, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.Convert(Expression.Constant(value, typeof(short)), typeof(char?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((char?)value), f());
        }

        private static void VerifyShortToDecimal(short value, bool useInterpreter)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.Convert(Expression.Constant(value, typeof(short)), typeof(decimal)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyShortToNullableDecimal(short value, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.Convert(Expression.Constant(value, typeof(short)), typeof(decimal?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyShortToDouble(short value, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.Convert(Expression.Constant(value, typeof(short)), typeof(double)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyShortToNullableDouble(short value, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.Convert(Expression.Constant(value, typeof(short)), typeof(double?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyShortToEnum(short value, bool useInterpreter)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.Convert(Expression.Constant(value, typeof(short)), typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile(useInterpreter);

            Assert.Equal((E)value, f());
        }

        private static void VerifyShortToNullableEnum(short value, bool useInterpreter)
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.Convert(Expression.Constant(value, typeof(short)), typeof(E?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile(useInterpreter);

            Assert.Equal((E)value, f());
        }

        private static void VerifyShortToEnumLong(short value, bool useInterpreter)
        {
            Expression<Func<El>> e =
                Expression.Lambda<Func<El>>(
                    Expression.Convert(Expression.Constant(value, typeof(short)), typeof(El)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El> f = e.Compile(useInterpreter);

            Assert.Equal((El)value, f());
        }

        private static void VerifyShortToNullableEnumLong(short value, bool useInterpreter)
        {
            Expression<Func<El?>> e =
                Expression.Lambda<Func<El?>>(
                    Expression.Convert(Expression.Constant(value, typeof(short)), typeof(El?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?> f = e.Compile(useInterpreter);

            Assert.Equal((El)value, f());
        }

        private static void VerifyShortToFloat(short value, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.Convert(Expression.Constant(value, typeof(short)), typeof(float)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyShortToNullableFloat(short value, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.Convert(Expression.Constant(value, typeof(short)), typeof(float?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyShortToInt(short value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Convert(Expression.Constant(value, typeof(short)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyShortToNullableInt(short value, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.Convert(Expression.Constant(value, typeof(short)), typeof(int?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyShortToLong(short value, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.Convert(Expression.Constant(value, typeof(short)), typeof(long)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyShortToNullableLong(short value, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.Convert(Expression.Constant(value, typeof(short)), typeof(long?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyShortToSByte(short value, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.Convert(Expression.Constant(value, typeof(short)), typeof(sbyte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((sbyte)value), f());
        }

        private static void VerifyShortToNullableSByte(short value, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.Convert(Expression.Constant(value, typeof(short)), typeof(sbyte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((sbyte)value), f());
        }

        private static void VerifyShortToShort(short value, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.Convert(Expression.Constant(value, typeof(short)), typeof(short)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyShortToNullableShort(short value, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.Convert(Expression.Constant(value, typeof(short)), typeof(short?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyShortToUInt(short value, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.Convert(Expression.Constant(value, typeof(short)), typeof(uint)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((uint)value), f());
        }

        private static void VerifyShortToNullableUInt(short value, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.Convert(Expression.Constant(value, typeof(short)), typeof(uint?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((uint)value), f());
        }

        private static void VerifyShortToULong(short value, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.Convert(Expression.Constant(value, typeof(short)), typeof(ulong)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((ulong)value), f());
        }

        private static void VerifyShortToNullableULong(short value, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.Convert(Expression.Constant(value, typeof(short)), typeof(ulong?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((ulong)value), f());
        }

        private static void VerifyShortToUShort(short value, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.Convert(Expression.Constant(value, typeof(short)), typeof(ushort)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((ushort)value), f());
        }

        private static void VerifyShortToNullableUShort(short value, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.Convert(Expression.Constant(value, typeof(short)), typeof(ushort?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((ushort)value), f());
        }

        private static void VerifyNullableShortToByte(short? value, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.Convert(Expression.Constant(value, typeof(short?)), typeof(byte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((byte)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableShortToNullableByte(short? value, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.Convert(Expression.Constant(value, typeof(short?)), typeof(byte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((byte?)value), f());
        }

        private static void VerifyNullableShortToChar(short? value, bool useInterpreter)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.Convert(Expression.Constant(value, typeof(short?)), typeof(char)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((char)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableShortToNullableChar(short? value, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.Convert(Expression.Constant(value, typeof(short?)), typeof(char?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((char?)value), f());
        }

        private static void VerifyNullableShortToDecimal(short? value, bool useInterpreter)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.Convert(Expression.Constant(value, typeof(short?)), typeof(decimal)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableShortToNullableDecimal(short? value, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.Convert(Expression.Constant(value, typeof(short?)), typeof(decimal?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableShortToDouble(short? value, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.Convert(Expression.Constant(value, typeof(short?)), typeof(double)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableShortToNullableDouble(short? value, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.Convert(Expression.Constant(value, typeof(short?)), typeof(double?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableShortToEnum(short? value, bool useInterpreter)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.Convert(Expression.Constant(value, typeof(short?)), typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal((E)value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableShortToNullableEnum(short? value, bool useInterpreter)
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.Convert(Expression.Constant(value, typeof(short?)), typeof(E?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile(useInterpreter);

            Assert.Equal((E?)value, f());
        }

        private static void VerifyNullableShortToEnumLong(short? value, bool useInterpreter)
        {
            Expression<Func<El>> e =
                Expression.Lambda<Func<El>>(
                    Expression.Convert(Expression.Constant(value, typeof(short?)), typeof(El)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal((El)value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableShortToNullableEnumLong(short? value, bool useInterpreter)
        {
            Expression<Func<El?>> e =
                Expression.Lambda<Func<El?>>(
                    Expression.Convert(Expression.Constant(value, typeof(short?)), typeof(El?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?> f = e.Compile(useInterpreter);

            Assert.Equal((El?)value, f());
        }

        private static void VerifyNullableShortToFloat(short? value, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.Convert(Expression.Constant(value, typeof(short?)), typeof(float)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableShortToNullableFloat(short? value, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.Convert(Expression.Constant(value, typeof(short?)), typeof(float?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableShortToInt(short? value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Convert(Expression.Constant(value, typeof(short?)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableShortToNullableInt(short? value, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.Convert(Expression.Constant(value, typeof(short?)), typeof(int?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableShortToLong(short? value, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.Convert(Expression.Constant(value, typeof(short?)), typeof(long)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableShortToNullableLong(short? value, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.Convert(Expression.Constant(value, typeof(short?)), typeof(long?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableShortToSByte(short? value, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.Convert(Expression.Constant(value, typeof(short?)), typeof(sbyte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((sbyte)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableShortToNullableSByte(short? value, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.Convert(Expression.Constant(value, typeof(short?)), typeof(sbyte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((sbyte?)value), f());
        }

        private static void VerifyNullableShortToShort(short? value, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.Convert(Expression.Constant(value, typeof(short?)), typeof(short)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableShortToNullableShort(short? value, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.Convert(Expression.Constant(value, typeof(short?)), typeof(short?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableShortToUInt(short? value, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.Convert(Expression.Constant(value, typeof(short?)), typeof(uint)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((uint)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableShortToNullableUInt(short? value, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.Convert(Expression.Constant(value, typeof(short?)), typeof(uint?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((uint?)value), f());
        }

        private static void VerifyNullableShortToULong(short? value, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.Convert(Expression.Constant(value, typeof(short?)), typeof(ulong)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((ulong)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableShortToNullableULong(short? value, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.Convert(Expression.Constant(value, typeof(short?)), typeof(ulong?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((ulong?)value), f());
        }

        private static void VerifyNullableShortToUShort(short? value, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.Convert(Expression.Constant(value, typeof(short?)), typeof(ushort)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((ushort)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableShortToNullableUShort(short? value, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.Convert(Expression.Constant(value, typeof(short?)), typeof(ushort?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((ushort?)value), f());
        }

        private static void VerifyUIntToByte(uint value, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.Convert(Expression.Constant(value, typeof(uint)), typeof(byte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((byte)value), f());
        }

        private static void VerifyUIntToNullableByte(uint value, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.Convert(Expression.Constant(value, typeof(uint)), typeof(byte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((byte)value), f());
        }

        private static void VerifyUIntToChar(uint value, bool useInterpreter)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.Convert(Expression.Constant(value, typeof(uint)), typeof(char)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((char)value), f());
        }

        private static void VerifyUIntToNullableChar(uint value, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.Convert(Expression.Constant(value, typeof(uint)), typeof(char?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((char?)value), f());
        }

        private static void VerifyUIntToDecimal(uint value, bool useInterpreter)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.Convert(Expression.Constant(value, typeof(uint)), typeof(decimal)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyUIntToNullableDecimal(uint value, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.Convert(Expression.Constant(value, typeof(uint)), typeof(decimal?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyUIntToDouble(uint value, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.Convert(Expression.Constant(value, typeof(uint)), typeof(double)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyUIntToNullableDouble(uint value, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.Convert(Expression.Constant(value, typeof(uint)), typeof(double?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyUIntToEnum(uint value, bool useInterpreter)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.Convert(Expression.Constant(value, typeof(uint)), typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((E)value), f());
        }

        private static void VerifyUIntToNullableEnum(uint value, bool useInterpreter)
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.Convert(Expression.Constant(value, typeof(uint)), typeof(E?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((E)value), f());
        }

        private static void VerifyUIntToEnumLong(uint value, bool useInterpreter)
        {
            Expression<Func<El>> e =
                Expression.Lambda<Func<El>>(
                    Expression.Convert(Expression.Constant(value, typeof(uint)), typeof(El)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El> f = e.Compile(useInterpreter);

            Assert.Equal((El)value, f());
        }

        private static void VerifyUIntToNullableEnumLong(uint value, bool useInterpreter)
        {
            Expression<Func<El?>> e =
                Expression.Lambda<Func<El?>>(
                    Expression.Convert(Expression.Constant(value, typeof(uint)), typeof(El?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?> f = e.Compile(useInterpreter);

            Assert.Equal((El)value, f());
        }

        private static void VerifyUIntToFloat(uint value, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.Convert(Expression.Constant(value, typeof(uint)), typeof(float)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyUIntToNullableFloat(uint value, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.Convert(Expression.Constant(value, typeof(uint)), typeof(float?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyUIntToInt(uint value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Convert(Expression.Constant(value, typeof(uint)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((int)value), f());
        }

        private static void VerifyUIntToNullableInt(uint value, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.Convert(Expression.Constant(value, typeof(uint)), typeof(int?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((int)value), f());
        }

        private static void VerifyUIntToLong(uint value, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.Convert(Expression.Constant(value, typeof(uint)), typeof(long)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyUIntToNullableLong(uint value, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.Convert(Expression.Constant(value, typeof(uint)), typeof(long?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyUIntToSByte(uint value, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.Convert(Expression.Constant(value, typeof(uint)), typeof(sbyte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((sbyte)value), f());
        }

        private static void VerifyUIntToNullableSByte(uint value, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.Convert(Expression.Constant(value, typeof(uint)), typeof(sbyte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((sbyte)value), f());
        }

        private static void VerifyUIntToShort(uint value, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.Convert(Expression.Constant(value, typeof(uint)), typeof(short)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((short)value), f());
        }

        private static void VerifyUIntToNullableShort(uint value, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.Convert(Expression.Constant(value, typeof(uint)), typeof(short?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((short)value), f());
        }

        private static void VerifyUIntToUInt(uint value, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.Convert(Expression.Constant(value, typeof(uint)), typeof(uint)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyUIntToNullableUInt(uint value, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.Convert(Expression.Constant(value, typeof(uint)), typeof(uint?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyUIntToULong(uint value, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.Convert(Expression.Constant(value, typeof(uint)), typeof(ulong)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyUIntToNullableULong(uint value, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.Convert(Expression.Constant(value, typeof(uint)), typeof(ulong?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyUIntToUShort(uint value, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.Convert(Expression.Constant(value, typeof(uint)), typeof(ushort)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((ushort)value), f());
        }

        private static void VerifyUIntToNullableUShort(uint value, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.Convert(Expression.Constant(value, typeof(uint)), typeof(ushort?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((ushort)value), f());
        }

        private static void VerifyNullableUIntToByte(uint? value, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.Convert(Expression.Constant(value, typeof(uint?)), typeof(byte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((byte)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableUIntToNullableByte(uint? value, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.Convert(Expression.Constant(value, typeof(uint?)), typeof(byte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((byte?)value), f());
        }

        private static void VerifyNullableUIntToChar(uint? value, bool useInterpreter)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.Convert(Expression.Constant(value, typeof(uint?)), typeof(char)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((char)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableUIntToNullableChar(uint? value, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.Convert(Expression.Constant(value, typeof(uint?)), typeof(char?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((char?)value), f());
        }

        private static void VerifyNullableUIntToDecimal(uint? value, bool useInterpreter)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.Convert(Expression.Constant(value, typeof(uint?)), typeof(decimal)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableUIntToNullableDecimal(uint? value, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.Convert(Expression.Constant(value, typeof(uint?)), typeof(decimal?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableUIntToDouble(uint? value, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.Convert(Expression.Constant(value, typeof(uint?)), typeof(double)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableUIntToNullableDouble(uint? value, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.Convert(Expression.Constant(value, typeof(uint?)), typeof(double?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableUIntToEnum(uint? value, bool useInterpreter)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.Convert(Expression.Constant(value, typeof(uint?)), typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((E)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableUIntToNullableEnum(uint? value, bool useInterpreter)
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.Convert(Expression.Constant(value, typeof(uint?)), typeof(E?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((E?)value), f());
        }

        private static void VerifyNullableUIntToEnumLong(uint? value, bool useInterpreter)
        {
            Expression<Func<El>> e =
                Expression.Lambda<Func<El>>(
                    Expression.Convert(Expression.Constant(value, typeof(uint?)), typeof(El)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal((El)value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableUIntToNullableEnumLong(uint? value, bool useInterpreter)
        {
            Expression<Func<El?>> e =
                Expression.Lambda<Func<El?>>(
                    Expression.Convert(Expression.Constant(value, typeof(uint?)), typeof(El?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?> f = e.Compile(useInterpreter);

            Assert.Equal((El?)value, f());
        }

        private static void VerifyNullableUIntToFloat(uint? value, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.Convert(Expression.Constant(value, typeof(uint?)), typeof(float)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableUIntToNullableFloat(uint? value, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.Convert(Expression.Constant(value, typeof(uint?)), typeof(float?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableUIntToInt(uint? value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Convert(Expression.Constant(value, typeof(uint?)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((int)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableUIntToNullableInt(uint? value, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.Convert(Expression.Constant(value, typeof(uint?)), typeof(int?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((int?)value), f());
        }

        private static void VerifyNullableUIntToLong(uint? value, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.Convert(Expression.Constant(value, typeof(uint?)), typeof(long)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableUIntToNullableLong(uint? value, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.Convert(Expression.Constant(value, typeof(uint?)), typeof(long?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);

            Assert.Equal((long?)value, f());
        }

        private static void VerifyNullableUIntToSByte(uint? value, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.Convert(Expression.Constant(value, typeof(uint?)), typeof(sbyte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((sbyte)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableUIntToNullableSByte(uint? value, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.Convert(Expression.Constant(value, typeof(uint?)), typeof(sbyte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((sbyte?)value), f());
        }

        private static void VerifyNullableUIntToShort(uint? value, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.Convert(Expression.Constant(value, typeof(uint?)), typeof(short)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((short)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableUIntToNullableShort(uint? value, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.Convert(Expression.Constant(value, typeof(uint?)), typeof(short?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((short?)value), f());
        }

        private static void VerifyNullableUIntToUInt(uint? value, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.Convert(Expression.Constant(value, typeof(uint?)), typeof(uint)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableUIntToNullableUInt(uint? value, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.Convert(Expression.Constant(value, typeof(uint?)), typeof(uint?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableUIntToULong(uint? value, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.Convert(Expression.Constant(value, typeof(uint?)), typeof(ulong)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableUIntToNullableULong(uint? value, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.Convert(Expression.Constant(value, typeof(uint?)), typeof(ulong?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableUIntToUShort(uint? value, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.Convert(Expression.Constant(value, typeof(uint?)), typeof(ushort)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((ushort)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableUIntToNullableUShort(uint? value, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.Convert(Expression.Constant(value, typeof(uint?)), typeof(ushort?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((ushort?)value), f());
        }

        private static void VerifyULongToByte(ulong value, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.Convert(Expression.Constant(value, typeof(ulong)), typeof(byte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((byte)value), f());
        }

        private static void VerifyULongToNullableByte(ulong value, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.Convert(Expression.Constant(value, typeof(ulong)), typeof(byte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((byte)value), f());
        }

        private static void VerifyULongToChar(ulong value, bool useInterpreter)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.Convert(Expression.Constant(value, typeof(ulong)), typeof(char)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((char)value), f());
        }

        private static void VerifyULongToNullableChar(ulong value, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.Convert(Expression.Constant(value, typeof(ulong)), typeof(char?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((char?)value), f());
        }

        private static void VerifyULongToDecimal(ulong value, bool useInterpreter)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.Convert(Expression.Constant(value, typeof(ulong)), typeof(decimal)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyULongToNullableDecimal(ulong value, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.Convert(Expression.Constant(value, typeof(ulong)), typeof(decimal?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyULongToDouble(ulong value, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.Convert(Expression.Constant(value, typeof(ulong)), typeof(double)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyULongToNullableDouble(ulong value, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.Convert(Expression.Constant(value, typeof(ulong)), typeof(double?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyULongToEnum(ulong value, bool useInterpreter)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.Convert(Expression.Constant(value, typeof(ulong)), typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((E)value), f());
        }

        private static void VerifyULongToNullableEnum(ulong value, bool useInterpreter)
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.Convert(Expression.Constant(value, typeof(ulong)), typeof(E?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((E)value), f());
        }

        private static void VerifyULongToEnumLong(ulong value, bool useInterpreter)
        {
            Expression<Func<El>> e =
                Expression.Lambda<Func<El>>(
                    Expression.Convert(Expression.Constant(value, typeof(ulong)), typeof(El)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((El)value), f());
        }

        private static void VerifyULongToNullableEnumLong(ulong value, bool useInterpreter)
        {
            Expression<Func<El?>> e =
                Expression.Lambda<Func<El?>>(
                    Expression.Convert(Expression.Constant(value, typeof(ulong)), typeof(El?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((El)value), f());
        }

        private static void VerifyULongToFloat(ulong value, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.Convert(Expression.Constant(value, typeof(ulong)), typeof(float)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyULongToNullableFloat(ulong value, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.Convert(Expression.Constant(value, typeof(ulong)), typeof(float?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyULongToInt(ulong value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Convert(Expression.Constant(value, typeof(ulong)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((int)value), f());
        }

        private static void VerifyULongToNullableInt(ulong value, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.Convert(Expression.Constant(value, typeof(ulong)), typeof(int?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((int)value), f());
        }

        private static void VerifyULongToLong(ulong value, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.Convert(Expression.Constant(value, typeof(ulong)), typeof(long)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((long)value), f());
        }

        private static void VerifyULongToNullableLong(ulong value, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.Convert(Expression.Constant(value, typeof(ulong)), typeof(long?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((long)value), f());
        }

        private static void VerifyULongToSByte(ulong value, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.Convert(Expression.Constant(value, typeof(ulong)), typeof(sbyte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((sbyte)value), f());
        }

        private static void VerifyULongToNullableSByte(ulong value, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.Convert(Expression.Constant(value, typeof(ulong)), typeof(sbyte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((sbyte)value), f());
        }

        private static void VerifyULongToShort(ulong value, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.Convert(Expression.Constant(value, typeof(ulong)), typeof(short)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((short)value), f());
        }

        private static void VerifyULongToNullableShort(ulong value, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.Convert(Expression.Constant(value, typeof(ulong)), typeof(short?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((short)value), f());
        }

        private static void VerifyULongToUInt(ulong value, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.Convert(Expression.Constant(value, typeof(ulong)), typeof(uint)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((uint)value), f());
        }

        private static void VerifyULongToNullableUInt(ulong value, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.Convert(Expression.Constant(value, typeof(ulong)), typeof(uint?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((uint)value), f());
        }

        private static void VerifyULongToULong(ulong value, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.Convert(Expression.Constant(value, typeof(ulong)), typeof(ulong)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyULongToNullableULong(ulong value, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.Convert(Expression.Constant(value, typeof(ulong)), typeof(ulong?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyULongToUShort(ulong value, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.Convert(Expression.Constant(value, typeof(ulong)), typeof(ushort)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((ushort)value), f());
        }

        private static void VerifyULongToNullableUShort(ulong value, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.Convert(Expression.Constant(value, typeof(ulong)), typeof(ushort?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((ushort)value), f());
        }

        private static void VerifyNullableULongToByte(ulong? value, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.Convert(Expression.Constant(value, typeof(ulong?)), typeof(byte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((byte)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableULongToNullableByte(ulong? value, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.Convert(Expression.Constant(value, typeof(ulong?)), typeof(byte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((byte?)value), f());
        }

        private static void VerifyNullableULongToChar(ulong? value, bool useInterpreter)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.Convert(Expression.Constant(value, typeof(ulong?)), typeof(char)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((char)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableULongToNullableChar(ulong? value, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.Convert(Expression.Constant(value, typeof(ulong?)), typeof(char?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((char?)value), f());
        }

        private static void VerifyNullableULongToDecimal(ulong? value, bool useInterpreter)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.Convert(Expression.Constant(value, typeof(ulong?)), typeof(decimal)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableULongToNullableDecimal(ulong? value, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.Convert(Expression.Constant(value, typeof(ulong?)), typeof(decimal?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableULongToDouble(ulong? value, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.Convert(Expression.Constant(value, typeof(ulong?)), typeof(double)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableULongToNullableDouble(ulong? value, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.Convert(Expression.Constant(value, typeof(ulong?)), typeof(double?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableULongToEnum(ulong? value, bool useInterpreter)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.Convert(Expression.Constant(value, typeof(ulong?)), typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((E)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableULongToNullableEnum(ulong? value, bool useInterpreter)
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.Convert(Expression.Constant(value, typeof(ulong?)), typeof(E?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((E?)value), f());
        }

        private static void VerifyNullableULongToEnumLong(ulong? value, bool useInterpreter)
        {
            Expression<Func<El>> e =
                Expression.Lambda<Func<El>>(
                    Expression.Convert(Expression.Constant(value, typeof(ulong?)), typeof(El)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((El)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableULongToNullableEnumLong(ulong? value, bool useInterpreter)
        {
            Expression<Func<El?>> e =
                Expression.Lambda<Func<El?>>(
                    Expression.Convert(Expression.Constant(value, typeof(ulong?)), typeof(El?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((El?)value), f());
        }

        private static void VerifyNullableULongToFloat(ulong? value, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.Convert(Expression.Constant(value, typeof(ulong?)), typeof(float)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableULongToNullableFloat(ulong? value, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.Convert(Expression.Constant(value, typeof(ulong?)), typeof(float?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableULongToInt(ulong? value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Convert(Expression.Constant(value, typeof(ulong?)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((int)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableULongToNullableInt(ulong? value, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.Convert(Expression.Constant(value, typeof(ulong?)), typeof(int?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((int?)value), f());
        }

        private static void VerifyNullableULongToLong(ulong? value, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.Convert(Expression.Constant(value, typeof(ulong?)), typeof(long)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((long)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableULongToNullableLong(ulong? value, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.Convert(Expression.Constant(value, typeof(ulong?)), typeof(long?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((long?)value), f());
        }

        private static void VerifyNullableULongToSByte(ulong? value, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.Convert(Expression.Constant(value, typeof(ulong?)), typeof(sbyte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((sbyte)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableULongToNullableSByte(ulong? value, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.Convert(Expression.Constant(value, typeof(ulong?)), typeof(sbyte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((sbyte?)value), f());
        }

        private static void VerifyNullableULongToShort(ulong? value, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.Convert(Expression.Constant(value, typeof(ulong?)), typeof(short)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((short)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableULongToNullableShort(ulong? value, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.Convert(Expression.Constant(value, typeof(ulong?)), typeof(short?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((short?)value), f());
        }

        private static void VerifyNullableULongToUInt(ulong? value, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.Convert(Expression.Constant(value, typeof(ulong?)), typeof(uint)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((uint)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableULongToNullableUInt(ulong? value, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.Convert(Expression.Constant(value, typeof(ulong?)), typeof(uint?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((uint?)value), f());
        }

        private static void VerifyNullableULongToULong(ulong? value, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.Convert(Expression.Constant(value, typeof(ulong?)), typeof(ulong)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableULongToNullableULong(ulong? value, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.Convert(Expression.Constant(value, typeof(ulong?)), typeof(ulong?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableULongToUShort(ulong? value, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.Convert(Expression.Constant(value, typeof(ulong?)), typeof(ushort)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((ushort)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableULongToNullableUShort(ulong? value, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.Convert(Expression.Constant(value, typeof(ulong?)), typeof(ushort?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((ushort?)value), f());
        }

        private static void VerifyUShortToByte(ushort value, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.Convert(Expression.Constant(value, typeof(ushort)), typeof(byte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((byte)value), f());
        }

        private static void VerifyUShortToNullableByte(ushort value, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.Convert(Expression.Constant(value, typeof(ushort)), typeof(byte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((byte)value), f());
        }

        private static void VerifyUShortToChar(ushort value, bool useInterpreter)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.Convert(Expression.Constant(value, typeof(ushort)), typeof(char)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile(useInterpreter);

            Assert.Equal((char)value, f());
        }

        private static void VerifyUShortToNullableChar(ushort value, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.Convert(Expression.Constant(value, typeof(ushort)), typeof(char?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile(useInterpreter);

            Assert.Equal((char?)value, f());
        }

        private static void VerifyUShortToDecimal(ushort value, bool useInterpreter)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.Convert(Expression.Constant(value, typeof(ushort)), typeof(decimal)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyUShortToNullableDecimal(ushort value, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.Convert(Expression.Constant(value, typeof(ushort)), typeof(decimal?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyUShortToDouble(ushort value, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.Convert(Expression.Constant(value, typeof(ushort)), typeof(double)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyUShortToNullableDouble(ushort value, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.Convert(Expression.Constant(value, typeof(ushort)), typeof(double?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyUShortToEnum(ushort value, bool useInterpreter)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.Convert(Expression.Constant(value, typeof(ushort)), typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile(useInterpreter);

            Assert.Equal((E)value, f());
        }

        private static void VerifyUShortToNullableEnum(ushort value, bool useInterpreter)
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.Convert(Expression.Constant(value, typeof(ushort)), typeof(E?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile(useInterpreter);

            Assert.Equal((E)value, f());
        }

        private static void VerifyUShortToEnumLong(ushort value, bool useInterpreter)
        {
            Expression<Func<El>> e =
                Expression.Lambda<Func<El>>(
                    Expression.Convert(Expression.Constant(value, typeof(ushort)), typeof(El)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El> f = e.Compile(useInterpreter);

            Assert.Equal((El)value, f());
        }

        private static void VerifyUShortToNullableEnumLong(ushort value, bool useInterpreter)
        {
            Expression<Func<El?>> e =
                Expression.Lambda<Func<El?>>(
                    Expression.Convert(Expression.Constant(value, typeof(ushort)), typeof(El?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?> f = e.Compile(useInterpreter);

            Assert.Equal((El)value, f());
        }

        private static void VerifyUShortToFloat(ushort value, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.Convert(Expression.Constant(value, typeof(ushort)), typeof(float)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyUShortToNullableFloat(ushort value, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.Convert(Expression.Constant(value, typeof(ushort)), typeof(float?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyUShortToInt(ushort value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Convert(Expression.Constant(value, typeof(ushort)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyUShortToNullableInt(ushort value, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.Convert(Expression.Constant(value, typeof(ushort)), typeof(int?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyUShortToLong(ushort value, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.Convert(Expression.Constant(value, typeof(ushort)), typeof(long)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyUShortToNullableLong(ushort value, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.Convert(Expression.Constant(value, typeof(ushort)), typeof(long?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyUShortToSByte(ushort value, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.Convert(Expression.Constant(value, typeof(ushort)), typeof(sbyte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((sbyte)value), f());
        }

        private static void VerifyUShortToNullableSByte(ushort value, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.Convert(Expression.Constant(value, typeof(ushort)), typeof(sbyte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((sbyte)value), f());
        }

        private static void VerifyUShortToShort(ushort value, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.Convert(Expression.Constant(value, typeof(ushort)), typeof(short)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((short)value), f());
        }

        private static void VerifyUShortToNullableShort(ushort value, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.Convert(Expression.Constant(value, typeof(ushort)), typeof(short?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((short)value), f());
        }

        private static void VerifyUShortToUInt(ushort value, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.Convert(Expression.Constant(value, typeof(ushort)), typeof(uint)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyUShortToNullableUInt(ushort value, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.Convert(Expression.Constant(value, typeof(ushort)), typeof(uint?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyUShortToULong(ushort value, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.Convert(Expression.Constant(value, typeof(ushort)), typeof(ulong)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyUShortToNullableULong(ushort value, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.Convert(Expression.Constant(value, typeof(ushort)), typeof(ulong?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyUShortToUShort(ushort value, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.Convert(Expression.Constant(value, typeof(ushort)), typeof(ushort)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyUShortToNullableUShort(ushort value, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.Convert(Expression.Constant(value, typeof(ushort)), typeof(ushort?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableUShortToByte(ushort? value, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.Convert(Expression.Constant(value, typeof(ushort?)), typeof(byte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((byte)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableUShortToNullableByte(ushort? value, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.Convert(Expression.Constant(value, typeof(ushort?)), typeof(byte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((byte?)value), f());
        }

        private static void VerifyNullableUShortToChar(ushort? value, bool useInterpreter)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.Convert(Expression.Constant(value, typeof(ushort?)), typeof(char)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal((char)value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableUShortToNullableChar(ushort? value, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.Convert(Expression.Constant(value, typeof(ushort?)), typeof(char?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile(useInterpreter);

            Assert.Equal((char?)value, f());
        }

        private static void VerifyNullableUShortToDecimal(ushort? value, bool useInterpreter)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.Convert(Expression.Constant(value, typeof(ushort?)), typeof(decimal)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableUShortToNullableDecimal(ushort? value, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.Convert(Expression.Constant(value, typeof(ushort?)), typeof(decimal?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableUShortToDouble(ushort? value, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.Convert(Expression.Constant(value, typeof(ushort?)), typeof(double)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableUShortToNullableDouble(ushort? value, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.Convert(Expression.Constant(value, typeof(ushort?)), typeof(double?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableUShortToEnum(ushort? value, bool useInterpreter)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.Convert(Expression.Constant(value, typeof(ushort?)), typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal((E)value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableUShortToNullableEnum(ushort? value, bool useInterpreter)
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.Convert(Expression.Constant(value, typeof(ushort?)), typeof(E?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile(useInterpreter);

            Assert.Equal((E?)value, f());
        }

        private static void VerifyNullableUShortToEnumLong(ushort? value, bool useInterpreter)
        {
            Expression<Func<El>> e =
                Expression.Lambda<Func<El>>(
                    Expression.Convert(Expression.Constant(value, typeof(ushort?)), typeof(El)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal((El)value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableUShortToNullableEnumLong(ushort? value, bool useInterpreter)
        {
            Expression<Func<El?>> e =
                Expression.Lambda<Func<El?>>(
                    Expression.Convert(Expression.Constant(value, typeof(ushort?)), typeof(El?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?> f = e.Compile(useInterpreter);

            Assert.Equal((El?)value, f());
        }

        private static void VerifyNullableUShortToFloat(ushort? value, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.Convert(Expression.Constant(value, typeof(ushort?)), typeof(float)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableUShortToNullableFloat(ushort? value, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.Convert(Expression.Constant(value, typeof(ushort?)), typeof(float?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableUShortToInt(ushort? value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Convert(Expression.Constant(value, typeof(ushort?)), typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableUShortToNullableInt(ushort? value, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.Convert(Expression.Constant(value, typeof(ushort?)), typeof(int?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableUShortToLong(ushort? value, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.Convert(Expression.Constant(value, typeof(ushort?)), typeof(long)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableUShortToNullableLong(ushort? value, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.Convert(Expression.Constant(value, typeof(ushort?)), typeof(long?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableUShortToSByte(ushort? value, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.Convert(Expression.Constant(value, typeof(ushort?)), typeof(sbyte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((sbyte)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableUShortToNullableSByte(ushort? value, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.Convert(Expression.Constant(value, typeof(ushort?)), typeof(sbyte?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((sbyte?)value), f());
        }

        private static void VerifyNullableUShortToShort(ushort? value, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.Convert(Expression.Constant(value, typeof(ushort?)), typeof(short)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(unchecked((short)value.GetValueOrDefault()), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableUShortToNullableShort(ushort? value, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.Convert(Expression.Constant(value, typeof(ushort?)), typeof(short?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((short?)value), f());
        }

        private static void VerifyNullableUShortToUInt(ushort? value, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.Convert(Expression.Constant(value, typeof(ushort?)), typeof(uint)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableUShortToNullableUInt(ushort? value, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.Convert(Expression.Constant(value, typeof(ushort?)), typeof(uint?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableUShortToULong(ushort? value, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.Convert(Expression.Constant(value, typeof(ushort?)), typeof(ulong)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value.GetValueOrDefault(), f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableUShortToNullableULong(ushort? value, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.Convert(Expression.Constant(value, typeof(ushort?)), typeof(ulong?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableUShortToUShort(ushort? value, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.Convert(Expression.Constant(value, typeof(ushort?)), typeof(ushort)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);

            if (value.HasValue)
                Assert.Equal(value, f());
            else
                Assert.Throws<InvalidOperationException>(() => f());
        }

        private static void VerifyNullableUShortToNullableUShort(ushort? value, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.Convert(Expression.Constant(value, typeof(ushort?)), typeof(ushort?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        #endregion

        private class PerverselyNamedMembers
        {
            public readonly uint Field;

            public PerverselyNamedMembers(uint value)
            {
                Field = value;
            }

            public static uint op_Implicit()
            {
                return 0x8BADF00D;
            }
        }

        private struct HalfLiftedTo
        {
        }

        private struct ImplicitHalfLiftedFrom
        {
            public bool NullEquiv { get; set; }

            public static implicit operator HalfLiftedTo? (ImplicitHalfLiftedFrom source) =>
                source.NullEquiv ? default(HalfLiftedTo?) : new HalfLiftedTo();
        }

        private struct ExplicitHalfLiftedFrom
        {
            public bool NullEquiv { get; set; }

            public static explicit operator HalfLiftedTo? (ExplicitHalfLiftedFrom source) =>
                source.NullEquiv ? default(HalfLiftedTo?) : new HalfLiftedTo();
        }

        private struct ImplicitHalfLiftedOverloaded
        {
            public static implicit operator HalfLiftedTo?(ImplicitHalfLiftedOverloaded source) => new HalfLiftedTo();

            public static implicit operator HalfLiftedTo?(ImplicitHalfLiftedOverloaded? source) => new HalfLiftedTo();
        }

        private struct ImplicitHalfLiftedFromReverse
        {
            public static implicit operator HalfLiftedTo(ImplicitHalfLiftedFromReverse? source) => new HalfLiftedTo();
        }

        private struct HalfLiftedToTargetOperator
        {
            public static implicit operator HalfLiftedToTargetOperator?(HalfLiftedFromTargetOperator source) =>
                source.NullEquiv ? default(HalfLiftedToTargetOperator?) : new HalfLiftedToTargetOperator();
        }

        private struct HalfLiftedFromTargetOperator
        {
            public bool NullEquiv { get; set; }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ImplicitHalfLiftedConversionFromCSCompiler(bool useInterpreter)
        {
            Expression<Func<ImplicitHalfLiftedFrom?, HalfLiftedTo?>> e = x => x;
            Func<ImplicitHalfLiftedFrom?, HalfLiftedTo?> f = e.Compile(useInterpreter);
            Assert.NotNull(f(new ImplicitHalfLiftedFrom()));
            Assert.Null(f(new ImplicitHalfLiftedFrom { NullEquiv = true }));
            Assert.Null(f(null));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ExplicitHalfLiftedConversionFromCSCompiler(bool useInterpreter)
        {
            Expression<Func<ExplicitHalfLiftedFrom?, HalfLiftedTo?>> e = x => (HalfLiftedTo?)x;
            Func<ExplicitHalfLiftedFrom?, HalfLiftedTo?> f = e.Compile(useInterpreter);
            Assert.NotNull(f(new ExplicitHalfLiftedFrom()));
            Assert.Null(f(new ExplicitHalfLiftedFrom { NullEquiv = true }));
            Assert.Null(f(null));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ImplicitHalfLiftedOverloadedConversionFromCSCompiler(bool useInterpreter)
        {
            Expression<Func<ImplicitHalfLiftedOverloaded?, HalfLiftedTo?>> e = x => x;
            Func<ImplicitHalfLiftedOverloaded?, HalfLiftedTo?> f = e.Compile(useInterpreter);
            Assert.NotNull(f(new ImplicitHalfLiftedOverloaded()));
            Assert.NotNull(f(null));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ImplicitHalfLiftedConversion(bool useInterpreter)
        {
            ParameterExpression x = Expression.Parameter(typeof(ImplicitHalfLiftedFrom?));
            Expression<Func<ImplicitHalfLiftedFrom?, HalfLiftedTo?>> e =
                Expression.Lambda<Func<ImplicitHalfLiftedFrom?, HalfLiftedTo?>>(
                    Expression.Convert(x, typeof(HalfLiftedTo?)),
                    x);
            Func<ImplicitHalfLiftedFrom?, HalfLiftedTo?> f = e.Compile(useInterpreter);
            Assert.NotNull(f(new ImplicitHalfLiftedFrom()));
            Assert.Null(f(new ImplicitHalfLiftedFrom { NullEquiv = true }));
            Assert.Null(f(null));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ExplicitHalfLiftedConversion(bool useInterpreter)
        {
            ParameterExpression x = Expression.Parameter(typeof(ExplicitHalfLiftedFrom?));
            Expression<Func<ExplicitHalfLiftedFrom?, HalfLiftedTo?>> e =
                Expression.Lambda<Func<ExplicitHalfLiftedFrom?, HalfLiftedTo?>>(
                    Expression.Convert(x, typeof(HalfLiftedTo?)),
                    x);
            Func<ExplicitHalfLiftedFrom?, HalfLiftedTo?> f = e.Compile(useInterpreter);
            Assert.NotNull(f(new ExplicitHalfLiftedFrom()));
            Assert.Null(f(new ExplicitHalfLiftedFrom { NullEquiv = true }));
            Assert.Null(f(null));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ImplicitHalfLiftedOverloadedConversion(bool useInterpreter)
        {
            ParameterExpression x = Expression.Parameter(typeof(ImplicitHalfLiftedOverloaded?));
            Expression<Func<ImplicitHalfLiftedOverloaded?, HalfLiftedTo?>> e =
                Expression.Lambda<Func<ImplicitHalfLiftedOverloaded?, HalfLiftedTo?>>(
                    Expression.Convert(x, typeof(HalfLiftedTo?)),
                    x);
            Func<ImplicitHalfLiftedOverloaded?, HalfLiftedTo?> f = e.Compile(useInterpreter);
            Assert.NotNull(f(new ImplicitHalfLiftedOverloaded()));
            Assert.NotNull(f(null));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ImplicitHalfLiftedConversionExplicitlySetMethod(bool useInterpreter)
        {
            ParameterExpression x = Expression.Parameter(typeof(ImplicitHalfLiftedFrom?));
            Expression<Func<ImplicitHalfLiftedFrom?, HalfLiftedTo?>> e =
                Expression.Lambda<Func<ImplicitHalfLiftedFrom?, HalfLiftedTo?>>(
                    Expression.Convert(x, typeof(HalfLiftedTo?), typeof(ImplicitHalfLiftedFrom).GetMethod("op_Implicit")),
                    x);
            Func<ImplicitHalfLiftedFrom?, HalfLiftedTo?> f = e.Compile(useInterpreter);
            Assert.NotNull(f(new ImplicitHalfLiftedFrom()));
            Assert.Null(f(new ImplicitHalfLiftedFrom { NullEquiv = true }));
            Assert.Null(f(null));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ExplicitHalfLiftedConversionExplicitlySetMethod(bool useInterpreter)
        {
            ParameterExpression x = Expression.Parameter(typeof(ExplicitHalfLiftedFrom?));
            Expression<Func<ExplicitHalfLiftedFrom?, HalfLiftedTo?>> e =
                Expression.Lambda<Func<ExplicitHalfLiftedFrom?, HalfLiftedTo?>>(
                    Expression.Convert(x, typeof(HalfLiftedTo?), typeof(ExplicitHalfLiftedFrom).GetMethod("op_Explicit")),
                    x);
            Func<ExplicitHalfLiftedFrom?, HalfLiftedTo?> f = e.Compile(useInterpreter);
            Assert.NotNull(f(new ExplicitHalfLiftedFrom()));
            Assert.Null(f(new ExplicitHalfLiftedFrom { NullEquiv = true }));
            Assert.Null(f(null));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ImplicitHalfLiftedOverloadedConversionExplicitlySetMethod(bool useInterpreter)
        {
            List<MethodInfo> opMethods =
                typeof(ImplicitHalfLiftedOverloaded).GetMethods().Where(m => m.Name == "op_Implicit").ToList();
            MethodInfo direct =
                opMethods.First(m => m.GetParameters()[0].ParameterType == typeof(ImplicitHalfLiftedOverloaded?));
            MethodInfo liftNeeded =
                opMethods.First(m => m.GetParameters()[0].ParameterType == typeof(ImplicitHalfLiftedOverloaded));
            ParameterExpression x = Expression.Parameter(typeof(ImplicitHalfLiftedOverloaded?));
            Expression<Func<ImplicitHalfLiftedOverloaded?, HalfLiftedTo?>> e =
                Expression.Lambda<Func<ImplicitHalfLiftedOverloaded?, HalfLiftedTo?>>(
                    Expression.Convert(x, typeof(HalfLiftedTo?), direct),
                    x);
            Func<ImplicitHalfLiftedOverloaded?, HalfLiftedTo?> f = e.Compile(useInterpreter);
            Assert.NotNull(f(new ImplicitHalfLiftedOverloaded()));
            Assert.NotNull(f(null));
            e = Expression.Lambda<Func<ImplicitHalfLiftedOverloaded?, HalfLiftedTo?>>(
                Expression.Convert(x, typeof(HalfLiftedTo?), liftNeeded), x);
            f = e.Compile(useInterpreter);
            Assert.NotNull(f(new ImplicitHalfLiftedOverloaded()));
            Assert.Null(f(null));
        }

        [Fact]
        public static void ImplicitHalfLiftedReverseConversion()
        {
            // In the case where there is a conversion from? -> to, then if
            // we want to do from? -> to? we should do two conversions;
            // from? -> to -> to?, since converting any value to the nullable
            // form of its type is always possible and well-defined.
            // The compiler correctly does this double-conversion in such cases.
            // We should probably not allow it to be done as a single lifted operation.
            ParameterExpression x = Expression.Parameter(typeof(ImplicitHalfLiftedFromReverse?));
            Assert.Throws<InvalidOperationException>(() => Expression.Convert(
                x, typeof(HalfLiftedTo?), typeof(ImplicitHalfLiftedFromReverse).GetMethod("op_Implicit")));
            Assert.Throws<InvalidOperationException>(() => Expression.Convert(
                x, typeof(HalfLiftedTo?)));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ImplicitHalfLiftedConversionOpOnTargetFromCSCompiler(bool useInterpreter)
        {
            Expression<Func<HalfLiftedFromTargetOperator?, HalfLiftedToTargetOperator?>> e = x => x;
            Func<HalfLiftedFromTargetOperator?, HalfLiftedToTargetOperator?> f = e.Compile(useInterpreter);
            Assert.NotNull(f(new HalfLiftedFromTargetOperator()));
            Assert.Null(f(new HalfLiftedFromTargetOperator { NullEquiv = true }));
            Assert.Null(f(null));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ImplicitHalfLiftedConversionOpOnTarget(bool useInterpreter)
        {
            ParameterExpression x = Expression.Parameter(typeof(HalfLiftedFromTargetOperator?));
            Expression<Func<HalfLiftedFromTargetOperator?, HalfLiftedToTargetOperator?>> e =
                Expression.Lambda<Func<HalfLiftedFromTargetOperator?, HalfLiftedToTargetOperator?>>(
                    Expression.Convert(x, typeof(HalfLiftedToTargetOperator?)),
                    x);
            Func<HalfLiftedFromTargetOperator?, HalfLiftedToTargetOperator?> f = e.Compile(useInterpreter);
            Assert.NotNull(f(new HalfLiftedFromTargetOperator()));
            Assert.Null(f(new HalfLiftedFromTargetOperator { NullEquiv = true }));
            Assert.Null(f(null));
        }

        [Fact]
        public static void ExplicitOpImplicit()
        {
            Assert.Throws<InvalidOperationException>(() => Expression.Convert(Expression.Constant(new PerverselyNamedMembers(0)), typeof(uint)));
        }

        [Fact]
        public static void OpenGenericnType()
        {
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Convert(Expression.Constant(null), typeof(List<>)));
        }

        [Fact]
        public static void TypeContainingGenericParameters()
        {
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Convert(Expression.Constant(null), typeof(List<>.Enumerator)));
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Convert(Expression.Constant(null), typeof(List<>).MakeGenericType(typeof(List<>))));
        }

        [Fact]
        public static void ByRefType()
        {
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Convert(Expression.Constant(null), typeof(object).MakeByRefType()));
        }

        [Fact]
        public static void PointerType()
        {
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Convert(Expression.Constant(null), typeof(int*)));
        }

        public static IEnumerable<object[]> Conversions()
        {
            yield return new object[] { 3, 3 };
            yield return new object[] { (byte)3, 3 };
            yield return new object[] { 3, 3.0 };
            yield return new object[] { 3.0, 3 };
            yield return new object[] { 12345678, (short)24910 };
        }

        [Theory, PerCompilationType(nameof(Conversions))]
        public static void ConvertMakeUnary(object source, object result, bool useInterpreter)
        {
            LambdaExpression lambda = Expression.Lambda(
                Expression.MakeUnary(ExpressionType.Convert, Expression.Constant(source), result.GetType())
                );
            Delegate del = lambda.Compile(useInterpreter);
            Assert.Equal(result, del.DynamicInvoke());
        }

        private class CustomConversions
        {
            public int Value { get; set; }

            public static int ConvertToInt(CustomConversions cc) => cc.Value;

            public static CustomConversions ConvertFromInt(int x) => new CustomConversions {Value = x};

            public static CustomConversions ConvertFromRefInt(ref int x) => new CustomConversions { Value = x++ };

            public static void DoNothing(CustomConversions cc)
            {
            }

            public static CustomConversions Create() => new CustomConversions();

            public static CustomConversions FromAddition(int x, int y) => new CustomConversions {Value = x + y};
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CustomConversionNotStandardNameTo(bool useInterpreter)
        {
            Expression operand = Expression.Constant(new CustomConversions { Value = 9 });
            MethodInfo method = typeof(CustomConversions).GetMethod(nameof(CustomConversions.ConvertToInt));
            Expression<Func<int>> lambda = Expression.Lambda<Func<int>>(
                Expression.Convert(operand, typeof(int), method));
            Func<int> func = lambda.Compile(useInterpreter);
            Assert.Equal(9, func());
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CustomConversionNotStandardNameFrom(bool useInterpreter)
        {
            Expression operand = Expression.Constant(4);
            MethodInfo method = typeof(CustomConversions).GetMethod(nameof(CustomConversions.ConvertFromInt));
            Expression<Func<CustomConversions>> lambda = Expression.Lambda<Func<CustomConversions>>(
                Expression.Convert(operand, typeof(CustomConversions), method));
            Func<CustomConversions> func = lambda.Compile(useInterpreter);
            Assert.Equal(4, func().Value);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CustomConversionNotStandardNameFromLifted(bool useInterpreter)
        {
            Expression operand = Expression.Constant(4, typeof(int?));
            MethodInfo method = typeof(CustomConversions).GetMethod(nameof(CustomConversions.ConvertFromInt));
            Expression<Func<CustomConversions>> lambda = Expression.Lambda<Func<CustomConversions>>(
                Expression.Convert(operand, typeof(CustomConversions), method));
            Func<CustomConversions> func = lambda.Compile(useInterpreter);
            Assert.Equal(4, func().Value);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CustomConversionNotStandardNameFromLiftedNullOperand(bool useInterpreter)
        {
            Expression operand = Expression.Constant(null, typeof(int?));
            MethodInfo method = typeof(CustomConversions).GetMethod(nameof(CustomConversions.ConvertFromInt));
            Expression<Func<CustomConversions>> lambda = Expression.Lambda<Func<CustomConversions>>(
                Expression.Convert(operand, typeof(CustomConversions), method));
            Func<CustomConversions> func = lambda.Compile(useInterpreter);
            Assert.Throws<InvalidOperationException>(() => func());
        }

        public delegate TResult ByRefFunc<T, TResult>(ref T arg);

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CustomConversionNotStandardNameFromLiftedByRef(bool useInterpreter)
        {
            var param = Expression.Parameter(typeof(int?).MakeByRefType());
            MethodInfo method = typeof(CustomConversions).GetMethod(nameof(CustomConversions.ConvertFromRefInt));
            Expression<ByRefFunc<int?, CustomConversions>> lambda = Expression.Lambda<ByRefFunc<int?, CustomConversions>>(
                Expression.Convert(param, typeof(CustomConversions), method), param);
            ByRefFunc<int?, CustomConversions> func = lambda.Compile(useInterpreter);
            int? x = 5;
            Assert.Equal(5, func(ref x).Value);
            Assert.Equal(5, x); // Refness is lost on lifting.
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CustomConversionNotStandardNameFromByRef(bool useInterpreter)
        {
            var param = Expression.Parameter(typeof(int).MakeByRefType());
            MethodInfo method = typeof(CustomConversions).GetMethod(nameof(CustomConversions.ConvertFromRefInt));
            Expression<ByRefFunc<int, CustomConversions>> lambda = Expression.Lambda<ByRefFunc<int, CustomConversions>>(
                Expression.Convert(param, typeof(CustomConversions), method), param);
            ByRefFunc<int, CustomConversions> func = lambda.Compile(useInterpreter);
            int x = 5;
            Assert.Equal(5, func(ref x).Value);
            Assert.Equal(6, x);
        }

        [Fact]
        public static void CustomConversionNotStandardNameToWrongType()
        {
            Expression operand = Expression.Constant(new CustomConversions { Value = 9 });
            MethodInfo method = typeof(CustomConversions).GetMethod(nameof(CustomConversions.ConvertToInt));
            Assert.Throws<InvalidOperationException>(() => Expression.Convert(operand, typeof(long), method));
        }

        [Fact]
        public static void CustomConversionNotStandardNameFromWrongType()
        {
            Expression operand = Expression.Constant(4L);
            MethodInfo method = typeof(CustomConversions).GetMethod(nameof(CustomConversions.ConvertFromInt));
            Assert.Throws<InvalidOperationException>(() => Expression.Convert(operand, typeof(CustomConversions), method));
        }

        [Fact]
        public static void CustomConversionNotStandardNameToVoidReturn()
        {
            Expression operand = Expression.Constant(new CustomConversions { Value = 9 });
            MethodInfo method = typeof(CustomConversions).GetMethod(nameof(CustomConversions.DoNothing));
            AssertExtensions.Throws<ArgumentException>("method", () => Expression.Convert(operand, typeof(int), method));
        }

        [Fact]
        public static void CustomConversionNotStandardNameToNullary()
        {
            Expression operand = Expression.Constant(new CustomConversions { Value = 9 });
            MethodInfo method = typeof(CustomConversions).GetMethod(nameof(CustomConversions.Create));
            AssertExtensions.Throws<ArgumentException>("method", () => Expression.Convert(operand, typeof(int), method));
        }

        [Fact]
        public static void CustomConversionNotStandardNameToExcessiveArity()
        {
            Expression operand = Expression.Constant(new CustomConversions { Value = 9 });
            MethodInfo method = typeof(CustomConversions).GetMethod(nameof(CustomConversions.FromAddition));
            AssertExtensions.Throws<ArgumentException>("method", () => Expression.Convert(operand, typeof(int), method));
        }

        [Fact]
        public static void CannotConvertNonVoidToVoid()
        {
            Assert.Throws<InvalidOperationException>(() => Expression.Convert(Expression.Constant(1), typeof(void)));
            Assert.Throws<InvalidOperationException>(() => Expression.Convert(Expression.Constant("a"), typeof(void)));
            Assert.Throws<InvalidOperationException>(() => Expression.Convert(Expression.Constant(DateTime.MinValue), typeof(void)));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertVoidToVoid(bool useInterpreter)
        {
            Action act = Expression.Lambda<Action>(Expression.Convert(Expression.Empty(), typeof(void)))
                .Compile(useInterpreter);
            act();
        }

        [Fact]
        public static void ConvertReferenceArrayToValueTypeArray()
        {
            Assert.Throws<InvalidOperationException>(
                () => Expression.Convert(Expression.Default(typeof(string[])), typeof(int[])));
        }

        [Fact]
        public static void ConvertValueTypeArrayToValueTypeArray()
        {
            Assert.Throws<InvalidOperationException>(
                () => Expression.Convert(Expression.Default(typeof(long[])), typeof(int[])));
        }

        [Fact]
        public static void ConvertValueTypeArrayToReferenceArray()
        {
            Assert.Throws<InvalidOperationException>(
                () => Expression.Convert(Expression.Default(typeof(StringComparison[])), typeof(string[])));
        }

        [Fact]
        public static void ConvertSealedTypeArrayToNonImplementedInterfaceArray()
        {
            Assert.Throws<InvalidOperationException>(
                () => Expression.Convert(Expression.Default(typeof(string[])), typeof(IAsyncResult[])));
        }

        [Fact]
        public static void ConvertNonImplementedInterfaceArrayToSealedTypeArray()
        {
            Assert.Throws<InvalidOperationException>(
                () => Expression.Convert(Expression.Default(typeof(IAsyncResult[])), typeof(string[])));
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
            Expression<Func<NonSealed[][], IInterface[][]>> e = a => (IInterface[][])a;
            Func<NonSealed[][], IInterface[][]> f = e.Compile(useInterpreter);
            Derived[][] arr = new[]{new[] {new Derived(), new Derived(), new Derived(), new Derived()}};
            Assert.Same(arr, f(arr));
            Assert.Null(f(null));
            Assert.Throws<InvalidCastException>(() => f(Array.Empty<NonSealed[]>()));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void IfaceArrayToNonSealedArray(bool useInterpreter)
        {
            Expression<Func<IInterface[][], NonSealed[][]>> e = a => (NonSealed[][])a;
            Func<IInterface[][], NonSealed[][]> f = e.Compile(useInterpreter);
            Derived[][] arr = new[] {new[] {new Derived(), new Derived(), new Derived(), new Derived()}};
            Assert.Same(arr, f(arr));
            Assert.Null(f(null));
            Assert.Throws<InvalidCastException>(() => f(Array.Empty<IInterface[]>()));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void NonSealedICollectionToIfaceArray(bool useInterpreter)
        {
            Expression<Func<ICollection<NonSealed[]>, IInterface[][]>> e = a => (IInterface[][])a;
            Func<ICollection<NonSealed[]>, IInterface[][]> f = e.Compile(useInterpreter);
            Derived[][] arr = new[] {new[] {new Derived(), new Derived(), new Derived(), new Derived()}};
            Assert.Same(arr, f(arr));
            Assert.Null(f(null));
            Assert.Throws<InvalidCastException>(() => f(Array.Empty<NonSealed[]>()));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void IfaceArrayToNonSealedIList(bool useInterpreter)
        {
            Expression<Func<IInterface[][], IList<NonSealed>[]>> e = a => (IList<NonSealed>[])a;
            Func<IInterface[][], IList<NonSealed>[]> f = e.Compile(useInterpreter);
            Derived[][] arr = new[] {new[] {new Derived(), new Derived(), new Derived(), new Derived()}};
            Assert.Same(arr, f(arr));
            Assert.Null(f(null));
            Assert.Throws<InvalidCastException>(() => f(Array.Empty<IInterface[]>()));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void NonSealedArrayToIfaceIEnumerable(bool useInterpreter)
        {
            Expression<Func<NonSealed[][], IEnumerable<IInterface>[]>> e = a => (IEnumerable<IInterface>[])a;
            Func<NonSealed[][], IEnumerable<IInterface>[]> f = e.Compile(useInterpreter);
            Derived[][] arr = new[] {new[] {new Derived(), new Derived(), new Derived(), new Derived()}};
            Assert.Same(arr, f(arr));
            Assert.Null(f(null));
            Assert.Throws<InvalidCastException>(() => f(Array.Empty<NonSealed[]>()));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void IfaceIReadonlyCollectionToNonSealedArray(bool useInterpreter)
        {
            Expression<Func<IReadOnlyCollection<IInterface>[], NonSealed[][]>> e = a => (NonSealed[][])a;
            Func<IReadOnlyCollection<IInterface>[], NonSealed[][]> f = e.Compile(useInterpreter);
            Derived[][] arr = new[] {new[] {new Derived(), new Derived(), new Derived(), new Derived()}};
            Assert.Same(arr, f(arr));
            Assert.Null(f(null));
            Assert.Throws<InvalidCastException>(() => f(Array.Empty<IInterface[]>()));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void IFaceIListToObjectArray(bool useInterpreter)
        {
            Expression<Func<IList<IInterface[]>, object[][]>> e = a => (object[][])a;
            Func<IList<IInterface[]>, object[][]> f = e.Compile(useInterpreter);
            Derived[][] arr = new[] { new[] { new Derived(), new Derived(), new Derived(), new Derived() } };
            Assert.Same(arr, f(arr));
            Assert.Null(f(null));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ObjectIListToIFaceArray(bool useInterpreter)
        {
            Expression<Func<IList<object[]>, IInterface[][]>> e = a => (IInterface[][])a;
            Func<IList<object[]>, IInterface[][]> f = e.Compile(useInterpreter);
            Derived[][] arr = new[] { new[] { new Derived(), new Derived(), new Derived(), new Derived() } };
            Assert.Same(arr, f(arr));
            Assert.Null(f(null));
            Assert.Throws<InvalidCastException>(() => f(Array.Empty<string[]>()));
        }

        [Fact]
        public static void IfaceToNonSZArray()
        {
            Assert.Throws<InvalidOperationException>(
                () => Expression.Convert(Expression.Default(typeof(IList<NonSealed>[])), typeof(NonSealed[,][])));
        }

        [Fact]
        public static void NonSZArrayToIface()
        {
            Assert.Throws<InvalidOperationException>(
                () => Expression.Convert(Expression.Default(typeof(NonSealed[,][])), typeof(IList<NonSealed>[])));
        }

        [Fact]
        public static void ArrayToNonArrayCompatibleIFace()
        {
            Assert.Throws<InvalidOperationException>(
                () => Expression.Convert(Expression.Default(typeof(NonSealed[][])), typeof(IEquatable<NonSealed>[])));
            Assert.Throws<InvalidOperationException>(
                () => Expression.Convert(Expression.Default(typeof(NonSealed[][])), typeof(IDictionary<NonSealed, NonSealed>[])));
        }

        [Fact]
        public static void NonArrayCompatibleIFaceToArray()
        {
            Assert.Throws<InvalidOperationException>(
                () => Expression.Convert(Expression.Default(typeof(IEquatable<NonSealed>[])), typeof(NonSealed[][])));
            Assert.Throws<InvalidOperationException>(
                () => Expression.Convert(Expression.Default(typeof(IDictionary<NonSealed, NonSealed>[])), typeof(NonSealed[][])));
        }

        [Fact]
        public static void ArrayToNotRelated()
        {
            Assert.Throws<InvalidOperationException>(
                () => Expression.Convert(Expression.Default(typeof(NonSealed[][][])), typeof(string[][])));
        }
    }
}
