// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// These types are temporary workarounds for an inability to stackalloc object references.
// Once we're able to do `stackalloc object[n]`, these can be removed.

// Suppress warnings for unused private fields
#pragma warning disable CS0169, CA1823, IDE0051, IDE0044

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System
{
    [InlineArray(2)]
    internal struct TwoObjects
    {
        internal object? Arg0;

        public TwoObjects(object? arg0, object? arg1)
        {
            this[0] = arg0;
            this[1] = arg1;
        }
    }

    [InlineArray(3)]
    internal struct ThreeObjects
    {
        internal object? Arg0;

        public ThreeObjects(object? arg0, object? arg1, object? arg2)
        {
            this[0] = arg0;
            this[1] = arg1;
            this[2] = arg2;
        }
    }
}
