// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

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

   [DllImport("api-ms-win-core-sysinfo-l1-1-0.dll")]
   public extern static int GetTickCount();

   [DllImport("libcoreclr")]
   public extern static int GetCurrentThreadId();

   static public void TestInterop()
   {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            GetTickCount();
        }
        else
        {
            GetCurrentThreadId();
        }
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
        X = "Overriden";
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
