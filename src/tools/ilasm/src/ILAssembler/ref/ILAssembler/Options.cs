// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ILAssembler
{
    public enum DebugMode
    {
        Impl,
        Opt
    }

    public sealed class Options
    {
        public bool AppContainer { get; set; }
        public string? AssemblyName { get; set; }
        public System.Reflection.PortableExecutable.CorFlags? CorFlags { get; set; }
        public bool Debug { get; set; }
        public DebugMode? DebugMode { get; set; }
        public bool Deterministic { get; set; }
        public bool Dll { get; set; }
        public bool ErrorTolerant { get; set; }
        public int? FileAlignment { get; set; }
        public bool Fold { get; set; }
        public bool HighEntropyVA { get; set; }
        public long? ImageBase { get; set; }
        public string? KeyFile { get; set; }
        public System.Reflection.PortableExecutable.Machine? Machine { get; set; }
        public string? MetadataVersion { get; set; }
        public bool NoAutoInherit { get; set; }
        public bool Optimize { get; set; }
        public string? OutputFileName { get; set; }
        public bool Pdb { get; set; }
        public bool Prefer32Bit { get; set; }
        public long? StackReserve { get; set; }
        public bool StripReloc { get; set; }
        public System.Reflection.PortableExecutable.Subsystem? Subsystem { get; set; }
        public (ushort Major, ushort Minor)? SubsystemVersion { get; set; }
    }
}
