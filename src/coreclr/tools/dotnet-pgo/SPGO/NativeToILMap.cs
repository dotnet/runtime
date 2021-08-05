// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ILCompiler.Reflection.ReadyToRun;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;

namespace Microsoft.Diagnostics.Tools.Pgo
{
    public class NativeToILMap
    {
        // Native offsets in order
        private uint[] _nativeOffsets;
        // Map from native offset to IL offset
        private int[] _ilOffsets;

        public NativeToILMap(uint[] nativeOffsets, int[] ilOffsets)
        {
            _nativeOffsets = nativeOffsets;
            _ilOffsets = ilOffsets;
        }

        private int LookupIndex(uint rva)
        {
            int index = Array.BinarySearch(_nativeOffsets, rva);
            if (index < 0)
                index = ~index - 1;

            // If rva is before first binary search will return ~0 so index will be -1.
            if (index < 0)
                return -1;

            return index;
        }

        /// <summary>Look up IL offset associated with block that contains RVA.</summary>
        public int Lookup(uint rva)
            => LookupIndex(rva) switch
            {
                -1 => -1,
                int index => _ilOffsets[index]
            };

        public IEnumerable<int> LookupRange(uint rvaStart, uint rvaEnd)
        {
            int start = LookupIndex(rvaStart);
            if (start < 0)
                start = 0;

            int end = LookupIndex(rvaEnd);
            if (end < 0)
                yield break;

            for (int i = start; i <= end; i++)
                yield return _ilOffsets[i];
        }

        internal static NativeToILMap FromR2RBounds(List<DebugInfoBoundsEntry> boundsList)
        {
            List<DebugInfoBoundsEntry> sorted = boundsList.OrderBy(e => e.NativeOffset).ToList();

            return new NativeToILMap(sorted.Select(e => e.NativeOffset).ToArray(), sorted.Select(e => (int)e.ILOffset).ToArray());
        }

        internal static NativeToILMap FromEvent(MethodILToNativeMapTraceData ev)
        {
            List<(uint rva, int ilOffset)> pairs = new List<(uint rva, int ilOffset)>(ev.CountOfMapEntries);
            for (int i = 0; i < ev.CountOfMapEntries; i++)
                pairs.Add(((uint)ev.NativeOffset(i), ev.ILOffset(i)));

            pairs.RemoveAll(p => p.ilOffset < 0);
            pairs.Sort((p1, p2) => p1.rva.CompareTo(p2.rva));
            return new NativeToILMap(pairs.Select(p => p.rva).ToArray(), pairs.Select(p => p.ilOffset).ToArray());
        }
    }
}
