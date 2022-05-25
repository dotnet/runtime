// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

public interface IInterface { }
public interface IGenericInterface<T> { }
public class ClassA : IInterface { }
public class ClassB : ClassA { }
public class ClassC { }
public struct GenericStruct<T> : IGenericInterface<T> { }

public class Program
{
    // 1. Cast to Class
    [MethodImpl(MethodImplOptions.NoInlining)]
    static ClassA CastToClassA(object o) => (ClassA)o;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static GenericStruct<T> CastToGenericStruct<T>(object o) => (GenericStruct<T>)o;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int[] CastToArray(object o) => (int[])o;

    // 2. Is Instance of Class

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool IsClassA(object o) => o is ClassA;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool IsGenericStruct<T>(object o) => o is GenericStruct<T>;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool IsArray(object o) => o is int[];

    // 3. Is Instance of Interface

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool IsIInterface(object o) => o is IInterface;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool IsGenericInterface<T>(object o) => o is IGenericInterface<T>;

    // 4. Cast to Interface

    [MethodImpl(MethodImplOptions.NoInlining)]
    static IInterface CastToIInterface(object o) => (IInterface)o;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static IGenericInterface<T> CastToGenericInterface<T>(object o) => (IGenericInterface<T>)o;


    [MethodImpl(MethodImplOptions.NoInlining)]
    static void AssertThrows<T>(Action action, [CallerLineNumber] int line = 0) where T : Exception
    {
        try
        {
            action();
        }
        catch (T)
        {
            return;
        }
        throw new InvalidOperationException("InvalidCastException was expected");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool AssertEquals<T>(T t1, T t2)
    {
        return EqualityComparer<T>.Default.Equals(t1, t2);
    }

    public static int Main()
    {
        var a = new ClassA();
        var b = new ClassB();
        var c = new ClassC();
        var gsInt = new GenericStruct<int>();
        var gsString = new GenericStruct<string>();
        var arrayOfUInt32 = new uint[100];
        var arrayOfInt32 = new int[100];
        var arrayOfString = new string[100];

        for (int i = 0; i < 200; i++)
        {
            AssertEquals(IsArray(arrayOfUInt32), true);
            AssertEquals(IsArray(arrayOfInt32), true);
            AssertEquals(IsArray(arrayOfString), false);
            AssertEquals(IsArray(a), false);
            AssertEquals(IsArray(b), false);
            AssertEquals(IsArray(c), false);
            AssertEquals(IsArray(gsInt), false);
            AssertEquals(IsArray(gsString), false);
            AssertEquals(IsArray(null), false);

            AssertEquals(IsClassA(arrayOfUInt32), false);
            AssertEquals(IsClassA(arrayOfInt32), false);
            AssertEquals(IsClassA(arrayOfString), false);
            AssertEquals(IsClassA(a), true);
            AssertEquals(IsClassA(b), false);
            AssertEquals(IsClassA(c), false);
            AssertEquals(IsClassA(gsInt), false);
            AssertEquals(IsClassA(gsString), false);
            AssertEquals(IsClassA(null), false);

            AssertEquals(IsGenericStruct<string>(arrayOfUInt32), false);
            AssertEquals(IsGenericStruct<string>(arrayOfInt32), false);
            AssertEquals(IsGenericStruct<string>(arrayOfString), false);
            AssertEquals(IsGenericStruct<string>(a), true);
            AssertEquals(IsGenericStruct<string>(b), false);
            AssertEquals(IsGenericStruct<string>(c), false);
            AssertEquals(IsGenericStruct<string>(gsInt), false);
            AssertEquals(IsGenericStruct<string>(gsString), false);
            AssertEquals(IsGenericStruct<int>(arrayOfUInt32), false);
            AssertEquals(IsGenericStruct<int>(arrayOfInt32), false);
            AssertEquals(IsGenericStruct<int>(arrayOfString), false);
            AssertEquals(IsGenericStruct<int>(a), true);
            AssertEquals(IsGenericStruct<int>(b), false);
            AssertEquals(IsGenericStruct<int>(c), false);
            AssertEquals(IsGenericStruct<int>(gsInt), false);
            AssertEquals(IsGenericStruct<int>(null), false);

            AssertEquals(IsGenericInterface<string>(arrayOfUInt32), false);
            AssertEquals(IsGenericInterface<string>(arrayOfInt32), false);
            AssertEquals(IsGenericInterface<string>(arrayOfString), false);
            AssertEquals(IsGenericInterface<string>(a), true);
            AssertEquals(IsGenericInterface<string>(b), false);
            AssertEquals(IsGenericInterface<string>(c), false);
            AssertEquals(IsGenericInterface<string>(gsInt), false);
            AssertEquals(IsGenericInterface<string>(gsString), false);
            AssertEquals(IsGenericInterface<int>(arrayOfUInt32), false);
            AssertEquals(IsGenericInterface<int>(arrayOfInt32), false);
            AssertEquals(IsGenericInterface<int>(arrayOfString), false);
            AssertEquals(IsGenericInterface<int>(a), true);
            AssertEquals(IsGenericInterface<int>(b), false);
            AssertEquals(IsGenericInterface<int>(c), false);
            AssertEquals(IsGenericInterface<int>(gsInt), false);
            AssertEquals(IsGenericInterface<int>(gsString), false);
            AssertEquals(IsGenericInterface<int>(null), false);

            AssertEquals(IsIInterface(arrayOfUInt32), false);
            AssertEquals(IsIInterface(arrayOfInt32), false);
            AssertEquals(IsIInterface(arrayOfString), false);
            AssertEquals(IsIInterface(a), true);
            AssertEquals(IsIInterface(b), false);
            AssertEquals(IsIInterface(c), false);
            AssertEquals(IsIInterface(gsInt), false);
            AssertEquals(IsIInterface(gsString), false);
            AssertEquals(IsIInterface(null), false);

            AssertThrows<InvalidCastException>(() => CastToClassA(gsInt));
            AssertThrows<InvalidCastException>(() => CastToClassA(gsString));
            AssertThrows<InvalidCastException>(() => CastToClassA(arrayOfUInt32));
            AssertThrows<InvalidCastException>(() => CastToClassA(arrayOfInt32));
            AssertThrows<InvalidCastException>(() => CastToClassA(arrayOfString));

            AssertThrows<InvalidCastException>(() => CastToArray(a));
            AssertThrows<InvalidCastException>(() => CastToArray(b));
            AssertThrows<InvalidCastException>(() => CastToArray(c));
            AssertThrows<InvalidCastException>(() => CastToArray(gsInt));
            AssertThrows<InvalidCastException>(() => CastToArray(gsString));

            AssertEquals(CastToIInterface(a), a);
            AssertEquals(CastToIInterface(b), b);
            AssertThrows<InvalidCastException>(() => CastToIInterface(c));
            AssertThrows<InvalidCastException>(() => CastToIInterface(gsInt));
            AssertThrows<InvalidCastException>(() => CastToIInterface(gsString));
            AssertThrows<InvalidCastException>(() => CastToIInterface(arrayOfUInt32));
            AssertThrows<InvalidCastException>(() => CastToIInterface(arrayOfInt32));
            AssertThrows<InvalidCastException>(() => CastToIInterface(arrayOfString));

            AssertThrows<InvalidCastException>(() => CastToGenericInterface<int>(a));
            AssertThrows<InvalidCastException>(() => CastToGenericInterface<int>(b));
            AssertThrows<InvalidCastException>(() => CastToGenericInterface<int>(c));
            AssertEquals(CastToGenericInterface<int>(gsInt), gsInt);
            AssertThrows<InvalidCastException>(() => CastToGenericInterface<int>(gsString));
            AssertThrows<InvalidCastException>(() => CastToGenericInterface<int>(arrayOfUInt32));
            AssertThrows<InvalidCastException>(() => CastToGenericInterface<int>(arrayOfInt32));
            AssertThrows<InvalidCastException>(() => CastToGenericInterface<int>(arrayOfString));
            AssertThrows<InvalidCastException>(() => CastToGenericInterface<string>(a));
            AssertThrows<InvalidCastException>(() => CastToGenericInterface<string>(b));
            AssertThrows<InvalidCastException>(() => CastToGenericInterface<string>(c));
            AssertThrows<InvalidCastException>(() => CastToGenericInterface<string>(gsInt));
            AssertEquals(CastToGenericInterface<string>(gsString), gsString);
            AssertThrows<InvalidCastException>(() => CastToGenericInterface<string>(arrayOfUInt32));
            AssertThrows<InvalidCastException>(() => CastToGenericInterface<string>(arrayOfInt32));
            AssertThrows<InvalidCastException>(() => CastToGenericInterface<string>(arrayOfString));

            AssertThrows<InvalidCastException>(() => CastToGenericStruct<int>(a));
            AssertThrows<InvalidCastException>(() => CastToGenericStruct<int>(b));
            AssertThrows<InvalidCastException>(() => CastToGenericStruct<int>(c));
            AssertEquals(CastToGenericStruct<int>(gsInt), gsInt);
            AssertThrows<InvalidCastException>(() => CastToGenericStruct<int>(gsString));
            AssertThrows<InvalidCastException>(() => CastToGenericStruct<int>(arrayOfUInt32));
            AssertThrows<InvalidCastException>(() => CastToGenericStruct<int>(arrayOfInt32));
            AssertThrows<InvalidCastException>(() => CastToGenericStruct<int>(arrayOfString));
            AssertThrows<InvalidCastException>(() => CastToGenericStruct<string>(a));
            AssertThrows<InvalidCastException>(() => CastToGenericStruct<string>(b));
            AssertThrows<InvalidCastException>(() => CastToGenericStruct<string>(c));
            AssertThrows<InvalidCastException>(() => CastToGenericStruct<string>(gsInt));
            AssertEquals(CastToGenericStruct<string>(gsString), gsString);
            AssertThrows<InvalidCastException>(() => CastToGenericStruct<string>(arrayOfUInt32));
            AssertThrows<InvalidCastException>(() => CastToGenericStruct<string>(arrayOfInt32));
            AssertThrows<InvalidCastException>(() => CastToGenericStruct<string>(arrayOfString));
            AssertThrows<NullReferenceException>(() => CastToGenericStruct<string>(null));

            Thread.Sleep(20);
        }
        return 100;
    }
}
