// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace R2RDump
{
    /// <summary>
    /// based on <a href="https://github.com/dotnet/coreclr/blob/master/src/inc/readytorun.h">src/inc/readytorun.h</a> READYTORUN_HEADER
    /// </summary>
    public class R2RHeader
    {
        [Flags]
        public enum ReadyToRunFlag
        {
            NONE = 0x00000000,
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
        public int RelativeVirtualAddress { get; set; }

        /// <summary>
        /// Size of the ReadyToRun header
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        /// Signature of the header in string and hex formats
        /// </summary>
        public string SignatureString { get; set; }
        public uint Signature { get; set; }

        /// <summary>
        /// The ReadyToRun version
        /// </summary>
        public ushort MajorVersion { get; set; }
        public ushort MinorVersion { get; set; }

        /// <summary>
        /// Flags in the header
        /// eg. PLATFORM_NEUTRAL_SOURCE, SKIP_TYPE_VALIDATION
        /// </summary>
        public uint Flags { get; set; }

        /// <summary>
        /// The ReadyToRun section RVAs and sizes
        /// </summary>
        public IDictionary<R2RSection.SectionType, R2RSection> Sections { get; }

        public R2RHeader() { }

        /// <summary>
        /// Initializes the fields of the R2RHeader
        /// </summary>
        /// <param name="image">PE image</param>
        /// <param name="rva">Relative virtual address of the ReadyToRun header</param>
        /// <param name="curOffset">Index in the image byte array to the start of the ReadyToRun header</param>
        /// <exception cref="BadImageFormatException">The signature must be 0x00525452</exception>
        public R2RHeader(byte[] image, int rva, int curOffset)
        {
            RelativeVirtualAddress = rva;
            int startOffset = curOffset;

            byte[] signature = new byte[sizeof(uint) - 1]; // -1 removes the null character at the end of the cstring
            Array.Copy(image, curOffset, signature, 0, sizeof(uint) - 1);
            SignatureString = Encoding.UTF8.GetString(signature);
            Signature = NativeReader.ReadUInt32(image, ref curOffset);
            if (Signature != READYTORUN_SIGNATURE)
            {
                throw new System.BadImageFormatException("Incorrect R2R header signature: " + SignatureString);
            }

            MajorVersion = NativeReader.ReadUInt16(image, ref curOffset);
            MinorVersion = NativeReader.ReadUInt16(image, ref curOffset);
            Flags = NativeReader.ReadUInt32(image, ref curOffset);
            int nSections = NativeReader.ReadInt32(image, ref curOffset);
            Sections = new Dictionary<R2RSection.SectionType, R2RSection>();

            for (int i = 0; i < nSections; i++)
            {
                int type = NativeReader.ReadInt32(image, ref curOffset);
                var sectionType = (R2RSection.SectionType)type;
                if (!Enum.IsDefined(typeof(R2RSection.SectionType), type))
                {
                    R2RDump.WriteWarning("Invalid ReadyToRun section type");
                }
                Sections[sectionType] = new R2RSection(sectionType,
                    NativeReader.ReadInt32(image, ref curOffset),
                    NativeReader.ReadInt32(image, ref curOffset));
            }

            Size = curOffset - startOffset;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Signature: 0x{Signature:X8} ({SignatureString})");
            sb.AppendLine($"RelativeVirtualAddress: 0x{RelativeVirtualAddress:X8}");
            if (Signature == READYTORUN_SIGNATURE)
            {
                sb.AppendLine($"Size: {Size} bytes");
                sb.AppendLine($"MajorVersion: 0x{MajorVersion:X4}");
                sb.AppendLine($"MinorVersion: 0x{MinorVersion:X4}");
                sb.AppendLine($"Flags: 0x{Flags:X8}");
                foreach (ReadyToRunFlag flag in Enum.GetValues(typeof(ReadyToRunFlag)))
                {
                    if ((Flags & (uint)flag) != 0)
                    {
                        sb.AppendLine($"  - {Enum.GetName(typeof(ReadyToRunFlag), flag)}");
                    }
                }
            }
            return sb.ToString();
        }
    }
}
