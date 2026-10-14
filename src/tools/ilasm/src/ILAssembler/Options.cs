// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.PortableExecutable;

namespace ILAssembler
{
    /// <summary>
    /// Debug mode for the assembler, controlling JIT optimization and sequence points.
    /// </summary>
    public enum DebugMode
    {
        /// <summary>
        /// Implicit sequence points: JIT optimization is disabled, and the JIT uses implicit sequence points
        /// rather than those in the PDB. Edit and Continue is not enabled.
        /// Produces DebuggingModes = Default | IgnoreSymbolStoreSequencePoints | DisableOptimizations (0x103).
        /// </summary>
        Impl,

        /// <summary>
        /// Optimized debugging - enables JIT optimization while preserving debug info; the JIT uses implicit
        /// sequence points rather than those in the PDB.
        /// Produces DebuggingModes = Default | IgnoreSymbolStoreSequencePoints (0x03).
        /// </summary>
        Opt
    }

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
        /// Target machine type (x64, arm64).
        /// </summary>
        public Machine? Machine { get; set; }

        /// <summary>
        /// Produce a DLL image instead of an executable.
        /// </summary>
        public bool Dll { get; set; }

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
        /// <remarks>
        /// The same input and options give the same image and PDB bytes. The image records
        /// <see cref="PdbFilePath"/> in its CodeView entry, or, when that is null, the fallback it describes
        /// (<see cref="OutputFileName"/> with its extension replaced by <c>.pdb</c>, or <c>assembly.pdb</c>), so it
        /// depends on that path;
        /// the PDB does not.
        /// </remarks>
        public bool Deterministic { get; set; }

        /// <summary>
        /// Metadata version string.
        /// </summary>
        public string? MetadataVersion { get; set; }

        /// <summary>
        /// Enable debug mode: produce a Portable PDB and add a <c>DebuggableAttribute</c> to the assembly
        /// that disables JIT optimization (see <see cref="DebugMode"/> for the other settings).
        /// </summary>
        /// <remarks>
        /// The <c>DebuggableAttribute</c> is added only when the source declares an assembly (<c>.assembly</c>);
        /// a module without one gets no attribute.
        /// The PDB is returned in <see cref="CompilationResult.PortablePdb"/>, not embedded in the image.
        /// The image references it through its debug directory; see <see cref="PdbFilePath"/>.
        /// </remarks>
        public bool Debug { get; set; }

        /// <summary>
        /// Produce a Portable PDB without enabling debug info tracking: this option does not itself add a
        /// <c>DebuggableAttribute</c>, so on its own it leaves the JIT settings of the assembly unchanged.
        /// </summary>
        /// <remarks>
        /// Combined with <see cref="Debug"/> or <see cref="DebugMode"/>, the attribute those options add is
        /// still added. The PDB is returned in <see cref="CompilationResult.PortablePdb"/>, not embedded in
        /// the image.
        /// </remarks>
        public bool Pdb { get; set; }

        /// <summary>
        /// Debug mode, as native ilasm's <c>/DEBUG=IMPL</c> and <c>/DEBUG=OPT</c>: selects the modes of the
        /// <c>DebuggableAttribute</c>.
        /// When null with Debug=true, uses default (DisableOptimizations).
        /// </summary>
        /// <remarks>
        /// A non-null value implies <see cref="Debug"/>: it produces a Portable PDB and adds a
        /// <c>DebuggableAttribute</c> with the selected debugging modes, likewise only when the source
        /// declares an assembly.
        /// </remarks>
        public DebugMode? DebugMode { get; set; }

        /// <summary>
        /// The path of the Portable PDB file, recorded in the image's CodeView debug directory entry
        /// when a PDB is produced (see <see cref="CompilationResult.PortablePdb"/>).
        /// </summary>
        /// <remarks>
        /// The CodeView entry records this value as given. The assembler does not write the PDB file; the
        /// caller writes <see cref="CompilationResult.PortablePdb"/> to the file this path names, where a
        /// file name alone names a file beside the image. The command-line tool writes the PDB to the output
        /// path with its extension replaced by <c>.pdb</c>, and passes the full path of that file, or, with
        /// <see cref="Deterministic"/>, only its file name and extension, so that a deterministic image does
        /// not depend on the directory it is written to. When null, the CodeView entry names
        /// <see cref="OutputFileName"/> with its extension replaced by <c>.pdb</c>, or <c>assembly.pdb</c>
        /// when no output file name is set.
        /// </remarks>
        public string? PdbFilePath { get; set; }

        /// <summary>
        /// Override the name of the compiled assembly.
        /// </summary>
        public string? AssemblyName { get; set; }

        /// <summary>
        /// Path to key file for strong name signing.
        /// </summary>
        public string? KeyFile { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether instruction encodings are optimized.
        /// </summary>
        /// <remarks>The default is <see langword="false"/>, preserving the instruction forms in the source.</remarks>
        public bool Optimize { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether recognized pseudo custom attributes
        /// are lowered into metadata flags and auxiliary tables.
        /// </summary>
        /// <value><see langword="true" /> to enable lowering; otherwise, <see langword="false" />.
        /// The default is <see langword="false" />.</value>
        public bool PseudoAttributes { get; set; }

        /// <summary>
        /// Fold identical method bodies into one.
        /// </summary>
        public bool Fold { get; set; }

        /// <summary>
        /// Output file name (filename only, no directory). Used as default module name when no .module directive is present.
        /// </summary>
        public string? OutputFileName { get; set; }

        /// <summary>
        /// Try to create output file despite errors (results may be invalid).
        /// </summary>
        public bool ErrorTolerant { get; set; }
    }
}
