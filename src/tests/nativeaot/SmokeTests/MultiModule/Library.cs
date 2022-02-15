// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

public class MultiModuleLibrary
{
    // Do not reference these three (3) statics in this assembly.
    // We're testing that statics in library code are rooted for use by consuming application code.
    public static int ReturnValue;
    public static string StaticString;
    [ThreadStatic]
    public static int ThreadStaticInt;

    public static bool MethodThatUsesGenerics()
    {
        // Force the existence of a generic dictionary for GenericClass<string>
        // It's important we only use one canonical method and that method is not used from the consumption EXE.
        if (GenericClass<string>.IsArrayOfT(null))
            return false;
        if (!GenericClass<string>.IsArrayOfT(new string[0]))
            return false;

        // Force the existence of a generic dictionary for GenericClass<GenericStruct<string>>
        // Here we test a canonical method that will be used from the consumption EXE too.
        if (!GenericClass<GenericStruct<string>>.IsT(new GenericStruct<string>()))
            return false;

        return true;
    }

    public class GenericClass<T>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool IsT(object o) => o is T;
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool IsArrayOfT(object o) => o is T[];
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool IsMdArrayOfT(object o) => o is T[,];
    }

    public struct GenericStruct<T>
    {
        public T Value;
    }

    public class GenericClassWithTLS<T>
    {
        [ThreadStatic]
        public static int ThreadStaticInt;
    }

    public static bool MethodThatUsesGenericWithTLS()
    {
        GenericClassWithTLS<int>.ThreadStaticInt += 1;
        return GenericClassWithTLS<int>.ThreadStaticInt == 1;
    }

    public enum MyEnum
    {
        One, Two
    }
}
