// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public class StackReferenceData
{
    public bool HasRegisterInformation { get; init; }
    public int Register { get; init; }
    public int Offset { get; init; }
    public TargetPointer Address { get; init; }
    public TargetPointer Object { get; init; }
    public uint Flags { get; init; }
    public bool IsStackSourceFrame { get; init; }
    public TargetPointer Source { get; init; }
    public TargetPointer StackPointer { get; init; }
}
