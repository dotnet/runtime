// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

using Xunit;

using static System.Runtime.InteropServices.ObjectiveC.ObjectiveCMarshal;

namespace System.Runtime.InteropServices.Tests
{
    [PlatformSpecific(TestPlatforms.OSX)]
    internal static class LibObjC
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

        public static IntPtr CallPInvoke(MessageSendFunction msgSend, IntPtr inst, IntPtr sel)
        {
            switch (msgSend)
            {
                case MessageSendFunction.MsgSend:
                    return LibObjC.objc_msgSend(inst, sel);
                case MessageSendFunction.MsgSendFpret:
                    return LibObjC.objc_msgSend_fpret(inst, sel);
                case MessageSendFunction.MsgSendStret:
                {
                    IntPtr ret;
                    LibObjC.objc_msgSend_stret(out ret, inst, sel);
                    return ret;
                }
                case MessageSendFunction.MsgSendSuper:
                    return LibObjC.objc_msgSendSuper(inst, sel);
                case MessageSendFunction.MsgSendSuperStret:
                {
                    IntPtr ret;
                    LibObjC.objc_msgSendSuper_stret(out ret, inst, sel);
                    return ret;
                }
                default:
                    throw new ArgumentException($"Unknown {nameof(MessageSendFunction)}: {msgSend}");
            }
        }
    }
}