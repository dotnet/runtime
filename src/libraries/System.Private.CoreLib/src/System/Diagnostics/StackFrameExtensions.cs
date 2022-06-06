// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

namespace System.Diagnostics
{
    public static partial class StackFrameExtensions
    {
        public static bool HasNativeImage(this StackFrame stackFrame)
        {
            return stackFrame.GetNativeImageBase() != 0;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "StackFrame.GetMethod is used to establish if method is available.")]
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

        public static nint GetNativeIP(this StackFrame stackFrame)
        {
            return 0;
        }

        public static nint GetNativeImageBase(this StackFrame stackFrame)
        {
            return 0;
        }
    }
}
