// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Xunit.Performance;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.CompilerServices;

public class LowLevelPerf
{
    [Benchmark]
    public static void EmptyStaticFunction()
    {
        foreach (var iteration in Benchmark.Iterations)
        {
            using (iteration.StartMeasurement())
            {
                Class.EmptyStaticFunction();
                Class.EmptyStaticFunction();
                Class.EmptyStaticFunction();
                Class.EmptyStaticFunction();
                Class.EmptyStaticFunction();
                Class.EmptyStaticFunction();
                Class.EmptyStaticFunction();
                Class.EmptyStaticFunction();
                Class.EmptyStaticFunction();
                Class.EmptyStaticFunction();
            }
        }
    }

    [Benchmark]
    public static void EmptyStaticFunction5Arg()
    {
        foreach (var iteration in Benchmark.Iterations)
        {
            using (iteration.StartMeasurement())
            {
                Class.EmptyStaticFunction5Arg(1, 2, 3, 4, 5);
                Class.EmptyStaticFunction5Arg(1, 2, 3, 4, 5);
                Class.EmptyStaticFunction5Arg(1, 2, 3, 4, 5);
                Class.EmptyStaticFunction5Arg(1, 2, 3, 4, 5);
                Class.EmptyStaticFunction5Arg(1, 2, 3, 4, 5);
                Class.EmptyStaticFunction5Arg(1, 2, 3, 4, 5);
                Class.EmptyStaticFunction5Arg(1, 2, 3, 4, 5);
                Class.EmptyStaticFunction5Arg(1, 2, 3, 4, 5);
                Class.EmptyStaticFunction5Arg(1, 2, 3, 4, 5);
                Class.EmptyStaticFunction5Arg(1, 2, 3, 4, 5);
            }
        }
    }

    [Benchmark]
    public static void EmptyInstanceFunction()
    {
        Class aClass = new Class();

        foreach (var iteration in Benchmark.Iterations)
        {
            using (iteration.StartMeasurement())
            {
                aClass.EmptyInstanceFunction();
                aClass.EmptyInstanceFunction();
                aClass.EmptyInstanceFunction();
                aClass.EmptyInstanceFunction();
                aClass.EmptyInstanceFunction();
                aClass.EmptyInstanceFunction();
                aClass.EmptyInstanceFunction();
                aClass.EmptyInstanceFunction();
                aClass.EmptyInstanceFunction();
                aClass.EmptyInstanceFunction();
            }
        }
    }

    [Benchmark]
    public static void InterfaceInterfaceMethod()
    {
        AnInterface aInterface = new Class();

        foreach (var iteration in Benchmark.Iterations)
        {
            using (iteration.StartMeasurement())
            {
                CallInterfaceMethod(aInterface);
                CallInterfaceMethod(aInterface);
                CallInterfaceMethod(aInterface);
                CallInterfaceMethod(aInterface);
                CallInterfaceMethod(aInterface);
                CallInterfaceMethod(aInterface);
                CallInterfaceMethod(aInterface);
                CallInterfaceMethod(aInterface);
                CallInterfaceMethod(aInterface);
                CallInterfaceMethod(aInterface);
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CallInterfaceMethod(AnInterface aInterface)
    {
        aInterface.InterfaceMethod();
    }

    [Benchmark]
    public static void InterfaceInterfaceMethodLongHierarchy()
    {
        AnInterface aInterface = new LongHierarchyChildClass();

        //generate all the not-used call site first
        CallInterfaceMethod(new LongHierarchyClass1());
        CallInterfaceMethod(new LongHierarchyClass2());
        CallInterfaceMethod(new LongHierarchyClass3());
        CallInterfaceMethod(new LongHierarchyClass4());
        CallInterfaceMethod(new LongHierarchyClass5());
        CallInterfaceMethod(new LongHierarchyClass6());
        CallInterfaceMethod(new LongHierarchyClass7());
        CallInterfaceMethod(new LongHierarchyClass8());
        CallInterfaceMethod(new LongHierarchyClass9());
        CallInterfaceMethod(new LongHierarchyClass11());
        CallInterfaceMethod(new LongHierarchyClass12());

        foreach (var iteration in Benchmark.Iterations)
            using (iteration.StartMeasurement())
                CallInterfaceMethod(aInterface);
    }

    [Benchmark]
    public static void InterfaceInterfaceMethodSwitchCallType()
    {
        AnInterface aInterface = new LongHierarchyChildClass();
        AnInterface aInterface1 = new LongHierarchyClass1();

        foreach (var iteration in Benchmark.Iterations)
        {
            using (iteration.StartMeasurement())
            {
                CallInterfaceMethod(aInterface);
                CallInterfaceMethod(aInterface1);
            }
        }
    }

    [Benchmark]
    public static int ClassVirtualMethod()
    {
        SuperClass aClass = new Class();

        int x = 0;
        foreach (var iteration in Benchmark.Iterations)
            using (iteration.StartMeasurement())
                x = aClass.VirtualMethod();

        return x;
    }

    [Benchmark]
    public static void SealedClassInterfaceMethod()
    {
        SealedClass aSealedClass = new SealedClass();

        foreach (var iteration in Benchmark.Iterations)
            using (iteration.StartMeasurement())
                aSealedClass.InterfaceMethod();
    }

    [Benchmark]
    public static void StructWithInterfaceInterfaceMethod()
    {
        StructWithInterface aStructWithInterface = new StructWithInterface();

        foreach (var iteration in Benchmark.Iterations)
            using (iteration.StartMeasurement())
                aStructWithInterface.InterfaceMethod();
    }

    [Benchmark]
    public static void StaticIntPlus()
    {
        Class aClass = new Class();

        foreach (var iteration in Benchmark.Iterations)
            using (iteration.StartMeasurement())
                Class.aStaticInt += 1;
    }

    [Benchmark]
    public static bool ObjectStringIsString()
    {
        object aObjectString = "aString1";
        bool b = false;

        foreach (var iteration in Benchmark.Iterations)
            using (iteration.StartMeasurement())
                b = aObjectString is String;

        return b;
    }

    [Benchmark]
    public static void NewDelegateClassEmptyInstanceFn()
    {
        Class aClass = new Class();
        MyDelegate aMyDelegate;

        foreach (var iteration in Benchmark.Iterations)
            using (iteration.StartMeasurement())
                aMyDelegate = new MyDelegate(aClass.EmptyInstanceFunction);
    }

    [Benchmark]
    public static void NewDelegateClassEmptyStaticFn()
    {
        Class aClass = new Class();
        MyDelegate aMyDelegate;

        foreach (var iteration in Benchmark.Iterations)
            using (iteration.StartMeasurement())
                aMyDelegate = new MyDelegate(Class.EmptyStaticFunction);
    }

    [Benchmark]
    public static void InstanceDelegate()
    {
        Class aClass = new Class();
        MyDelegate aInstanceDelegate = new MyDelegate(aClass.EmptyInstanceFunction);

        foreach (var iteration in Benchmark.Iterations)
            using (iteration.StartMeasurement())
                aInstanceDelegate();
    }

    [Benchmark]
    public static void StaticDelegate()
    {
        Class aClass = new Class();
        MyDelegate aStaticDelegate = new MyDelegate(Class.EmptyStaticFunction);

        foreach (var iteration in Benchmark.Iterations)
            using (iteration.StartMeasurement())
                aStaticDelegate();
    }

    [Benchmark]
    public static void MeasureEvents()
    {
        Class aClass = new Class();
        aClass.AnEvent += new MyDelegate(aClass.EmptyInstanceFunction);

        foreach (var iteration in Benchmark.Iterations)
            using (iteration.StartMeasurement())
                aClass.MeasureFire100();
    }

    [Benchmark]
    public static void GenericClassWithIntGenericInstanceField()
    {
        GenericClass<int> aGenericClassWithInt = new GenericClass<int>();

        foreach (var iteration in Benchmark.Iterations)
            using (iteration.StartMeasurement())
                aGenericClassWithInt.aGenericInstanceFieldT = 1;
    }

    [Benchmark]
    public static void GenericClassGenericStaticField()
    {
        foreach (var iteration in Benchmark.Iterations)
            using (iteration.StartMeasurement())
                GenericClass<int>.aGenericStaticFieldT = 1;
    }

    [Benchmark]
    public static int GenericClassGenericInstanceMethod()
    {
        GenericClass<int> aGenericClassWithInt = new GenericClass<int>();

        int x = 0;
        foreach (var iteration in Benchmark.Iterations)
            using (iteration.StartMeasurement())
                x = aGenericClassWithInt.ClassGenericInstanceMethod();

        return x;
    }

    [Benchmark]
    public static int GenericClassGenericStaticMethod()
    {
        int x = 0;
        foreach (var iteration in Benchmark.Iterations)
            using (iteration.StartMeasurement())
                x = GenericClass<int>.ClassGenericStaticMethod();

        return x;
    }

    [Benchmark]
    public static int GenericGenericMethod()
    {
        // Warmup
        int x = 0;
        foreach (var iteration in Benchmark.Iterations)
            using (iteration.StartMeasurement())
                x = Class.GenericMethod<int>();

        return x;
    }

    [Benchmark]
    public static void GenericClassWithSTringGenericInstanceMethod()
    {
        GenericClass<string> aGenericClassWithString = new GenericClass<string>();
        string aString = "foo";

        foreach (var iteration in Benchmark.Iterations)
            using (iteration.StartMeasurement())
                aGenericClassWithString.aGenericInstanceFieldT = aString;
    }

    [Benchmark]
    public static int ForeachOverList100Elements()
    {
        List<int> iList = new List<int>();
        for (int i = 0; i < 100; i++)
            iList.Add(i);

        int iResult = 0;
        foreach (var iteration in Benchmark.Iterations)
            using (iteration.StartMeasurement())
                foreach (int i in iList)
                    iResult = i;

        return iResult;
    }

    [Benchmark]
    public static Type TypeReflectionObjectGetType()
    {
        Type type = null;
        object anObject = "aString";

        foreach (var iteration in Benchmark.Iterations)
            using (iteration.StartMeasurement())
                type = anObject.GetType();

        return type;
    }

    [Benchmark]
    public static Type TypeReflectionArrayGetType()
    {
        Type type = null;
        object anArray = new string[0];

        foreach (var iteration in Benchmark.Iterations)
            using (iteration.StartMeasurement())
                type = anArray.GetType();

        return type;
    }

    [Benchmark]
    public static string IntegerFormatting()
    {
        int number = Int32.MaxValue;

        string result = null;
        foreach (var iteration in Benchmark.Iterations)
            using (iteration.StartMeasurement())
                result = number.ToString();

        return result;
    }
}

#region Support Classes
// classes and method needed to perform the experiments. 

public interface AnInterface
{
    int InterfaceMethod();
}

public class SuperClass : AnInterface
{
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    public virtual int InterfaceMethod() { return 2; }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    public virtual int VirtualMethod()
    {
        return 1;
    }
}

public struct ValueType
{
    public int x;
    public int y;
    public int z;
}

public delegate int MyDelegate();

public struct StructWithInterface : AnInterface
{
    public int x;
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    public int InterfaceMethod()
    {
        return x++;
    }
}

public sealed class SealedClass : SuperClass
{
    public int aInstanceInt;
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    public override int VirtualMethod()
    {
        return aInstanceInt++;
    }
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    public override int InterfaceMethod()
    {
        return aInstanceInt++;
    }
}

/// <summary>
/// A example class.  It inherits, overrides, has intefaces etc.  
/// It excercises most of the common runtime features 
/// </summary>
public class Class : SuperClass
{
    public event MyDelegate AnEvent;

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    public override int VirtualMethod() { return aInstanceInt++; }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    public override int InterfaceMethod() { return aInstanceInt++; }

    public int aInstanceInt;
    public string aInstanceString;

    public static int aStaticInt;
    public static string aStaticString = "Hello";
    public static ValueType aStaticValueType;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int EmptyStaticFunction()
    {
        return aStaticInt++;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int EmptyStaticFunction5Arg(int arg1, int arg2, int arg3, int arg4, int arg5)
    {
        return aStaticInt++;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int EmptyInstanceFunction()
    {
        return aInstanceInt++;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int GenericMethod<T>()
    {
        return aStaticInt++;
    }

    public void MeasureFire100()
    {
        #region callAnEvent
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        AnEvent();
        //});
        #endregion
    }
}

public class GenericClass<T>
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public T ClassGenericInstanceMethod()
    {
        tmp++; // need this to not be optimized away
        return aGenericInstanceFieldT;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static T ClassGenericStaticMethod()
    {
        sTmp++; // need this to not be optimized away
        return aGenericStaticFieldT;
    }

    public static int sTmp;
    public int tmp;
    public T aGenericInstanceFieldT;
    public static T aGenericStaticFieldT;
}

#region LongHierarchyClass
public class LongHierarchyClass1 : AnInterface
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public virtual int InterfaceMethod() { return 2; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public virtual int VirtualMethod()
    {
        return 1;
    }
}

public class LongHierarchyClass2 : LongHierarchyClass1
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public override int InterfaceMethod() { return 2; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override int VirtualMethod()
    {
        return 1;
    }
}

public class LongHierarchyClass3 : LongHierarchyClass2
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public override int InterfaceMethod() { return 2; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override int VirtualMethod()
    {
        return 1;
    }
}

public class LongHierarchyClass4 : LongHierarchyClass3
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public override int InterfaceMethod() { return 2; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override int VirtualMethod()
    {
        return 1;
    }
}

public class LongHierarchyClass5 : LongHierarchyClass4
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public override int InterfaceMethod() { return 2; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override int VirtualMethod()
    {
        return 1;
    }
}

public class LongHierarchyClass6 : LongHierarchyClass5
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public override int InterfaceMethod() { return 2; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override int VirtualMethod()
    {
        return 1;
    }
}

public class LongHierarchyClass7 : LongHierarchyClass6
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public override int InterfaceMethod() { return 2; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override int VirtualMethod()
    {
        return 1;
    }
}

public class LongHierarchyClass8 : LongHierarchyClass7
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public override int InterfaceMethod() { return 2; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override int VirtualMethod()
    {
        return 1;
    }
}

public class LongHierarchyClass9 : LongHierarchyClass8
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public override int InterfaceMethod() { return 2; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override int VirtualMethod()
    {
        return 1;
    }
}

public class LongHierarchyClass10 : LongHierarchyClass9
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public override int InterfaceMethod() { return 2; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override int VirtualMethod()
    {
        return 1;
    }
}

public class LongHierarchyClass11 : LongHierarchyClass10
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public override int InterfaceMethod() { return 2; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override int VirtualMethod()
    {
        return 1;
    }
}

public class LongHierarchyClass12 : LongHierarchyClass11
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public override int InterfaceMethod() { return 2; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override int VirtualMethod()
    {
        return 1;
    }
}

public class LongHierarchyChildClass : LongHierarchyClass12
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public override int InterfaceMethod() { return 2; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override int VirtualMethod()
    {
        return 1;
    }
}

#endregion

#endregion

