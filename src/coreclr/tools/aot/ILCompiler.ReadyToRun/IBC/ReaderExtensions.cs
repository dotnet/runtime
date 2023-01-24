// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

namespace ILCompiler.IBC
{
    public static class ReaderExtensions
    {
        public static Guid ReadGuid(this BinaryReader reader)
        {
            return new Guid(reader.ReadBytes(16));
        }

        // Strings in the scenario section of IBC data are obfuscated to frustrate
        // a casual observer. Encoding is not encryption!
        public static string ReadEncodedString(this BinaryReader reader, int length)
        {
            char[] characters = new char[length];
            for (int i = 0; i < length; ++i)
            {
                characters[i] = (char)reader.ReadInt16();
            }

            // The last character (encoded or not) is '\0'.

            if ((characters[0] & 0x8000) != 0)
            {
                characters[0] &= (char)0x7FFF;

                char previous = '\0';
                for (int i = 0; i < characters.Length - 1; ++i)
                {
                    characters[i] = (char)((characters[i] - ' ') ^ previous);
                    previous = characters[i];
                }
            }

            return new string(characters, 0, length - 1);
        }

        // BinaryReader.Read7BitEncodedInt is protected internal, so its
        // implementation is duplicated here.
        public static int Read7BitEncodedInt(this BinaryReader reader)
        {
            int result = 0;
            int shift = 0;
            byte current;
            do
            {
                current = reader.ReadByte();

                result |= (current & 0x7f) << shift;

                shift += 7;
            }
            while ((shift <= 28) && ((current & 0x80) != 0));

            if ((current & 0x80) != 0)
            {
                throw new InvalidDataException("Improperly encoded integer");
            }

            return result;
        }

        public static uint Read7BitEncodedUInt(this BinaryReader reader)
        {
            return (uint)reader.Read7BitEncodedInt();
        }
    }

    public static class WriterExtensions
    {
        public static void Write(this BinaryWriter writer, Guid guid)
        {
            writer.Write(guid.ToByteArray());
        }

        // Strings in the scenario section of IBC data are obfuscated to frustrate
        // a casual observer. Encoding is not encryption!
        public static void WriteEncodedString(this BinaryWriter writer, string s)
        {
            char previous = (char)0x8000;

            for (int i = 0; i < s.Length; ++i)
            {
                writer.Write((short)((s[i] ^ previous) + ' '));
                previous = s[i];
            }

            writer.Write((short)'\0');
        }

        // BinaryWriter.Write7BitEncodedInt is protected internal, so its
        // implementation is duplicated here.
        public static void Write7BitEncodedInt(this BinaryWriter writer, uint i)
        {
            while (i > 0x7f)
            {
                writer.Write((byte)(i | 0x80));
                i >>= 7;
            }

            writer.Write((byte)i);
        }

        public static void Write7BitEncodedInt(this BinaryWriter writer, int i)
        {
            writer.Write7BitEncodedInt((uint)i);
        }
    }
}
