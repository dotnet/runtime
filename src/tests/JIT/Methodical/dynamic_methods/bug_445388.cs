// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace Test_bug_445388_cs
{
internal class Host
{
    private static string s_field = "somefield";
}

public class Program
{
    private delegate string Getter();

    [Fact]
    public static int TestEntryPoint()
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
}
