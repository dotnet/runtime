// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

internal static class ConversionExtensions
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

    /// <summary>
    /// Converts a ClrDataAddress to a TargetPointer, ensuring the address is within the valid range for the target platform.
    /// </summary>
    public static TargetPointer ToTargetPointer(this ClrDataAddress address, Target target)
    {
        if (target.PointerSize == sizeof(ulong))
        {
            return new TargetPointer(address);
        }
        else
        {
            long signedAddr = (long)address.Value;
            if (signedAddr > int.MaxValue || signedAddr < int.MinValue)
            {
                throw new ArgumentException(nameof(address), "ClrDataAddress out of range for the target platform.");
            }
            return new TargetPointer((ulong)address);
        }
    }
}
