// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Diagnostics
{
    public static partial class StackFrameExtensions
    {
        public static IntPtr GetNativeIP(this StackFrame stackFrame)
        {
            return stackFrame.GetNativeIP();
        }

        public static IntPtr GetNativeImageBase(this StackFrame stackFrame)
        {
            return stackFrame.GetNativeImageBase();
        }
    }
}
