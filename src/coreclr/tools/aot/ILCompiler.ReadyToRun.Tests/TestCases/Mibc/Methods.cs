// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

public static class ProfiledMethods
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int AfterMissing() => 1;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Control() => 2;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int NotInProfile() => 3;
}
