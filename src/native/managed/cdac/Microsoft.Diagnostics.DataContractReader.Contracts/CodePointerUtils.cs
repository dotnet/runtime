// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader;

internal static class CodePointerUtils
{
    private const uint Arm32ThumbBit = 1;

    internal static TargetCodePointer CodePointerFromAddress(TargetPointer address, Target target)
    {
        if (address == TargetPointer.Null)
        {
            return TargetCodePointer.Null;
        }

        IPlatformMetadata metadata = target.Contracts.PlatformMetadata;
        CodePointerFlags flags = metadata.GetCodePointerFlags();
        if (flags.HasFlag(CodePointerFlags.HasArm32ThumbBit))
        {
            return new TargetCodePointer(address.Value | Arm32ThumbBit);
        }
        else if (flags.HasFlag(CodePointerFlags.HasArm64PtrAuth))
        {
            throw new NotImplementedException($"{nameof(CodePointerFromAddress)}: ARM64 with pointer authentication");
        }
        Debug.Assert(flags == default);
        return new TargetCodePointer(address.Value);
    }

    internal static TargetPointer AddressFromCodePointer(TargetCodePointer code, Target target)
    {
        IPlatformMetadata metadata = target.Contracts.PlatformMetadata;
        CodePointerFlags flags = metadata.GetCodePointerFlags();
        if (flags.HasFlag(CodePointerFlags.HasArm32ThumbBit))
        {
            return new TargetPointer(code.Value & ~Arm32ThumbBit);
        }
        else if (flags.HasFlag(CodePointerFlags.HasArm64PtrAuth))
        {
            throw new NotImplementedException($"{nameof(AddressFromCodePointer)}: ARM64 with pointer authentication");
        }
        Debug.Assert(flags == default);
        return new TargetPointer(code.Value);
    }
}
