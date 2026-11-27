// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection.PortableExecutable;

namespace Microsoft.Diagnostics.DataContractReader.Legacy.EnumMemory;

internal sealed class PEImageCollector(CollectorTarget target)
{
    private readonly CollectorTarget _target = target;

    public void EnumerateMemoryRegions(TargetPointer imageBase, uint imageSize, bool isMapped, bool includeExportsAndResources = false)
    {
        if (!PEImageInfo.TryCreate(imageBase, imageSize, isMapped, ReadMemory, out PEImageInfo image))
            return;

        foreach (TargetSpan range in image.EnumerateMemoryRegions(includeExportsAndResources))
            _target.Emitter.Add(range.Address.Value, range.Size);

        foreach (PEImageInfo.DebugEntry entry in image.EnumerateDebugEntries(ReadMemory))
        {
            _target.Emitter.Add(entry.Data.Address.Value, entry.Data.Size);
            if (_target.DumpType == DumpType.Triage && entry.Type == DebugDirectoryEntryType.CodeView)
                Sanitizer.SanitizePdbPath(_target, _target.Emitter, entry.Data);
        }
    }

    private bool ReadMemory(ulong address, Span<byte> buffer)
    {
        _target.Target.ReadBuffer(address, buffer);
        return true;
    }
}
