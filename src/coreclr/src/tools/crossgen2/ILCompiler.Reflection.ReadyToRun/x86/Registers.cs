// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace R2RDump.x86
{
    /// <summary>
    /// based on <a href="https://github.com/dotnet/coreclr/blob/master/src/gcdump/i386/gcdumpx86.cpp">src\gcdump\i386\gcdumpx86.cpp</a> RegName
    /// </summary>
    public enum Registers
    {
        EAX = 0x00,
        ECX = 0x01,
        EDX = 0x02,
        EBX = 0x03,
        ESP = 0x04,
        EBP = 0x05,
        ESI = 0x06,
        EDI = 0x07,
    };

    /// <summary>
    /// based on <a href="https://github.com/dotnet/coreclr/blob/master/src/gcdump/i386/gcdumpx86.cpp">src\gcdump\i386\gcdumpx86.cpp</a> CalleeSavedRegName
    /// </summary>
    public enum CalleeSavedRegisters
    {
        EDI = 0x00,
        ESI = 0x01,
        EBX = 0x02,
        EBP = 0x03,
    };
}
