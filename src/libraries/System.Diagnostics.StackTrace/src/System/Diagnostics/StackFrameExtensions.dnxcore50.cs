// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace System.Diagnostics
{
    public static partial class StackFrameExtensions
    {
        public static IntPtr GetNativeIP(this StackFrame stackFrame)
        {
            return IntPtr.Zero;
        }

        public static IntPtr GetNativeImageBase(this StackFrame stackFrame)
        {
            return IntPtr.Zero;
        }
    }
}
