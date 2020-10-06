// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

class Program
{
    static int Main(string[] args)
    {
        var alc = new AssemblyLoadContext("test", isCollectible: true);
        var a = alc.LoadFromAssemblyPath(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Unloaded.dll"));

        var accessor = (IStaticTest)Activator.CreateInstance(a.GetType("StaticTest"));
        accessor.SetStatic(12759, 548739, 5468, 8518, 9995);
        accessor.GetStatic(out int val1, out int val2, out int val3, out int val4, out int val5);
        if (val1 != 12759)
            return 1;
        if (val2 != 548739)
            return 2;
        if (val3 != 5468)
            return 3;
        if (val4 != 8518)
            return 4;
        if (val5 != 9995)
            return 5;

        object obj1 = new object();
        object obj2 = new object();
        object obj3 = new object();
        object obj4 = new object();
        object obj5 = new object();
        accessor.SetStaticObject(obj1, obj2, obj3, obj4, obj5);
        accessor.GetStaticObject(out object val1Obj, out object val2Obj, out object val3Obj, out object val4Obj, out object val5Obj);
        if (val1Obj != obj1)
            return 11;
        if (val2Obj != obj2)
            return 12;
        if (val3Obj != obj3)
            return 13;
        if (val4Obj != obj4)
            return 14;
        if (val5Obj != obj5)
            return 15;

        GC.KeepAlive(accessor);
        return 100;
    }
}