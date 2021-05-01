// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ObjectiveC;

using Microsoft.DotNet.RemoteExecutor;
using Xunit;

using static System.Runtime.InteropServices.ObjectiveC.ObjectiveCMarshal;

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
        private static IntPtr MsgSend(IntPtr inst, IntPtr sel) => SetPendingException();

        [UnmanagedCallersOnly]
        private static IntPtr MsgSendFpret(IntPtr inst, IntPtr sel) => SetPendingException();

        [UnmanagedCallersOnly]
        private static void MsgSendStret(IntPtr* ret, IntPtr inst, IntPtr sel) => *ret = SetPendingException();

        [UnmanagedCallersOnly]
        private static IntPtr MsgSendSuper(IntPtr inst, IntPtr sel) => SetPendingException();

        [UnmanagedCallersOnly]
        private static void MsgSendSuperStret(IntPtr* ret, IntPtr inst, IntPtr sel) => *ret = SetPendingException();

        private static IntPtr SetPendingException([CallerMemberName] string callerName = "")
        {
            ObjectiveCMarshal.SetMessageSendPendingException(new PendingException(callerName));
            return IntPtr.Zero;
        }

        private static (MessageSendFunction MsgSend, IntPtr Func)[] msgSendOverrides =
        {
            (MessageSendFunction.MsgSend,           (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, IntPtr>)&MsgSend),
            (MessageSendFunction.MsgSendFpret,      (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, IntPtr>)&MsgSendFpret),
            (MessageSendFunction.MsgSendStret,      (IntPtr)(delegate* unmanaged<IntPtr*, IntPtr, IntPtr, void>)&MsgSendStret),
            (MessageSendFunction.MsgSendSuper,      (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, IntPtr>)&MsgSendSuper),
            (MessageSendFunction.MsgSendSuperStret, (IntPtr)(delegate* unmanaged<IntPtr*, IntPtr, IntPtr, void>)&MsgSendSuperStret),
        };

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(MessageSendFunction.MsgSend)]
        [InlineData(MessageSendFunction.MsgSendFpret)]
        [InlineData(MessageSendFunction.MsgSendStret)]
        [InlineData(MessageSendFunction.MsgSendSuper)]
        [InlineData(MessageSendFunction.MsgSendSuperStret)]
        public void ValidateSetMessageSendPendingException(MessageSendFunction func)
        {
            // Pass functions to override as a string for remote execution
            RemoteExecutor.Invoke((string funcToOverrideAsStr) =>
            {
                MessageSendFunction msgSend = Enum.Parse<MessageSendFunction>(funcToOverrideAsStr);
                Assert.True(Enum.IsDefined<MessageSendFunction>(msgSend));

                ValidateSetMessageSendPendingExceptionImpl(msgSend);
            }, func.ToString()).Dispose();
        }

        private static void ValidateSetMessageSendPendingExceptionImpl(MessageSendFunction funcToOverride)
        {
            foreach (var (msgSend, func) in msgSendOverrides)
            {
                if (funcToOverride != msgSend)
                {
                    continue;
                }

                // Override message send function
                ObjectiveCMarshal.SetMessageSendCallback(msgSend, func);

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