// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace System
{
    // ByReference is meant to be used to represent a tracked reference in cases where C#
    // proves difficult. See use in Reflection.
    [NonVersionable]
    internal readonly ref struct ByReference
    {
        public readonly ref byte Value;
        public ByReference(ref byte value) => Value = ref value;

        public static ByReference Create<T>(ref T p) => new ByReference(ref Unsafe.As<T, byte>(ref p));
    }
}
