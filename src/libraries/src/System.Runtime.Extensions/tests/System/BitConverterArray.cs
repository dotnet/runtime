// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Tests
{
    public class BitConverterArray : BitConverterBase
    {
        public override void ConvertFromBool(bool boolean, byte[] expected)
        {
            Assert.Equal(expected, BitConverter.GetBytes(boolean));
        }

        public override void ConvertFromShort(short num, byte[] expected)
        {
            Assert.Equal(expected, BitConverter.GetBytes(num));
        }

        public override void ConvertFromChar(char character, byte[] expected)
        {
            Assert.Equal(expected, BitConverter.GetBytes(character));
        }

        public override void ConvertFromInt(int num, byte[] expected)
        {
            Assert.Equal(expected, BitConverter.GetBytes(num));
        }

        public override void ConvertFromLong(long num, byte[] expected)
        {
            Assert.Equal(expected, BitConverter.GetBytes(num));
        }

        public override void ConvertFromUShort(ushort num, byte[] expected)
        {
            Assert.Equal(expected, BitConverter.GetBytes(num));
        }

        public override void ConvertFromUInt(uint num, byte[] expected)
        {
            Assert.Equal(expected, BitConverter.GetBytes(num));
        }

        public override void ConvertFromULong(ulong num, byte[] expected)
        {
            Assert.Equal(expected, BitConverter.GetBytes(num));
        }

        public override void ConvertFromFloat(float num, byte[] expected)
        {
            Assert.Equal(expected, BitConverter.GetBytes(num));
        }

        public override void ConvertFromDouble(double num, byte[] expected)
        {
            Assert.Equal(expected, BitConverter.GetBytes(num));
        }

        public override void ToChar(int index, char expected, byte[] byteArray)
        {
            Assert.Equal(expected, BitConverter.ToChar(byteArray, index));
        }

        public override void ToInt16(int index, short expected, byte[] byteArray)
        {
            Assert.Equal(expected, BitConverter.ToInt16(byteArray, index));
        }

        public override void ToInt32(int expected, byte[] byteArray)
        {
            Assert.Equal(expected, BitConverter.ToInt32(byteArray, 0));
        }

        public override void ToInt64(int index, long expected, byte[] byteArray)
        {
            Assert.Equal(expected, BitConverter.ToInt64(byteArray, index));
        }

        public override void ToUInt16(int index, ushort expected, byte[] byteArray)
        {
            Assert.Equal(expected, BitConverter.ToUInt16(byteArray, index));
        }

        public override void ToUInt32(int index, uint expected, byte[] byteArray)
        {
            Assert.Equal(expected, BitConverter.ToUInt32(byteArray, index));
        }

        public override void ToUInt64(int index, ulong expected, byte[] byteArray)
        {
            Assert.Equal(expected, BitConverter.ToUInt64(byteArray, index));
        }

        public override void ToSingle(int index, float expected, byte[] byteArray)
        {
            Assert.Equal(expected, BitConverter.ToSingle(byteArray, index));
        }

        public override void ToDouble(int index, double expected, byte[] byteArray)
        {
            Assert.Equal(expected, BitConverter.ToDouble(byteArray, index));
        }

        public override void ToBoolean(int index, bool expected, byte[] byteArray)
        {
            Assert.Equal(expected, BitConverter.ToBoolean(byteArray, index));
        }
    }
}
