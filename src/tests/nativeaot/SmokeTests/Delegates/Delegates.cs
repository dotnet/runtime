// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq.Expressions;

using Pointer = System.Reflection.Pointer;

public class BringUpTests
{
    const int Pass = 100;
    const int Fail = -1;

    public static int Main()
    {
        int result = Pass;

        if (!TestValueTypeDelegates())
        {
            Console.WriteLine("Failed");
            result = Fail;
        }

        if (!TestVirtualDelegates())
        {
            Console.WriteLine("Failed");
            result = Fail;
        }

        if (!TestInterfaceDelegates())
        {
            Console.WriteLine("Failed");
            result = Fail;
        }

        if (!TestStaticOpenClosedDelegates())
        {
            Console.WriteLine("Failed");
            result = Fail;
        }

        if (!TestMulticastDelegates())
        {
            Console.WriteLine("Failed");
            result = Fail;
        }

        if (!TestDynamicInvoke())
        {
            Console.WriteLine("Failed");
            result = Fail;
        }

#if !CODEGEN_CPP
        TestLinqExpressions.Run();
#endif

        TestDefaultInterfaceMethods.Run();

        return result;
    }

    public static bool TestValueTypeDelegates()
    {
        Console.Write("Testing delegates to value types...");

        {
            TestValueType t = new TestValueType { X = 123 };
            Func<string, string> d = t.GiveX;
            string result = d("MyPrefix");
            if (result != "MyPrefix123")
                return false;
        }

        {
            object t = new TestValueType { X = 456 };
            Func<string> d = t.ToString;
            string result = d();
            if (result != "456")
                return false;
        }

        {
            Func<int, TestValueType> d = TestValueType.MakeValueType;
            TestValueType result = d(789);
            if (result.X != 789)
                return false;
        }

        Console.WriteLine("OK");
        return true;
    }

    public static bool TestVirtualDelegates()
    {
        Console.Write("Testing delegates to virtual methods...");

        {
            Mid t = new Mid();
            if (t.GetBaseDo()() != "Base")
                return false;

            if (t.GetDerivedDo()() != "Mid")
                return false;
        }

        {
            Mid t = new Derived();
            if (t.GetBaseDo()() != "Base")
                return false;

            if (t.GetDerivedDo()() != "Derived")
                return false;
        }

        {
            // This will end up being a delegate to a sealed virtual method.
            ClassWithIFoo t = new ClassWithIFoo("Class");
            Func<int, string> d = t.DoFoo;
            if (d(987) != "Class987")
                return false;
        }

        Console.WriteLine("OK");
        return true;
    }

    public static bool TestInterfaceDelegates()
    {
        Console.Write("Testing delegates to interface methods...");

        {
            IFoo t = new ClassWithIFoo("Class");
            Func<int, string> d = t.DoFoo;
            if (d(987) != "Class987")
                return false;
        }

        {
            IFoo t = new StructWithIFoo("Struct");
            Func<int, string> d = t.DoFoo;
            if (d(654) != "Struct654")
                return false;
        }

        Console.WriteLine("OK");
        return true;
    }

    public static bool TestStaticOpenClosedDelegates()
    {
        Console.Write("Testing static open and closed delegates...");

        {
            Func<string, string, string> d = ExtensionClass.Combine;
            if (d("Hello", "World") != "HelloWorld")
                return false;
        }

        {
            Func<string, string> d = "Hi".Combine;
            if (d("There") != "HiThere")
                return false;
        }

        Console.WriteLine("OK");
        return true;
    }

    public static bool TestMulticastDelegates()
    {
        Console.Write("Testing multicast delegates...");

        {
            ClassThatMutates t = new ClassThatMutates();

            Action d = t.AddOne;
            d();

            if (t.State != 1)
                return false;
            t.State = 0;

            d += t.AddTwo;
            d();

            if (t.State != 3)
                return false;
            t.State = 0;

            d += t.AddOne;
            d();

            if (t.State != 4)
                return false;
        }

        Console.WriteLine("OK");
        return true;
    }

    public static bool TestDynamicInvoke()
    {
        Console.Write("Testing dynamic invoke...");

        {
            TestValueType t = new TestValueType { X = 123 };
            Func<string, string> d = t.GiveX;
            string result = (string)d.DynamicInvoke(new object[] { "MyPrefix" });
            if (result != "MyPrefix123")
                return false;
        }

        {
            Func<int, TestValueType> d = TestValueType.MakeValueType;
            TestValueType result = (TestValueType)d.DynamicInvoke(new object[] { 789 });
            if (result.X != 789)
                return false;
        }

        {
            IFoo t = new ClassWithIFoo("Class");
            Func<int, string> d = t.DoFoo;
            if ((string)d.DynamicInvoke(new object[] { 987 }) != "Class987")
                return false;
        }

        {
            IFoo t = new StructWithIFoo("Struct");
            Func<int, string> d = t.DoFoo;
            if ((string)d.DynamicInvoke(new object[] { 654 }) != "Struct654")
                return false;
        }

        {
            Func<string, string, string> d = ExtensionClass.Combine;
            if ((string)d.DynamicInvoke(new object[] { "Hello", "World" }) != "HelloWorld")
                return false;
        }

        {
            Func<string, string> d = "Hi".Combine;
            if ((string)d.DynamicInvoke(new object[] { "There" }) != "HiThere")
                return false;
        }

        {
            Mutate<int> d = ClassWithByRefs.Mutate;
            object[] args = new object[] { 8 };
            d.DynamicInvoke(args);
            if ((int)args[0] != 50)
                return false;
        }

        {
            Mutate<string> d = ClassWithByRefs.Mutate;
            object[] args = new object[] { "Hello" };
            d.DynamicInvoke(args);
            if ((string)args[0] != "HelloMutated")
                return false;
        }

        unsafe
        {
            GetAndReturnPointerDelegate d = ClassWithPointers.GetAndReturnPointer;
            if (Pointer.Unbox(d.DynamicInvoke(new object[] { (IntPtr)8 })) != (void*)50)
                return false;

            if (Pointer.Unbox(d.DynamicInvoke(new object[] { Pointer.Box((void*)9, typeof(void*)) })) != (void*)51)
                return false;
        }

#if false
        // This is hitting an EH bug around throw/rethrow from a catch block (pass is not set properly)
        unsafe
        {
            PassPointerByRefDelegate d = ClassWithPointers.PassPointerByRef;
            var args = new object[] { (IntPtr)8 };

            bool caught = false;
            try
            {
                d.DynamicInvoke(args);
            }
            catch (ArgumentException)
            {
                caught = true;
            }

            if (!caught)
                return false;
        }
#endif

        Console.WriteLine("OK");
        return true;
    }

    struct TestValueType
    {
        public int X;

        public string GiveX(string prefix)
        {
            return prefix + X.ToString();
        }

        public static TestValueType MakeValueType(int value)
        {
            return new TestValueType { X = value };
        }

        public override string ToString()
        {
            return X.ToString();
        }
    }

    class Base
    {
        public virtual string Do()
        {
            return "Base";
        }
    }

    class Mid : Base
    {
        public override string Do()
        {
            return "Mid";
        }

        public Func<string> GetBaseDo()
        {
            return base.Do;
        }

        public Func<string> GetDerivedDo()
        {
            return Do;
        }
    }

    class Derived : Mid
    {
        public override string Do()
        {
            return "Derived";
        }
    }

    interface IFoo
    {
        string DoFoo(int x);
    }

    class ClassWithIFoo : IFoo
    {
        string _prefix;

        public ClassWithIFoo(string prefix)
        {
            _prefix = prefix;
        }

        public string DoFoo(int x)
        {
            return _prefix + x.ToString();
        }
    }

    struct StructWithIFoo : IFoo
    {
        string _prefix;

        public StructWithIFoo(string prefix)
        {
            _prefix = prefix;
        }

        public string DoFoo(int x)
        {
            return _prefix + x.ToString();
        }
    }

    class ClassThatMutates
    {
        public int State;

        public void AddOne()
        {
            State++;
        }

        public void AddTwo()
        {
            State += 2;
        }
    }
}

static class ExtensionClass
{
    public static string Combine(this string s1, string s2)
    {
        return s1 + s2;
    }
}

unsafe delegate byte* GetAndReturnPointerDelegate(void* ptr);
unsafe delegate void PassPointerByRefDelegate(ref void* ptr);

unsafe static class ClassWithPointers
{
    public static byte* GetAndReturnPointer(void* ptr)
    {
        return (byte*)ptr + 42;
    }

    public static void PassPointerByRef(ref void* ptr)
    {
        ptr = (byte*)ptr + 42;
    }
}

delegate void Mutate<T>(ref T x);

class ClassWithByRefs
{
    public static void Mutate(ref int x)
    {
        x += 42;
    }

    public static void Mutate(ref string x)
    {
        x += "Mutated";
    }
}

class TestLinqExpressions
{
    public static void ModifyByRefAndThrow(ref int i)
    {
        i = 123;
        throw new Exception();
    }

    delegate void RefIntDelegate(ref int i);

    public static void Run()
    {
        Console.WriteLine("Testing LINQ Expressions...");

        {
            ParameterExpression pX = Expression.Parameter(typeof(int).MakeByRefType());
            RefIntDelegate del =
                Expression.Lambda<RefIntDelegate>(
                    Expression.Call(null, typeof(TestLinqExpressions).GetMethod(nameof(ModifyByRefAndThrow)), pX), pX).Compile();

            int i = 0;
            try
            {
                del(ref i);
            }
            catch (Exception) { }

            if (i != 123)
                throw new Exception();
        }
    }
}

class TestDefaultInterfaceMethods
{
    interface IFoo
    {
        int GetNumber() => 42;
    }

    interface IBar : IFoo
    {
        int IFoo.GetNumber() => 43;
    }

    class Foo : IFoo { }
    class Bar : IBar { }

    class Baz : IFoo
    {
        public int GetNumber() => 100;
    }

    public static void Run()
    {
        Console.WriteLine("Testing default interface methods...");

        Func<int> a1 = ((IFoo)new Foo()).GetNumber;
        if (a1() != 42)
            throw new Exception();

        Func<int> a2 = ((IFoo)new Bar()).GetNumber;
        if (a2() != 43)
            throw new Exception();

        Func<int> a3 = ((IFoo)new Baz()).GetNumber;
        if (a3() != 100)
            throw new Exception();
    }
}
