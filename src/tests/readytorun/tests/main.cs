// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
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

struct LocallyDefinedStructure {}


static class OpenClosedDelegateExtension
{
    public static string OpenClosedDelegateTarget(this string x, string foo)
    {
        return x + ", " + foo;
    }
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

        // V2 adds override of ToString
        if (typeof(MyStructWithVirtuals).GetMethod("ToString").DeclaringType == typeof(MyStructWithVirtuals))
        {
            // Make sure the constrained call to ToString doesn't box
            var mystruct = new MyStructWithVirtuals();
            mystruct.ToString();
            Assert.AreEqual(mystruct.X, "Overridden");
        }
    }

    static void TestMovedVirtualMethods()
    {
        var o = new MyChildClass();

        Assert.AreEqual(o.MovedToBaseClass(), "MovedToBaseClass");
        Assert.AreEqual(o.ChangedToVirtual(), "ChangedToVirtual");

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


    static void TestConstrainedMethodCalls()
    {
        using (MyStruct s = new MyStruct())
        {
             ((Object)s).ToString();
        }

        // Enum.GetHashCode optimization requires special treatment
        // in native signature encoding
        MyEnum e = MyEnum.Apple;
        e.GetHashCode();
    }

    static void TestConstrainedMethodCalls_Unsupported()
    {
        MyStruct s = new MyStruct();
        s.ToString();
    }

    static void TestInterop()
    {
        MyClass.NativeMethod();

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

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static void TestGenericNonVirtualMethod()
    {
        var c = new MyChildGeneric<string>();
        Assert.AreEqual(CallGeneric(c), "MyGeneric.NonVirtualMethod");
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static string CallGeneric<T>(MyGeneric<T, T> g)
    {
        return g.NonVirtualMethod();
    }

    static void TestGenericOverStruct()
    {
        var o1 = new MyGeneric<String, MyGrowingStruct>();
        Assert.AreEqual(o1.GenericVirtualMethod < MyChangingStruct, IEnumerable<Program>>(),
            "System.StringMyGrowingStructMyChangingStructSystem.Collections.Generic.IEnumerable`1[Program]");

        var o2 = new MyChildGeneric<MyChangingStruct>();
        Assert.AreEqual(o2.MovedToBaseClass<MyGrowingStruct>(), typeof(List<MyGrowingStruct>).ToString());
        Assert.AreEqual(o2.ChangedToVirtual<MyGrowingStruct>(), typeof(List<MyGrowingStruct>).ToString());
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

    class MyLoadContext : AssemblyLoadContext
    {
        // If running in a collectible context, make the MyLoadContext collectible too so that it doesn't prevent
        // unloading.
        public MyLoadContext() : base(AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly()).IsCollectible)
        {
        }

        public void TestMultipleLoads()
        {
            Assembly a = LoadFromAssemblyPath(Path.Combine(Directory.GetCurrentDirectory(), "test.dll"));
            Assert.AreEqual(AssemblyLoadContext.GetLoadContext(a), this);
        }

        protected override Assembly Load(AssemblyName an)
        {
            throw new NotImplementedException();
        }
    }

    static void TestMultipleLoads()
    {
        // Runtime should be able to load the same R2R image in another load context,
        // even though it will be treated as an IL-only image.
        new MyLoadContext().TestMultipleLoads();
    }

    static void TestFieldLayoutNGenMixAndMatch()
    {
        // This test is verifying consistent field layout when ReadyToRun images are combined with NGen images
        // "ngen install /nodependencies main.exe" to exercise the interesting case
        var o = new ByteChildClass(67);
        Assert.AreEqual(o.ChildByte, (byte)67);
    }

    static void TestOpenClosedDelegate()
    {
        // This test is verifying the fixups for open vs. closed delegate created against the same target
        // method are encoded correctly.

        Func<string, string, object> idOpen = OpenClosedDelegateExtension.OpenClosedDelegateTarget;
        Assert.AreEqual(idOpen("World", "foo"), "World, foo");

        Func<string, object> idClosed = "World".OpenClosedDelegateTarget;
        Assert.AreEqual(idClosed("hey"), "World, hey");
    }

    static void GenericLdtokenFieldsTest()
    {
        Func<FieldInfo, string> FieldFullName = (fi) => fi.FieldType + " " + fi.DeclaringType.ToString() + "::" + fi.Name;

        IFieldGetter getter1 = new FieldGetter<string>();
        IFieldGetter getter2 = new FieldGetter<object>();
        IFieldGetter getter3 = new FieldGetter<int>();

        foreach (var instArg in new Type[]{typeof(String), typeof(object), typeof(int)})
        {
            IFieldGetter getter = (IFieldGetter)Activator.CreateInstance(typeof(FieldGetter<>).MakeGenericType(instArg));

            string expectedField1 = "System.Int32 Gen`1[???]::m_Field1".Replace("???", instArg.ToString());
            string expectedField2 = "System.String Gen`1[???]::m_Field2".Replace("???", instArg.ToString());
            string expectedField3 = "??? Gen`1[???]::m_Field3".Replace("???", instArg.ToString());
            string expectedField4 = "System.Collections.Generic.List`1[???] Gen`1[???]::m_Field4".Replace("???", instArg.ToString());
            string expectedField5 = "System.Collections.Generic.KeyValuePair`2[???,System.Int32] Gen`1[???]::m_Field5".Replace("???", instArg.ToString());

            string expectedDllField1 = "System.String MyGeneric`2[???,???]::m_Field1".Replace("???", instArg.ToString());
            string expectedDllField2 = "??? MyGeneric`2[???,???]::m_Field2".Replace("???", instArg.ToString());
            string expectedDllField3 = "System.Collections.Generic.List`1[???] MyGeneric`2[???,???]::m_Field3".Replace("???", instArg.ToString());
            string expectedDllField4 = "System.Collections.Generic.KeyValuePair`2[???,System.Int32] MyGeneric`2[???,???]::m_Field4".Replace("???", instArg.ToString());
            string expectedDllField5 = "System.Int32 MyGeneric`2[???,???]::m_Field5".Replace("???", instArg.ToString());

            Assert.AreEqual(expectedField1, FieldFullName(getter.GetGenT_Field1()));
            Assert.AreEqual(expectedField2, FieldFullName(getter.GetGenT_Field2()));
            Assert.AreEqual(expectedField3, FieldFullName(getter.GetGenT_Field3()));
            Assert.AreEqual(expectedField4, FieldFullName(getter.GetGenT_Field4()));
            Assert.AreEqual(expectedField5, FieldFullName(getter.GetGenT_Field5()));

            Assert.AreEqual(expectedDllField1, FieldFullName(getter.GetGenDllT_Field1()));
            Assert.AreEqual(expectedDllField2, FieldFullName(getter.GetGenDllT_Field2()));
            Assert.AreEqual(expectedDllField3, FieldFullName(getter.GetGenDllT_Field3()));
            Assert.AreEqual(expectedDllField4, FieldFullName(getter.GetGenDllT_Field4()));
            Assert.AreEqual(expectedDllField5, FieldFullName(getter.GetGenDllT_Field5()));
        }
    }

    static void RVAFieldTest()
    {
        ReadOnlySpan<byte> value = new byte[] { 9, 8, 7, 6, 5, 4, 3, 2, 1, 0 };
        for (byte i = 0; i < value.Length; i++)
            Assert.AreEqual(value[i], (byte)(9 - i));
    }

    static void TestLoadR2RImageFromByteArray()
    {
        Assembly assembly1 = typeof(Program).Assembly;

        byte[] array = File.ReadAllBytes(assembly1.Location);
        Assembly assembly2 = Assembly.Load(array);

        Assert.AreEqual(assembly2.FullName, assembly1.FullName);
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static void TestILBodyChange()
    {
        int actualMethodCallResult = (int)typeof(ILInliningTest).GetMethod("TestDifferentIntValue").Invoke(null, new object[]{});
        Console.WriteLine(actualMethodCallResult);
        Assert.AreEqual(ILInliningTest.TestDifferentIntValue(), actualMethodCallResult);
    }

    static void RunAllTests()
    {
        Console.WriteLine("TestVirtualMethodCalls");
        TestVirtualMethodCalls();
        Console.WriteLine("TestMovedVirtualMethod");
        TestMovedVirtualMethods();

        Console.WriteLine("TestConstrainedMethodCalls");
        TestConstrainedMethodCalls();

        Console.WriteLine("TestConstrainedMethodCalls_Unsupported");
        TestConstrainedMethodCalls_Unsupported();

        Console.WriteLine("TestInterop");
        TestInterop();

        Console.WriteLine("TestStaticFields");
        TestStaticFields();

        Console.WriteLine("TestPreInitializedArray");
        TestPreInitializedArray();

        Console.WriteLine("TestMultiDimmArray");
        TestMultiDimmArray();

        Console.WriteLine("TestGenericVirtualMethod");
        TestGenericVirtualMethod();
        Console.WriteLine("TestMovedGenericVirtualMethod");
        TestMovedGenericVirtualMethod();
        Console.WriteLine("TestGenericNonVirtualMethod");
        TestGenericNonVirtualMethod();

        Console.WriteLine("TestGenericOverStruct");
        TestGenericOverStruct();

        Console.WriteLine("TestInstanceFields");
        TestInstanceFields();

        Console.WriteLine("TestInstanceFieldsWithLayout");
        TestInstanceFieldsWithLayout();

        Console.WriteLine("TestInheritingFromGrowingBase");
        TestInheritingFromGrowingBase();

        Console.WriteLine("TestGrowingStruct");
        TestGrowingStruct();
        Console.WriteLine("TestChangingStruct");
        TestChangingStruct();
        Console.WriteLine("TestChangingHFAStruct");
        TestChangingHFAStruct();

        Console.WriteLine("TestGetType");
        TestGetType();

        Console.WriteLine("TestMultipleLoads");
        TestMultipleLoads();

        Console.WriteLine("TestFieldLayoutNGenMixAndMatch");
        TestFieldLayoutNGenMixAndMatch();

        Console.WriteLine("TestStaticBaseCSE");
        TestStaticBaseCSE();

        Console.WriteLine("TestIsInstCSE");
        TestIsInstCSE();

        Console.WriteLine("TestCastClassCSE");
        TestCastClassCSE();

        Console.WriteLine("TestRangeCheckElimination");
        TestRangeCheckElimination();

        Console.WriteLine("TestOpenClosedDelegate");
        TestOpenClosedDelegate();

        Console.WriteLine("GenericLdtokenFieldsTest");
        GenericLdtokenFieldsTest();

        Console.WriteLine("RVAFieldTest");
        RVAFieldTest();

//        Disable for https://github.com/dotnet/runtime/issues/71507
//        Console.WriteLine("TestLoadR2RImageFromByteArray");
//        TestLoadR2RImageFromByteArray();

        Console.WriteLine("TestILBodyChange");
        TestILBodyChange();
        ILInliningVersioningTest<LocallyDefinedStructure>.RunAllTests(typeof(Program).Assembly);
    }

    static int Main()
    {
        // Run all tests 3x times to exercise both slow and fast paths work
        for (int i = 0; i < 3; i++)
           RunAllTests();

        if (!Assert.HasAssertFired)
            Console.WriteLine("PASSED");
        else
            Console.WriteLine("FAILED");
        return Assert.HasAssertFired ? 1 : 100;
    }

    static int s;
}
