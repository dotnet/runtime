// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices.ObjectiveC;
using System.Runtime.Versioning;
using System.Threading;

namespace Internal.Runtime.CompilerHelpers
{
    internal static partial class InteropHelpers
    {
        [SupportedOSPlatform("macos")]
        private static readonly IntPtr[] s_ObjcMessageSendFunctions = new IntPtr[(int)ObjectiveCMarshal.MessageSendFunction.MsgSendSuperStret + 1];

        [SupportedOSPlatform("macos")]
        internal static bool TrySetGlobalMessageSendCallback(
            ObjectiveCMarshal.MessageSendFunction msgSendFunction,
            IntPtr func)
        {
            return Interlocked.CompareExchange(ref s_ObjcMessageSendFunctions[(int)msgSendFunction], func, IntPtr.Zero) == IntPtr.Zero;
        }

        [ThreadStatic]
        private static Exception? s_pendingExceptionObject;

        [StackTraceHidden]
        internal static void ThrowPendingExceptionObject()
        {
            Exception? ex = s_pendingExceptionObject;
            if (ex != null)
            {
                s_pendingExceptionObject = null;
                ExceptionDispatchInfo.Throw(ex);
            }
        }

        internal static void SetPendingExceptionObject(Exception? exception)
        {
            s_pendingExceptionObject = exception;
        }
    }
}
