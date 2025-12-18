// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Linq;
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

        int barResult;
        if (TestLibrary.Utilities.IsNativeAot)
        {
            mi = typeof(Async2Reflection).GetMethod("Bar", BindingFlags.Instance | BindingFlags.NonPublic)!;
            barResult = ((Task<int>)mi.Invoke(new Async2Reflection(), null)!).Result;
        }
        else
        {
            dynamic d = new Async2Reflection();
            barResult = d.Bar().Result;
        }

        Assert.Equal(100, (int)(r.Result + barResult));
    }

    [Fact]
    public static void MethodInfo_Invoke_AsyncHelper()
    {
        var mi = typeof(System.Runtime.CompilerServices.AsyncHelpers).GetMethod("Await", BindingFlags.Static | BindingFlags.Public, new Type[] { typeof(Task) })!;
        Assert.NotNull(mi);
        Assert.Throws<NotSupportedException>(() => mi.Invoke(null, new object[] { FooTask() }));

        // Sadly the following does not throw and results in UB
        // We cannot completely prevent putting a token of an Async method into IL stream.
        // CONSIDER: perhaps JIT could throw?
        //
        // dynamic d = FooTask();
        // System.Runtime.CompilerServices.AsyncHelpers.Await(d);
    }

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
    [ActiveIssue("https://github.com/dotnet/runtime/issues/89157", typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNativeAot))]
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

    [ConditionalFact(typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsReflectionEmitSupported))]
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
        var mi = typeof(System.Runtime.CompilerServices.AsyncHelpers).GetMethod("Await", BindingFlags.Static | BindingFlags.Public, new Type[] { typeof(Task) })!;
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

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/122517", typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNativeAot))]
    public static void CurrentMethod()
    {
        // Note: async1 leaks implementation details here and returns "Void MoveNext()"
        Assert.Equal("System.Threading.Tasks.Task`1[System.String] GetCurrentMethodAsync()", GetCurrentMethodAsync().Result);
        Assert.Equal("System.Threading.Tasks.Task`1[System.String] GetCurrentMethodAsync()", GetCurrentMethodAwait().Result);

        Assert.Equal("System.Threading.Tasks.Task`1[System.String] GetCurrentMethodTask()", GetCurrentMethodTask().Result);
        Assert.Equal("System.Threading.Tasks.Task`1[System.String] GetCurrentMethodTask()", GetCurrentMethodAwaitTask().Result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<string> GetCurrentMethodAsync()
    {
        await Task.Yield();
        MethodInfo mi = (MethodInfo)MethodBase.GetCurrentMethod()!;
        return mi.ToString()!;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<string> GetCurrentMethodAwait()
    {
        return await GetCurrentMethodAsync();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Task<string> GetCurrentMethodTask()
    {
        MethodInfo mi = (MethodInfo)MethodBase.GetCurrentMethod()!;
        return Task.FromResult(mi.ToString()!);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<string> GetCurrentMethodAwaitTask()
    {
        return await GetCurrentMethodTask();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public static void FromStack(int level)
    {
        // StackFrame.GetMethod() is not supported on NativeAOT
        if (TestLibrary.Utilities.IsNativeAot)
        {
            return;
        }

        if (level == 0)
        {
            // Note: async1 leaks implementation details here and returns "Void MoveNext()"
            Assert.Equal("System.Threading.Tasks.Task`1[System.String] FromStackAsync(Int32)", FromStackAsync(0).Result);
            Assert.Equal("System.Threading.Tasks.Task`1[System.String] FromStackAsync(Int32)", FromStackAwait(0).Result);

            Assert.Equal("System.Threading.Tasks.Task`1[System.String] FromStackTask(Int32)", FromStackTask(0).Result);
            Assert.Equal("System.Threading.Tasks.Task`1[System.String] FromStackTask(Int32)", FromStackAwaitTask(0).Result);
        }
        else
        {
            // Note: we go through suspend/resume, that is why we see dispatcher as the caller.
            //       we do not see the resume stub though.
            Assert.Equal("Void DispatchContinuations()", FromStackAsync(1).Result);
            Assert.Equal("Void DispatchContinuations()", FromStackAwait(1).Result);

            Assert.Equal("Void FromStack(Int32)", FromStackTask(1).Result);
            // Note: we do not go through suspend/resume, that is why we see the actual caller.
            //       we do not see the async->Task thunk though.
            Assert.Equal("System.Threading.Tasks.Task`1[System.String] FromStackAwaitTask(Int32)", FromStackAwaitTask(1).Result);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<string> FromStackAsync(int level)
    {
        await Task.Yield();
        StackFrame stackFrame = new StackFrame(level);
        MethodInfo mi = (MethodInfo)stackFrame.GetMethod();
        return mi.ToString()!;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<string> FromStackAwait(int level)
    {
        return await FromStackAsync(level);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Task<string> FromStackTask(int level)
    {
        StackFrame stackFrame = new StackFrame(level);
        MethodInfo mi = (MethodInfo)stackFrame.GetMethod();
        return Task.FromResult(mi.ToString()!);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<string> FromStackAwaitTask(int level)
    {
        return await FromStackTask(level);
    }

    [ActiveIssue("https://github.com/dotnet/runtime/issues/122547", typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNativeAot))]
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public static void FromStackDMI(int level)
    {
        if (level == 0)
        {
            // Note: async1 leaks implementation details here and returns "Void MoveNext()"
            Assert.Equal("FromStackDMIAsync", FromStackDMIAsync(0).Result);
            Assert.Equal("FromStackDMIAsync", FromStackDMIAwait(0).Result);

            Assert.Equal("FromStackDMITask", FromStackDMITask(0).Result);
            Assert.Equal("FromStackDMITask", FromStackDMIAwaitTask(0).Result);
        }
        else
        {
            // Note: we go through suspend/resume, that is why we see dispatcher as the caller.
            //       we do not see the resume stub though.
            Assert.Equal("DispatchContinuations", FromStackDMIAsync(1).Result);
            Assert.Equal("DispatchContinuations", FromStackDMIAwait(1).Result);

            Assert.Equal("FromStackDMI", FromStackDMITask(1).Result);
            // Note: we do not go through suspend/resume, that is why we see the actual caller.
            //       we do not see the async->Task thunk though.
            Assert.Equal("FromStackDMIAwaitTask", FromStackDMIAwaitTask(1).Result);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<string> FromStackDMIAsync(int level)
    {
        await Task.Yield();
        StackFrame stackFrame = new StackFrame(level);
        DiagnosticMethodInfo mi = DiagnosticMethodInfo.Create(stackFrame);
        return mi.Name;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<string> FromStackDMIAwait(int level)
    {
        return await FromStackDMIAsync(level);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Task<string> FromStackDMITask(int level)
    {
        StackFrame stackFrame = new StackFrame(level);
        DiagnosticMethodInfo mi = DiagnosticMethodInfo.Create(stackFrame);
        return Task.FromResult(mi.Name);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<string> FromStackDMIAwaitTask(int level)
    {
        return await FromStackDMITask(level);
    }

    [Fact]
    public static void EnumerateAll()
    {
        string[] actual = EnumAll.GetAll();
        string[] expected =
            {"Boolean Equals(System.Object)",
                 "Void Finalize()",
                 "System.Threading.Tasks.Task`1[System.Int32] get_P1()",
                 "System.String[] GetAll()",
                 "Int32 GetHashCode()",
                 "System.Type GetType()",
                 "System.Threading.Tasks.Task`1[System.Int32] M1()",
                 "System.Threading.Tasks.Task`1[System.Int32] M2()",
                 "System.Object MemberwiseClone()",
                 "System.String ToString()" };

        Assert.Equal(expected.Length, actual.Length);
        for (int i = 0; i < actual.Length; i++)
        {
            Assert.Equal(actual[i], expected[i]);
        }
    }

    class EnumAll
    {
        public static Task<int> M1() => Task.FromResult(1);

        public async Task<int> M2() => 1;

        public static Task<int> P1 => Task.FromResult(1);

        public static string[] GetAll()
        {
            Type t = typeof(EnumAll);
            List<string> names = new();
            foreach (MethodInfo mi in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).OrderBy(it => it.Name))
            {
                names.Add(mi.ToString()!);
            }

            return names.ToArray();
        }
    }
}
