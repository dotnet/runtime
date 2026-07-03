// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

internal enum CorILMethodFlags
{
    CorILMethod_TinyFormatMask = 0x3,
    CorILMethod_FatFormatMask = 0x7,
    CorILMethod_TinyFormat = 0x0002,
    CorILMethod_FatFormat = 0x0003,
}
internal static class HeaderReaderHelpers
{
    // See ECMA-335 II.25.4.1
    private static bool IsTiny(byte firstByte)
        => (firstByte & (int)CorILMethodFlags.CorILMethod_TinyFormatMask) == (int)CorILMethodFlags.CorILMethod_TinyFormat;

    private static bool IsFat(byte firstByte)
        => (firstByte & (int)CorILMethodFlags.CorILMethod_FatFormatMask) == (int)CorILMethodFlags.CorILMethod_FatFormat;

    public static int GetHeaderSize(Target target, TargetPointer ilHeader)
    {
        // see ECMA-335 II.25.4
        byte firstByte = target.Read<byte>(ilHeader);
        if (IsTiny(firstByte))
            return 1;
        if (IsFat(firstByte))
            return 12;
        throw new BadImageFormatException("Invalid IL method header.");
    }

    public static int GetCodeSize(Target target, TargetPointer ilHeader)
    {
        // see ECMA-335 II.25.4
        byte firstByte = target.Read<byte>(ilHeader);
        if (IsTiny(firstByte))
            return firstByte >> 2;
        if (IsFat(firstByte))
            return (int)target.Read<uint>(ilHeader + 4);
        throw new BadImageFormatException("Invalid IL method header.");
    }

    public static bool TryGetLocalVarSigToken(Target target, TargetPointer ilHeader, out int localVarSigToken)
    {
        // see ECMA-335 II.25.4
        localVarSigToken = 0;
        byte firstByte = target.Read<byte>(ilHeader);
        if (!IsFat(firstByte))
            return false;

        localVarSigToken = target.Read<int>(ilHeader + 8);
        return true;
    }
}
