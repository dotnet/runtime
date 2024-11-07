// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader;

internal static class CodePointerUtils
{
    internal static TargetCodePointer CodePointerFromAddress(TargetPointer address, Target target)
    {
        IPlatformMetadata metadata = target.Contracts.PlatformMetadata;
        CodePointerFlags flags = metadata.GetCodePointerFlags();
        if (flags.HasFlag(CodePointerFlags.HasArm32ThumbBit))
        {
            return new TargetCodePointer(address.Value | 1);
        }
        else if (flags.HasFlag(CodePointerFlags.HasArm64PtrAuth))
        {
            throw new NotImplementedException($"{nameof(CodePointerFromAddress)}: ARM64 with pointer authentication");
        }
        Debug.Assert(flags == default);
        return new TargetCodePointer(address.Value);
    }
}
