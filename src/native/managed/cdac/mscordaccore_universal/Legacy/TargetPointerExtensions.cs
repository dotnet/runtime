// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

internal static class TargetPointerExtensions
{
    /// <summary>
    /// Converts a TargetPointer to a ClrDataAddress using sign extension if required.
    /// </summary>
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
