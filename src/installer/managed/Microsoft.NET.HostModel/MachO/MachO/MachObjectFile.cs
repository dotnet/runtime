// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.NET.HostModel.MachO.Streams;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.NET.HostModel.MachO
{
    public sealed class MachObjectFile
    {
        private readonly Stream stream;

        internal MachObjectFile()
            : this(Stream.Null)
        {
        }

        internal MachObjectFile(Stream stream)
        {
            this.stream = stream;
        }

        internal bool Is64Bit => CpuType.HasFlag(MachCpuType.Architecture64);

        internal bool IsLittleEndian { get; set; }

        internal MachCpuType CpuType { get; set; }

        internal uint CpuSubType { get; set; }

        internal MachFileType FileType { get; set; }

        internal MachHeaderFlags Flags { get; set; }

        internal IList<MachLoadCommand> LoadCommands { get; } = new List<MachLoadCommand>();

        /// <summary>
        /// For object files the relocation, symbol tables and other data are stored at the end of the
        /// file but not covered by any segment/section. We maintain a list of these data to make it
        /// easier to address.
        ///
        /// For linked files this points to the real __LINKEDIT segment. We slice it into subsections
        /// based on the known LinkEdit commands though.
        /// </summary>
        internal IEnumerable<MachLinkEditData> LinkEditData => LoadCommands.SelectMany(command => command.LinkEditData);

        internal IEnumerable<MachSegment> Segments => LoadCommands.OfType<MachSegment>();

        /// <summary>
        /// Get the lowest file offset of any section in the file. This allows calculating space that is
        /// reserved for adding new load commands (header pad).
        /// </summary>
        internal ulong GetLowestSectionFileOffset()
        {
            ulong lowestFileOffset = ulong.MaxValue;

            foreach (var segment in Segments)
            {
                foreach (var section in segment.Sections)
                {
                    if (section.IsInFile &&
                        section.FileOffset < lowestFileOffset)
                    {
                        lowestFileOffset = section.FileOffset;
                    }
                }
            }

            return lowestFileOffset == ulong.MaxValue ? 0 : lowestFileOffset;
        }

        internal ulong GetSize()
        {
            // Assume the size is the highest file offset+size of any segment
            return Segments.Max(s => s.FileOffset + s.FileSize);
        }

        internal ulong GetSigningLimit()
        {
            var codeSignature = LoadCommands.OfType<MachCodeSignature>().FirstOrDefault();
            if (codeSignature != null)
            {
                // If code signature is present it has to be at the end of the file
                return codeSignature.FileOffset;
            }
            else
            {
                // If no code signature is present then we return the whole file size
                return GetSize();
            }
        }

        internal Stream GetOriginalStream()
        {
            if (stream == null)
                return Stream.Null;

            return stream.Slice(0, stream.Length);
        }
    }
}
