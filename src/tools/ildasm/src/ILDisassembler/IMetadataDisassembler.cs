// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ILDisassembler;

/// <summary>
/// Where the output of an <see cref="IMetadataDisassembler"/> goes. Those of the same phase run in the order they are given.
/// </summary>
internal enum DisassemblyPhase
{
    // In the comment block at the start, in place of the vtable fixup summary. This also runs for an image without a
    // CLR header, which cannot be disassembled otherwise.
    Headers,

    // In the comment block at the start, after the headers.
    Summary,

    // Before the manifest, unless there is no IL to show or only one item is disassembled.
    BeforeManifest,

    // At the end of the output.
    End,
}

/// <summary>
/// An operation over the whole image whose output goes at a fixed place in the disassembly.
/// </summary>
internal interface IMetadataDisassembler
{
    DisassemblyPhase Phase { get; }

    void Disassemble(Dasm dasm);
}
