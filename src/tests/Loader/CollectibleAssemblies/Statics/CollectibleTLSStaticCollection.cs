// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Threading;
using Xunit;
using TestLibrary;

namespace CollectibleThreadStaticShutdownRace
{
    public interface IGetAnInt
    {
        int GetInt();
    }

    public class GetAnInt : IGetAnInt
    {
        public int GetInt()
        {
            return 1;
        }
    }

    public class CollectibleThreadStaticShutdownRace
    {
        Action? UseTLSStaticFromLoaderAllocator = null;
        GCHandle IsLoaderAllocatorLive;
        static ulong s_collectibleIndex;

        [MethodImpl(MethodImplOptions.NoInlining)]
        void CallUseTLSStaticFromLoaderAllocator()
        {
            UseTLSStaticFromLoaderAllocator!();
            UseTLSStaticFromLoaderAllocator = null;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        bool CheckLALive()
        {
            return IsLoaderAllocatorLive.Target != null;
        }


        void ThreadThatWaitsForLoaderAllocatorToDisappear()
        {
            CallUseTLSStaticFromLoaderAllocator();
            while (CheckLALive())
            {
                GC.Collect(2);
            }
        }

        public static IGetAnInt s_getAnInt = new GetAnInt();
        static FieldInfo s_getAnIntField;
        static MethodInfo s_getAnIntMethod;

        void CreateLoaderAllocatorWithTLS()
        {
            ulong collectibleIndex = s_collectibleIndex++;

            var ab =
                AssemblyBuilder.DefineDynamicAssembly(
                    new AssemblyName($"CollectibleDerivedAssembly{collectibleIndex}"),
                    AssemblyBuilderAccess.RunAndCollect);
            var mob = ab.DefineDynamicModule($"CollectibleDerivedModule{collectibleIndex}");
            var tb =
                mob.DefineType(
                    $"CollectibleDerived{collectibleIndex}",
                    TypeAttributes.Class | TypeAttributes.Public,
                    typeof(object));

            {
                var fb =
                    tb.DefineField(
                        "Field", typeof(int), FieldAttributes.Static);
                fb.SetCustomAttribute(typeof(ThreadStaticAttribute).GetConstructors()[0], new byte[0]);

                var mb =
                    tb.DefineMethod(
                        "Method",
                        MethodAttributes.Public | MethodAttributes.Static);
                var ilg = mb.GetILGenerator();
                ilg.Emit(OpCodes.Ldsfld, s_getAnIntField);
                ilg.Emit(OpCodes.Callvirt, s_getAnIntMethod);
                ilg.Emit(OpCodes.Stsfld, fb);
                ilg.Emit(OpCodes.Ret);
            }

            Type createdType = tb.CreateType();
            UseTLSStaticFromLoaderAllocator = (Action)createdType.GetMethods()[0].CreateDelegate(typeof(Action));
            IsLoaderAllocatorLive = GCHandle.Alloc(createdType, GCHandleType.WeakTrackResurrection);
        }

        void ForceCollectibleTLSStaticToGoThroughThreadTermination()
        {
            int iteration = 0;
            for (int i = 0; i < 10; i++)
            {
                Console.WriteLine($"Iteration {iteration++}");
                var createLAThread = new Thread(CreateLoaderAllocatorWithTLS);
                createLAThread.Start();
                createLAThread.Join();

                var crashThread = new Thread(ThreadThatWaitsForLoaderAllocatorToDisappear);
                crashThread.Start();
                crashThread.Join();
            }

        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference RunInCollectibleAlc(string assemblyPath, string typeName, string methodName)
        {
            AssemblyLoadContext context = new AssemblyLoadContext(typeName, isCollectible: true);
            Assembly assembly = context.LoadFromAssemblyPath(assemblyPath);
            assembly.GetType(typeName)!.GetMethod(methodName)!.Invoke(null, null);
            context.Unload();
            return new WeakReference(context);
        }

        private static void CollectContext(WeakReference context)
        {
            for (int i = 0; i < 100 && context.IsAlive; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            Assert.False(context.IsAlive);

            // Native LoaderAllocator cleanup must finish before its TLS index can be reused.
            for (int i = 0; i < 5; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/40394", TestRuntimes.Mono)]
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMultithreadingSupported), nameof(PlatformDetection.IsCollectibleAssembliesSupported))]
        public static void ReuseCollectibleThreadStaticIndex()
        {
            string assemblyPath = Path.Combine(AppContext.BaseDirectory, "StaticsUnloaded.dll");
            WeakReference context = RunInCollectibleAlc(
                assemblyPath,
                nameof(ThreadStaticInFlightDataBeforeUnload),
                nameof(ThreadStaticInFlightDataBeforeUnload.Touch));

            CollectContext(context);

            RunInCollectibleAlc(
                assemblyPath,
                nameof(ThreadStaticInFlightDataAfterUnload),
                nameof(ThreadStaticInFlightDataAfterUnload.Set));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsCoreCLR), nameof(PlatformDetection.IsMultithreadingSupported), nameof(PlatformDetection.IsCollectibleAssembliesSupported))]
        public static void CleanupCollectibleThreadStaticsWhileStartingThreads()
        {
            using var stop = new ManualResetEventSlim();
            using var started = new ManualResetEventSlim();
            Thread thread = new Thread(() =>
            {
                do
                {
                    Thread worker = new Thread(static () => { });
                    worker.Start();
                    worker.Join();
                    started.Set();
                }
                while (!stop.IsSet);
            });

            thread.Start();
            started.Wait();

            try
            {
                string assemblyPath = Path.Combine(AppContext.BaseDirectory, "StaticsUnloaded.dll");
                for (int i = 0; i < 20; i++)
                {
                    WeakReference context = RunInCollectibleAlc(
                        assemblyPath,
                        nameof(ThreadStaticInFlightDataBeforeUnload),
                        nameof(ThreadStaticInFlightDataBeforeUnload.Touch));

                    CollectContext(context);
                }
            }
            finally
            {
                stop.Set();
                thread.Join();
            }
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/40394", TestRuntimes.Mono)]
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMultithreadingSupported), nameof(PlatformDetection.IsCollectibleAssembliesSupported))]
        public static void TestEntryPoint()
        {
            s_getAnIntField = typeof(CollectibleThreadStaticShutdownRace).GetField("s_getAnInt");
            s_getAnIntMethod = typeof(IGetAnInt).GetMethod("GetInt");

            new CollectibleThreadStaticShutdownRace().ForceCollectibleTLSStaticToGoThroughThreadTermination();
        }
    }
}
