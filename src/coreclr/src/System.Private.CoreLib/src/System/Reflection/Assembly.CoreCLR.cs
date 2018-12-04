// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Configuration.Assemblies;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Loader;
using StackCrawlMark = System.Threading.StackCrawlMark;

namespace System.Reflection
{
    public abstract partial class Assembly : ICustomAttributeProvider, ISerializable
    {
        private static volatile bool s_LoadFromResolveHandlerSetup = false;
        private static object s_syncRootLoadFrom = new object();
        private static List<string> s_LoadFromAssemblyList = new List<string>();
        private static object s_syncLoadFromAssemblyList = new object();

        private static Assembly LoadFromResolveHandler(object sender, ResolveEventArgs args)
        {
            Assembly requestingAssembly = args.RequestingAssembly;
            if (requestingAssembly == null)
            {
                return null;
            }

            // Requesting assembly for LoadFrom is always loaded in defaultContext - proceed only if that
            // is the case.
            if (AssemblyLoadContext.Default != AssemblyLoadContext.GetLoadContext(requestingAssembly))
                return null;

            // Get the path where requesting assembly lives and check if it is in the list
            // of assemblies for which LoadFrom was invoked.
            bool fRequestorLoadedViaLoadFrom = false;
            string requestorPath = Path.GetFullPath(requestingAssembly.Location);
            if (string.IsNullOrEmpty(requestorPath))
                return null;

            lock(s_syncLoadFromAssemblyList)
            {
                fRequestorLoadedViaLoadFrom = s_LoadFromAssemblyList.Contains(requestorPath);
            }
            
            // If the requestor assembly was not loaded using LoadFrom, exit.
            if (!fRequestorLoadedViaLoadFrom)
                return null;

            // Requestor assembly was loaded using loadFrom, so look for its dependencies
            // in the same folder as it.
            // Form the name of the assembly using the path of the assembly that requested its load.
            AssemblyName requestedAssemblyName = new AssemblyName(args.Name);
            string requestedAssemblyPath = Path.Combine(Path.GetDirectoryName(requestorPath), requestedAssemblyName.Name+".dll");

            // Load the dependency via LoadFrom so that it goes through the same path of being in the LoadFrom list.
            Assembly resolvedAssembly = null;

            try
            {
                resolvedAssembly = Assembly.LoadFrom(requestedAssemblyPath);
            }
            catch(FileNotFoundException)
            {
                // Catch FileNotFoundException when attempting to resolve assemblies via this handler to account for missing assemblies.
                resolvedAssembly = null;
            }

            return resolvedAssembly;
        }

        public static Assembly LoadFrom(string assemblyFile)
        {
            if (assemblyFile == null)
                throw new ArgumentNullException(nameof(assemblyFile));
            
            string fullPath = Path.GetFullPath(assemblyFile);

            if (!s_LoadFromResolveHandlerSetup)
            {
                lock (s_syncRootLoadFrom)
                {
                    if (!s_LoadFromResolveHandlerSetup)
                    {
                        AssemblyLoadContext.AssemblyResolve += LoadFromResolveHandler;
                        s_LoadFromResolveHandlerSetup = true;
                    }
                }
            }

            // Add the path to the LoadFrom path list which we will consult
            // before handling the resolves in our handler.
            lock(s_syncLoadFromAssemblyList)
            {
                if (!s_LoadFromAssemblyList.Contains(fullPath))
                {
                    s_LoadFromAssemblyList.Add(fullPath);
                }
            }

            return AssemblyLoadContext.Default.LoadFromAssemblyPath(fullPath);
        }

        public static Assembly LoadFrom(string assemblyFile,
                                        byte[] hashValue,
                                        AssemblyHashAlgorithm hashAlgorithm)
        {
            throw new NotSupportedException(SR.NotSupported_AssemblyLoadFromHash);
        }

        // Locate an assembly by the long form of the assembly name. 
        // eg. "Toolbox.dll, version=1.1.10.1220, locale=en, publickey=1234567890123456789012345678901234567890"
        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        public static Assembly Load(string assemblyString)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeAssembly.InternalLoad(assemblyString, ref stackMark);
        }

        // Locate an assembly by its name. The name can be strong or
        // weak. The assembly is loaded into the domain of the caller.
        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        public static Assembly Load(AssemblyName assemblyRef)
        {
            AssemblyName modifiedAssemblyRef = null;
            if (assemblyRef != null && assemblyRef.CodeBase != null)
            {
                modifiedAssemblyRef = (AssemblyName)assemblyRef.Clone();
                modifiedAssemblyRef.CodeBase = null;
            }
            else
            {
                modifiedAssemblyRef = assemblyRef;
            }
            
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeAssembly.InternalLoadAssemblyName(modifiedAssemblyRef, null, ref stackMark, true /*thrownOnFileNotFound*/);
        }

        // Locate an assembly by its name. The name can be strong or
        // weak. The assembly is loaded into the domain of the caller.
        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        internal static Assembly Load(AssemblyName assemblyRef, IntPtr ptrLoadContextBinder)
        {
            AssemblyName modifiedAssemblyRef = null;
            if (assemblyRef != null && assemblyRef.CodeBase != null)
            {
                modifiedAssemblyRef = (AssemblyName)assemblyRef.Clone();
                modifiedAssemblyRef.CodeBase = null;
            }
            else
            {
                modifiedAssemblyRef = assemblyRef;
            }

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeAssembly.InternalLoadAssemblyName(modifiedAssemblyRef, null, ref stackMark, true /*thrownOnFileNotFound*/, ptrLoadContextBinder);
        }

        // Loads the assembly with a COFF based IMAGE containing
        // an emitted assembly. The assembly is loaded into a fully isolated ALC with resolution fully deferred to the AssemblyLoadContext.Default.
        // The second parameter is the raw bytes representing the symbol store that matches the assembly.
        public static Assembly Load(byte[] rawAssembly,
                                    byte[] rawSymbolStore)
        {
            if (rawAssembly == null)
                throw new ArgumentNullException(nameof(rawAssembly));

#if FEATURE_APPX
            if (ApplicationModel.IsUap)
                throw new NotSupportedException(SR.Format(SR.NotSupported_AppX, "Assembly.Load(byte[], ...)"));
#endif

            AssemblyLoadContext alc = new IndividualAssemblyLoadContext();
            MemoryStream assemblyStream = new MemoryStream(rawAssembly);
            MemoryStream symbolStream = (rawSymbolStore != null) ? new MemoryStream(rawSymbolStore) : null;
            return alc.LoadFromStream(assemblyStream, symbolStream);
        }

        private static Dictionary<string, Assembly> s_loadfile = new Dictionary<string, Assembly>();

        public static Assembly LoadFile(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

#if FEATURE_APPX
            if (ApplicationModel.IsUap)
                throw new NotSupportedException(SR.Format(SR.NotSupported_AppX, "Assembly.LoadFile"));
#endif

            if (PathInternal.IsPartiallyQualified(path))
            {
                throw new ArgumentException(SR.Argument_AbsolutePathRequired, nameof(path));
            }

            string normalizedPath = Path.GetFullPath(path);

            Assembly result = null;
            lock (s_loadfile)
            {
                if (s_loadfile.TryGetValue(normalizedPath, out result))
                    return result;
                AssemblyLoadContext alc = new IndividualAssemblyLoadContext();
                result = alc.LoadFromAssemblyPath(normalizedPath);
                s_loadfile.Add(normalizedPath, result);
            }
            return result;
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void GetExecutingAssembly(StackCrawlMarkHandle stackMark, ObjectHandleOnStack retAssembly);

        internal static RuntimeAssembly GetExecutingAssembly(ref StackCrawlMark stackMark)
        {
            RuntimeAssembly retAssembly = null;
            GetExecutingAssembly(JitHelpers.GetStackCrawlMarkHandle(ref stackMark), JitHelpers.GetObjectHandleOnStack(ref retAssembly));
            return retAssembly;
        }

        // Get the assembly that the current code is running from.
        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod 
        public static Assembly GetExecutingAssembly()
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return GetExecutingAssembly(ref stackMark);
        }

        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        public static Assembly GetCallingAssembly()
        {
            // LookForMyCallersCaller is not guaranteed to return the correct stack frame
            // because of inlining, tail calls, etc. As a result GetCallingAssembly is not 
            // guaranteed to return the correct result. It's also documented as such.
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCallersCaller;
            return GetExecutingAssembly(ref stackMark);
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void GetEntryAssembly(ObjectHandleOnStack retAssembly);

        // internal test hook
        private static bool s_forceNullEntryPoint = false;

        public static Assembly GetEntryAssembly()
        {
            if (s_forceNullEntryPoint)
                return null;

            RuntimeAssembly entryAssembly = null;
            GetEntryAssembly(JitHelpers.GetObjectHandleOnStack(ref entryAssembly));
            return entryAssembly;
        }
    }
}
