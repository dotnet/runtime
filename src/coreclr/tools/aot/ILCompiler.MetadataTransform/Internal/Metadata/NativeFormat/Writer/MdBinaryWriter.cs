// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.NativeFormat;
using Debug = System.Diagnostics.Debug;

namespace Internal.Metadata.NativeFormat.Writer
{
    internal static partial class MdBinaryWriter
    {
        public static void Write(this NativeWriter writer, bool value)
        {
            writer.WriteUInt8((byte)(value ? 1 : 0));
        }

        public static void Write(this NativeWriter writer, byte value)
        {
            writer.WriteUInt8(value);
        }

        public static void Write(this NativeWriter writer, ushort value)
        {
            writer.WriteUnsigned(value);
        }

        public static void Write(this NativeWriter writer, int value)
        {
            writer.WriteSigned(value);
        }

        public static void Write(this NativeWriter writer, uint value)
        {
            writer.WriteUnsigned(value);
        }

        public static void Write(this NativeWriter writer, string value)
        {
            Debug.Assert(value != null);
            writer.WriteString(value);
        }

        public static void Write(this NativeWriter writer, MetadataRecord record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.HandleType | (uint)(record.HandleOffset << 7));
            else
                writer.WriteUnsigned(0);
        }
    }
}
