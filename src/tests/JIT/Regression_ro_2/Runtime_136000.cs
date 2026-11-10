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

    [ConditionalTheory(typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsReflectionEmitSupported), nameof(TestLibrary.Utilities.IsNotMonoRuntime))]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public static void TestEntryPoint(bool generic, bool byref)
    {
        AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Runtime_136000"), AssemblyBuilderAccess.Run);
        TypeBuilder type = assembly.DefineDynamicModule("Module").DefineType("Test", TypeAttributes.Public);
        if (generic)
        {
            type.DefineGenericParameters("T");
        }

        MethodBuilder method = type.DefineMethod("Test", MethodAttributes.Public | MethodAttributes.Static, typeof(Task<bool>), new[] { typeof(Task) });
        method.SetImplementationFlags(MethodImplAttributes.Async | MethodImplAttributes.NoInlining | MethodImplAttributes.AggressiveOptimization);

        // Emit IL because C# forbids reading a byref or ref struct local after an await.
        ILGenerator il = method.GetILGenerator();
        LocalBuilder local = il.DeclareLocal(byref ? typeof(int).MakeByRefType() : typeof(Payload));
        if (byref)
        {
            LocalBuilder value = il.DeclareLocal(typeof(int));
            il.Emit(OpCodes.Ldloca, value);
            il.Emit(OpCodes.Call, typeof(Runtime_136000).GetMethod(nameof(GetByref)));
            il.Emit(OpCodes.Stloc, local);
        }
        else
        {
            il.Emit(OpCodes.Ldloca, local);
            il.Emit(OpCodes.Call, typeof(Runtime_136000).GetMethod(nameof(Initialize)));
        }

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, typeof(AsyncHelpers).GetMethod(nameof(AsyncHelpers.Await), new[] { typeof(Task) }));
        il.Emit(byref ? OpCodes.Ldloc : OpCodes.Ldloca, local);
        il.Emit(OpCodes.Call, typeof(Runtime_136000).GetMethod(byref ? nameof(IsNull) : nameof(IsDefault)));
        il.Emit(OpCodes.Ret);

        Type createdType = type.CreateType();
        if (generic)
        {
            createdType = createdType.MakeGenericType(typeof(object));
        }

        Func<Task, Task<bool>> test = createdType.GetMethod("Test").CreateDelegate<Func<Task, Task<bool>>>();
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

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ref int GetByref(ref int value) => ref value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool IsNull(ref int value) => Unsafe.IsNullRef(ref value);
}
