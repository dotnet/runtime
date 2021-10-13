// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

public partial class Program
{
    public static void TestIsAssignableToGeneric()
    {
        // Primitive types
        IsTrue (Type.IsAssignableTo<byte, byte>());
        IsTrue (Type.IsAssignableTo<int, int>());
        IsTrue (Type.IsAssignableTo<float, float>());
        IsFalse (Type.IsAssignableTo<float, double>());
        IsTrue (Type.IsAssignableTo<double, double>());

        // Covariance/Contravariance 
        IsTrue (Type.IsAssignableTo<List<string>, IEnumerable<object>>());

        // System.__Canon
        IsTrue (IsAssignableToGeneric<KeyValuePair<IDisposable, IDisposable>, KeyValuePair<IDisposable, IDisposable>>());
        IsTrue (IsAssignableToGeneric<KeyValuePair<IDisposable, object>, KeyValuePair<IDisposable, object>>());
        IsTrue (IsAssignableToGeneric<IDictionary<IDisposable, IDisposable>, IDictionary<IDisposable, IDisposable>>());
        IsTrue (IsAssignableToGeneric<IDictionary<IDisposable, object>, IDictionary<IDisposable, object>>());
        IsTrue (IsAssignableToGeneric<Dictionary<IDisposable, IDisposable>, Dictionary<IDisposable, IDisposable>>());
        IsTrue (IsAssignableToGeneric<Dictionary<IDisposable, object>, Dictionary<IDisposable, object>>());
        IsTrue (IsAssignableToGeneric<KeyValuePair<int, int>, KeyValuePair<int, int>>());
        IsTrue (IsAssignableToGeneric<KeyValuePair<IEnumerable<int>, IEnumerable<int>>, KeyValuePair<IEnumerable<int>, IEnumerable<int>>>());
        IsFalse(IsAssignableToGeneric<KeyValuePair<IDisposable, object>, KeyValuePair<IDisposable, IDisposable>>());
        IsFalse(IsAssignableToGeneric<KeyValuePair<IDisposable, object>, KeyValuePair<IDisposable, int>>());
        IsFalse(IsAssignableToGeneric<IDictionary<IDisposable, object>, IDictionary<IDisposable, IDisposable>>());
        IsFalse(IsAssignableToGeneric<IDictionary<IDisposable, object>, IDictionary<IDisposable, int>>());
        IsFalse(IsAssignableToGeneric<Dictionary<IDisposable, object>, Dictionary<IDisposable, IDisposable>>());
        IsFalse(IsAssignableToGeneric<Dictionary<IDisposable, object>, Dictionary<IDisposable, int>>());
        IsFalse(IsAssignableToGeneric<KeyValuePair<int, object>, KeyValuePair<int, int>>());
        IsFalse(IsAssignableToGeneric<KeyValuePair<IEnumerable<int>, IEnumerable<uint>>, KeyValuePair<IEnumerable<int>, IEnumerable<int>>>());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool IsAssignableToGeneric<TFrom, TTo>() => Type.IsAssignableTo<TFrom, TTo>();
}
