// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices.Swift
{
    public readonly struct SwiftSelf
    {
        public SwiftSelf(IntPtr value) {
            Value = value;
        }
        public IntPtr Value { get; }
    }

    public readonly struct SwiftError
    {
        public SwiftError(IntPtr value) {
            Value = value;
        }
        public IntPtr Value { get; }
    }

    public readonly struct SwiftAsyncContext
    {
        public SwiftAsyncContext(IntPtr value) {
            Value = value;
        }
        public IntPtr Value { get; }
    }
}
