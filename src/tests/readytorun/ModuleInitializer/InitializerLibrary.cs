// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace ModuleInitializerTest;

public static class InitializerLibrary
{
    [ModuleInitializer]
    public static void Initialize()
    {
        GetIsSet(null) = true;
    }

    [UnsafeAccessor(UnsafeAccessorKind.StaticField, Name = "s_isSet")]
    private static extern ref bool GetIsSet(FieldHolder holder);

    // Inlining this empty call must not remove the module's initialization dependency.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Touch()
    {
    }
}
