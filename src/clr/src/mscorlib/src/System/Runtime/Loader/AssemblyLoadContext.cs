// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Reflection;
using System.Diagnostics.Contracts;
using System.IO;
using System.Runtime.Versioning;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;

#if FEATURE_HOST_ASSEMBLY_RESOLVER

namespace System.Runtime.Loader
{
    [System.Security.SecuritySafeCritical]
    public abstract class AssemblyLoadContext
    {
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern bool OverrideDefaultAssemblyLoadContextForCurrentDomain(IntPtr ptrNativeAssemblyLoadContext);

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern bool CanUseAppPathAssemblyLoadContextInCurrentDomain();
        
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern IntPtr InitializeAssemblyLoadContext(IntPtr ptrAssemblyLoadContext);
        
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern IntPtr LoadFromStream(IntPtr ptrNativeAssemblyLoadContext, IntPtr ptrAssemblyArray, int iAssemblyArrayLen, IntPtr ptrSymbols, int iSymbolArrayLen, ObjectHandleOnStack retAssembly);
        
#if FEATURE_MULTICOREJIT
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        internal static extern void InternalSetProfileRoot(string directoryPath);

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        internal static extern void InternalStartProfile(string profile, IntPtr ptrNativeAssemblyLoadContext);
#endif // FEATURE_MULTICOREJIT

        [System.Security.SecuritySafeCritical]
        protected AssemblyLoadContext()
        {
            // Initialize the VM side of AssemblyLoadContext if not already done.
            GCHandle gchALC = GCHandle.Alloc(this);
            IntPtr ptrALC = GCHandle.ToIntPtr(gchALC);
            m_pNativeAssemblyLoadContext = InitializeAssemblyLoadContext(ptrALC);
        }

        internal AssemblyLoadContext(bool fDummy)
        {
        }
        
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern void LoadFromPath(IntPtr ptrNativeAssemblyLoadContext, string ilPath, string niPath, ObjectHandleOnStack retAssembly);
        
        // These are helpers that can be used by AssemblyLoadContext derivations.
        // They are used to load assemblies in DefaultContext.
        protected Assembly LoadFromAssemblyPath(string assemblyPath)
        {
            if (assemblyPath == null)
            {
                throw new ArgumentNullException("assemblyPath");
            }

            if (Path.IsRelative(assemblyPath))
            {
                throw new ArgumentException( Environment.GetResourceString("Argument_AbsolutePathRequired"), "assemblyPath");
            }

            RuntimeAssembly loadedAssembly = null;
            LoadFromPath(m_pNativeAssemblyLoadContext, assemblyPath, null, JitHelpers.GetObjectHandleOnStack(ref loadedAssembly));
            return loadedAssembly;
        }
        
        protected Assembly LoadFromNativeImagePath(string nativeImagePath, string assemblyPath)
        {
            if (nativeImagePath == null)
            {
                throw new ArgumentNullException("nativeImagePath");
            }

            if (Path.IsRelative(nativeImagePath))
            {
                throw new ArgumentException( Environment.GetResourceString("Argument_AbsolutePathRequired"), "nativeImagePath");
            }

            // Check if the nativeImagePath has ".ni.dll" or ".ni.exe" extension
            if (!(nativeImagePath.EndsWith(".ni.dll", StringComparison.InvariantCultureIgnoreCase) || 
                  nativeImagePath.EndsWith(".ni.exe", StringComparison.InvariantCultureIgnoreCase)))
            {
                throw new ArgumentException("nativeImagePath");
            }

            if (assemblyPath != null && Path.IsRelative(assemblyPath))
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_AbsolutePathRequired"), "assemblyPath");
            }

            // Basic validation has succeeded - lets try to load the NI image.
            // Ask LoadFile to load the specified assembly in the DefaultContext
            RuntimeAssembly loadedAssembly = null;
            LoadFromPath(m_pNativeAssemblyLoadContext, assemblyPath, nativeImagePath, JitHelpers.GetObjectHandleOnStack(ref loadedAssembly));
            return loadedAssembly;
        }
        
        protected Assembly LoadFromStream(Stream assembly)
        {
            return LoadFromStream(assembly, null);
        }
        
        protected Assembly LoadFromStream(Stream assembly, Stream assemblySymbols)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException("assembly");
            }
            
            int iAssemblyStreamLength = (int)assembly.Length;
            int iSymbolLength = 0;
            
            // Allocate the byte[] to hold the assembly
            byte[] arrAssembly = new byte[iAssemblyStreamLength];
            
            // Copy the assembly to the byte array
            assembly.Read(arrAssembly, 0, iAssemblyStreamLength);
            
            // Get the symbol stream in byte[] if provided
            byte[] arrSymbols = null;
            if (assemblySymbols != null)
            {
                iSymbolLength = (int)assemblySymbols.Length;
                arrSymbols = new byte[iSymbolLength];
                
                assemblySymbols.Read(arrSymbols, 0, iSymbolLength);
            }
            
            RuntimeAssembly loadedAssembly = null;
            unsafe 
            {
                fixed(byte *ptrAssembly = arrAssembly, ptrSymbols = arrSymbols)
                {
                    LoadFromStream(m_pNativeAssemblyLoadContext, new IntPtr(ptrAssembly), iAssemblyStreamLength, new IntPtr(ptrSymbols), iSymbolLength, JitHelpers.GetObjectHandleOnStack(ref loadedAssembly));
                }
            }
            
            return loadedAssembly;
        }
        
        // Custom AssemblyLoadContext implementations can override this
        // method to perform custom processing and use one of the protected
        // helpers above to load the assembly.
        protected abstract Assembly Load(AssemblyName assemblyName);
        
        // This method is invoked by the VM when using the host-provided assembly load context
        // implementation.
        private static Assembly Resolve(IntPtr gchManagedAssemblyLoadContext, AssemblyName assemblyName)
        {
            AssemblyLoadContext context = (AssemblyLoadContext)(GCHandle.FromIntPtr(gchManagedAssemblyLoadContext).Target);
            
            return context.LoadFromAssemblyName(assemblyName);
        }
        
        public Assembly LoadFromAssemblyName(AssemblyName assemblyName)
        {
            // AssemblyName is mutable. Cache the expected name before anybody gets a chance to modify it.
            string requestedSimpleName = assemblyName.Name;
 
            Assembly assembly = Load(assemblyName);
            if (assembly == null)
            {
                throw new FileLoadException(Environment.GetResourceString("IO.FileLoad"), requestedSimpleName);
            }
            
            // Get the name of the loaded assembly
            string loadedSimpleName = null;
            
            // Derived type's Load implementation is expected to use one of the LoadFrom* methods to get the assembly
            // which is a RuntimeAssembly instance. However, since Assembly type can be used build any other artifact (e.g. AssemblyBuilder),
            // we need to check for RuntimeAssembly.
            RuntimeAssembly rtLoadedAssembly = assembly as RuntimeAssembly;
            if (rtLoadedAssembly != null)
            {
                loadedSimpleName = rtLoadedAssembly.GetSimpleName();
            }
            
            // The simple names should match at the very least
            if (String.IsNullOrEmpty(loadedSimpleName) || (!requestedSimpleName.Equals(loadedSimpleName, StringComparison.InvariantCultureIgnoreCase)))
                throw new InvalidOperationException(Environment.GetResourceString("Argument_CustomAssemblyLoadContextRequestedNameMismatch"));
 
            return assembly;

        }
        
        // Custom AssemblyLoadContext implementations can override this
        // method to perform the load of unmanaged native dll
        // This function needs to return the HMODULE of the dll it loads
        protected virtual  IntPtr LoadUnmanagedDll(String unmanagedDllName)
        {
            //defer to default coreclr poilcy of loading unmanaged dll
            return IntPtr.Zero; 
        }

        // This method is invoked by the VM when using the host-provided assembly load context
        // implementation.
        private static IntPtr ResolveUnmanagedDll(String unmanagedDllName, IntPtr gchManagedAssemblyLoadContext)
        {
            AssemblyLoadContext context = (AssemblyLoadContext)(GCHandle.FromIntPtr(gchManagedAssemblyLoadContext).Target);
            return context.LoadUnmanagedDll(unmanagedDllName);
        }

        public static AssemblyLoadContext Default
        {
            get
            {
                while (m_DefaultAssemblyLoadContext == null)
                {
                    // Try to initialize the default assembly load context with apppath one if we are allowed to
                    if (AssemblyLoadContext.CanUseAppPathAssemblyLoadContextInCurrentDomain())
                    {
#pragma warning disable 0420
                        Interlocked.CompareExchange(ref m_DefaultAssemblyLoadContext, new AppPathAssemblyLoadContext(), null);
                        break;
#pragma warning restore 0420
                    }
                    // Otherwise, need to yield to other thread to finish the initialization
                    Thread.Yield();
                }
                
                return m_DefaultAssemblyLoadContext;
            }
        }

        // This will be used to set the AssemblyLoadContext for DefaultContext, for the AppDomain,
        // by a host. Once set, the runtime will invoke the LoadFromAssemblyName method against it to perform
        // assembly loads for the DefaultContext.
        //
        // This method will throw if the Default AssemblyLoadContext is already set or the Binding model is already locked.
        public static void InitializeDefaultContext(AssemblyLoadContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }
            
            // Try to override the default assembly load context
            if (!AssemblyLoadContext.OverrideDefaultAssemblyLoadContextForCurrentDomain(context.m_pNativeAssemblyLoadContext))
            {
                throw new InvalidOperationException(Environment.GetResourceString("AppDomain_BindingModelIsLocked"));
            }
            
            // Update the managed side as well.
            m_DefaultAssemblyLoadContext = context;
        }
        
        // This call opens and closes the file, but does not add the
        // assembly to the domain.
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern AssemblyName nGetFileInformation(String s);
        
        // Helper to return AssemblyName corresponding to the path of an IL assembly
        public static AssemblyName GetAssemblyName(string assemblyPath)
        {
            if (assemblyPath == null)
            {
                throw new ArgumentNullException("assemblyPath");
            }
            
            String fullPath = Path.GetFullPathInternal(assemblyPath);
            return nGetFileInformation(fullPath);
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern IntPtr GetLoadContextForAssembly(RuntimeAssembly assembly);
        
        // Returns the load context in which the specified assembly has been loaded
        public static AssemblyLoadContext GetLoadContext(Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException("assembly");
            }
            
            AssemblyLoadContext loadContextForAssembly = null;
            IntPtr ptrAssemblyLoadContext = GetLoadContextForAssembly((RuntimeAssembly)assembly);
            if (ptrAssemblyLoadContext == IntPtr.Zero)
            {
                // If the load context is returned null, then the assembly was bound using the TPA binder
                // and we shall return reference to the active "Default" binder - which could be the TPA binder
                // or an overridden CLRPrivBinderAssemblyLoadContext instance.
                loadContextForAssembly = AssemblyLoadContext.Default;
            }
            else
            {
                loadContextForAssembly = (AssemblyLoadContext)(GCHandle.FromIntPtr(ptrAssemblyLoadContext).Target);
            }
            
            return loadContextForAssembly;
        }
        
        // Set the root directory path for profile optimization.
        public void SetProfileOptimizationRoot(string directoryPath)
        {
#if FEATURE_MULTICOREJIT
            InternalSetProfileRoot(directoryPath);
#endif // FEATURE_MULTICOREJIT
        }

        // Start profile optimization for the specified profile name.
        public void StartProfileOptimization(string profile)
        {
#if FEATURE_MULTICOREJIT
            InternalStartProfile(profile, m_pNativeAssemblyLoadContext);
#endif // FEATURE_MULTICOREJI
        }
        
        // Contains the reference to VM's representation of the AssemblyLoadContext
        private IntPtr m_pNativeAssemblyLoadContext;
        
        // Each AppDomain contains the reference to its AssemblyLoadContext instance, if one is
        // specified by the host. By having the field as a static, we are
        // making it an AppDomain-wide field.
        private static volatile AssemblyLoadContext m_DefaultAssemblyLoadContext;
    }

    [System.Security.SecuritySafeCritical]
    class AppPathAssemblyLoadContext : AssemblyLoadContext
    {
        internal AppPathAssemblyLoadContext() : base(false)
        {
        }

        [System.Security.SecuritySafeCritical]  
        protected override Assembly Load(AssemblyName assemblyName)
        {
            return Assembly.Load(assemblyName);
        }
    }
}

#endif // FEATURE_HOST_ASSEMBLY_RESOLVER