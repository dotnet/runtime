// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Formats.Tar.Tests
{
    // Tests specific to Ustar format.
    // The shared Write* tests (RegularFile, HardLink, SymbolicLink, Directory, CharacterDevice,
    // BlockDevice, Fifo) live in TarWriter_WriteEntry_Base and TarWriter_WriteEntry_Posix_Base.
    public class TarWriter_WriteEntry_Ustar_Tests : TarWriter_WriteEntry_Posix_Base
    {
        protected override TarEntryFormat TestFormat => TarEntryFormat.Ustar;
    }
}