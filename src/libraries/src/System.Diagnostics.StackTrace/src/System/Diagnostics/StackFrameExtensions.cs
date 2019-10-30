// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
