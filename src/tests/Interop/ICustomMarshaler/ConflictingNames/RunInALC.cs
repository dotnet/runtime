// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using TestLibrary;

public class RunInALC
{
    public static int Main(string[] args)
    {
        try
        {
            ConflictingCustomMarshalerNamesInCollectibleLoadContexts_Succeeds();
            ConflictingCustomMarshalerNamesInNoncollectibleLoadContexts_Succeeds();
        }
        catch (System.Exception e)
        {
            Console.WriteLine(e.ToString());
            return 101;
        }
        return 100;
    }

    static void ConflictingCustomMarshalerNamesInCollectibleLoadContexts_Succeeds()
    {
        Run(new UnloadableLoadContext());
        Run(new UnloadableLoadContext());
    }

    static void ConflictingCustomMarshalerNamesInNoncollectibleLoadContexts_Succeeds()
    {
        Run(new CustomLoadContext());
        Run(new CustomLoadContext());
    }

    static void Run(AssemblyLoadContext context)
    {
        string currentAssemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        Assembly inContextAssembly = context.LoadFromAssemblyPath(Path.Combine(currentAssemblyDirectory, "CustomMarshaler.dll"));
        Type inContextType = inContextAssembly.GetType("CustomMarshalers.CustomMarshalerTest");
        object instance = Activator.CreateInstance(inContextType);
        MethodInfo parseIntMethod = inContextType.GetMethod("ParseInt", BindingFlags.Instance | BindingFlags.Public);
        Assert.AreEqual(1234, (int)parseIntMethod.Invoke(instance, new object[]{"1234"}));
        GC.KeepAlive(context);
    }
}

class UnloadableLoadContext : AssemblyLoadContext
{
    public UnloadableLoadContext()
        :base(true)
    {

    }

    protected override Assembly Load(AssemblyName assemblyName)
    {
        return null;
    }
}

class CustomLoadContext : AssemblyLoadContext
{
    protected override Assembly Load(AssemblyName assemblyName)
    {
        return null;
    }
}
