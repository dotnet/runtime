// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Threading
{
    public static class Volatile
    {
        private struct VolatileIntPtr { public volatile nint Value; }

        [Intrinsic]
        public static nint Read(ref readonly nint location) =>
            Unsafe.As<nint, VolatileIntPtr>(ref Unsafe.AsRef(in location)).Value;

        [Intrinsic]
        public static void Write(ref nint location, nint value) =>
            Unsafe.As<nint, VolatileIntPtr>(ref location).Value = value;
    }
}
