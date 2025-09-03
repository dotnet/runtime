// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using Xunit;

namespace PrivateLib
{
    class Class1
    {
        static int StaticField;
        int InstanceField = 456;

        static Class1()
        {
            StaticField = 123;
        }

        Class1() { }

        static Class1 GetClass() => new Class1();

        Class2 GetClass2() => new Class2();
    }

    class Class2 { }

    class GenericClass<T>
    {
        List<T> M1() => new List<T>();

        List<U> M2<U>() => new List<U>();

        List<int> M3() => new List<int>();

        List<Class2> M4() => new List<Class2>();

        bool M5<V, W>(List<T> a, List<V> b, List<W> c, List<Class2> d) where W : T => true;

        Type M6<X>(Dictionary<X, int> a) => typeof(X);

        Type M7<Y>(Dictionary<int, Y> a) => typeof(Y);

        Z M8<Z>() where Z : class, new() => new Z();

        bool M9<Z>(List<List<Z>> a, List<Z[,,]> b, List<List<T[,][]>> c) => true;
    }
}
