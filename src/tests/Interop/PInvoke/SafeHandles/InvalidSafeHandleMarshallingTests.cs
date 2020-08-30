// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.InteropServices;
using TestLibrary;

namespace SafeHandleTests
{
    public class InvalidSafeHandleMarshallingTests
    {
        public static void RunTest()
        {
            Assert.Throws<InvalidOperationException>(() => SafeHandleNative.SafeHandle_Invalid(new TestSafeHandle()));
            Assert.Throws<MarshalDirectiveException>(() => SafeHandleNative.SafeHandle_Invalid(new TestSafeHandle[1]));
            Assert.Throws<TypeLoadException>(() => SafeHandleNative.SafeHandle_Invalid(new SafeHandleNative.StructWithSafeHandleArray()));
        }
    }
}
