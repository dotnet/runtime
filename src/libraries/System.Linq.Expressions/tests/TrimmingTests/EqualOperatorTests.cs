// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

/// <summary>
/// Tests that Expression.Equal/NotEqual expressions still work correctly and find
/// equality operators in a trimmed app.
/// </summary>
internal class Program
{
    static int Main(string[] args)
    {
        List<(object left, object right, ExpressionType expressionType, bool expected)> testData = new()
        {
            (new Class1("left"), new Class1("right"), ExpressionType.Equal, true),
            (new Class1("left"), new Class1("notright"), ExpressionType.Equal, false),
            (new Class1("left"), new Class1("right"), ExpressionType.NotEqual, false),
            (new Class1("left"), new Class1("notright"), ExpressionType.NotEqual, true),

            (new Class1("left"), new Class2("right"), ExpressionType.Equal, true),
            (new Class1("left"), new Class2("notright"), ExpressionType.Equal, false),
            (new Class1("left"), new Class2("right"), ExpressionType.NotEqual, false),
            (new Class1("left"), new Class2("notright"), ExpressionType.NotEqual, true),
        };

        foreach ((object left, object right, ExpressionType expressionType, bool expected) in testData)
        {
            ParameterExpression leftParameter = Expression.Parameter(typeof(object));
            ParameterExpression rightParameter = Expression.Parameter(typeof(object));

            Expression leftConverted = Expression.Convert(leftParameter, left.GetType());
            Expression rightConverted = Expression.Convert(rightParameter, right.GetType());
            Expression condition;
            if (expressionType == ExpressionType.Equal)
            {
                condition = Expression.Equal(leftConverted, rightConverted);
            }
            else
            {
                condition = Expression.NotEqual(leftConverted, rightConverted);
            }

            ParameterExpression result = Expression.Variable(typeof(bool));

            Func<object, object, bool> func =
                Expression.Lambda<Func<object, object, bool>>(
                    Expression.Block(
                        new[] { result },
                        Expression.IfThenElse(
                            condition,
                            Expression.Assign(result, Expression.Constant(true)),
                            Expression.Assign(result, Expression.Constant(false))),
                        result),
                    leftParameter, rightParameter)
                .Compile();

            bool actual = func(left, right);
            if (actual != expected)
            {
                return -1;
            }
        }

        // make sure Class3 was trimmed since it wasn't used, even though Class1 has equality operators to it
        int i = 3;
        if (typeof(Program).Assembly.GetType("Class" + i) != null)
        {
            return -2;
        }

        return 100;
    }
}

#pragma warning disable CS0660 // Type defines operator == or operator != but does not override Object.Equals(object o)
#pragma warning disable CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
internal class Class1
#pragma warning restore CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
#pragma warning restore CS0660 // Type defines operator == or operator != but does not override Object.Equals(object o)
{
    public Class1(string name) => Name = name;

    public string Name { get; set; }

    // use very unique rules to ensure these operators get invoked
    public static bool operator ==(Class1 left, Class1 right) =>
        left.Name == "left" && right.Name == "right";
    public static bool operator !=(Class1 left, Class1 right) =>
        !(left.Name == "left" && right.Name == "right");

    public static bool operator ==(Class1 left, Class2 right) =>
        left.Name == "left" && right.Name == "right";
    public static bool operator !=(Class1 left, Class2 right) =>
        !(left.Name == "left" && right.Name == "right");

    public static bool operator ==(Class1 left, Class3 right) => left.Name == right.Name;
    public static bool operator !=(Class1 left, Class3 right) => left.Name == right.Name;
}

internal class Class2
{
    public Class2(string name) => Name = name;

    public string Name { get; set; }
}

internal class Class3
{
    public Class3(string name) => Name = name;

    public string Name { get; set; }
}
