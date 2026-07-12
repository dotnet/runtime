// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.CompilerServices;

#if NETFRAMEWORK || NETSTANDARD2_0

/// <summary>Provides a downlevel polyfill for <c>RuntimeFeature</c>.</summary>
internal static class RuntimeFeature
{
    public static bool IsDynamicCodeSupported => true;
}

#endif
