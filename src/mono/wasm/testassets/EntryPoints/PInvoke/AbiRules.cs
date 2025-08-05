using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public struct SingleFloatStruct {
    public float Value;
}
public struct SingleDoubleStruct {
    public struct Nested1 {
        // This field is private on purpose to ensure we treat visibility correctly
        double Value;
    }
    public Nested1 Value;
}
public struct SingleI64Struct {
    public Int64 Value;
}
public struct PairStruct {
    public int A, B;
}
public unsafe struct MyFixedArray {
    public fixed int elements[2];
}
[System.Runtime.CompilerServices.InlineArray(2)]
public struct MyInlineArray {
    public int element0;
}

public class Test
{
    public static unsafe int Main(string[] argv)
    {
        var i64_a = 0xFF00FF00FF00FF0L;
        var i64_b = ~i64_a;
        var resI = direct64(i64_a);
        Console.WriteLine("TestOutput -> l (l)=" + resI);

        var sis = new SingleI64Struct { Value = i64_a };
        var resSI = indirect64(sis);
        Console.WriteLine("TestOutput -> s (s)=" + resSI.Value);

        var resF = direct(3.14);
        Console.WriteLine("TestOutput -> f (d)=" + resF);

        SingleDoubleStruct sds = default;
        Unsafe.As<SingleDoubleStruct, double>(ref sds) = 3.14;

        resF = indirect_arg(sds);
        Console.WriteLine("TestOutput -> f (s)=" + resF);

        var res = indirect(sds);
        Console.WriteLine("TestOutput -> s (s)=" + res.Value);

        var pair = new PairStruct { A = 1, B = 2 };
        var paires = accept_and_return_pair(pair);
        Console.WriteLine("TestOutput -> paires.B=" + paires.B);

        // This test is split into methods to simplify debugging issues with it
        var ia = InlineArrayTest1();
        var iares = InlineArrayTest2(ia);
        Console.WriteLine($"TestOutput -> iares[0]={iares[0]} iares[1]={iares[1]}");

        MyFixedArray fa = new ();
        for (int i = 0; i < 2; i++)
            fa.elements[i] = i;
        var fares = accept_and_return_fixedarray(fa);
        Console.WriteLine("TestOutput -> fares.elements[1]=" + fares.elements[1]);

        int exitCode = (int)res.Value;
        return exitCode;
    }

    public static unsafe MyInlineArray InlineArrayTest1 () {
        MyInlineArray ia = new ();
        for (int i = 0; i < 2; i++)
            ia[i] = i;
        return ia;
    }

    public static unsafe MyInlineArray InlineArrayTest2 (MyInlineArray ia) {
        return accept_and_return_inlinearray(ia);
    }

    [DllImport("wasm-abi", EntryPoint="accept_double_struct_and_return_float_struct")]
    public static extern SingleFloatStruct indirect(SingleDoubleStruct arg);

    [DllImport("wasm-abi", EntryPoint="accept_double_struct_and_return_float_struct")]
    public static extern float indirect_arg(SingleDoubleStruct arg);

    [DllImport("wasm-abi", EntryPoint="accept_double_struct_and_return_float_struct")]
    public static extern float direct(double arg);

    [DllImport("wasm-abi", EntryPoint="accept_and_return_i64_struct")]
    public static extern SingleI64Struct indirect64(SingleI64Struct arg);

    [DllImport("wasm-abi", EntryPoint="accept_and_return_i64_struct")]
    public static extern Int64 direct64(Int64 arg);

    [DllImport("wasm-abi", EntryPoint="accept_and_return_pair")]
    public static extern PairStruct accept_and_return_pair(PairStruct arg);

    [DllImport("wasm-abi", EntryPoint="accept_and_return_fixedarray")]
    public static extern MyFixedArray accept_and_return_fixedarray(MyFixedArray arg);

    [DllImport("wasm-abi", EntryPoint="accept_and_return_inlinearray")]
    public static extern MyInlineArray accept_and_return_inlinearray(MyInlineArray arg);
}
