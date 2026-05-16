// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

internal enum CorILMethodFlags
{
    CorILMethod_FormatShift = 3,
    CorILMethod_FormatMask = (1 << CorILMethod_FormatShift) - 1,
    CorILMethod_TinyFormat = 0x0002,
    CorILMethod_FatFormat = 0x0003,
}
internal static class HeaderReaderHelpers
{
    public static int GetHeaderSize(Target target, TargetPointer ilHeader)
    {
        // see ECMA-335 II.25.4
        ushort sizeAndFlags = target.Read<ushort>(ilHeader);
        CorILMethodFlags flags = (CorILMethodFlags)(sizeAndFlags & (int)CorILMethodFlags.CorILMethod_FormatMask);

        return flags switch
        {
            CorILMethodFlags.CorILMethod_TinyFormat => 1,
            CorILMethodFlags.CorILMethod_FatFormat => 12,
            _ => throw new BadImageFormatException("Invalid IL method header."),
        };
    }

    public static int GetCodeSize(Target target, TargetPointer ilHeader)
    {
        // see ECMA-335 II.25.4
        ushort sizeAndFlags = target.Read<ushort>(ilHeader);
        CorILMethodFlags flags = (CorILMethodFlags)(sizeAndFlags & (int)CorILMethodFlags.CorILMethod_FormatMask);

        return flags switch
        {
            CorILMethodFlags.CorILMethod_TinyFormat => sizeAndFlags >> 2,
            CorILMethodFlags.CorILMethod_FatFormat => (int)target.Read<uint>(ilHeader + 4),
            _ => throw new BadImageFormatException("Invalid IL method header."),
        };
    }

    public static bool TryGetLocalVarSigToken(Target target, TargetPointer ilHeader, out int localVarSigToken)
    {
        // see ECMA-335 II.25.4
        localVarSigToken = 0;
        ushort sizeAndFlags = target.Read<ushort>(ilHeader); // get flags and size of il header
        CorILMethodFlags flags = (CorILMethodFlags)(sizeAndFlags & (int)CorILMethodFlags.CorILMethod_FormatMask);
        if (flags != CorILMethodFlags.CorILMethod_FatFormat)
            return false;

        localVarSigToken = target.Read<int>(ilHeader + 8);
        return true;
    }
}
