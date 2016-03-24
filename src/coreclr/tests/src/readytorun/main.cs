// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
#if CORECLR
using System.Runtime.Loader;
#endif
using System.Reflection;
using System.IO;

class InstanceFieldTest : MyClass
{
    public int Value;
}

class InstanceFieldTest2 : InstanceFieldTest
{
    public int Value2;
}

[StructLayout(LayoutKind.Sequential)]
class InstanceFieldTestWithLayout : MyClassWithLayout
{
    public int Value;
}

class GrowingBase
{
    MyGrowingStruct s;
}

class InheritingFromGrowingBase : GrowingBase
{
    public int x;
}

class Program
{
    static void TestVirtualMethodCalls()
    {
         var o = new MyClass();
         Assert.AreEqual(o.VirtualMethod(), "Virtual method result");

         var iface = (IMyInterface)o;
         Assert.AreEqual(iface.InterfaceMethod(" "), "Interface result");
         Assert.AreEqual(MyClass.TestInterfaceMethod(iface, "+"), "Interface+result");
    }

    static void TestMovedVirtualMethods()
    {
        var o = new MyChildClass();

        Assert.AreEqual(o.MovedToBaseClass(), "MovedToBaseClass");
        Assert.AreEqual(o.ChangedToVirtual(), "ChangedToVirtual");

        if (!LLILCJitEnabled)
        {
            o = null;

            try
            {
                o.MovedToBaseClass();
            }
            catch (NullReferenceException)
            {
                try
                {
                    o.ChangedToVirtual();
                }
                catch (NullReferenceException)
                {
                    return;
                }
            }

            Assert.AreEqual("NullReferenceException", "thrown");
        }
    }


    static void TestConstrainedMethodCalls()
    {
        using (MyStruct s = new MyStruct())
        {
             ((Object)s).ToString();
        }
    }

    static void TestConstrainedMethodCalls_Unsupported()
    {
        MyStruct s = new MyStruct();
        s.ToString();
    }

    static void TestInterop()
    {
        // Verify both intra-module and inter-module PInvoke interop
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            MyClass.GetTickCount();
        }
        else
        {
            MyClass.GetCurrentThreadId();
        }

        MyClass.TestInterop();
    }

    static void TestStaticFields()
    {
        MyClass.StaticObjectField = 894;
        MyClass.StaticLongField = 4392854;
        MyClass.StaticNullableGuidField = new Guid("0D7E505F-E767-4FEF-AEEC-3243A3005673");
        MyClass.ThreadStaticStringField = "Hello";
        MyClass.ThreadStaticIntField = 735;
        MyClass.ThreadStaticDateTimeField = new DateTime(2011, 1, 1);

        MyClass.TestStaticFields();

#if false // TODO: Enable once LDFTN is supported
        Task.Run(() => { 
           MyClass.ThreadStaticStringField = "Garbage";
           MyClass.ThreadStaticIntField = 0xBAAD;
           MyClass.ThreadStaticDateTimeField = DateTime.Now;
        }).Wait();
#endif

        Assert.AreEqual(MyClass.StaticObjectField, 894 + 12345678 /* + 1234 */);
        Assert.AreEqual(MyClass.StaticLongField, (long)(4392854 * 456 /* * 45 */));
        Assert.AreEqual(MyClass.StaticNullableGuidField, null);
        Assert.AreEqual(MyClass.ThreadStaticStringField, "HelloWorld");
        Assert.AreEqual(MyClass.ThreadStaticIntField, 735/78);
        Assert.AreEqual(MyClass.ThreadStaticDateTimeField, new DateTime(2011, 1, 1) + new TimeSpan(123));
    }

    static void TestPreInitializedArray()
    {
        var a = new int[] { 1, 2, 4, 8, 16, 32, 64, 128, 256, 512 };

        int sum = 0;
        foreach (var e in a) sum += e;
        Assert.AreEqual(sum, 1023);
    }

    static void TestMultiDimmArray()
    {
       var a = new int[2,3,4];
       a[0,1,2] = a[0,0,0] + a[1,1,1];
       a.ToString();
    }

    static void TestGenericVirtualMethod()
    {
        var o = new MyGeneric<String, Object>();
        Assert.AreEqual(o.GenericVirtualMethod<Program, IEnumerable<String>>(),
            "System.StringSystem.ObjectProgramSystem.Collections.Generic.IEnumerable`1[System.String]");
    }

    static void TestMovedGenericVirtualMethod()
    {
        var o = new MyChildGeneric<Object>();

        Assert.AreEqual(o.MovedToBaseClass<WeakReference>(), typeof(List<WeakReference>).ToString());
        Assert.AreEqual(o.ChangedToVirtual<WeakReference>(), typeof(List<WeakReference>).ToString());

        if (!LLILCJitEnabled)
        {
            o = null;

            try
            {
                o.MovedToBaseClass<WeakReference>();
            }
            catch (NullReferenceException)
            {
                try
                {
                    o.ChangedToVirtual<WeakReference>();
                }
                catch (NullReferenceException)
                {
                    return;
                }
            }

            Assert.AreEqual("NullReferenceException", "thrown");
        }
    }

    static void TestInstanceFields()
    {
        var t = new InstanceFieldTest2();
        t.Value = 123;
        t.Value2 = 234;
        t.InstanceField = 345;

        Assert.AreEqual(typeof(InstanceFieldTest).GetRuntimeField("Value").GetValue(t), 123);
        Assert.AreEqual(typeof(InstanceFieldTest2).GetRuntimeField("Value2").GetValue(t), 234);
        Assert.AreEqual(typeof(MyClass).GetRuntimeField("InstanceField").GetValue(t), 345);
    }

    static void TestInstanceFieldsWithLayout()
    {
        var t = new InstanceFieldTestWithLayout();
        t.Value = 123;

        Assert.AreEqual(typeof(InstanceFieldTestWithLayout).GetRuntimeField("Value").GetValue(t), 123);
    }

    static void TestInheritingFromGrowingBase()
    {
        var o = new InheritingFromGrowingBase();
        o.x = 6780;
        Assert.AreEqual(typeof(InheritingFromGrowingBase).GetRuntimeField("x").GetValue(o), 6780);
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static void TestGrowingStruct()
    {
        MyGrowingStruct s = MyGrowingStruct.Construct();
        MyGrowingStruct.Check(ref s);
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static void TestChangingStruct()
    {
        MyChangingStruct s = MyChangingStruct.Construct();
        s.x++;
        MyChangingStruct.Check(ref s);
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static void TestChangingHFAStruct()
    {
        MyChangingHFAStruct s = MyChangingHFAStruct.Construct();
        MyChangingHFAStruct.Check(s);
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static void TestGetType()
    {
        new MyClass().GetType().ToString();
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static void TestStaticBaseCSE()
    {
        // There should be just one call to CORINFO_HELP_READYTORUN_STATIC_BASE
        // in the generated code.
        s++;
        s++;
        Assert.AreEqual(s, 2);
        s = 0;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static void TestIsInstCSE()
    {
        // There should be just one call to CORINFO_HELP_READYTORUN_ISINSTANCEOF
        // in the generated code.
        object o1 = (s < 1) ? (object)"foo" : (object)1;
        Assert.AreEqual(o1 is string, true);
        Assert.AreEqual(o1 is string, true);
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static void TestCastClassCSE()
    {
        // There should be just one call to CORINFO_HELP_READYTORUN_CHKCAST
        // in the generated code.
        object o1 = (s < 1) ? (object)"foo" : (object)1;
        string str1 = (string)o1;
        string str2 = (string)o1;
        Assert.AreEqual(str1, str2);
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static void TestRangeCheckElimination()
    {
        // Range checks for array accesses should be eliminated by the compiler.
        int[] array = new int[5];
        array[2] = 2;
        Assert.AreEqual(array[2], 2);
    }

#if CORECLR
    class MyLoadContext : AssemblyLoadContext
    {
        public MyLoadContext()
        {
        }

        public void TestMultipleLoads()
        {
            Assembly a = LoadFromAssemblyPath(Path.Combine(Directory.GetCurrentDirectory(), "test.ni.dll"));
            Assert.AreEqual(AssemblyLoadContext.GetLoadContext(a), this);
        }

        protected override Assembly Load(AssemblyName an)
        {
            throw new NotImplementedException();
        }
    }

    static void TestMultipleLoads()
    {
        if (!LLILCJitEnabled) {
            try
            {
                new MyLoadContext().TestMultipleLoads();
            }
            catch (FileLoadException e)
            {
                Assert.AreEqual(e.ToString().Contains("Native image cannot be loaded multiple times"), true);
                return;
            }

            Assert.AreEqual("FileLoadException", "thrown");
        }
    }
#endif

    static void TestFieldLayoutNGenMixAndMatch()
    {
        // This test is verifying consistent field layout when ReadyToRun images are combined with NGen images
        // "ngen install /nodependencies main.exe" to exercise the interesting case
        var o = new ByteChildClass(67);
        Assert.AreEqual(o.ChildByte, (byte)67);
    }

    static void RunAllTests()
    {
        TestVirtualMethodCalls();
        TestMovedVirtualMethods();

        TestConstrainedMethodCalls();

        TestConstrainedMethodCalls_Unsupported();

        TestInterop();

        TestStaticFields();

        TestPreInitializedArray();

        TestMultiDimmArray();

        TestGenericVirtualMethod();
        TestMovedGenericVirtualMethod();

        TestInstanceFields();

        TestInstanceFieldsWithLayout();

        TestInheritingFromGrowingBase();

        TestGrowingStruct();
        TestChangingStruct();
        TestChangingHFAStruct();

        TestGetType();

#if CORECLR
        TestMultipleLoads();
#endif

        TestFieldLayoutNGenMixAndMatch();

        TestStaticBaseCSE();

        TestIsInstCSE();

        TestCastClassCSE();

        TestRangeCheckElimination();
    }

    static int Main()
    {
        // Code compiled by LLILC jit can't catch exceptions yet so the tests
        // don't throw them if LLILC jit is enabled. This should be removed once
        // exception catching is supported by LLILC jit.
        string AltJitName = System.Environment.GetEnvironmentVariable("complus_altjitname");
        LLILCJitEnabled =
            ((AltJitName != null) && AltJitName.ToLower().StartsWith("llilcjit") &&
             ((System.Environment.GetEnvironmentVariable("complus_altjit") != null) ||
              (System.Environment.GetEnvironmentVariable("complus_altjitngen") != null)));

        // Run all tests 3x times to exercise both slow and fast paths work
        for (int i = 0; i < 3; i++)
           RunAllTests();

        Console.WriteLine("PASSED");
        return Assert.HasAssertFired ? 1 : 100;
    }

    static bool LLILCJitEnabled;

    static int s;
}
