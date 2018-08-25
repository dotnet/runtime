// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using System.Runtime.Versioning;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;


namespace System.Runtime.Loader
{
    public abstract class AssemblyLoadContext
    {
        private static readonly Dictionary<long, WeakReference<AssemblyLoadContext>> ContextsToUnload = new Dictionary<long, WeakReference<AssemblyLoadContext>>();
        private static long _nextId;
        private static bool _isProcessExiting;

        // Id used by contextsToUnload
        private readonly long id;

        // synchronization primitive to protect against usage of this instance while unloading
        private readonly object unloadLock = new object();

        // Indicates the state of this ALC (Alive or in Unloading state)
        private InternalState state;

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern bool CanUseAppPathAssemblyLoadContextInCurrentDomain();

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern IntPtr InitializeAssemblyLoadContext(IntPtr ptrAssemblyLoadContext, bool fRepresentsTPALoadContext, bool isCollectible);

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void PrepareForAssemblyLoadContextRelease(IntPtr ptrNativeAssemblyLoadContext, IntPtr ptrAssemblyLoadContextStrong);

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadFromStream(IntPtr ptrNativeAssemblyLoadContext, IntPtr ptrAssemblyArray, int iAssemblyArrayLen, IntPtr ptrSymbols, int iSymbolArrayLen, ObjectHandleOnStack retAssembly);

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        internal static extern void InternalSetProfileRoot(string directoryPath);

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        internal static extern void InternalStartProfile(string profile, IntPtr ptrNativeAssemblyLoadContext);

        static AssemblyLoadContext()
        {
            // We register the cleanup of all AssemblyLoadContext that have not been finalized in the AppContext.Unloading
            AppContext.Unloading += OnAppContextUnloading;
        }

        protected AssemblyLoadContext() : this(false, false)
        {
        }

        protected AssemblyLoadContext(bool isCollectible) : this(false, isCollectible)
        {
        }

        internal AssemblyLoadContext(bool fRepresentsTPALoadContext, bool isCollectible)
        {
            // Initialize the VM side of AssemblyLoadContext if not already done.
            IsCollectible = isCollectible;

            // Add this instance to the list of alive ALC
            lock (ContextsToUnload)
            {
                if (_isProcessExiting)
                {
                    throw new InvalidOperationException(SR.GetResourceString("AssemblyLoadContext_Constructor_CannotInstantiateWhileUnloading"));
                }

                // If this is a collectible ALC, we are creating a weak handle otherwise we use a strong handle
                var thisHandle = GCHandle.Alloc(this, IsCollectible ? GCHandleType.Weak : GCHandleType.Normal);
                var thisHandlePtr = GCHandle.ToIntPtr(thisHandle);
                m_pNativeAssemblyLoadContext = InitializeAssemblyLoadContext(thisHandlePtr, fRepresentsTPALoadContext, isCollectible);

                // Initialize event handlers to be null by default
                Resolving = null;
                Unloading = null;

                id = _nextId++;
                ContextsToUnload.Add(id, new WeakReference<AssemblyLoadContext>(this, true));
            }
        }

        ~AssemblyLoadContext()
        {
            // Only valid for a Collectible ALC. Non-collectible ALCs have the finalizer suppressed.
            // We get here only in case the explicit Unload was not initiated.
            Debug.Assert(state != InternalState.Unloading);
            InitiateUnload();
        }

        private void InitiateUnload()
        {
            var unloading = Unloading;
            Unloading = null;
            unloading?.Invoke(this);

            // When in Unloading state, we are not supposed to be called on the finalizer
            // as the native side is holding a strong reference after calling Unload
            lock (unloadLock)
            {
                if (!_isProcessExiting)
                {
                    Debug.Assert(state == InternalState.Alive);

                    var thisStrongHandle = GCHandle.Alloc(this, GCHandleType.Normal);
                    var thisStrongHandlePtr = GCHandle.ToIntPtr(thisStrongHandle);
                    // The underlying code will transform the original weak handle
                    // created by InitializeLoadContext to a strong handle
                    PrepareForAssemblyLoadContextRelease(m_pNativeAssemblyLoadContext, thisStrongHandlePtr);
                }

                state = InternalState.Unloading;
            }

            if (!_isProcessExiting)
            {
                lock (ContextsToUnload)
                {
                    ContextsToUnload.Remove(id);
                }
            }
        }

        public bool IsCollectible { get; }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void LoadFromPath(IntPtr ptrNativeAssemblyLoadContet, string ilPath, string niPath, ObjectHandleOnStack retAssembly);

        public static Assembly[] GetLoadedAssemblies()
        {
            return AppDomain.CurrentDomain.GetAssemblies(false);
        }

        // These methods load assemblies into the current AssemblyLoadContext 
        // They may be used in the implementation of an AssemblyLoadContext derivation
        public Assembly LoadFromAssemblyPath(string assemblyPath)
        {
            if (assemblyPath == null)
            {
                throw new ArgumentNullException(nameof(assemblyPath));
            }

            lock (unloadLock)
            {
                VerifyIsAlive();
                if (PathInternal.IsPartiallyQualified(assemblyPath))
                {
                    throw new ArgumentException(SR.GetResourceString("Argument_AbsolutePathRequired"),
                        nameof(assemblyPath));
                }

                RuntimeAssembly loadedAssembly = null;
                LoadFromPath(m_pNativeAssemblyLoadContext, assemblyPath, null, JitHelpers.GetObjectHandleOnStack(ref loadedAssembly));
                return loadedAssembly;
            }
        }

        public Assembly LoadFromNativeImagePath(string nativeImagePath, string assemblyPath)
        {
            if (nativeImagePath == null)
            {
                throw new ArgumentNullException(nameof(nativeImagePath));
            }

            lock (unloadLock)
            {
                VerifyIsAlive();

                if (PathInternal.IsPartiallyQualified(nativeImagePath))
                {
                    throw new ArgumentException(SR.GetResourceString("Argument_AbsolutePathRequired"),
                        nameof(nativeImagePath));
                }

                if (assemblyPath != null && PathInternal.IsPartiallyQualified(assemblyPath))
                {
                    throw new ArgumentException(SR.GetResourceString("Argument_AbsolutePathRequired"),
                        nameof(assemblyPath));
                }

                // Basic validation has succeeded - lets try to load the NI image.
                RuntimeAssembly loadedAssembly = null;
                LoadFromPath(m_pNativeAssemblyLoadContext, assemblyPath, nativeImagePath, JitHelpers.GetObjectHandleOnStack(ref loadedAssembly));
                return loadedAssembly;
            }
        }

        public Assembly LoadFromStream(Stream assembly)
        {
            return LoadFromStream(assembly, null);
        }

        public Assembly LoadFromStream(Stream assembly, Stream assemblySymbols)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            if (assembly.Length <= 0)
            {
                throw new BadImageFormatException(SR.BadImageFormat_BadILFormat);
            }
            lock (unloadLock)
            {
                VerifyIsAlive();

                int iAssemblyStreamLength = (int) assembly.Length;
                int iSymbolLength = 0;

                // Allocate the byte[] to hold the assembly
                byte[] arrAssembly = new byte[iAssemblyStreamLength];

                // Copy the assembly to the byte array
                assembly.Read(arrAssembly, 0, iAssemblyStreamLength);

                // Get the symbol stream in byte[] if provided
                byte[] arrSymbols = null;
                if (assemblySymbols != null)
                {
                    iSymbolLength = (int) assemblySymbols.Length;
                    arrSymbols = new byte[iSymbolLength];

                    assemblySymbols.Read(arrSymbols, 0, iSymbolLength);
                }

                RuntimeAssembly loadedAssembly = null;
                unsafe
                {
                    fixed (byte* ptrAssembly = arrAssembly, ptrSymbols = arrSymbols)
                    {
                        LoadFromStream(m_pNativeAssemblyLoadContext, new IntPtr(ptrAssembly), iAssemblyStreamLength,
                            new IntPtr(ptrSymbols), iSymbolLength, JitHelpers.GetObjectHandleOnStack(ref loadedAssembly));
                    }
                }
                return loadedAssembly;
            }
        }

        public void Unload()
        {
            if (!IsCollectible)
            {
                throw new InvalidOperationException(SR.GetResourceString("AssemblyLoadContext_Unload_CannotUnloadIfNotCollectible"));
            }

            GC.SuppressFinalize(this);
            InitiateUnload();
        }

        private void VerifyIsAlive()
        {
            if (state != InternalState.Alive)
            {
                throw new InvalidOperationException(SR.GetResourceString("AssemblyLoadContext_Verify_NotUnloading"));
            }
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
                foreach (Func<AssemblyLoadContext, AssemblyName, Assembly> handler in assemblyResolveHandler.GetInvocationList())
                {
                    resolvedAssembly = handler(this, assemblyName);
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
            if (string.IsNullOrEmpty(loadedSimpleName) || (!requestedSimpleName.Equals(loadedSimpleName, StringComparison.InvariantCultureIgnoreCase)))
                throw new InvalidOperationException(SR.Argument_CustomAssemblyLoadContextRequestedNameMismatch);

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
                throw new FileNotFoundException(SR.IO_FileLoad, simpleName);
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
        private static extern IntPtr InternalLoadUnmanagedDllFromPath(string unmanagedDllPath);

        // This method provides a way for overriders of LoadUnmanagedDll() to load an unmanaged DLL from a specific path in a
        // platform-independent way. The DLL is loaded with default load flags.
        protected IntPtr LoadUnmanagedDllFromPath(string unmanagedDllPath)
        {
            if (unmanagedDllPath == null)
            {
                throw new ArgumentNullException(nameof(unmanagedDllPath));
            }
            if (unmanagedDllPath.Length == 0)
            {
                throw new ArgumentException(SR.Argument_EmptyPath, nameof(unmanagedDllPath));
            }
            if (PathInternal.IsPartiallyQualified(unmanagedDllPath))
            {
                throw new ArgumentException(SR.Argument_AbsolutePathRequired, nameof(unmanagedDllPath));
            }

            return InternalLoadUnmanagedDllFromPath(unmanagedDllPath);
        }

        // Custom AssemblyLoadContext implementations can override this
        // method to perform the load of unmanaged native dll
        // This function needs to return the HMODULE of the dll it loads
        protected virtual IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            //defer to default coreclr policy of loading unmanaged dll
            return IntPtr.Zero;
        }

        // This method is invoked by the VM when using the host-provided assembly load context
        // implementation.
        private static IntPtr ResolveUnmanagedDll(string unmanagedDllName, IntPtr gchManagedAssemblyLoadContext)
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
                        lock (s_initLock)
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

        // Helper to return AssemblyName corresponding to the path of an IL assembly
        public static AssemblyName GetAssemblyName(string assemblyPath)
        {
            if (assemblyPath == null)
            {
                throw new ArgumentNullException(nameof(assemblyPath));
            }

            return AssemblyName.GetAssemblyName(assemblyPath);
        }
        
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern IntPtr GetLoadContextForAssembly(RuntimeAssembly assembly);

        // Returns the load context in which the specified assembly has been loaded
        public static AssemblyLoadContext GetLoadContext(Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
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
            InternalSetProfileRoot(directoryPath);
        }

        // Start profile optimization for the specified profile name.
        public void StartProfileOptimization(string profile)
        {
            InternalStartProfile(profile, m_pNativeAssemblyLoadContext);
        }

        private void OnAppContextUnloading()
        {
            InitiateUnload();
        }

        private static void OnAppContextUnloading(object sender, EventArgs e)
        {
            lock (ContextsToUnload)
            {
                _isProcessExiting = true;
                foreach (var alcAlive in ContextsToUnload)
                {
                    AssemblyLoadContext alc;
                    if (alcAlive.Value.TryGetTarget(out alc))
                    {
                        // Should we use a try/catch?
                        alc.OnAppContextUnloading();
                    }
                }
                ContextsToUnload.Clear();
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
        private static readonly object s_initLock = new object();

        // Occurs when an Assembly is loaded
        public static event AssemblyLoadEventHandler AssemblyLoad
        {
            add { AppDomain.CurrentDomain.AssemblyLoad += value; }
            remove { AppDomain.CurrentDomain.AssemblyLoad -= value; }
        }

        // Occurs when resolution of type fails
        public static event ResolveEventHandler TypeResolve
        {
            add { AppDomain.CurrentDomain.TypeResolve += value; }
            remove { AppDomain.CurrentDomain.TypeResolve -= value; }
        }

        // Occurs when resolution of resource fails
        public static event ResolveEventHandler ResourceResolve
        {
            add { AppDomain.CurrentDomain.ResourceResolve += value; }
            remove { AppDomain.CurrentDomain.ResourceResolve -= value; }
        }

        // Occurs when resolution of assembly fails
        // This event is fired after resolve events of AssemblyLoadContext fails
        public static event ResolveEventHandler AssemblyResolve
        {
            add { AppDomain.CurrentDomain.AssemblyResolve += value; }
            remove { AppDomain.CurrentDomain.AssemblyResolve -= value; }
        }

        private enum InternalState
        {
            /// <summary>
            /// The ALC is alive (default)
            /// </summary>
            Alive,

            /// <summary>
            /// The unload process has started, the Unloading event will be called
            /// once the underlying LoaderAllocator has been finalized
            /// </summary>
            Unloading
        }
    }

    internal class AppPathAssemblyLoadContext : AssemblyLoadContext
    {
        internal AppPathAssemblyLoadContext() : base(true, false)
        {
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            // We were loading an assembly into TPA ALC that was not found on TPA list. As a result we are here.
            // Returning null will result in the AssemblyResolve event subscribers to be invoked to help resolve the assembly.
            return null;
        }
    }

    internal class IndividualAssemblyLoadContext : AssemblyLoadContext
    {
        internal IndividualAssemblyLoadContext() : base(false, false)
        {
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            return null;
        }
    }

}

