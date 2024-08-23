// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.IO;

public static class Assert
{
    public static bool HasAssertFired;

    public static void AreEqual(Object actual, Object expected)
    {
        if (!(actual == null && expected == null) && !actual.Equals(expected))
        {
            Console.WriteLine("Not equal!");
            Console.WriteLine("actual   = " + actual.ToString());
            Console.WriteLine("expected = " + expected.ToString());
            HasAssertFired = true;
        }
    }
}

public interface IMyInterface
{
#if V2
    // Adding new methods to interfaces is incompatible change, but we will make sure that it works anyway
    void NewInterfaceMethod();
#endif

    string InterfaceMethod(string s);
}

public class MyClass : IMyInterface
{
#if V2
    public int _field1;
    public int _field2;
    public int _field3;
#endif
    public int InstanceField;

#if V2
    public static Object StaticObjectField2;

    [ThreadStatic] public static String ThreadStaticStringField2;

    [ThreadStatic] public static int ThreadStaticIntField;

    public static Nullable<Guid> StaticNullableGuidField;

    public static Object StaticObjectField;

    [ThreadStatic] public static int ThreadStaticIntField2;

    public static long StaticLongField;

    [ThreadStatic] public static DateTime ThreadStaticDateTimeField2;

    public static long StaticLongField2;

    [ThreadStatic] public static DateTime ThreadStaticDateTimeField;

    public static Nullable<Guid> StaticNullableGuidField2;

    [ThreadStatic] public static String ThreadStaticStringField;
#else
    public static Object StaticObjectField;

    public static long StaticLongField;

    public static Nullable<Guid> StaticNullableGuidField;

    [ThreadStatic] public static String ThreadStaticStringField;

    [ThreadStatic] public static int ThreadStaticIntField;

    [ThreadStatic] public static DateTime ThreadStaticDateTimeField;
#endif

   public MyClass()
   {
   }

#if V2
   public virtual void NewVirtualMethod()
   {
   }

   public virtual void NewInterfaceMethod()
   {
       throw new Exception();
   }
#endif

   public virtual string VirtualMethod()
   {
       return "Virtual method result";
   }

   public virtual string InterfaceMethod(string s)
   {
       return "Interface" + s + "result";
   }

   public static string TestInterfaceMethod(IMyInterface i, string s)
   {
       return i.InterfaceMethod(s);
   }

   public static void TestStaticFields()
   {
       StaticObjectField = (int)StaticObjectField + 12345678;

       StaticLongField *= 456;

       Assert.AreEqual(StaticNullableGuidField, new Guid("0D7E505F-E767-4FEF-AEEC-3243A3005673"));
       StaticNullableGuidField = null;

       ThreadStaticStringField += "World";

       ThreadStaticIntField /= 78;

       ThreadStaticDateTimeField = ThreadStaticDateTimeField + new TimeSpan(123);

       MyGeneric<int,int>.ThreadStatic = new Object();

#if false // TODO: Enable once LDFTN is supported
       // Do some operations on static fields on a different thread to verify that we are not mixing thread-static and non-static
       Task.Run(() => {

           StaticObjectField = (int)StaticObjectField + 1234;

           StaticLongField *= 45;

           ThreadStaticStringField = "Garbage";

           ThreadStaticIntField = 0xBAAD;

           ThreadStaticDateTimeField = DateTime.Now;

        }).Wait();
#endif
   }

   [DllImport("nativelibrary")]
   public extern static int NativeMethod();

   static public void TestInterop()
   {
        NativeMethod();
   }

#if V2
    public string MovedToBaseClass()
    {
        return "MovedToBaseClass";
    }
#endif

#if V2
    public virtual string ChangedToVirtual()
    {
        return null;
    }
#else
   public string ChangedToVirtual()
   {
       return "ChangedToVirtual";
   }
#endif

    public static void ThrowIOE()
    {
#if !V2
        throw new InvalidOperationException();
#endif
    }
}

public class MyChildClass : MyClass
{
    public MyChildClass()
    {
    }

#if !V2
    public string MovedToBaseClass()
    {
        return "MovedToBaseClass";
    }
#endif

#if V2
    public override string ChangedToVirtual()
    {
        return "ChangedToVirtual";
    }
#endif
}


public struct MyStruct : IDisposable
{
   int x;

#if V2
   void IDisposable.Dispose()
   {
   }
#else
   public void Dispose()
   {
   }
#endif
}

public class MyGeneric<T,U>
{
#if V2
    public object m_unused1;
    public string m_Field1;

    public object m_unused2;
    public T m_Field2;

    public object m_unused3;
    public List<T> m_Field3;

    static public object m_unused4;
    static public KeyValuePair<T, int> m_Field4;

    static public object m_unused5;
    static public int m_Field5;

    public object m_unused6;
    static public object m_unused7;
#else
    public string m_Field1;
    public T m_Field2;
    public List<T> m_Field3;
    static public KeyValuePair<T, int> m_Field4;
    static public int m_Field5;
#endif

    [ThreadStatic] public static Object ThreadStatic;

    public MyGeneric()
    {
    }

    public virtual string GenericVirtualMethod<V,W>()
    {
        return typeof(T).ToString() + typeof(U).ToString() + typeof(V).ToString() + typeof(W).ToString();
    }

#if V2
    public string MovedToBaseClass<W>()
    {
        typeof(Dictionary<W,W>).ToString();
        return typeof(List<W>).ToString();
    }
#endif

#if V2
    public virtual string ChangedToVirtual<W>()
    {
        return null;
    }
#else
    public string ChangedToVirtual<W>()
    {
        return typeof(List<W>).ToString();
    }
#endif

    public string NonVirtualMethod()
    {
        return "MyGeneric.NonVirtualMethod";
    }
}

public class MyChildGeneric<T> : MyGeneric<T,T>
{
    public MyChildGeneric()
    {
    }

#if !V2
    public string MovedToBaseClass<W>()
    {
        return typeof(List<W>).ToString();
    }
#endif

#if V2
    public override string ChangedToVirtual<W>()
    {
        typeof(Dictionary<Int32, W>).ToString();
        return typeof(List<W>).ToString();
    }
#endif
}

[StructLayout(LayoutKind.Sequential)]
public class MyClassWithLayout
{
#if V2
    public int _field1;
    public int _field2;
    public int _field3;
#endif
}

public struct MyGrowingStruct
{
    int x;
    int y;
#if V2
    Object o1;
    Object o2;
#endif

    static public MyGrowingStruct Construct()
    {
        return new MyGrowingStruct() { x = 111, y = 222 };
    }

    public static void Check(ref MyGrowingStruct s)
    {
        Assert.AreEqual(s.x, 111);
        Assert.AreEqual(s.y, 222);
    }
}

public struct MyChangingStruct
{
#if V2
    public int y;
    public int x;
#else
    public int x;
    public int y;
#endif

    static public MyChangingStruct Construct()
    {
        return new MyChangingStruct() { x = 111, y = 222 };
    }

    public static void Check(ref MyChangingStruct s)
    {
        Assert.AreEqual(s.x, 112);
        Assert.AreEqual(s.y, 222);
    }
}

public struct MyChangingHFAStruct
{
#if V2
    float x;
    float y;
#else
    int x;
    int y;
#endif
    static public MyChangingHFAStruct Construct()
    {
        return new MyChangingHFAStruct() { x = 12, y = 23 };
    }

    public static void Check(MyChangingHFAStruct s)
    {
#if V2
        Assert.AreEqual(s.x, 12.0f);
        Assert.AreEqual(s.y, 23.0f);
#else
        Assert.AreEqual(s.x, 12);
        Assert.AreEqual(s.y, 23);
#endif
    }
}

public struct MyStructWithVirtuals
{
    public string X;

#if V2
    public override string ToString()
    {
        X = "Overridden";
        return base.ToString();
    }
#endif
}

public class ByteBaseClass : List<byte>
{
    public byte BaseByte;
}
public class ByteChildClass : ByteBaseClass
{
    public byte ChildByte;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public ByteChildClass(byte value)
    {
        ChildByte = 67;
    }
}

public enum MyEnum
{
    Apple = 1,
    Banana = 2,
    Orange = 3
}

public class ILInliningTest
{
    public static int TestDifferentIntValue()
    {
        // This test is used to detect when the if the CHECK_ILBODY fixup works
#if V2
        return 2;
#else
        return 1;
#endif
    }
}

public class NonGenericClass{}

static class OpenClosedDelegateExtensionTest
{
    public static string OpenClosedDelegateTarget(this string x, string foo)
    {
        return x + ", " + foo;
    }
}

public interface IDefaultVsExactStaticVirtual
{
    static virtual string Method() =>
#if V2
        "Error - IDefaultVsExactStaticVirtual.Method shouldn't be used in V2"
#else
        "DefaultVsExactStaticVirtualMethod"
#endif
    ;
}

public class DefaultVsExactStaticVirtualClass : IDefaultVsExactStaticVirtual
{
#if V2
    static string IDefaultVsExactStaticVirtual.Method() => "DefaultVsExactStaticVirtualMethod";
#endif
}

// Test dependent versioning details
public class ILInliningVersioningTest<T>
{
    // These tests are designed to observe that the fixups we use for versioning continue to respect version boundaries when the code has been copied
    // to another binary and a CHECK_ILBODY fixup is in use (and that fixup passes, but some of the other dependencies may change)
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


    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static void TestVirtualMethodCalls()
    {
         var o = new MyClass();
         Assert.AreEqual(o.VirtualMethod(), "Virtual method result");

         var iface = (IMyInterface)o;
         Assert.AreEqual(iface.InterfaceMethod(" "), "Interface result");
         Assert.AreEqual(MyClass.TestInterfaceMethod(iface, "+"), "Interface+result");
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static void TestVirtualMethodCallsOnStruct()
    {
        // V2 adds override of ToString
        if (typeof(MyStructWithVirtuals).GetMethod("ToString").DeclaringType == typeof(MyStructWithVirtuals))
        {
            // Make sure the constrained call to ToString doesn't box
            var mystruct = new MyStructWithVirtuals();
            mystruct.ToString();
            Assert.AreEqual(mystruct.X, "Overridden");
        }
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static void TestMovedVirtualMethods()
    {
        var o = new MyChildClass();

        Assert.AreEqual(o.MovedToBaseClass(), "MovedToBaseClass");
        Assert.AreEqual(o.ChangedToVirtual(), "ChangedToVirtual");
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static void TestMovedVirtualMethodsOnNullReference()
    {
        MyChildClass o = null;

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

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static void TestConstrainedMethodCalls()
    {
        using (MyStruct s = new MyStruct())
        {
             ((Object)s).ToString();
        }
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static void TestConstrainedMethodCallsOnEnum()
    {
        // Enum.GetHashCode optimization requires special treatment
        // in native signature encoding
        MyEnum e = MyEnum.Apple;
        e.GetHashCode();
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static void TestConstrainedMethodCalls_Unsupported()
    {
        MyStruct s = new MyStruct();
        s.ToString();
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static void TestInterop()
    {
        MyClass.NativeMethod();

        MyClass.TestInterop();
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
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

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static void TestPreInitializedArray()
    {
        var a = new int[] { 1, 2, 4, 8, 16, 32, 64, 128, 256, 512 };

        int sum = 0;
        foreach (var e in a) sum += e;
        Assert.AreEqual(sum, 1023);
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static void TestMultiDimmArray()
    {
       var a = new int[2,3,4];
       a[0,1,2] = a[0,0,0] + a[1,1,1];
       a.ToString();
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static void TestGenericVirtualMethod()
    {
        var o = new MyGeneric<String, Object>();
        Assert.AreEqual(o.GenericVirtualMethod<NonGenericClass, IEnumerable<String>>(),
            "System.StringSystem.ObjectNonGenericClassSystem.Collections.Generic.IEnumerable`1[System.String]");
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static void TestMovedGenericVirtualMethod()
    {
        var o = new MyChildGeneric<Object>();

        Assert.AreEqual(o.MovedToBaseClass<WeakReference>(), typeof(List<WeakReference>).ToString());
        Assert.AreEqual(o.ChangedToVirtual<WeakReference>(), typeof(List<WeakReference>).ToString());
    }



    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static void TestMovedGenericVirtualMethodOnNullReference()
    {
        MyChildGeneric<Object> o = null;

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
    static string CallGeneric<TCallGeneric>(MyGeneric<TCallGeneric, TCallGeneric> g)
    {
        return g.NonVirtualMethod();
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static void TestGenericOverStruct()
    {
        var o1 = new MyGeneric<String, MyGrowingStruct>();
        Assert.AreEqual(o1.GenericVirtualMethod < MyChangingStruct, IEnumerable<NonGenericClass>>(),
            "System.StringMyGrowingStructMyChangingStructSystem.Collections.Generic.IEnumerable`1[NonGenericClass]");

        var o2 = new MyChildGeneric<MyChangingStruct>();
        Assert.AreEqual(o2.MovedToBaseClass<MyGrowingStruct>(), typeof(List<MyGrowingStruct>).ToString());
        Assert.AreEqual(o2.ChangedToVirtual<MyGrowingStruct>(), typeof(List<MyGrowingStruct>).ToString());
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
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

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static void TestInstanceFieldsWithLayout()
    {
        var t = new InstanceFieldTestWithLayout();
        t.Value = 123;

        Assert.AreEqual(typeof(InstanceFieldTestWithLayout).GetRuntimeField("Value").GetValue(t), 123);
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
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

    class MyLoadContext : System.Runtime.Loader.AssemblyLoadContext
    {
        // If running in a collectible context, make the MyLoadContext collectible too so that it doesn't prevent
        // unloading.
        public MyLoadContext() : base(System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly()).IsCollectible)
        {
        }

        public void TestMultipleLoads()
        {
            Assembly a = LoadFromAssemblyPath(Path.Combine(Directory.GetCurrentDirectory(), "test.dll"));
            Assert.AreEqual(System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(a), this);
        }

        protected override Assembly Load(AssemblyName an)
        {
            throw new NotImplementedException();
        }
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static void TestMultipleLoads()
    {
        // Runtime should be able to load the same R2R image in another load context,
        // even though it will be treated as an IL-only image.
        new MyLoadContext().TestMultipleLoads();
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static void TestFieldLayoutNGenMixAndMatch()
    {
        // This test is verifying consistent field layout when ReadyToRun images are combined with NGen images
        // "ngen install /nodependencies main.exe" to exercise the interesting case
        var o = new ByteChildClass(67);
        Assert.AreEqual(o.ChildByte, (byte)67);
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static void TestOpenClosedDelegate()
    {
        // This test is verifying the fixups for open vs. closed delegate created against the same target
        // method are encoded correctly.

        Func<string, string, object> idOpen = OpenClosedDelegateExtensionTest.OpenClosedDelegateTarget;
        Assert.AreEqual(idOpen("World", "foo"), "World, foo");

        Func<string, object> idClosed = "World".OpenClosedDelegateTarget;
        Assert.AreEqual(idClosed("hey"), "World, hey");
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static void TestGenericLdtokenFields()
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

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static void TestRVAField()
    {
        ReadOnlySpan<byte> value = new byte[] { 9, 8, 7, 6, 5, 4, 3, 2, 1, 0 };
        for (byte i = 0; i < value.Length; i++)
            Assert.AreEqual(value[i], (byte)(9 - i));
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static void TestILBodyChange()
    {
        int actualMethodCallResult = (int)typeof(ILInliningTest).GetMethod("TestDifferentIntValue").Invoke(null, new object[]{});
        Console.WriteLine(actualMethodCallResult);
        Assert.AreEqual(ILInliningTest.TestDifferentIntValue(), actualMethodCallResult);
    }

    private static void ValidateTestHasCrossModuleImplementation(string testName, List<string> testMethodData, bool expectedToBePresent = true)
    {
        Console.WriteLine(testName);
        bool found = false;
        foreach (string s in testMethodData)
        {
            if (s.Contains(testName))
                found = true;
        }
        Console.WriteLine($"Found:{found}");
        Assert.AreEqual(expectedToBePresent, found);
    }

    public static void RunAllTests(Assembly assembly)
    {
        // This test is designed to run a representative sample of the sorts of things that R2R can inline across modules
        // In addition to the pattern of extracting the code into this generic, it uses the map file generated by the
        // crossgen2 build to ensure that the expected set of methods was actually compiled into the compilation target.
        // As we remove barriers to further compilation of cross module stuff, we should change this test to validate
        // that the right methods got compiled.
        //
        // Unfortunately, as it is extremely difficult to reliably determine if a method's R2R code was used, we cannot
        // validate that the expected methods were rejected.
        string mapFile = Path.ChangeExtension(assembly.Location, "map");
        var mapFileData = File.ReadAllLines(mapFile);
        List<string> linesWithILInliningVersioningTest = new List<string>();
        foreach (string s in mapFileData)
        {
            if (s.Contains("ILInliningVersioningTest") && s.Contains("MethodWithGCInfo"))
                linesWithILInliningVersioningTest.Add(s);
        }

        Console.WriteLine("ILInliningVersioningTest cross module inlined methods");
        foreach (string s in linesWithILInliningVersioningTest)
        {
            Console.WriteLine(s);
        }

        ValidateTestHasCrossModuleImplementation("TestVirtualMethodCalls", linesWithILInliningVersioningTest);
        TestVirtualMethodCalls();

        ValidateTestHasCrossModuleImplementation("TestVirtualMethodCallsOnStruct", linesWithILInliningVersioningTest, expectedToBePresent: false /* To protect against type level changes, R2R currently does not allow these calls. Fixing this requires an additional fixup to ensure that the constrained dispatch will be resolved correctly. */);
        TestVirtualMethodCallsOnStruct();

        ValidateTestHasCrossModuleImplementation("TestMovedVirtualMethods", linesWithILInliningVersioningTest);
        TestMovedVirtualMethods();

        ValidateTestHasCrossModuleImplementation("TestMovedVirtualMethodsOnNullReference", linesWithILInliningVersioningTest, expectedToBePresent: false /* EH catch clauses cannot be inlined across modules,  Fixing this requires an adjustment to the EH data for inlined methods to allow pulling the tokens from somewhere. */);
        TestMovedVirtualMethodsOnNullReference();

        ValidateTestHasCrossModuleImplementation("TestConstrainedMethodCalls", linesWithILInliningVersioningTest);
        TestConstrainedMethodCalls();

        ValidateTestHasCrossModuleImplementation("TestConstrainedMethodCallsOnEnum", linesWithILInliningVersioningTest);
        TestConstrainedMethodCallsOnEnum();

        ValidateTestHasCrossModuleImplementation("TestConstrainedMethodCalls_Unsupported", linesWithILInliningVersioningTest, expectedToBePresent: false /* constrained dispatch of these forms not supported in R2R at this time. */);
        TestConstrainedMethodCalls_Unsupported();

        ValidateTestHasCrossModuleImplementation("TestInterop", linesWithILInliningVersioningTest);
        TestInterop();

        ValidateTestHasCrossModuleImplementation("TestStaticFields", linesWithILInliningVersioningTest);
        TestStaticFields();

        ValidateTestHasCrossModuleImplementation("TestPreInitializedArray", linesWithILInliningVersioningTest);
        TestPreInitializedArray();

        ValidateTestHasCrossModuleImplementation("TestMultiDimmArray", linesWithILInliningVersioningTest);
        TestMultiDimmArray();

        ValidateTestHasCrossModuleImplementation("TestGenericVirtualMethod", linesWithILInliningVersioningTest);
        TestGenericVirtualMethod();
        ValidateTestHasCrossModuleImplementation("TestMovedGenericVirtualMethod", linesWithILInliningVersioningTest);
        TestMovedGenericVirtualMethod();
        ValidateTestHasCrossModuleImplementation("TestMovedGenericVirtualMethodOnNullReference", linesWithILInliningVersioningTest, expectedToBePresent: false /* EH catch clauses cannot be inlined across modules */);
        TestMovedGenericVirtualMethodOnNullReference();
        ValidateTestHasCrossModuleImplementation("TestGenericNonVirtualMethod", linesWithILInliningVersioningTest);
        TestGenericNonVirtualMethod();

        ValidateTestHasCrossModuleImplementation("TestGenericOverStruct", linesWithILInliningVersioningTest);
        TestGenericOverStruct();

        ValidateTestHasCrossModuleImplementation("TestInstanceFields", linesWithILInliningVersioningTest);
        TestInstanceFields();

        ValidateTestHasCrossModuleImplementation("TestInstanceFieldsWithLayout", linesWithILInliningVersioningTest);
        TestInstanceFieldsWithLayout();

        ValidateTestHasCrossModuleImplementation("TestInheritingFromGrowingBase", linesWithILInliningVersioningTest);
        TestInheritingFromGrowingBase();

        ValidateTestHasCrossModuleImplementation("TestGrowingStruct", linesWithILInliningVersioningTest);
        TestGrowingStruct();
        ValidateTestHasCrossModuleImplementation("TestChangingStruct", linesWithILInliningVersioningTest);
        TestChangingStruct();
        ValidateTestHasCrossModuleImplementation("TestChangingHFAStruct", linesWithILInliningVersioningTest);
        TestChangingHFAStruct();

        ValidateTestHasCrossModuleImplementation("TestGetType", linesWithILInliningVersioningTest);
        TestGetType();

        ValidateTestHasCrossModuleImplementation("TestMultipleLoads", linesWithILInliningVersioningTest);
        TestMultipleLoads();

        ValidateTestHasCrossModuleImplementation("TestFieldLayoutNGenMixAndMatch", linesWithILInliningVersioningTest);
        TestFieldLayoutNGenMixAndMatch();

        ValidateTestHasCrossModuleImplementation("TestStaticBaseCSE", linesWithILInliningVersioningTest);
        TestStaticBaseCSE();

        ValidateTestHasCrossModuleImplementation("TestIsInstCSE", linesWithILInliningVersioningTest);
        TestIsInstCSE();

        ValidateTestHasCrossModuleImplementation("TestCastClassCSE", linesWithILInliningVersioningTest);
        TestCastClassCSE();

        ValidateTestHasCrossModuleImplementation("TestRangeCheckElimination", linesWithILInliningVersioningTest);
        TestRangeCheckElimination();

        ValidateTestHasCrossModuleImplementation("TestOpenClosedDelegate", linesWithILInliningVersioningTest);
        TestOpenClosedDelegate();

        ValidateTestHasCrossModuleImplementation("TestGenericLdtokenFields", linesWithILInliningVersioningTest);
        TestGenericLdtokenFields();

        ValidateTestHasCrossModuleImplementation("TestRVAField", linesWithILInliningVersioningTest);
        TestRVAField();

        ValidateTestHasCrossModuleImplementation("TestILBodyChange", linesWithILInliningVersioningTest);
        TestILBodyChange();
    }

    static int s;
}