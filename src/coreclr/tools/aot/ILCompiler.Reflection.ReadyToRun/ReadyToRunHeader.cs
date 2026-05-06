// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

using Internal.ReadyToRunConstants;
using Internal.Runtime;

namespace ILCompiler.Reflection.ReadyToRun
{
    /// <summary>
    /// Structure representing an element of the assembly table in composite R2R images.
    /// </summary>
    public class ComponentAssembly
    {
        public const int Size = 4 * sizeof(int);

        public readonly int CorHeaderRVA;
        public readonly int CorHeaderSize;
        public readonly int AssemblyHeaderRVA;
        public readonly int AssemblyHeaderSize;

        public ComponentAssembly(NativeReader imageReader, ref int curOffset)
        {
            CorHeaderRVA = imageReader.ReadInt32(ref curOffset);
            CorHeaderSize = imageReader.ReadInt32(ref curOffset);
            AssemblyHeaderRVA = imageReader.ReadInt32(ref curOffset);
            AssemblyHeaderSize = imageReader.ReadInt32(ref curOffset);
        }
    }

    /// <summary>
    /// Fields common to the global R2R header and per assembly headers in composite R2R images.
    /// </summary>
    public class ReadyToRunCoreHeader
    {
        /// <summary>
        /// Flags in the header
        /// eg. PLATFORM_NEUTRAL_SOURCE, SKIP_TYPE_VALIDATION
        /// </summary>
        public uint Flags { get; set; }

        /// <summary>
        /// The ReadyToRun section RVAs and sizes
        /// </summary>
        public IDictionary<ReadyToRunSectionType, ReadyToRunSection> Sections { get; private set; }

        public ReadyToRunCoreHeader()
        {
        }

        public ReadyToRunCoreHeader(NativeReader imageReader, ref int curOffset)
        {
            ParseCoreHeader(imageReader, ref curOffset);
        }

        /// <summary>
        /// Parse core header fields common to global R2R file header and per assembly headers in composite R2R images.
        /// </summary>
        /// <param name="imageReader">PE Image reader</param>
        /// <param name="curOffset">Index in the image byte array to the start of the ReadyToRun core header</param>
        public void ParseCoreHeader(NativeReader imageReader, ref int curOffset)
        {
            Flags = imageReader.ReadUInt32(ref curOffset);
            int nSections = imageReader.ReadInt32(ref curOffset);
            Sections = new Dictionary<ReadyToRunSectionType, ReadyToRunSection>();

            for (int i = 0; i < nSections; i++)
            {
                int type = imageReader.ReadInt32(ref curOffset);
                var sectionType = (ReadyToRunSectionType)type;
                if (!Enum.IsDefined(typeof(ReadyToRunSectionType), type))
                {
                    throw new BadImageFormatException("Warning: Invalid ReadyToRun section type");
                }
                int sectionStartRva = imageReader.ReadInt32(ref curOffset);
                int sectionLength = imageReader.ReadInt32(ref curOffset);
                Sections[sectionType] = new ReadyToRunSection(sectionType, sectionStartRva, sectionLength);
            }
        }
    }


    /// <summary>
    /// based on <a href="https://github.com/dotnet/runtime/blob/main/src/coreclr/inc/readytorun.h">src/inc/readytorun.h</a> READYTORUN_HEADER
    /// </summary>
    public class ReadyToRunHeader : ReadyToRunCoreHeader
    {
        /// <summary>
        /// The expected signature of a ReadyToRun header
        /// </summary>
        public const uint READYTORUN_SIGNATURE = 0x00525452; // 'RTR'

        /// <summary>
        /// RVA to the beginning of the ReadyToRun header
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

        public ReadyToRunHeader() { }

        /// <summary>
        /// Initializes the fields of the R2RHeader
        /// </summary>
        /// <param name="imageReader">PE Image reader</param>
        /// <param name="rva">Relative virtual address of the ReadyToRun header</param>
        /// <param name="curOffset">Index in the image byte array to the start of the ReadyToRun header</param>
        /// <exception cref="BadImageFormatException">The signature must be 0x00525452</exception>
        public ReadyToRunHeader(NativeReader imageReader, int rva, int curOffset)
        {
            RelativeVirtualAddress = rva;
            int startOffset = curOffset;

            byte[] signature = new byte[sizeof(uint) - 1]; // -1 removes the null character at the end of the cstring
            imageReader.ReadSpanAt(ref curOffset, signature);
            curOffset = startOffset;
            SignatureString = Encoding.UTF8.GetString(signature);
            Signature = imageReader.ReadUInt32(ref curOffset);
            if (Signature != READYTORUN_SIGNATURE)
            {
                throw new System.BadImageFormatException("Incorrect R2R header signature: " + SignatureString);
            }

            MajorVersion = imageReader.ReadUInt16(ref curOffset);
            MinorVersion = imageReader.ReadUInt16(ref curOffset);

            ParseCoreHeader(imageReader, ref curOffset);

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
                foreach (ReadyToRunFlags flag in Enum.GetValues(typeof(ReadyToRunFlags)))
                {
                    if ((Flags & (uint)flag) != 0)
                    {
                        sb.AppendLine($"  - {Enum.GetName(typeof(ReadyToRunFlags), flag)}");
                    }
                }
            }
            return sb.ToString();
        }
    }
}
