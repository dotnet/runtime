// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq.Expressions;

/// <summary>
/// Tests that Expression.Add expressions still work correctly and find
/// the + operator in a trimmed app.
/// </summary>
internal class Program
{
    static int Main(string[] args)
    {
        ParameterExpression leftParameter = Expression.Parameter(typeof(Class1));
        ParameterExpression rightParameter = Expression.Parameter(typeof(Class1));
        ParameterExpression result = Expression.Variable(typeof(Class1));
        
        Func<Class1, Class1, Class1> func =
            Expression.Lambda<Func<Class1, Class1, Class1>>(
                Expression.Block(
                    new[] { result },
                    Expression.Assign(result, Expression.Add(leftParameter, rightParameter)),
                    result),
                leftParameter, rightParameter)
            .Compile();

        Class1 actual = func(new Class1("left"), new Class1("right"));
        if (actual.Name != "left+right")
        {
            return -1;
        }

        // make sure Class2 was trimmed since it wasn't used, even though Class1 has a binary operator using it
        int i = 2;
        if (typeof(Program).Assembly.GetType("Class" + i) != null)
        {
            return -2;
        }

        return 100;
    }
}

internal class Class1
{
    public Class1(string name) => Name = name;

    public string Name { get; set; }

    public static Class1 operator +(Class1 left, Class1 right) =>
        new Class1($"{left.Name}+{right.Name}");

    public static Class1 operator +(Class1 left, Class2 right) =>
        new Class1($"{left.Name}+{right.Name}2");
    public static Class2 operator +(Class2 left, Class1 right) =>
        new Class2($"{left.Name}2+{right.Name}");
}

internal class Class2
{
    public Class2(string name) => Name = name;

    public string Name { get; set; }
}
