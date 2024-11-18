// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.NET.HostModel.MachO;

/// <summary>
/// See https://github.com/apple-oss-distributions/cctools/blob/7a5450708479bbff61527d5e0c32a3f7b7e4c1d0/include/mach-o/loader.h#L282 for reference.
/// </summary>
internal enum MachLoadCommandType : uint
{
    Segment = 0x1,
    SymbolTable = 0x2,
    Segment64 = 0x19,
    CodeSignature = 0x1d,
}
