// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Xunit;

public class Async2Reflection
{
    [Fact]
    public static void MethodInfo_Invoke_TaskReturning()
    {
        var mi = typeof(Async2Reflection).GetMethod("Foo", BindingFlags.Static | BindingFlags.NonPublic)!;
        Task<int> r = (Task<int>)mi.Invoke(null, null)!;

        dynamic d = new Async2Reflection();

        Assert.Equal(100, (int)(r.Result + d.Bar().Result));
    }

#pragma warning disable SYSLIB5007 // 'System.Runtime.CompilerServices.AsyncHelpers' is for evaluation purposes only
    [Fact]
    public static void MethodInfo_Invoke_AsyncHelper()
    {
        var mi = typeof(System.Runtime.CompilerServices.AsyncHelpers).GetMethod("Await", BindingFlags.Static | BindingFlags.Public, new Type[] { typeof(Task) })!;
        Assert.NotNull(mi);
        Assert.Throws<TargetInvocationException>(() => mi.Invoke(null, new object[] { FooTask() }));

        // Sadly the following does not throw and results in UB
        // We cannot completely prevent putting a token of an Async method into IL stream.
        // CONSIDER: perhaps JIT could throw?
        //
        // dynamic d = FooTask();
        // System.Runtime.CompilerServices.AsyncHelpers.Await(d);
    }
#pragma warning restore SYSLIB5007

    private static async Task<int> Foo()
    {
        await Task.Yield();
        return 90;
    }

    private static async Task FooTask()
    {
        await Task.Yield();
    }

    private async Task<int> Bar()
    {
        await Task.Yield();
        return 10;
    }

[Fact]
    public static void AwaitTaskReturningExpressionLambda()
    {
        var expr1 = (Expression<Func<Task<int>>>)(() => Task.FromResult(42));
        var del = expr1.Compile();
        Assert.Equal(42, del().Result);

        AwaitF(42, del).GetAwaiter().GetResult();
    }

    static async Task AwaitF<T>(T expected, Func<Task<T>> f)
    {
        var res = await f.Invoke();
        Assert.Equal(expected, res);
    }

    public interface IExample<T>
    {
        Task TaskReturning();
        T TReturning();
    }

    public class ExampleClass : IExample<Task>
    {
        public Task TaskReturning()
        {
            return null;
        }

        public Task TReturning()
        {
            return null;
        }
    }

    public struct ExampleStruct : IExample<Task>
    {
        public Task TaskReturning()
        {
            return null;
        }

        public Task TReturning()
        {
            return null;
        }
    }

    [Fact]
    public static void GetInterfaceMap()
    {
        Type interfaceType = typeof(IExample<Task>);
        Type classType = typeof(ExampleClass);

        InterfaceMapping map = classType.GetInterfaceMap(interfaceType);

        Assert.Equal(2, map.InterfaceMethods.Length);
        Assert.Equal("System.Threading.Tasks.Task TaskReturning() --> System.Threading.Tasks.Task TaskReturning()",
            $"{map.InterfaceMethods[0]?.ToString()} --> {map.TargetMethods[0]?.ToString()}");

        Assert.Equal("System.Threading.Tasks.Task TReturning() --> System.Threading.Tasks.Task TReturning()",
            $"{map.InterfaceMethods[1]?.ToString()} --> {map.TargetMethods[1]?.ToString()}");

        Type structType = typeof(ExampleStruct);

        map = structType.GetInterfaceMap(interfaceType);
        Assert.Equal(2, map.InterfaceMethods.Length);
        Assert.Equal("System.Threading.Tasks.Task TaskReturning() --> System.Threading.Tasks.Task TaskReturning()",
            $"{map.InterfaceMethods[0]?.ToString()} --> {map.TargetMethods[0]?.ToString()}");

        Assert.Equal("System.Threading.Tasks.Task TReturning() --> System.Threading.Tasks.Task TReturning()",
            $"{map.InterfaceMethods[1]?.ToString()} --> {map.TargetMethods[1]?.ToString()}");
    }

    [Fact]
    public static void TypeBuilder_DefineMethod()
    {
        //  we will be compiling a dynamic vesion of this method
        //
        //  public async static Task StaticMethod(Task arg)
        //  {
        //    await arg;
        //  }

        // Define a dynamic assembly and module
        AssemblyName assemblyName = new AssemblyName("DynamicAssembly");
        AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicModule");

        // Define a type
        TypeBuilder typeBuilder = moduleBuilder.DefineType("DynamicType", TypeAttributes.Public);

        // Define a method
        MethodBuilder methodBuilder = typeBuilder.DefineMethod(
            "DynamicMethod",
            MethodAttributes.Public | MethodAttributes.Static,
            typeof(Task),
            new Type[] { typeof(Task) });

        // Set `MethodImpl.Async` flag
        methodBuilder.SetImplementationFlags(MethodImplAttributes.Async);

        // {
        //   Await(arg_0);
        //   ret;
        // }
        ILGenerator ilGenerator = methodBuilder.GetILGenerator();
        ilGenerator.Emit(OpCodes.Ldarg_0);
#pragma warning disable SYSLIB5007 // 'System.Runtime.CompilerServices.AsyncHelpers' is for evaluation purposes only
        var mi = typeof(System.Runtime.CompilerServices.AsyncHelpers).GetMethod("Await", BindingFlags.Static | BindingFlags.Public, new Type[] { typeof(Task) })!;
#pragma warning restore SYSLIB5007
        ilGenerator.EmitCall(OpCodes.Call, mi, new Type[] { typeof(Task) });
        ilGenerator.Emit(OpCodes.Ret);

        // Create the type and invoke the method
        Type dynamicType = typeBuilder.CreateType();
        MethodInfo dynamicMethod = dynamicType.GetMethod("DynamicMethod");
        var del = dynamicMethod.CreateDelegate<Func<Task, Task>>();

        // the following should not crash
        del(Task.CompletedTask);
        del(FooTask());
    }

    public class PrivateAsync1<T>
    {
        public static int s;
        private static async Task<T> a_task1(int i)
        {
            s++;
            if (i == 0)
            {
                await Task.Yield();
                return default;
            }

            return await Accessors2.accessor<T>(null, i - 1);
        }
    }

    public class PrivateAsync2
    {
        public static int s;
        private static async Task<T> a_task2<T>(int i)
        {
            s++;
            if (i == 0)
            {
                await Task.Yield();
                return default;
            }

            return await Accessors1<T>.accessor(null, i - 1);
        }
    }

    public class Accessors1<T>
    {
        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "a_task1")]
        public extern static Task<T> accessor(PrivateAsync1<T> o, int i);
    }

    public class Accessors2
    {
        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "a_task2")]
        public extern static Task<T> accessor<T>(PrivateAsync2 o, int i);
    }

    [Fact]
    public static void UnsafeAccessors()
    {
        Accessors2.accessor<int>(null, 7).GetAwaiter().GetResult();
        Assert.Equal(4, PrivateAsync1<int>.s);
        Assert.Equal(4, PrivateAsync2.s);

        Accessors1<int>.accessor(null, 7).GetAwaiter().GetResult();
        Assert.Equal(8, PrivateAsync1<int>.s);
        Assert.Equal(8, PrivateAsync2.s);
    }
}
