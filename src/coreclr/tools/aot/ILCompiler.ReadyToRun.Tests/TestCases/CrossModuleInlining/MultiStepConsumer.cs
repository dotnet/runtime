// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

public static class MultiStepConsumer
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int GetValueFromLibA()
    {
        return MultiStepLibA.GetValue();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static string GetLabelFromLibA()
    {
        return MultiStepLibA.GetLabel();
    }
}
