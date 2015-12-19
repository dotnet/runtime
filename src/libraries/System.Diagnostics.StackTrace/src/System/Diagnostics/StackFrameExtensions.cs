// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace System.Diagnostics
{
    public static partial class StackFrameExtensions
    {
        public static bool HasNativeImage(this StackFrame stackFrame)
        {
            return stackFrame.GetNativeImageBase() != IntPtr.Zero;
        }

        public static bool HasMethod(this StackFrame stackFrame)
        {
            return stackFrame.GetMethod() != null;
        }

        public static bool HasILOffset(this StackFrame stackFrame)
        {
            return stackFrame.GetILOffset() != StackFrame.OFFSET_UNKNOWN;
        }

        public static bool HasSource(this StackFrame stackFrame)
        {
            return stackFrame.GetFileName() != null;
        }
    }
}
