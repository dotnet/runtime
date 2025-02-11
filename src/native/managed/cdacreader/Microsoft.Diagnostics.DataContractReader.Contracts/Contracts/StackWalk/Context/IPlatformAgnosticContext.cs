// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

public interface IPlatformAgnosticContext
{
    public abstract uint Size { get; }
    public abstract uint DefaultContextFlags { get; }

    public TargetPointer StackPointer { get; set; }
    public TargetPointer InstructionPointer { get; set; }
    public TargetPointer FramePointer { get; set; }

    public abstract void Clear();
    public abstract void ReadFromAddress(Target target, TargetPointer address);
    public abstract void FillFromBuffer(Span<byte> buffer);
    public abstract byte[] GetBytes();
    public abstract IPlatformAgnosticContext Clone();
    public abstract void Unwind(Target target);

    public static IPlatformAgnosticContext GetContextForPlatform(Target target)
    {
        switch (target.Platform)
        {
            case Target.CorDebugPlatform.CORDB_PLATFORM_WINDOWS_AMD64:
            case Target.CorDebugPlatform.CORDB_PLATFORM_POSIX_AMD64:
            case Target.CorDebugPlatform.CORDB_PLATFORM_MAC_AMD64:
                return new CotnextHolder<AMD64Context>();
            case Target.CorDebugPlatform.CORDB_PLATFORM_POSIX_ARM64:
            case Target.CorDebugPlatform.CORDB_PLATFORM_WINDOWS_ARM64:
                return new CotnextHolder<ARM64Context>();
            default:
                throw new InvalidOperationException($"Unsupported platform {target.Platform}");
        }
    }
}
