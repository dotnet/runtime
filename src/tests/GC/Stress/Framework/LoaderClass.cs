// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Reflection;
using System.Threading;
using System.IO;
using System.Runtime.Loader;
#if !PROJECTK_BUILD
using System.Runtime.Remoting;
#endif

public enum eReasonForUnload
{
    GeneralUnload,
    AssemblyLoad,
    AppDomainUnload,
    Replay
}


/// <summary>
/// The LoaderClass is how we communicate with other app domains.  It has to do 3 important things:  1) Load assemblies into the
/// remote app domain (via Load/LoadFrom), 2) get back an object which represents the test (this is either an I...RelibilityTest or 
/// a string indicating the assembly to run) (via GetTest), and 3) verify  that our app domain is still running & healthy (via StillAlive)
/// </summary>
public class LoaderClass
#if !PROJECTK_BUILD
    : MarshalByRefObject
#endif 
{
    private Assembly assem;
    string assembly;
#if !PROJECTK_BUILD
    ReliabilityFramework myRf;
#endif 

    public LoaderClass()
    {
    }

    public void SuppressConsole()
    {
        Console.SetOut(System.IO.TextWriter.Null);
    }

#pragma warning disable SYSLIB0012 // Assembly.CodeBase is obsolete

    /// <summary>
    /// Executes a LoadFrom in the app domain LoaderClass has been loaded into.  Attempts to load a given assembly, looking in the
    /// given paths & the current directory.
    /// </summary>
    /// <param name="path">The assembly to load</param>
    /// <param name="paths">Paths to search for the given assembly</param>
    public void LoadFrom(string path, string[] paths
#if !PROJECTK_BUILD
        , ReliabilityFramework rf
#endif
        )
    {
#if !PROJECTK_BUILD
        myRf = rf;

        AssemblyName an = new AssemblyName();
        an.CodeBase = assembly = path;

        //register AssemblyLoad and DomainUnload events
        AppDomain.CurrentDomain.AssemblyLoad += new AssemblyLoadEventHandler(this.UnloadOnAssemblyLoad);
        AppDomain.CurrentDomain.DomainUnload += new EventHandler(this.UnloadOnDomainUnload);
#else
        AssemblyLoadContext alc = AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly());
#endif
        try
        {
#if !PROJECTK_BUILD
            assem = Assembly.Load(an);
#else
            assembly = path;
            assem = alc.LoadFromAssemblyPath(assembly);
#endif
        }
        catch
        {
            try
            {
                FileInfo fi = new FileInfo(path);
                assembly = fi.FullName;
#if !PROJECTK_BUILD
                an = new AssemblyName();
                an.CodeBase = assembly;

                assem = Assembly.Load(an);
#else
                assem = alc.LoadFromAssemblyPath(assembly);
#endif
            }
            catch
            {
                if (paths != null)
                {
                    foreach (string basePath in paths)
                    {
                        try
                        {
                            assembly = ReliabilityConfig.ConvertPotentiallyRelativeFilenameToFullPath(basePath, path);
#if !PROJECTK_BUILD
                            an = new AssemblyName();
                            an.CodeBase = assembly;

                            assem = Assembly.Load(an);
#else
                            assem = alc.LoadFromAssemblyPath(assembly);
#endif
                            break;
                        }
                        catch
                        {
                        }
                    }
                }
            }
        }
    }
#pragma warning restore SYSLIB0012

    /// <summary>
    /// Attempts to load an assembly w/ a simple name, looking in the given paths if a normal load fails
    /// </summary>
    /// <param name="assemblyName">The assembly name to load</param>
    /// <param name="paths">paths to look in if the initial load fails</param>
    public void Load(string assemblyName, string[] paths
#if !PROJECTK_BUILD
        , ReliabilityFramework rf
#endif
        )
    {
#if !PROJECTK_BUILD
        myRf = rf;

        AssemblyName an = new AssemblyName(assemblyName);
        assembly = assemblyName;
#endif
        try
        {
#if !PROJECTK_BUILD
            assem = Assembly.Load(an);
#else
            LoadFrom(assemblyName + ".dll", paths);
#endif
        }
        catch
        {
            Console.WriteLine("Load failed for: {0}", assemblyName);

#if !PROJECTK_BUILD
            LoadFrom(assemblyName, paths, rf);	// couldn't load the assembly, try doing a LoadFrom with paths.
#endif
        }
    }

    /// <summary>
    /// Checks the test's entry point for an STA or MTA thread attribute.  Returns
    /// the apartment state if set, or ApartmentState.Unknown
    /// </summary>
    /// <returns></returns>
    public ApartmentState CheckMainForThreadType()
    {
        if (assem != null)
        {
            MethodInfo mi = assem.EntryPoint;
            object[] attrs = mi.GetCustomAttributes(typeof(STAThreadAttribute), false);
            if (attrs != null && attrs.Length > 0)
            {
                return (ApartmentState.STA);
            }
            attrs = mi.GetCustomAttributes(typeof(MTAThreadAttribute), false);
            if (attrs != null && attrs.Length > 0)
            {
                return (ApartmentState.MTA);
            }
        }
        return (ApartmentState.Unknown);
    }

    /// <summary>
    /// Gets back the test object.  If the test is an assembly (to be executed) we return a string which is the full path.
    /// If we're an ISingleReliabilityTest or IMultipleReliabilityTest we return the object it's self.
    /// </summary>
    /// <returns>a string (executable test) or a ISingleReliabilityTest or IMultipleReliabilityTest</returns>
    public Object GetTest()
    {
        if (assem == null)
            throw new Exception("Could not load specified assembly: " + assembly);

        Type[] assemTypes = null;
        try
        {
            assemTypes = assem.GetTypes();
        }
        catch (System.Reflection.ReflectionTypeLoadException e)
        {
            if (assembly.ToLower().IndexOf(".dll") != -1)
                return (assembly);
            throw new Exception(String.Format("Couldn't GetTypes for {0} ({1})", assembly, e.Message));
        }

        // now create an instance of the correct type in the app domain
        Object ourObj = null;
        if (assemTypes != null)
        {
            foreach (Type t in assemTypes)
            {
                if (t.GetInterface("ISingleReliabilityTest") != null || t.GetInterface("IMultipleReliabilityTest") != null)
                {
#if !PROJECTK_BUILD
                    ObjectHandle handle;
                    if (assembly.IndexOf("\\") != -1)
                        handle = AppDomain.CurrentDomain.CreateInstanceFrom(assembly, t.FullName);
                    else if (assembly.ToLower().IndexOf(".exe") == -1 && assembly.ToLower().IndexOf(".dll") == -1)
                        handle = AppDomain.CurrentDomain.CreateInstance(assembly, t.FullName);
                    else
                        handle = AppDomain.CurrentDomain.CreateInstanceFrom(".\\" + assembly, t.FullName);
                    if (handle != null)
                    {
                        ourObj = handle.Unwrap();
                        break;
                    }
#else
                    ourObj = Activator.CreateInstance(t);
#endif
                }

            }
        }

        if (ourObj == null)
            return (assembly);
#if !PROJECTK_BUILD
        if (!(ourObj is MarshalByRefObject))
            throw new ArgumentException("All tests implementing ISingleReliabilityTest or IMultipleReliabilityTest must inherit from MarshalByRefObject");
#endif

        return (ourObj);
    }
#if !PROJECTK_BUILD
    /// <summary>
    /// This stops our MBR from being deallocated by the distributed GC mechanism in remoting.
    /// </summary>
    /// <returns></returns>
    public override Object InitializeLifetimeService()
    {
        return (null);
    }
    public void UnloadOnAssemblyLoad(object sender, AssemblyLoadEventArgs args)
    {
        myRf.UnloadOnEvent(AppDomain.CurrentDomain, eReasonForUnload.AssemblyLoad);
    }
    public void UnloadOnDomainUnload(object sender, EventArgs args)
    {
        myRf.UnloadOnEvent(AppDomain.CurrentDomain, eReasonForUnload.AppDomainUnload);
    }
#endif
}


