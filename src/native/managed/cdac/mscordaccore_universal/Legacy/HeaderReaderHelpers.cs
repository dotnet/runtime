// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    public static int GetLocalVarSigToken(Target _target, TargetPointer ilHeader)
    {
        // see ECMA-335 II.25.4
        if (ilHeader == TargetPointer.Null)
            return -1;
        ushort sizeAndFlags = _target.Read<ushort>(ilHeader); // get flags and size of il header
        CorILMethodFlags flags = (CorILMethodFlags)(sizeAndFlags & (int)CorILMethodFlags.CorILMethod_FormatMask);
        if (flags != CorILMethodFlags.CorILMethod_FatFormat)
            return -1;

        int localToken = _target.Read<int>(ilHeader + 8);
        return localToken;
    }
}
