// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Swift;
using Xunit;

public class SelfContextTests
{
    private const string SwiftLib = "libSwiftSelfContext.dylib";

    [DllImport(SwiftLib, EntryPoint = "$s16SwiftSelfContext0B7LibraryC11getInstanceSvyFZ")]
    public static extern IntPtr getInstance();

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s16SwiftSelfContext0B7LibraryC14getMagicNumberSiyF")]
    public static extern nint getMagicNumber(SwiftSelf self);

    [Fact]
    public static void TestSwiftSelfContext()
    {
        IntPtr pointer = getInstance();
        SwiftSelf self = new SwiftSelf(pointer);

        if (self.Value == IntPtr.Zero) {
            Assert.Fail("Failed to obtain an instance of SwiftSelf from the Swift library.");
        } else {
            int result = (int)getMagicNumber(self);
            if (result != 42) {
                Assert.Fail("The result from Swift does not match the expected value.");
            }
        }
    }
}
