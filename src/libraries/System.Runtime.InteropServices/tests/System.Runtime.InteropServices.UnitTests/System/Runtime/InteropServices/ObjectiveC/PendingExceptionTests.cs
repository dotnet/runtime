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

        private static void ValidateSetMessageSendPendingExceptionImpl(MessageSendFunction msgSend)
        {
            if (!LibObjC.SupportedOnPlatform(msgSend))
            {
                return;
            }

            IntPtr func = msgSend switch
            {
                MessageSendFunction.MsgSend => (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, IntPtr>)&MsgSend,
                MessageSendFunction.MsgSendFpret => (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, IntPtr>)&MsgSendFpret,
                MessageSendFunction.MsgSendStret => (IntPtr)(delegate* unmanaged<IntPtr*, IntPtr, IntPtr, void>)&MsgSendStret,
                MessageSendFunction.MsgSendSuper => (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, IntPtr>)&MsgSendSuper,
                MessageSendFunction.MsgSendSuperStret => (IntPtr)(delegate* unmanaged<IntPtr*, IntPtr, IntPtr, void>)&MsgSendSuperStret,
                _ => throw new Exception($"Unknown {nameof(MessageSendFunction)}"),
            };

            // Override message send function
            //
            // We are using the overriding mechanism to enable validating in the Libraries test suite.
            // Technically any Objective-C code that is entered via msgSend could call the managed SetMessageSendPendingException()
            // and it would be thrown when returning from the P/Invoke. This approach avoids us having to
            // create a pure Objective-C library for testing this behavior.
            ObjectiveCMarshal.SetMessageSendCallback(msgSend, func);

            // Call message send function through P/Invoke
            IntPtr inst = IntPtr.Zero;
            IntPtr sel = IntPtr.Zero;

            Exception ex = Assert.Throws<PendingException>(() => LibObjC.CallPInvoke(msgSend, inst, sel));
            Assert.Equal(msgSend.ToString(), ex.Message);
        }
    }
}