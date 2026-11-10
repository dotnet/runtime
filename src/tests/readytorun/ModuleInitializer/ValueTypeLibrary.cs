// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

namespace ModuleInitializerTest;

public struct ValueTypeLibrary
{
    [ModuleInitializer]
    public static void Initialize()
    {
        AppContext.SetSwitch("ValueTypeLibrary.Initialized", true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetValue() => 42;
}
