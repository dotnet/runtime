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
    public unsafe static extern void* getInstance();

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s16SwiftSelfContext0B7LibraryC14getMagicNumberSiyF")]
    public static extern nint getMagicNumber(SwiftSelf self);

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s16SwiftSelfContext0B7LibraryC14getMagicNumberSiyF")]
    public static extern nint getMagicNumberOnStack(int dummy0, int dummy1, int dummy2, int dummy3, int dummy4, int dummy5, int dummy6, int dummy7, int dummy8, int dummy9, SwiftSelf self);

    [Fact]
    public unsafe static void TestSwiftSelfContext()
    {
        void* pointer = getInstance();
        SwiftSelf self = new SwiftSelf(pointer);
        Assert.True(self.Value != null, "Failed to obtain an instance of SwiftSelf from the Swift library.");

        int result = (int)getMagicNumber(self);
        Assert.True(result == 42, "The result from Swift does not match the expected value.");
    }

    [Fact]
    public unsafe static void TestSwiftSelfContextOnStack()
    {
        void* pointer = getInstance();
        SwiftSelf self = new SwiftSelf(pointer);
        Assert.True(self.Value != null, "Failed to obtain an instance of SwiftSelf from the Swift library.");

        int i = 0;
        int result = (int)getMagicNumberOnStack(i, i + 1, i + 2, i + 3, i + 4, i + 5, i + 6, i + 7, i + 8, i + 9, self);
        Assert.True(result == 42, "The result from Swift does not match the expected value.");
    }
}
