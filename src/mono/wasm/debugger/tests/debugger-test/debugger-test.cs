// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
public partial class Math
{ //Only append content to this class as the test suite depends on line info
    [System.Runtime.InteropServices.JavaScript.JSExport] public static int IntAdd(int a, int b)
    {
        int c = a + b;
        int d = c + b;
        int e = d + a;
        bool f = true;
        return e;
    }

    [System.Runtime.InteropServices.JavaScript.JSExport] public static int UseComplex(int a, int b)
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

    [System.Runtime.InteropServices.JavaScript.JSExport] public static int DelegatesTest()
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

    [System.Runtime.InteropServices.JavaScript.JSExport] public static int GenericTypesTest()
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

    [System.Runtime.InteropServices.JavaScript.JSExport] public static void OuterMethod()
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

    public partial class NestedInMath
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

        [System.Runtime.InteropServices.JavaScript.JSExport] public static async System.Threading.Tasks.Task<bool> AsyncTest(string s, int i)
        {
            var li = 10 + i;
            var ls = s + "test";
            return await new NestedInMath().AsyncMethod0(s, i);
        }

        public SimpleStruct SimpleStructProperty { get; set; }
    }

    public static void PrimitiveTypesTest()
    {
        char c0 = '\u20AC';
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

public partial class DebuggerTest
{
    [System.Runtime.InteropServices.JavaScript.JSExport] public static void run_all()
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

public partial class HiddenSequencePointTest {
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
        public static void RunMethod(string className, string methodName, string methodName2, string methodName3)
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
            myMethod = myType.GetMethod(methodName2);
            myMethod.Invoke(null, null);

            ApplyUpdate(loadedAssembly, 2);

            myType = loadedAssembly.GetType($"ApplyUpdateReferencedAssembly.{className}");
            myMethod = myType.GetMethod(methodName3);
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

public class LoopClass
{
    public static void LoopToBreak()
    {
        for (int i = 0; i < 10; i++)
        {
            Console.WriteLine($"should pause only on i == 3");
        }
        Console.WriteLine("breakpoint to check");
    }
}

public class SteppingInto
{
    static int currentCount = 0;
    static MyIncrementer incrementer = new MyIncrementer();
    public static void MethodToStep()
    {
        currentCount = incrementer.Increment(currentCount);
    }
}

public class MyIncrementer
{
    private Func<DateTime> todayFunc = () => new DateTime(2061, 1, 5); // Wednesday

    public int Increment(int count)
    {
        var today = todayFunc();
        if (today.DayOfWeek == DayOfWeek.Sunday)
        {
            return count + 2;
        }

        return count + 1;
    }
}

public class DebuggerAttribute
{
    [System.Diagnostics.DebuggerHidden]
    public static void HiddenMethod()
    {
        var a = 9;
    }

    [System.Diagnostics.DebuggerHidden]
    public static void HiddenMethodUserBreak()
    {
        System.Diagnostics.Debugger.Break();
    }

    public static void RunDebuggerHidden()
    {
        HiddenMethod();
        HiddenMethodUserBreak();
    }

    [System.Diagnostics.DebuggerStepThroughAttribute]
    public static void StepThroughBp()
    {
        var a = 0;
        a++;
        var b = 1;
    }

    [System.Diagnostics.DebuggerStepThroughAttribute]
    public static void StepThroughUserBp()
    {
        System.Diagnostics.Debugger.Break();
    }

    public static void RunStepThrough()
    {
        StepThroughBp();
        StepThroughUserBp();
    }

    [System.Diagnostics.DebuggerNonUserCode]
    public static void NonUserCodeBp()
    {
        var a = 0;
        a++;
        var b = 1;
    }

    [System.Diagnostics.DebuggerNonUserCode]
    public static void NonUserCodeUserBp()
    {
        System.Diagnostics.Debugger.Break();
    }

    public static void RunNonUserCode()
    {
        NonUserCodeBp();
        NonUserCodeUserBp();
    }

    [System.Diagnostics.DebuggerStepperBoundary]
    public static void BoundaryBp()
    {
        var a = 5;
    }

    [System.Diagnostics.DebuggerStepperBoundary]
    public static void BoundaryUserBp()
    {
        System.Diagnostics.Debugger.Break();
    }

    [System.Diagnostics.DebuggerNonUserCode]
    public static void NonUserCodeForBoundaryEscape(Action boundaryTestFun)
    {
        boundaryTestFun();
    }

    public static void RunNoBoundary()
    {
        NonUserCodeForBoundaryEscape(DebuggerAttribute.BoundaryBp);
        NonUserCodeForBoundaryEscape(DebuggerAttribute.BoundaryUserBp);
    }

    [System.Diagnostics.DebuggerStepThroughAttribute]
    [System.Diagnostics.DebuggerHidden]
    public static void StepThroughWithHiddenBp()
    {
        var a = 9;
    }

    [System.Diagnostics.DebuggerStepThroughAttribute]
    [System.Diagnostics.DebuggerHidden]
    public static void StepThroughWithHiddenUserBp()
    {
        System.Diagnostics.Debugger.Break();
    }

    public static void RunStepThroughWithHidden()
    {
        StepThroughWithHiddenBp();
        StepThroughWithHiddenUserBp();
    }

    [System.Diagnostics.DebuggerStepThroughAttribute]
    [System.Diagnostics.DebuggerNonUserCode]
    public static void StepThroughWithNonUserCodeBp()
    {
        var a = 0;
        a++;
        var b = 1;
    }

    [System.Diagnostics.DebuggerStepThroughAttribute]
    [System.Diagnostics.DebuggerNonUserCode]
    public static void StepThroughWithNonUserCodeUserBp()
    {
        System.Diagnostics.Debugger.Break();
    }

    public static void RunStepThroughWithNonUserCode()
    {
        StepThroughWithNonUserCodeBp();
        StepThroughWithNonUserCodeUserBp();
    }

    [System.Diagnostics.DebuggerNonUserCode]
    [System.Diagnostics.DebuggerHidden]
    public static void NonUserCodeWithHiddenBp()
    {
        var a = 9;
    }

    [System.Diagnostics.DebuggerNonUserCode]
    [System.Diagnostics.DebuggerHidden]
    public static void NonUserCodeWithHiddenUserBp()
    {
        System.Diagnostics.Debugger.Break();
    }

    public static void RunNonUserCodeWithHidden()
    {
        NonUserCodeWithHiddenBp();
        NonUserCodeWithHiddenUserBp();
    }
}

public class DebugTypeFull
{
    public static void CallToEvaluateLocal()
    {
        var asm = System.Reflection.Assembly.LoadFrom("debugger-test-with-full-debug-type.dll");
        var myType = asm.GetType("DebuggerTests.ClassToInspectWithDebugTypeFull");
        var myMethod = myType.GetConstructor(new Type[] { });
        var a = myMethod.Invoke(new object[]{});
        System.Diagnostics.Debugger.Break();
    }
}

public class TestHotReloadUsingSDB {
        static System.Reflection.Assembly loadedAssembly;
        public static string LoadLazyHotReload(string asm_base64, string pdb_base64)
        {
            byte[] asm_bytes = Convert.FromBase64String(asm_base64);
            byte[] pdb_bytes = Convert.FromBase64String(pdb_base64);

            loadedAssembly = System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromStream(new System.IO.MemoryStream(asm_bytes), new System.IO.MemoryStream(pdb_bytes));
            var GUID = loadedAssembly.Modules.FirstOrDefault()?.ModuleVersionId.ToByteArray();
            return Convert.ToBase64String(GUID);
        }

        public static string GetModuleGUID()
        {
            var GUID = loadedAssembly.Modules.FirstOrDefault()?.ModuleVersionId.ToByteArray();
            return Convert.ToBase64String(GUID);
        }

        public static void RunMethod(string className, string methodName)
        {
            if (loadedAssembly is null)
                throw new InvalidOperationException($"{nameof(loadedAssembly)} is null!");
            var myType = loadedAssembly.GetType($"ApplyUpdateReferencedAssembly.{className}");
            var myMethod = myType.GetMethod(methodName);
            myMethod.Invoke(null, null);
        }
}

#region Default Interface Method
public interface IDefaultInterface
{
    public static string defaultInterfaceMember = "defaultInterfaceMember";

    string DefaultMethod()
    {
        string localString = "DefaultMethod()";
        DefaultInterfaceMethod.MethodForCallingFromDIM();
        return $"{localString} from IDefaultInterface";
    }

    int DefaultMethodToOverride()
    {
        int retValue = 10;
        return retValue;
    }

    async System.Threading.Tasks.Task DefaultMethodAsync()
    {
        string localString = "DefaultMethodAsync()";
        DefaultInterfaceMethod.MethodForCallingFromDIM();
        await System.Threading.Tasks.Task.FromResult(0);
    }
    static string DefaultMethodStatic()
    {
        string localString = "DefaultMethodStatic()";
        DefaultInterfaceMethod.MethodForCallingFromDIM();
        return $"{localString} from IDefaultInterface";
    }

    // cannot override the static method of the interface - skipping

    static async System.Threading.Tasks.Task DefaultMethodAsyncStatic()
    {
        string localString = "DefaultMethodAsyncStatic()";
        DefaultInterfaceMethod.MethodForCallingFromDIM();
        await System.Threading.Tasks.Task.FromResult(0);
    }
}

public interface IExtendIDefaultInterface : IDefaultInterface
{
    void DefaultMethod2(out string t)
    {
        string localString = "DefaultMethod2()";
        t = $"{localString} from IExtendIDefaultInterface";
    }

    int IDefaultInterface.DefaultMethodToOverride()
    {
        int retValue = 110;
        DefaultInterfaceMethod.MethodForCallingFromDIM();
        return retValue;
    }

    [System.Diagnostics.DebuggerHidden]
    void HiddenDefaultMethod()
    {
        var a = 9;
    }

    [System.Diagnostics.DebuggerStepThroughAttribute]
    void StepThroughDefaultMethod()
    {
        var a = 0;
    }

    [System.Diagnostics.DebuggerNonUserCode]
    void NonUserCodeDefaultMethod(Action boundaryTestFun = null)
    {
        if (boundaryTestFun != null)
            boundaryTestFun();
    }

    [System.Diagnostics.DebuggerStepperBoundary]
    void BoundaryBp()
    {
        var a = 15;
    }
}

public class DIMClass : IExtendIDefaultInterface
{
    public int dimClassMember = 123;
}

public static class DefaultInterfaceMethod
{
    public static void Evaluate()
    {
        IExtendIDefaultInterface extendDefaultInter = new DIMClass();
        string defaultFromIDefault = extendDefaultInter.DefaultMethod();
        int overrideFromIExtend = extendDefaultInter.DefaultMethodToOverride();
        extendDefaultInter.DefaultMethod2(out string default2FromIExtend);
    }

    public static async void EvaluateAsync()
    {
        IDefaultInterface defaultInter = new DIMClass();
        await defaultInter.DefaultMethodAsync();
    }

    public static void EvaluateHiddenAttr()
    {
        IExtendIDefaultInterface extendDefaultInter = new DIMClass();
        extendDefaultInter.HiddenDefaultMethod();
    }

    public static void EvaluateStepThroughAttr()
    {
        IExtendIDefaultInterface extendDefaultInter = new DIMClass();
        extendDefaultInter.StepThroughDefaultMethod();
    }

    public static void EvaluateNonUserCodeAttr()
    {
        IExtendIDefaultInterface extendDefaultInter = new DIMClass();
        extendDefaultInter.NonUserCodeDefaultMethod();
    }

    public static void EvaluateStepperBoundaryAttr()
    {
        IExtendIDefaultInterface extendDefaultInter = new DIMClass();
        extendDefaultInter.NonUserCodeDefaultMethod(extendDefaultInter.BoundaryBp);
    }

    public static void EvaluateStatic()
    {
        IExtendIDefaultInterface.DefaultMethodStatic();
    }

    public static async void EvaluateAsyncStatic()
    {
        await IExtendIDefaultInterface.DefaultMethodAsyncStatic();
    }

    public static void MethodForCallingFromDIM()
    {
        string text = "a place for pausing and inspecting DIM";
    }
}
#endregion
public class DebugWithDeletedPdb
{
    public static void Run()
    {
        var asm = System.Reflection.Assembly.LoadFrom("debugger-test-with-pdb-deleted.dll");
        var myType = asm.GetType("DebuggerTests.ClassWithPdbDeleted");
        var myMethod = myType.GetConstructor(new Type[] { });
        var exc = myMethod.Invoke(new object[]{});
        System.Diagnostics.Debugger.Break();
    }
}

public class DebugWithoutDebugSymbols
{
    public static void Run()
    {
        var asm = System.Reflection.Assembly.LoadFrom("debugger-test-without-debug-symbols.dll");
        var myType = asm.GetType("DebuggerTests.ClassWithoutDebugSymbols");
        var myMethod = myType.GetConstructor(new Type[] { });
        var exc = myMethod.Invoke(new object[]{});
        System.Diagnostics.Debugger.Break();
    }
}

public class AsyncGeneric
{
    public static async void TestAsyncGeneric1Parm()
    {
        var a = await GetAsyncMethod<int>(10);
        Console.WriteLine(a);
    }
    protected static async System.Threading.Tasks.Task<K> GetAsyncMethod<K>(K parm)
    {
        await System.Threading.Tasks.Task.Delay(1);
        System.Diagnostics.Debugger.Break();
        return parm;
    }

    public static async void TestKlassGenericAsyncGeneric()
    {
        var a = await MyKlass<bool, char>.GetAsyncMethod<int>(10);
        Console.WriteLine(a);
    }
    class MyKlass<T, L>
    {
        public static async System.Threading.Tasks.Task<K> GetAsyncMethod<K>(K parm)
        {
            await System.Threading.Tasks.Task.Delay(1);
            System.Diagnostics.Debugger.Break();
            return parm;
        }
        public static async System.Threading.Tasks.Task<K> GetAsyncMethod2<K, R>(K parm)
        {
            await System.Threading.Tasks.Task.Delay(1);
            System.Diagnostics.Debugger.Break();
            return parm;
        }
    }

    public static async void TestKlassGenericAsyncGeneric2()
    {
        var a = await MyKlass<bool>.GetAsyncMethod<int>(10);
        Console.WriteLine(a);
    }
    class MyKlass<T>
    {
        public static async System.Threading.Tasks.Task<K> GetAsyncMethod<K>(K parm)
        {
            await System.Threading.Tasks.Task.Delay(1);
            System.Diagnostics.Debugger.Break();
            return parm;
        }
        public static async System.Threading.Tasks.Task<K> GetAsyncMethod2<K, R>(K parm)
        {
            await System.Threading.Tasks.Task.Delay(1);
            System.Diagnostics.Debugger.Break();
            return parm;
        }
        public class MyKlassNested<U>
        {
            public static async System.Threading.Tasks.Task<K> GetAsyncMethod<K>(K parm)
            {
                await System.Threading.Tasks.Task.Delay(1);
                System.Diagnostics.Debugger.Break();
                return parm;
            }
        }
    }

    public static async void TestKlassGenericAsyncGeneric3()
    {
        var a = await MyKlass<bool>.GetAsyncMethod2<int, char>(10);
        Console.WriteLine(a);
    }
    public static async void TestKlassGenericAsyncGeneric4()
    {
        var a = await MyKlass<bool, double>.GetAsyncMethod2<int, char>(10);
        Console.WriteLine(a);
    }
    public static async void TestKlassGenericAsyncGeneric5()
    {
        var a = await MyKlass<bool>.MyKlassNested<int>.GetAsyncMethod<char>('1');
        Console.WriteLine(a);
    }
    public static async void TestKlassGenericAsyncGeneric6()
    {
        var a = await MyKlass<MyKlass<int>>.GetAsyncMethod<char>('1');
        Console.WriteLine(a);
    }
}

public class InspectIntPtr
{
    public static void Run()
    {
        IntPtr myInt = default;
        IntPtr myInt2 = new IntPtr(1);

        System.Diagnostics.Debugger.Break();
    }
}

public partial class HiddenSequencePointTest {
    public static void StepOverHiddenSP3()
    {
        MethodWithHiddenLinesAtTheEnd3();
        System.Diagnostics.Debugger.Break();
    }
    public static void MethodWithHiddenLinesAtTheEnd3()
    {
        Console.WriteLine ($"MethodWithHiddenLinesAtTheEnd");
#line hidden
        Console.WriteLine ($"debugger shouldn't be able to step here");
    }
#line default
}

public class ClassInheritsFromClassWithoutDebugSymbols : DebuggerTests.ClassWithoutDebugSymbolsToInherit
{
    public static void Run()
    {
        var myVar = new ClassInheritsFromClassWithoutDebugSymbols();
        myVar.CallMethod();
    }

    public void CallMethod()
    {
        System.Diagnostics.Debugger.Break();
    }
    public int myField2;
    public int myField;
}

[System.Diagnostics.DebuggerNonUserCode]
public class ClassNonUserCodeToInherit
{
    private int propA {get;}
    public int propB {get;}
    protected int propC {get;}
    private int d;
    public int e;
    protected int f;
    private int G
    {
        get {return f + 1;}
    }
    private int H => f;

    public ClassNonUserCodeToInherit()
    {
        propA = 10;
        propB = 20;
        propC = 30;
        d = 40;
        e = 50;
        f = 60;
        Console.WriteLine(propA);
        Console.WriteLine(propB);
        Console.WriteLine(propC);
        Console.WriteLine(d);
        Console.WriteLine(e);
        Console.WriteLine(f);
    }
}

public class ClassInheritsFromNonUserCodeClass : ClassNonUserCodeToInherit
{
    public static void Run()
    {
        var myVar = new ClassInheritsFromNonUserCodeClass();
        myVar.CallMethod();
    }

    public void CallMethod()
    {
        System.Diagnostics.Debugger.Break();
    }

    public int myField2;
    public int myField;
}

public class ClassInheritsFromNonUserCodeClassThatInheritsFromNormalClass : DebuggerTests.ClassNonUserCodeToInheritThatInheritsFromNormalClass
{
    public static void Run()
    {
        var myVar = new ClassInheritsFromNonUserCodeClassThatInheritsFromNormalClass();
        myVar.CallMethod();
    }

    public void CallMethod()
    {
        System.Diagnostics.Debugger.Break();
    }

    public int myField;
}
public class ReadOnlySpanTest
{
    struct S1 {
        internal double d1, d2;
    }

    ref struct R1 {
        internal ref double d1; // 8
        internal ref object o1; // += sizeof(MonoObject*)
        internal object o2;  // skip
        internal ref S1 s1; //+= instance_size(S1)
        internal S1 s2; // skip
        public R1(ref double d1, ref object o1, ref S1 s1) {
            this.d1 = ref d1;
            this.o1 = ref o1;
            this.s1 = ref s1;
        }
        internal double Run()
        {
            return s1.d1;
        }
    }

    ref struct R1Sample2 {
        public ref double d1;
        public R1Sample2(ref double d1) {
            this.d1 = ref d1;
        }
    }

    ref struct R2Sample2 {
        R1Sample2 r1;
        public R2Sample2 (ref double d1) {
            r1 = new R1Sample2 (ref d1);
        }

        public void Modify(double newDouble) {
            r1.d1 = newDouble;
        }

        public double Run() {
            return r1.d1;
        }
    }

    public static void Run()
    {
        Invoke(new string[] {"TEST"});
        ReadOnlySpan<object> var1 = new ReadOnlySpan<object>();
        System.Diagnostics.Debugger.Break();
    }
    public static void Invoke(object[] parameters)
    {
        CheckArguments(parameters);
    }
    public static void CheckArguments(ReadOnlySpan<object> parameters)
    {
        double d1 = 123.0;
        object o1 = new String("hi");
        var s1 = new S1();
        s1.d1 = 10;
        s1.d2 = 20;
        R1 myR1 = new R1(ref d1, ref o1, ref s1);
        myR1.o2 = new String("hi");
        myR1.s2 = new S1();
        myR1.s2.d1 = 30;
        myR1.s2.d2 = 40;
        double xyz = 123.0;
        R2Sample2 r2 = new R2Sample2(ref xyz);
        xyz = 456.0;
        System.Diagnostics.Debugger.Break();
    }
}

public class ToStringOverriden
{
    class ToStringOverridenA {
        public override string ToString()
        {
            return "helloToStringOverridenA";
        }
    }
    class ToStringOverridenB: ToStringOverridenA {}

    class ToStringOverridenC {}
    class ToStringOverridenD: ToStringOverridenC
    {
        public override string ToString()
        {
            return "helloToStringOverridenD";
        }
    }

    struct ToStringOverridenE
    {
        public override string ToString()
        {
            return "helloToStringOverridenE";
        }
    }

    class ToStringOverridenF
    {
        public override string ToString()
        {
            return "helloToStringOverridenF";
        }
    }
    class ToStringOverridenG: ToStringOverridenF
    {
        public override string ToString()
        {
            return "helloToStringOverridenG";
        }
    }

    class ToStringOverridenH
    {
        public override string ToString()
        {
            return "helloToStringOverridenH";
        }
        public string ToString(bool withParms = true)
        {
            return "helloToStringOverridenHWrong";
        }
    }

    class ToStringOverridenI
    {
        public string ToString(bool withParms = true)
        {
            return "helloToStringOverridenIWrong";
        }
    }

    struct ToStringOverridenJ
    {
        public override string ToString()
        {
            return "helloToStringOverridenJ";
        }
        public string ToString(bool withParms = true)
        {
            return "helloToStringOverridenJWrong";
        }
    }

    struct ToStringOverridenK
    {
        public string ToString(bool withParms = true)
        {
            return "helloToStringOverridenKWrong";
        }
    }

    record ToStringOverridenL
    {
        public override string ToString()
        {
            return "helloToStringOverridenL";
        }
    }

    record ToStringOverridenM
    {
        public string ToString(bool withParms = true)
        {
            return "helloToStringOverridenMWrong";
        }
    }

    record ToStringOverridenN
    {
        public override string ToString()
        {
            return "helloToStringOverridenN";
        }
        public string ToString(bool withParms = true)
        {
            return "helloToStringOverridenNWrong";
        }
    }

    public override string ToString()
    {
        return "helloToStringOverriden";
    }
    public static void Run()
    {
        var a = new ToStringOverriden();
        var b = new ToStringOverridenB();
        var c = new ToStringOverridenD();
        var d = new ToStringOverridenE();
        ToStringOverridenA e = new ToStringOverridenB();
        object f = new ToStringOverridenB();
        var g = new ToStringOverridenG();
        var h = new ToStringOverridenH();
        var i = new ToStringOverridenI();
        var j = new ToStringOverridenJ();
        var k = new ToStringOverridenK();
        var l = new ToStringOverridenL();
        var m = new ToStringOverridenM();
        var n = new ToStringOverridenN();
        System.Diagnostics.Debugger.Break();
    }
}
public class TestLoadSymbols
{
    public static void Run()
    {
        var array = new Newtonsoft.Json.Linq.JArray();
        var text = new Newtonsoft.Json.Linq.JValue("Manual text");
        var date = new Newtonsoft.Json.Linq.JValue(new DateTime(2000, 5, 23));

        System.Diagnostics.Debugger.Break();

        array.Add(text);
        array.Add(date);
    }
}

public class MultiThreadedTest
{
    public static void Run()
    {
        System.Collections.Generic.List<System.Threading.Thread> myThreads = new();
        for (int i = 0 ; i < 3; i++)
        {
            var t = new System.Threading.Thread (() => Write("y"));
            myThreads.Add(t);
            t.Start();
        }
        foreach (System.Threading.Thread curThread in myThreads)
        {
            curThread.Join();
        }
    }
    static void Write(string input)
    {
        var currentThread = System.Threading.Thread.CurrentThread.ManagedThreadId;
        Console.WriteLine($"Thread:{currentThread} - {input}");
    }
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class CustomAttribute<TInterface> : Attribute
{
}

[Custom<GenericCustomAttributeDecoratedClassInheritsFromClassWithoutDebugSymbols>]
public class GenericCustomAttributeDecoratedClassInheritsFromClassWithoutDebugSymbols : DebuggerTests.ClassWithoutDebugSymbolsToInherit
{
    public static void Run()
    {
        var myVar = new GenericCustomAttributeDecoratedClassInheritsFromClassWithoutDebugSymbols();
        myVar.CallMethod();
    }

    public void CallMethod()
    {
        System.Diagnostics.Debugger.Break();
    }
    public int myField2;
    public int myField;
}

[Custom<GenericCustomAttributeDecoratedClassInheritsFromNonUserCodeClass>]
public class GenericCustomAttributeDecoratedClassInheritsFromNonUserCodeClass : ClassNonUserCodeToInherit
{
    public static void Run()
    {
        var myVar = new GenericCustomAttributeDecoratedClassInheritsFromNonUserCodeClass();
        myVar.CallMethod();
    }

    public void CallMethod()
    {
        System.Diagnostics.Debugger.Break();
    }

    public int myField2;
    public int myField;
}

[Custom<GenericCustomAttributeDecoratedClassInheritsFromNonUserCodeClassThatInheritsFromNormalClass>]
public class GenericCustomAttributeDecoratedClassInheritsFromNonUserCodeClassThatInheritsFromNormalClass : DebuggerTests.ClassNonUserCodeToInheritThatInheritsFromNormalClass
{
    public static void Run()
    {
        var myVar = new GenericCustomAttributeDecoratedClassInheritsFromNonUserCodeClassThatInheritsFromNormalClass();
        myVar.CallMethod();
    }

    public void CallMethod()
    {
        System.Diagnostics.Debugger.Break();
    }

    public int myField;
}
