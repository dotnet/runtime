// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Xunit;

public class Program
{
    [Fact]
    public static void EntryPoint()
    {
        string directoryPath = Path.Combine(AppContext.BaseDirectory, "ToDelete");
        string originalAssemblyPath = typeof(Program).Assembly.Location;
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
                    context.LoadFromAssemblyPath(newAssemblyPath);
                });

            assemblyContext.Unload();
        }
        finally
        {
            Directory.Delete(directoryPath, recursive: true);
        }

        Assert.False(Directory.Exists(directoryPath));
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
