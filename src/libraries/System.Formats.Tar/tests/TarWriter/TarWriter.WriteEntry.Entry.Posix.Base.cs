// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

namespace System.Formats.Tar.Tests
{
    // Tests shared by the formats that support Posix entry types with device/fifo support: Gnu, Pax and Ustar.
    // V7 does not support these entry types, so it derives directly from TarWriter_WriteEntry_Base instead.
    public abstract class TarWriter_WriteEntry_Posix_Base : TarWriter_WriteEntry_Base
    {
        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public Task WriteCharacterDevice(bool async) => WriteAndVerifyEntry(TarEntryType.CharacterDevice, async);

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public Task WriteBlockDevice(bool async) => WriteAndVerifyEntry(TarEntryType.BlockDevice, async);

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public Task WriteFifo(bool async) => WriteAndVerifyEntry(TarEntryType.Fifo, async);
    }
}
