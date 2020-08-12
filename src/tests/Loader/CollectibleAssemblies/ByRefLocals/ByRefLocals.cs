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
            if (gch1.Target == null)
            {
                return 1;
            }
            if (gch2.Target == null)
            {
                return 2;
            }
        }

        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ReadOnlySpan<byte> LoadAssembly(out GCHandle gchToAssembly)
    {
        var alc = new AssemblyLoadContext("test", isCollectible: true);
        var a = alc.LoadFromAssemblyPath(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Unloaded.dll"));
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