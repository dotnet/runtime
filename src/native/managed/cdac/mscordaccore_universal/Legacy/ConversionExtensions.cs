// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

internal static class ConversionExtensions
{
    private const uint Arm32ThumbBit = 1;

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
    /// When overrideCheck is true, this will not check the range and will allow any address. This is used on legacy endpoints which
    /// may pass in invalid ClrDataAddress values.
    /// </summary>
    public static TargetPointer ToTargetPointer(this ClrDataAddress address, Target target, bool overrideCheck = false)
    {
        if (target.PointerSize == sizeof(ulong))
        {
            return new TargetPointer(address);
        }
        else
        {
            long signedAddr = (long)address.Value;
            if (!overrideCheck && (signedAddr > int.MaxValue || signedAddr < int.MinValue))
            {
                throw new ArgumentException($"ClrDataAddress 0x{address.Value:x} out of range for the target platform.", nameof(address));
            }
            return new TargetPointer((uint)address);
        }
    }

    /// <summary>
    /// Converts a ClrDataAddress to a TargetCodePointer, ensuring the address is within the valid range for the target platform.
    /// </summary>
    public static TargetCodePointer ToTargetCodePointer(this ClrDataAddress address, Target target)
    {
        if (target.PointerSize == sizeof(ulong))
        {
            return new TargetCodePointer(address);
        }
        else
        {
            long signedAddr = (long)address.Value;
            if (signedAddr > int.MaxValue || signedAddr < int.MinValue)
            {
                throw new ArgumentException($"ClrDataAddress 0x{address.Value:x} out of range for the target platform.", nameof(address));
            }
            return new TargetCodePointer((uint)address);
        }
    }

    /// <summary>
    /// Converts a TargetCodePointer to an address TargetPointer, removing any platform-specific bits such as the ARM32 Thumb bit or ARM64 pointer authentication.
    /// </summary>
    internal static TargetPointer ToAddress(this TargetCodePointer code, Target target)
    {
        IPlatformMetadata metadata = target.Contracts.PlatformMetadata;
        CodePointerFlags flags = metadata.GetCodePointerFlags();
        if (flags.HasFlag(CodePointerFlags.HasArm32ThumbBit))
        {
            return new TargetPointer(code.Value & ~Arm32ThumbBit);
        }
        else if (flags.HasFlag(CodePointerFlags.HasArm64PtrAuth))
        {
            throw new NotImplementedException($"{nameof(ToAddress)}: ARM64 with pointer authentication");
        }
        Debug.Assert(flags == default);
        return new TargetPointer(code.Value);
    }
}
