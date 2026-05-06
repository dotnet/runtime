// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

/// <summary>
/// Tests that the System.Linq.Expressions.Expression.Property will correctly preserve both
/// accessors even if only one is referenced by MethodInfo.
/// </summary>
internal class Program
{
    static int Main(string[] args)
    {
        int result = ExplicitCreation.Test();
        if (result != 100)
            return result;

        result = LambdaCreation.Test();

        return result;
    }

    class ExplicitCreation
    {
        class TestType
        {
            public bool _testPropertyValue;

            public bool TestProperty { 
                get => _testPropertyValue;
                set { _testPropertyValue = value; }
            }
        }

        public static int Test()
        {
            var obj = new TestType();
            var param = Expression.Parameter(typeof(TestType));
            var prop = Expression.Property(param, typeof(TestType).GetMethod("get_TestProperty"));
            ((PropertyInfo)prop.Member).SetValue(obj, true);
            if (obj._testPropertyValue != true)
                return -1;

            return 100;
        }
    }

    class LambdaCreation
    {
        class TestType
        {
            public bool _testPropertyValue;

            public bool TestProperty { 
                get => _testPropertyValue;
                set { _testPropertyValue = value; }
            }
        }

        public static int Test()
        {
            var obj = new TestType ();
            Expression<Func<TestType, bool>> expression = t => t.TestProperty;
            var prop = ((MemberExpression)expression.Body);
            ((PropertyInfo)prop.Member).SetValue(obj, true);

            if (obj._testPropertyValue != true)
                return -2;

            return 100;
        }
    }
}
