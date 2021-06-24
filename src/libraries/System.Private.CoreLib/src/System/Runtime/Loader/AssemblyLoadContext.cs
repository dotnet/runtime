// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Runtime.Loader
{
    public partial class AssemblyLoadContext
    {
        // Keep in sync with MonoManagedAssemblyLoadContextInternalState in object-internals.h
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

        private static volatile Dictionary<long, WeakReference<AssemblyLoadContext>>? s_allContexts;
        private static long s_nextId;

        [MemberNotNull(nameof(s_allContexts))]
        private static Dictionary<long, WeakReference<AssemblyLoadContext>> AllContexts =>
            s_allContexts ??
            Interlocked.CompareExchange(ref s_allContexts, new Dictionary<long, WeakReference<AssemblyLoadContext>>(), null) ??
            s_allContexts;

#region private data members
        // If you modify this field, you must also update the
        // AssemblyLoadContextBaseObject structure in object.h
        // and MonoManagedAssemblyLoadContext in object-internals.h

        // Contains the reference to VM's representation of the AssemblyLoadContext
        private readonly IntPtr _nativeAssemblyLoadContext;
#endregion

        // synchronization primitive to protect against usage of this instance while unloading
        private readonly object _unloadLock;

        private event Func<Assembly, string, IntPtr>? _resolvingUnmanagedDll;

        private event Func<AssemblyLoadContext, AssemblyName, Assembly>? _resolving;

        private event Action<AssemblyLoadContext>? _unloading;

        private readonly string? _name;

        // Id used by s_allContexts
        private readonly long _id;

        // Indicates the state of this ALC (Alive or in Unloading state)
        private InternalState _state;

        private readonly bool _isCollectible;

        protected AssemblyLoadContext() : this(false, false, null)
        {
        }

        protected AssemblyLoadContext(bool isCollectible) : this(false, isCollectible, null)
        {
        }

        public AssemblyLoadContext(string? name, bool isCollectible = false) : this(false, isCollectible, name)
        {
        }

        private protected AssemblyLoadContext(bool representsTPALoadContext, bool isCollectible, string? name)
        {
            // Initialize the VM side of AssemblyLoadContext if not already done.
            _isCollectible = isCollectible;

            _name = name;

            // The _unloadLock needs to be assigned after the IsCollectible to ensure proper behavior of the finalizer
            // even in case the following allocation fails or the thread is aborted between these two lines.
            _unloadLock = new object();

            if (!isCollectible)
            {
                // For non collectible AssemblyLoadContext, the finalizer should never be called and thus the AssemblyLoadContext should not
                // be on the finalizer queue.
                GC.SuppressFinalize(this);
            }

            // If this is a collectible ALC, we are creating a weak handle tracking resurrection otherwise we use a strong handle
            var thisHandle = GCHandle.Alloc(this, IsCollectible ? GCHandleType.WeakTrackResurrection : GCHandleType.Normal);
            var thisHandlePtr = GCHandle.ToIntPtr(thisHandle);
            _nativeAssemblyLoadContext = InitializeAssemblyLoadContext(thisHandlePtr, representsTPALoadContext, isCollectible);

            // Add this instance to the list of alive ALC
            Dictionary<long, WeakReference<AssemblyLoadContext>> allContexts = AllContexts;
            lock (allContexts)
            {
                _id = s_nextId++;
                allContexts.Add(_id, new WeakReference<AssemblyLoadContext>(this, true));
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

        private void RaiseUnloadEvent()
        {
            // Ensure that we raise the Unload event only once
            Interlocked.Exchange(ref _unloading, null!)?.Invoke(this);
        }

        private void InitiateUnload()
        {
            RaiseUnloadEvent();

            // When in Unloading state, we are not supposed to be called on the finalizer
            // as the native side is holding a strong reference after calling Unload
            lock (_unloadLock)
            {
                Debug.Assert(_state == InternalState.Alive);

                var thisStrongHandle = GCHandle.Alloc(this, GCHandleType.Normal);
                var thisStrongHandlePtr = GCHandle.ToIntPtr(thisStrongHandle);
                // The underlying code will transform the original weak handle
                // created by InitializeLoadContext to a strong handle
                PrepareForAssemblyLoadContextRelease(_nativeAssemblyLoadContext, thisStrongHandlePtr);

                _state = InternalState.Unloading;
            }

            Dictionary<long, WeakReference<AssemblyLoadContext>> allContexts = AllContexts;
            lock (allContexts)
            {
                allContexts.Remove(_id);
            }
        }

        public IEnumerable<Assembly> Assemblies
        {
            get
            {
                foreach (Assembly a in GetLoadedAssemblies())
                {
                    AssemblyLoadContext? alc = GetLoadContext(a);

                    if (alc == this)
                    {
                        yield return a;
                    }
                }
            }
        }

        // Event handler for resolving native libraries.
        // This event is raised if the native library could not be resolved via
        // the default resolution logic [including AssemblyLoadContext.LoadUnmanagedDll()]
        //
        // Inputs: Invoking assembly, and library name to resolve
        // Returns: A handle to the loaded native library
        public event Func<Assembly, string, IntPtr>? ResolvingUnmanagedDll
        {
#if MONO
            [DynamicDependency(nameof(MonoResolveUnmanagedDllUsingEvent))]
#endif
            add
            {
                _resolvingUnmanagedDll += value;
            }
            remove
            {
                _resolvingUnmanagedDll -= value;
            }
        }

        // Event handler for resolving managed assemblies.
        // This event is raised if the managed assembly could not be resolved via
        // the default resolution logic [including AssemblyLoadContext.Load()]
        //
        // Inputs: The AssemblyLoadContext and AssemblyName to be loaded
        // Returns: The Loaded assembly object.
        public event Func<AssemblyLoadContext, AssemblyName, Assembly?>? Resolving
        {
#if MONO
            [DynamicDependency(nameof(MonoResolveUsingResolvingEvent))]
#endif
            add
            {
                _resolving += value;
            }
            remove
            {
                _resolving -= value;
            }
        }

        public event Action<AssemblyLoadContext>? Unloading
        {
            add
            {
                _unloading += value;
            }
            remove
            {
                _unloading -= value;
            }
        }

#region AppDomainEvents
        // Occurs when an Assembly is loaded
#if MONO
        [method: DynamicDependency(nameof(OnAssemblyLoad))]
#endif
        internal static event AssemblyLoadEventHandler? AssemblyLoad;

        // Occurs when resolution of type fails
#if MONO
        [method: DynamicDependency(nameof(OnTypeResolve))]
#endif
        internal static event ResolveEventHandler? TypeResolve;

        // Occurs when resolution of resource fails
#if MONO
        [method: DynamicDependency(nameof(OnResourceResolve))]
#endif
        internal static event ResolveEventHandler? ResourceResolve;

        // Occurs when resolution of assembly fails
        // This event is fired after resolve events of AssemblyLoadContext fails
#if MONO
        [method: DynamicDependency(nameof(OnAssemblyResolve))]
#endif
        internal static event ResolveEventHandler? AssemblyResolve;
#endregion

        public static AssemblyLoadContext Default => DefaultAssemblyLoadContext.s_loadContext;

        public bool IsCollectible => _isCollectible;

        public string? Name => _name;

        public override string ToString() => "\"" + Name + "\" " + GetType().ToString() + " #" + _id;

        public static IEnumerable<AssemblyLoadContext> All
        {
            get
            {
                _ = Default; // Ensure default is initialized

                Dictionary<long, WeakReference<AssemblyLoadContext>>? allContexts = s_allContexts;
                Debug.Assert(allContexts != null, "Creating the default context should have initialized the contexts collection.");

                WeakReference<AssemblyLoadContext>[] alcSnapshot;
                lock (allContexts)
                {
                    // To make this thread safe we need a quick snapshot while locked
                    alcSnapshot = new WeakReference<AssemblyLoadContext>[allContexts.Count];
                    int pos = 0;
                    foreach (KeyValuePair<long, WeakReference<AssemblyLoadContext>> item in allContexts)
                    {
                        alcSnapshot[pos++] = item.Value;
                    }
                }

                foreach (WeakReference<AssemblyLoadContext> weakAlc in alcSnapshot)
                {
                    if (weakAlc.TryGetTarget(out AssemblyLoadContext? alc))
                    {
                        yield return alc;
                    }
                }
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

        // Custom AssemblyLoadContext implementations can override this
        // method to perform custom processing and use one of the protected
        // helpers above to load the assembly.
        protected virtual Assembly? Load(AssemblyName assemblyName)
        {
            return null;
        }

#if !CORERT
        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        public Assembly LoadFromAssemblyName(AssemblyName assemblyName)
        {
            if (assemblyName == null)
                throw new ArgumentNullException(nameof(assemblyName));

            // Attempt to load the assembly, using the same ordering as static load, in the current load context.
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeAssembly.InternalLoad(assemblyName, ref stackMark, this);
        }
#endif

        // These methods load assemblies into the current AssemblyLoadContext
        // They may be used in the implementation of an AssemblyLoadContext derivation
        [RequiresUnreferencedCode("Types and members the loaded assembly depends on might be removed")]
        public Assembly LoadFromAssemblyPath(string assemblyPath)
        {
            if (assemblyPath == null)
            {
                throw new ArgumentNullException(nameof(assemblyPath));
            }

            if (PathInternal.IsPartiallyQualified(assemblyPath))
            {
                throw new ArgumentException(SR.Format(SR.Argument_AbsolutePathRequired, assemblyPath), nameof(assemblyPath));
            }

            lock (_unloadLock)
            {
                VerifyIsAlive();

                return InternalLoadFromPath(assemblyPath, null);
            }
        }

        [RequiresUnreferencedCode("Types and members the loaded assembly depends on might be removed")]
        public Assembly LoadFromNativeImagePath(string nativeImagePath, string? assemblyPath)
        {
            if (nativeImagePath == null)
            {
                throw new ArgumentNullException(nameof(nativeImagePath));
            }

            if (PathInternal.IsPartiallyQualified(nativeImagePath))
            {
                throw new ArgumentException(SR.Format(SR.Argument_AbsolutePathRequired, nativeImagePath), nameof(nativeImagePath));
            }

            if (assemblyPath != null && PathInternal.IsPartiallyQualified(assemblyPath))
            {
                throw new ArgumentException(SR.Format(SR.Argument_AbsolutePathRequired, assemblyPath), nameof(assemblyPath));
            }

            lock (_unloadLock)
            {
                VerifyIsAlive();

                return InternalLoadFromPath(assemblyPath, nativeImagePath);
            }
        }

        [RequiresUnreferencedCode("Types and members the loaded assembly depends on might be removed")]
        public Assembly LoadFromStream(Stream assembly)
        {
            return LoadFromStream(assembly, null);
        }

        [RequiresUnreferencedCode("Types and members the loaded assembly depends on might be removed")]
        public Assembly LoadFromStream(Stream assembly, Stream? assemblySymbols)
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
            byte[]? arrSymbols = null;
            if (assemblySymbols != null)
            {
                int iSymbolLength = (int)assemblySymbols.Length;
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
                throw new ArgumentException(SR.Format(SR.Argument_AbsolutePathRequired, unmanagedDllPath), nameof(unmanagedDllPath));
            }

            return NativeLibrary.Load(unmanagedDllPath);
        }

        // Custom AssemblyLoadContext implementations can override this
        // method to perform the load of unmanaged native dll
        // This function needs to return the HMODULE of the dll it loads
        protected virtual IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            // defer to default coreclr policy of loading unmanaged dll
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
            Dictionary<long, WeakReference<AssemblyLoadContext>>? allContexts = s_allContexts;
            if (allContexts is null)
            {
                // If s_allContexts was never initialized, there are no contexts for which to raise an unload event.
                return;
            }

            lock (allContexts)
            {
                foreach (KeyValuePair<long, WeakReference<AssemblyLoadContext>> alcAlive in allContexts)
                {
                    if (alcAlive.Value.TryGetTarget(out AssemblyLoadContext? alc))
                    {
                        alc.RaiseUnloadEvent();
                    }
                }
            }
        }

        private void VerifyIsAlive()
        {
            if (_state != InternalState.Alive)
            {
                throw new InvalidOperationException(SR.AssemblyLoadContext_Verify_NotUnloading);
            }
        }

        private static AsyncLocal<AssemblyLoadContext?>? s_asyncLocalCurrent;

        /// <summary>Nullable current AssemblyLoadContext used for context sensitive reflection APIs</summary>
        /// <remarks>
        /// This is an advanced setting used in reflection assembly loading scenarios.
        ///
        /// There are a set of contextual reflection APIs which load managed assemblies through an inferred AssemblyLoadContext.
        /// * <see cref="System.Activator.CreateInstance" />
        /// * <see cref="System.Reflection.Assembly.Load" />
        /// * <see cref="System.Reflection.Assembly.GetType" />
        /// * <see cref="System.Type.GetType" />
        ///
        /// When CurrentContextualReflectionContext is null, the AssemblyLoadContext is inferred.
        /// The inference logic is simple.
        /// * For static methods, it is the AssemblyLoadContext which loaded the method caller's assembly.
        /// * For instance methods, it is the AssemblyLoadContext which loaded the instance's assembly.
        ///
        /// When this property is set, the CurrentContextualReflectionContext value is used by these contextual reflection APIs for loading.
        ///
        /// This property is typically set in a using block by
        /// <see cref="System.Runtime.Loader.AssemblyLoadContext.EnterContextualReflection"/>.
        ///
        /// The property is stored in an AsyncLocal&lt;AssemblyLoadContext&gt;. This means the setting can be unique for every async or thread in the process.
        ///
        /// For more details see https://github.com/dotnet/runtime/blob/main/docs/design/features/AssemblyLoadContext.ContextualReflection.md
        /// </remarks>
        public static AssemblyLoadContext? CurrentContextualReflectionContext => s_asyncLocalCurrent?.Value;

        private static void SetCurrentContextualReflectionContext(AssemblyLoadContext? value)
        {
            if (s_asyncLocalCurrent == null)
            {
                Interlocked.CompareExchange<AsyncLocal<AssemblyLoadContext?>?>(ref s_asyncLocalCurrent, new AsyncLocal<AssemblyLoadContext?>(), null);
            }
            s_asyncLocalCurrent!.Value = value; // Remove ! when compiler specially-recognizes CompareExchange for nullability
        }

        /// <summary>Enter scope using this AssemblyLoadContext for ContextualReflection</summary>
        /// <returns>A disposable ContextualReflectionScope for use in a using block</returns>
        /// <remarks>
        /// Sets CurrentContextualReflectionContext to this instance.
        /// <see cref="System.Runtime.Loader.AssemblyLoadContext.CurrentContextualReflectionContext"/>
        ///
        /// Returns a disposable ContextualReflectionScope for use in a using block. When the using calls the
        /// Dispose() method, it restores the ContextualReflectionScope to its previous value.
        /// </remarks>
        public ContextualReflectionScope EnterContextualReflection()
        {
            return new ContextualReflectionScope(this);
        }

        /// <summary>Enter scope using this AssemblyLoadContext for ContextualReflection</summary>
        /// <param name="activating">Set CurrentContextualReflectionContext to the AssemblyLoadContext which loaded activating.</param>
        /// <returns>A disposable ContextualReflectionScope for use in a using block</returns>
        /// <remarks>
        /// Sets CurrentContextualReflectionContext to to the AssemblyLoadContext which loaded activating.
        /// <see cref="System.Runtime.Loader.AssemblyLoadContext.CurrentContextualReflectionContext"/>
        ///
        /// Returns a disposable ContextualReflectionScope for use in a using block. When the using calls the
        /// Dispose() method, it restores the ContextualReflectionScope to its previous value.
        /// </remarks>
        public static ContextualReflectionScope EnterContextualReflection(Assembly? activating)
        {
            if (activating == null)
                return new ContextualReflectionScope(null);

            AssemblyLoadContext? assemblyLoadContext = GetLoadContext(activating);

            if (assemblyLoadContext == null)
            {
                // All RuntimeAssemblies & Only RuntimeAssemblies have an AssemblyLoadContext
                throw new ArgumentException(SR.Arg_MustBeRuntimeAssembly, nameof(activating));
            }

            return assemblyLoadContext.EnterContextualReflection();
        }

        /// <summary>Opaque disposable struct used to restore CurrentContextualReflectionContext</summary>
        /// <remarks>
        /// This is an implmentation detail of the AssemblyLoadContext.EnterContextualReflection APIs.
        /// It is a struct, to avoid heap allocation.
        /// It is required to be public to avoid boxing.
        /// <see cref="System.Runtime.Loader.AssemblyLoadContext.EnterContextualReflection"/>
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public struct ContextualReflectionScope : IDisposable
        {
            private readonly AssemblyLoadContext? _activated;
            private readonly AssemblyLoadContext? _predecessor;
            private readonly bool _initialized;

            internal ContextualReflectionScope(AssemblyLoadContext? activating)
            {
                _predecessor = AssemblyLoadContext.CurrentContextualReflectionContext;
                AssemblyLoadContext.SetCurrentContextualReflectionContext(activating);
                _activated = activating;
                _initialized = true;
            }

            public void Dispose()
            {
                if (_initialized)
                {
                    // Do not clear initialized. Always restore the _predecessor in Dispose()
                    // _initialized = false;
                    AssemblyLoadContext.SetCurrentContextualReflectionContext(_predecessor);
                }
            }
        }

#if !CORERT
        // This method is invoked by the VM when using the host-provided assembly load context
        // implementation.
        private static Assembly? Resolve(IntPtr gchManagedAssemblyLoadContext, AssemblyName assemblyName)
        {
            AssemblyLoadContext context = (AssemblyLoadContext)(GCHandle.FromIntPtr(gchManagedAssemblyLoadContext).Target)!;

            return context.ResolveUsingLoad(assemblyName);
        }

        [UnconditionalSuppressMessage("SingleFile", "IL3000: Avoid accessing Assembly file path when publishing as a single file",
            Justification = "The code handles the Assembly.Location equals null")]
        private Assembly? GetFirstResolvedAssemblyFromResolvingEvent(AssemblyName assemblyName)
        {
            Assembly? resolvedAssembly = null;

            Func<AssemblyLoadContext, AssemblyName, Assembly>? resolvingHandler = _resolving;

            if (resolvingHandler != null)
            {
                // Loop through the event subscribers and return the first non-null Assembly instance
                foreach (Func<AssemblyLoadContext, AssemblyName, Assembly> handler in resolvingHandler.GetInvocationList())
                {
                    resolvedAssembly = handler(this, assemblyName);
#if CORECLR
                    if (AssemblyLoadContext.IsTracingEnabled())
                    {
                        AssemblyLoadContext.TraceResolvingHandlerInvoked(
                            assemblyName.FullName,
                            handler.Method.Name,
                            this != AssemblyLoadContext.Default ? ToString() : Name,
                            resolvedAssembly?.FullName,
                            resolvedAssembly != null && !resolvedAssembly.IsDynamic ? resolvedAssembly.Location : null);
                    }
#endif // CORECLR
                    if (resolvedAssembly != null)
                    {
                        return resolvedAssembly;
                    }
                }
            }

            return null;
        }

        private static Assembly ValidateAssemblyNameWithSimpleName(Assembly assembly, string? requestedSimpleName)
        {
            if (string.IsNullOrEmpty(requestedSimpleName))
            {
                throw new ArgumentException(SR.ArgumentNull_AssemblyNameName);
            }

            // Get the name of the loaded assembly
            string? loadedSimpleName = null;

            // Derived type's Load implementation is expected to use one of the LoadFrom* methods to get the assembly
            // which is a RuntimeAssembly instance. However, since Assembly type can be used build any other artifact (e.g. AssemblyBuilder),
            // we need to check for RuntimeAssembly.
            RuntimeAssembly? rtLoadedAssembly = GetRuntimeAssembly(assembly);
            if (rtLoadedAssembly != null)
            {
                loadedSimpleName = rtLoadedAssembly.GetSimpleName();
            }

            // The simple names should match at the very least
            if (string.IsNullOrEmpty(loadedSimpleName) || !requestedSimpleName.Equals(loadedSimpleName, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new InvalidOperationException(SR.Argument_CustomAssemblyLoadContextRequestedNameMismatch);
            }

            return assembly;
        }

        private Assembly? ResolveUsingLoad(AssemblyName assemblyName)
        {
            string? simpleName = assemblyName.Name;
            Assembly? assembly = Load(assemblyName);

            if (assembly != null)
            {
                assembly = ValidateAssemblyNameWithSimpleName(assembly, simpleName);
            }

            return assembly;
        }

        private Assembly? ResolveUsingEvent(AssemblyName assemblyName)
        {
            string? simpleName = assemblyName.Name;

            // Invoke the Resolving event callbacks if wired up
            Assembly? assembly = GetFirstResolvedAssemblyFromResolvingEvent(assemblyName);
            if (assembly != null)
            {
                assembly = ValidateAssemblyNameWithSimpleName(assembly, simpleName);
            }

            return assembly;
        }

        // This method is called by the VM.
        private static void OnAssemblyLoad(RuntimeAssembly assembly)
        {
            AssemblyLoad?.Invoke(AppDomain.CurrentDomain, new AssemblyLoadEventArgs(assembly));
        }

        // This method is called by the VM.
        private static RuntimeAssembly? OnResourceResolve(RuntimeAssembly assembly, string resourceName)
        {
            return InvokeResolveEvent(ResourceResolve, assembly, resourceName);
        }

        // This method is called by the VM
        private static RuntimeAssembly? OnTypeResolve(RuntimeAssembly assembly, string typeName)
        {
            return InvokeResolveEvent(TypeResolve, assembly, typeName);
        }

        // This method is called by the VM.
        private static RuntimeAssembly? OnAssemblyResolve(RuntimeAssembly assembly, string assemblyFullName)
        {
            return InvokeResolveEvent(AssemblyResolve, assembly, assemblyFullName);
        }

        [UnconditionalSuppressMessage("SingleFile", "IL3000: Avoid accessing Assembly file path when publishing as a single file",
            Justification = "The code handles the Assembly.Location equals null")]
        private static RuntimeAssembly? InvokeResolveEvent(ResolveEventHandler? eventHandler, RuntimeAssembly assembly, string name)
        {
            if (eventHandler == null)
                return null;

            var args = new ResolveEventArgs(name, assembly);

            foreach (ResolveEventHandler handler in eventHandler.GetInvocationList())
            {
                Assembly? asm = handler(AppDomain.CurrentDomain, args);
#if CORECLR
                if (eventHandler == AssemblyResolve && AssemblyLoadContext.IsTracingEnabled())
                {
                    AssemblyLoadContext.TraceAssemblyResolveHandlerInvoked(
                        name,
                        handler.Method.Name,
                        asm?.FullName,
                        asm != null && !asm.IsDynamic ? asm.Location : null);
                }
#endif // CORECLR
                RuntimeAssembly? ret = GetRuntimeAssembly(asm);
                if (ret != null)
                    return ret;
            }

            return null;
        }
#endif // !CORERT

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "Satellite assemblies have no code in them and loading is not a problem")]
        [UnconditionalSuppressMessage("SingleFile", "IL3000: Avoid accessing Assembly file path when publishing as a single file",
            Justification = "This call is fine because native call runs before this and checks BindSatelliteResourceFromBundle")]
        private Assembly? ResolveSatelliteAssembly(AssemblyName assemblyName)
        {
            // Called by native runtime when CultureName is not empty
            Debug.Assert(assemblyName.CultureName?.Length > 0);

            const string SatelliteSuffix = ".resources";

            if (assemblyName.Name == null || !assemblyName.Name.EndsWith(SatelliteSuffix, StringComparison.Ordinal))
                return null;

            string parentAssemblyName = assemblyName.Name.Substring(0, assemblyName.Name.Length - SatelliteSuffix.Length);

            Assembly parentAssembly = LoadFromAssemblyName(new AssemblyName(parentAssemblyName));

            AssemblyLoadContext parentALC = GetLoadContext(parentAssembly)!;

            string? parentDirectory = Path.GetDirectoryName(parentAssembly.Location);
            if (parentDirectory == null)
                 return null;

            string assemblyPath = Path.Combine(parentDirectory, assemblyName.CultureName!, $"{assemblyName.Name}.dll");

            bool exists = System.IO.FileSystem.FileExists(assemblyPath);
            if (!exists && PathInternal.IsCaseSensitive)
            {
#if CORECLR
                if (AssemblyLoadContext.IsTracingEnabled())
                {
                    AssemblyLoadContext.TraceSatelliteSubdirectoryPathProbed(assemblyPath, HResults.COR_E_FILENOTFOUND);
                }
#endif // CORECLR
                assemblyPath = Path.Combine(parentDirectory, assemblyName.CultureName!.ToLowerInvariant(), $"{assemblyName.Name}.dll");
                exists = System.IO.FileSystem.FileExists(assemblyPath);
            }

            Assembly? asm = exists ? parentALC.LoadFromAssemblyPath(assemblyPath) : null;
#if CORECLR
            if (AssemblyLoadContext.IsTracingEnabled())
            {
                AssemblyLoadContext.TraceSatelliteSubdirectoryPathProbed(assemblyPath, exists ? HResults.S_OK : HResults.COR_E_FILENOTFOUND);
            }
#endif // CORECLR

            return asm;
        }

        internal IntPtr GetResolvedUnmanagedDll(Assembly assembly, string unmanagedDllName)
        {
            IntPtr resolvedDll = IntPtr.Zero;

            Func<Assembly, string, IntPtr>? dllResolveHandler = _resolvingUnmanagedDll;

            if (dllResolveHandler != null)
            {
                // Loop through the event subscribers and return the first non-null native library handle
                foreach (Func<Assembly, string, IntPtr> handler in dllResolveHandler.GetInvocationList())
                {
                    resolvedDll = handler(assembly, unmanagedDllName);
                    if (resolvedDll != IntPtr.Zero)
                    {
                        return resolvedDll;
                    }
                }
            }

            return IntPtr.Zero;
        }
    }

    internal sealed class DefaultAssemblyLoadContext : AssemblyLoadContext
    {
        internal static readonly AssemblyLoadContext s_loadContext = new DefaultAssemblyLoadContext();

        internal DefaultAssemblyLoadContext() : base(true, false, "Default")
        {
        }
    }

    internal sealed class IndividualAssemblyLoadContext : AssemblyLoadContext
    {
        internal IndividualAssemblyLoadContext(string name) : base(false, false, name)
        {
        }
    }
}
