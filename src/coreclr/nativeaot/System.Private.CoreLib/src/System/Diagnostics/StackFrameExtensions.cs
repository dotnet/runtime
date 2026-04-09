// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime;

namespace System.Diagnostics
{
    public static class StackFrameExtensions
    {
        /// <summary>
        /// Return load address of the native image pointed to by the stack frame.
        /// </summary>
        public static IntPtr GetNativeImageBase(this StackFrame stackFrame)
        {
            return RuntimeImports.RhGetOSModuleFromPointer(stackFrame.GetNativeIPAddress());
        }

        /// <summary>
        /// Return stack frame native IP address.
        /// </summary>
        public static IntPtr GetNativeIP(this StackFrame stackFrame)
        {
            return stackFrame.GetNativeIPAddress();
        }

        /// <summary>
        /// Return true when the stack frame information can be converted to IL offset information
        /// within the MSIL method body.
        /// </summary>
        public static bool HasILOffset(this StackFrame stackFrame)
        {
            return stackFrame.GetILOffset() != StackFrame.OFFSET_UNKNOWN;
        }

        /// <summary>
        /// Return true when a MethodBase reflection info is available for the stack frame
        /// </summary>
        public static bool HasMethod(this StackFrame stackFrame)
        {
            return stackFrame.HasMethod();
        }

        /// <summary>
        /// Return true when the stack frame information corresponds to a native image
        /// </summary>
        public static bool HasNativeImage(this StackFrame stackFrame)
        {
            // In .NET Native, everything has a native image (at least today)
            return true;
        }

        /// <summary>
        /// Return true when stack frame information supports source file / line information lookup
        /// </summary>
        public static bool HasSource(this StackFrame stackFrame)
        {
            return stackFrame.GetFileName() != null;
        }
    }
}
