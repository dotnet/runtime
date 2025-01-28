// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

internal interface IContext
{
    public static abstract uint Size { get; }
    public static abstract uint DefaultContextFlags { get; }

    public TargetPointer StackPointer { get; set; }
    public TargetPointer InstructionPointer { get; set; }
    public TargetPointer FramePointer { get; set; }

    public abstract void Clear();
    public abstract void ReadFromAddress(Target target, TargetPointer address);
    public abstract void Unwind(Target target);

    public static IContext GetContextForPlatform(Target target)
    {
        target.GetPlatform(out Target.CorDebugPlatform platform);
        switch (platform)
        {
            case Target.CorDebugPlatform.CORDB_PLATFORM_WINDOWS_AMD64:
            case Target.CorDebugPlatform.CORDB_PLATFORM_POSIX_AMD64:
            case Target.CorDebugPlatform.CORDB_PLATFORM_MAC_AMD64:
                AMD64Context amd64Context = default;
                return amd64Context;
            case Target.CorDebugPlatform.CORDB_PLATFORM_POSIX_ARM64:
            case Target.CorDebugPlatform.CORDB_PLATFORM_WINDOWS_ARM64:
                ARM64Context arm64Context = default;
                return arm64Context;
            default:
                throw new ArgumentOutOfRangeException(nameof(platform), platform, null);
        }
    }
}
