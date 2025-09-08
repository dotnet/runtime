using System;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Linq;

public struct EmptyStruct {
}

[StructLayout(LayoutKind.Sequential)]
public struct EmptySequentialStruct {
}

[StructLayout(LayoutKind.Explicit)]
public struct EmptyExplicitStruct {
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct EmptySequentialPackStruct {
}

[StructLayout(LayoutKind.Explicit, Pack = 4)]
public struct EmptyExplicitPackStruct {
}

[StructLayout(LayoutKind.Explicit, Size = 0)]
public struct EmptyExplicitSize0Struct {
}

[StructLayout(LayoutKind.Explicit, Size = 1)]
public struct EmptyExplicitSize1Struct {
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct TestOffsets {
    public int A;
    public EmptyStruct B;
    public int C;
    public EmptyExplicitSize0Struct D;
    public int E;
    public EmptySequentialStruct F;
    public int G;
}

class Program {    
    private static unsafe void CheckSize<T> (int expected, ref int exitCode) {
        var t = typeof(T);
        var actualSize = Marshal.SizeOf(t);

        Console.WriteLine($"Marshal.SizeOf({t.Name}) == {actualSize}, expected {expected}");

        if (actualSize != expected)
            exitCode += 1;
    }

    // https://bugzilla.xamarin.com/show_bug.cgi?id=18941
    // Marshal.SizeOf should never report 0, even for empty structs or structs with Size=0 attribute
    public static int Main () {
        int exitCode = 0;

        CheckSize<EmptyStruct>(1, ref exitCode);
        CheckSize<EmptySequentialStruct>(1, ref exitCode);
        CheckSize<EmptyExplicitStruct>(1, ref exitCode);
        CheckSize<EmptySequentialPackStruct>(1, ref exitCode);
        CheckSize<EmptyExplicitPackStruct>(1, ref exitCode);
        CheckSize<EmptyExplicitSize0Struct>(1, ref exitCode);
        CheckSize<EmptyExplicitSize1Struct>(1, ref exitCode);
        CheckSize<TestOffsets>(19, ref exitCode);

        Console.WriteLine("--");

        var t = typeof(TestOffsets);
        var actualOffsets = (
            from f in t.GetFields() 
            select (name: f.Name, offset: Marshal.OffsetOf(t, f.Name).ToInt32())
        ).ToList();

        var expectedOffsets = new [] {
            (name: "A", offset: 0),
            (name: "B", offset: 4),
            (name: "C", offset: 5),
            (name: "D", offset: 9),
            (name: "E", offset: 10),
            (name: "F", offset: 14),
            (name: "G", offset: 15)
        };

        if (!actualOffsets.SequenceEqual(expectedOffsets)) {
            Console.WriteLine("Field offset mismatch:");

            for (int i = 0; i < expectedOffsets.Length; i++) {
                var expected = expectedOffsets[i];
                var actual = actualOffsets[i];
                Console.WriteLine($"OffsetOf({t.Name}.{actual.name}) == {actual.offset}, expected OffsetOf({expected.name}) == {expected.offset}.");
            }
        }

        return exitCode;
    }
}