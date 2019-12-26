using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public class Program
{
    public static unsafe int Main(string[] args)
    {
        int errors = 0;

        if (!typeof(int).IsPrimitive) errors++;
        if (!typeof(int).IsValueType) errors++;
        if (typeof(int).IsClass) errors++;

        if (typeof(int?).IsPrimitive) errors++;
        if (!typeof(int?).IsValueType) errors++;
        if (typeof(int?).IsClass) errors++;

        if (typeof(int*).IsPrimitive) errors++;
        if (typeof(int*).IsValueType) errors++;
        if (!typeof(int*).IsClass) errors++;

        if (typeof(void*).IsPrimitive) errors++;
        if (typeof(void*).IsValueType) errors++;
        if (!typeof(void*).IsClass) errors++;

        if (typeof(decimal).IsPrimitive) errors++;
        if (!typeof(decimal).IsValueType) errors++;
        if (typeof(decimal).IsClass) errors++;

        if (typeof(string).IsPrimitive) errors++;
        if (typeof(string).IsValueType) errors++;
        if (!typeof(string).IsClass) errors++;

        if (typeof(object).IsPrimitive) errors++;
        if (typeof(object).IsValueType) errors++;
        if (!typeof(object).IsClass) errors++;

        if (typeof(IEnumerable<int>).IsPrimitive) errors++;
        if (typeof(IEnumerable<int>).IsValueType) errors++;
        if (typeof(IEnumerable<int>).IsClass) errors++;

        if (typeof(Action<int>).IsPrimitive) errors++;
        if (typeof(Action<int>).IsValueType) errors++;
        if (!typeof(Action<int>).IsClass) errors++;

        if (typeof(GenericStruct<int>).IsPrimitive) errors++;
        if (!typeof(GenericStruct<int>).IsValueType) errors++;
        if (typeof(GenericStruct<int>).IsClass) errors++;

        if (typeof(GenericStruct<string>).IsPrimitive) errors++;
        if (!typeof(GenericStruct<string>).IsValueType) errors++;
        if (typeof(GenericStruct<string>).IsClass) errors++;


        if (!IsPrimitive<int>(42)) errors++;
        if (!IsPrimitive<int?>(new Nullable<int>(42))) errors++;
        if (IsPrimitive<decimal>(42M)) errors++;
        if (IsPrimitive<string>("42")) errors++;
        if (IsPrimitive<object>(new object())) errors++;
        if (IsPrimitive<IEnumerable<int>>(new int[10])) errors++;
        if (IsPrimitive<Action<int>>(_ => { })) errors++;
        if (IsPrimitive<GenericStruct<int>>(default)) errors++;
        if (IsPrimitive<GenericStruct<string>>(default)) errors++;
        if (!IsPrimitive(CreateDynamic1())) errors++;
        if (IsPrimitive(CreateDynamic2())) errors++;

        if (!IsValueType<int>(42)) errors++;
        if (!IsValueType<int?>(new Nullable<int>(42))) errors++;
        if (!IsValueType<decimal>(42M)) errors++;
        if (IsValueType<string>("42")) errors++;
        if (IsValueType<object>(new object())) errors++;
        if (IsValueType<IEnumerable<int>>(new int[10])) errors++;
        if (IsValueType<Action<int>>(_ => { })) errors++;
        if (!IsValueType<GenericStruct<int>>(default)) errors++;
        if (!IsValueType<GenericStruct<string>>(default)) errors++;
        if (!IsValueType(CreateDynamic1())) errors++;
        if (IsValueType(CreateDynamic2())) errors++;

        if (IsClass<int>(42)) errors++;
        if (IsClass<int?>(new Nullable<int>(42))) errors++;
        if (IsClass<decimal>(42M)) errors++;
        if (!IsClass<string>("42")) errors++;
        if (!IsClass<object>(new object())) errors++;
        if (!IsClass<IEnumerable<int>>(new int[10])) errors++;
        if (!IsClass<Action<int>>(_ => { })) errors++;
        if (IsClass<GenericStruct<int>>(default)) errors++;
        if (IsClass<GenericStruct<string>>(default)) errors++;
        if (IsClass(CreateDynamic1())) errors++;
        if (!IsClass(CreateDynamic2())) errors++;


        if (!IsPrimitiveObj(42)) errors++;
        if (!IsPrimitiveObj(new Nullable<int>(42))) errors++;
        if (!IsPrimitiveObj(new decimal(42))) errors++;
        if (IsPrimitiveObj("42")) errors++;
        if (IsPrimitiveObj(new object())) errors++;
        if (IsPrimitiveObj(new int[10])) errors++;
        if (IsPrimitiveObj((Action<int>)(_ => { }))) errors++;
        if (!IsPrimitiveObj(new GenericStruct<int>())) errors++;
        if (!IsPrimitiveObj(new GenericStruct<string>())) errors++;
        if (!IsPrimitiveObj(CreateDynamic1())) errors++;
        if (IsPrimitiveObj(CreateDynamic2())) errors++;

        if (!IsValueTypeObj(42)) errors++;
        if (!IsValueTypeObj(new Nullable<int>(42))) errors++;
        if (!IsValueTypeObj(42M)) errors++;
        if (IsValueTypeObj("42")) errors++;
        if (IsValueTypeObj(new object())) errors++;
        if (IsValueTypeObj(new int[10])) errors++;
        if (IsValueTypeObj((Action<int>)(_ => { }))) errors++;
        if (!IsValueTypeObj(new GenericStruct<int>())) errors++;
        if (!IsValueTypeObj(new GenericStruct<string>())) errors++;
        if (!IsValueTypeObj(CreateDynamic1())) errors++;
        if (IsValueTypeObj(CreateDynamic2())) errors++;

        if (!IsClassObj(42)) errors++;
        if (!IsClassObj(new Nullable<int>(42))) errors++;
        if (!IsClassObj(42M)) errors++;
        if (IsClassObj("42")) errors++;
        if (IsClassObj(new object())) errors++;
        if (IsClassObj(new int[10])) errors++;
        if (IsClassObj((Action<int>)(_ => { }))) errors++;
        if (!IsClassObj(new GenericStruct<int>())) errors++;
        if (!IsClassObj(new GenericStruct<string>())) errors++;
        if (!IsClassObj(CreateDynamic1())) errors++;
        if (IsClassObj(CreateDynamic2())) errors++;

        return 100 + errors;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPrimitive<T>(T val) => val.GetType().IsPrimitive;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValueType<T>(T val) => val.GetType().IsValueType;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsClass<T>(T val) => val.GetType().IsClass;


    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool IsPrimitiveObj(object val) => val.GetType().IsValueType;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool IsValueTypeObj(object val) => val.GetType().IsValueType;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool IsClassObj(object val) => val.GetType().IsValueType;


    [MethodImpl(MethodImplOptions.NoInlining)]
    public static dynamic CreateDynamic1() => 42;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static dynamic CreateDynamic2() => new { Name = "Test" };
}

public struct GenericStruct<T>
{
    public T field;
}

