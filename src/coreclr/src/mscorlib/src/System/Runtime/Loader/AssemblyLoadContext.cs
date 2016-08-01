// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


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
        private static extern IntPtr InitializeAssemblyLoadContext(IntPtr ptrAssemblyLoadContext, bool fRepresentsTPALoadContext);

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern IntPtr LoadFromAssemblyName(IntPtr ptrNativeAssemblyLoadContext, bool fRepresentsTPALoadContext);
        
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

        protected AssemblyLoadContext()
        {
            // Initialize the ALC representing non-TPA LoadContext
            InitializeLoadContext(false);
        }

        internal AssemblyLoadContext(bool fRepresentsTPALoadContext)
        {
            // Initialize the ALC representing TPA LoadContext
            InitializeLoadContext(fRepresentsTPALoadContext);
        }
        
        [System.Security.SecuritySafeCritical]
        void InitializeLoadContext(bool fRepresentsTPALoadContext)
        {
            // Initialize the VM side of AssemblyLoadContext if not already done.
            GCHandle gchALC = GCHandle.Alloc(this);
            IntPtr ptrALC = GCHandle.ToIntPtr(gchALC);
            m_pNativeAssemblyLoadContext = InitializeAssemblyLoadContext(ptrALC, fRepresentsTPALoadContext);

            // Initialize event handlers to be null by default
            Resolving = null;
            Unloading = null;

            // Since unloading an AssemblyLoadContext is not yet implemented, this is a temporary solution to raise the
            // Unloading event on process exit. Register for the current AppDomain's ProcessExit event, and the handler will in
            // turn raise the Unloading event.
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern void LoadFromPath(IntPtr ptrNativeAssemblyLoadContext, string ilPath, string niPath, ObjectHandleOnStack retAssembly);
        
        // These are helpers that can be used by AssemblyLoadContext derivations.
        // They are used to load assemblies in DefaultContext.
        public Assembly LoadFromAssemblyPath(string assemblyPath)
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
        
        public Assembly LoadFromNativeImagePath(string nativeImagePath, string assemblyPath)
        {
            if (nativeImagePath == null)
            {
                throw new ArgumentNullException("nativeImagePath");
            }

            if (Path.IsRelative(nativeImagePath))
            {
                throw new ArgumentException( Environment.GetResourceString("Argument_AbsolutePathRequired"), "nativeImagePath");
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
        
        public Assembly LoadFromStream(Stream assembly)
        {
            return LoadFromStream(assembly, null);
        }
        
        public Assembly LoadFromStream(Stream assembly, Stream assemblySymbols)
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
            
            return context.ResolveUsingLoad(assemblyName);
        }
        
        // This method is invoked by the VM to resolve an assembly reference using the Resolving event
        // after trying assembly resolution via Load override and TPA load context without success.
        private static Assembly ResolveUsingResolvingEvent(IntPtr gchManagedAssemblyLoadContext, AssemblyName assemblyName)
        {
            AssemblyLoadContext context = (AssemblyLoadContext)(GCHandle.FromIntPtr(gchManagedAssemblyLoadContext).Target);
            
            // Invoke the AssemblyResolve event callbacks if wired up
            return context.ResolveUsingEvent(assemblyName);
        }
        
        private Assembly GetFirstResolvedAssembly(AssemblyName assemblyName)
        {
            Assembly resolvedAssembly = null;

            Func<AssemblyLoadContext, AssemblyName, Assembly> assemblyResolveHandler = Resolving;

            if (assemblyResolveHandler != null)
            {
                // Loop through the event subscribers and return the first non-null Assembly instance
                Delegate [] arrSubscribers = assemblyResolveHandler.GetInvocationList();
                for(int i = 0; i < arrSubscribers.Length; i++)
                {
                    resolvedAssembly = ((Func<AssemblyLoadContext, AssemblyName, Assembly>)arrSubscribers[i])(this, assemblyName);
                    if (resolvedAssembly != null)
                    {
                        break;
                    }
                }
            }
            
            return resolvedAssembly;
        }

        private Assembly ValidateAssemblyNameWithSimpleName(Assembly assembly, string requestedSimpleName)
        {
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
        
        private Assembly ResolveUsingLoad(AssemblyName assemblyName)
        {
            string simpleName = assemblyName.Name;
            Assembly assembly = Load(assemblyName);
            
            if (assembly != null)
            {
                assembly = ValidateAssemblyNameWithSimpleName(assembly, simpleName);
            }
            
            return assembly;
        }
        
        private Assembly ResolveUsingEvent(AssemblyName assemblyName)
        {
            string simpleName = assemblyName.Name;
            
            // Invoke the AssemblyResolve event callbacks if wired up
            Assembly assembly = GetFirstResolvedAssembly(assemblyName);
            if (assembly != null)
            {
                assembly = ValidateAssemblyNameWithSimpleName(assembly, simpleName);
            }
            
            // Since attempt to resolve the assembly via Resolving event is the last option,
            // throw an exception if we do not find any assembly.
            if (assembly == null)
            {
                throw new FileNotFoundException(Environment.GetResourceString("IO.FileLoad"), simpleName);
            }
            
            return assembly;
        }
        
        public Assembly LoadFromAssemblyName(AssemblyName assemblyName)
        {
            // Attempt to load the assembly, using the same ordering as static load, in the current load context.
            Assembly loadedAssembly = Assembly.Load(assemblyName, m_pNativeAssemblyLoadContext);

            return loadedAssembly;
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern IntPtr InternalLoadUnmanagedDllFromPath(string unmanagedDllPath);

        // This method provides a way for overriders of LoadUnmanagedDll() to load an unmanaged DLL from a specific path in a
        // platform-independent way. The DLL is loaded with default load flags.
        protected IntPtr LoadUnmanagedDllFromPath(string unmanagedDllPath)
        {
            if (unmanagedDllPath == null)
            {
                throw new ArgumentNullException("unmanagedDllPath");
            }
            if (unmanagedDllPath.Length == 0)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"), "unmanagedDllPath");
            }
            if (Path.IsRelative(unmanagedDllPath))
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_AbsolutePathRequired"), "unmanagedDllPath");
            }

            return InternalLoadUnmanagedDllFromPath(unmanagedDllPath);
        }
        
        // Custom AssemblyLoadContext implementations can override this
        // method to perform the load of unmanaged native dll
        // This function needs to return the HMODULE of the dll it loads
        protected virtual IntPtr LoadUnmanagedDll(String unmanagedDllName)
        {
            //defer to default coreclr policy of loading unmanaged dll
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
                if (s_DefaultAssemblyLoadContext == null)
                {
                    // Try to initialize the default assembly load context with apppath one if we are allowed to
                    if (AssemblyLoadContext.CanUseAppPathAssemblyLoadContextInCurrentDomain())
                    {
                        // Synchronize access to initializing Default ALC
                        lock(s_initLock)
                        {
                            if (s_DefaultAssemblyLoadContext == null)
                            {
                                s_DefaultAssemblyLoadContext = new AppPathAssemblyLoadContext();
                            }
                        }
                    }
                }
                
                return s_DefaultAssemblyLoadContext;
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
            s_DefaultAssemblyLoadContext = context;
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

            RuntimeAssembly rtAsm = assembly as RuntimeAssembly;
            
            // We only support looking up load context for runtime assemblies.
            if (rtAsm != null)
            {
                IntPtr ptrAssemblyLoadContext = GetLoadContextForAssembly(rtAsm);
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

        private void OnProcessExit(object sender, EventArgs e)
        {
            var unloading = Unloading;
            if (unloading != null)
            {
                unloading(this);
            }
        }

        public event Func<AssemblyLoadContext, AssemblyName, Assembly> Resolving;
        public event Action<AssemblyLoadContext> Unloading;

        // Contains the reference to VM's representation of the AssemblyLoadContext
        private IntPtr m_pNativeAssemblyLoadContext;
        
        // Each AppDomain contains the reference to its AssemblyLoadContext instance, if one is
        // specified by the host. By having the field as a static, we are
        // making it an AppDomain-wide field.
        private static volatile AssemblyLoadContext s_DefaultAssemblyLoadContext;

        // Synchronization primitive for controlling initialization of Default load context
        private static readonly object s_initLock = new Object();
    }

    [System.Security.SecuritySafeCritical]
    class AppPathAssemblyLoadContext : AssemblyLoadContext
    {
        internal AppPathAssemblyLoadContext() : base(true)
        {
        }

        [System.Security.SecuritySafeCritical]  
        protected override Assembly Load(AssemblyName assemblyName)
        {
            // We were loading an assembly into TPA ALC that was not found on TPA list. As a result we are here.
            // Returning null will result in the AssemblyResolve event subscribers to be invoked to help resolve the assembly.
            return null;
        }
    }
}

#endif // FEATURE_HOST_ASSEMBLY_RESOLVER
