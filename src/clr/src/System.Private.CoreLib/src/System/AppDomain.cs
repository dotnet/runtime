// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace System
{
    /// <summary>
    /// Domains represent an application within the runtime. Objects cannot be
    /// shared between domains and each domain can be configured independently.
    /// </summary>
    internal sealed class AppDomain
    {
        // Domain security information
        // These fields initialized from the other side only. (NOTE: order
        // of these fields cannot be changed without changing the layout in
        // the EE- AppDomainBaseObject in this case)

        public event AssemblyLoadEventHandler AssemblyLoad;

        private ResolveEventHandler _TypeResolve;

        public event ResolveEventHandler TypeResolve
        {
            add
            {
                lock (this)
                {
                    _TypeResolve += value;
                }
            }

            remove
            {
                lock (this)
                {
                    _TypeResolve -= value;
                }
            }
        }

        private ResolveEventHandler _ResourceResolve;

        public event ResolveEventHandler ResourceResolve
        {
            add
            {
                lock (this)
                {
                    _ResourceResolve += value;
                }
            }

            remove
            {
                lock (this)
                {
                    _ResourceResolve -= value;
                }
            }
        }

        private ResolveEventHandler _AssemblyResolve;

        public event ResolveEventHandler AssemblyResolve
        {
            add
            {
                lock (this)
                {
                    _AssemblyResolve += value;
                }
            }

            remove
            {
                lock (this)
                {
                    _AssemblyResolve -= value;
                }
            }
        }

        private EventHandler _processExit;

        private EventHandler _domainUnload;

        private UnhandledExceptionEventHandler _unhandledException;

        // Delegate that will hold references to FirstChance exception notifications
        private EventHandler<FirstChanceExceptionEventArgs> _firstChanceException;

        private IntPtr _pDomain;                      // this is an unmanaged pointer (AppDomain * m_pDomain)` used from the VM.

#if FEATURE_APPX
        private static APPX_FLAGS s_flags;

        //
        // Keep in async with vm\appdomainnative.cpp
        //
        [Flags]
        private enum APPX_FLAGS
        {
            APPX_FLAGS_INITIALIZED = 0x01,

            APPX_FLAGS_APPX_MODEL = 0x02,
        }

        private static APPX_FLAGS Flags
        {
            get
            {
                if (s_flags == 0)
                    s_flags = nGetAppXFlags();

                Debug.Assert(s_flags != 0);
                return s_flags;
            }
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.I4)]
        private static extern APPX_FLAGS nGetAppXFlags();
#endif

        /// <summary>
        ///     If this AppDomain is configured to have an AppDomain manager then create the instance of it.
        ///     This method is also called from the VM to create the domain manager in the default domain.
        /// </summary>
        private void CreateAppDomainManager()
        {
            string trustedPlatformAssemblies = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES");
            if (trustedPlatformAssemblies != null)
            {
                string platformResourceRoots = (string)AppContext.GetData("PLATFORM_RESOURCE_ROOTS") ?? string.Empty;
                string appPaths = (string)AppContext.GetData("APP_PATHS") ?? string.Empty;
                string appNiPaths = (string)AppContext.GetData("APP_NI_PATHS") ?? string.Empty;
                string appLocalWinMD = (string)AppContext.GetData("APP_LOCAL_WINMETADATA") ?? string.Empty;
                SetupBindingPaths(trustedPlatformAssemblies, platformResourceRoots, appPaths, appNiPaths, appLocalWinMD);
            }
        }

        /// <summary>
        ///     Returns whether the current AppDomain follows the AppX rules.
        /// </summary>
        [Pure]
        internal static bool IsAppXModel()
        {
#if FEATURE_APPX
            return (Flags & APPX_FLAGS.APPX_FLAGS_APPX_MODEL) != 0;
#else
            return false;
#endif
        }

        /// <summary>
        ///     Checks (and throws on failure) if the domain supports Assembly.LoadFrom.
        /// </summary>
        [Pure]
        internal static void CheckLoadFromSupported()
        {
#if FEATURE_APPX
            if (IsAppXModel())
                throw new NotSupportedException(SR.Format(SR.NotSupported_AppX, "Assembly.LoadFrom"));
#endif
        }

        /// <summary>
        ///     Checks (and throws on failure) if the domain supports Assembly.LoadFile.
        /// </summary>
        [Pure]
        internal static void CheckLoadFileSupported()
        {
#if FEATURE_APPX
            if (IsAppXModel())
                throw new NotSupportedException(SR.Format(SR.NotSupported_AppX, "Assembly.LoadFile"));
#endif
        }

        /// <summary>
        ///     Checks (and throws on failure) if the domain supports Assembly.Load(byte[] ...).
        /// </summary>
        [Pure]
        internal static void CheckLoadByteArraySupported()
        {
#if FEATURE_APPX
            if (IsAppXModel())
                throw new NotSupportedException(SR.Format(SR.NotSupported_AppX, "Assembly.Load(byte[], ...)"));
#endif
        }

        public static AppDomain CurrentDomain => Thread.GetDomain();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern Assembly[] nGetAssemblies(bool forIntrospection);

        internal Assembly[] GetAssemblies(bool forIntrospection)
        {
            return nGetAssemblies(forIntrospection);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void PublishAnonymouslyHostedDynamicMethodsAssembly(RuntimeAssembly assemblyHandle);

        [Obsolete("AppDomain.GetCurrentThreadId has been deprecated because it does not provide a stable Id when managed threads are running on fibers (aka lightweight threads). To get a stable identifier for a managed thread, use the ManagedThreadId property on Thread.  http://go.microsoft.com/fwlink/?linkid=14202", false)]
        [DllImport(Interop.Libraries.Kernel32)]
        public static extern int GetCurrentThreadId();

        private AppDomain()
        {
            Debug.Fail("Object cannot be created through this constructor.");
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void nSetupBindingPaths(string trustedPlatformAssemblies, string platformResourceRoots, string appPath, string appNiPaths, string appLocalWinMD);

        internal void SetupBindingPaths(string trustedPlatformAssemblies, string platformResourceRoots, string appPath, string appNiPaths, string appLocalWinMD)
        {
            nSetupBindingPaths(trustedPlatformAssemblies, platformResourceRoots, appPath, appNiPaths, appLocalWinMD);
        }

        // support reliability for certain event handlers, if the target
        // methods also participate in this discipline.  If caller passes
        // an existing MulticastDelegate, then we could use a MDA to indicate
        // that reliability is not guaranteed.  But if it is a single cast
        // scenario, we can make it work.

        public event EventHandler ProcessExit
        {
            add
            {
                if (value != null)
                {
                    RuntimeHelpers.PrepareContractedDelegate(value);
                    lock (this)
                        _processExit += value;
                }
            }
            remove
            {
                lock (this)
                    _processExit -= value;
            }
        }

        public event EventHandler DomainUnload
        {
            add
            {
                if (value != null)
                {
                    RuntimeHelpers.PrepareContractedDelegate(value);
                    lock (this)
                        _domainUnload += value;
                }
            }
            remove
            {
                lock (this)
                    _domainUnload -= value;
            }
        }

        public event UnhandledExceptionEventHandler UnhandledException
        {
            add
            {
                if (value != null)
                {
                    RuntimeHelpers.PrepareContractedDelegate(value);
                    lock (this)
                        _unhandledException += value;
                }
            }
            remove
            {
                lock (this)
                    _unhandledException -= value;
            }
        }

        // This is the event managed code can wireup against to be notified
        // about first chance exceptions.
        //
        // To register/unregister the callback, the code must be SecurityCritical.
        public event EventHandler<FirstChanceExceptionEventArgs> FirstChanceException
        {
            add
            {
                if (value != null)
                {
                    RuntimeHelpers.PrepareContractedDelegate(value);
                    lock (this)
                        _firstChanceException += value;
                }
            }
            remove
            {
                lock (this)
                    _firstChanceException -= value;
            }
        }

        // This method is called by the VM.
        private void OnAssemblyLoadEvent(RuntimeAssembly LoadedAssembly)
        {
            AssemblyLoad?.Invoke(this, new AssemblyLoadEventArgs(LoadedAssembly));
        }

        // This method is called by the VM.
        private RuntimeAssembly OnResourceResolveEvent(RuntimeAssembly assembly, string resourceName)
        {
            return InvokeResolveEvent(_ResourceResolve, assembly, resourceName);
        }

        // This method is called by the VM
        private RuntimeAssembly OnTypeResolveEvent(RuntimeAssembly assembly, string typeName)
        {
            return InvokeResolveEvent(_TypeResolve, assembly, typeName);
        }

        // This method is called by the VM.
        private RuntimeAssembly OnAssemblyResolveEvent(RuntimeAssembly assembly, string assemblyFullName)
        {
            return InvokeResolveEvent(_AssemblyResolve, assembly, assemblyFullName);
        }

        private RuntimeAssembly InvokeResolveEvent(ResolveEventHandler eventHandler, RuntimeAssembly assembly, string name)
        {
            if (eventHandler == null)
                return null;

            var args = new ResolveEventArgs(name, assembly);

            foreach (ResolveEventHandler handler in eventHandler.GetInvocationList())
            {
                Assembly asm = handler(this, args);
                RuntimeAssembly ret = GetRuntimeAssembly(asm);
                if (ret != null)
                    return ret;
            }

            return null;
        }

#if FEATURE_COMINTEROP
        // Called by VM - code:CLRPrivTypeCacheWinRT::RaiseDesignerNamespaceResolveEvent
        private string[] OnDesignerNamespaceResolveEvent(string namespaceName)
        {
            return System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMetadata.OnDesignerNamespaceResolveEvent(this, namespaceName);
        }
#endif // FEATURE_COMINTEROP

        private static RuntimeAssembly GetRuntimeAssembly(Assembly asm)
        {
            return
                asm == null ? null :
                asm is RuntimeAssembly rtAssembly ? rtAssembly :
                asm is AssemblyBuilder ab ? ab.InternalAssembly :
                null;
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void nSetNativeDllSearchDirectories(string paths);

        private static void Setup(string friendlyName,
                                    string[] propertyNames,
                                    string[] propertyValues)
        {
            AppDomain ad = CurrentDomain;

            if (propertyNames != null && propertyValues != null)
            {
                for (int i = 0; i < propertyNames.Length; i++)
                {
                    // We want to set native dll probing directories before any P/Invokes have a
                    // chance to fire. The Path class, for one, has P/Invokes.
                    if (propertyNames[i] == "NATIVE_DLL_SEARCH_DIRECTORIES")
                    {
                        if (propertyValues[i] == null)
                            throw new ArgumentNullException("NATIVE_DLL_SEARCH_DIRECTORIES");

                        string paths = propertyValues[i];
                        if (paths.Length == 0)
                            break;

                        nSetNativeDllSearchDirectories(paths);
                    }
                }

                for (int i = 0; i < propertyNames.Length; i++)
                {
                    if (propertyNames[i] != null)
                    {
                        AppContext.SetData(propertyNames[i], propertyValues[i]);
                    }
                }
            }

            // set up the friendly name
            ad.nSetupFriendlyName(friendlyName);

            ad.CreateAppDomainManager(); // could modify FusionStore's object
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void nSetupFriendlyName(string friendlyName);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal extern string IsStringInterned(string str);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal extern string GetOrInternString(string str);

        public int Id => GetId();

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal extern int GetId();
    }
}
