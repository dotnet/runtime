// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices
{
    public struct SwiftSelf
    {
        public IntPtr Value { get; set; }

        public SwiftSelf()
        {
            Value = IntPtr.Zero;
        }
    }

    public readonly struct SwiftError
    {
        public IntPtr Value { get; }

        public SwiftError()
        {
            Value = IntPtr.Zero;
        }
    }
}
