// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

internal class StackRefData
{
    public enum SourceTypes
    {
        StackSourceIP = 0,
        StackSourceFrame = 1,
    }

    public bool HasRegisterInformation { get; set; }
    public int Register { get; set; }
    public int Offset { get; set; }
    public TargetPointer Address { get; set; }
    public TargetPointer Object { get; set; }
    public GcScanFlags Flags { get; set; }
    public SourceTypes SourceType { get; set; }
    public TargetPointer Source { get; set; }
    public TargetPointer StackPointer { get; set; }
}
