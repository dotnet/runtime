// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.NET.HostModel.MachO;

internal enum MachLoadCommandType : uint
{
    Segment = 0x1,
    SymbolTable = 0x2,
    Segment64 = 0x19,
    CodeSignature = 0x1d,
}
