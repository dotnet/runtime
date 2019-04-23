// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;

class Program
{
    static int Main()
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

        return 100;
    }
}