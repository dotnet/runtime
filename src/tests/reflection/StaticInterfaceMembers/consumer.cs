// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using Xunit;

public class Program
{
    [Fact]
    public static void TestEntryPoint()
    {
        {
            FieldInfo fi = typeof(IFoo<object>).GetField("O");
            object val = fi.GetValue(null);
            if (!object.ReferenceEquals(val, typeof(object[])))
                throw new Exception();
        }

        {
            MethodInfo mi = typeof(IFoo<string>).GetMethod("GimmeT");
            object val = mi.Invoke(null, Array.Empty<object>());
            if (!object.ReferenceEquals(val, typeof(string)))
                throw new Exception();
        }

        {
            MethodInfo mi = typeof(IFoo<object>).GetMethod("GimmeU").MakeGenericMethod(typeof(string));
            object val = mi.Invoke(null, Array.Empty<object>());
            if (!object.ReferenceEquals(val, typeof(string)))
                throw new Exception();
        }
    }
}
