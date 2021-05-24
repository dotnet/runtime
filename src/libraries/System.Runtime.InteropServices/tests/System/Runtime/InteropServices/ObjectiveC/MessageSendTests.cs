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
    public unsafe class MessageSendTests
    {
        private static int s_count = 1;
        private static bool s_callbackInvoked = false;

        [UnmanagedCallersOnly]
        private static IntPtr ObjCMsgSend(IntPtr inst, IntPtr sel) => ReturnPtr(MessageSendFunction.MsgSend);

        [UnmanagedCallersOnly]
        private static IntPtr ObjCMsgSendFpret(IntPtr inst, IntPtr sel) => ReturnPtr(MessageSendFunction.MsgSendFpret);

        [UnmanagedCallersOnly]
        private static void ObjCMsgSendStret(IntPtr* ret, IntPtr inst, IntPtr sel) => *ret = ReturnPtr(MessageSendFunction.MsgSendStret);

        [UnmanagedCallersOnly]
        private static IntPtr ObjCMsgSendSuper(IntPtr inst, IntPtr sel) => ReturnPtr(MessageSendFunction.MsgSendSuper);

        [UnmanagedCallersOnly]
        private static void ObjCMsgSendSuperStret(IntPtr* ret, IntPtr inst, IntPtr sel) => *ret = ReturnPtr(MessageSendFunction.MsgSendSuperStret);

        private static IntPtr ReturnPtr(MessageSendFunction msgSendFunc)
        {
            s_callbackInvoked = true;
            return new IntPtr(s_count + (int)msgSendFunc);
        }

        private static (MessageSendFunction MsgSend, IntPtr Func)[] msgSendOverrides =
        {
            (MessageSendFunction.MsgSend,           (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, IntPtr>)&ObjCMsgSend),
            (MessageSendFunction.MsgSendFpret,      (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, IntPtr>)&ObjCMsgSendFpret),
            (MessageSendFunction.MsgSendStret,      (IntPtr)(delegate* unmanaged<IntPtr*, IntPtr, IntPtr, void>)&ObjCMsgSendStret),
            (MessageSendFunction.MsgSendSuper,      (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, IntPtr>)&ObjCMsgSendSuper),
            (MessageSendFunction.MsgSendSuperStret, (IntPtr)(delegate* unmanaged<IntPtr*, IntPtr, IntPtr, void>)&ObjCMsgSendSuperStret),
        };

        [Fact]
        public void SetMessageSendCallback_NullCallback()
        {
            Assert.Throws<ArgumentNullException>(() => ObjectiveCMarshal.SetMessageSendCallback(MessageSendFunction.MsgSend, IntPtr.Zero));
        }

        [Fact]
        public void SetMessageSendCallback_InvalidMessageSendFunction()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ObjectiveCMarshal.SetMessageSendCallback((MessageSendFunction)100, msgSendOverrides[0].Func));
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void SetMessageSendCallback_AlreadySet()
        {
            RemoteExecutor.Invoke(() =>
            {
                var (msgSend, func) = msgSendOverrides[0];
                ObjectiveCMarshal.SetMessageSendCallback(msgSend, func);
                Assert.Throws<InvalidOperationException>(() => ObjectiveCMarshal.SetMessageSendCallback(msgSend, func));
            }).Dispose();
        }

        public static IEnumerable<object[]> MessageSendFunctionsToOverride()
        {
            yield return new[] { (MessageSendFunction[])Enum.GetValues<MessageSendFunction>() };
            yield return new[] { new MessageSendFunction[]{ MessageSendFunction.MsgSend, MessageSendFunction.MsgSendStret } };
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [MemberData(nameof(MessageSendFunctionsToOverride))]
        public void SetMessageSendCallback(MessageSendFunction[] funcsToOverride)
        {
            // Pass functions to override as a string for remote execution
            RemoteExecutor.Invoke((string funcsToOverrideAsStr) =>
            {
                string[] msgSendStrArray = funcsToOverrideAsStr.Split(';');
                MessageSendFunction[] msgSendArray = new MessageSendFunction[msgSendStrArray.Length];
                for (int i = 0; i < msgSendStrArray.Length; i++)
                {
                    MessageSendFunction msgSend = Enum.Parse<MessageSendFunction>(msgSendStrArray[i]);
                    Assert.True(Enum.IsDefined<MessageSendFunction>(msgSend));
                    msgSendArray[i] = msgSend;
                }

                SetMessageSendCallbackImpl(msgSendArray);
            }, string.Join(';', funcsToOverride)).Dispose();
        }

        private static void SetMessageSendCallbackImpl(MessageSendFunction[] funcsToOverride)
        {
            foreach (var (msgSend, func) in msgSendOverrides)
            {
                bool shouldOverride = Array.IndexOf(funcsToOverride, msgSend) >= 0;

                IntPtr expected;
                IntPtr inst = IntPtr.Zero;
                IntPtr sel = IntPtr.Zero;
                if (shouldOverride)
                {
                    // Override message send function
                    ObjectiveCMarshal.SetMessageSendCallback(msgSend, func);
                    expected = (IntPtr)(s_count + (int)msgSend);
                }
                else
                {
                    if (msgSend == MessageSendFunction.MsgSendSuper || msgSend == MessageSendFunction.MsgSendSuperStret)
                    {
                        // Calling super message functions requires a valid superclass and selector
                        var super = new LibObjC.objc_super()
                        {
                            receiver = IntPtr.Zero,
                            super_class = LibObjC.objc_getClass("NSObject")
                        };
                        inst = (IntPtr)(&super);
                        sel = LibObjC.sel_getUid("self");
                    }

                    // Sending message to nil should return nil
                    expected = IntPtr.Zero;
                }

                // Call message send function through P/Invoke
                IntPtr ret = LibObjC.CallPInvoke(msgSend, inst, sel);

                Assert.Equal(shouldOverride, s_callbackInvoked);
                Assert.Equal(expected, ret);

                s_count++;
                s_callbackInvoked = false;
            }
        }
    }
}

