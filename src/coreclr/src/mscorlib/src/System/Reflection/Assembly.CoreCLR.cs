// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Security.Policy;
using System.IO;
using System.Configuration.Assemblies;
using StackCrawlMark = System.Threading.StackCrawlMark;
using System.Runtime.Serialization;
using System.Diagnostics.Contracts;
using System.Runtime.Loader;

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
            return Assembly.LoadFrom(requestedAssemblyPath);
        }

        public static Assembly LoadFrom(String assemblyFile)
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
                        AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(LoadFromResolveHandler);
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

        // Evidence is protected in Assembly.Load()
        [Obsolete("This method is obsolete and will be removed in a future release of the .NET Framework. Please use an overload of LoadFrom which does not take an Evidence parameter. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        internal static Assembly LoadFrom(String assemblyFile,
                                        Evidence securityEvidence)
        {
            Contract.Ensures(Contract.Result<Assembly>() != null);

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;

            return RuntimeAssembly.InternalLoadFrom(
                assemblyFile,
                securityEvidence,
                null, // hashValue
                AssemblyHashAlgorithm.None,
                false,// forIntrospection);
                ref stackMark);
        }

        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        public static Assembly LoadFrom(String assemblyFile,
                                        byte[] hashValue,
                                        AssemblyHashAlgorithm hashAlgorithm)
        {
            throw new NotSupportedException(SR.NotSupported_AssemblyLoadFromHash);
        }

        // Locate an assembly by the long form of the assembly name. 
        // eg. "Toolbox.dll, version=1.1.10.1220, locale=en, publickey=1234567890123456789012345678901234567890"
        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        public static Assembly Load(String assemblyString)
        {
            Contract.Ensures(Contract.Result<Assembly>() != null);
            Contract.Ensures(!Contract.Result<Assembly>().ReflectionOnly);

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeAssembly.InternalLoad(assemblyString, null, ref stackMark, false /*forIntrospection*/);
        }

        // Returns type from the assembly while keeping compatibility with Assembly.Load(assemblyString).GetType(typeName) for managed types.
        // Calls Type.GetType for WinRT types.
        // Note: Type.GetType fails for assembly names that start with weird characters like '['. By calling it for managed types we would 
        // break AppCompat.
        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        internal static Type GetType_Compat(String assemblyString, String typeName)
        {
            // Normally we would get the stackMark only in public APIs. This is internal API, but it is AppCompat replacement of public API 
            // call Assembly.Load(assemblyString).GetType(typeName), therefore we take the stackMark here as well, to be fully compatible with 
            // the call sequence.
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;

            RuntimeAssembly assembly;
            AssemblyName assemblyName = RuntimeAssembly.CreateAssemblyName(
                assemblyString,
                false /*forIntrospection*/,
                out assembly);

            if (assembly == null)
            {
                if (assemblyName.ContentType == AssemblyContentType.WindowsRuntime)
                {
                    return Type.GetType(typeName + ", " + assemblyString, true /*throwOnError*/, false /*ignoreCase*/);
                }

                assembly = RuntimeAssembly.InternalLoadAssemblyName(
                    assemblyName, null, null, ref stackMark,
                    true /*thrownOnFileNotFound*/, false /*forIntrospection*/);
            }
            return assembly.GetType(typeName, true /*throwOnError*/, false /*ignoreCase*/);
        }

        // Locate an assembly by its name. The name can be strong or
        // weak. The assembly is loaded into the domain of the caller.
        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        public static Assembly Load(AssemblyName assemblyRef)
        {
            Contract.Ensures(Contract.Result<Assembly>() != null);
            Contract.Ensures(!Contract.Result<Assembly>().ReflectionOnly);
            
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
            return RuntimeAssembly.InternalLoadAssemblyName(modifiedAssemblyRef, null, null, ref stackMark, true /*thrownOnFileNotFound*/, false /*forIntrospection*/);
        }

        // Locate an assembly by its name. The name can be strong or
        // weak. The assembly is loaded into the domain of the caller.
        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        internal static Assembly Load(AssemblyName assemblyRef, IntPtr ptrLoadContextBinder)
        {
            Contract.Ensures(Contract.Result<Assembly>() != null);
            Contract.Ensures(!Contract.Result<Assembly>().ReflectionOnly);

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
            return RuntimeAssembly.InternalLoadAssemblyName(modifiedAssemblyRef, null, null, ref stackMark, true /*thrownOnFileNotFound*/, false /*forIntrospection*/, ptrLoadContextBinder);
        }

        // Loads the assembly with a COFF based IMAGE containing
        // an emitted assembly. The assembly is loaded into the domain
        // of the caller. The second parameter is the raw bytes
        // representing the symbol store that matches the assembly.
        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        public static Assembly Load(byte[] rawAssembly,
                                    byte[] rawSymbolStore)
        {
            Contract.Ensures(Contract.Result<Assembly>() != null);
            Contract.Ensures(!Contract.Result<Assembly>().ReflectionOnly);

            AppDomain.CheckLoadByteArraySupported();

            if (rawAssembly == null)
                throw new ArgumentNullException(nameof(rawAssembly));
            AssemblyLoadContext alc = new IndividualAssemblyLoadContext();
            MemoryStream assemblyStream = new MemoryStream(rawAssembly);
            MemoryStream symbolStream = (rawSymbolStore != null) ? new MemoryStream(rawSymbolStore) : null;
            return alc.LoadFromStream(assemblyStream, symbolStream);
        }

        private static Dictionary<string, Assembly> s_loadfile = new Dictionary<string, Assembly>();

        public static Assembly LoadFile(String path)
        {
            Contract.Ensures(Contract.Result<Assembly>() != null);
            Contract.Ensures(!Contract.Result<Assembly>().ReflectionOnly);

            AppDomain.CheckLoadFileSupported();

            Assembly result = null;
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            if (PathInternal.IsPartiallyQualified(path))
            {
                throw new ArgumentException(SR.Argument_AbsolutePathRequired, nameof(path));
            }

            string normalizedPath = Path.GetFullPath(path);

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

        /*
         * Get the assembly that the current code is running from.
         */
        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod 
        public static Assembly GetExecutingAssembly()
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeAssembly.GetExecutingAssembly(ref stackMark);
        }

        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        public static Assembly GetCallingAssembly()
        {
            // LookForMyCallersCaller is not guarantee to return the correct stack frame
            // because of inlining, tail calls, etc. As a result GetCallingAssembly is not 
            // ganranteed to return the correct result. We should document it as such.
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCallersCaller;
            return RuntimeAssembly.GetExecutingAssembly(ref stackMark);
        }

        public static Assembly GetEntryAssembly()
        {
            AppDomainManager domainManager = AppDomain.CurrentDomain.DomainManager;
            if (domainManager == null)
                domainManager = new AppDomainManager();
            return domainManager.EntryAssembly;
        }
    }
}
