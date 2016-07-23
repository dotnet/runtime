// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: Domains represent an application within the runtime. Objects can 
**          not be shared between domains and each domain can be configured
**          independently. 
**
**
=============================================================================*/

namespace System {
    using System;
#if FEATURE_CLICKONCE    
    using System.Deployment.Internal.Isolation;
    using System.Deployment.Internal.Isolation.Manifest;
    using System.Runtime.Hosting;    
#endif    
    using System.Reflection;
    using System.Runtime;
    using System.Runtime.CompilerServices;
#if FEATURE_REMOTING    
    using System.Runtime.Remoting.Channels;
    using System.Runtime.Remoting.Contexts;
#endif    
    using System.Security;
    using System.Security.Permissions;
#if FEATURE_IMPERSONATION
    using System.Security.Principal;
#endif
    using System.Security.Policy;
    using System.Security.Util;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;
    using System.Runtime.InteropServices;
    using System.Runtime.Remoting;   
#if FEATURE_REMOTING        
    using Context = System.Runtime.Remoting.Contexts.Context;
#endif
    using System.Reflection.Emit;
    using CultureInfo = System.Globalization.CultureInfo;
#if !FEATURE_CORECLR
    using System.Globalization;
#endif
    using System.IO;
    using AssemblyHashAlgorithm = System.Configuration.Assemblies.AssemblyHashAlgorithm;
    using System.Text;
    using Microsoft.Win32;
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.Versioning;
    using System.Diagnostics.Contracts;
#if FEATURE_EXCEPTION_NOTIFICATIONS
    using System.Runtime.ExceptionServices;
#endif // FEATURE_EXCEPTION_NOTIFICATIONS

    [ComVisible(true)]
    public class ResolveEventArgs : EventArgs
    {
        private String _Name;
        private Assembly _RequestingAssembly;

        public String Name {
            get {
                return _Name;
            }
        }

        public Assembly RequestingAssembly 
        {
            get
            {
                return _RequestingAssembly;
            }
        }

        public ResolveEventArgs(String name)
        {
            _Name = name;
        }

        public ResolveEventArgs(String name, Assembly requestingAssembly)
        {
            _Name = name;
            _RequestingAssembly = requestingAssembly;
        }
    }

    [ComVisible(true)]
    public class AssemblyLoadEventArgs : EventArgs
    {
        private Assembly _LoadedAssembly;

        public Assembly LoadedAssembly {
            get {
                return _LoadedAssembly;
            }
        }

        public AssemblyLoadEventArgs(Assembly loadedAssembly)
        {
            _LoadedAssembly = loadedAssembly;
        }
    }


    #if FEATURE_CORECLR
    [System.Security.SecurityCritical] // auto-generated
    #endif
    [Serializable]
    [ComVisible(true)]
    public delegate Assembly ResolveEventHandler(Object sender, ResolveEventArgs args);

    [Serializable]
    [ComVisible(true)]
    public delegate void AssemblyLoadEventHandler(Object sender, AssemblyLoadEventArgs args);

    [Serializable]
    [ComVisible(true)]
    public delegate void AppDomainInitializer(string[] args);

    internal class AppDomainInitializerInfo
    {
        internal class ItemInfo
        {
            public string TargetTypeAssembly;
            public string TargetTypeName;
            public string MethodName;
        }

        internal ItemInfo[] Info;

        internal AppDomainInitializerInfo(AppDomainInitializer init)
        {
            Info=null;
            if (init==null)
                return;
            List<ItemInfo> itemInfo = new List<ItemInfo>();
            List<AppDomainInitializer> nestedDelegates = new List<AppDomainInitializer>();
            nestedDelegates.Add(init);
            int idx=0;
 
            while (nestedDelegates.Count>idx)
            {
                AppDomainInitializer curr = nestedDelegates[idx++];
                Delegate[] list= curr.GetInvocationList();
                for (int i=0;i<list.Length;i++)
                {
                    if (!list[i].Method.IsStatic) 
                    {
                        if(list[i].Target==null)
                            continue;
                    
                        AppDomainInitializer nested = list[i].Target as AppDomainInitializer;
                        if (nested!=null)
                            nestedDelegates.Add(nested);
                        else
                            throw new ArgumentException(Environment.GetResourceString("Arg_MustBeStatic"),
                               list[i].Method.ReflectedType.FullName+"::"+list[i].Method.Name);
                    }
                    else
                    {
                        ItemInfo info=new ItemInfo();
                        info.TargetTypeAssembly=list[i].Method.ReflectedType.Module.Assembly.FullName;
                        info.TargetTypeName=list[i].Method.ReflectedType.FullName;
                        info.MethodName=list[i].Method.Name;
                        itemInfo.Add(info);
                    }
                    
                }
            }

            Info = itemInfo.ToArray();            
        }
        
        [System.Security.SecuritySafeCritical]  // auto-generated
        internal AppDomainInitializer Unwrap()
        {
            if (Info==null)
                return null;
            AppDomainInitializer retVal=null;
            new ReflectionPermission(ReflectionPermissionFlag.MemberAccess).Assert();
            for (int i=0;i<Info.Length;i++)
            {
                Assembly assembly=Assembly.Load(Info[i].TargetTypeAssembly);
                AppDomainInitializer newVal=(AppDomainInitializer)Delegate.CreateDelegate(typeof(AppDomainInitializer),
                        assembly.GetType(Info[i].TargetTypeName),
                        Info[i].MethodName);
                if(retVal==null)
                    retVal=newVal;
                else
                    retVal+=newVal;
            }
            return retVal;
        }
    }


    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(System._AppDomain))]
    [ComVisible(true)]
    public sealed class AppDomain :
#if FEATURE_REMOTING
        MarshalByRefObject,
#endif
        _AppDomain, IEvidenceFactory
    {
        // Domain security information
        // These fields initialized from the other side only. (NOTE: order 
        // of these fields cannot be changed without changing the layout in 
        // the EE)

        [System.Security.SecurityCritical] // auto-generated
        private AppDomainManager _domainManager;
        private Dictionary<String, Object[]> _LocalStore;
        private AppDomainSetup   _FusionStore;
        private Evidence         _SecurityIdentity;
#pragma warning disable 169
        private Object[]         _Policies; // Called from the VM.
#pragma warning restore 169
        [method: System.Security.SecurityCritical]
        public event AssemblyLoadEventHandler AssemblyLoad;

        [System.Security.SecurityCritical]
        private ResolveEventHandler _TypeResolve;

        public event ResolveEventHandler TypeResolve
        {
            [System.Security.SecurityCritical]
            add
            {
                lock (this)
                {
                    _TypeResolve += value;
                }
            }

            [System.Security.SecurityCritical]
            remove
            {
                lock (this)
                {
                    _TypeResolve -= value;
                }
            }
        }

        [System.Security.SecurityCritical]
        private ResolveEventHandler _ResourceResolve;

        public event ResolveEventHandler ResourceResolve
        {
            [System.Security.SecurityCritical]
            add
            {
                lock (this)
                {
                    _ResourceResolve += value;
                }
            }

            [System.Security.SecurityCritical]
            remove
            {
                lock (this)
                {
                    _ResourceResolve -= value;
                }
            }
        }

        [System.Security.SecurityCritical]
        private ResolveEventHandler _AssemblyResolve;

        public event ResolveEventHandler AssemblyResolve
        {
            [System.Security.SecurityCritical]
            add
            {
                lock (this)
                {
                    _AssemblyResolve += value;
                }
            }

            [System.Security.SecurityCritical]
            remove
            {
                lock (this)
                {
                    _AssemblyResolve -= value;
                }
            }
        }

#if FEATURE_REFLECTION_ONLY_LOAD
        [method: System.Security.SecurityCritical]
        public event ResolveEventHandler ReflectionOnlyAssemblyResolve;
#endif // FEATURE_REFLECTION_ONLY

#if FEATURE_REMOTING        
        private Context          _DefaultContext;
#endif

#if FEATURE_CLICKONCE
        private ActivationContext _activationContext;        
        private ApplicationIdentity _applicationIdentity;        
#endif
        private ApplicationTrust _applicationTrust;

#if FEATURE_IMPERSONATION
        private IPrincipal       _DefaultPrincipal;
#endif // FEATURE_IMPERSONATION
#if FEATURE_REMOTING
        private DomainSpecificRemotingData _RemotingData;
#endif
        private EventHandler     _processExit;

        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] 
        #endif
        private EventHandler     _domainUnload;

        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        private UnhandledExceptionEventHandler _unhandledException;

#if FEATURE_APTCA
        private String[]         _aptcaVisibleAssemblies;
#endif

        // The compat flags are set at domain creation time to indicate that the given breaking
        // changes (named in the strings) should not be used in this domain. We only use the 
        // keys, the vhe values are ignored.
        private Dictionary<String, object>  _compatFlags;

#if FEATURE_EXCEPTION_NOTIFICATIONS
        // Delegate that will hold references to FirstChance exception notifications
        private EventHandler<FirstChanceExceptionEventArgs> _firstChanceException;
#endif // FEATURE_EXCEPTION_NOTIFICATIONS

        private IntPtr           _pDomain;                      // this is an unmanaged pointer (AppDomain * m_pDomain)` used from the VM.

#if FEATURE_CAS_POLICY
        private PrincipalPolicy  _PrincipalPolicy;              // this is an enum
#endif        
        private bool             _HasSetPolicy;
        private bool             _IsFastFullTrustDomain;        // quick check to see if the AppDomain is fully trusted and homogenous
        private bool             _compatFlagsInitialized;

        internal const String TargetFrameworkNameAppCompatSetting = "TargetFrameworkName";

#if FEATURE_APPX
        private static APPX_FLAGS s_flags;

        //
        // Keep in async with vm\appdomainnative.cpp
        //
        [Flags]
        private enum APPX_FLAGS
        {
            APPX_FLAGS_INITIALIZED =        0x01,

            APPX_FLAGS_APPX_MODEL =         0x02,
            APPX_FLAGS_APPX_DESIGN_MODE =   0x04,
            APPX_FLAGS_APPX_NGEN =          0x08,
            APPX_FLAGS_APPX_MASK =          APPX_FLAGS_APPX_MODEL |
                                            APPX_FLAGS_APPX_DESIGN_MODE |
                                            APPX_FLAGS_APPX_NGEN,

            APPX_FLAGS_API_CHECK =          0x10,
        }

        private static APPX_FLAGS Flags
        {
            [SecuritySafeCritical]
            get
            {
                if (s_flags == 0)
                    s_flags = nGetAppXFlags();

                Contract.Assert(s_flags != 0);
                return s_flags;
            }
        }

        internal static bool ProfileAPICheck
        {
            [SecuritySafeCritical]
            get
            {
                return (Flags & APPX_FLAGS.APPX_FLAGS_API_CHECK) != 0;
            }
        }

        internal static bool IsAppXNGen
        {
            [SecuritySafeCritical]
            get
            {
                return (Flags & APPX_FLAGS.APPX_FLAGS_APPX_NGEN) != 0;
            }
        }
#endif // FEATURE_APPX

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DisableFusionUpdatesFromADManager(AppDomainHandle domain);

#if FEATURE_APPX
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.I4)]
        private static extern APPX_FLAGS nGetAppXFlags();
#endif

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity]
        private static extern void GetAppDomainManagerType(AppDomainHandle domain,
                                                           StringHandleOnStack retAssembly,
                                                           StringHandleOnStack retType);

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity]
        private static extern void SetAppDomainManagerType(AppDomainHandle domain,
                                                           string assembly,
                                                           string type);

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void nSetHostSecurityManagerFlags (HostSecurityManagerOptions flags);

        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity]
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void SetSecurityHomogeneousFlag(AppDomainHandle domain,
                                                              [MarshalAs(UnmanagedType.Bool)] bool runtimeSuppliedHomogenousGrantSet);

#if FEATURE_CAS_POLICY
        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity]
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void SetLegacyCasPolicyEnabled(AppDomainHandle domain);

        [SecurityCritical]
        private void SetLegacyCasPolicyEnabled()
        {
            SetLegacyCasPolicyEnabled(GetNativeHandle());
        }
#endif // FEATURE_CAS_POLICY

        /// <summary>
        ///     Get a handle used to make a call into the VM pointing to this domain
        /// </summary>
        internal AppDomainHandle GetNativeHandle()
        {
            // This should never happen under normal circumstances. However, there ar ways to create an
            // uninitialized object through remoting, etc.
            if (_pDomain.IsNull())
            {
                throw new InvalidOperationException(Environment.GetResourceString("Argument_InvalidHandle"));
            }

#if FEATURE_REMOTING
            BCLDebug.Assert(!RemotingServices.IsTransparentProxy(this), "QCalls should be made with the real AppDomain object rather than a transparent proxy");
#endif // FEATURE_REMOTING
            return new AppDomainHandle(_pDomain);
        }

        /// <summary>
        ///     If this AppDomain is configured to have an AppDomain manager then create the instance of it.
        ///     This method is also called from the VM to create the domain manager in the default domain.
        /// </summary>
        [SecuritySafeCritical]
        private void CreateAppDomainManager()
        {
            Contract.Assert(_domainManager == null, "_domainManager == null");

            AppDomainSetup adSetup = FusionStore;
#if FEATURE_VERSIONING
            String trustedPlatformAssemblies = (String)(GetData("TRUSTED_PLATFORM_ASSEMBLIES"));
            if (trustedPlatformAssemblies != null)
            {
                String platformResourceRoots = (String)(GetData("PLATFORM_RESOURCE_ROOTS"));
                if (platformResourceRoots == null)
                {
                    platformResourceRoots = String.Empty;
                }

                String appPaths = (String)(GetData("APP_PATHS"));
                if (appPaths == null)
                {
                    appPaths = String.Empty;
                }

                String appNiPaths = (String)(GetData("APP_NI_PATHS"));
                if (appNiPaths == null)
                {
                    appNiPaths = String.Empty;
                }

                String appLocalWinMD = (String)(GetData("APP_LOCAL_WINMETADATA"));
                if (appLocalWinMD == null)
                {
                    appLocalWinMD = String.Empty;
                }
                SetupBindingPaths(trustedPlatformAssemblies, platformResourceRoots, appPaths, appNiPaths, appLocalWinMD);
            }
#endif // FEATURE_VERSIONING

            string domainManagerAssembly;
            string domainManagerType;
            GetAppDomainManagerType(out domainManagerAssembly, out domainManagerType);

            if (domainManagerAssembly != null && domainManagerType != null)
            {
                try
                {
                    new PermissionSet(PermissionState.Unrestricted).Assert();
                    _domainManager = CreateInstanceAndUnwrap(domainManagerAssembly, domainManagerType) as AppDomainManager;
                    CodeAccessPermission.RevertAssert();
                }
                catch (FileNotFoundException e)
                {
                    throw new TypeLoadException(Environment.GetResourceString("Argument_NoDomainManager"), e);
                }
                catch (SecurityException e)
                {
                    throw new TypeLoadException(Environment.GetResourceString("Argument_NoDomainManager"), e);
                }
                catch (TypeLoadException e)
                {
                    throw new TypeLoadException(Environment.GetResourceString("Argument_NoDomainManager"), e);
                }

                if (_domainManager == null)
                {
                    throw new TypeLoadException(Environment.GetResourceString("Argument_NoDomainManager"));
                }

                // If this domain was not created by a managed call to CreateDomain, then the AppDomainSetup
                // will not have the correct values for the AppDomainManager set.
                FusionStore.AppDomainManagerAssembly = domainManagerAssembly;
                FusionStore.AppDomainManagerType = domainManagerType;

                bool notifyFusion = _domainManager.GetType() != typeof(System.AppDomainManager) && !DisableFusionUpdatesFromADManager();



                AppDomainSetup FusionStoreOld = null;
                if (notifyFusion)
                    FusionStoreOld = new AppDomainSetup(FusionStore, true);

                // Initialize the AppDomainMAnager and register the instance with the native host if requested
                _domainManager.InitializeNewDomain(FusionStore);

                if (notifyFusion)
                    SetupFusionStore(_FusionStore, FusionStoreOld); // Notify Fusion about the changes the user implementation of InitializeNewDomain may have made to the FusionStore object.
                                
#if FEATURE_APPDOMAINMANAGER_INITOPTIONS                
                AppDomainManagerInitializationOptions flags = _domainManager.InitializationFlags;
                if ((flags & AppDomainManagerInitializationOptions.RegisterWithHost) == AppDomainManagerInitializationOptions.RegisterWithHost)
                {
                    _domainManager.RegisterWithHost();
                }
#endif // FEATURE_APPDOMAINMANAGER_INITOPTIONS
            }

            InitializeCompatibilityFlags();       
        }

        /// <summary>
        ///     Initialize the compatibility flags to non-NULL values.
        ///     This method is also called from the VM when the default domain dosen't have a domain manager.
        /// </summary>
        private void InitializeCompatibilityFlags()
        {
            AppDomainSetup adSetup = FusionStore;
            
            // set up shim flags regardless of whether we create a DomainManager in this method.
            if (adSetup.GetCompatibilityFlags() != null)
            {
                _compatFlags = new Dictionary<String, object>(adSetup.GetCompatibilityFlags(), StringComparer.OrdinalIgnoreCase);
            }

            // for perf, we don't intialize the _compatFlags dictionary when we don't need to.  However, we do need to make a 
            // note that we've run this method, because IsCompatibilityFlagsSet needs to return different values for the
            // case where the compat flags have been setup.
            Contract.Assert(!_compatFlagsInitialized);
            _compatFlagsInitialized = true;

            CompatibilitySwitches.InitializeSwitches();
        }

        // Retrieves a possibly-cached target framework name for this appdomain.  This could be set
        // either by a host in native, a host in managed using an AppDomainSetup, or by the 
        // TargetFrameworkAttribute on the executable (VS emits its target framework moniker using this
        // attribute starting in version 4).
        [SecuritySafeCritical]
        internal String GetTargetFrameworkName()
        {
            String targetFrameworkName = _FusionStore.TargetFrameworkName;

            if (targetFrameworkName == null && IsDefaultAppDomain() && !_FusionStore.CheckedForTargetFrameworkName)
            {
                // This should only be run in the default appdomain.  All other appdomains should have
                // values copied from the default appdomain and/or specified by the host.
                Assembly assembly = Assembly.GetEntryAssembly();
                if (assembly != null)
                {
                    TargetFrameworkAttribute[] attrs = (TargetFrameworkAttribute[])assembly.GetCustomAttributes(typeof(TargetFrameworkAttribute));
                    if (attrs != null && attrs.Length > 0)
                    {
                        Contract.Assert(attrs.Length == 1);
                        targetFrameworkName = attrs[0].FrameworkName;
                        _FusionStore.TargetFrameworkName = targetFrameworkName;
                    }
                }
                _FusionStore.CheckedForTargetFrameworkName = true;
            }

            return targetFrameworkName;
        }

        /// <summary>
        ///     Returns the setting of the corresponding compatibility config switch (see CreateAppDomainManager for the impact).
        /// </summary>
        [SecuritySafeCritical]
        internal bool DisableFusionUpdatesFromADManager()
        {
            return DisableFusionUpdatesFromADManager(GetNativeHandle());
        }

        /// <summary>
        ///     Returns whether the current AppDomain follows the AppX rules.
        /// </summary>
        [SecuritySafeCritical]
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
        ///     Returns the setting of the AppXDevMode config switch.
        /// </summary>
        [SecuritySafeCritical]
        [Pure]
        internal static bool IsAppXDesignMode()
        {
#if FEATURE_APPX
            return (Flags & APPX_FLAGS.APPX_FLAGS_APPX_MASK) == (APPX_FLAGS.APPX_FLAGS_APPX_MODEL | APPX_FLAGS.APPX_FLAGS_APPX_DESIGN_MODE);
#else
            return false;
#endif
        }

        /// <summary>
        ///     Checks (and throws on failure) if the domain supports Assembly.LoadFrom.
        /// </summary>
        [SecuritySafeCritical]
        [Pure]
        internal static void CheckLoadFromSupported()
        {
#if FEATURE_APPX
            if (IsAppXModel())
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_AppX", "Assembly.LoadFrom"));
#endif
        }

        /// <summary>
        ///     Checks (and throws on failure) if the domain supports Assembly.LoadFile.
        /// </summary>
        [SecuritySafeCritical]
        [Pure]
        internal static void CheckLoadFileSupported()
        {
#if FEATURE_APPX
            if (IsAppXModel())
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_AppX", "Assembly.LoadFile"));
#endif
        }

        /// <summary>
        ///     Checks (and throws on failure) if the domain supports Assembly.ReflectionOnlyLoad.
        /// </summary>
        [SecuritySafeCritical]
        [Pure]
        internal static void CheckReflectionOnlyLoadSupported()
        {
#if FEATURE_APPX
            if (IsAppXModel())
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_AppX", "Assembly.ReflectionOnlyLoad"));
#endif
        }

        /// <summary>
        ///     Checks (and throws on failure) if the domain supports Assembly.LoadWithPartialName.
        /// </summary>
        [SecuritySafeCritical]
        [Pure]
        internal static void CheckLoadWithPartialNameSupported(StackCrawlMark stackMark)
        {
#if FEATURE_APPX
            if (IsAppXModel())
            {
                RuntimeAssembly callingAssembly = RuntimeAssembly.GetExecutingAssembly(ref stackMark);
                bool callerIsFxAssembly = callingAssembly != null && callingAssembly.IsFrameworkAssembly();
                if (!callerIsFxAssembly)
                {
                    throw new NotSupportedException(Environment.GetResourceString("NotSupported_AppX", "Assembly.LoadWithPartialName"));
                }
            }
#endif
        }

        /// <summary>
        ///     Checks (and throws on failure) if the domain supports DefinePInvokeMethod.
        /// </summary>
        [SecuritySafeCritical]
        [Pure]
        internal static void CheckDefinePInvokeSupported()
        {
            // We don't want users to use DefinePInvokeMethod in RefEmit to bypass app store validation on allowed native libraries.
#if FEATURE_APPX
            if (IsAppXModel())
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_AppX", "DefinePInvokeMethod"));
#endif
        }

        /// <summary>
        ///     Checks (and throws on failure) if the domain supports Assembly.Load(byte[] ...).
        /// </summary>
        [SecuritySafeCritical]
        [Pure]
        internal static void CheckLoadByteArraySupported()
        {
#if FEATURE_APPX
            if (IsAppXModel())
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_AppX", "Assembly.Load(byte[], ...)"));
#endif
        }

        /// <summary>
        ///     Checks (and throws on failure) if the domain supports AppDomain.CreateDomain.
        /// </summary>
        [SecuritySafeCritical]
        [Pure]
        internal static void CheckCreateDomainSupported()
        {
#if FEATURE_APPX
            // Can create a new domain in an AppX process only when DevMode is enabled and
            // AssemblyLoadingCompat is not enabled (since there is no multi-domain support
            // for LoadFrom and LoadFile in AppX.
            if(IsAppXModel())
            {
                if (!IsAppXDesignMode())
                {
                    throw new NotSupportedException(Environment.GetResourceString("NotSupported_AppX", "AppDomain.CreateDomain"));
                }
            }
#endif
        }

        /// <summary>
        ///     Get the name of the assembly and type that act as the AppDomainManager for this domain
        /// </summary>
        [SecuritySafeCritical]
        internal void GetAppDomainManagerType(out string assembly, out string type)
        {
            // We can't just use our parameters because we need to ensure that the strings used for hte QCall
            // are on the stack.
            string localAssembly = null;
            string localType = null;

            GetAppDomainManagerType(GetNativeHandle(),
                                    JitHelpers.GetStringHandleOnStack(ref localAssembly),
                                    JitHelpers.GetStringHandleOnStack(ref localType));

            assembly = localAssembly;
            type = localType;
        }

        /// <summary>
        ///     Set the assembly and type which act as the AppDomainManager for this domain
        /// </summary>
        [SecuritySafeCritical]
        private void SetAppDomainManagerType(string assembly, string type)
        {
            Contract.Assert(assembly != null, "assembly != null");
            Contract.Assert(type != null, "type != null");
            SetAppDomainManagerType(GetNativeHandle(), assembly, type);
        }

#if FEATURE_APTCA
        internal String[] PartialTrustVisibleAssemblies
        {
            get { return _aptcaVisibleAssemblies; }

            [SecuritySafeCritical]
            set
            {
                _aptcaVisibleAssemblies = value;

                // Build up the canonical representaiton of this list to allow the VM to do optimizations in
                // common cases
                string canonicalConditionalAptcaList = null;
                if (value != null)
                {
                    StringBuilder conditionalAptcaListBuilder = StringBuilderCache.Acquire();
                    for (int i = 0; i < value.Length; ++i)
                    {
                        if (value[i] != null)
                        {
                            conditionalAptcaListBuilder.Append(value[i].ToUpperInvariant());
                            if (i != value.Length - 1)
                            {
                                conditionalAptcaListBuilder.Append(';');
                            }
                        }
                    }

                    canonicalConditionalAptcaList = StringBuilderCache.GetStringAndRelease(conditionalAptcaListBuilder);
                }

                SetCanonicalConditionalAptcaList(canonicalConditionalAptcaList);
            }
        }

        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity]
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void SetCanonicalConditionalAptcaList(AppDomainHandle appDomain, string canonicalList);

        [SecurityCritical]
        private void SetCanonicalConditionalAptcaList(string canonicalList)
        {
            SetCanonicalConditionalAptcaList(GetNativeHandle(), canonicalList);
        }
#endif // FEATURE_APTCA

#if FEATURE_CLICKONCE
        /// <summary>
        ///    If the CLR is being started up to run a ClickOnce applicaiton, setup the default AppDomain
        ///    with information about that application.
        /// </summary>
        private void SetupDefaultClickOnceDomain(string fullName, string[] manifestPaths, string[] activationData)
        {
            Contract.Requires(fullName != null, "fullName != null");
            FusionStore.ActivationArguments = new ActivationArguments(fullName, manifestPaths, activationData);
        }
#endif // FEATURE_CLICKONCE

        /// <summary>
        ///     Called for every AppDomain (including the default domain) to initialize the security of the AppDomain)
        /// </summary>
        [SecurityCritical]
        private void InitializeDomainSecurity(Evidence providedSecurityInfo,
                                              Evidence creatorsSecurityInfo,
                                              bool generateDefaultEvidence,
                                              IntPtr parentSecurityDescriptor,
                                              bool publishAppDomain)
        {
            AppDomainSetup adSetup = FusionStore;

#if FEATURE_CAS_POLICY
            // If the AppDomain is setup to use legacy CAS policy, then set that bit in the application
            // security descriptor.
            if (CompatibilitySwitches.IsNetFx40LegacySecurityPolicy)
            {
                SetLegacyCasPolicyEnabled();
            }
#endif // FEATURE_CAS_POLICY

#if FEATURE_CLICKONCE

            // Check if the domain manager set an ActivationContext (Debug-In-Zone for example)
            // or if this is an AppDomain with an ApplicationTrust.
            if (adSetup.ActivationArguments != null) {
                // Merge the new evidence with the manifest's evidence if applicable
                ActivationContext activationContext = null;
                ApplicationIdentity appIdentity = null;
                string[] activationData = null;
                CmsUtils.CreateActivationContext(adSetup.ActivationArguments.ApplicationFullName,
                                                 adSetup.ActivationArguments.ApplicationManifestPaths,
                                                 adSetup.ActivationArguments.UseFusionActivationContext,
                                                 out appIdentity, out activationContext);
                activationData = adSetup.ActivationArguments.ActivationData;
                providedSecurityInfo = CmsUtils.MergeApplicationEvidence(providedSecurityInfo,
                                                                         appIdentity,
                                                                         activationContext,
                                                                         activationData,
                                                                         adSetup.ApplicationTrust);
                SetupApplicationHelper(providedSecurityInfo, creatorsSecurityInfo, appIdentity, activationContext, activationData);
            }
            else 
#endif // FEATURE_CLICKONCE
            {
                bool runtimeSuppliedHomogenousGrant = false;
                ApplicationTrust appTrust = adSetup.ApplicationTrust;
                
#if FEATURE_CAS_POLICY
                // In non-legacy CAS mode, domains should be homogenous.  If the host has not specified a sandbox
                // of their own, we'll set it up to be fully trusted.  We must read the IsLegacyCasPolicy
                // enabled property here rathern than just reading the switch from above because the entire
                // process may also be opted into legacy CAS policy mode.
                if (appTrust == null && !IsLegacyCasPolicyEnabled) {
                    _IsFastFullTrustDomain = true;
                    runtimeSuppliedHomogenousGrant = true;
                }
#endif // FEATURE_CAS_POLICY

                if (appTrust != null) {
                    SetupDomainSecurityForHomogeneousDomain(appTrust, runtimeSuppliedHomogenousGrant);
                }
                else if (_IsFastFullTrustDomain) {
                    SetSecurityHomogeneousFlag(GetNativeHandle(), runtimeSuppliedHomogenousGrant);
                }
            }

            // Get the evidence supplied for the domain.  If no evidence was supplied, it means that we want
            // to use the default evidence creation strategy for this domain
            Evidence newAppDomainEvidence = (providedSecurityInfo != null ? providedSecurityInfo : creatorsSecurityInfo);
            if (newAppDomainEvidence == null && generateDefaultEvidence) {
#if FEATURE_CAS_POLICY
                newAppDomainEvidence = new Evidence(new AppDomainEvidenceFactory(this));
#else // !FEATURE_CAS_POLICY
                newAppDomainEvidence = new Evidence();
#endif // FEATURE_CAS_POLICY
            }

#if FEATURE_CAS_POLICY
            if (_domainManager != null) {
                // Give the host a chance to alter the AppDomain evidence
                HostSecurityManager securityManager = _domainManager.HostSecurityManager;
                if (securityManager != null) {
                    nSetHostSecurityManagerFlags (securityManager.Flags);
                    if ((securityManager.Flags & HostSecurityManagerOptions.HostAppDomainEvidence) == HostSecurityManagerOptions.HostAppDomainEvidence) {
                        newAppDomainEvidence = securityManager.ProvideAppDomainEvidence(newAppDomainEvidence);
                        // If this is a disconnected evidence collection, then attach it to the AppDomain,
                        // allowing the host security manager to get callbacks for delay generated evidence
                        if (newAppDomainEvidence != null && newAppDomainEvidence.Target == null) {
                            newAppDomainEvidence.Target = new AppDomainEvidenceFactory(this);
                        }
                    }
                }
            }

#endif // FEATURE_CAS_POLICY

            // Set the evidence on the managed side
            _SecurityIdentity = newAppDomainEvidence;

            // Set the evidence of the AppDomain in the VM.
            // Also, now that the initialization is complete, signal that to the security system.
            // Finish the AppDomain initialization and resolve the policy for the AppDomain evidence.
            SetupDomainSecurity(newAppDomainEvidence,
                                parentSecurityDescriptor,
                                publishAppDomain);

#if FEATURE_CAS_POLICY
            // The AppDomain is now resolved. Go ahead and set the PolicyLevel
            // from the HostSecurityManager if specified.
            if (_domainManager != null)
                RunDomainManagerPostInitialization(_domainManager);
#endif // FEATURE_CAS_POLICY
        }

#if FEATURE_CAS_POLICY
        [System.Security.SecurityCritical]  // auto-generated
        private void RunDomainManagerPostInitialization (AppDomainManager domainManager)
        {
            // force creation of the HostExecutionContextManager for the current AppDomain
            HostExecutionContextManager contextManager = domainManager.HostExecutionContextManager;

            if (IsLegacyCasPolicyEnabled)
            {
#pragma warning disable 618
                HostSecurityManager securityManager = domainManager.HostSecurityManager;
                if (securityManager != null)
                {
                    if ((securityManager.Flags & HostSecurityManagerOptions.HostPolicyLevel) == HostSecurityManagerOptions.HostPolicyLevel)
                    {
                        // set AppDomain policy if specified
                        PolicyLevel level = securityManager.DomainPolicy;
                        if (level != null)
                            SetAppDomainPolicy(level);
                    }
                }
#pragma warning restore 618
            }
        }
#endif 


#if FEATURE_CLICKONCE

        [System.Security.SecurityCritical]  // auto-generated
        private void SetupApplicationHelper (Evidence providedSecurityInfo, Evidence creatorsSecurityInfo, ApplicationIdentity appIdentity, ActivationContext activationContext, string[] activationData) {
            Contract.Requires(providedSecurityInfo != null);
            HostSecurityManager securityManager = AppDomain.CurrentDomain.HostSecurityManager;
            ApplicationTrust appTrust = securityManager.DetermineApplicationTrust(providedSecurityInfo, creatorsSecurityInfo, new TrustManagerContext());
            if (appTrust == null || !appTrust.IsApplicationTrustedToRun)
                throw new PolicyException(Environment.GetResourceString("Policy_NoExecutionPermission"),
                                          System.__HResults.CORSEC_E_NO_EXEC_PERM,
                                          null);

            // The application is trusted to run. Set up the AppDomain according to the manifests.
            if (activationContext != null)
                SetupDomainForApplication(activationContext, activationData);
            SetupDomainSecurityForApplication(appIdentity, appTrust);
        }

        [System.Security.SecurityCritical]  // auto-generated
        private void SetupDomainForApplication(ActivationContext activationContext, string[] activationData) {
            Contract.Requires(activationContext != null);
            if (IsDefaultAppDomain()) {
                // make the ActivationArguments available off the AppDomain object.
                AppDomainSetup adSetup = this.FusionStore;
                adSetup.ActivationArguments = new ActivationArguments(activationContext, activationData);

                // set the application base to point at where the application resides
                string entryPointPath = CmsUtils.GetEntryPointFullPath(activationContext);
                if (!String.IsNullOrEmpty(entryPointPath))
                    adSetup.SetupDefaults(entryPointPath);
                else
                    adSetup.ApplicationBase = activationContext.ApplicationDirectory;

                // update fusion context
                SetupFusionStore(adSetup, null);
            }

            // perform app data directory migration.
            activationContext.PrepareForExecution();
            activationContext.SetApplicationState(ActivationContext.ApplicationState.Starting);
            // set current app data directory.
            activationContext.SetApplicationState(ActivationContext.ApplicationState.Running);

            // make data directory path available.
            IPermission permission = null;
            string dataDirectory = activationContext.DataDirectory;
            if (dataDirectory != null && dataDirectory.Length > 0)
                permission = new FileIOPermission(FileIOPermissionAccess.PathDiscovery, dataDirectory);
            this.SetData("DataDirectory", dataDirectory, permission);

            _activationContext = activationContext;
        }

        [System.Security.SecurityCritical]  // auto-generated
        private void SetupDomainSecurityForApplication(ApplicationIdentity appIdentity,
                                                       ApplicationTrust appTrust)
        {
            // Set the Application trust on the managed side.
            _applicationIdentity = appIdentity;
            SetupDomainSecurityForHomogeneousDomain(appTrust, false);
        }
#endif // FEATURE_CLICKONCE

        [System.Security.SecurityCritical]  // auto-generated
        private void SetupDomainSecurityForHomogeneousDomain(ApplicationTrust appTrust,
                                                             bool runtimeSuppliedHomogenousGrantSet)
        {
            // If the CLR has supplied the homogenous grant set (that is, this domain would have been
            // heterogenous in v2.0), then we need to strip the ApplicationTrust from the AppDomainSetup of
            // the current domain.  This prevents code which does:
            //   AppDomain.CreateDomain(..., AppDomain.CurrentDomain.SetupInformation);
            // 
            // From looking like it is trying to create a homogenous domain intentionally, and therefore
            // having its evidence check bypassed.
            if (runtimeSuppliedHomogenousGrantSet)
            {
                BCLDebug.Assert(_FusionStore.ApplicationTrust != null, "Expected to find runtime supplied ApplicationTrust");
#if FEATURE_CAS_POLICY
                _FusionStore.ApplicationTrust = null;
#endif // FEATURE_CAS_POLICY
            }

            _applicationTrust = appTrust;

            // Set the homogeneous bit in the VM's ApplicationSecurityDescriptor.
            SetSecurityHomogeneousFlag(GetNativeHandle(),
                                       runtimeSuppliedHomogenousGrantSet);
        }

        // This method is called from CorHost2::ExecuteApplication to activate a ClickOnce application in the default AppDomain.
#if FEATURE_CLICKONCE
        [System.Security.SecuritySafeCritical]  // auto-generated
        private int ActivateApplication () {
            ObjectHandle oh = Activator.CreateInstance(AppDomain.CurrentDomain.ActivationContext);
            return (int) oh.Unwrap();
        }
#endif //FEATURE_CLICKONCE

        public AppDomainManager DomainManager {
            [System.Security.SecurityCritical]  // auto-generated_required
            get {
                return _domainManager;
            }
        }

#if FEATURE_CAS_POLICY
        internal HostSecurityManager HostSecurityManager {
            [System.Security.SecurityCritical]  // auto-generated
            get {
                HostSecurityManager securityManager = null;
                AppDomainManager domainManager = AppDomain.CurrentDomain.DomainManager;
                if (domainManager != null)
                    securityManager = domainManager.HostSecurityManager;

                if (securityManager == null)
                    securityManager = new HostSecurityManager();
                return securityManager;
            }
        }
#endif // FEATURE_CAS_POLICY
#if FEATURE_REFLECTION_ONLY_LOAD        
        private Assembly ResolveAssemblyForIntrospection(Object sender, ResolveEventArgs args)
        {
            Contract.Requires(args != null);
            return Assembly.ReflectionOnlyLoad(ApplyPolicy(args.Name));
        }
        
        // Helper class for method code:EnableResolveAssembliesForIntrospection
        private class NamespaceResolverForIntrospection
        {
            private IEnumerable<string> _packageGraphFilePaths;
            public NamespaceResolverForIntrospection(IEnumerable<string> packageGraphFilePaths)
            {
                _packageGraphFilePaths = packageGraphFilePaths;
            }
            
            [System.Security.SecurityCritical]
            public void ResolveNamespace(
                object sender, 
                System.Runtime.InteropServices.WindowsRuntime.NamespaceResolveEventArgs args)
            {
                Contract.Requires(args != null);
                
                IEnumerable<string> fileNames = System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMetadata.ResolveNamespace(
                    args.NamespaceName,
                    null,   // windowsSdkFilePath ... Use OS installed .winmd files
                    _packageGraphFilePaths);
                foreach (string fileName in fileNames)
                {
                    args.ResolvedAssemblies.Add(Assembly.ReflectionOnlyLoadFrom(fileName));
                }
            }
        }
        
        // Called only by native function code:ValidateWorker
        [System.Security.SecuritySafeCritical]
        private void EnableResolveAssembliesForIntrospection(string verifiedFileDirectory)
        {
            CurrentDomain.ReflectionOnlyAssemblyResolve += new ResolveEventHandler(ResolveAssemblyForIntrospection);
            
            string[] packageGraphFilePaths = null;
            if (verifiedFileDirectory != null)
                packageGraphFilePaths = new string[] { verifiedFileDirectory };
            NamespaceResolverForIntrospection namespaceResolver = new NamespaceResolverForIntrospection(packageGraphFilePaths);
            
            System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMetadata.ReflectionOnlyNamespaceResolve += 
                new EventHandler<System.Runtime.InteropServices.WindowsRuntime.NamespaceResolveEventArgs>(namespaceResolver.ResolveNamespace);
        }
#endif // FEATURE_REFLECTION_ONLY_LOAD


        /**********************************************
        * If an AssemblyName has a public key specified, the assembly is assumed
        * to have a strong name and a hash will be computed when the assembly
        * is saved.
        **********************************************/
        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public AssemblyBuilder DefineDynamicAssembly(
            AssemblyName            name,
            AssemblyBuilderAccess   access)
        {
            Contract.Ensures(Contract.Result<AssemblyBuilder>() != null);

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return InternalDefineDynamicAssembly(name, access, null,
                                                 null, null, null, null, ref stackMark, null, SecurityContextSource.CurrentAssembly);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public AssemblyBuilder DefineDynamicAssembly(
            AssemblyName            name,
            AssemblyBuilderAccess   access,
            IEnumerable<CustomAttributeBuilder> assemblyAttributes)
        {
            Contract.Ensures(Contract.Result<AssemblyBuilder>() != null);

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return InternalDefineDynamicAssembly(name,
                                                 access,
                                                 null, null, null, null, null,
                                                 ref stackMark,
                                                 assemblyAttributes, SecurityContextSource.CurrentAssembly);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Due to the stack crawl mark
        [SecuritySafeCritical]
        public AssemblyBuilder DefineDynamicAssembly(AssemblyName name,
                                                     AssemblyBuilderAccess access,
                                                     IEnumerable<CustomAttributeBuilder> assemblyAttributes,
                                                     SecurityContextSource securityContextSource)
        {
            Contract.Ensures(Contract.Result<AssemblyBuilder>() != null);

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return InternalDefineDynamicAssembly(name,
                                                 access,
                                                 null, null, null, null, null,
                                                 ref stackMark,
                                                 assemblyAttributes,
                                                 securityContextSource);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public AssemblyBuilder DefineDynamicAssembly(
            AssemblyName            name,
            AssemblyBuilderAccess   access,
            String                  dir)
        {
            Contract.Ensures(Contract.Result<AssemblyBuilder>() != null);

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return InternalDefineDynamicAssembly(name, access, dir,
                                                 null, null, null, null,
                                                 ref stackMark,
                                                 null,
                                                 SecurityContextSource.CurrentAssembly);
        }
    
        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        [Obsolete("Assembly level declarative security is obsolete and is no longer enforced by the CLR by default.  See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        public AssemblyBuilder DefineDynamicAssembly(
            AssemblyName            name,
            AssemblyBuilderAccess   access,
            Evidence                evidence)
        {
            Contract.Ensures(Contract.Result<AssemblyBuilder>() != null);

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return InternalDefineDynamicAssembly(name, access, null,
                                                 evidence, null, null, null,
                                                 ref stackMark,
                                                 null,
                                                 SecurityContextSource.CurrentAssembly);
        }
    
        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        [Obsolete("Assembly level declarative security is obsolete and is no longer enforced by the CLR by default.  See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        public AssemblyBuilder DefineDynamicAssembly(
            AssemblyName            name,
            AssemblyBuilderAccess   access,
            PermissionSet           requiredPermissions,
            PermissionSet           optionalPermissions,
            PermissionSet           refusedPermissions)
        {
            Contract.Ensures(Contract.Result<AssemblyBuilder>() != null);

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return InternalDefineDynamicAssembly(name, access, null, null,
                                                 requiredPermissions,
                                                 optionalPermissions,
                                                 refusedPermissions,
                                                 ref stackMark,
                                                 null,
                                                 SecurityContextSource.CurrentAssembly);
        }
    
        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        [Obsolete("Methods which use evidence to sandbox are obsolete and will be removed in a future release of the .NET Framework. Please use an overload of DefineDynamicAssembly which does not take an Evidence parameter. See http://go.microsoft.com/fwlink/?LinkId=155570 for more information.")]
        public AssemblyBuilder DefineDynamicAssembly(
            AssemblyName            name,
            AssemblyBuilderAccess   access,
            String                  dir,
            Evidence                evidence)
        {
            Contract.Ensures(Contract.Result<AssemblyBuilder>() != null);

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return InternalDefineDynamicAssembly(name, access, dir, evidence,
                                                 null, null, null, ref stackMark, null, SecurityContextSource.CurrentAssembly);
        }
    
        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        [Obsolete("Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        public AssemblyBuilder DefineDynamicAssembly(
            AssemblyName            name,
            AssemblyBuilderAccess   access,
            String                  dir,
            PermissionSet           requiredPermissions,
            PermissionSet           optionalPermissions,
            PermissionSet           refusedPermissions)
        {
            Contract.Ensures(Contract.Result<AssemblyBuilder>() != null);

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return InternalDefineDynamicAssembly(name, access, dir, null,
                                                 requiredPermissions,
                                                 optionalPermissions,
                                                 refusedPermissions,
                                                 ref stackMark,
                                                 null,
                                                 SecurityContextSource.CurrentAssembly);
        }
    
        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        [Obsolete("Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        public AssemblyBuilder DefineDynamicAssembly(
            AssemblyName            name,
            AssemblyBuilderAccess   access,
            Evidence                evidence,
            PermissionSet           requiredPermissions,
            PermissionSet           optionalPermissions,
            PermissionSet           refusedPermissions)
        {
            Contract.Ensures(Contract.Result<AssemblyBuilder>() != null);

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return InternalDefineDynamicAssembly(name, access, null,
                                                 evidence,
                                                 requiredPermissions,
                                                 optionalPermissions,
                                                 refusedPermissions,
                                                 ref stackMark,
                                                 null,
                                                 SecurityContextSource.CurrentAssembly);
        }
    
        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        [Obsolete("Assembly level declarative security is obsolete and is no longer enforced by the CLR by default.  Please see http://go.microsoft.com/fwlink/?LinkId=155570 for more information.")]
        public AssemblyBuilder DefineDynamicAssembly(
            AssemblyName            name,
            AssemblyBuilderAccess   access,
            String                  dir,
            Evidence                evidence,
            PermissionSet           requiredPermissions,
            PermissionSet           optionalPermissions,
            PermissionSet           refusedPermissions)
        {
            Contract.Ensures(Contract.Result<AssemblyBuilder>() != null);

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return InternalDefineDynamicAssembly(name, access, dir,
                                                 evidence,
                                                 requiredPermissions,
                                                 optionalPermissions,
                                                 refusedPermissions,
                                                 ref stackMark,
                                                 null,
                                                 SecurityContextSource.CurrentAssembly);
        }


        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        [Obsolete("Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        public AssemblyBuilder DefineDynamicAssembly(
            AssemblyName            name,
            AssemblyBuilderAccess   access,
            String                  dir,
            Evidence                evidence,
            PermissionSet           requiredPermissions,
            PermissionSet           optionalPermissions,
            PermissionSet           refusedPermissions,
            bool                    isSynchronized)
        {
            Contract.Ensures(Contract.Result<AssemblyBuilder>() != null);

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return InternalDefineDynamicAssembly(name,
                                                 access,
                                                 dir,
                                                 evidence,
                                                 requiredPermissions,
                                                 optionalPermissions,
                                                 refusedPermissions,
                                                 ref stackMark,
                                                 null,
                                                 SecurityContextSource.CurrentAssembly);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        [Obsolete("Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        public AssemblyBuilder DefineDynamicAssembly(
                    AssemblyName name,
                    AssemblyBuilderAccess access,
                    String dir,
                    Evidence evidence,
                    PermissionSet requiredPermissions,
                    PermissionSet optionalPermissions,
                    PermissionSet refusedPermissions,
                    bool isSynchronized,
                    IEnumerable<CustomAttributeBuilder> assemblyAttributes)
        {
            Contract.Ensures(Contract.Result<AssemblyBuilder>() != null);

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return InternalDefineDynamicAssembly(name,
                                                 access,
                                                 dir,
                                                 evidence,
                                                 requiredPermissions,
                                                 optionalPermissions,
                                                 refusedPermissions,
                                                 ref stackMark,
                                                 assemblyAttributes,
                                                 SecurityContextSource.CurrentAssembly);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public AssemblyBuilder DefineDynamicAssembly(
                    AssemblyName name,
                    AssemblyBuilderAccess access,
                    String dir,
                    bool isSynchronized,
                    IEnumerable<CustomAttributeBuilder> assemblyAttributes)
        {
            Contract.Ensures(Contract.Result<AssemblyBuilder>() != null);

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return InternalDefineDynamicAssembly(name,
                                                 access,
                                                 dir,
                                                 null,
                                                 null,
                                                 null,
                                                 null,
                                                 ref stackMark,
                                                 assemblyAttributes,
                                                 SecurityContextSource.CurrentAssembly);
        }

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        private AssemblyBuilder InternalDefineDynamicAssembly(
            AssemblyName name,
            AssemblyBuilderAccess access,
            String dir,
            Evidence evidence,
            PermissionSet requiredPermissions,
            PermissionSet optionalPermissions,
            PermissionSet refusedPermissions,
            ref StackCrawlMark stackMark,
            IEnumerable<CustomAttributeBuilder> assemblyAttributes,
            SecurityContextSource securityContextSource)
        {
            return AssemblyBuilder.InternalDefineDynamicAssembly(name,
                                                                 access,
                                                                 dir,
                                                                 evidence,
                                                                 requiredPermissions,
                                                                 optionalPermissions,
                                                                 refusedPermissions,
                                                                 ref stackMark,
                                                                 assemblyAttributes,
                                                                 securityContextSource);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern String nApplyPolicy(AssemblyName an);
       
        // Return the assembly name that results from applying policy.
        [ComVisible(false)]
        public String ApplyPolicy(String assemblyName)
        {
            AssemblyName asmName = new AssemblyName(assemblyName);  

            byte[] pk = asmName.GetPublicKeyToken();
            if (pk == null)
                pk = asmName.GetPublicKey();

            // Simply-named assemblies cannot have policy, so for those,
            // we simply return the passed-in assembly name.
            if ((pk == null) || (pk.Length == 0))
                return assemblyName;
            else
                return nApplyPolicy(asmName);
        }


        public ObjectHandle CreateInstance(String assemblyName,
                                           String typeName)
                                         
        {
            // jit does not check for that, so we should do it ...
            if (this == null)
                throw new NullReferenceException();

            if (assemblyName == null)
                throw new ArgumentNullException("assemblyName");
            Contract.EndContractBlock();

            return Activator.CreateInstance(assemblyName,
                                            typeName);
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal ObjectHandle InternalCreateInstanceWithNoSecurity (string assemblyName, string typeName) {
            PermissionSet.s_fullTrust.Assert();
            return CreateInstance(assemblyName, typeName);
        }

        public ObjectHandle CreateInstanceFrom(String assemblyFile,
                                               String typeName)
                                         
        {
            // jit does not check for that, so we should do it ...
            if (this == null)
                throw new NullReferenceException();
            Contract.EndContractBlock();

            return Activator.CreateInstanceFrom(assemblyFile,
                                                typeName);
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal ObjectHandle InternalCreateInstanceFromWithNoSecurity (string assemblyName, string typeName) {
            PermissionSet.s_fullTrust.Assert();
            return CreateInstanceFrom(assemblyName, typeName);
        }

#if FEATURE_COMINTEROP
        // The first parameter should be named assemblyFile, but it was incorrectly named in a previous 
        //  release, and the compatibility police won't let us change the name now.
        public ObjectHandle CreateComInstanceFrom(String assemblyName,
                                                  String typeName)
                                         
        {
            if (this == null)
                throw new NullReferenceException();
            Contract.EndContractBlock();

            return Activator.CreateComInstanceFrom(assemblyName,
                                                   typeName);
        }

        public ObjectHandle CreateComInstanceFrom(String assemblyFile,
                                                  String typeName,
                                                  byte[] hashValue, 
                                                  AssemblyHashAlgorithm hashAlgorithm)
                                         
        {
            if (this == null)
                throw new NullReferenceException();
            Contract.EndContractBlock();

            return Activator.CreateComInstanceFrom(assemblyFile,
                                                   typeName,
                                                   hashValue, 
                                                   hashAlgorithm);
        }

#endif // FEATURE_COMINTEROP

        public ObjectHandle CreateInstance(String assemblyName,
                                           String typeName,
                                           Object[] activationAttributes)
                                         
        {
            // jit does not check for that, so we should do it ...
            if (this == null)
                throw new NullReferenceException();

            if (assemblyName == null)
                throw new ArgumentNullException("assemblyName");
            Contract.EndContractBlock();

            return Activator.CreateInstance(assemblyName,
                                            typeName,
                                            activationAttributes);
        }
                                  
        public ObjectHandle CreateInstanceFrom(String assemblyFile,
                                               String typeName,
                                               Object[] activationAttributes)
                                               
        {
            // jit does not check for that, so we should do it ...
            if (this == null)
                throw new NullReferenceException();
            Contract.EndContractBlock();

            return Activator.CreateInstanceFrom(assemblyFile,
                                                typeName,
                                                activationAttributes);
        }
                                         
        [Obsolete("Methods which use evidence to sandbox are obsolete and will be removed in a future release of the .NET Framework. Please use an overload of CreateInstance which does not take an Evidence parameter. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        public ObjectHandle CreateInstance(String assemblyName, 
                                           String typeName, 
                                           bool ignoreCase,
                                           BindingFlags bindingAttr, 
                                           Binder binder,
                                           Object[] args,
                                           CultureInfo culture,
                                           Object[] activationAttributes,
                                           Evidence securityAttributes)
        {
            // jit does not check for that, so we should do it ...
            if (this == null)
                throw new NullReferenceException();
            
            if (assemblyName == null)
                throw new ArgumentNullException("assemblyName");
            Contract.EndContractBlock();

#if FEATURE_CAS_POLICY
            if (securityAttributes != null && !IsLegacyCasPolicyEnabled)
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_RequiresCasPolicyImplicit"));   
            }
#endif // FEATURE_CAS_POLICY

#pragma warning disable 618
            return Activator.CreateInstance(assemblyName,
                                            typeName,
                                            ignoreCase,
                                            bindingAttr,
                                            binder,
                                            args,
                                            culture,
                                            activationAttributes,
                                            securityAttributes);
#pragma warning restore 618
        }

        public ObjectHandle CreateInstance(string assemblyName,
                                           string typeName,
                                           bool ignoreCase,
                                           BindingFlags bindingAttr,
                                           Binder binder,
                                           object[] args,
                                           CultureInfo culture,
                                           object[] activationAttributes)
        {
            // jit does not check for that, so we should do it ...
            if (this == null)
                throw new NullReferenceException();

            if (assemblyName == null)
                throw new ArgumentNullException("assemblyName");
            Contract.EndContractBlock();

            return Activator.CreateInstance(assemblyName,
                                            typeName,
                                            ignoreCase,
                                            bindingAttr,
                                            binder,
                                            args,
                                            culture,
                                            activationAttributes);
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal ObjectHandle InternalCreateInstanceWithNoSecurity (string assemblyName, 
                                                                    string typeName,
                                                                    bool ignoreCase,
                                                                    BindingFlags bindingAttr,
                                                                    Binder binder,
                                                                    Object[] args,
                                                                    CultureInfo culture,
                                                                    Object[] activationAttributes,
                                                                    Evidence securityAttributes)
        {
#if FEATURE_CAS_POLICY
            Contract.Assert(IsLegacyCasPolicyEnabled || securityAttributes == null);
#endif // FEATURE_CAS_POLICY

            PermissionSet.s_fullTrust.Assert();
#pragma warning disable 618
            return CreateInstance(assemblyName, typeName, ignoreCase, bindingAttr, binder, args, culture, activationAttributes, securityAttributes);
#pragma warning restore 618
        }

        [Obsolete("Methods which use evidence to sandbox are obsolete and will be removed in a future release of the .NET Framework. Please use an overload of CreateInstanceFrom which does not take an Evidence parameter. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        public ObjectHandle CreateInstanceFrom(String assemblyFile,
                                               String typeName, 
                                               bool ignoreCase,
                                               BindingFlags bindingAttr, 
                                               Binder binder,
                                               Object[] args,
                                               CultureInfo culture,
                                               Object[] activationAttributes,
                                               Evidence securityAttributes)

        {
            // jit does not check for that, so we should do it ...
            if (this == null)
                throw new NullReferenceException();
            Contract.EndContractBlock();

#if FEATURE_CAS_POLICY
            if (securityAttributes != null && !IsLegacyCasPolicyEnabled)
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_RequiresCasPolicyImplicit"));
            }
#endif // FEATURE_CAS_POLICY

            return Activator.CreateInstanceFrom(assemblyFile,
                                                typeName,
                                                ignoreCase,
                                                bindingAttr,
                                                binder,
                                                args,
                                                culture,
                                                activationAttributes,
                                                securityAttributes);
        }

        public ObjectHandle CreateInstanceFrom(string assemblyFile,
                                               string typeName,
                                               bool ignoreCase,
                                               BindingFlags bindingAttr,
                                               Binder binder,
                                               object[] args,
                                               CultureInfo culture,
                                               object[] activationAttributes)
        {
            // jit does not check for that, so we should do it ...
            if (this == null)
                throw new NullReferenceException();
            Contract.EndContractBlock();

            return Activator.CreateInstanceFrom(assemblyFile,
                                                typeName,
                                                ignoreCase,
                                                bindingAttr,
                                                binder,
                                                args,
                                                culture,
                                                activationAttributes);
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal ObjectHandle InternalCreateInstanceFromWithNoSecurity (string assemblyName, 
                                                                        string typeName,
                                                                        bool ignoreCase,
                                                                        BindingFlags bindingAttr,
                                                                        Binder binder,
                                                                        Object[] args,
                                                                        CultureInfo culture,
                                                                        Object[] activationAttributes,
                                                                        Evidence securityAttributes)
        {
#if FEATURE_CAS_POLICY
            Contract.Assert(IsLegacyCasPolicyEnabled || securityAttributes == null);
#endif // FEATURE_CAS_POLICY

            PermissionSet.s_fullTrust.Assert();
#pragma warning disable 618
            return CreateInstanceFrom(assemblyName, typeName, ignoreCase, bindingAttr, binder, args, culture, activationAttributes, securityAttributes);
#pragma warning restore 618
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public Assembly Load(AssemblyName assemblyRef)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeAssembly.InternalLoadAssemblyName(assemblyRef, null, null, ref stackMark, true /*thrownOnFileNotFound*/, false, false);
        }
        
        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public Assembly Load(String assemblyString)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeAssembly.InternalLoad(assemblyString, null, ref stackMark, false);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public Assembly Load(byte[] rawAssembly)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeAssembly.nLoadImage(rawAssembly,
                                       null, // symbol store
                                       null, // evidence
                                       ref stackMark,
                                       false,
                                       SecurityContextSource.CurrentAssembly);

        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public Assembly Load(byte[] rawAssembly,
                             byte[] rawSymbolStore)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeAssembly.nLoadImage(rawAssembly,
                                       rawSymbolStore,
                                       null, // evidence
                                       ref stackMark,
                                       false, // fIntrospection
                                       SecurityContextSource.CurrentAssembly);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
#pragma warning disable 618
        [SecurityPermissionAttribute(SecurityAction.Demand, ControlEvidence = true)]
#pragma warning restore 618
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        [Obsolete("Methods which use evidence to sandbox are obsolete and will be removed in a future release of the .NET Framework. Please use an overload of Load which does not take an Evidence parameter. See http://go.microsoft.com/fwlink/?LinkId=155570 for more information.")]
        public Assembly Load(byte[] rawAssembly,
                             byte[] rawSymbolStore,
                             Evidence securityEvidence)
        {
#if FEATURE_CAS_POLICY
            if (securityEvidence != null && !IsLegacyCasPolicyEnabled)
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_RequiresCasPolicyImplicit"));
            }
#endif // FEATURE_CAS_POLICY

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeAssembly.nLoadImage(rawAssembly,
                                       rawSymbolStore,
                                       securityEvidence,
                                       ref stackMark,
                                       false, // fIntrospection
                                       SecurityContextSource.CurrentAssembly);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        [Obsolete("Methods which use evidence to sandbox are obsolete and will be removed in a future release of the .NET Framework. Please use an overload of Load which does not take an Evidence parameter. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        public Assembly Load(AssemblyName assemblyRef,
                             Evidence assemblySecurity)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeAssembly.InternalLoadAssemblyName(assemblyRef, assemblySecurity, null, ref stackMark, true /*thrownOnFileNotFound*/, false, false);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        [Obsolete("Methods which use evidence to sandbox are obsolete and will be removed in a future release of the .NET Framework. Please use an overload of Load which does not take an Evidence parameter. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        public Assembly Load(String assemblyString,
                             Evidence assemblySecurity)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeAssembly.InternalLoad(assemblyString, assemblySecurity, ref stackMark, false);
        }

        public int ExecuteAssembly(String assemblyFile)
        {
            return ExecuteAssembly(assemblyFile, (string[])null);
        }

        [Obsolete("Methods which use evidence to sandbox are obsolete and will be removed in a future release of the .NET Framework. Please use an overload of ExecuteAssembly which does not take an Evidence parameter. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        public int ExecuteAssembly(String assemblyFile,
                                   Evidence assemblySecurity)
        {
            return ExecuteAssembly(assemblyFile, assemblySecurity, null);
        }
    
        [Obsolete("Methods which use evidence to sandbox are obsolete and will be removed in a future release of the .NET Framework. Please use an overload of ExecuteAssembly which does not take an Evidence parameter. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        public int ExecuteAssembly(String assemblyFile,
                                   Evidence assemblySecurity,
                                   String[] args)
        {
#if FEATURE_CAS_POLICY
            if (assemblySecurity != null && !IsLegacyCasPolicyEnabled)
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_RequiresCasPolicyImplicit"));
            }
#endif // FEATURE_CAS_POLICY

            RuntimeAssembly assembly = (RuntimeAssembly)Assembly.LoadFrom(assemblyFile, assemblySecurity);
    
            if (args == null)
                args = new String[0];

            return nExecuteAssembly(assembly, args);
        }

        public int ExecuteAssembly(string assemblyFile, string[] args)
        {
            RuntimeAssembly assembly = (RuntimeAssembly)Assembly.LoadFrom(assemblyFile);

            if (args == null)
                args = new String[0];

            return nExecuteAssembly(assembly, args);
        }

        [Obsolete("Methods which use evidence to sandbox are obsolete and will be removed in a future release of the .NET Framework. Please use an overload of ExecuteAssembly which does not take an Evidence parameter. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        public int ExecuteAssembly(String assemblyFile,
                                   Evidence assemblySecurity,
                                   String[] args,
                                   byte[] hashValue, 
                                   AssemblyHashAlgorithm hashAlgorithm)
        {
#if FEATURE_CAS_POLICY
            if (assemblySecurity != null && !IsLegacyCasPolicyEnabled)
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_RequiresCasPolicyImplicit"));
            }
#endif // FEATURE_CAS_POLICY

            RuntimeAssembly assembly = (RuntimeAssembly)Assembly.LoadFrom(assemblyFile, 
                                                                          assemblySecurity,
                                                                          hashValue,
                                                                          hashAlgorithm);
            if (args == null)
                args = new String[0];

            return nExecuteAssembly(assembly, args);
        }

        public int ExecuteAssembly(string assemblyFile,
                                   string[] args,
                                   byte[] hashValue,
                                   AssemblyHashAlgorithm hashAlgorithm)
        {
            RuntimeAssembly assembly = (RuntimeAssembly)Assembly.LoadFrom(assemblyFile,
                                                                          hashValue,
                                                                          hashAlgorithm);
            if (args == null)
                args = new String[0];

            return nExecuteAssembly(assembly, args);
        }

        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        public int ExecuteAssemblyByName(String assemblyName)
        {
            return ExecuteAssemblyByName(assemblyName, (string[])null);
        }

        [Obsolete("Methods which use evidence to sandbox are obsolete and will be removed in a future release of the .NET Framework. Please use an overload of ExecuteAssemblyByName which does not take an Evidence parameter. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        public int ExecuteAssemblyByName(String assemblyName,
                                         Evidence assemblySecurity)
        {
#pragma warning disable 618
            return ExecuteAssemblyByName(assemblyName, assemblySecurity, null);
#pragma warning restore 618
        }

        [Obsolete("Methods which use evidence to sandbox are obsolete and will be removed in a future release of the .NET Framework. Please use an overload of ExecuteAssemblyByName which does not take an Evidence parameter. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        public int ExecuteAssemblyByName(String assemblyName,
                                         Evidence assemblySecurity,
                                         params String[] args)
        {
#if FEATURE_CAS_POLICY
            if (assemblySecurity != null && !IsLegacyCasPolicyEnabled)
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_RequiresCasPolicyImplicit"));
            }
#endif // FEATURE_CAS_POLICY

            RuntimeAssembly assembly = (RuntimeAssembly)Assembly.Load(assemblyName, assemblySecurity);
    
            if (args == null)
                args = new String[0];

            return nExecuteAssembly(assembly, args);
        }

        public int ExecuteAssemblyByName(string assemblyName, params string[] args)
        {
            RuntimeAssembly assembly = (RuntimeAssembly)Assembly.Load(assemblyName);

            if (args == null)
                args = new String[0];

            return nExecuteAssembly(assembly, args);
        }

        [Obsolete("Methods which use evidence to sandbox are obsolete and will be removed in a future release of the .NET Framework. Please use an overload of ExecuteAssemblyByName which does not take an Evidence parameter. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        public int ExecuteAssemblyByName(AssemblyName assemblyName,
                                         Evidence assemblySecurity,
                                         params String[] args)
        {
#if FEATURE_CAS_POLICY
            if (assemblySecurity != null && !IsLegacyCasPolicyEnabled)
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_RequiresCasPolicyImplicit"));
            }
#endif // FEATURE_CAS_POLICY

            RuntimeAssembly assembly = (RuntimeAssembly)Assembly.Load(assemblyName, assemblySecurity);
    
            if (args == null)
                args = new String[0];

            return nExecuteAssembly(assembly, args);
        }

        public int ExecuteAssemblyByName(AssemblyName assemblyName, params string[] args)
        {
            RuntimeAssembly assembly = (RuntimeAssembly)Assembly.Load(assemblyName);

            if (args == null)
                args = new String[0];

            return nExecuteAssembly(assembly, args);
        }

        public static AppDomain CurrentDomain
        {
            get {
                Contract.Ensures(Contract.Result<AppDomain>() != null);
                return Thread.GetDomain();
            }
        }

#if FEATURE_CAS_POLICY
        public Evidence Evidence
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            [SecurityPermissionAttribute(SecurityAction.Demand, ControlEvidence = true)]
            get {
                return EvidenceNoDemand;
            }
        }

        internal Evidence EvidenceNoDemand {
            [SecurityCritical]
            get {
                if (_SecurityIdentity == null) {
                    if (!IsDefaultAppDomain() && nIsDefaultAppDomainForEvidence()) {
#if !FEATURE_CORECLR                         
                        //
                        // V1.x compatibility: If this is an AppDomain created 
                        // by the default appdomain without an explicit evidence
                        // then reuse the evidence of the default AppDomain.
                        //
                        return GetDefaultDomain().Evidence;
#else
                       Contract.Assert(false,"This code should not be called for core CLR");
                        
                        // This operation is not allowed
                        throw new InvalidOperationException();
#endif
                    }
                    else {
                        // We can't cache this value, since the VM needs to differentiate between AppDomains
                        // which have no user supplied evidence and those which do and it uses the presence
                        // of Evidence on the domain to make that switch.
                        return new Evidence(new AppDomainEvidenceFactory(this));
                    }
                }
                else {
                    return _SecurityIdentity.Clone();
                }
            }
        }

        internal Evidence InternalEvidence
        {
            get {
                    return _SecurityIdentity;
            }
        }

        internal EvidenceBase GetHostEvidence(Type type)
        {
            if (_SecurityIdentity != null)
            {
                return _SecurityIdentity.GetHostEvidence(type);
            }
            else
            {
                return new Evidence(new AppDomainEvidenceFactory(this)).GetHostEvidence(type);
            }
        }
#endif // FEATURE_CAS_POLICY

        public String FriendlyName
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get { return nGetFriendlyName(); }
        } 

        public String BaseDirectory
        {
#if FEATURE_CORECLR
            [System.Security.SecurityCritical]
#endif        
            get {
                return FusionStore.ApplicationBase;
            }
        }

#if FEATURE_FUSION
        public String RelativeSearchPath
        {
            get { return FusionStore.PrivateBinPath; }
        }

        public bool ShadowCopyFiles
        {
            get {
                String s = FusionStore.ShadowCopyFiles;
                if((s != null) && 
                   (String.Compare(s, "true", StringComparison.OrdinalIgnoreCase) == 0))
                    return true;
                else
                    return false;
            }
        }
#endif

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override String ToString()
        {
            StringBuilder sb = StringBuilderCache.Acquire();
            
            String fn = nGetFriendlyName();
            if (fn != null) {
                sb.Append(Environment.GetResourceString("Loader_Name") + fn);
                sb.Append(Environment.NewLine);
            }
    
            if(_Policies == null || _Policies.Length == 0) 
                sb.Append(Environment.GetResourceString("Loader_NoContextPolicies")
                          + Environment.NewLine);
            else {
                sb.Append(Environment.GetResourceString("Loader_ContextPolicies")
                          + Environment.NewLine);
                for(int i = 0;i < _Policies.Length; i++) {
                    sb.Append(_Policies[i]);
                    sb.Append(Environment.NewLine);
                }
            }
    
            return StringBuilderCache.GetStringAndRelease(sb);
        }
        
        public Assembly[] GetAssemblies()
        {
            return nGetAssemblies(false /* forIntrospection */);
        }


        public Assembly[] ReflectionOnlyGetAssemblies()
        {
            return nGetAssemblies(true /* forIntrospection */);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern Assembly[] nGetAssemblies(bool forIntrospection);

        // this is true when we've removed the handles etc so really can't do anything
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern bool IsUnloadingForcedFinalize();

    // this is true when we've just started going through the finalizers and are forcing objects to finalize
    // so must be aware that certain infrastructure may have gone away
        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public extern bool IsFinalizingForUnload();

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void PublishAnonymouslyHostedDynamicMethodsAssembly(RuntimeAssembly assemblyHandle);

#if  FEATURE_FUSION
        // Appends the following string to the private path. Valid paths
        // are of the form "bin;util/i386" etc.
        [System.Security.SecurityCritical]  // auto-generated_required
        [Obsolete("AppDomain.AppendPrivatePath has been deprecated. Please investigate the use of AppDomainSetup.PrivateBinPath instead. http://go.microsoft.com/fwlink/?linkid=14202")]
        public void AppendPrivatePath(String path)
        {
            if(path == null || path.Length == 0)
                return;

            String current = FusionStore.Value[(int) AppDomainSetup.LoaderInformation.PrivateBinPathValue];
            StringBuilder appendPath = StringBuilderCache.Acquire();

            if(current != null && current.Length > 0) {
                // See if the last character is a separator
                appendPath.Append(current);
                if((current[current.Length-1] != Path.PathSeparator) &&
                   (path[0] != Path.PathSeparator))
                    appendPath.Append(Path.PathSeparator);
            }
            appendPath.Append(path);

            String result = StringBuilderCache.GetStringAndRelease(appendPath);
            InternalSetPrivateBinPath(result);
        }

        
        [System.Security.SecurityCritical]  // auto-generated_required
        [Obsolete("AppDomain.ClearPrivatePath has been deprecated. Please investigate the use of AppDomainSetup.PrivateBinPath instead. http://go.microsoft.com/fwlink/?linkid=14202")]
        public void ClearPrivatePath()
        {
            InternalSetPrivateBinPath(String.Empty);
        }

        [System.Security.SecurityCritical]  // auto-generated_required
        [Obsolete("AppDomain.ClearShadowCopyPath has been deprecated. Please investigate the use of AppDomainSetup.ShadowCopyDirectories instead. http://go.microsoft.com/fwlink/?linkid=14202")]
        public void ClearShadowCopyPath()
        {
            InternalSetShadowCopyPath(String.Empty);
        }

        [System.Security.SecurityCritical]  // auto-generated_required
        [Obsolete("AppDomain.SetCachePath has been deprecated. Please investigate the use of AppDomainSetup.CachePath instead. http://go.microsoft.com/fwlink/?linkid=14202")]
        public void SetCachePath(String path)
        {
            InternalSetCachePath(path);
        }
#endif // FEATURE_FUSION

        [System.Security.SecurityCritical]  // auto-generated_required
        public void SetData (string name, object data) {
#if FEATURE_CORECLR        
            if (!name.Equals("LOCATION_URI"))
            {
                // Only LOCATION_URI can be set using AppDomain.SetData
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_SetData_OnlyLocationURI", name));
            }
#endif // FEATURE_CORECLR            
            SetDataHelper(name, data, null);
        }

        [System.Security.SecurityCritical]  // auto-generated_required
        public void SetData (string name, object data, IPermission permission) {
#if FEATURE_CORECLR        
            if (!name.Equals("LOCATION_URI"))
            {
                // Only LOCATION_URI can be set using AppDomain.SetData
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_SetData_OnlyLocationURI", name));
            }
#endif // FEATURE_CORECLR                    
            SetDataHelper(name, data, permission);
        }

        [System.Security.SecurityCritical]  // auto-generated
        private void SetDataHelper (string name, object data, IPermission permission) {
            if (name == null)
                throw new ArgumentNullException("name");
            Contract.EndContractBlock();

            //
            // Synopsis:
            //   IgnoreSystemPolicy is provided as a legacy flag to allow callers to
            //   skip enterprise, machine and user policy levels. When this flag is set,
            //   any demands triggered in this AppDomain will be evaluated against the
            //   AppDomain CAS policy level that is set on the AppDomain.
            // Security Requirements:
            //   The caller needs to be fully trusted in order to be able to set
            //   this legacy mode.
            // Remarks:
            //   There needs to be an AppDomain policy level set before this compat
            //   switch can be set on the AppDomain.
            //
#if FEATURE_FUSION
            if (name.Equals(TargetFrameworkNameAppCompatSetting)) {
                _FusionStore.TargetFrameworkName = (String) data;
                return;
            }
#if FEATURE_CAS_POLICY
            if (name.Equals("IgnoreSystemPolicy")) {
                lock (this) {
                    if (!_HasSetPolicy)
                        throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_SetData"));
                }
                new PermissionSet(PermissionState.Unrestricted).Demand();
            }
#endif
            int key = AppDomainSetup.Locate(name);

            if(key == -1) {
                lock (((ICollection)LocalStore).SyncRoot) {
                    LocalStore[name] = new object[] {data, permission};
                }
            }
            else {
                if (permission != null)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_SetData"));
                // Be sure to call these properties, not Value, since
                // these do more than call Value.
                switch(key) {
                case (int) AppDomainSetup.LoaderInformation.DynamicBaseValue:
                    FusionStore.DynamicBase = (string) data;
                    break;
                case (int) AppDomainSetup.LoaderInformation.DevPathValue:
                    FusionStore.DeveloperPath = (string) data;
                    break;
                case (int) AppDomainSetup.LoaderInformation.ShadowCopyDirectoriesValue:
                    FusionStore.ShadowCopyDirectories = (string) data;
                    break;
                case (int) AppDomainSetup.LoaderInformation.DisallowPublisherPolicyValue:
                    if(data != null)
                        FusionStore.DisallowPublisherPolicy = true;
                    else
                        FusionStore.DisallowPublisherPolicy = false;
                    break;
                case (int) AppDomainSetup.LoaderInformation.DisallowCodeDownloadValue:
                    if (data != null)
                         FusionStore.DisallowCodeDownload = true;
                    else
                        FusionStore.DisallowCodeDownload = false;
                    break;
                case (int) AppDomainSetup.LoaderInformation.DisallowBindingRedirectsValue:
                    if(data != null)
                        FusionStore.DisallowBindingRedirects = true;
                    else
                        FusionStore.DisallowBindingRedirects = false;
                    break;
                case (int) AppDomainSetup.LoaderInformation.DisallowAppBaseProbingValue:
                    if(data != null)
                        FusionStore.DisallowApplicationBaseProbing = true;
                    else
                        FusionStore.DisallowApplicationBaseProbing = false;
                    break;
                case (int) AppDomainSetup.LoaderInformation.ConfigurationBytesValue:
                    FusionStore.SetConfigurationBytes((byte[]) data);
                    break;
                default:
                    FusionStore.Value[key] = (string) data;
                    break;
                }
            }
#else // FEATURE_FUSION
#if FEATURE_CORECLR
            // SetData should only be used to set values that don't already exist.
            {
                object[] currentVal;
                lock (((ICollection)LocalStore).SyncRoot) {
                    LocalStore.TryGetValue(name, out currentVal);
                }
                if (currentVal != null && currentVal[0] != null)
                {
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_SetData_OnlyOnce"));
                }
            }

#endif // FEATURE_CORECLR
            lock (((ICollection)LocalStore).SyncRoot) {
                LocalStore[name] = new object[] {data, permission};
            }
#endif // FEATURE_FUSION
        }

        [Pure]
        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #else
        [System.Security.SecuritySafeCritical]
        #endif
        public Object GetData(string name)
        {
            if(name == null)
                throw new ArgumentNullException("name");
            Contract.EndContractBlock();

            int key = AppDomainSetup.Locate(name);
            if(key == -1) 
            {
#if FEATURE_LOADER_OPTIMIZATION
                if(name.Equals(AppDomainSetup.LoaderOptimizationKey))
                    return FusionStore.LoaderOptimization;
                else 
#endif // FEATURE_LOADER_OPTIMIZATION                    
                {
                    object[] data;
                    lock (((ICollection)LocalStore).SyncRoot) {
                        LocalStore.TryGetValue(name, out data);
                    }
                    if (data == null)
                        return null;
                    if (data[1] != null) {
                        IPermission permission = (IPermission) data[1];
                        permission.Demand();
                    }
                    return data[0];
                }
            }
           else {
                // Be sure to call these properties, not Value, so
                // that the appropriate permission demand will be done
                switch(key) {
                case (int) AppDomainSetup.LoaderInformation.ApplicationBaseValue:
                    return FusionStore.ApplicationBase;
                case (int) AppDomainSetup.LoaderInformation.ApplicationNameValue:
                    return FusionStore.ApplicationName;
#if FEATURE_FUSION
                case (int) AppDomainSetup.LoaderInformation.ConfigurationFileValue:
                    return FusionStore.ConfigurationFile;
                case (int) AppDomainSetup.LoaderInformation.DynamicBaseValue:
                    return FusionStore.DynamicBase;
                case (int) AppDomainSetup.LoaderInformation.DevPathValue:
                    return FusionStore.DeveloperPath;
                case (int) AppDomainSetup.LoaderInformation.PrivateBinPathValue:
                    return FusionStore.PrivateBinPath;
                case (int) AppDomainSetup.LoaderInformation.PrivateBinPathProbeValue:
                    return FusionStore.PrivateBinPathProbe;
                case (int) AppDomainSetup.LoaderInformation.ShadowCopyDirectoriesValue:
                    return FusionStore.ShadowCopyDirectories;
                case (int) AppDomainSetup.LoaderInformation.ShadowCopyFilesValue:
                    return FusionStore.ShadowCopyFiles;
                case (int) AppDomainSetup.LoaderInformation.CachePathValue:
                    return FusionStore.CachePath;
                case (int) AppDomainSetup.LoaderInformation.LicenseFileValue:
                    return FusionStore.LicenseFile;
                case (int) AppDomainSetup.LoaderInformation.DisallowPublisherPolicyValue:
                    return FusionStore.DisallowPublisherPolicy;
                case (int) AppDomainSetup.LoaderInformation.DisallowCodeDownloadValue:
                    return FusionStore.DisallowCodeDownload;
                case (int) AppDomainSetup.LoaderInformation.DisallowBindingRedirectsValue:
                    return FusionStore.DisallowBindingRedirects;
                case (int) AppDomainSetup.LoaderInformation.DisallowAppBaseProbingValue:
                    return FusionStore.DisallowApplicationBaseProbing;
                case (int) AppDomainSetup.LoaderInformation.ConfigurationBytesValue:
                    return FusionStore.GetConfigurationBytes();
#endif //FEATURE_FUSION
                    
                default:
                    Contract.Assert(false, "Need to handle new LoaderInformation value in AppDomain.GetData()");
                    return null;
                }
            }
                   
        }

        // The compat flags are set at domain creation time to indicate that the given breaking
        // change should not be used in this domain.
        //
        // After the domain has been created, this Nullable boolean returned by this method should
        // always have a value.  Code in the runtime uses this to know if it is safe to cache values
        // that might change if the compatibility switches have not been set yet.    
        public Nullable<bool> IsCompatibilitySwitchSet(String value)
        {
            Nullable<bool> fReturn;

            if (_compatFlagsInitialized == false) 
            {
                fReturn = new Nullable<bool>();
            } 
            else
            {
                fReturn  = new Nullable<bool>(_compatFlags != null && _compatFlags.ContainsKey(value));
            }

            return fReturn;
        }
        
        [Obsolete("AppDomain.GetCurrentThreadId has been deprecated because it does not provide a stable Id when managed threads are running on fibers (aka lightweight threads). To get a stable identifier for a managed thread, use the ManagedThreadId property on Thread.  http://go.microsoft.com/fwlink/?linkid=14202", false)]
        [DllImport(Microsoft.Win32.Win32Native.KERNEL32)]
        public static extern int GetCurrentThreadId();

#if FEATURE_REMOTING
        [System.Security.SecuritySafeCritical]  // auto-generated
        [SecurityPermissionAttribute( SecurityAction.Demand, ControlAppDomain = true ),
         ReliabilityContract(Consistency.MayCorruptAppDomain, Cer.MayFail)]            
        public static void Unload(AppDomain domain)
        {
            if (domain == null)
                throw new ArgumentNullException("domain");
            Contract.EndContractBlock();

            try {
                Int32 domainID = AppDomain.GetIdForUnload(domain);
                if (domainID==0)
                    throw new CannotUnloadAppDomainException();
                AppDomain.nUnload(domainID);
            }
            catch(Exception e) {
                throw e;    // throw it again to reset stack trace
            }
        }
#endif

        // Explicitly set policy for a domain (providing policy hasn't been set
        // previously). Making this call will guarantee that previously loaded
        // assemblies will be granted permissions based on the default machine
        // policy that was in place prior to this call.
#if FEATURE_CAS_POLICY        
        [System.Security.SecurityCritical]  // auto-generated_required
        [Obsolete("AppDomain policy levels are obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        public void SetAppDomainPolicy(PolicyLevel domainPolicy)
        {
            if (domainPolicy == null)
                throw new ArgumentNullException("domainPolicy");
            Contract.EndContractBlock();

            if (!IsLegacyCasPolicyEnabled)
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_RequiresCasPolicyExplicit"));
            }

            // Check that policy has not been set previously.
            lock (this) {
                if (_HasSetPolicy)
                    throw new PolicyException(Environment.GetResourceString("Policy_PolicyAlreadySet"));
                _HasSetPolicy = true;

                // Make sure that the loader allows us to change security policy
                // at this time (this will throw if not.)
                nChangeSecurityPolicy();
            }

            // Add the new policy level.
            SecurityManager.PolicyManager.AddLevel(domainPolicy);
        }
#endif //#if !FEATURE_CAS_POLICY
#if FEATURE_CLICKONCE
        public ActivationContext ActivationContext {
            [System.Security.SecurityCritical]  // auto-generated_required
            get {
                return _activationContext;
            }
        }

        public ApplicationIdentity ApplicationIdentity {
            [System.Security.SecurityCritical]  // auto-generated_required
            get {
                return _applicationIdentity;
            }
        }
#endif // FEATURE_CLICKONCE


#if FEATURE_CLICKONCE
        public ApplicationTrust ApplicationTrust {
            [System.Security.SecurityCritical]  // auto-generated_required
#else // FEATURE_CLICKONCE
        internal ApplicationTrust ApplicationTrust {
#endif // FEATURE_CLICKONCE
            get {
                if (_applicationTrust == null && _IsFastFullTrustDomain) {
                    _applicationTrust = new ApplicationTrust(new PermissionSet(PermissionState.Unrestricted));
                }

                return _applicationTrust;
            }
        }

#if FEATURE_IMPERSONATION
        // Set the default principal object to be attached to threads if they
        // attempt to bind to a principal while executing in this appdomain. The
        // default can only be set once.
        [System.Security.SecuritySafeCritical]  // auto-generated
        [SecurityPermissionAttribute(SecurityAction.Demand, Flags=SecurityPermissionFlag.ControlPrincipal)]
        public void SetThreadPrincipal(IPrincipal principal)
        {
            if (principal == null)
                throw new ArgumentNullException("principal");
            Contract.EndContractBlock();

            lock (this) {
                // Check that principal has not been set previously.
                if (_DefaultPrincipal != null)
                    throw new PolicyException(Environment.GetResourceString("Policy_PrincipalTwice"));

                _DefaultPrincipal = principal;
            }
        }
#endif // FEATURE_IMPERSONATION

#if FEATURE_CAS_POLICY
        // Similar to the above, but sets the class of principal to be created
        // instead.
        [System.Security.SecuritySafeCritical]  // auto-generated
        [SecurityPermissionAttribute(SecurityAction.Demand, Flags=SecurityPermissionFlag.ControlPrincipal)]
        public void SetPrincipalPolicy(PrincipalPolicy policy)
        {
            _PrincipalPolicy = policy;
        }
#endif


#if FEATURE_REMOTING        
        // This method gives AppDomain an infinite life time by preventing a lease from being
        // created
        [System.Security.SecurityCritical]  // auto-generated_required
        public override Object InitializeLifetimeService()
        {
            return null;
        }
        // This is useful for requesting execution of some code
        // in another appDomain ... the delegate may be defined 
        // on a marshal-by-value object or a marshal-by-ref or 
        // contextBound object.
        public void DoCallBack(CrossAppDomainDelegate callBackDelegate)
        {
            if (callBackDelegate == null)
                throw new ArgumentNullException("callBackDelegate");
            Contract.EndContractBlock();

            callBackDelegate();        
        }
#endif


        public String DynamicDirectory
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
                String dyndir = GetDynamicDir();
                if (dyndir != null)
                    new FileIOPermission( FileIOPermissionAccess.PathDiscovery, dyndir ).Demand();

                return dyndir;
            }
        }

#if FEATURE_CAS_POLICY
        public static AppDomain CreateDomain(String friendlyName,
                                             Evidence securityInfo) // Optional
        {
            return CreateDomain(friendlyName,
                                securityInfo,
                                null);
        }
    
        public static AppDomain CreateDomain(String friendlyName,
                                             Evidence securityInfo, // Optional
                                             String appBasePath,
                                             String appRelativeSearchPath,
                                             bool shadowCopyFiles)
        {            
            AppDomainSetup info = new AppDomainSetup();
            info.ApplicationBase = appBasePath;
            info.PrivateBinPath = appRelativeSearchPath;
            if(shadowCopyFiles)
                info.ShadowCopyFiles = "true";

            return CreateDomain(friendlyName,
                                securityInfo,
                                info);
        }
#endif // #if FEATURE_CAS_POLICY    (not exposed in core)


        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        extern private String GetDynamicDir();

        // Private helpers called from unmanaged code.

#if FEATURE_REMOTING        

        public static AppDomain CreateDomain(String friendlyName)
        {
            return CreateDomain(friendlyName, null, null);
        }


        // Marshal a single object into a serialized blob.
        [System.Security.SecurityCritical]  // auto-generated
        private static byte[] MarshalObject(Object o)
        {
            CodeAccessPermission.Assert(true);

            return Serialize(o);
        }

        // Marshal two objects into serialized blobs.
        [System.Security.SecurityCritical]  // auto-generated
        private static byte[] MarshalObjects(Object o1, Object o2, out byte[] blob2)
        {
            CodeAccessPermission.Assert(true);

            byte[] blob1 = Serialize(o1);
            blob2 = Serialize(o2);
            return blob1;
        }

        // Unmarshal a single object from a serialized blob.
        [System.Security.SecurityCritical]  // auto-generated
        private static Object UnmarshalObject(byte[] blob)
        {
            CodeAccessPermission.Assert(true);

            return Deserialize(blob);
        }

        // Unmarshal two objects from serialized blobs.
        [System.Security.SecurityCritical]  // auto-generated
        private static Object UnmarshalObjects(byte[] blob1, byte[] blob2, out Object o2)
        {
            CodeAccessPermission.Assert(true);

            Object o1 = Deserialize(blob1);
            o2 = Deserialize(blob2);
            return o1;
        }

        // Helper routines.
        [System.Security.SecurityCritical]  // auto-generated
        private static byte[] Serialize(Object o)
        {
            if (o == null)
            {
                return null;
            }
            else if (o is ISecurityEncodable)
            {
                SecurityElement element = ((ISecurityEncodable)o).ToXml();
                MemoryStream ms = new MemoryStream( 4096 );
                ms.WriteByte( 0 );
                StreamWriter writer = new StreamWriter( ms, Encoding.UTF8 );
                element.ToWriter( writer );
                writer.Flush();
                return ms.ToArray();
            }
            else
            {
                MemoryStream ms = new MemoryStream();
                ms.WriteByte( 1 );
                CrossAppDomainSerializer.SerializeObject(o, ms);
                return ms.ToArray();
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        private static Object Deserialize(byte[] blob)
        {
            if (blob == null)
                return null;

            if (blob[0] == 0)
            {
                Parser parser = new Parser( blob, Tokenizer.ByteTokenEncoding.UTF8Tokens, 1 );
                SecurityElement root = parser.GetTopElement();
                if (root.Tag.Equals( "IPermission" ) || root.Tag.Equals( "Permission" ))
                {
                    IPermission ip = System.Security.Util.XMLUtil.CreatePermission( root, PermissionState.None, false );

                    if (ip == null)
                    {
                        return null;
                    }

                    ip.FromXml( root );

                    return ip;
                }
                else if (root.Tag.Equals( "PermissionSet" ))
                {
                    PermissionSet permissionSet = new PermissionSet();

                    permissionSet.FromXml( root, false, false );

                    return permissionSet;
                }
                else if (root.Tag.Equals( "PermissionToken" ))
                {
                    PermissionToken pToken = new PermissionToken();

                    pToken.FromXml( root );

                    return pToken;
                }
                else
                {
                    return null;
                }

            }
            else
            {
                Object obj = null;
                using(MemoryStream stream = new MemoryStream( blob, 1, blob.Length - 1 )) {
                    obj = CrossAppDomainSerializer.DeserializeObject(stream);
                }

                Contract.Assert( !(obj is IPermission), "IPermission should be xml deserialized" );
                Contract.Assert( !(obj is PermissionSet), "PermissionSet should be xml deserialized" );

                return obj;
            }
        }

#endif // FEATURE_REMOTING        

#if !FEATURE_CORECLR
        //
        // Called by the VM if ICLRExecutionManager::Pause is called with the PAUSE_APP_DOMAINS flag.
        // This mimics the behavior of the CoreCLR FAS (fast application switching) model, to ensure
        // that code that depends on things happening in a particular order will work.
        //
        [System.Security.SecurityCritical]
        internal static void Pause()
        {
            AppDomainPauseManager.Instance.Pausing();
            AppDomainPauseManager.Instance.Paused();
        }

        //
        // Called by the VM if ICLRExecutionManager::Resume is called after ICLRExecutionManager::Pause 
        // was called with the PAUSE_APP_DOMAINS flag.
        // This mimics the behavior of the CoreCLR FAS (fast application switching) model, to ensure
        // that code that depends on things happening in a particular order will work.
        //
        [System.Security.SecurityCritical]
        internal static void Resume()
        {
            if (AppDomainPauseManager.IsPaused)
            {
                AppDomainPauseManager.Instance.Resuming();
                AppDomainPauseManager.Instance.Resumed();
            }
        }
#endif

        private AppDomain() {
            throw new NotSupportedException(Environment.GetResourceString(ResId.NotSupported_Constructor));
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern int _nExecuteAssembly(RuntimeAssembly assembly, String[] args);
        internal int nExecuteAssembly(RuntimeAssembly assembly, String[] args)
        {
            return _nExecuteAssembly(assembly, args);
        }

#if FEATURE_VERSIONING
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern void nCreateContext();

        [System.Security.SecurityCritical]
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern void nSetupBindingPaths(String trustedPlatformAssemblies, String platformResourceRoots, String appPath, String appNiPaths, String appLocalWinMD);

        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        internal void SetupBindingPaths(String trustedPlatformAssemblies, String platformResourceRoots, String appPath, String appNiPaths, String appLocalWinMD)
        {
            nSetupBindingPaths(trustedPlatformAssemblies, platformResourceRoots, appPath, appNiPaths, appLocalWinMD);
        }
#endif // FEATURE_VERSIONING

#if FEATURE_REMOTING
        internal void CreateRemotingData()
        {
                    lock(this) {
                        if (_RemotingData == null)
                            _RemotingData = new DomainSpecificRemotingData();
                    }
        }
        
        internal DomainSpecificRemotingData RemotingData
        {
            get 
            { 
                if (_RemotingData == null) 
                    CreateRemotingData();

                return _RemotingData;
            }
        }
#endif // FEATURE_REMOTING

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern String nGetFriendlyName();
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern bool nIsDefaultAppDomainForEvidence();

        // support reliability for certain event handlers, if the target
        // methods also participate in this discipline.  If caller passes
        // an existing MulticastDelegate, then we could use a MDA to indicate
        // that reliability is not guaranteed.  But if it is a single cast
        // scenario, we can make it work.

        public event EventHandler ProcessExit
        {
            [System.Security.SecuritySafeCritical]  // auto-generated_required
            add
            {
                if (value != null)
                {
                    RuntimeHelpers.PrepareContractedDelegate(value);
                    lock(this)
                        _processExit += value;
                }
            }
            remove
            {
                lock(this)
                    _processExit -= value;
            }
        }


        public event EventHandler DomainUnload
        {
            [System.Security.SecuritySafeCritical]  // auto-generated_required
            add
            {
                if (value != null)
                {
                    RuntimeHelpers.PrepareContractedDelegate(value);
                    lock(this)
                        _domainUnload += value;
                }
            }
#if FEATURE_CORECLR
            [System.Security.SecuritySafeCritical] 
#endif
            remove
            {
                lock(this)
                    _domainUnload -= value;
            }
        }


        public event UnhandledExceptionEventHandler UnhandledException
        {
            [System.Security.SecurityCritical]  // auto-generated_required
            add
            {
                if (value != null)
                {
                    RuntimeHelpers.PrepareContractedDelegate(value);
                    lock(this)
                        _unhandledException += value;
                }
            }
            [System.Security.SecurityCritical]  // auto-generated_required
            remove
            {
                lock(this)
                    _unhandledException -= value;
            }
        }

#if FEATURE_EXCEPTION_NOTIFICATIONS
        // This is the event managed code can wireup against to be notified
        // about first chance exceptions. 
        //
        // To register/unregister the callback, the code must be SecurityCritical.
        public event EventHandler<FirstChanceExceptionEventArgs> FirstChanceException
        {
            [System.Security.SecurityCritical]  // auto-generated_required
            add
            {
                if (value != null)
                {
                    RuntimeHelpers.PrepareContractedDelegate(value);
                    lock(this)
                        _firstChanceException += value;
                }
            }
            [System.Security.SecurityCritical]  // auto-generated_required
            remove
            {
                lock(this)
                    _firstChanceException -= value;
            }
        }
#endif // FEATURE_EXCEPTION_NOTIFICATIONS

        private void OnAssemblyLoadEvent(RuntimeAssembly LoadedAssembly)
        {
            AssemblyLoadEventHandler eventHandler = AssemblyLoad;
            if (eventHandler != null) {
                AssemblyLoadEventArgs ea = new AssemblyLoadEventArgs(LoadedAssembly);
                eventHandler(this, ea);
            }
        }
    
        // This method is called by the VM.
        [System.Security.SecurityCritical]
        private RuntimeAssembly OnResourceResolveEvent(RuntimeAssembly assembly, String resourceName)
        {
            ResolveEventHandler eventHandler = _ResourceResolve;
            if ( eventHandler == null)
                return null;

            Delegate[] ds = eventHandler.GetInvocationList();
            int len = ds.Length;
            for (int i = 0; i < len; i++) {
                Assembly asm = ((ResolveEventHandler)ds[i])(this, new ResolveEventArgs(resourceName, assembly));
                RuntimeAssembly ret = GetRuntimeAssembly(asm);
                if (ret != null)
                    return ret;
            }

            return null;
        }
        
        // This method is called by the VM
        [System.Security.SecurityCritical]
        private RuntimeAssembly OnTypeResolveEvent(RuntimeAssembly assembly, String typeName)
        {
            ResolveEventHandler eventHandler = _TypeResolve;
            if (eventHandler == null)
                return null;

            Delegate[] ds = eventHandler.GetInvocationList();
            int len = ds.Length;
            for (int i = 0; i < len; i++) {
                Assembly asm = ((ResolveEventHandler)ds[i])(this, new ResolveEventArgs(typeName, assembly));
                RuntimeAssembly ret = GetRuntimeAssembly(asm);
                if (ret != null)
                    return ret;
            }

            return null;
        }

        // This method is called by the VM.
        [System.Security.SecurityCritical]
        private RuntimeAssembly OnAssemblyResolveEvent(RuntimeAssembly assembly, String assemblyFullName)
        {
            ResolveEventHandler eventHandler = _AssemblyResolve;

            if (eventHandler == null)
            {
                return null;
            }

            Delegate[] ds = eventHandler.GetInvocationList();
            int len = ds.Length;
            for (int i = 0; i < len; i++) {
                Assembly asm = ((ResolveEventHandler)ds[i])(this, new ResolveEventArgs(assemblyFullName, assembly));
                RuntimeAssembly ret = GetRuntimeAssembly(asm);
                if (ret != null)
                    return ret;
            }
            
            return null;
        }

#if FEATURE_REFLECTION_ONLY_LOAD
        
        private RuntimeAssembly OnReflectionOnlyAssemblyResolveEvent(RuntimeAssembly  assembly, String assemblyFullName)
        {
            ResolveEventHandler eventHandler = ReflectionOnlyAssemblyResolve;
            if (eventHandler != null) {

                Delegate[] ds = eventHandler.GetInvocationList();
                int len = ds.Length;
                for (int i = 0; i < len; i++) {
                    Assembly asm = ((ResolveEventHandler)ds[i])(this, new ResolveEventArgs(assemblyFullName, assembly));
                    RuntimeAssembly ret = GetRuntimeAssembly(asm);
                    if (ret != null)
                        return ret;
                }
            }

            return null;
        }
        
#if FEATURE_COMINTEROP
        // Called by VM - code:CLRPrivTypeCacheReflectionOnlyWinRT::RaiseNamespaceResolveEvent
        private RuntimeAssembly[] OnReflectionOnlyNamespaceResolveEvent(RuntimeAssembly assembly, string namespaceName)
        {
            return System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMetadata.OnReflectionOnlyNamespaceResolveEvent(this, assembly, namespaceName);
        }
#endif // FEATURE_COMINTEROP

#endif // FEATURE_REFLECTION_ONLY_LOAD

#if FEATURE_COMINTEROP
        // Called by VM - code:CLRPrivTypeCacheWinRT::RaiseDesignerNamespaceResolveEvent
        private string[] OnDesignerNamespaceResolveEvent(string namespaceName)
        {
            return System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMetadata.OnDesignerNamespaceResolveEvent(this, namespaceName);
        }
#endif // FEATURE_COMINTEROP

        internal AppDomainSetup FusionStore
        {
            get {
                Contract.Assert(_FusionStore != null, 
                                "Fusion store has not been correctly setup in this domain");
                return _FusionStore;
            }
        }

        internal static RuntimeAssembly GetRuntimeAssembly(Assembly asm)
        {
            if (asm == null)
                return null;

            RuntimeAssembly rtAssembly = asm as RuntimeAssembly;
            if (rtAssembly != null)
                return rtAssembly;

            AssemblyBuilder ab = asm as AssemblyBuilder;
            if (ab != null)
                return ab.InternalAssembly;

            return null;
        }

        private Dictionary<String, Object[]> LocalStore
        {
            get { 
                if (_LocalStore != null)
                    return _LocalStore;
                else {
                    _LocalStore = new Dictionary<String, Object[]>();
                    return _LocalStore;
                }
            }
        }

#if FEATURE_FUSION
        private void TurnOnBindingRedirects()
        {
            _FusionStore.DisallowBindingRedirects = false;
        }
#endif

        // This will throw a CannotUnloadAppDomainException if the appdomain is
        // in another process.
#if FEATURE_REMOTING        
        [System.Security.SecurityCritical]  // auto-generated
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]  
        internal static Int32 GetIdForUnload(AppDomain domain)
        {
            if (RemotingServices.IsTransparentProxy(domain))
            {
                return RemotingServices.GetServerDomainIdForProxy(domain);
            }
            else
                return domain.Id;
        }
#endif

        // Used to determine if server object context is valid in
        // x-domain remoting scenarios.
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal static extern bool IsDomainIdValid(Int32 id);
        
#if FEATURE_REMOTING        
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern AppDomain GetDefaultDomain();
#endif

#if FEATURE_IMPERSONATION
        // Internal routine to retrieve the default principal object. If this is
        // called before the principal has been explicitly set, it will
        // automatically allocate a default principal based on the policy set by
        // SetPrincipalPolicy.
        internal IPrincipal GetThreadPrincipal()
        {
            IPrincipal principal = null;
            if (_DefaultPrincipal == null) {
#if FEATURE_CAS_POLICY                    
                switch (_PrincipalPolicy) {
                case PrincipalPolicy.NoPrincipal:
                    principal = null;
                    break;
                case PrincipalPolicy.UnauthenticatedPrincipal:
                    principal = new GenericPrincipal(new GenericIdentity("", ""),
                                                     new String[] {""});
                    break;
                case PrincipalPolicy.WindowsPrincipal:
                    principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
                    break;
                default:
                    principal = null;
                    break;
                    }
#else
                principal = new GenericPrincipal(new GenericIdentity("", ""),
                                                 new String[] {""});

#endif
            }
            else
                principal = _DefaultPrincipal;

            return principal;
        }
#endif // FEATURE_IMPERSONATION

#if FEATURE_REMOTING        

        [System.Security.SecurityCritical]  // auto-generated
        internal void CreateDefaultContext()
        {
                lock(this) {
                    // if it has not been created we ask the Context class to 
                    // create a new default context for this appdomain.
                    if (_DefaultContext == null)
                        _DefaultContext = Context.CreateDefaultContext();
                }            
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal Context GetDefaultContext()
        {
            if (_DefaultContext == null)
                CreateDefaultContext();
            return _DefaultContext;
        }

        // Ensure that evidence provided when creating an AppDomain would not have been used to create a
        // sandbox in legacy CAS mode.  If it could have been used to create a sandbox, and we're not in CAS
        // mode, then we throw an exception to prevent acciental creation of unsandboxed domains where a
        // sandbox would have been expected.
        [SecuritySafeCritical]
        internal static void CheckDomainCreationEvidence(AppDomainSetup creationDomainSetup,
                                                         Evidence creationEvidence)
        {
            if (creationEvidence != null && !CurrentDomain.IsLegacyCasPolicyEnabled)
            {
                if (creationDomainSetup == null || creationDomainSetup.ApplicationTrust == null)
                {
                    // We allow non-null evidence in CAS mode to support the common pattern of passing in
                    // AppDomain.CurrentDomain.Evidence.  Since the zone evidence must have been changed
                    // if the user has any expectation of sandboxing the domain under legacy CAS policy,
                    // we use a zone comparison to check for this pattern.  A strict comparison will not
                    // work, since MSDN samples for creating a domain show using a modified version of the
                    // current domain's evidence and we would capturce people who copied and pasted these
                    // samples without intending to sandbox.
                    Zone creatorsZone = CurrentDomain.EvidenceNoDemand.GetHostEvidence<Zone>();
                    SecurityZone creatorsSecurityZone = creatorsZone != null ?
                        creatorsZone.SecurityZone :
                        SecurityZone.MyComputer;

                    Zone suppliedZone = creationEvidence.GetHostEvidence<Zone>();
                    if (suppliedZone != null)
                    {
                        if (suppliedZone.SecurityZone != creatorsSecurityZone &&
                            suppliedZone.SecurityZone != SecurityZone.MyComputer)
                        {
                            throw new NotSupportedException(Environment.GetResourceString("NotSupported_RequiresCasPolicyImplicit"));
                        }
                    }
                }
            }
        }

#if FEATURE_CAS_POLICY
        [System.Security.SecuritySafeCritical]  // auto-generated
        [SecurityPermissionAttribute( SecurityAction.Demand, ControlAppDomain = true )]
        public static AppDomain CreateDomain(String friendlyName,
                                      Evidence securityInfo,
                                      AppDomainSetup info)
        {
            return InternalCreateDomain(friendlyName, securityInfo, info);
        }
#else
        internal static AppDomain CreateDomain(String friendlyName,
                                      Evidence securityInfo,
                                      AppDomainSetup info)
        {
            return InternalCreateDomain(friendlyName, securityInfo, info);
        }
#endif

        [System.Security.SecurityCritical]  // auto-generated
        internal static AppDomain InternalCreateDomain(String friendlyName,
                                      Evidence securityInfo,
                                      AppDomainSetup info)
        {
            if (friendlyName == null)
                throw new ArgumentNullException("friendlyName", Environment.GetResourceString("ArgumentNull_String"));

            Contract.EndContractBlock();

            AppDomain.CheckCreateDomainSupported();

            if (info == null)
                info = new AppDomainSetup();
            if (info.TargetFrameworkName == null)
                info.TargetFrameworkName = AppDomain.CurrentDomain.GetTargetFrameworkName();

            AppDomainManager domainManager = AppDomain.CurrentDomain.DomainManager;
            if (domainManager != null)
                return domainManager.CreateDomain(friendlyName, securityInfo, info);

            // No AppDomainManager is set up for this domain

            // If evidence is provided, we check to make sure that is allowed.    
            if (securityInfo != null)
            {
                new SecurityPermission(SecurityPermissionFlag.ControlEvidence).Demand();

                // If we're potentially trying to sandbox without using a homogenous domain, we need to reject
                // the domain creation.
                CheckDomainCreationEvidence(info, securityInfo);
            }

            return nCreateDomain(friendlyName,
                                 info,
                                 securityInfo,
                                 securityInfo == null ? AppDomain.CurrentDomain.InternalEvidence : null,
                                 AppDomain.CurrentDomain.GetSecurityDescriptor());
        }
#endif // FEATURE_REMOTING

#if FEATURE_CAS_POLICY   

        public static AppDomain CreateDomain (string friendlyName,
                                              Evidence securityInfo,
                                              AppDomainSetup info,
                                              PermissionSet grantSet,
                                              params StrongName[] fullTrustAssemblies)
        {
            if (info == null)
                throw new ArgumentNullException("info");
            if (info.ApplicationBase == null)
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_AppDomainSandboxAPINeedsExplicitAppBase"));
            Contract.EndContractBlock();

            if (fullTrustAssemblies == null)
            {
                fullTrustAssemblies = new StrongName[0];
            }

            info.ApplicationTrust = new ApplicationTrust(grantSet, fullTrustAssemblies);
            return CreateDomain(friendlyName, securityInfo, info);
        }

        public static AppDomain CreateDomain(String friendlyName,
                                             Evidence securityInfo, // Optional
                                             String appBasePath,
                                             String appRelativeSearchPath,
                                             bool shadowCopyFiles,
                                             AppDomainInitializer adInit, 
                                             string[] adInitArgs) 
        {
            AppDomainSetup info = new AppDomainSetup();
            info.ApplicationBase = appBasePath;
            info.PrivateBinPath = appRelativeSearchPath;
            info.AppDomainInitializer=adInit;
            info.AppDomainInitializerArguments=adInitArgs;
            if(shadowCopyFiles)
                info.ShadowCopyFiles = "true";

            return CreateDomain(friendlyName,
                                securityInfo,
                                info);
        }
#endif // FEATURE_CAS_POLICY

#if FEATURE_CORECLR
        [System.Security.SecurityCritical]
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern void nSetNativeDllSearchDirectories(string paths);
#endif

        [System.Security.SecurityCritical]  // auto-generated
        private void SetupFusionStore(AppDomainSetup info, AppDomainSetup oldInfo)
        {
            Contract.Requires(info != null);

#if FEATURE_FUSION
            if (oldInfo == null) {
                        
       // Create the application base and configuration file from the imagelocation
            // passed in or use the Win32 Image name.
            if(info.Value[(int) AppDomainSetup.LoaderInformation.ApplicationBaseValue] == null ||
               info.Value[(int) AppDomainSetup.LoaderInformation.ConfigurationFileValue] == null ) 
#else        
            if (info.ApplicationBase == null)
#endif                

            {
#if FEATURE_FUSION
                AppDomain defaultDomain = GetDefaultDomain();
                if (this == defaultDomain) {
                    // The default domain gets its defaults from the main process.
                    info.SetupDefaults(RuntimeEnvironment.GetModuleFileName(), imageLocationAlreadyNormalized : true);
                }
                else {
                    // Other domains get their defaults from the default domain. This way, a host process
                    // can use AppDomainManager to set up the defaults for every domain created in the process.
                    if (info.Value[(int) AppDomainSetup.LoaderInformation.ConfigurationFileValue] == null)
                        info.ConfigurationFile = defaultDomain.FusionStore.Value[(int) AppDomainSetup.LoaderInformation.ConfigurationFileValue];
                    if (info.Value[(int) AppDomainSetup.LoaderInformation.ApplicationBaseValue] == null)
                        info.ApplicationBase = defaultDomain.FusionStore.Value[(int) AppDomainSetup.LoaderInformation.ApplicationBaseValue];
                    if (info.Value[(int) AppDomainSetup.LoaderInformation.ApplicationNameValue] == null)
                        info.ApplicationName = defaultDomain.FusionStore.Value[(int) AppDomainSetup.LoaderInformation.ApplicationNameValue];
                }
#else
                info.SetupDefaults(RuntimeEnvironment.GetModuleFileName(), imageLocationAlreadyNormalized : true);
#endif
                
            }

#if FEATURE_FUSION
            // If there is no relative path then check the
            // environment
            if(info.Value[(int) AppDomainSetup.LoaderInformation.PrivateBinPathValue] == null)
                info.PrivateBinPath = Environment.nativeGetEnvironmentVariable(AppDomainSetup.PrivateBinPathEnvironmentVariable);

            // Add the developer path if it exists on this
            // machine.
            if(info.DeveloperPath == null)
                info.DeveloperPath = RuntimeEnvironment.GetDeveloperPath();

            }
                        
            // Set up the fusion context
            IntPtr fusionContext = GetFusionContext();
            info.SetupFusionContext(fusionContext, oldInfo);

            // Set loader optimization policy
#else
#if FEATURE_VERSIONING
            nCreateContext();
#endif // FEATURE_VERSIONING
#endif // FEATURE_FUSION

#if FEATURE_LOADER_OPTIMIZATION
            if (info.LoaderOptimization != LoaderOptimization.NotSpecified || (oldInfo != null && info.LoaderOptimization != oldInfo.LoaderOptimization))
                UpdateLoaderOptimization(info.LoaderOptimization);
#endif                        



            // This must be the last action taken
            _FusionStore = info;
        }

        // used to package up evidence, so it can be serialized
        //   for the call to InternalRemotelySetupRemoteDomain
        [Serializable]
        private class EvidenceCollection
        {
            public Evidence ProvidedSecurityInfo;
            public Evidence CreatorsSecurityInfo;
        }

        private static void RunInitializer(AppDomainSetup setup)
        {
            if (setup.AppDomainInitializer!=null)
            {
                string[] args=null;
                if (setup.AppDomainInitializerArguments!=null)
                    args=(string[])setup.AppDomainInitializerArguments.Clone();
                setup.AppDomainInitializer(args);
            }
        }

        // Used to switch into other AppDomain and call SetupRemoteDomain.
        //   We cannot simply call through the proxy, because if there
        //   are any remoting sinks registered, they can add non-mscorlib
        //   objects to the message (causing an assembly load exception when
        //   we try to deserialize it on the other side)
        [System.Security.SecurityCritical]  // auto-generated
        private static object PrepareDataForSetup(String friendlyName,
                                                        AppDomainSetup setup,
                                                        Evidence providedSecurityInfo,
                                                        Evidence creatorsSecurityInfo,
                                                        IntPtr parentSecurityDescriptor,
                                                        string sandboxName,
                                                        string[] propertyNames,
                                                        string[] propertyValues)
        {
            byte[] serializedEvidence = null;
            bool generateDefaultEvidence = false;

#if FEATURE_CAS_POLICY
            // serialize evidence
            EvidenceCollection evidenceCollection = null;

            if (providedSecurityInfo != null || creatorsSecurityInfo != null)
            {
                // If we're just passing through AppDomain.CurrentDomain.Evidence, and that evidence is just
                // using the standard runtime AppDomainEvidenceFactory, don't waste time serializing it and
                // deserializing it back -- instead, we can recreate a new AppDomainEvidenceFactory in the new
                // domain.  We only want to do this if there is no HostSecurityManager, otherwise the
                // HostSecurityManager could have added additional evidence on top of our standard factory.
                HostSecurityManager hsm = CurrentDomain.DomainManager != null ? CurrentDomain.DomainManager.HostSecurityManager : null;
                bool hostMayContributeEvidence = hsm != null &&
                                                 hsm.GetType() != typeof(HostSecurityManager) &&
                                                 (hsm.Flags & HostSecurityManagerOptions.HostAppDomainEvidence) == HostSecurityManagerOptions.HostAppDomainEvidence;
                if (!hostMayContributeEvidence)
                {
                    if (providedSecurityInfo != null &&
                        providedSecurityInfo.IsUnmodified &&
                        providedSecurityInfo.Target != null &&
                        providedSecurityInfo.Target is AppDomainEvidenceFactory)
                    {
                        providedSecurityInfo = null;
                        generateDefaultEvidence = true;
                    }
                    if (creatorsSecurityInfo != null &&
                        creatorsSecurityInfo.IsUnmodified &&
                        creatorsSecurityInfo.Target != null &&
                        creatorsSecurityInfo.Target is AppDomainEvidenceFactory)
                    {
                        creatorsSecurityInfo = null;
                        generateDefaultEvidence = true;
                    }
                }
            }
            if ((providedSecurityInfo != null) ||
                (creatorsSecurityInfo != null)) {
                evidenceCollection = new EvidenceCollection();
                evidenceCollection.ProvidedSecurityInfo = providedSecurityInfo;
                evidenceCollection.CreatorsSecurityInfo = creatorsSecurityInfo;
            }

            if (evidenceCollection != null) {
                serializedEvidence =
                    CrossAppDomainSerializer.SerializeObject(evidenceCollection).GetBuffer();                
            }
#endif // FEATURE_CAS_POLICY

            AppDomainInitializerInfo initializerInfo = null;
            if (setup!=null && setup.AppDomainInitializer!=null)
                initializerInfo=new AppDomainInitializerInfo(setup.AppDomainInitializer);

            // will travel x-Ad, drop non-agile data 
            AppDomainSetup newSetup = new AppDomainSetup(setup, false);

#if FEATURE_CORECLR
            // Remove the special AppDomainCompatSwitch entries from the set of name value pairs
            // And add them to the AppDomainSetup
            //
            // This is only supported on CoreCLR through ICLRRuntimeHost2.CreateAppDomainWithManager
            // Desktop code should use System.AppDomain.CreateDomain() or 
            // System.AppDomainManager.CreateDomain() and add the flags to the AppDomainSetup
            List<String> compatList = new List<String>();
                        
            if(propertyNames!=null && propertyValues != null)
            {
                for (int i=0; i<propertyNames.Length; i++)
                {
                    if(String.Compare(propertyNames[i], "AppDomainCompatSwitch", StringComparison.OrdinalIgnoreCase) == 0) 
                    {
                        compatList.Add(propertyValues[i]);
                        propertyNames[i] = null;                        
                        propertyValues[i] = null;
                    }

                }
                
                if (compatList.Count > 0)
                {
                    newSetup.SetCompatibilitySwitches(compatList);
                }            
            }
#endif // FEATURE_CORECLR


            return new Object[] 
            {
                friendlyName, 
                newSetup, 
                parentSecurityDescriptor, 
                generateDefaultEvidence,
                serializedEvidence,
                initializerInfo,
                sandboxName,
                propertyNames,
                propertyValues
            };  
        } // PrepareDataForSetup


        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        private static Object Setup(Object arg)
        {
            Contract.Requires(arg != null && arg is Object[]);
            Contract.Requires(((Object[])arg).Length >= 8);

            Object[] args=(Object[])arg;
            String           friendlyName               = (String)args[0];
            AppDomainSetup   setup                      = (AppDomainSetup)args[1];
            IntPtr           parentSecurityDescriptor   = (IntPtr)args[2];
            bool             generateDefaultEvidence    = (bool)args[3];
            byte[]           serializedEvidence         = (byte[])args[4];
            AppDomainInitializerInfo initializerInfo    = (AppDomainInitializerInfo)args[5];
            string           sandboxName                = (string)args[6];
            string[]         propertyNames              = (string[])args[7]; // can contain null elements
            string[]         propertyValues             = (string[])args[8]; // can contain null elements           
            // extract evidence
            Evidence providedSecurityInfo = null;
            Evidence creatorsSecurityInfo = null;


            AppDomain ad = AppDomain.CurrentDomain;
            AppDomainSetup newSetup=new AppDomainSetup(setup,false);

            if(propertyNames!=null && propertyValues != null)
            {
#if FEATURE_CORECLR
                StringBuilder normalisedAppPathList = null;
#endif // FEATURE_CORECLR
                for (int i=0; i<propertyNames.Length; i++)
                {
                    if(propertyNames[i]=="APPBASE") // make sure in sync with Fusion
                    {
                        if(propertyValues[i]==null)
                            throw new ArgumentNullException("APPBASE");
                            
                        if (Path.IsRelative(propertyValues[i]))
                            throw new ArgumentException( Environment.GetResourceString( "Argument_AbsolutePathRequired" ) );

                        newSetup.ApplicationBase = NormalizePath(propertyValues[i], fullCheck: true);

                    }
#if FEATURE_CAS_POLICY
                    else if(propertyNames[i]=="LOCATION_URI" && providedSecurityInfo==null)
                    {
                        providedSecurityInfo=new Evidence();
                        providedSecurityInfo.AddHostEvidence(new Url(propertyValues[i]));
                        ad.SetDataHelper(propertyNames[i],propertyValues[i],null);
                    }
#endif // FEATURE_CAS_POLICY
#if FEATURE_LOADER_OPTIMIZATION
                    else
                    if(propertyNames[i]=="LOADER_OPTIMIZATION")
                    {
                        if(propertyValues[i]==null)
                            throw new ArgumentNullException("LOADER_OPTIMIZATION");

                        switch(propertyValues[i])
                        {
                            case "SingleDomain": newSetup.LoaderOptimization=LoaderOptimization.SingleDomain;break;
                            case "MultiDomain": newSetup.LoaderOptimization=LoaderOptimization.MultiDomain;break;
                            case "MultiDomainHost": newSetup.LoaderOptimization=LoaderOptimization.MultiDomainHost;break;
                            case "NotSpecified": newSetup.LoaderOptimization=LoaderOptimization.NotSpecified;break;
                            default: throw new ArgumentException(Environment.GetResourceString("Argument_UnrecognizedLoaderOptimization"), "LOADER_OPTIMIZATION");
                        }
                    }
#endif // FEATURE_LOADER_OPTIMIZATION
#if FEATURE_CORECLR
                    else
                    if(propertyNames[i]=="NATIVE_DLL_SEARCH_DIRECTORIES")
                    {
                        if(propertyValues[i]==null)
                            throw new ArgumentNullException("NATIVE_DLL_SEARCH_DIRECTORIES");
                        ad.SetDataHelper(propertyNames[i],propertyValues[i],null);
                        string paths = (string)propertyValues[i];
                        if( paths.Length==0 )
                            continue;
                        nSetNativeDllSearchDirectories(paths);
                    }
                    else
                    if(propertyNames[i]=="TRUSTED_PLATFORM_ASSEMBLIES" ||
                       propertyNames[i]=="PLATFORM_RESOURCE_ROOTS" ||
                       propertyNames[i]=="APP_PATHS" ||
                       propertyNames[i]=="APP_NI_PATHS")
                    {
                        string values = propertyValues[i];
                        if(values==null)
                            throw new ArgumentNullException(propertyNames[i]);

                        int estimatedLength = values.Length + 1; // +1 for extra separator temporarily added at end
                        if (normalisedAppPathList == null) {
                            normalisedAppPathList = new StringBuilder(estimatedLength);
                        }
                        else {
                            normalisedAppPathList.Clear();
                            if (normalisedAppPathList.Capacity < estimatedLength)
                                normalisedAppPathList.Capacity = estimatedLength;
                        }

                        for (int pos = 0; pos < values.Length; pos++)
                        {
                            string path;

                            int nextPos = values.IndexOf(Path.PathSeparator, pos);
                            if (nextPos == -1)
                            {
                                path = values.Substring(pos);
                                pos = values.Length - 1;
                            }
                            else
                            {
                                path = values.Substring(pos, nextPos - pos);
                                pos = nextPos;
                            }

                            if( path.Length==0 )                  // skip empty dirs
                                continue;

                            if (Path.IsRelative(path))
                                throw new ArgumentException( Environment.GetResourceString( "Argument_AbsolutePathRequired" ) );

                            string appPath = NormalizePath(path, fullCheck: true);
                            normalisedAppPathList.Append(appPath);
                            normalisedAppPathList.Append(Path.PathSeparator);
                        }
                        // Strip the last separator
                        if (normalisedAppPathList.Length > 0)
                        {
                            normalisedAppPathList.Remove(normalisedAppPathList.Length - 1, 1);
                        }
                        ad.SetDataHelper(propertyNames[i],normalisedAppPathList.ToString(),null);        // not supported by fusion, so set explicitly
                    }
                    else
                    if(propertyNames[i]!= null)
                    {
                        ad.SetDataHelper(propertyNames[i],propertyValues[i],null);     // just propagate
                    }
#endif

                }
            }

#if !FEATURE_CORECLR
            AppDomainSortingSetupInfo sortingSetup = newSetup._AppDomainSortingSetupInfo;

            if(sortingSetup != null)
            {          
                if(sortingSetup._pfnIsNLSDefinedString == IntPtr.Zero || sortingSetup._pfnCompareStringEx == IntPtr.Zero || sortingSetup._pfnLCMapStringEx == IntPtr.Zero || sortingSetup._pfnFindNLSStringEx == IntPtr.Zero
                    || sortingSetup._pfnCompareStringOrdinal == IntPtr.Zero || sortingSetup._pfnGetNLSVersionEx == IntPtr.Zero) 
                {
             
                    if(!(sortingSetup._pfnIsNLSDefinedString == IntPtr.Zero && sortingSetup._pfnCompareStringEx == IntPtr.Zero && sortingSetup._pfnLCMapStringEx == IntPtr.Zero && sortingSetup._pfnFindNLSStringEx == IntPtr.Zero
                        && sortingSetup._pfnCompareStringOrdinal == IntPtr.Zero && sortingSetup._pfnGetNLSVersionEx == IntPtr.Zero)) 
                    {
                        // Some functions defined but not all of them.
                        throw new ArgumentException(Environment.GetResourceString("ArgumentException_NotAllCustomSortingFuncsDefined"));
                    }

                }
            }
#endif

            ad.SetupFusionStore(newSetup, null); // makes FusionStore a ref to newSetup
            
            // technically, we don't need this, newSetup refers to the same object as FusionStore 
            // but it's confusing since it isn't immediately obvious whether we have a ref or a copy
            AppDomainSetup adSetup = ad.FusionStore; 

#if FEATURE_CORECLR
            adSetup.InternalSetApplicationTrust(sandboxName);
#endif // FEATURE_CORECLR

#if !FEATURE_CORECLR  // not used by coreclr
            if (serializedEvidence != null) {
                EvidenceCollection evidenceCollection = (EvidenceCollection)
                    CrossAppDomainSerializer.DeserializeObject(new MemoryStream(serializedEvidence));
                providedSecurityInfo  = evidenceCollection.ProvidedSecurityInfo;
                creatorsSecurityInfo  = evidenceCollection.CreatorsSecurityInfo;
            }
#endif
            // set up the friendly name
            ad.nSetupFriendlyName(friendlyName);

#if FEATURE_COMINTEROP
            if (setup != null && setup.SandboxInterop)
            {
                ad.nSetDisableInterfaceCache();
            }
#endif // FEATURE_COMINTEROP

            // set up the AppDomainManager for this domain and initialize security.
            if (adSetup.AppDomainManagerAssembly != null && adSetup.AppDomainManagerType != null)
            {
                ad.SetAppDomainManagerType(adSetup.AppDomainManagerAssembly, adSetup.AppDomainManagerType);
            }

#if FEATURE_APTCA
            // set any conditial-aptca visible assemblies
            ad.PartialTrustVisibleAssemblies = adSetup.PartialTrustVisibleAssemblies;
#endif // FEATURE_APTCA

            ad.CreateAppDomainManager(); // could modify FusionStore's object
            ad.InitializeDomainSecurity(providedSecurityInfo,
                                        creatorsSecurityInfo,
                                        generateDefaultEvidence,
                                        parentSecurityDescriptor,
                                        true);
            
            // can load user code now
            if(initializerInfo!=null)
                adSetup.AppDomainInitializer=initializerInfo.Unwrap();
            RunInitializer(adSetup);

            // Activate the application if needed.
#if FEATURE_CLICKONCE 
            ObjectHandle oh = null;
            if (adSetup.ActivationArguments != null && adSetup.ActivationArguments.ActivateInstance)
                oh = Activator.CreateInstance(ad.ActivationContext);
            return RemotingServices.MarshalInternal(oh, null, null);
#else
            return null;
#endif // FEATURE_CLICKONCE
        }

        [SecuritySafeCritical]
        internal static string NormalizePath(string path, bool fullCheck)
        {
#if FEATURE_PATHCOMPAT
            // Appcontext switches can't currently be safely hit during AppDomain bringup
            return Path.LegacyNormalizePath(
                path: path,
                fullCheck: fullCheck,
                maxPathLength: PathInternal.MaxShortPath,
                expandShortPaths: true);
#else
            return Path.NormalizePath(
                path: path,
                fullCheck: fullCheck,
                expandShortPaths: true);
#endif
        }

#if FEATURE_APTCA
        // Called from DomainAssembly in Conditional APTCA cases
        [PermissionSet(SecurityAction.Assert, Unrestricted = true)]
        [SecuritySafeCritical]
        private bool IsAssemblyOnAptcaVisibleList(RuntimeAssembly assembly)
        {
            if (_aptcaVisibleAssemblies == null)
                return false;

            AssemblyName assemblyName = assembly.GetName();
            String name = assemblyName.GetNameWithPublicKey();

            name = name.ToUpperInvariant();

            int index = Array.BinarySearch<string>(_aptcaVisibleAssemblies, name,
                                                   StringComparer.OrdinalIgnoreCase);
            return (index >=0);

        }

        //Used to binary search the list of C-APTCA strings for an AssemblyName.  It compares assembly name
        //and public key token.
        private class CAPTCASearcher : IComparer
        {
            int IComparer.Compare(object /*string*/lhs, object /*AssemblyName*/rhs)
            {
                AssemblyName captcaEntry = new AssemblyName((string)lhs);
                AssemblyName comparand = (AssemblyName)rhs;
                int nameComp = string.Compare(captcaEntry.Name,
                                              comparand.Name,
                                              StringComparison.OrdinalIgnoreCase);
                if (nameComp != 0)
                {
                    return nameComp;
                }

                //simple names match.  Compare public key tokens.
                byte[] lhsKey = captcaEntry.GetPublicKeyToken();
                byte[] rhsKey = comparand.GetPublicKeyToken();

                // We require both sides have a public key token
                if (lhsKey == null)
                {
                    return -1;
                }
                if (rhsKey == null)
                {
                    return 1;
                }
                if (lhsKey.Length < rhsKey.Length)
                {
                    return -1;
                }
                if (lhsKey.Length > rhsKey.Length)
                {
                    return 1;
                }

                // Tokens seem valid - make sure the compare correctly
                for (int i = 0; i < lhsKey.Length; ++i)
                {
                    byte lhsByte = lhsKey[i];
                    byte rhsByte = rhsKey[i];

                    if (lhsByte < rhsByte)
                    {
                        return -1;
                    }
                    if (lhsByte > rhsByte)
                    {
                        return 1;
                    }
                }

                //They match.
                return 0;
            }
        }

        [System.Security.SecurityCritical]
        private unsafe bool IsAssemblyOnAptcaVisibleListRaw(char * namePtr, int nameLen, byte * keyTokenPtr,
                                                            int keyTokenLen)
        {
            //This version is used for checking ngen dependencies against the C-APTCA list.  It lets us
            //reject ngen images that depend on an assembly that has been disabled in the current domain.
            //Since we only have the public key token in the ngen image, we'll check against that.  The
            //rationale is that if you have a public key token collision in your process you have many
            //problems.  Since the source of this public key token is an ngen image for a full trust
            //assembly, there is essentially no risk to the reduced check.
            if (_aptcaVisibleAssemblies == null)
                return false;

            string name = new string(namePtr, 0, nameLen);
            byte[] keyToken = new byte[keyTokenLen];
            for( int i = 0; i < keyToken.Length; ++i )
                keyToken[i] = keyTokenPtr[i];

            AssemblyName asmName = new AssemblyName();
            asmName.Name = name;
            asmName.SetPublicKeyToken(keyToken);
            try
            {
                int index = Array.BinarySearch(_aptcaVisibleAssemblies, asmName, new CAPTCASearcher());
                return (index >= 0);
            }
            catch (InvalidOperationException) { /* Can happen for poorly formed assembly names */ return false; }
        }

#endif

        // This routine is called from unmanaged code to
        // set the default fusion context.
        [System.Security.SecurityCritical]  // auto-generated
        private void SetupDomain(bool allowRedirects, String path, String configFile, String[] propertyNames, String[] propertyValues)
        {
            // It is possible that we could have multiple threads initializing
            // the default domain. We will just take the winner of these two.
            // (eg. one thread doing a com call and another doing attach for IJW)
            lock (this) {
                if(_FusionStore == null) {
                    AppDomainSetup setup = new AppDomainSetup();
#if FEATURE_CORECLR
                    // always use internet permission set
                    setup.InternalSetApplicationTrust("Internet");
#endif // FEATURE_CORECLR
#if FEATURE_FUSION
                    setup.SetupDefaults(RuntimeEnvironment.GetModuleFileName(), imageLocationAlreadyNormalized : true);
                    if(path != null)
                        setup.Value[(int) AppDomainSetup.LoaderInformation.ApplicationBaseValue] = path;
                    if(configFile != null)
                        setup.Value[(int) AppDomainSetup.LoaderInformation.ConfigurationFileValue] = configFile;

                    // Default fusion context starts with binding redirects turned off.
                    if (!allowRedirects)
                        setup.DisallowBindingRedirects = true;
#endif

#if !FEATURE_CORECLR
                    if (propertyNames != null) {
                        BCLDebug.Assert(propertyValues != null, "propertyValues != null");
                        BCLDebug.Assert(propertyNames.Length == propertyValues.Length, "propertyNames.Length == propertyValues.Length");

                        for (int i = 0; i < propertyNames.Length; ++i) {
                            if (String.Equals(propertyNames[i], "PARTIAL_TRUST_VISIBLE_ASSEMBLIES", StringComparison.Ordinal)) {
                                // The value of the PARTIAL_TRUST_VISIBLE_ASSEMBLIES property is a semicolon
                                // delimited list of assembly names to add to the
                                // PartialTrustVisibleAssemblies setting of the domain setup
                                if (propertyValues[i] != null) {
                                    if (propertyValues[i].Length > 0) {
                                        setup.PartialTrustVisibleAssemblies = propertyValues[i].Split(';');
                                    }
                                    else {
                                        setup.PartialTrustVisibleAssemblies = new string[0];
                                    }
                                }
                            }
                            else {
                                // In v4 we disallow anything but PARTIAL_TRUST_VISIBLE_ASSEMBLIES to come
                                // in via the default domain properties.  That restriction could be lifted
                                // in a future release, at which point this assert should be removed.
                                // 
                                // This should be kept in sync with the real externally facing filter code
                                // in CorHost2::SetPropertiesForDefaultAppDomain
                                BCLDebug.Assert(false, "Unexpected default domain property");
                            }
                        }
                    }
#endif // !FEATURE_CORECLR

#if FEATURE_APTCA
                    // Propigate the set of conditional APTCA assemblies that will be used in the default
                    // domain onto the domain itself and also into the VM
                    PartialTrustVisibleAssemblies = setup.PartialTrustVisibleAssemblies;
#endif // FEATURE_APTCA

                    SetupFusionStore(setup, null);
                }
            }
        }

#if FEATURE_LOADER_OPTIMIZATION
       [System.Security.SecurityCritical]  // auto-generated
       private void SetupLoaderOptimization(LoaderOptimization policy)
        {
            if(policy != LoaderOptimization.NotSpecified) {
                Contract.Assert(FusionStore.LoaderOptimization == LoaderOptimization.NotSpecified,
                                "It is illegal to change the Loader optimization on a domain");

                FusionStore.LoaderOptimization = policy;
                UpdateLoaderOptimization(FusionStore.LoaderOptimization);
            }
        }
#endif
           
#if FEATURE_FUSION
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern IntPtr GetFusionContext();
#endif // FEATURE_FUSION

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern IntPtr GetSecurityDescriptor();

#if FEATURE_REMOTING        
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern AppDomain nCreateDomain(String friendlyName,
                                                      AppDomainSetup setup,
                                                      Evidence providedSecurityInfo,
                                                      Evidence creatorsSecurityInfo,
                                                      IntPtr parentSecurityDescriptor);

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern ObjRef nCreateInstance(String friendlyName,
                                                      AppDomainSetup setup,
                                                      Evidence providedSecurityInfo,
                                                      Evidence creatorsSecurityInfo,
                                                      IntPtr parentSecurityDescriptor);
#endif

        [SecurityCritical]
        private void SetupDomainSecurity(Evidence appDomainEvidence,
                                         IntPtr creatorsSecurityDescriptor,
                                         bool publishAppDomain)
        {
            Evidence stackEvidence = appDomainEvidence;
            SetupDomainSecurity(GetNativeHandle(),
                                JitHelpers.GetObjectHandleOnStack(ref stackEvidence),
                                creatorsSecurityDescriptor,
                                publishAppDomain);

        }

        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity]
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void SetupDomainSecurity(AppDomainHandle appDomain,
                                                       ObjectHandleOnStack appDomainEvidence,
                                                       IntPtr creatorsSecurityDescriptor,
                                                       [MarshalAs(UnmanagedType.Bool)] bool publishAppDomain);

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern void nSetupFriendlyName(string friendlyName);

#if FEATURE_COMINTEROP
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern void nSetDisableInterfaceCache();
#endif // FEATURE_COMINTEROP

#if FEATURE_LOADER_OPTIMIZATION
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern void UpdateLoaderOptimization(LoaderOptimization optimization);
#endif

#if FEATURE_FUSION       
        //
        // This is just designed to prevent compiler warnings.
        // This field is used from native, but we need to prevent the compiler warnings.
        //

        [System.Security.SecurityCritical]  // auto-generated_required
        [Obsolete("AppDomain.SetShadowCopyPath has been deprecated. Please investigate the use of AppDomainSetup.ShadowCopyDirectories instead. http://go.microsoft.com/fwlink/?linkid=14202")]
        public void SetShadowCopyPath(String path)
        {
            InternalSetShadowCopyPath(path);
        }

        [System.Security.SecurityCritical]  // auto-generated_required
        [Obsolete("AppDomain.SetShadowCopyFiles has been deprecated. Please investigate the use of AppDomainSetup.ShadowCopyFiles instead. http://go.microsoft.com/fwlink/?linkid=14202")]
        public void SetShadowCopyFiles()
        {
            InternalSetShadowCopyFiles();
        }

        [System.Security.SecurityCritical]  // auto-generated_required
        [Obsolete("AppDomain.SetDynamicBase has been deprecated. Please investigate the use of AppDomainSetup.DynamicBase instead. http://go.microsoft.com/fwlink/?linkid=14202")]
        public void SetDynamicBase(String path)
        {
            InternalSetDynamicBase(path);
        }
#endif // FEATURE_FUSION

        public AppDomainSetup SetupInformation
        {
            get {
                return new AppDomainSetup(FusionStore,true);
            }
        }

#if FEATURE_FUSION
        [System.Security.SecurityCritical]  // auto-generated
        internal void InternalSetShadowCopyPath(String path)
        {
            if (path != null)
            {
                IntPtr fusionContext = GetFusionContext();
                AppDomainSetup.UpdateContextProperty(fusionContext, AppDomainSetup.ShadowCopyDirectoriesKey, path);
            }
            FusionStore.ShadowCopyDirectories = path;
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal void InternalSetShadowCopyFiles()
        {
            IntPtr fusionContext = GetFusionContext();
            AppDomainSetup.UpdateContextProperty(fusionContext, AppDomainSetup.ShadowCopyFilesKey, "true");
            FusionStore.ShadowCopyFiles = "true";
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal void InternalSetCachePath(String path)
        {
            FusionStore.CachePath = path;
            if (FusionStore.Value[(int) AppDomainSetup.LoaderInformation.CachePathValue] != null)
            {
                IntPtr fusionContext = GetFusionContext();
                AppDomainSetup.UpdateContextProperty(fusionContext, AppDomainSetup.CachePathKey,
                                                     FusionStore.Value[(int) AppDomainSetup.LoaderInformation.CachePathValue]);
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal void InternalSetPrivateBinPath(String path)
        {
            IntPtr fusionContext = GetFusionContext();
            AppDomainSetup.UpdateContextProperty(fusionContext, AppDomainSetup.PrivateBinPathKey, path);
            FusionStore.PrivateBinPath = path;
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal void InternalSetDynamicBase(String path)
        {
            FusionStore.DynamicBase = path;
            if (FusionStore.Value[(int) AppDomainSetup.LoaderInformation.DynamicBaseValue] != null)
            {
                IntPtr fusionContext = GetFusionContext();
                AppDomainSetup.UpdateContextProperty(fusionContext, AppDomainSetup.DynamicBaseKey,
                                                     FusionStore.Value[(int) AppDomainSetup.LoaderInformation.DynamicBaseValue]);
            }
        }
#endif // FEATURE_FUSION

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern String IsStringInterned(String str);

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern String GetOrInternString(String str);
        
        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity]
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void GetGrantSet(AppDomainHandle domain, ObjectHandleOnStack retGrantSet);

#if FEATURE_CAS_POLICY
        [SecurityCritical]
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetIsLegacyCasPolicyEnabled(AppDomainHandle domain);
#endif // FEATURE_CAS_POLICY

        public PermissionSet PermissionSet
        {
            // SecurityCritical because permissions can contain sensitive information such as paths
            [SecurityCritical]
            get
            {
                PermissionSet grantSet = null;
                GetGrantSet(GetNativeHandle(), JitHelpers.GetObjectHandleOnStack(ref grantSet));

                if (grantSet != null)
                {
                    return grantSet.Copy();
                }
                else
                {
                    return new PermissionSet(PermissionState.Unrestricted);
                }
            }
        }

        public bool IsFullyTrusted
        {
            [SecuritySafeCritical]
            get
            {
                PermissionSet grantSet = null;
                GetGrantSet(GetNativeHandle(), JitHelpers.GetObjectHandleOnStack(ref grantSet));

                return grantSet == null || grantSet.IsUnrestricted();
            }
        }

        public bool IsHomogenous
        {
            get
            {
                // Homogenous AppDomains always have an ApplicationTrust associated with them
                return _IsFastFullTrustDomain || _applicationTrust != null;
            }
        }

#if FEATURE_CAS_POLICY
        internal bool IsLegacyCasPolicyEnabled
        {
            [SecuritySafeCritical]
            get
            {
                return GetIsLegacyCasPolicyEnabled(GetNativeHandle());
            }
        }

        // Determine what this homogenous domain thinks the grant set should be for a specific set of evidence
        [SecuritySafeCritical]
        internal PermissionSet GetHomogenousGrantSet(Evidence evidence)
        {
            Contract.Assert(evidence != null);
            Contract.Assert(IsHomogenous);
            Contract.Assert(evidence.GetHostEvidence<GacInstalled>() == null);

            if (_IsFastFullTrustDomain)
            {
                return new PermissionSet(PermissionState.Unrestricted);
            }

            // If the ApplicationTrust's full trust list calls out the assembly, then it is fully trusted
            if (evidence.GetDelayEvaluatedHostEvidence<StrongName>() != null)
            {
                foreach (StrongName fullTrustAssembly in ApplicationTrust.FullTrustAssemblies)
                {
                    StrongNameMembershipCondition sn = new StrongNameMembershipCondition(fullTrustAssembly.PublicKey,
                                                                                         fullTrustAssembly.Name,
                                                                                         fullTrustAssembly.Version);

                    object usedEvidence = null;
                    if ((sn as IReportMatchMembershipCondition).Check(evidence, out usedEvidence))
                    {
                        IDelayEvaluatedEvidence delayEvidence = usedEvidence as IDelayEvaluatedEvidence;
                        if (usedEvidence != null)
                        {
                            delayEvidence.MarkUsed();
                        }

                        return new PermissionSet(PermissionState.Unrestricted);
                    }
                }
            }

            // Otherwise, the grant set is just the default grant set
            return ApplicationTrust.DefaultGrantSet.PermissionSet.Copy();
        }
#endif // FEATURE_CAS_POLICY

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern void nChangeSecurityPolicy();

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [ReliabilityContract(Consistency.MayCorruptAppDomain, Cer.MayFail)]
        internal static extern void nUnload(Int32 domainInternal);
           
        public Object CreateInstanceAndUnwrap(String assemblyName,
                                              String typeName)
        {
            ObjectHandle oh = CreateInstance(assemblyName, typeName);
            if (oh == null)
                return null;

            return oh.Unwrap();
        } // CreateInstanceAndUnwrap


        public Object CreateInstanceAndUnwrap(String assemblyName, 
                                              String typeName,
                                              Object[] activationAttributes)
        {
            ObjectHandle oh = CreateInstance(assemblyName, typeName, activationAttributes);
            if (oh == null)
                return null; 

            return oh.Unwrap();
        } // CreateInstanceAndUnwrap


        [Obsolete("Methods which use evidence to sandbox are obsolete and will be removed in a future release of the .NET Framework. Please use an overload of CreateInstanceAndUnwrap which does not take an Evidence parameter. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        public Object CreateInstanceAndUnwrap(String assemblyName, 
                                              String typeName, 
                                              bool ignoreCase,
                                              BindingFlags bindingAttr, 
                                              Binder binder,
                                              Object[] args,
                                              CultureInfo culture,
                                              Object[] activationAttributes,
                                              Evidence securityAttributes)
        {
#pragma warning disable 618
            ObjectHandle oh = CreateInstance(assemblyName, typeName, ignoreCase, bindingAttr,
                binder, args, culture, activationAttributes, securityAttributes);
#pragma warning restore 618

            if (oh == null)
                return null; 
            
            return oh.Unwrap();
        } // CreateInstanceAndUnwrap

        public object CreateInstanceAndUnwrap(string assemblyName,
                                              string typeName,
                                              bool ignoreCase,
                                              BindingFlags bindingAttr,
                                              Binder binder,
                                              object[] args,
                                              CultureInfo culture,
                                              object[] activationAttributes)
        {
            ObjectHandle oh = CreateInstance(assemblyName,
                                             typeName,
                                             ignoreCase,
                                             bindingAttr,
                                             binder,
                                             args,
                                             culture,
                                             activationAttributes);

            if (oh == null)
            {
                return null;
            }

            return oh.Unwrap();
        }

        // The first parameter should be named assemblyFile, but it was incorrectly named in a previous 
        //  release, and the compatibility police won't let us change the name now.
        public Object CreateInstanceFromAndUnwrap(String assemblyName,
                                                  String typeName)
        {
            ObjectHandle oh = CreateInstanceFrom(assemblyName, typeName);
            if (oh == null)
                return null;  

            return oh.Unwrap();                
        } // CreateInstanceAndUnwrap


        // The first parameter should be named assemblyFile, but it was incorrectly named in a previous 
        //  release, and the compatibility police won't let us change the name now.
        public Object CreateInstanceFromAndUnwrap(String assemblyName,
                                                  String typeName,
                                                  Object[] activationAttributes)
        {
            ObjectHandle oh = CreateInstanceFrom(assemblyName, typeName, activationAttributes);
            if (oh == null)
                return null; 

            return oh.Unwrap();
        } // CreateInstanceAndUnwrap


        // The first parameter should be named assemblyFile, but it was incorrectly named in a previous 
        //  release, and the compatibility police won't let us change the name now.
        [Obsolete("Methods which use evidence to sandbox are obsolete and will be removed in a future release of the .NET Framework. Please use an overload of CreateInstanceFromAndUnwrap which does not take an Evidence parameter. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        public Object CreateInstanceFromAndUnwrap(String assemblyName, 
                                                  String typeName, 
                                                  bool ignoreCase,
                                                  BindingFlags bindingAttr, 
                                                  Binder binder,
                                                  Object[] args,
                                                  CultureInfo culture,
                                                  Object[] activationAttributes,
                                                  Evidence securityAttributes)
        {
#pragma warning disable 618
            ObjectHandle oh = CreateInstanceFrom(assemblyName, typeName, ignoreCase, bindingAttr,
                binder, args, culture, activationAttributes, securityAttributes);
#pragma warning restore 618

            if (oh == null)
                return null; 

            return oh.Unwrap();
        } // CreateInstanceAndUnwrap

        public object CreateInstanceFromAndUnwrap(string assemblyFile,
                                                  string typeName,
                                                  bool ignoreCase,
                                                  BindingFlags bindingAttr,
                                                  Binder binder,
                                                  object[] args,
                                                  CultureInfo culture,
                                                  object[] activationAttributes)
        {
            ObjectHandle oh = CreateInstanceFrom(assemblyFile,
                                                 typeName,
                                                 ignoreCase,
                                                 bindingAttr,
                                                 binder,
                                                 args,
                                                 culture,
                                                 activationAttributes);
            if (oh == null)
            {
                return null;
            }
            
            return oh.Unwrap();
        }

        public Int32 Id
        {
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]  
            get {
                return GetId();
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]              
        internal extern Int32 GetId();
        
        internal const Int32 DefaultADID = 1;
        
        public bool IsDefaultAppDomain()
        {
            if (GetId()==DefaultADID)
                return true;
            return false;
        }

#if FEATURE_FUSION
        private static AppDomainSetup InternalCreateDomainSetup(String imageLocation)
        {
            int i = imageLocation.LastIndexOf('\\');
 
            Contract.Assert(i != -1, "invalid image location");

            AppDomainSetup info = new AppDomainSetup();
            info.ApplicationBase = imageLocation.Substring(0, i+1);

            StringBuilder config = new StringBuilder(imageLocation.Substring(i+1));
            config.Append(AppDomainSetup.ConfigurationExtension);
            info.ConfigurationFile = config.ToString();

            return info;
        }

        // Used by the validator for testing but not executing an assembly
        private static AppDomain InternalCreateDomain(String imageLocation)
        {
            AppDomainSetup info = InternalCreateDomainSetup(imageLocation);
            return CreateDomain("Validator",
                                null,
                                info);
        }

        [System.Security.SecurityCritical]  // auto-generated
        private void InternalSetDomainContext(String imageLocation)
        {
            SetupFusionStore(InternalCreateDomainSetup(imageLocation), null);
        }
#endif

#if FEATURE_APPDOMAIN_RESOURCE_MONITORING
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void nEnableMonitoring();

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern bool nMonitoringIsEnabled();

        // return -1 if ARM is not supported.
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern Int64 nGetTotalProcessorTime();

        // return -1 if ARM is not supported.
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern Int64 nGetTotalAllocatedMemorySize();

        // return -1 if ARM is not supported.
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern Int64 nGetLastSurvivedMemorySize();

        // return -1 if ARM is not supported.
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern Int64 nGetLastSurvivedProcessMemorySize();

        public static bool MonitoringIsEnabled
        {
            [System.Security.SecurityCritical]
            get {
                return nMonitoringIsEnabled();
            }
            
            [System.Security.SecurityCritical]
            set {
                if (value == false)
                {
                    throw new ArgumentException(Environment.GetResourceString("Arg_MustBeTrue"));
                }
                else
                {
                    nEnableMonitoring();
                }
            }
        }

        // Gets the total processor time for this AppDomain.
        // Throws NotSupportedException if ARM is not enabled.
        public TimeSpan MonitoringTotalProcessorTime 
        {
            [System.Security.SecurityCritical]
            get {
                Int64 i64ProcessorTime = nGetTotalProcessorTime();
                if (i64ProcessorTime == -1)
                {
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_WithoutARM"));
                }
                return new TimeSpan(i64ProcessorTime);
            }
        }

        // Gets the number of bytes allocated in this AppDomain since
        // the AppDomain was created.
        // Throws NotSupportedException if ARM is not enabled.
        public Int64 MonitoringTotalAllocatedMemorySize 
        {
            [System.Security.SecurityCritical]
            get {
                Int64 i64AllocatedMemory = nGetTotalAllocatedMemorySize();
                if (i64AllocatedMemory == -1)
                {
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_WithoutARM"));
                }
                return i64AllocatedMemory;
            }
        }

        // Gets the number of bytes survived after the last collection
        // that are known to be held by this AppDomain. After a full 
        // collection this number is accurate and complete. After an 
        // ephemeral collection this number is potentially incomplete.
        // Throws NotSupportedException if ARM is not enabled.
        public Int64 MonitoringSurvivedMemorySize
        {
            [System.Security.SecurityCritical]
            get {
                Int64 i64LastSurvivedMemory = nGetLastSurvivedMemorySize();
                if (i64LastSurvivedMemory == -1)
                {
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_WithoutARM"));
                }
                return i64LastSurvivedMemory;
            }
        }

        // Gets the total bytes survived from the last collection. After 
        // a full collection this number represents the number of the bytes 
        // being held live in managed heaps. (This number should be close 
        // to the number obtained from GC.GetTotalMemory for a full collection.)
        // After an ephemeral collection this number represents the number 
        // of bytes being held live in ephemeral generations.
        // Throws NotSupportedException if ARM is not enabled.
        public static Int64 MonitoringSurvivedProcessMemorySize
        {
            [System.Security.SecurityCritical]
            get {
                Int64 i64LastSurvivedProcessMemory = nGetLastSurvivedProcessMemorySize();
                if (i64LastSurvivedProcessMemory == -1)
                {
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_WithoutARM"));
                }
                return i64LastSurvivedProcessMemory;
            }
        }
#endif

#if !FEATURE_CORECLR
        // this method is required so Object.GetType is not made virtual by the compiler
        // _AppDomain.GetType()
        public new Type GetType()
        {
            return base.GetType();
        }

        void _AppDomain.GetTypeInfoCount(out uint pcTInfo)
        {
            throw new NotImplementedException();
        }

        void _AppDomain.GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo)
        {
            throw new NotImplementedException();
        }

        void _AppDomain.GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId)
        {
            throw new NotImplementedException();
        }

        // If you implement this method, make sure to include _AppDomain.Invoke in VM\DangerousAPIs.h and 
        // include _AppDomain in SystemDomain::IsReflectionInvocationMethod in AppDomain.cpp.
        void _AppDomain.Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr)
        {
            throw new NotImplementedException();
        }
#endif
    }

    //  CallBacks provide a facility to request execution of some code
    //  in another context/appDomain.
    //  CrossAppDomainDelegate type is defined for appdomain call backs. 
    //  The delegate used to request a callbak through the DoCallBack method
    //  must be of CrossContextDelegate type.
#if FEATURE_REMOTING    
[System.Runtime.InteropServices.ComVisible(true)]
    public delegate void CrossAppDomainDelegate();
#endif

    /// <summary>
    ///     Handle used to marshal an AppDomain to the VM (eg QCall). When marshaled via a QCall, the target
    ///     method in the VM will recieve a QCall::AppDomainHandle parameter.
    /// </summary>
    internal struct AppDomainHandle
    {
        private IntPtr m_appDomainHandle;

        // Note: generall an AppDomainHandle should not be directly constructed, instead the
        // code:System.AppDomain.GetNativeHandle method should be called to get the handle for a specific
        // AppDomain.
        internal AppDomainHandle(IntPtr domainHandle)
        {
            m_appDomainHandle = domainHandle;
        }
    }
}
