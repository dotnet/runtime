﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Melanzana.MachO
{
    [GenerateReaderWriter]
    public partial class MachEncryptionInfo : MachLoadCommand
    {
        public uint CryptOffset;
        public uint CryptSize;
        public uint CryptId;
    }
}
