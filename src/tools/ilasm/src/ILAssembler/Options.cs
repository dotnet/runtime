// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.PortableExecutable;

namespace ILAssembler
{
    public sealed class Options
    {
        /// <summary>
        /// Disable inheriting from System.Object by default.
        /// </summary>
        public bool NoAutoInherit { get; set; }

        /// <summary>
        /// Subsystem value in the NT Optional header (overrides .subsystem directive).
        /// </summary>
        public Subsystem? Subsystem { get; set; }

        /// <summary>
        /// Subsystem version (major.minor) in the NT Optional header.
        /// </summary>
        public (ushort Major, ushort Minor)? SubsystemVersion { get; set; }

        /// <summary>
        /// FileAlignment value in the NT Optional header (overrides .alignment directive).
        /// </summary>
        public int? FileAlignment { get; set; }

        /// <summary>
        /// ImageBase value in the NT Optional header (overrides .imagebase directive).
        /// </summary>
        public long? ImageBase { get; set; }

        /// <summary>
        /// SizeOfStackReserve value in the NT Optional header (overrides .stackreserve directive).
        /// </summary>
        public long? StackReserve { get; set; }

        /// <summary>
        /// CLR ImageFlags value in the CLR header (overrides .corflags directive).
        /// </summary>
        public CorFlags? CorFlags { get; set; }

        /// <summary>
        /// Target machine type (x64, arm, arm64).
        /// </summary>
        public Machine? Machine { get; set; }

        /// <summary>
        /// Create an AppContainer exe or dll.
        /// </summary>
        public bool AppContainer { get; set; }

        /// <summary>
        /// Set High Entropy Virtual Address capable PE32+ images.
        /// </summary>
        public bool HighEntropyVA { get; set; }

        /// <summary>
        /// Indicate that no base relocations are needed.
        /// </summary>
        public bool StripReloc { get; set; }

        /// <summary>
        /// Create a 32BitPreferred image.
        /// </summary>
        public bool Prefer32Bit { get; set; }

        /// <summary>
        /// Produce deterministic outputs.
        /// </summary>
        public bool Deterministic { get; set; }

        /// <summary>
        /// Metadata version string.
        /// </summary>
        public string? MetadataVersion { get; set; }

        /// <summary>
        /// Enable debug mode: create PDB, disable JIT optimization.
        /// </summary>
        public bool Debug { get; set; }

        /// <summary>
        /// Create PDB file without enabling debug info tracking.
        /// </summary>
        public bool Pdb { get; set; }

        /// <summary>
        /// Debug mode: "impl" for implicit sequence points, "opt" to enable JIT optimization.
        /// </summary>
        public string? DebugMode { get; set; }

        /// <summary>
        /// Override the name of the compiled assembly.
        /// </summary>
        public string? AssemblyName { get; set; }

        /// <summary>
        /// Path to key file for strong name signing.
        /// </summary>
        public string? KeyFile { get; set; }

        /// <summary>
        /// Optimize long instructions to short.
        /// </summary>
        public bool Optimize { get; set; }

        /// <summary>
        /// Fold identical method bodies into one.
        /// </summary>
        public bool Fold { get; set; }

        /// <summary>
        /// Metadata stream version (major.minor).
        /// </summary>
        public (byte Major, byte Minor)? MetadataStreamVersion { get; set; }
    }
}
