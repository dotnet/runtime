// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection.PortableExecutable;
using System.Text;

namespace ILCompiler.Reflection.ReadyToRun.x86
{
    public class NoGcRegionTable
    {
        public class NoGcRegion
        {
            public uint Offset { get; set; }
            public uint Size { get; set; }

            public NoGcRegion(uint offset, uint size)
            {
                Offset = offset;
                Size = size;
            }

            public override string ToString()
            {
                return $"            [{Offset:04X}-{Offset + Size:04X})\n";
            }
        }

        public List<NoGcRegion> Regions { get; set; }

        public NoGcRegionTable() { }

        public NoGcRegionTable(NativeReader imageReader, InfoHdrSmall header, ref int offset)
        {
            Regions = new List<NoGcRegion>((int)header.NoGCRegionCnt);

            uint count = header.NoGCRegionCnt;
            while (count-- > 0)
            {
                uint regionOffset = imageReader.DecodeUnsignedGc(ref offset);
                uint regionSize = imageReader.DecodeUnsignedGc(ref offset);
                Regions.Add(new NoGcRegion(regionOffset, regionSize));
            }
        }

        public override string ToString()
        {
            if (Regions.Count > 0)
            {
                StringBuilder sb = new StringBuilder();

                sb.AppendLine($"        No GC regions:");
                foreach (NoGcRegion region in Regions)
                {
                    sb.Append(region.ToString());
                }

                return sb.ToString();
            }

            return string.Empty;
        }
    }
}
