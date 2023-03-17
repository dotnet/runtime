// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace Unity.CoreCLRHelpers;

/// <summary>
/// These extensions are specifically for transitioning to/from our current native representation
/// These are not GC safe and will change in the future
/// </summary>
static class UnsafeExtensions
{
    public static object ToManagedRepresentation(this nint intPtr)
        => Unsafe.As<nint, object>(ref intPtr);

    public static nint ToNativeRepresentation(this object obj)
        => Unsafe.As<object, nint>(ref obj);
}
