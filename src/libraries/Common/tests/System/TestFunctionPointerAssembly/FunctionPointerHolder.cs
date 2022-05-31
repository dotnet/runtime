// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Threading;

public unsafe class FunctionPointerHolderSeparateModule
{
    public delegate* managed<int> Field_Int;
    public delegate* managed<DateOnly> Field_DateOnly; // Verify non-primitive which will have its own Rid
    public delegate* managed<int> Prop_Int { get; }
    public delegate* managed<DateOnly> Prop_DateOnly { get; } 
    public delegate* managed<int> MethodReturnValue_Int() => default;
    public delegate* managed<DateOnly> MethodReturnValue_DateOnly() => default;
    public delegate* unmanaged<int> MethodUnmanagedReturnValue_Int() => default;
    public delegate* unmanaged<DateOnly> MethodUnmanagedReturnValue_DateOnly() => default;
}
