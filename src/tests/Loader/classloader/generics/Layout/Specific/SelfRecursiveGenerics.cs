// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Xunit;

public class SelfRecursiveGenerics
{

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void WillFailOnCoreCLRDueToLimitationsInTypeLoader()
    {
        Console.WriteLine(new SelfReferentialGenericStructWithNoFieldsAutoNonLoadable<int, byte>());
    }
    [Fact]
    public static void TestEntryPoint()
    {
        Console.WriteLine(new SelfReferentialStructWithNoFieldsAuto());
        Console.WriteLine(new SelfReferentialStructWithNoFieldsSequential());
        Console.WriteLine(new SelfReferentialStructWithStringFieldSequential());
        Console.WriteLine(new SelfReferentialStructWithExplicitLayout());

        Console.WriteLine(new SelfReferentialGenericStructWithNoFieldsAuto<int>());
        Console.WriteLine(new SelfReferentialGenericStructWithNoFieldsSequential<int>());
        Console.WriteLine(new SelfReferentialGenericStructWithStringFieldSequential<int>());

        Console.WriteLine(new SelfReferentialGenericStructWithNoFieldsAuto<string>());
        Console.WriteLine(new SelfReferentialGenericStructWithNoFieldsSequential<string>());
        Console.WriteLine(new SelfReferentialGenericStructWithStringFieldSequential<string>());

        Console.WriteLine(typeof(MyNodeAuto).FullName);
        Console.WriteLine(typeof(MyNodeSequential).FullName);

        try
        {
            WillFailOnCoreCLRDueToLimitationsInTypeLoader();
        }
        catch (TypeLoadException tle)
        {
            Console.WriteLine("Hit TLE" + tle.ToString());
        }
    }

    public class Container<T> {
        public struct Nested { }
    }

    [StructLayout(LayoutKind.Auto)]
    public struct SelfReferentialStructWithNoFieldsAuto {
        public Container<SelfReferentialStructWithNoFieldsAuto>.Nested Nested;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct SelfReferentialStructWithNoFieldsSequential {
        public Container<SelfReferentialStructWithNoFieldsSequential>.Nested Nested;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct SelfReferentialStructWithStringFieldSequential {
        public Container<SelfReferentialStructWithStringFieldSequential>.Nested Nested;
        public string String;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct SelfReferentialStructWithExplicitLayout {
        [FieldOffset(1)]
        public Container<SelfReferentialStructWithExplicitLayout>.Nested Nested;
        [FieldOffset(0)]
        public int Fld1;
        [FieldOffset(4)]
        public int Fld2;
    }

    [StructLayout(LayoutKind.Auto)]
    public struct SelfReferentialGenericStructWithNoFieldsAutoNonLoadable<T,V> {
        public Container<SelfReferentialGenericStructWithNoFieldsAutoNonLoadable<V,T>>.Nested Nested;
    }

    [StructLayout(LayoutKind.Auto)]
    public struct SelfReferentialGenericStructWithNoFieldsAuto<T> {
        public Container<SelfReferentialGenericStructWithNoFieldsAuto<T>>.Nested Nested;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct SelfReferentialGenericStructWithNoFieldsSequential<T> {
        public Container<SelfReferentialGenericStructWithNoFieldsSequential<T>>.Nested Nested;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct SelfReferentialGenericStructWithStringFieldSequential<T> {
        public Container<SelfReferentialGenericStructWithStringFieldSequential<T>>.Nested Nested;
        public string String;
    }


    /// <summary>
    /// List of T expressed as a value type
    /// </summary>
    public struct ValueList<T>
    {
        private T[] _arr;
        private int _count;
    }

    [StructLayout(LayoutKind.Auto)]
    public struct MyNodeAuto
    {
        public int NodeData;

        public ValueList<MyNodeAuto> Nodes;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MyNodeSequential
    {
        public int NodeData;

        public ValueList<MyNodeSequential> Nodes;
    }
}
