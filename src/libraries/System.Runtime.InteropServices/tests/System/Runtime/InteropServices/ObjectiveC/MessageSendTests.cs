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
    public unsafe class MessageSendTests
    {
        private static int s_count = 1;
        private static bool s_callbackInvoked = false;

        [UnmanagedCallersOnly]
        private static IntPtr ObjCMsgSend(IntPtr inst, IntPtr sel) => ReturnPtr(MsgSendFunction.ObjCMsgSend);

        [UnmanagedCallersOnly]
        private static IntPtr ObjCMsgSendFpret(IntPtr inst, IntPtr sel) => ReturnPtr(MsgSendFunction.ObjCMsgSendFpret);

        [UnmanagedCallersOnly]
        private static void ObjCMsgSendStret(IntPtr* ret, IntPtr inst, IntPtr sel) => *ret = ReturnPtr(MsgSendFunction.ObjCMsgSendStret);

        [UnmanagedCallersOnly]
        private static IntPtr ObjCMsgSendSuper(IntPtr inst, IntPtr sel) => ReturnPtr(MsgSendFunction.ObjCMsgSendSuper);

        [UnmanagedCallersOnly]
        private static void ObjCMsgSendSuperStret(IntPtr* ret, IntPtr inst, IntPtr sel) => *ret = ReturnPtr(MsgSendFunction.ObjCMsgSendSuperStret);

        private static IntPtr ReturnPtr(MsgSendFunction msgSendFunc)
        {
            s_callbackInvoked = true;
            return new IntPtr(s_count + (int)msgSendFunc);
        }

        private static (MsgSendFunction MsgSend, IntPtr Func)[] msgSendOverrides =
        {
            (MsgSendFunction.ObjCMsgSend,           (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, IntPtr>)&ObjCMsgSend),
            (MsgSendFunction.ObjCMsgSendFpret,      (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, IntPtr>)&ObjCMsgSendFpret),
            (MsgSendFunction.ObjCMsgSendStret,      (IntPtr)(delegate* unmanaged<IntPtr*, IntPtr, IntPtr, void>)&ObjCMsgSendStret),
            (MsgSendFunction.ObjCMsgSendSuper,      (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, IntPtr>)&ObjCMsgSendSuper),
            (MsgSendFunction.ObjCMsgSendSuperStret, (IntPtr)(delegate* unmanaged<IntPtr*, IntPtr, IntPtr, void>)&ObjCMsgSendSuperStret),
        };

        [Fact]
        public void SetMessageSendCallback_NullCallback()
        {
            Assert.Throws<ArgumentNullException>(() => Bridge.SetMessageSendCallback(MsgSendFunction.ObjCMsgSend, IntPtr.Zero));
        }

        [Fact]
        public void SetMessageSendCallback_InvalidMsgSendFunction()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Bridge.SetMessageSendCallback((MsgSendFunction)100, msgSendOverrides[0].Func));
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void SetMessageSendCallback_AlreadySet()
        {
            RemoteExecutor.Invoke(() =>
            {
                var (msgSend, func) = msgSendOverrides[0];
                Bridge.SetMessageSendCallback(msgSend, func);
                Assert.Throws<InvalidOperationException>(() => Bridge.SetMessageSendCallback(msgSend, func));
            }).Dispose();
        }

        public static IEnumerable<object[]> MsgSendFunctionsToOverride()
        {
            yield return new[] { (MsgSendFunction[])Enum.GetValues<MsgSendFunction>() };
            yield return new[] { new MsgSendFunction[]{ MsgSendFunction.ObjCMsgSend, MsgSendFunction.ObjCMsgSendStret } };
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [MemberData(nameof(MsgSendFunctionsToOverride))]
        public void SetMessageSendCallback(MsgSendFunction[] funcsToOverride)
        {
            // Pass functions to override as a string for remote execution
            RemoteExecutor.Invoke((string funcsToOverrideAsStr) =>
            {
                string[] msgSendStrArray = funcsToOverrideAsStr.Split(';');
                MsgSendFunction[] msgSendArray = new MsgSendFunction[msgSendStrArray.Length];
                for (int i = 0; i < msgSendStrArray.Length; i++)
                {
                    MsgSendFunction msgSend = Enum.Parse<MsgSendFunction>(msgSendStrArray[i]);
                    Assert.True(Enum.IsDefined<MsgSendFunction>(msgSend));
                    msgSendArray[i] = msgSend;
                }

                SetMessageSendCallbackImpl(msgSendArray);
            }, string.Join(';', funcsToOverride)).Dispose();
        }

        private static IntPtr CallPInvoke(MsgSendFunction msgSend, IntPtr inst, IntPtr sel)
        {
            switch (msgSend)
            {
                case MsgSendFunction.ObjCMsgSend:
                    return libobjc.objc_msgSend(inst, sel);
                case MsgSendFunction.ObjCMsgSendFpret:
                    return libobjc.objc_msgSend_fpret(inst, sel);
                case MsgSendFunction.ObjCMsgSendStret:
                {
                    IntPtr ret;
                    libobjc.objc_msgSend_stret(out ret, inst, sel);
                    return ret;
                }
                case MsgSendFunction.ObjCMsgSendSuper:
                    return libobjc.objc_msgSendSuper(inst, sel);
                case MsgSendFunction.ObjCMsgSendSuperStret:
                {
                    IntPtr ret;
                    libobjc.objc_msgSendSuper_stret(out ret, inst, sel);
                    return ret;
                }
                default:
                    throw new ArgumentException($"Unknown {nameof(MsgSendFunction)}: {msgSend}");
            }
        }

        private static void SetMessageSendCallbackImpl(MsgSendFunction[] funcsToOverride)
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
                    Bridge.SetMessageSendCallback(msgSend, func);
                    expected = (IntPtr)(s_count + (int)msgSend);
                }
                else
                {
                    if (msgSend == MsgSendFunction.ObjCMsgSendSuper || msgSend == MsgSendFunction.ObjCMsgSendSuperStret)
                    {
                        // Calling super message functions requires a valid superclass and selector
                        var super = new libobjc.objc_super()
                        {
                            receiver = IntPtr.Zero,
                            super_class = libobjc.objc_getClass("NSObject")
                        };
                        inst = (IntPtr)(&super);
                        sel = libobjc.sel_getUid("self");
                    }

                    // Sending message to nil should return nil
                    expected = IntPtr.Zero;
                }

                // Call message send function through P/Invoke
                IntPtr ret = CallPInvoke(msgSend, inst, sel);

                Assert.Equal(shouldOverride, s_callbackInvoked);
                Assert.Equal(expected, ret);

                s_count++;
                s_callbackInvoked = false;
            }
        }

        private static class libobjc
        {
            private const string LibName = "/usr/lib/libobjc.dylib";

            [DllImport(LibName)]
            public static extern IntPtr objc_msgSend(IntPtr self, IntPtr selector);

            [DllImport(LibName)]
            public static extern IntPtr objc_msgSend_fpret(IntPtr self, IntPtr selector);

            [DllImport(LibName)]
            public static extern void objc_msgSend_stret(out IntPtr ret, IntPtr self, IntPtr selector);

            [DllImport(LibName)]
            public static extern IntPtr objc_msgSendSuper(IntPtr super, IntPtr selector);

            [DllImport(LibName)]
            public static extern void objc_msgSendSuper_stret(out IntPtr ret, IntPtr super, IntPtr selector);

            [DllImport(LibName)]
            public static extern IntPtr objc_getClass(string className);

            [DllImport(LibName)]
            public static extern IntPtr sel_getUid(string selector);

            // https://developer.apple.com/documentation/objectivec/objc_super
            [StructLayout(LayoutKind.Sequential)]
            public struct objc_super
            {
                public IntPtr receiver;
                public IntPtr super_class;
            }
        }
    }
}

