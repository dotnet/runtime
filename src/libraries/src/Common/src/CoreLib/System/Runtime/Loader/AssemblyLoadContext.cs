// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Runtime.Loader
{
    public abstract partial class AssemblyLoadContext
    {
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

        private static readonly Dictionary<long, WeakReference<AssemblyLoadContext>> s_contextsToUnload = new Dictionary<long, WeakReference<AssemblyLoadContext>>();
        private static long s_nextId;
        private static bool s_isProcessExiting;

        // Indicates the state of this ALC (Alive or in Unloading state)
        private InternalState _state;

        // Id used by s_contextsToUnload
        private readonly long _id;

        // synchronization primitive to protect against usage of this instance while unloading
        private readonly object _unloadLock;

        // Contains the reference to VM's representation of the AssemblyLoadContext
        private readonly IntPtr _nativeAssemblyLoadContext;

        protected AssemblyLoadContext() : this(false, false)
        {
        }

        protected AssemblyLoadContext(bool isCollectible) : this(false, isCollectible)
        {
        }

        private protected AssemblyLoadContext(bool representsTPALoadContext, bool isCollectible)
        {
            // Initialize the VM side of AssemblyLoadContext if not already done.
            IsCollectible = isCollectible;
            // The _unloadLock needs to be assigned after the IsCollectible to ensure proper behavior of the finalizer
            // even in case the following allocation fails or the thread is aborted between these two lines.
            _unloadLock = new object();

            if (!isCollectible)
            {
                // For non collectible AssemblyLoadContext, the finalizer should never be called and thus the AssemblyLoadContext should not
                // be on the finalizer queue.
                GC.SuppressFinalize(this);
            }

            // Add this instance to the list of alive ALC
            lock (s_contextsToUnload)
            {
                if (s_isProcessExiting)
                {
                    throw new InvalidOperationException(SR.AssemblyLoadContext_Constructor_CannotInstantiateWhileUnloading);
                }

                // If this is a collectible ALC, we are creating a weak handle tracking resurrection otherwise we use a strong handle
                var thisHandle = GCHandle.Alloc(this, IsCollectible ? GCHandleType.WeakTrackResurrection : GCHandleType.Normal);
                var thisHandlePtr = GCHandle.ToIntPtr(thisHandle);
                _nativeAssemblyLoadContext = InitializeAssemblyLoadContext(thisHandlePtr, representsTPALoadContext, isCollectible);

                _id = s_nextId++;
                s_contextsToUnload.Add(_id, new WeakReference<AssemblyLoadContext>(this, true));
            }
        }

        ~AssemblyLoadContext()
        {
            // Use the _unloadLock as a guard to detect the corner case when the constructor of the AssemblyLoadContext was not executed
            // e.g. due to the JIT failing to JIT it.
            if (_unloadLock != null)
            {
                // Only valid for a Collectible ALC. Non-collectible ALCs have the finalizer suppressed.
                Debug.Assert(IsCollectible);
                // We get here only in case the explicit Unload was not initiated.
                Debug.Assert(_state != InternalState.Unloading);
                InitiateUnload();
            }
        }

        private void InitiateUnload()
        {
            var unloading = Unloading;
            Unloading = null;
            unloading?.Invoke(this);

            // When in Unloading state, we are not supposed to be called on the finalizer
            // as the native side is holding a strong reference after calling Unload
            lock (_unloadLock)
            {
                if (!s_isProcessExiting)
                {
                    Debug.Assert(_state == InternalState.Alive);

                    var thisStrongHandle = GCHandle.Alloc(this, GCHandleType.Normal);
                    var thisStrongHandlePtr = GCHandle.ToIntPtr(thisStrongHandle);
                    // The underlying code will transform the original weak handle
                    // created by InitializeLoadContext to a strong handle
                    PrepareForAssemblyLoadContextRelease(_nativeAssemblyLoadContext, thisStrongHandlePtr);
                }

                _state = InternalState.Unloading;
            }

            if (!s_isProcessExiting)
            {
                lock (s_contextsToUnload)
                {
                    s_contextsToUnload.Remove(_id);
                }
            }
        }

        // Event handler for resolving native libraries.
        // This event is raised if the native library could not be resolved via
        // the default resolution logic [including AssemblyLoadContext.LoadUnmanagedDll()]
        // 
        // Inputs: Invoking assembly, and library name to resolve
        // Returns: A handle to the loaded native library
        public event Func<Assembly, string, IntPtr> ResolvingUnmanagedDll;

        // Event handler for resolving managed assemblies.
        // This event is raised if the managed assembly could not be resolved via
        // the default resolution logic [including AssemblyLoadContext.Load()]
        // 
        // Inputs: The AssemblyLoadContext and AssemblyName to be loaded
        // Returns: The Loaded assembly object.
        public event Func<AssemblyLoadContext, AssemblyName, Assembly> Resolving;

        public event Action<AssemblyLoadContext> Unloading;

        // Occurs when an Assembly is loaded
        public static event AssemblyLoadEventHandler AssemblyLoad;

        // Occurs when resolution of type fails
        public static event ResolveEventHandler TypeResolve;

        // Occurs when resolution of resource fails
        public static event ResolveEventHandler ResourceResolve;

        // Occurs when resolution of assembly fails
        // This event is fired after resolve events of AssemblyLoadContext fails
        public static event ResolveEventHandler AssemblyResolve;

        public static AssemblyLoadContext Default => DefaultAssemblyLoadContext.s_loadContext;

        public bool IsCollectible { get; }

        // Helper to return AssemblyName corresponding to the path of an IL assembly
        public static AssemblyName GetAssemblyName(string assemblyPath)
        {
            if (assemblyPath == null)
            {
                throw new ArgumentNullException(nameof(assemblyPath));
            }

            return AssemblyName.GetAssemblyName(assemblyPath);
        }

        // Custom AssemblyLoadContext implementations can override this
        // method to perform custom processing and use one of the protected
        // helpers above to load the assembly.
        protected abstract Assembly Load(AssemblyName assemblyName);

        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        public Assembly LoadFromAssemblyName(AssemblyName assemblyName)
        {
            if (assemblyName == null)
                throw new ArgumentNullException(nameof(assemblyName));

            // Attempt to load the assembly, using the same ordering as static load, in the current load context.
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return Assembly.Load(assemblyName, ref stackMark, _nativeAssemblyLoadContext);
        }

        // These methods load assemblies into the current AssemblyLoadContext 
        // They may be used in the implementation of an AssemblyLoadContext derivation
        public Assembly LoadFromAssemblyPath(string assemblyPath)
        {
            if (assemblyPath == null)
            {
                throw new ArgumentNullException(nameof(assemblyPath));
            }

            if (PathInternal.IsPartiallyQualified(assemblyPath))
            {
                throw new ArgumentException(SR.Argument_AbsolutePathRequired, nameof(assemblyPath));
            }

            lock (_unloadLock)
            {
                VerifyIsAlive();

                return InternalLoadFromPath(assemblyPath, null);
            }
        }

        public Assembly LoadFromNativeImagePath(string nativeImagePath, string assemblyPath)
        {
            if (nativeImagePath == null)
            {
                throw new ArgumentNullException(nameof(nativeImagePath));
            }

            if (PathInternal.IsPartiallyQualified(nativeImagePath))
            {
                throw new ArgumentException(SR.Argument_AbsolutePathRequired, nameof(nativeImagePath));
            }

            if (assemblyPath != null && PathInternal.IsPartiallyQualified(assemblyPath))
            {
                throw new ArgumentException(SR.Argument_AbsolutePathRequired, nameof(assemblyPath));
            }

            lock (_unloadLock)
            {
                VerifyIsAlive();

                return InternalLoadFromPath(assemblyPath, nativeImagePath);
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

            int iAssemblyStreamLength = (int)assembly.Length;

            if (iAssemblyStreamLength <= 0)
            {
                throw new BadImageFormatException(SR.BadImageFormat_BadILFormat);
            }

            // Allocate the byte[] to hold the assembly
            byte[] arrAssembly = new byte[iAssemblyStreamLength];

            // Copy the assembly to the byte array
            assembly.Read(arrAssembly, 0, iAssemblyStreamLength);

            // Get the symbol stream in byte[] if provided
            byte[] arrSymbols = null;
            if (assemblySymbols != null)
            {
                var iSymbolLength = (int)assemblySymbols.Length;
                arrSymbols = new byte[iSymbolLength];

                assemblySymbols.Read(arrSymbols, 0, iSymbolLength);
            }

            lock (_unloadLock)
            {
                VerifyIsAlive();

                return InternalLoad(arrAssembly, arrSymbols);
            }
        }

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

        public void Unload()
        {
            if (!IsCollectible)
            {
                throw new InvalidOperationException(SR.AssemblyLoadContext_Unload_CannotUnloadIfNotCollectible);
            }

            GC.SuppressFinalize(this);
            InitiateUnload();
        }

        internal static void OnProcessExit()
        {
            lock (s_contextsToUnload)
            {
                s_isProcessExiting = true;
                foreach (var alcAlive in s_contextsToUnload)
                {
                    if (alcAlive.Value.TryGetTarget(out AssemblyLoadContext alc))
                    {
                        // Should we use a try/catch?
                        alc.InitiateUnload();
                    }
                }
                s_contextsToUnload.Clear();
            }
        }        

        private void VerifyIsAlive()
        {
            if (_state != InternalState.Alive)
            {
                throw new InvalidOperationException(SR.AssemblyLoadContext_Verify_NotUnloading);
            }
        }
    }

    internal sealed class DefaultAssemblyLoadContext : AssemblyLoadContext
    {
        internal static readonly AssemblyLoadContext s_loadContext = new DefaultAssemblyLoadContext();

        internal DefaultAssemblyLoadContext() : base(true, false)
        {
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            // We were loading an assembly into TPA ALC that was not found on TPA list. As a result we are here.
            // Returning null will result in the AssemblyResolve event subscribers to be invoked to help resolve the assembly.
            return null;
        }
    }

    internal sealed class IndividualAssemblyLoadContext : AssemblyLoadContext
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