// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Legacy.EnumMemory;

internal sealed class CollectorTarget(Target target, DumpType dumpType, MemoryRegionEmitter emitter)
{
    public Target Target { get; } = target;
    public DumpType DumpType { get; } = dumpType;
    public MemoryRegionEmitter Emitter { get; } = emitter;
    public MiniMetadataWriter MiniMetadata { get; } = new(target, emitter);

    public static implicit operator Target(CollectorTarget target) => target.Target;
}
