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
    class TestType
    {
        public bool _testPropertyValue;

        public bool TestProperty { 
            get => _testPropertyValue;
            set { _testPropertyValue = value; }
        }
    }

    static int Main(string[] args)
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
