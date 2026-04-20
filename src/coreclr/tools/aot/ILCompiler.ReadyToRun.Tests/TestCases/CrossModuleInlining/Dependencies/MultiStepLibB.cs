// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

public static class MultiStepLibB
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetCompositeValue() => MultiStepLibA.GetValue() + 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetCompositeLabel() => MultiStepLibA.GetLabel() + "_B";
}
