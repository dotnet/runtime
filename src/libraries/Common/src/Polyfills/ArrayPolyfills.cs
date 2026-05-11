// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System;

/// <summary>Provides downlevel polyfills for static members on <see cref="Array"/>.</summary>
internal static class ArrayPolyfills
{
    extension(Array)
    {
        public static int MaxLength => 0x7FFFFFC7;
    }
}
