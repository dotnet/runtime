// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ObjectiveC;

using Microsoft.DotNet.RemoteExecutor;
using Xunit;

using static System.Runtime.InteropServices.ObjectiveC.Bridge;

namespace System.Runtime.InteropServices.Tests
{
    [PlatformSpecific(TestPlatforms.OSX)]
    [SkipOnMono("Not currently implemented on Mono")]
    public unsafe class PendingExceptionTests
    {
        private sealed class PendingException : Exception
        {
            public PendingException(string message) : base(message) { }
        }

        [UnmanagedCallersOnly]
        private static IntPtr ObjCMsgSend(IntPtr inst, IntPtr sel) => SetPendingException();

        [UnmanagedCallersOnly]
        private static IntPtr ObjCMsgSendFpret(IntPtr inst, IntPtr sel) => SetPendingException();

        [UnmanagedCallersOnly]
        private static void ObjCMsgSendStret(IntPtr* ret, IntPtr inst, IntPtr sel) => *ret = SetPendingException();

        [UnmanagedCallersOnly]
        private static IntPtr ObjCMsgSendSuper(IntPtr inst, IntPtr sel) => SetPendingException();

        [UnmanagedCallersOnly]
        private static void ObjCMsgSendSuperStret(IntPtr* ret, IntPtr inst, IntPtr sel) => *ret = SetPendingException();

        private static IntPtr SetPendingException([CallerMemberName] string callerName = "")
        {
            Bridge.SetMessageSendPendingExceptionForThread(new PendingException(callerName));
            return IntPtr.Zero;
        }

        private static (MsgSendFunction MsgSend, IntPtr Func)[] msgSendOverrides =
        {
            (MsgSendFunction.ObjCMsgSend,           (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, IntPtr>)&ObjCMsgSend),
            (MsgSendFunction.ObjCMsgSendFpret,      (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, IntPtr>)&ObjCMsgSendFpret),
            (MsgSendFunction.ObjCMsgSendStret,      (IntPtr)(delegate* unmanaged<IntPtr*, IntPtr, IntPtr, void>)&ObjCMsgSendStret),
            (MsgSendFunction.ObjCMsgSendSuper,      (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, IntPtr>)&ObjCMsgSendSuper),
            (MsgSendFunction.ObjCMsgSendSuperStret, (IntPtr)(delegate* unmanaged<IntPtr*, IntPtr, IntPtr, void>)&ObjCMsgSendSuperStret),
        };

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(MsgSendFunction.ObjCMsgSend)]
        [InlineData(MsgSendFunction.ObjCMsgSendFpret)]
        [InlineData(MsgSendFunction.ObjCMsgSendStret)]
        [InlineData(MsgSendFunction.ObjCMsgSendSuper)]
        [InlineData(MsgSendFunction.ObjCMsgSendSuperStret)]
        public void ValidateSetMessageSendPendingException(MsgSendFunction func)
        {
            // Pass functions to override as a string for remote execution
            RemoteExecutor.Invoke((string funcToOverrideAsStr) =>
            {
                MsgSendFunction msgSend = Enum.Parse<MsgSendFunction>(funcToOverrideAsStr);
                Assert.True(Enum.IsDefined<MsgSendFunction>(msgSend));

                ValidateSetMessageSendPendingExceptionImpl(msgSend);
            }, func.ToString()).Dispose();
        }

        private static void ValidateSetMessageSendPendingExceptionImpl(MsgSendFunction funcToOverride)
        {
            foreach (var (msgSend, func) in msgSendOverrides)
            {
                if (funcToOverride != msgSend)
                {
                    continue;
                }

                // Override message send function
                Bridge.SetMessageSendCallback(msgSend, func);

                // Call message send function through P/Invoke
                IntPtr inst = IntPtr.Zero;
                IntPtr sel = IntPtr.Zero;

                Exception ex = Assert.Throws<PendingException>(() => LibObjC.CallPInvoke(msgSend, inst, sel));
                Assert.Equal(msgSend.ToString(), ex.Message);

                break;
            }
        }
    }
}