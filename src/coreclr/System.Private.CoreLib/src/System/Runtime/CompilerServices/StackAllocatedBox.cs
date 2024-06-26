// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.CompilerServices;

#pragma warning disable CA1823
#pragma warning disable CS0169

internal unsafe struct StackAllocatedBox<T>
{
    // These fields are only accessed from jitted code
    private MethodTable* _pMethodTable;
    private T _value;
}

#pragma warning restore CS0169
#pragma warning restore CA1823
