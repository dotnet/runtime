// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq.Expressions;
using System.Reflection;

/// <summary>
/// Tests that Expression.Convert expressions still work correctly and find
/// implicit and explicit operators in a trimmed app.
/// </summary>
internal class Program
{
    static int Main(string[] args)
    {
        Type[] convertTypes = new Type[] { typeof(Class2), typeof(Class3) };

        ParameterExpression class1Parameter = Expression.Parameter(typeof(Class1), "class1");
        MethodInfo getNameMethodInfo = typeof(Program).GetMethod("GetName");
        foreach (Type convertType in convertTypes)
        {
            UnaryExpression conversion = Expression.Convert(class1Parameter, convertType);

            Func<Class1, string> getNameFunc = 
                Expression.Lambda<Func<Class1, string>>(
                    Expression.Call(null, getNameMethodInfo, conversion), 
                    class1Parameter)
                .Compile();

            string name = getNameFunc(new Class1() { Name = convertType.Name });
            if (convertType.Name == "Class2")
            {
                if (name != "Class2_implicit")
                {
                    return -1;
                }
            }
            else if (convertType.Name == "Class3")
            {
                if (name != "Class3_explicit")
                {
                    return -2;
                }
            }
            else
            {
                return -3;
            }
        }

        //  make sure Class4 was trimmed since it wasn't used, even though Class1 has a conversion operator to it
        int i = 4;
        if (typeof(Program).Assembly.GetType("Class" + i) != null)
        {
            return -4;
        }

        return 100;
    }

    public static string GetName(IHasName hasName) => hasName.Name;
}

interface IHasName
{
    string Name { get; }
}

internal class Class1 : IHasName
{
    public string Name { get; set; }

    public static implicit operator Class2(Class1 class1) => new Class2() { Name = class1.Name + "_implicit" };
    public static explicit operator Class3(Class1 class1) => new Class3() { Name = class1.Name + "_explicit" };
    public static implicit operator Class4(Class1 class1) => new Class4() { Name = class1.Name + "_implicit" };
}

internal class Class2 : IHasName
{
    public string Name { get; set; }
}

internal class Class3 : IHasName
{
    public string Name { get; set; }
}

internal class Class4 : IHasName
{
    public string Name { get; set; }
}