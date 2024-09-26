// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Reflection;
using System.Runtime.InteropServices;

using TypeEquivalenceTypes;

public class EmptyType : IEmptyType
{
    /// <summary>
    /// Create an instance of <see cref="EmptyType" />
    /// </summary>
    public static object Create()
    {
        return new EmptyType();
    }
}

[TypeIdentifier("MyScope", "MyTypeId")]
public struct EquivalentValueType
{
    public int A;
}

/// <summary>
/// Implementation of interfaces that have no impact on inputs.
/// </summary>
public class IdempotentImpl : IMethodTestType
{
    public int ScaleInt(int i)
    {
        return i;
    }

    public string ScaleString(string s)
    {
        return s;
    }
}

public class MethodTestBase : IMethodTestType
{
    private readonly int scaleValue;

    /// <summary>
    /// Create an instance of <see cref="MethodTestBase" />
    /// </summary>
    public static object Create(int scaleValue)
    {
        return new MethodTestBase(scaleValue);
    }

    protected MethodTestBase(int scaleValue)
    {
        this.scaleValue = scaleValue;
    }

    public virtual int ScaleInt(int i)
    {
        return this.scaleValue * i;
    }

    public virtual string ScaleString(string s)
    {
        var sb = new StringBuilder(this.scaleValue * s.Length);
        for (int i = 0; i < this.scaleValue; ++i)
        {
            sb.Append(s);
        }

        return sb.ToString();
    }
}

public class SparseTest : ISparseType
{
    /// <summary>
    /// Create an instance of <see cref="SparseTest" />
    /// </summary>
    public static object Create()
    {
        return new SparseTest();
    }

    /// <summary>
    /// Get the number of methods on the <see cref="ISparseType" /> interface
    /// </summary>
    public static int GetSparseInterfaceMethodCount()
    {
        return typeof(ISparseType).GetMethods(BindingFlags.Public | BindingFlags.Instance).Length;
    }

    public int MultiplyBy1(int a) { return a * 1; }
    public int MultiplyBy2(int a) { return a * 2; }
    public int MultiplyBy3(int a) { return a * 3; }
    public int MultiplyBy4(int a) { return a * 4; }
    public int MultiplyBy5(int a) { return a * 5; }
    public int MultiplyBy6(int a) { return a * 6; }
    public int MultiplyBy7(int a) { return a * 7; }
    public int MultiplyBy8(int a) { return a * 8; }
    public int MultiplyBy9(int a) { return a * 9; }
    public int MultiplyBy10(int a) { return a * 10; }
    public int MultiplyBy11(int a) { return a * 11; }
    public int MultiplyBy12(int a) { return a * 12; }
    public int MultiplyBy13(int a) { return a * 13; }
    public int MultiplyBy14(int a) { return a * 14; }
    public int MultiplyBy15(int a) { return a * 15; }
    public int MultiplyBy16(int a) { return a * 16; }
    public int MultiplyBy17(int a) { return a * 17; }
    public int MultiplyBy18(int a) { return a * 18; }
    public int MultiplyBy19(int a) { return a * 19; }
    public int MultiplyBy20(int a) { return a * 20; }
}

public class OnlyLoadOnceCaller
{
    public static int GetField_1(OnlyLoadOnce_1 s)
    {
        return s.Field;
    }
    public static int GetField_2(OnlyLoadOnce_2 s)
    {
        return s.Field;
    }
    public static int GetField_3(OnlyLoadOnce_3 s)
    {
        return s.Field;
    }
}

public static class MethodCall
{
    // Include a generic type in the method signature before the type using type equivalence to ensure that
    // processing of the generic type does not affect subsequent type processing during signature comparison.
    public static System.Collections.Generic.List<int> InterfaceAfterGeneric(IEmptyType t) => null;
    public static System.Collections.Generic.List<int> ValueTypeAfterGeneric(TestValueType t) => null;

    // Generic type after the type using type equivalence should also not affect processing.
    public static void InterfaceBeforeGeneric(IEmptyType t, System.Collections.Generic.List<int> l) { }
    public static void ValueTypeBeforeGeneric(TestValueType t, System.Collections.Generic.List<int> l) { }
}
