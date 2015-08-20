// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using System.Globalization;
using Xunit;

internal class Outside
{
    public class Inside
    { }
}

internal class Outside<T>
{
    public class Inside<U>
    { }
}

public class TypeTests
{
    [Fact]
    public static void TestDeclaringType()
    {
        Type t;
        Type d;

        t = typeof(int);
        d = t.DeclaringType;
        Assert.Null(d);

        t = typeof(Outside.Inside);
        d = t.DeclaringType;
        Assert.Equal(d, typeof(Outside));

        t = typeof(int[]);
        d = t.DeclaringType;
        Assert.Null(d);

        t = typeof(Outside.Inside[]);
        d = t.DeclaringType;
        Assert.Null(d);

        t = typeof(Outside<int>);
        d = t.DeclaringType;
        Assert.Null(d);

        t = typeof(Outside<int>.Inside<double>);
        d = t.DeclaringType;
        Assert.Equal(d, typeof(Outside<>));
    }

    [Fact]
    public static void TestGenericParameterPosition()
    {
        Type t;
        int pos;

        t = typeof(int);
        Assert.Throws<InvalidOperationException>(() => pos = t.GenericParameterPosition);

        t = typeof(int[]);
        Assert.Throws<InvalidOperationException>(() => pos = t.GenericParameterPosition);

        t = typeof(IList<int>);
        Assert.Throws<InvalidOperationException>(() => pos = t.GenericParameterPosition);

        t = typeof(IList<>);
        Assert.Throws<InvalidOperationException>(() => pos = t.GenericParameterPosition);
    }

    [Fact]
    public static void TestGenericTypeArguments()
    {
        Type t;
        Type[] a;

        t = typeof(int);
        a = t.GenericTypeArguments;
        Assert.Equal(a.Length, 0);

        t = typeof(IDictionary<int, String>);
        a = t.GenericTypeArguments;
        Assert.Equal(a.Length, 2);
        Assert.Equal(a[0], typeof(int));
        Assert.Equal(a[1], typeof(String));

        t = typeof(IList<int>[]);
        a = t.GenericTypeArguments;
        Assert.Equal(a.Length, 0);

        t = typeof(IList<>);
        a = t.GenericTypeArguments;
        Assert.Equal(a.Length, 0);
    }

    [Fact]
    public static void TestHasElementType()
    {
        Type t;
        bool b;

        t = typeof(int);
        b = t.HasElementType;
        Assert.False(b);

        t = typeof(int[]);
        b = t.HasElementType;
        Assert.True(b);

        t = typeof(IList<int>);
        b = t.HasElementType;
        Assert.False(b);

        t = typeof(IList<>);
        b = t.HasElementType;
        Assert.False(b);
    }

    [Fact]
    public static void TestIsArray()
    {
        Type t;
        bool b;

        t = typeof(int);
        b = t.IsArray;
        Assert.False(b);

        t = typeof(int[]);
        b = t.IsArray;
        Assert.True(b);

        t = typeof(IList<int>);
        b = t.IsArray;
        Assert.False(b);

        t = typeof(IList<>);
        b = t.IsArray;
        Assert.False(b);
    }

    [Fact]
    public static void TestIsByRef()
    {
        Type t;
        bool b;

        t = typeof(int);
        b = t.IsByRef;
        Assert.False(b);

        t = typeof(int[]);
        b = t.IsByRef;
        Assert.False(b);

        t = typeof(IList<int>);
        b = t.IsByRef;
        Assert.False(b);

        t = typeof(IList<>);
        b = t.IsByRef;
        Assert.False(b);
    }

    [Fact]
    public static void TestIsPointer()
    {
        Type t;
        bool b;

        t = typeof(int);
        b = t.IsPointer;
        Assert.False(b);

        t = typeof(int[]);
        b = t.IsPointer;
        Assert.False(b);

        t = typeof(IList<int>);
        b = t.IsPointer;
        Assert.False(b);

        t = typeof(IList<int>);
        b = t.IsPointer;
        Assert.False(b);

        t = typeof (int *);
        b = t.IsPointer;
        Assert.True(b);
    }

    [Fact]
    public static void TestIsConstructedGenericType()
    {
        Type t;
        bool b;

        t = typeof(int);
        b = t.IsConstructedGenericType;
        Assert.False(b);

        t = typeof(int[]);
        b = t.IsConstructedGenericType;
        Assert.False(b);

        t = typeof(IList<int>);
        b = t.IsConstructedGenericType;
        Assert.True(b);

        t = typeof(IList<>);
        b = t.IsConstructedGenericType;
        Assert.False(b);
    }

    [Fact]
    public static void TestIsGenericParameter()
    {
        Type t;
        bool b;

        t = typeof(int);
        b = t.IsGenericParameter;
        Assert.False(b);

        t = typeof(int[]);
        b = t.IsGenericParameter;
        Assert.False(b);

        t = typeof(IList<int>);
        b = t.IsGenericParameter;
        Assert.False(b);

        t = typeof(IList<>);
        b = t.IsGenericParameter;
        Assert.False(b);
    }

    [Fact]
    public static void TestIsNested()
    {
        Type t;
        bool b;

        t = typeof(int);
        b = t.IsNested;
        Assert.False(b);

        t = typeof(Outside.Inside);
        b = t.IsNested;
        Assert.True(b);

        t = typeof(int[]);
        b = t.IsNested;
        Assert.False(b);

        t = typeof(Outside.Inside[]);
        b = t.IsNested;
        Assert.False(b);

        t = typeof(Outside<int>);
        b = t.IsNested;
        Assert.False(b);

        t = typeof(Outside<int>.Inside<double>);
        b = t.IsNested;
        Assert.True(b);
    }

    [Fact]
    public static void TestTypeHandle()
    {
        Type t, t1, t2;
        RuntimeTypeHandle r, r1;

        t = typeof(int);
        r = t.TypeHandle;
        t1 = typeof(Outside<int>).GenericTypeArguments[0];
        r1 = t1.TypeHandle;
        Assert.Equal(r, r1);
        t2 = Type.GetTypeFromHandle(r);
        Assert.Equal(t, t2);
        t2 = Type.GetTypeFromHandle(r1);
        Assert.Equal(t, t2);

        r = default(RuntimeTypeHandle);
        t = Type.GetTypeFromHandle(r);
        Assert.Null(t);

        t = typeof(int[]);
        r = t.TypeHandle;
        t1 = typeof(int[]);
        r1 = t1.TypeHandle;
        Assert.Equal(r, r1);
        t2 = Type.GetTypeFromHandle(r);
        Assert.Equal(t, t2);
        t2 = Type.GetTypeFromHandle(r1);
        Assert.Equal(t, t2);

        t = typeof(Outside<int>);
        r = t.TypeHandle;
        t1 = typeof(Outside<int>);
        r1 = t1.TypeHandle;
        Assert.Equal(r, r1);
        t2 = Type.GetTypeFromHandle(r);
        Assert.Equal(t, t2);
        t2 = Type.GetTypeFromHandle(r1);
        Assert.Equal(t, t2);
    }

    [Fact]
    public static void TestGetArrayRank()
    {
        Type t;
        int i;

        t = typeof(int);
        Assert.Throws<ArgumentException>(() => i = t.GetArrayRank());

        t = typeof(int[]);
        i = t.GetArrayRank();
        Assert.Equal(i, 1);

        t = typeof(int[,,]);
        i = t.GetArrayRank();
        Assert.Equal(i, 3);

        t = typeof(IList<int>);
        Assert.Throws<ArgumentException>(() => i = t.GetArrayRank());

        t = typeof(IList<>);
        Assert.Throws<ArgumentException>(() => i = t.GetArrayRank());
    }

    [Fact]
    public static void TestGetElementType()
    {
        Type t;
        Type d;

        t = typeof(int);
        d = t.GetElementType();
        Assert.Null(d);

        t = typeof(Outside.Inside);
        d = t.GetElementType();
        Assert.Null(d);

        t = typeof(int[]);
        d = t.GetElementType();
        Assert.Equal(d, typeof(int));

        t = typeof(Outside<int>.Inside<double>[]);
        d = t.GetElementType();
        Assert.Equal(d, typeof(Outside<int>.Inside<double>));

        t = typeof(Outside<int>);
        d = t.GetElementType();
        Assert.Null(d);

        t = typeof(Outside<int>.Inside<double>);
        d = t.GetElementType();
        Assert.Null(d);
    }

    [Fact]
    public static void TestMakeArrayType()
    {
        Type t1, t2, t3, t5;
        bool b;

        t1 = typeof(int);
        t2 = typeof(int[]);
        t3 = t1.MakeArrayType();

        b = t3.IsArray;
        Assert.True(b);

        b = t3.HasElementType;
        Assert.True(b);

        t5 = t3.GetElementType();
        b = t5.Equals(t1);
        Assert.True(b);

        b = t2.Equals(t3);
        Assert.True(b);

        t5 = t1.MakeArrayType();
        b = t5.Equals(t3);
        Assert.True(b);

        String s1 = t1.ToString();
        String s2 = t3.ToString();
        Assert.Equal<String>(s2, s1 + "[]");
    }

    [Fact]
    public static void TestMakeByRefType()
    {
        Type t1, t2, t3, t5;
        bool b;

        t1 = typeof(int);
        t2 = t1.MakeByRefType();
        t3 = t1.MakeByRefType();

        b = t3.IsByRef;
        Assert.True(b);

        b = t3.HasElementType;
        Assert.True(b);

        t5 = t3.GetElementType();
        b = t5.Equals(t1);
        Assert.True(b);

        b = t2.Equals(t3);
        Assert.True(b);

        String s1 = t1.ToString();
        String s2 = t3.ToString();
        Assert.Equal<String>(s2, s1 + "&");
    }

    [Theory, MemberData("GetTypeByNameTestData")]
    public static void TestGetTypeByName(string typeName, Type expectedType)
    {
        var actualType = Type.GetType(typeName, throwOnError: false, ignoreCase: false);
        Assert.Equal(expectedType, actualType);

        actualType = Type.GetType(typeName.ToLower(), throwOnError: false, ignoreCase: true);
        Assert.Equal(expectedType, actualType);
    }

    [Theory, MemberData("GetTypeByNameTestData_Error")]
    public static void TestGetTypeByName_ThrowOnError(string typeName, Type expectedException, bool alwaysThrowsException)
    {
        if (!alwaysThrowsException)
        {
            var actualType = Type.GetType(typeName, throwOnError: false, ignoreCase: false);
            Assert.Null(actualType);
        }

        Assert.Throws(expectedException, () => Type.GetType(typeName, throwOnError: true, ignoreCase: false));
    }

    public static IEnumerable<object[]> GetTypeByNameTestData
    {
        get
        {
            return new[]
            {
                new object[] { "System.Nullable`1[System.Int32]", typeof(int?) },
                new object[] { "System.Int32*", typeof(int*) },
                new object[] { "System.Int32**", typeof(int**) },
                new object[] { "Outside`1", typeof(Outside<>) },
                new object[] { "Outside`1+Inside`1", typeof(Outside<>.Inside<>) },
                new object[] { "Outside[]", typeof(Outside[]) },
                new object[] { "Outside[,,]", typeof(Outside[,,]) },
                new object[] { "Outside[][]", typeof(Outside[][]) },
                new object[] { "Outside`1[System.Nullable`1[System.Boolean]]", typeof(Outside<bool?>) },
            };
        }
    }

    public static IEnumerable<object[]> GetTypeByNameTestData_Error
    {
        get
        {
            return new[]
            {
                new object[] { "System.Nullable`1[System.Int32]".ToLower(), typeof(TypeLoadException), false },
                new object[] { "System.NonExistingType", typeof(TypeLoadException), false },
                new object[] { "", typeof(TypeLoadException), false },
                new object[] { "System.Int32[,*,]", typeof(ArgumentException), false },
                new object[] { "Outside`2", typeof(TypeLoadException), false },
                new object[] { "Outside`1[System.Boolean, System.Int32]", typeof(ArgumentException), true },
            };
        }
    }
}

