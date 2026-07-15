// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

/// <summary>
/// A canonical ITypeHandle backed by a real target-process address
/// (MethodTable* or TypeDesc*).
/// </summary>
internal sealed class TargetTypeHandle : ITypeHandle
{
    internal TargetTypeHandle(TargetPointer address)
    {
        Address = address;
    }

    public TargetPointer Address { get; }
    public bool IsNull => Address == TargetPointer.Null;
}
