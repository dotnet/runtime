using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Swift;
using Xunit;

public class InvalidCallingConvTests
{
    private const string SwiftLib = "dummy.dylib";

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s5Dummy9dummyFuncyyF")]
    public static extern nint DummyFunc1(SwiftSelf self1, SwiftSelf self2);

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s5Dummy9dummyFuncyyF")]
    public unsafe static extern nint DummyFunc2(SwiftError* error1, SwiftError* error2);

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s5Dummy9dummyFuncyyF")]
    public unsafe static extern nint DummyFunc3(SwiftSelf self1, SwiftSelf self2, SwiftError* error1, SwiftError* error2);

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s5Dummy9dummyFuncyyF")]
    public static extern nint DummyFunc4(SwiftError error1);

    [Fact]
    public unsafe static int TestEntryPoint()
    {
        SwiftSelf self = new SwiftSelf(IntPtr.Zero);
        SwiftError error = new SwiftError(IntPtr.Zero);

        try {
            DummyFunc1(self, self);
            return 101;
        } catch (InvalidProgramException e) { }
        try {
            DummyFunc2(&error, &error);
            return 102;
        } catch (InvalidProgramException e) { }
        try {
            DummyFunc3(self, self, &error, &error);
            return 103;
        } catch (InvalidProgramException e) { }
        try {
            DummyFunc4(error);
            return 104;
        } catch (InvalidProgramException e) { }
        return 100;
    }
}
