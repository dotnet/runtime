// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public enum CodePointerFlags : byte
{
    HasArm32ThumbBit = 0x1,
    HasArm64PtrAuth = 0x2,
}

public interface IPlatformMetadata : IContract
{
    static string IContract.Name { get; } = nameof(PlatformMetadata);
    TargetPointer GetPrecodeMachineDescriptor() => throw new NotImplementedException();
    CodePointerFlags GetCodePointerFlags() => throw new NotImplementedException();
}

public readonly struct PlatformMetadata : IPlatformMetadata
{

}
