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

        public static uint Read(this NativeReader reader, uint offset, out Handle handle)
        {
            uint rawValue;
            offset = reader.DecodeUnsigned(offset, out rawValue);
            handle = new Handle((HandleType)(rawValue & 0x7F), (int)(rawValue >> 7));
            return offset;
        }
    }
}
