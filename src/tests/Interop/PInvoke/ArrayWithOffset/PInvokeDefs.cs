// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Runtime.InteropServices;

unsafe class ArrayWithOffsetNative
{
    [DllImport(nameof(ArrayWithOffsetNative))]
    public static extern bool Marshal_InOut(int* expected, [In, Out] ArrayWithOffset actual, int numElements, int* newValue);

    [DllImport(nameof(ArrayWithOffsetNative))]
    public static extern bool Marshal_Invalid(ArrayWithOffset invalidArray);

    [DllImport(nameof(ArrayWithOffsetNative))]
    public static extern bool Marshal_Invalid(ref ArrayWithOffset array);

    [DllImport(nameof(ArrayWithOffsetNative))]
    public static extern ArrayWithOffset Marshal_Invalid_Return();
}
