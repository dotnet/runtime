// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using Xunit;
using TestLibrary;

public class Program
{
    class TestALC : AssemblyLoadContext
    {
        AssemblyLoadContext m_parentALC;
        public TestALC(AssemblyLoadContext parentALC) : base("test", isCollectible: true)
        {
            m_parentALC = parentALC;
        }

        protected override Assembly Load(AssemblyName name)
        {
            return m_parentALC.LoadFromAssemblyName(name);
        }
    }

    [ActiveIssue("https://github.com/dotnet/runtimelab/issues/155: Collectible assemblies", typeof(Utilities), nameof(Utilities.IsNativeAot))]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/34072", TestRuntimes.Mono)]
    [Fact]
    public static int TestEntryPoint()
    {
        if (Assembly.GetExecutingAssembly().Location.Length == 0)
        {
            return 100;
        }

        var holdResult = HoldAssembliesAliveThroughByRefFields(out GCHandle gch1, out GCHandle gch2);
        if (holdResult != 100)
            return holdResult;

        // At this point, nothing should keep the collectible assembly alive
        // Loop for a bit forcing the GC to run, and then it should be freed
        for (int i = 0; i < 10; i++)
        {
            GC.Collect(2);
            GC.WaitForPendingFinalizers();
        }

        if (gch1.Target != null)
        {
            return 3;
        }
        if (gch2.Target != null)
        {
            return 4;
        }

        return 100;
    }

    [ConditionalTheory(typeof(Utilities), nameof(Utilities.IsNotMonoRuntime),
        nameof(Utilities.IsNotNativeAot), nameof(Utilities.HasAssemblyFiles))]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public static void GenericContextKeepsAssemblyAlive(bool methodContext, bool collectInCatch)
    {
        WeakReference assembly = InvokeWithGenericContext(methodContext, collectInCatch);
        for (int i = 0; i < 10; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        Assert.False(assembly.IsAlive);
        Console.WriteLine($"{nameof(GenericContextKeepsAssemblyAlive)}({methodContext}, {collectInCatch}) passed.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference InvokeWithGenericContext(bool methodContext, bool collectInCatch)
    {
        DynamicMethod invoker = new DynamicMethod("InvokeGenericContext", typeof(Array),
            new[] { typeof(IntPtr), typeof(Action), typeof(bool) }, typeof(Program).Module);
        ILGenerator il = invoker.GetILGenerator();
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Ldarg_0);
        il.EmitCalli(OpCodes.Calli, CallingConventions.Standard, typeof(Array),
            new[] { typeof(Action), typeof(bool) }, null);
        il.Emit(OpCodes.Ret);
        Func<IntPtr, Action, bool, Array> invoke = invoker.CreateDelegate<Func<IntPtr, Action, bool, Array>>();

        // A raw entry point avoids keeping the instantiation alive through a MethodInfo or delegate.
        IntPtr entryPoint = LoadGenericContextTarget(methodContext, out AssemblyLoadContext context, out WeakReference assembly);
        Array result = invoke(entryPoint, () =>
        {
            context.Unload();
            context = null;
            for (int i = 0; i < 3; i++)
            {
                GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
                GC.WaitForPendingFinalizers();
            }

            Assert.True(assembly.IsAlive);
        }, collectInCatch);

        Assert.Equal("SpanAccessor", result.GetType().GetElementType().Name);

        return assembly;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static IntPtr LoadGenericContextTarget(bool methodContext, out AssemblyLoadContext context, out WeakReference assembly)
    {
        context = new TestALC(AssemblyLoadContext.GetLoadContext(typeof(Program).Assembly));
        Assembly payload = context.LoadFromAssemblyPath(Path.Combine(
            Path.GetDirectoryName(typeof(Program).Assembly.Location), "ByRefLocalsUnloaded.dll"));
        assembly = new WeakReference(payload);
        Type argument = payload.GetType("SpanAccessor", throwOnError: true);
        MethodInfo method = methodContext
            ? typeof(Program).GetMethod(nameof(GenericMethod), BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(argument)
            : typeof(GenericContext<>).MakeGenericType(argument).GetMethod(nameof(GenericContext<object>.Run));
        RuntimeHelpers.PrepareMethod(method.MethodHandle);

        return method.MethodHandle.GetFunctionPointer();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Array GenericMethod<T>(Action collect, bool collectInCatch)
    {
        if (collectInCatch)
        {
            try
            {
                throw new InvalidOperationException();
            }
            catch (InvalidOperationException)
            {
                collect();
            }
        }
        else
        {
            collect();
        }

        return new T[1];
    }

    private static class GenericContext<T>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Array Run(Action collect, bool collectInCatch)
        {
            if (collectInCatch)
            {
                try
                {
                    throw new InvalidOperationException();
                }
                catch (InvalidOperationException)
                {
                    collect();
                }
            }
            else
            {
                collect();
            }

            return new T[1];
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int HoldAssembliesAliveThroughByRefFields(out GCHandle gch1, out GCHandle gch2)
    {
        var span1 = LoadAssembly(out gch1);
        GC.Collect(2);
        GC.WaitForPendingFinalizers();
        GC.Collect(2);
        GC.WaitForPendingFinalizers();
        GC.Collect(2);
        GC.WaitForPendingFinalizers();
        var span2 = CreateAssemblyDynamically(out gch2);
        for (int i = 0; i < 10; i++)
        {
            Console.WriteLine(span1[0]);
            Console.WriteLine(span2[0]);
            GC.Collect(2);
            GC.WaitForPendingFinalizers();
            // The loop may be unrolled, in which case things won't be live on the final iteration.
            if (i < 9)
            {
                if (gch1.Target == null)
                {
                    return 1;
                }
                if (gch2.Target == null)
                {
                    return 2;
                }
            }
        }

        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ReadOnlySpan<byte> LoadAssembly(out GCHandle gchToAssembly)
    {
        var currentALC = AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly());
        var alc = new TestALC(currentALC);
        var a = alc.LoadFromAssemblyPath(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "ByRefLocalsUnloaded.dll"));
        gchToAssembly = GCHandle.Alloc(a, GCHandleType.WeakTrackResurrection);

        var spanAccessor = (IReturnSpan)Activator.CreateInstance(a.GetType("SpanAccessor"));

        alc.Unload();

        return spanAccessor.GetSpan();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ReadOnlySpan<byte> CreateAssemblyDynamically(out GCHandle gchToAssembly)
    {
        AssemblyBuilder ab =
            AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName("tempAssembly"),
            AssemblyBuilderAccess.RunAndCollect);
        ModuleBuilder modb = ab.DefineDynamicModule("tempAssembly.dll");

        var byRefAccessField = modb.DefineInitializedData("RawBytes", new byte[] {1,2,3,4,5}, FieldAttributes.Public | FieldAttributes.Static);
        modb.CreateGlobalFunctions();

        TypeBuilder tb = modb.DefineType("GetSpanType", TypeAttributes.Class, typeof(object), new Type[]{typeof(IReturnSpan)});
        var mb = tb.DefineMethod("GetSpan", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual, typeof(ReadOnlySpan<byte>), new Type[]{});
        ILGenerator myMethodIL = mb.GetILGenerator();
        myMethodIL.Emit(OpCodes.Ldsflda, byRefAccessField);
        myMethodIL.Emit(OpCodes.Ldc_I4_4);
        myMethodIL.Emit(OpCodes.Newobj, typeof(ReadOnlySpan<byte>).GetConstructor(new Type[]{typeof(void*), typeof(int)}));
        myMethodIL.Emit(OpCodes.Ret);

        var getSpanType = tb.CreateType();

        gchToAssembly = GCHandle.Alloc(getSpanType, GCHandleType.WeakTrackResurrection);

        var spanAccessor = (IReturnSpan)Activator.CreateInstance(getSpanType);

        return spanAccessor.GetSpan();
    }

}
