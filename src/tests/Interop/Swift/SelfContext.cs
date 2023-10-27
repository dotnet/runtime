using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

public class SelfContextTests
{
    [DllImport("libSelfContext.dylib", EntryPoint = "$s11SelfContext11MathLibraryC11getInstanceSvyFZ")]
    public static extern IntPtr getInstance();

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport("libSelfContext.dylib", EntryPoint = "$s11SelfContext11MathLibraryC14getMagicNumber5dummyySV_tF")]
    public static extern void getMagicNumber(SwiftSelf handle);

    [Fact]
    public static int TestEntryPoint()
    {
        SwiftSelf selfHandle = new SwiftSelf();
        selfHandle.Value = getInstance();

        if (selfHandle.Value != IntPtr.Zero) {
            Console.WriteLine($"Self instance: 0x{selfHandle.Value:X16}");
            getMagicNumber (selfHandle);
            return 100;
        } else {
            return 101;
        }
    }
}
