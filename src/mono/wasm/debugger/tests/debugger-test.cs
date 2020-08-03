// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

public partial class Math
{ //Only append content to this class as the test suite depends on line info
    public static int IntAdd(int a, int b)
    {
        int c = a + b;
        int d = c + b;
        int e = d + a;
        int f = 0;
        return e;
    }

    public static int UseComplex(int a, int b)
    {
        var complex = new Simple.Complex(10, "xx");
        int c = a + b;
        int d = c + b;
        int e = d + a;
        int f = 0;
        e += complex.DoStuff();
        return e;
    }

    delegate bool IsMathNull(Math m);

    public static int DelegatesTest()
    {
        Func<Math, bool> fn_func = (Math m) => m == null;
        Func<Math, bool> fn_func_null = null;
        Func<Math, bool>[] fn_func_arr = new Func<Math, bool>[] {
            (Math m) => m == null };

        Math.IsMathNull fn_del = Math.IsMathNullDelegateTarget;
        var fn_del_arr = new Math.IsMathNull[] { Math.IsMathNullDelegateTarget };
        var m_obj = new Math();
        Math.IsMathNull fn_del_null = null;
        bool res = fn_func(m_obj) && fn_del(m_obj) && fn_del_arr[0](m_obj) && fn_del_null == null && fn_func_null == null && fn_func_arr[0] != null;

        // Unused locals

        Func<Math, bool> fn_func_unused = (Math m) => m == null;
        Func<Math, bool> fn_func_null_unused = null;
        Func<Math, bool>[] fn_func_arr_unused = new Func<Math, bool>[] { (Math m) => m == null };

        Math.IsMathNull fn_del_unused = Math.IsMathNullDelegateTarget;
        Math.IsMathNull fn_del_null_unused = null;
        var fn_del_arr_unused = new Math.IsMathNull[] { Math.IsMathNullDelegateTarget };
        OuterMethod();
        Console.WriteLine("Just a test message, ignore");
        return res ? 0 : 1;
    }

    public static int GenericTypesTest()
    {
        var list = new System.Collections.Generic.Dictionary<Math[], IsMathNull>();
        System.Collections.Generic.Dictionary<Math[], IsMathNull> list_null = null;

        var list_arr = new System.Collections.Generic.Dictionary<Math[], IsMathNull>[] { new System.Collections.Generic.Dictionary<Math[], IsMathNull>() };
        System.Collections.Generic.Dictionary<Math[], IsMathNull>[] list_arr_null = null;

        Console.WriteLine($"list_arr.Length: {list_arr.Length}, list.Count: {list.Count}");

        // Unused locals

        var list_unused = new System.Collections.Generic.Dictionary<Math[], IsMathNull>();
        System.Collections.Generic.Dictionary<Math[], IsMathNull> list_null_unused = null;

        var list_arr_unused = new System.Collections.Generic.Dictionary<Math[], IsMathNull>[] { new System.Collections.Generic.Dictionary<Math[], IsMathNull>() };
        System.Collections.Generic.Dictionary<Math[], IsMathNull>[] list_arr_null_unused = null;

        OuterMethod();
        Console.WriteLine("Just a test message, ignore");
        return 0;
    }

    static bool IsMathNullDelegateTarget(Math m) => m == null;

    public static void OuterMethod()
    {
        Console.WriteLine($"OuterMethod called");
        var nim = new Math.NestedInMath();
        var i = 5;
        var text = "Hello";
        var new_i = nim.InnerMethod(i);
        Console.WriteLine($"i: {i}");
        Console.WriteLine($"-- InnerMethod returned: {new_i}, nim: {nim}, text: {text}");
        int k = 19;
        new_i = InnerMethod2("test string", new_i, out k);
        Console.WriteLine($"-- InnerMethod2 returned: {new_i}, and k: {k}");
    }

    static int InnerMethod2(string s, int i, out int k)
    {
        k = i + 10;
        Console.WriteLine($"s: {s}, i: {i}, k: {k}");
        return i - 2;
    }

    public class NestedInMath
    {
        public int InnerMethod(int i)
        {
            SimpleStructProperty = new SimpleStruct() { dt = new DateTime(2020, 1, 2, 3, 4, 5) };
            int j = i + 10;
            string foo_str = "foo";
            Console.WriteLine($"i: {i} and j: {j}, foo_str: {foo_str} ");
            j += 9;
            Console.WriteLine($"i: {i} and j: {j}");
            return j;
        }

        Math m = new Math();
        public async System.Threading.Tasks.Task<bool> AsyncMethod0(string s, int i)
        {
            string local0 = "value0";
            await System.Threading.Tasks.Task.Delay(1);
            Console.WriteLine($"* time for the second await, local0: {local0}");
            await AsyncMethodNoReturn();
            return true;
        }

        public async System.Threading.Tasks.Task AsyncMethodNoReturn()
        {
            var ss = new SimpleStruct() { dt = new DateTime(2020, 1, 2, 3, 4, 5) };
            var ss_arr = new SimpleStruct[] { };
            //ss.gs.StringField = "field in GenericStruct";

            //Console.WriteLine ($"Using the struct: {ss.dt}, {ss.gs.StringField}, ss_arr: {ss_arr.Length}");
            string str = "AsyncMethodNoReturn's local";
            //Console.WriteLine ($"* field m: {m}");
            await System.Threading.Tasks.Task.Delay(1);
            Console.WriteLine($"str: {str}");
        }

        public static async System.Threading.Tasks.Task<bool> AsyncTest(string s, int i)
        {
            var li = 10 + i;
            var ls = s + "test";
            return await new NestedInMath().AsyncMethod0(s, i);
        }

        public SimpleStruct SimpleStructProperty { get; set; }
    }

    public static void PrimitiveTypesTest()
    {
        char c0 = '€';
        char c1 = 'A';
        // TODO: other types!
        // just trying to ensure vars don't get optimized out
        if (c0 < 32 || c1 > 32)
            Console.WriteLine($"{c0}, {c1}");
    }

    public static int DelegatesSignatureTest()
    {
        Func<Math, GenericStruct<GenericStruct<int[]>>, GenericStruct<bool[]>> fn_func = (m, gs) => new GenericStruct<bool[]>();
        Func<Math, GenericStruct<GenericStruct<int[]>>, GenericStruct<bool[]>> fn_func_del = GenericStruct<int>.DelegateTargetForSignatureTest;
        Func<Math, GenericStruct<GenericStruct<int[]>>, GenericStruct<bool[]>> fn_func_null = null;
        Func<bool> fn_func_only_ret = () => { Console.WriteLine ($"hello"); return true; };
        var fn_func_arr = new Func<Math, GenericStruct<GenericStruct<int[]>>, GenericStruct<bool[]>>[] {
                (m, gs) => new GenericStruct<bool[]> () };

        Math.DelegateForSignatureTest fn_del = GenericStruct<int>.DelegateTargetForSignatureTest;
        Math.DelegateForSignatureTest fn_del_l = (m, gs) => new GenericStruct<bool[]> { StringField = "fn_del_l#lambda" };
        var fn_del_arr = new Math.DelegateForSignatureTest[] { GenericStruct<int>.DelegateTargetForSignatureTest, (m, gs) => new GenericStruct<bool[]> { StringField = "fn_del_arr#1#lambda" } };
        var m_obj = new Math();
        Math.DelegateForSignatureTest fn_del_null = null;
        var gs_gs = new GenericStruct<GenericStruct<int[]>>
        {
            List = new System.Collections.Generic.List<GenericStruct<int[]>>
            {
            new GenericStruct<int[]> { StringField = "gs#List#0#StringField" },
            new GenericStruct<int[]> { StringField = "gs#List#1#StringField" }
            }
        };

        Math.DelegateWithVoidReturn fn_void_del = Math.DelegateTargetWithVoidReturn;
        var fn_void_del_arr = new Math.DelegateWithVoidReturn[] { Math.DelegateTargetWithVoidReturn };
        Math.DelegateWithVoidReturn fn_void_del_null = null;

        var rets = new GenericStruct<bool[]>[]
        {
            fn_func(m_obj, gs_gs),
            fn_func_del(m_obj, gs_gs),
            fn_del(m_obj, gs_gs),
            fn_del_l(m_obj, gs_gs),
            fn_del_arr[0](m_obj, gs_gs),
            fn_func_arr[0](m_obj, gs_gs)
        };

        var gs = new GenericStruct<int[]>();
        fn_void_del(gs);
        fn_void_del_arr[0](gs);
        fn_func_only_ret();
        foreach (var ret in rets) Console.WriteLine($"ret: {ret}");
        OuterMethod();
        Console.WriteLine($"- {gs_gs.List[0].StringField}");
        return 0;
    }

    public static int ActionTSignatureTest()
    {
        Action<GenericStruct<int[]>> fn_action = (_) => { };
        Action<GenericStruct<int[]>> fn_action_del = Math.DelegateTargetWithVoidReturn;
        Action fn_action_bare = () => { };
        Action<GenericStruct<int[]>> fn_action_null = null;
        var fn_action_arr = new Action<GenericStruct<int[]>>[]
        {
            (gs) => new GenericStruct<int[]>(),
            Math.DelegateTargetWithVoidReturn,
            null
        };

        var gs = new GenericStruct<int[]>();
        fn_action(gs);
        fn_action_del(gs);
        fn_action_arr[0](gs);
        fn_action_bare();
        OuterMethod();
        return 0;
    }

    public static int NestedDelegatesTest()
    {
        Func<Func<int, bool>, bool> fn_func = (_) => { return true; };
        Func<Func<int, bool>, bool> fn_func_null = null;
        var fn_func_arr = new Func<Func<int, bool>, bool>[] {
                (gs) => { return true; } };

        var fn_del_arr = new Func<Func<int, bool>, bool>[] { DelegateTargetForNestedFunc<Func<int, bool>> };
        var m_obj = new Math();
        Func<Func<int, bool>, bool> fn_del_null = null;
        Func<int, bool> fs = (i) => i == 0;
        fn_func(fs);
        fn_del_arr[0](fs);
        fn_func_arr[0](fs);
        OuterMethod();
        return 0;
    }

    public static void DelegatesAsMethodArgsTest()
    {
        var _dst_arr = new DelegateForSignatureTest[]
        {
            GenericStruct<int>.DelegateTargetForSignatureTest,
            (m, gs) => new GenericStruct<bool[]>()
        };
        Func<char[], bool> _fn_func = (cs) => cs.Length == 0;
        Action<GenericStruct<int>[]> _fn_action = (gss) => { };

        new Math().MethodWithDelegateArgs(_dst_arr, _fn_func, _fn_action);
    }

    void MethodWithDelegateArgs(Math.DelegateForSignatureTest[] dst_arr, Func<char[], bool> fn_func,
        Action<GenericStruct<int>[]> fn_action)
    {
        Console.WriteLine($"Placeholder for breakpoint");
        OuterMethod();
    }

    public static async System.Threading.Tasks.Task MethodWithDelegatesAsyncTest()
    {
        await new Math().MethodWithDelegatesAsync();
    }

    async System.Threading.Tasks.Task MethodWithDelegatesAsync()
    {
        var _dst_arr = new DelegateForSignatureTest[]
        {
            GenericStruct<int>.DelegateTargetForSignatureTest,
            (m, gs) => new GenericStruct<bool[]>()
        };
        Func<char[], bool> _fn_func = (cs) => cs.Length == 0;
        Action<GenericStruct<int>[]> _fn_action = (gss) => { };

        Console.WriteLine($"Placeholder for breakpoint");
        await System.Threading.Tasks.Task.CompletedTask;
    }

    public delegate void DelegateWithVoidReturn(GenericStruct<int[]> gs);
    public static void DelegateTargetWithVoidReturn(GenericStruct<int[]> gs) { }

    delegate GenericStruct<bool[]> DelegateForSignatureTest(Math m, GenericStruct<GenericStruct<int[]>> gs);
    static bool DelegateTargetForNestedFunc<T>(T arg) => true;

    public struct SimpleStruct
    {
        public DateTime dt;
        public GenericStruct<DateTime> gs;
    }

    public struct GenericStruct<T>
    {
        public System.Collections.Generic.List<T> List;
        public string StringField;

        public static GenericStruct<bool[]> DelegateTargetForSignatureTest(Math m, GenericStruct<GenericStruct<T[]>> gs) => new GenericStruct<bool[]>();
    }

}

public class DebuggerTest
{
    public static void run_all()
    {
        locals();
    }

    public static int locals()
    {
        int l_int = 1;
        char l_char = 'A';
        long l_long = Int64.MaxValue;
        ulong l_ulong = UInt64.MaxValue;
        locals_inner();
        return 0;
    }

    static void locals_inner() { }
}