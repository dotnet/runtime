// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

public class Runtime_136000
{
    public ref struct Payload
    {
        public object Value;
        public int Number;
    }

    [ConditionalFact(typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsReflectionEmitSupported), nameof(TestLibrary.Utilities.IsNotMonoRuntime))]
    public static void TestEntryPoint()
    {
        AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Runtime_136000"), AssemblyBuilderAccess.Run);
        TypeBuilder type = assembly.DefineDynamicModule("Module").DefineType("Test", TypeAttributes.Public);
        MethodBuilder method = type.DefineMethod("Test", MethodAttributes.Public | MethodAttributes.Static, typeof(Task<bool>), new[] { typeof(Task) });
        method.SetImplementationFlags(MethodImplAttributes.Async | MethodImplAttributes.NoInlining | MethodImplAttributes.AggressiveOptimization);

        // Emit IL because C# forbids reading a ref struct local after an await.
        ILGenerator il = method.GetILGenerator();
        LocalBuilder local = il.DeclareLocal(typeof(Payload));
        il.Emit(OpCodes.Ldloca, local);
        il.Emit(OpCodes.Call, typeof(Runtime_136000).GetMethod(nameof(Initialize)));
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, typeof(AsyncHelpers).GetMethod(nameof(AsyncHelpers.Await), new[] { typeof(Task) }));
        il.Emit(OpCodes.Ldloca, local);
        il.Emit(OpCodes.Call, typeof(Runtime_136000).GetMethod(nameof(IsDefault)));
        il.Emit(OpCodes.Ret);

        Func<Task, Task<bool>> test = type.CreateType().GetMethod("Test").CreateDelegate<Func<Task, Task<bool>>>();
        Assert.False(test(Task.CompletedTask).GetAwaiter().GetResult());

        var completion = new TaskCompletionSource();
        Task<bool> result = test(completion.Task);
        Assert.False(result.IsCompleted);
        completion.SetResult();
        Assert.True(result.GetAwaiter().GetResult());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Initialize(ref Payload value)
    {
        value.Value = new object();
        value.Number = 42;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool IsDefault(ref Payload value) => value.Value is null && value.Number == 0;
}
