// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

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
            return $"            [{Offset:04X}-{Offset+Size:04X})\n";
        }
    }

    public List<NoGcRegion> Regions { get; set; }

    public NoGcRegionTable(Target target, InfoHdr header, ref TargetPointer offset)
    {
        Regions = new List<NoGcRegion>((int)header.NoGCRegionCount);

        uint count = header.NoGCRegionCount;
        while (count-- > 0)
        {
            uint regionOffset = target.GCDecodeUnsigned(ref offset);
            uint regionSize = target.GCDecodeUnsigned(ref offset);
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
