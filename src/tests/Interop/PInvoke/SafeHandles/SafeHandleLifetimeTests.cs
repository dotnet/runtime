// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using TestLibrary;

namespace SafeHandleTests
{
    public class SafeHandleLifetimeTests
    {
        private static readonly IntPtr initialValue = new IntPtr(458613);
        private static readonly IntPtr newValue = new IntPtr(987185);
        public static void RunTest()
        {
            var testHandle = new TestSafeHandle(initialValue);
            Assert.IsTrue(SafeHandleNative.SafeHandleByValue(testHandle, initialValue));
            Assert.IsFalse(testHandle.IsClosed);

            Assert.IsTrue(SafeHandleNative.SafeHandleByRef(ref testHandle, initialValue, newValue));
            Assert.IsFalse(testHandle.IsClosed);

            testHandle = null;
            SafeHandleNative.SafeHandleOut(out testHandle, initialValue);
            Assert.IsFalse(testHandle.IsClosed);

            testHandle = SafeHandleNative.SafeHandleReturn(newValue);
            Assert.IsFalse(testHandle.IsClosed);

            testHandle = SafeHandleNative.SafeHandleReturn_Swapped(newValue);
            Assert.IsFalse(testHandle.IsClosed);

            var str = new SafeHandleNative.StructWithHandle
            {
                handle = new TestSafeHandle(initialValue)
            };

            SafeHandleNative.StructWithSafeHandleByValue(str, initialValue);
            Assert.IsFalse(str.handle.IsClosed);
            
            SafeHandleNative.StructWithSafeHandleByRef(ref str, initialValue, initialValue);
            Assert.IsFalse(str.handle.IsClosed);
        }
    }
}
