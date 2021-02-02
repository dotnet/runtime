// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.ObjectiveC
{
    [SupportedOSPlatform("macos")]
    [CLSCompliant(false)]
    public static class Bridge
    {
        /// <summary>
        /// Objective-C msgSend function override options.
        /// </summary>
        /// <see href="https://developer.apple.com/documentation/objectivec/1456712-objc_msgsend"/>
        public enum MsgSendFunction
        {
            ObjCMsgSend,
            ObjCMsgSendFpret,
            ObjCMsgSendStret,
            ObjCMsgSendSuper,
            ObjCMsgSendSuperStret,
        }

        /// <summary>
        /// Set function pointer override for an Objective-C runtime message passing export.
        /// </summary>
        /// <param name="msgSendFunction">The export to override.</param>
        /// <param name="func">The function override.</param>
        /// <exception cref="InvalidOperationException">Thrown if the msgSend function has already been overridden.</exception>
        /// <remarks>
        /// Providing an override can enable support for Objective-C
        /// exception propagation and variadic argument support.
        /// </remarks>
        public static void SetMessageSendCallback(MsgSendFunction msgSendFunction, IntPtr func)
        {
            if (func == IntPtr.Zero)
                throw new ArgumentNullException(nameof(func));

            if (msgSendFunction < MsgSendFunction.ObjCMsgSend || msgSendFunction > MsgSendFunction.ObjCMsgSendSuperStret)
                throw new ArgumentOutOfRangeException(nameof(msgSendFunction));

            if (!TrySetGlobalMessageSendCallback(msgSendFunction, func))
                throw new InvalidOperationException(SR.InvalidOperation_ResetGlobalObjectiveCMsgSend);
        }

        [DllImport(RuntimeHelpers.QCall)]
        private static extern bool TrySetGlobalMessageSendCallback(
            MsgSendFunction msgSendFunction,
            IntPtr func);
    }
}
