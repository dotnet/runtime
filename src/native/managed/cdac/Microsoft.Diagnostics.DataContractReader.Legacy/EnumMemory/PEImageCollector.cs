// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection.PortableExecutable;

namespace Microsoft.Diagnostics.DataContractReader.Legacy.EnumMemory;

internal sealed class PEImageCollector(Target target)
{
    private readonly Target _target = target;

    public void EnumerateMemoryRegions(TargetPointer imageBase, uint imageSize, bool isMapped, MemoryRegionEmitter emitter, DumpType dumpType, bool includeExportsAndResources = false)
    {
        if (!PEImageInfo.TryCreate(imageBase, imageSize, isMapped, ReadMemory, out PEImageInfo image))
            return;

        foreach (TargetSpan range in image.EnumerateMemoryRegions(includeExportsAndResources))
            emitter.Add(range.Address.Value, range.Size);

        foreach (PEImageInfo.DebugEntry entry in image.EnumerateDebugEntries(ReadMemory))
        {
            emitter.Add(entry.Data.Address.Value, entry.Data.Size);
            if (dumpType == DumpType.Triage && entry.Type == DebugDirectoryEntryType.CodeView)
                Sanitizer.SanitizePdbPath(_target, emitter, entry.Data);
        }
    }

    private bool ReadMemory(ulong address, Span<byte> buffer)
    {
        _target.ReadBuffer(address, buffer);
        return true;
    }
}
