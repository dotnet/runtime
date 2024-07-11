// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class TypeHandleArray : IData<TypeHandleArray, (TargetPointer ptr, int size)>
{
    public static TypeHandleArray Create(Target target, (TargetPointer ptr, int size) address)
        => new TypeHandleArray(target, address);

    public readonly TypeHandle[] Types;

    public TypeHandleArray(Target target, (TargetPointer ptr, int size) key)
    {
        Span<TargetPointer> targetPointerSpan = stackalloc TargetPointer[4];
        Types = new TypeHandle[key.size];
        Span<TypeHandle> instantiationSpan = Types.AsSpan();
        if (key.size > targetPointerSpan.Length)
        {
            targetPointerSpan = new TargetPointer[key.size];
        }

        target.ReadPointers(key.ptr, targetPointerSpan);

        for (int i = 0; i < key.size; i++)
        {
            instantiationSpan[i] = target.Contracts.RuntimeTypeSystem.TypeHandleFromAddress(targetPointerSpan[i]);
        }
    }
}
