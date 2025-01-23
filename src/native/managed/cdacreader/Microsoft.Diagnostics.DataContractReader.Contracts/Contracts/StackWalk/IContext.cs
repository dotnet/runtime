// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

public interface IContext
{
    public static uint Size { get; }
    public static uint DefaultContextFlags { get; }

    public TargetPointer StackPointer { get; }
    public TargetPointer InstructionPointer { get; }
}
