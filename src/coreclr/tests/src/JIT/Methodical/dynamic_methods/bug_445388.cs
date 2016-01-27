// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Reflection.Emit;

internal class Host
{
    private static string s_field = "somefield";
}

internal class Program
{
    private delegate string Getter();

    private static int Main()
    {
        DynamicMethod method = new DynamicMethod("GetField",
            typeof(string), new Type[0], typeof(Host));

        ILGenerator il = method.GetILGenerator();
        il.Emit(OpCodes.Ldsfld, typeof(Host).GetField(
            "s_field", BindingFlags.Static | BindingFlags.NonPublic));
        il.Emit(OpCodes.Ret);

        Getter g = (Getter)method.CreateDelegate(typeof(Getter));

        string res = g();

        return Assert(res == "somefield", "Host has not been properly initialized");
    }

    private static int Assert(bool condition, string message)
    {
        if (condition)
        {
            Console.WriteLine("[assert passed]");
            return 100;
        }

        Console.WriteLine("[assert failed] {0}", message);
        return 101;
    }
}
