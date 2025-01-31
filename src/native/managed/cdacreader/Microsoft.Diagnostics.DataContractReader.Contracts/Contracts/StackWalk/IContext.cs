// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

internal interface IContext
{
    public abstract uint Size { get; }
    public abstract uint DefaultContextFlags { get; }

    public TargetPointer StackPointer { get; set; }
    public TargetPointer InstructionPointer { get; set; }
    public TargetPointer FramePointer { get; set; }

    public abstract void Clear();
    public unsafe void ReadFromAddress(Target target, TargetPointer address)
    {
        Span<byte> buffer = new byte[Size];
        target.ReadBuffer(address, buffer);
        FillFromBuffer(buffer);
    }
    public abstract void FillFromBuffer(Span<byte> buffer);
    public abstract byte[] GetBytes();
    public abstract IContext Clone();
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
