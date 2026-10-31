// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public class Program
{
    [SkipOnCoreClr("This test is not compatible with GC stress.", RuntimeTestModes.AnyGCStress)]
    [Fact]
    public static void EntryPoint() => LoadAssemblyOnDisk(deleteFile: false, replaceFile: false);

    [SkipOnCoreClr("This test is not compatible with GC stress.", RuntimeTestModes.AnyGCStress)]
    [ConditionalTheory(typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.IsNotWindows))]
    [InlineData(false)]
    [InlineData(true)]
    public static void DeletedAssemblyRemainsUsable(bool replaceFile) => LoadAssemblyOnDisk(deleteFile: true, replaceFile);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void LoadAssemblyOnDisk(bool deleteFile, bool replaceFile)
    {
        string originalAssemblyPath = typeof(Program).Assembly.Location;
        if (originalAssemblyPath.Length == 0)
        {
            return;
        }

        string directoryPath = Path.Combine(Path.GetTempPath(), $"AssemblyOnDisk-{Guid.NewGuid():N}");
        string newAssemblyPath = Path.Combine(directoryPath, Path.GetFileName(originalAssemblyPath));

        // If the directory already exists, delete it
        if (Directory.Exists(directoryPath))
        {
            Console.WriteLine("Temp directory already exists, deleting...");
            Directory.Delete(directoryPath, true);
        }

        // Create a directory to copy the assembly to
        Directory.CreateDirectory(directoryPath);
        try
        {
            File.Copy(originalAssemblyPath, newAssemblyPath);

            UnloadableAssemblyContext assemblyContext = UnloadableAssemblyContext.Create();
            assemblyContext.RunWithAssemblyLoadContext(
                context =>
                {
                    System.Reflection.Assembly assembly = context.LoadFromAssemblyPath(newAssemblyPath);
                    if (deleteFile)
                    {
                        File.Delete(newAssemblyPath);
                        if (replaceFile)
                        {
                            File.WriteAllBytes(newAssemblyPath, new byte[] { 1, 2, 3 });
                        }
                        Type type = assembly.GetType(nameof(Program), throwOnError: true)!;
                        System.Reflection.MethodInfo method = type.GetMethod(nameof(GetMarker))!;
                        Assert.Equal(nameof(Program), method.Invoke(null, null));
                    }
                });

            assemblyContext.Unload();
        }
        finally
        {
            Directory.Delete(directoryPath, recursive: true);
        }

        Assert.False(Directory.Exists(directoryPath));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static string GetMarker() => nameof(Program);

    [ConditionalTheory(typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.HasAssemblyFiles))]
    [InlineData(false)]
    [InlineData(true)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void InMemoryAssemblyOwnsItsBytes(bool useStream)
    {
        byte[] bytes = File.ReadAllBytes(typeof(Program).Assembly.Location);
        System.Reflection.Assembly assembly;
        if (useStream)
        {
            using MemoryStream stream = new(bytes);
            assembly = new AssemblyLoadContext("AssemblyFromStream").LoadFromStream(stream);
        }
        else
        {
            assembly = System.Reflection.Assembly.Load(bytes);
        }

        Array.Clear(bytes);
        Type type = assembly.GetType(nameof(Program), throwOnError: true)!;
        System.Reflection.MethodInfo method = type.GetMethod(nameof(GetMarker))!;
        Assert.Equal(nameof(Program), method.Invoke(null, null));
    }

    public static bool CanRunParallelFileLoads =>
        TestLibrary.Utilities.HasAssemblyFiles
        && TestLibrary.PlatformDetection.IsNotWindows
        && TestLibrary.PlatformDetection.IsMultithreadingSupported
        && !TestLibrary.PlatformDetection.IsWasm;

    [SkipOnCoreClr("This test is not compatible with GC stress.", RuntimeTestModes.AnyGCStress)]
    [ConditionalFact(typeof(Program), nameof(CanRunParallelFileLoads))]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ConcurrentLoadsShareFileImage()
    {
        string directoryPath = Path.Combine(Path.GetTempPath(), $"AssemblyOnDisk-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directoryPath);
        try
        {
            const int WorkerCount = 24;
            const int RoundCount = 32;
            for (int round = 0; round < RoundCount; round++)
            {
                string assemblyPath = Path.Combine(directoryPath, $"copy-{round}.dll");
                File.Copy(typeof(Program).Assembly.Location, assemblyPath);
                using Barrier barrier = new(WorkerCount);
                UnloadableAssemblyContext[] contexts = new UnloadableAssemblyContext[WorkerCount];
                Task[] tasks = new Task[WorkerCount];
                for (int worker = 0; worker < WorkerCount; worker++)
                {
                    int index = worker;
                    contexts[index] = UnloadableAssemblyContext.Create();
                    tasks[index] = Task.Factory.StartNew(() =>
                    {
                        barrier.SignalAndWait();
                        contexts[index].RunWithAssemblyLoadContext(context =>
                        {
                            System.Reflection.Assembly assembly = context.LoadFromAssemblyPath(assemblyPath);
                            Assert.Equal(typeof(Program).Assembly.GetName().Name, assembly.GetName().Name);
                        });
                    }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
                }
                Task.WaitAll(tasks);
                foreach (UnloadableAssemblyContext context in contexts)
                    context.Unload();
                File.Delete(assemblyPath);
            }
        }
        finally
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }

    class UnloadableAssemblyContext
    {
        private readonly WeakReference _weakAssemblyLoadContextReference;

        private AssemblyLoadContext? _assemblyLoadContext;

        private UnloadableAssemblyContext()
        {
            _assemblyLoadContext = new AssemblyLoadContext("AssemblyOnDiskTest", isCollectible: true);
            _weakAssemblyLoadContextReference = new WeakReference(_assemblyLoadContext, trackResurrection: true);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static UnloadableAssemblyContext Create()
        {
            return new UnloadableAssemblyContext();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void RunWithAssemblyLoadContext(Action<AssemblyLoadContext> action)
        {
            action(_assemblyLoadContext!);
        }

        public void Unload()
        {
            TriggerUnload();

            const int maxRetries = 32;
            for (int i = 0; _weakAssemblyLoadContextReference.IsAlive && i <= maxRetries; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();

                if (i == maxRetries)
                {
                    Assert.Fail("Could not unload AssemblyLoadContext.");
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void TriggerUnload()
        {
            _assemblyLoadContext!.Unload();
            _assemblyLoadContext = null;
        }
    }
}
