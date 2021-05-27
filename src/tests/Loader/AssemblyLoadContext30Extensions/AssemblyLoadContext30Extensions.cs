// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Loader;
using System.IO;
using System.Linq;
using My;

namespace My
{
    public class CustomAssemblyLoadContext : AssemblyLoadContext
    {
        public CustomAssemblyLoadContext() : base()
        {
        }
    }
}

public class Program
{
    private static bool passed = true;

    public static void Assert(bool value, string message = "none")
    {
        if (!value)
        {
            Console.WriteLine("FAIL! " + message);
            passed = false;
        }
    }

    public static bool AssembliesContainAssembly(AssemblyLoadContext alc, Assembly a)
    {
        foreach (Assembly b in alc.Assemblies)
        {
            if (a == b)
            {
                return true;
            }
        }
        return false;
    }

    public static bool PropertyAllContainsContext(AssemblyLoadContext alc)
    {
        foreach (AssemblyLoadContext c in AssemblyLoadContext.All)
        {
            if (alc == c)
            {
                return true;
            }
        }
        return false;
    }

    public static void DefaultName()
    {
        try
        {
            Console.WriteLine("DefaultName()");

            AssemblyLoadContext alc = AssemblyLoadContext.Default;
            Assert(alc == AssemblyLoadContext.GetLoadContext(typeof(Program).Assembly));
            Assert(PropertyAllContainsContext(alc));
            Assert(AssembliesContainAssembly(alc, typeof(Program).Assembly));

            Console.WriteLine(alc.Name);
            Assert(alc.Name == "Default");

            Console.WriteLine(alc.GetType().ToString());
            Assert(alc.GetType().ToString() == "System.Runtime.Loader.DefaultAssemblyLoadContext");

            Console.WriteLine(alc.ToString());
            Assert(alc.ToString().Contains("\"Default"));
            Assert(alc.ToString().Contains("\" System.Runtime.Loader.DefaultAssemblyLoadContext"));
            Assert(alc.ToString().Contains(" #"));
        }
        catch (Exception e)
        {
            Assert(false, e.ToString());
        }
    }

    public static void AssemblyLoadFileName()
    {
        try
        {
            Console.WriteLine("AssemblyLoadFileName()");

            String path = typeof(Program).Assembly.Location;
            Assembly a = Assembly.LoadFile(path);

            Assert(a != typeof(Program).Assembly);

            AssemblyLoadContext alc = AssemblyLoadContext.GetLoadContext(a);
            Assert(PropertyAllContainsContext(alc));
            Assert(AssembliesContainAssembly(alc, a));
            Assert(alc != AssemblyLoadContext.Default);


            Console.WriteLine(alc.Name);
            Assert(alc.Name == String.Format("Assembly.LoadFile({0})", path));

            Console.WriteLine(alc.GetType().ToString());
            Assert(alc.GetType().ToString() == "System.Runtime.Loader.IndividualAssemblyLoadContext");

            Console.WriteLine(alc.ToString());
            Assert(alc.ToString().Contains("\"" + String.Format("Assembly.LoadFile({0})", path)));
            Assert(alc.ToString().Contains("\" System.Runtime.Loader.IndividualAssemblyLoadContext"));
            Assert(alc.ToString().Contains(" #"));
        }
        catch (Exception e)
        {
            Assert(false, e.ToString());
        }
    }

    public static void AssemblyLoadByteArrayName()
    {
#if runDisabled // This test case fails when the assembly is a ready2run image
        try
        {
            Console.WriteLine("AssemblyLoadByteArrayName()");

            String path = typeof(Program).Assembly.Location;
            Byte [] byteArray = System.IO.File.ReadAllBytes(path);
            Assembly a = Assembly.Load(byteArray);
            AssemblyLoadContext alc = AssemblyLoadContext.GetLoadContext(a);
            Assert(PropertyAllContainsContext(alc));
            Assert(AssembliesContainAssembly(alc, a));

            Console.WriteLine(alc.Name);
            Assert(alc.Name == "Assembly.Load(byte[], ...)");

            Console.WriteLine(alc.GetType().ToString());
            Assert(alc.GetType().ToString() == "System.Runtime.Loader.IndividualAssemblyLoadContext");

            Console.WriteLine(alc.ToString());
            Assert(alc.ToString().Contains("\"Assembly.Load(byte[], ...)\""));
            Assert(alc.ToString().Contains("\" System.Runtime.Loader.IndividualAssemblyLoadContext"));
            Assert(alc.ToString().Contains(" #"));
        }
        catch (Exception e)
        {
            Assert(false, e.ToString());
        }
#endif
    }

    public static void CustomWOName()
    {
        try
        {
            Console.WriteLine("CustomWOName()");

            // ALC should be a concrete class
            AssemblyLoadContext alc = new My.CustomAssemblyLoadContext();
            Assert(PropertyAllContainsContext(alc));

            Console.WriteLine(alc.Name);
            Assert(alc.Name == null);

            Console.WriteLine(alc.GetType().ToString());
            Assert(alc.GetType().ToString() == "My.CustomAssemblyLoadContext");

            Console.WriteLine(alc.ToString());
            Assert(alc.ToString().Contains("\"\" "));
            Assert(alc.ToString().Contains("\" My.CustomAssemblyLoadContext"));
            Assert(alc.ToString().Contains(" #"));
        }
        catch (Exception e)
        {
            Assert(false, e.ToString());
        }
    }

    public static void CustomName()
    {
        try
        {
            Console.WriteLine("CustomName()");

            // ALC should be a concrete class
            AssemblyLoadContext alc = new AssemblyLoadContext("CustomName");
            Assert(PropertyAllContainsContext(alc));

            Console.WriteLine(alc.Name);
            Assert(alc.Name == "CustomName");

            Console.WriteLine(alc.GetType().ToString());
            Assert(alc.GetType().ToString() == "System.Runtime.Loader.AssemblyLoadContext");

            Console.WriteLine(alc.ToString());
            Assert(alc.ToString().Contains("\"CustomName"));
            Assert(alc.ToString().Contains("\" System.Runtime.Loader.AssemblyLoadContext"));
            Assert(alc.ToString().Contains(" #"));
        }
        catch (Exception e)
        {
            Assert(false, e.ToString());
        }
    }

    public static void GetLoadContextForDynamicAssembly(bool isCollectible)
    {
        try
        {
            Console.WriteLine($"{nameof(GetLoadContextForDynamicAssembly)}; isCollectible={isCollectible}");

            AssemblyLoadContext alc = new AssemblyLoadContext($"ALC - {isCollectible}", isCollectible);
            AssemblyBuilder assemblyBuilder;
            AssemblyName assemblyName = new AssemblyName($"DynamicAssembly_{Guid.NewGuid():N}");

            using (alc.EnterContextualReflection())
            {
                assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);
            }

            AssemblyLoadContext? context = AssemblyLoadContext.GetLoadContext(assemblyBuilder);

            Assert(context != null);
            Assert(alc == context);
            Assert(alc.Assemblies.Any(a => AssemblyName.ReferenceMatchesDefinition(a.GetName(), assemblyName)));
        }
        catch (Exception e)
        {
            Assert(false, e.ToString());
        }
    }

    public static int Main()
    {
        foreach (AssemblyLoadContext alc in AssemblyLoadContext.All)
        {
            Console.WriteLine(alc.ToString());
            foreach (Assembly a in alc.Assemblies)
            {
                Console.WriteLine(a.ToString());
            }
        }

        DefaultName();
        AssemblyLoadFileName();
        AssemblyLoadByteArrayName();
        CustomWOName();
        CustomName();
        GetLoadContextForDynamicAssembly(true);
        GetLoadContextForDynamicAssembly(false);

        foreach (AssemblyLoadContext alc in AssemblyLoadContext.All)
        {
            Console.WriteLine(alc.ToString());
            foreach (Assembly a in alc.Assemblies)
            {
                Console.WriteLine(a.ToString());
            }
        }

        if (passed)
        {
            Console.WriteLine("Test PASSED!");
            return 100;
        }
        else
        {
            Console.WriteLine("Test FAILED!");
            return -1;
        }
    }
}
