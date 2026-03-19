// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Xunit;

public class InvalidCastGenericALC
{
    public static void ForceCast<T>(object obj)
    {
        T result = (T)obj;
        GC.KeepAlive(result);
    }

    [Fact]
    public static void InvalidCastException_GenericTypeArg_ShowsDifferingAssemblyInfo()
    {
        string sharedTypePath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            "SharedType.dll");

        var alc1 = new AssemblyLoadContext("ALC1");
        var alc2 = new AssemblyLoadContext("ALC2");

        Assembly asm1 = alc1.LoadFromAssemblyPath(sharedTypePath);
        Assembly asm2 = alc2.LoadFromAssemblyPath(sharedTypePath);

        Type type1 = asm1.GetType("SharedAssembly.SharedType");
        Type type2 = asm2.GetType("SharedAssembly.SharedType");

        // StrongBox<T> is from System.Private.CoreLib - same in all contexts.
        // The generic argument types come from different ALCs.
        Type boxType1 = typeof(StrongBox<>).MakeGenericType(type1);
        Type boxType2 = typeof(StrongBox<>).MakeGenericType(type2);

        object instance = Activator.CreateInstance(boxType1);

        // Trigger InvalidCastException by trying to cast boxType1 instance to boxType2
        MethodInfo forceCast = typeof(InvalidCastGenericALC)
            .GetMethod(nameof(ForceCast))
            .MakeGenericMethod(boxType2);

        try
        {
            forceCast.Invoke(null, new object[] { instance });
            Assert.Fail("Expected InvalidCastException was not thrown");
        }
        catch (TargetInvocationException ex) when (ex.InnerException is InvalidCastException ice)
        {
            Console.WriteLine("InvalidCastException message:");
            Console.WriteLine(ice.Message);

            if (ice.Message.Contains("Debugging resource strings are unavailable"))
            {
                Console.WriteLine("Skipping message validation - resource strings unavailable");
                return;
            }

            // The message should mention the generic argument and its differing ALCs.
            // Before the fix, it would only report the outer type's assembly
            // (System.Private.CoreLib from the Default context for both),
            // making the message unhelpful.
            // After the fix, it reports the generic argument's assembly and ALC context.
            Assert.Contains("generic argument", ice.Message);
            Assert.Contains("SharedAssembly.SharedType", ice.Message);
            Assert.Contains("ALC1", ice.Message);
            Assert.Contains("ALC2", ice.Message);
        }
    }
}
