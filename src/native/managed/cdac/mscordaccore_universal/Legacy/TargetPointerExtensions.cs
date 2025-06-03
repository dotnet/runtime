// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

internal static class TargetPointerExtensions
{
    // Helper to convert to ClrDataAddres based on native CLRDATA_ADDRESS.
    // These types are sign extended when converting 32-bit addresses to 64-bits.
    // For more information, see TO_CDADDR in dacimpl.h.
    public static ClrDataAddress ToClrDataAddress(this TargetPointer address, Target target)
    {
        if (target.PointerSize == sizeof(ulong))
        {
            return address.Value;
        }
        else
        {
            return (ulong)(int)address.Value;
        }
    }
}
