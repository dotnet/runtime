// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

public class HelperClass
{
    // This method is used to test whether or not a method from a separate module 
    // referenced via Delegate is handled correctly. Do not call this method directly.
    public static void DelegateReferencedMethod()
    {
        Console.WriteLine("In helper method");
    }

    // This method is used to test whether or not a method from a separate module 
    // referenced as a function pointer is handled correctly. Do not call this method directly
    public static void FunctionPointerReferencedMethod()
    {
        Console.WriteLine("In function pointer method");
    }
}

//
// This is a test case for cross module sealed default method invocation
//
public interface IGenericWithSealedDefaultMethodAcrossModule<T>
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    sealed
    string Method()
    {
        Type t = typeof(T);
        return t.FullName;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    sealed
    string GenericMethod<V>()
    {
        Type t = typeof(V);
        return t.FullName;
    }

}

public struct GenericStructForLdtoken<T>
{
    T _value;
    int _intVal;

    public int NonGenericFunction(T genericValue, int inputIntValue)
    {
        if (!((object)genericValue).Equals(_value))
            return 0;
        if (inputIntValue != _intVal)
            return 0;
        return inputIntValue;
    }

    public int GenericFunction<V>(T genericValue, V genericMethodValue, string toStringResult, int inputIntValue)
    {
        if (!((object)genericValue).Equals(_value))
            return 0;
        if (genericMethodValue.ToString() != toStringResult)
            return 0;
        if (inputIntValue != _intVal)
            return 0;
        return inputIntValue;
    }
}

public class BaseTypeNonGenericMethodActuallyOnBase
{
    public string Method()
    {
        return "BaseTypeNonGenericMethodActuallyOnBase::Method";
    }

    public string GenMethod<T>()
    {
        return "BaseTypeNonGenericMethodActuallyOnBase::GenMethod<T>";
    }
}

public class BaseTypeGenericMethodActuallyOnBase<U>
{
    public string Method()
    {
        return "BaseTypeGenericMethodActuallyOnBase<U>::Method";
    }

    public string GenMethod<T>()
    {
        return "BaseTypeGenericMethodActuallyOnBase<U>::GenMethod<T>";
    }
}
