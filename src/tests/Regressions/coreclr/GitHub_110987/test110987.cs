// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Loader;
using Xunit;

class TestAssemblyLoadContext : AssemblyLoadContext
{
    public TestAssemblyLoadContext() : base(isCollectible: true)
    {
    }
}

public class Test110987
{
    [Fact]
    public static void TestDynamicMethodALC()
    {
        string tempFilePath = Path.GetTempFileName() + ".dll";

        // Create a simple type
        PersistedAssemblyBuilder ab = new PersistedAssemblyBuilder(new AssemblyName("MyAssembly"), typeof(object).Assembly);
        TypeBuilder typeBuilder = ab.DefineDynamicModule("MyModule").DefineType("MyType", TypeAttributes.Public | TypeAttributes.Class);
        MethodBuilder myMethod = typeBuilder.DefineMethod("MyMethod", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard);
        ILGenerator ilGenerator = myMethod.GetILGenerator();
        ilGenerator.Emit(OpCodes.Ret);
        typeBuilder.CreateType();
        ab.Save(tempFilePath);

        TestAssemblyLoadContext tlc = new TestAssemblyLoadContext();

        Type typeFromDisk = tlc.LoadFromAssemblyPath(tempFilePath).GetType("MyType")!;
        MethodInfo methodFromDisk = typeFromDisk.GetMethod("MyMethod")!;

        DynamicMethod callIt = new DynamicMethod(
            "CallIt",
            MethodAttributes.Public | MethodAttributes.Static,
            CallingConventions.Standard,
            returnType: typeof(void),
            parameterTypes: null,
            m: typeFromDisk.Module, // Using typeof(object).Module works
            skipVisibility: true);

        ILGenerator il = callIt.GetILGenerator();
        il.Emit(OpCodes.Call, methodFromDisk);
        il.Emit(OpCodes.Ret);

        Action invoker = (Action)callIt.CreateDelegate(typeof(Action));
        invoker(); 
    }
}
