// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Formats.Tar.Tests
{
    // Tests specific to V7 format.
    // The shared Write* tests (RegularFile, HardLink, SymbolicLink, Directory) live in
    // TarWriter_WriteEntry_Base. V7 does not support CharacterDevice/BlockDevice/Fifo, so it
    // derives directly from TarWriter_WriteEntry_Base instead of TarWriter_WriteEntry_Posix_Base.
    public class TarWriter_WriteEntry_V7_Tests : TarWriter_WriteEntry_Base
    {
        protected override TarEntryFormat TestFormat => TarEntryFormat.V7;
    }
}