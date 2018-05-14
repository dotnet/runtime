// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;

namespace R2RDump
{
    class R2RHeader
    {
        public enum ReadyToRunFlag
        {
            READYTORUN_FLAG_PLATFORM_NEUTRAL_SOURCE = 0x00000001,
            READYTORUN_FLAG_SKIP_TYPE_VALIDATION = 0x00000002
        }

        /// <summary>
        /// The expected signature of a ReadyToRun header
        /// </summary>
        public const uint READYTORUN_SIGNATURE = 0x00525452; // 'RTR'

        /// <summary>
        /// RVA to the begining of the ReadyToRun header
        /// </summary>
        public uint RelativeVirtualAddress { get; }

        /// <summary>
        /// Index in the image byte array to the start of the ReadyToRun header
        /// </summary>
        public int Offset { get; }

        /// <summary>
        /// Size of the ReadyToRun header
        /// </summary>
        public int Size { get; }

        /// <summary>
        /// Signature of the header in string and hex formats
        /// </summary>
        public string SignatureString { get; }
        public uint Signature { get; }

        /// <summary>
        /// The ReadyToRun version
        /// </summary>
        public ushort MajorVersion { get; }
        public ushort MinorVersion { get; }

        /// <summary>
        /// Flags in the header
        /// eg. PLATFORM_NEUTRAL_SOURCE, SKIP_TYPE_VALIDATION
        /// </summary>
        public uint Flags { get; }

        /// <summary>
        /// The ReadyToRun sections
        /// </summary>
        public uint NumberOfSections { get; }
        public R2RSection[] Sections { get; }

        /// <summary>
        /// Initializes the fields of the R2RHeader
        /// </summary>
        /// <param name="image">PE image</param>
        /// <param name="rva">Relative virtual address of the ReadyToRun header</param>
        /// <param name="curOffset">Index in the image byte array to the start of the ReadyToRun header</param>
        /// <exception cref="BadImageFormatException">The signature must be 0x00525452</exception>
        public R2RHeader(byte[] image, uint rva, int curOffset)
        {
            RelativeVirtualAddress = rva;
            Offset = curOffset;

            byte[] signature = new byte[sizeof(uint)];
            Array.Copy(image, curOffset, signature, 0, sizeof(uint));
            SignatureString = System.Text.Encoding.UTF8.GetString(signature);
            Signature = (uint)GetField(image, ref curOffset, sizeof(uint));
            if (Signature != READYTORUN_SIGNATURE)
            {
                throw new System.BadImageFormatException("Incorrect R2R header signature");
            }

            MajorVersion = (ushort)GetField(image, ref curOffset, sizeof(ushort));
            MinorVersion = (ushort)GetField(image, ref curOffset, sizeof(ushort));
            Flags = (uint)GetField(image, ref curOffset, sizeof(uint));
            NumberOfSections = (uint)GetField(image, ref curOffset, sizeof(uint));

            Sections = new R2RSection[NumberOfSections];
            for (int i = 0; i < NumberOfSections; i++)
            {
                Sections[i] = new R2RSection((int)GetField(image, ref curOffset, sizeof(int)),
                    (uint)GetField(image, ref curOffset, sizeof(uint)),
                    (uint)GetField(image, ref curOffset, sizeof(uint)));
            }

            Size = curOffset - Offset;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat($"Signature: 0x{Signature:X8} ({SignatureString})\n");
            sb.AppendFormat($"RelativeVirtualAddress: 0x{RelativeVirtualAddress:X8}\n");
            if (Signature == READYTORUN_SIGNATURE)
            {
                sb.AppendFormat($"Size: {Size} bytes\n");
                sb.AppendFormat($"MajorVersion: 0x{MajorVersion:X4}\n");
                sb.AppendFormat($"MinorVersion: 0x{MinorVersion:X4}\n");
                sb.AppendFormat($"Flags: 0x{Flags:X8}\n");
                foreach (ReadyToRunFlag flag in Enum.GetValues(typeof(ReadyToRunFlag)))
                {
                    if ((Flags & (uint)flag) != 0)
                    {
                        sb.AppendFormat($"  - {Enum.GetName(typeof(ReadyToRunFlag), flag)}\n");
                    }
                }
                sb.AppendFormat($"NumberOfSections: {NumberOfSections}\n");
            }
            return sb.ToString();
        }

        /// <summary>
        /// Extracts a value from the image byte array
        /// </summary>
        /// <param name="image">PE image</param>
        /// <param name="start">Starting index of the value</param>
        /// <param name="size">Size of the value in bytes</param>
        /// <exception cref="ArgumentException"><paramref name="size"/> is not 8, 4 or 2</exception>
        public long GetField(byte[] image, int start, int size)
        {
            return GetField(image, ref start, size);
        }

        /// <remarks>
        /// The <paramref name="start"/> gets incremented to the end of the value
        /// </remarks>
        public long GetField(byte[] image, ref int start, int size)
        {
            byte[] bytes = new byte[size];
            Array.Copy(image, start, bytes, 0, size);
            start += size;

            if (size == 8)
            {
                return BitConverter.ToInt64(bytes, 0);
            }
            else if (size == 4)
            {
                return BitConverter.ToInt32(bytes, 0);
            }
            else if (size == 2)
            {
                return BitConverter.ToInt16(bytes, 0);
            }
            throw new System.ArgumentException("Invalid field size");
        }
    }
}
