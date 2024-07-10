// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System
{
    [InlineArray(2)]
    internal struct TwoObjects
    {
        private object? _arg0;

        public TwoObjects(object? arg0, object? arg1)
        {
            this[0] = arg0;
            this[1] = arg1;
        }
    }

    [InlineArray(3)]
    internal struct ThreeObjects
    {
        private object? _arg0;

        public ThreeObjects(object? arg0, object? arg1, object? arg2)
        {
            this[0] = arg0;
            this[1] = arg1;
            this[2] = arg2;
        }
    }

    [InlineArray(8)]
    internal struct EightObjects
    {
        private object? _ref0;
    }
}
