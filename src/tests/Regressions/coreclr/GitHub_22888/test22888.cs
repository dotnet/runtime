// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

class TestAssemblyLoadContext : AssemblyLoadContext
{
    public TestAssemblyLoadContext() : base(isCollectible: true)
    {
    }

    protected override Assembly Load(AssemblyName name)
    {
        return null;
    }
}
public class Test22888
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static Stream LoadGetResourceStreamAndUnload(string assemblyPath, out WeakReference alcWeakRef)
    {
        var alc = new TestAssemblyLoadContext();
        alcWeakRef = new WeakReference(alc);

        Assembly a = alc.LoadFromAssemblyPath(assemblyPath);
        Stream resourceStream = a.GetManifestResourceStream($"{Path.GetFileNameWithoutExtension(assemblyPath)}.test22888.resources");
        alc.Unload();

        return resourceStream;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool LoadAndUnload(string assemblyPath, out WeakReference alcWeakRef)
    {
        Stream s = LoadGetResourceStreamAndUnload(assemblyPath, out alcWeakRef);

        bool success = (s != null);

        if (success)
        {
            for (int i = 0; alcWeakRef.IsAlive && (i < 10); i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            // Verify that the ALC is still alive - it should be kept alive by the Stream.
            success = alcWeakRef.IsAlive;
            if (!success)
            {
                Console.WriteLine("Failed to keep AssemblyLoadContext alive by the resource stream");
            }
            GC.KeepAlive(s);
        }
        else
        {
            Console.WriteLine("Failed to get resource stream from the test assembly");
        }

        return success;
    }

    public static int Main()
    {
        string currentAssemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string testAssemblyFullPath = Path.Combine(currentAssemblyDirectory, "test22888resources.dll");

        WeakReference alcWeakRef;
        bool success = LoadAndUnload(testAssemblyFullPath, out alcWeakRef);

        if (success)
        {
            for (int i = 0; alcWeakRef.IsAlive && (i < 10); i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            // Now the ALC should not be alive anymore as the resource stream is gone
            success =  !alcWeakRef.IsAlive;
            if (!success)
            {
                Console.WriteLine("Failed to unload the test assembly");
            }
        }

        return success ? 100 : 101;
    }
}
