// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Swift;
using Xunit;

public class SelfContextTests
{
    private const string SwiftLib = "libSelfContext.dylib";

    [DllImport(SwiftLib, EntryPoint = "$s11SelfContext0A7LibraryC11getInstanceSvyFZ")]
    public static extern IntPtr getInstance();

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s11SelfContext0A7LibraryC14getMagicNumberSiyF")]
    public static extern nint getMagicNumber(SwiftSelf self);

    [Fact]
    public static int TestEntryPoint()
    {
        IntPtr pointer = getInstance();
        SwiftSelf self = new SwiftSelf(pointer);

        if (self.Value == IntPtr.Zero) {
            return 101;
        }

        int result = (int) getMagicNumber(self);
        Assert.Equal(42, result);
        return 100;
    }
}
