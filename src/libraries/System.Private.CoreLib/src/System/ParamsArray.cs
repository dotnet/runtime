// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// These types are temporary workarounds for an inability to stackalloc object references.
// Once we're able to do `stackalloc object[n]`, these can be removed.

// Suppress warnings for unused private fields
#pragma warning disable CS0169
#pragma warning disable CA1823
#pragma warning disable IDE0051

namespace System
{
    internal struct TwoObjects
    {
        internal object? Arg0;
        private object? _arg1;

        public TwoObjects(object? arg0, object? arg1)
        {
            Arg0 = arg0;
            _arg1 = arg1;
        }
    }

    internal struct ThreeObjects
    {
        internal object? Arg0;
        private object? _arg1;
        private object? _arg2;

        public ThreeObjects(object? arg0, object? arg1, object? arg2)
        {
            Arg0 = arg0;
            _arg1 = arg1;
            _arg2 = arg2;
        }
    }
}
