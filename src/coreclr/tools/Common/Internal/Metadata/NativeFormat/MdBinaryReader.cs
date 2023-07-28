// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.NativeFormat;

namespace Internal.Metadata.NativeFormat
{
    internal static partial class MdBinaryReader
    {
        public static uint Read(this NativeReader reader, uint offset, out bool value)
        {
            value = (reader.ReadUInt8(offset) != 0) ? true : false;
            return offset + 1;
        }

        public static uint Read(this NativeReader reader, uint offset, out string value)
        {
            return reader.DecodeString(offset, out value);
        }

        public static uint Read(this NativeReader reader, uint offset, out char value)
        {
            uint val;
            offset = reader.DecodeUnsigned(offset, out val);
            value = (char)val;
            return offset;
        }

        public static uint Read(this NativeReader reader, uint offset, out short value)
        {
            int val;
            offset = reader.DecodeSigned(offset, out val);
            value = (short)val;
            return offset;
        }

        public static uint Read(this NativeReader reader, uint offset, out sbyte value)
        {
            value = (sbyte)reader.ReadUInt8(offset);
            return offset + 1;
        }

        public static uint Read(this NativeReader reader, uint offset, out ulong value)
        {
            return reader.DecodeUnsignedLong(offset, out value);
        }

        public static uint Read(this NativeReader reader, uint offset, out int value)
        {
            return reader.DecodeSigned(offset, out value);
        }

        public static uint Read(this NativeReader reader, uint offset, out uint value)
        {
            return reader.DecodeUnsigned(offset, out value);
        }

        public static uint Read(this NativeReader reader, uint offset, out byte value)
        {
            value = reader.ReadUInt8(offset);
            return offset + 1;
        }

        public static uint Read(this NativeReader reader, uint offset, out ushort value)
        {
            uint val;
            offset = reader.DecodeUnsigned(offset, out val);
            value = (ushort)val;
            return offset;
        }

        public static uint Read(this NativeReader reader, uint offset, out long value)
        {
            return reader.DecodeSignedLong(offset, out value);
        }

        public static uint Read(this NativeReader reader, uint offset, out Handle handle)
        {
            uint rawValue;
            offset = reader.DecodeUnsigned(offset, out rawValue);
            handle = new Handle((HandleType)(byte)rawValue, (int)(rawValue >> 8));
            return offset;
        }

        public static uint Read(this NativeReader reader, uint offset, out float value)
        {
            value = reader.ReadFloat(offset);
            return offset + 4;
        }

        public static uint Read(this NativeReader reader, uint offset, out double value)
        {
            value = reader.ReadDouble(offset);
            return offset + 8;
        }
    }
}
