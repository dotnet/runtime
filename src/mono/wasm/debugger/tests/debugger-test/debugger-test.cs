// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
public partial class Math
{ //Only append content to this class as the test suite depends on line info
    public static int IntAdd(int a, int b)
    {
        int c = a + b;
        int d = c + b;
        int e = d + a;
        bool f = true;
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
        Func<bool> fn_func_only_ret = () => { Console.WriteLine($"hello"); return true; };
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

    public delegate GenericStruct<bool[]> DelegateForSignatureTest(Math m, GenericStruct<GenericStruct<int[]>> gs);
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

    public static void TestSimpleStrings()
    {
        string str_null = null;
        string str_empty = String.Empty;
        string str_spaces = " ";
        string str_esc = "\\";

        var strings = new[]
        {
            str_null,
            str_empty,
            str_spaces,
            str_esc
        };
        Console.WriteLine($"break here");
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

    public static void BoxingTest()
    {
        int? n_i = 5;
        object o_i = n_i.Value;
        object o_n_i = n_i;

        object o_s = "foobar";
        object o_obj = new Math();
        DebuggerTests.ValueTypesTest.GenericStruct<int>? n_gs = new DebuggerTests.ValueTypesTest.GenericStruct<int> { StringField = "n_gs#StringField" };
        object o_gs = n_gs.Value;
        object o_n_gs = n_gs;

        DateTime? n_dt = new DateTime(2310, 1, 2, 3, 4, 5);
        object o_dt = n_dt.Value;
        object o_n_dt = n_dt;
        object o_null = null;
        object o_ia = new int[] {918, 58971};

        Console.WriteLine ($"break here");
    }

    public static async System.Threading.Tasks.Task BoxingTestAsync()
    {
        int? n_i = 5;
        object o_i = n_i.Value;
        object o_n_i = n_i;

        object o_s = "foobar";
        object o_obj = new Math();
        DebuggerTests.ValueTypesTest.GenericStruct<int>? n_gs = new DebuggerTests.ValueTypesTest.GenericStruct<int> { StringField = "n_gs#StringField" };
        object o_gs = n_gs.Value;
        object o_n_gs = n_gs;

        DateTime? n_dt = new DateTime(2310, 1, 2, 3, 4, 5);
        object o_dt = n_dt.Value;
        object o_n_dt = n_dt;
        object o_null = null;
        object o_ia = new int[] {918, 58971};

        Console.WriteLine ($"break here");
        await System.Threading.Tasks.Task.CompletedTask;
    }

    public static void BoxedTypeObjectTest()
    {
        int i = 5;
        object o0 = i;
        object o1 = o0;
        object o2 = o1;
        object o3 = o2;

        object oo = new object();
        object oo0 = oo;
        Console.WriteLine ($"break here");
    }
    public static async System.Threading.Tasks.Task BoxedTypeObjectTestAsync()
    {
        int i = 5;
        object o0 = i;
        object o1 = o0;
        object o2 = o1;
        object o3 = o2;

        object oo = new object();
        object oo0 = oo;
        Console.WriteLine ($"break here");
        await System.Threading.Tasks.Task.CompletedTask;
    }

    public static void BoxedAsClass()
    {
        ValueType vt_dt = new DateTime(4819, 5, 6, 7, 8, 9);
        ValueType vt_gs = new Math.GenericStruct<string> { StringField = "vt_gs#StringField" };
        Enum e = new System.IO.FileMode();
        Enum ee = System.IO.FileMode.Append;

        Console.WriteLine ($"break here");
    }

    public static async System.Threading.Tasks.Task BoxedAsClassAsync()
    {
        ValueType vt_dt = new DateTime(4819, 5, 6, 7, 8, 9);
        ValueType vt_gs = new Math.GenericStruct<string> { StringField = "vt_gs#StringField" };
        Enum e = new System.IO.FileMode();
        Enum ee = System.IO.FileMode.Append;

        Console.WriteLine ($"break here");
        await System.Threading.Tasks.Task.CompletedTask;
    }
}

public class MulticastDelegateTestClass
{
    event EventHandler<string> TestEvent;
    MulticastDelegate Delegate;

    public static void run()
    {
        var obj = new MulticastDelegateTestClass();
        obj.Test();
        obj.TestAsync().Wait();
    }

    public void Test()
    {
        TestEvent += (_, s) => Console.WriteLine(s);
        TestEvent += (_, s) => Console.WriteLine(s + "qwe");
        Delegate = TestEvent;

        TestEvent?.Invoke(this, Delegate?.ToString());
    }

    public async System.Threading.Tasks.Task TestAsync()
    {
        TestEvent += (_, s) => Console.WriteLine(s);
        TestEvent += (_, s) => Console.WriteLine(s + "qwe");
        Delegate = TestEvent;

        TestEvent?.Invoke(this, Delegate?.ToString());
        await System.Threading.Tasks.Task.CompletedTask;
    }
}

public class EmptyClass
{
    public static void StaticMethodWithNoLocals()
    {
        Console.WriteLine($"break here");
    }

    public static async System.Threading.Tasks.Task StaticMethodWithNoLocalsAsync()
    {
        Console.WriteLine($"break here");
        await System.Threading.Tasks.Task.CompletedTask;
    }

    public static void run()
    {
        StaticMethodWithNoLocals();
        StaticMethodWithNoLocalsAsync().Wait();
    }
}

public struct EmptyStruct
{
    public static void StaticMethodWithNoLocals()
    {
        Console.WriteLine($"break here");
    }

    public static async System.Threading.Tasks.Task StaticMethodWithNoLocalsAsync()
    {
        Console.WriteLine($"break here");
        await System.Threading.Tasks.Task.CompletedTask;
    }

    public static void StaticMethodWithLocalEmptyStruct()
    {
        var es = new EmptyStruct();
        Console.WriteLine($"break here");
    }

    public static async System.Threading.Tasks.Task StaticMethodWithLocalEmptyStructAsync()
    {
        var es = new EmptyStruct();
        Console.WriteLine($"break here");
        await System.Threading.Tasks.Task.CompletedTask;
    }

    public static void run()
    {
        StaticMethodWithNoLocals();
        StaticMethodWithNoLocalsAsync().Wait();

        StaticMethodWithLocalEmptyStruct();
        StaticMethodWithLocalEmptyStructAsync().Wait();
    }
}

public class LoadDebuggerTest {
    public static void LoadLazyAssembly(string asm_base64, string pdb_base64)
    {
        byte[] asm_bytes = Convert.FromBase64String(asm_base64);
        byte[] pdb_bytes = null;
        if (pdb_base64 != null)
            pdb_bytes = Convert.FromBase64String(pdb_base64);

        var loadedAssembly = System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromStream(new System.IO.MemoryStream(asm_bytes), new System.IO.MemoryStream(pdb_bytes));
        Console.WriteLine($"Loaded - {loadedAssembly}");
    }
}

public class HiddenSequencePointTest {
    public static void StepOverHiddenSP()
    {
        Console.WriteLine("first line");
        #line hidden
        Console.WriteLine("second line");
        StepOverHiddenSP2();
        #line default
        Console.WriteLine("third line");
        MethodWithHiddenLinesAtTheEnd();
    }
    public static void StepOverHiddenSP2()
    {
        Console.WriteLine("StepOverHiddenSP2");
    }

    public static void MethodWithHiddenLinesAtTheEnd()
    {
        Console.WriteLine ($"MethodWithHiddenLinesAtTheEnd");
#line hidden
        Console.WriteLine ($"debugger shouldn't be able to step here");
    }
#line default
}

public class LoadDebuggerTestALC {
    static System.Reflection.Assembly loadedAssembly;
    public static void LoadLazyAssemblyInALC(string asm_base64, string pdb_base64)
    {
        var context = new System.Runtime.Loader.AssemblyLoadContext("testContext", true);
        byte[] asm_bytes = Convert.FromBase64String(asm_base64);
        byte[] pdb_bytes = null;
        if (pdb_base64 != null)
            pdb_bytes = Convert.FromBase64String(pdb_base64);

        loadedAssembly = context.LoadFromStream(new System.IO.MemoryStream(asm_bytes), new System.IO.MemoryStream(pdb_bytes));
        Console.WriteLine($"Loaded - {loadedAssembly}");
    }
    public static void RunMethodInALC(string type_name, string method_name)
    {
        var myType = loadedAssembly.GetType(type_name);
        var myMethod = myType.GetMethod(method_name);
        myMethod.Invoke(null, new object[] { 5, 10 });
    }
}

    public class TestHotReload {
        static System.Reflection.Assembly loadedAssembly;
        static byte[] dmeta_data1_bytes;
        static byte[] dil_data1_bytes;
        static byte[] dpdb_data1_bytes;
        static byte[] dmeta_data2_bytes;
        static byte[] dil_data2_bytes;
        static byte[] dpdb_data2_bytes;
        public static void LoadLazyHotReload(string asm_base64, string pdb_base64, string dmeta_data1, string dil_data1, string dpdb_data1, string dmeta_data2, string dil_data2, string dpdb_data2)
        {
            byte[] asm_bytes = Convert.FromBase64String(asm_base64);
            byte[] pdb_bytes = Convert.FromBase64String(pdb_base64);

            dmeta_data1_bytes = Convert.FromBase64String(dmeta_data1);
            dil_data1_bytes = Convert.FromBase64String(dil_data1);
            dpdb_data1_bytes = Convert.FromBase64String(dpdb_data1);

            dmeta_data2_bytes = Convert.FromBase64String(dmeta_data2);
            dil_data2_bytes = Convert.FromBase64String(dil_data2);
            dpdb_data2_bytes = Convert.FromBase64String(dpdb_data2);


            loadedAssembly = System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromStream(new System.IO.MemoryStream(asm_bytes), new System.IO.MemoryStream(pdb_bytes));
            Console.WriteLine($"Loaded - {loadedAssembly}");

        }
        public static void RunMethod(string className, string methodName)
        {
            var ty = typeof(System.Reflection.Metadata.MetadataUpdater);
            var mi = ty.GetMethod("GetCapabilities", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static, Array.Empty<Type>());

            if (mi == null)
                return;

            var caps = mi.Invoke(null, null) as string;

            if (String.IsNullOrEmpty(caps))
                return;

            var myType = loadedAssembly.GetType($"ApplyUpdateReferencedAssembly.{className}");
            var myMethod = myType.GetMethod(methodName);
            myMethod.Invoke(null, null);

            ApplyUpdate(loadedAssembly, 1);

            myType = loadedAssembly.GetType($"ApplyUpdateReferencedAssembly.{className}");
            myMethod = myType.GetMethod(methodName);
            myMethod.Invoke(null, null);

            ApplyUpdate(loadedAssembly, 2);

            myType = loadedAssembly.GetType($"ApplyUpdateReferencedAssembly.{className}");
            myMethod = myType.GetMethod(methodName);
            myMethod.Invoke(null, null);
        }

        internal static void ApplyUpdate (System.Reflection.Assembly assm, int version)
        {
            string basename = assm.Location;
            if (basename == "")
                basename = assm.GetName().Name + ".dll";
            Console.Error.WriteLine($"Apply Delta Update for {basename}, revision {version}");

            if (version == 1)
            {
                System.Reflection.Metadata.MetadataUpdater.ApplyUpdate(assm, dmeta_data1_bytes, dil_data1_bytes, dpdb_data1_bytes);
            }
            else if (version == 2)
            {
                System.Reflection.Metadata.MetadataUpdater.ApplyUpdate(assm, dmeta_data2_bytes, dil_data2_bytes, dpdb_data2_bytes);
            }

        }
    }


public class Something
{
    public string Name { get; set; }
    public Something() => Name = "Same of something";
    public override string ToString() => Name;
}

public class Foo
{
    public string Bar => Stuffs.First(x => x.Name.StartsWith('S')).Name;
    public System.Collections.Generic.List<Something> Stuffs { get; } = Enumerable.Range(0, 10).Select(x => new Something()).ToList();
    public string Lorem { get; set; } = "Safe";
    public string Ipsum { get; set; } = "Side";
    public Something What { get; } = new Something();
    public int Bart()
    {
        int ret;
        if (Lorem.StartsWith('S'))
            ret = 0;
        else
            ret = 1;
        return ret;
    }
    public static void RunBart()
    {
        Foo foo = new Foo();
        foo.Bart();
        Console.WriteLine(foo.OtherBar());
        foo.OtherBarAsync().Wait(10);
    }
    public bool OtherBar()
    {
        var a = 1;
        var b = 2;
        var x = "Stew";
        var y = "00.123";
        var c = a + b == 3 || b + a == 2;
        var d = TimeSpan.TryParseExact(y, @"ss\.fff", null, out var ts) && x.Contains('S');
        var e = TimeSpan.TryParseExact(y, @"ss\.fff", null, out var ts1)
                && x.Contains('S');
        var f = TimeSpan.TryParseExact(y, @"ss\.fff", null, out var ts2)
                &&
                x.Contains('S');
        var g = TimeSpan.TryParseExact(y, @"ss\.fff", null, out var ts3) &&
                x.Contains('S');
        return d && e == true;
    }
    public async System.Threading.Tasks.Task OtherBarAsync()
    {
        var a = 1;
        var b = 2;
        var x = "Stew";
        var y = "00.123";
        var c = a + b == 3 || b + a == 2;
        var d = TimeSpan.TryParseExact(y, @"ss\.fff", null, out var ts) && await AsyncMethod();
        var e = TimeSpan.TryParseExact(y, @"ss\.fff", null, out var ts1)
                && await AsyncMethod();
        var f = TimeSpan.TryParseExact(y, @"ss\.fff", null, out var ts2)
                &&
                await AsyncMethod();
        var g = await AsyncMethod() &&
                await AsyncMethod();
        Console.WriteLine(g);
        await System.Threading.Tasks.Task.CompletedTask;
    }
    public async System.Threading.Tasks.Task<bool> AsyncMethod()
    {
        await System.Threading.Tasks.Task.Delay(1);
        Console.WriteLine($"time for await");
        return true;
    }

}

public class MainPage
{
    public MainPage()
    {
    }

    int count = 0;
    private int someValue;

    public int SomeValue
    {
        get
        {
            return someValue;
        }
        set
        {
            someValue = value;
            count++;

            if (count == 10)
            {
                var view = 150;

                if (view != 50)
                {

                }
                System.Diagnostics.Debugger.Break();
            }

            SomeValue = count;
        }
    }

    public static void CallSetValue()
    {
        var mainPage = new MainPage();
        mainPage.SomeValue = 10;
    }
}

