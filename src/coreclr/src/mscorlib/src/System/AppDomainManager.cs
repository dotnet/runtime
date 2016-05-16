// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
// An AppDomainManager gives a hosting application the chance to 
// participate in the creation and control the settings of new AppDomains.
//

namespace System {
    using System.Collections;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Security;
    using System.Security.Permissions;
    using System.Security.Policy;
    using System.Threading;
#if FEATURE_CLICKONCE
    using System.Runtime.Hosting;
#endif
    using System.Runtime.Versioning;
    using System.Runtime.InteropServices;
    using System.Diagnostics.Contracts;

#if FEATURE_APPDOMAINMANAGER_INITOPTIONS
    [Flags]
    [System.Runtime.InteropServices.ComVisible(true)]
    public enum AppDomainManagerInitializationOptions {
        None             = 0x0000,
        RegisterWithHost = 0x0001
    }
#endif // FEATURE_APPDOMAINMANAGER_INITOPTIONS

    [System.Security.SecurityCritical]  // auto-generated_required
    [System.Runtime.InteropServices.ComVisible(true)]
#if !FEATURE_CORECLR
    [SecurityPermissionAttribute(SecurityAction.InheritanceDemand, Flags = SecurityPermissionFlag.Infrastructure)]
#endif
#if FEATURE_REMOTING
    public class AppDomainManager : MarshalByRefObject {
#else // FEATURE_REMOTING
    public class AppDomainManager {
#endif // FEATURE_REMOTING
        public AppDomainManager () {}
#if FEATURE_REMOTING
        [System.Security.SecurityCritical]  // auto-generated
        public virtual AppDomain CreateDomain (string friendlyName,
                                               Evidence securityInfo,
                                               AppDomainSetup appDomainInfo) {
            return CreateDomainHelper(friendlyName, securityInfo, appDomainInfo);
        }

        [System.Security.SecurityCritical]  // auto-generated_required
        [SecurityPermissionAttribute(SecurityAction.Demand, ControlAppDomain = true)]
        protected static AppDomain CreateDomainHelper (string friendlyName,
                                                       Evidence securityInfo,
                                                       AppDomainSetup appDomainInfo) {
            if (friendlyName == null)
                throw new ArgumentNullException("friendlyName", Environment.GetResourceString("ArgumentNull_String"));

            Contract.EndContractBlock();
            // If evidence is provided, we check to make sure that is allowed.
            if (securityInfo != null) {
                new SecurityPermission(SecurityPermissionFlag.ControlEvidence).Demand();

                // Check the evidence to ensure that if it expects a sandboxed domain, it actually gets one.
                AppDomain.CheckDomainCreationEvidence(appDomainInfo, securityInfo);
            }

            if (appDomainInfo == null) {
                appDomainInfo = new AppDomainSetup();
            }

            // If there was no specified AppDomainManager for the new domain, default it to being the same
            // as the current domain's AppDomainManager.
            if (appDomainInfo.AppDomainManagerAssembly == null || appDomainInfo.AppDomainManagerType == null) {
                string inheritedDomainManagerAssembly;
                string inheritedDomainManagerType;

                AppDomain.CurrentDomain.GetAppDomainManagerType(out inheritedDomainManagerAssembly,
                                                                out inheritedDomainManagerType);

                if (appDomainInfo.AppDomainManagerAssembly == null) {
                    appDomainInfo.AppDomainManagerAssembly = inheritedDomainManagerAssembly;
                }
                if (appDomainInfo.AppDomainManagerType == null) {
                    appDomainInfo.AppDomainManagerType = inheritedDomainManagerType;
                }
            }

            // If there was no specified TargetFrameworkName for the new domain, default it to the current domain's.
            if (appDomainInfo.TargetFrameworkName == null)
                appDomainInfo.TargetFrameworkName = AppDomain.CurrentDomain.GetTargetFrameworkName();

            return AppDomain.nCreateDomain(friendlyName,
                                           appDomainInfo,
                                           securityInfo,
                                           securityInfo == null ? AppDomain.CurrentDomain.InternalEvidence : null,
                                           AppDomain.CurrentDomain.GetSecurityDescriptor());
        }
#endif // FEATURE_REMOTING

        [System.Security.SecurityCritical]
        public virtual void InitializeNewDomain (AppDomainSetup appDomainInfo) {
            // By default, InitializeNewDomain does nothing. AppDomain.CreateAppDomainManager relies on this fact.
        }

#if FEATURE_APPDOMAINMANAGER_INITOPTIONS

        private AppDomainManagerInitializationOptions m_flags = AppDomainManagerInitializationOptions.None;
        public AppDomainManagerInitializationOptions InitializationFlags {
            get {
                return m_flags;
            }
            set {
                m_flags = value;
            }
        }
#endif // FEATURE_APPDOMAINMANAGER_INITOPTIONS

#if FEATURE_CLICKONCE
        private ApplicationActivator m_appActivator = null;
        public virtual ApplicationActivator ApplicationActivator {
            get {
                if (m_appActivator == null)
                    m_appActivator = new ApplicationActivator();
                return m_appActivator;
            }
        }
#endif //#if FEATURE_CLICKONCE

#if FEATURE_CAS_POLICY
        public virtual HostSecurityManager HostSecurityManager {
            get {
                return null;
            }
        }

        public virtual HostExecutionContextManager HostExecutionContextManager {
            get {
                // By default, the AppDomainManager returns the HostExecutionContextManager.
                return HostExecutionContextManager.GetInternalHostExecutionContextManager();
            }
        }
#endif // FEATURE_CAS_POLICY

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurity]
        private static extern void GetEntryAssembly(ObjectHandleOnStack retAssembly);

        private Assembly m_entryAssembly = null;
        public virtual Assembly EntryAssembly {
            [System.Security.SecurityCritical]  // auto-generated
            get {
                // The default AppDomainManager sets the EntryAssembly depending on whether the
                // AppDomain is a manifest application domain or not. In the first case, we parse
                // the application manifest to find out the entry point assembly and return that assembly.
                // In the second case, we maintain the old behavior by calling GetEntryAssembly().
                if (m_entryAssembly == null)
                {

#if FEATURE_CLICKONCE
                    AppDomain domain = AppDomain.CurrentDomain;
                    if (domain.IsDefaultAppDomain() && domain.ActivationContext != null) {
                        ManifestRunner runner = new ManifestRunner(domain, domain.ActivationContext);
                        m_entryAssembly = runner.EntryAssembly;
                    } else
#endif //#if FEATURE_CLICKONCE
                    {
                        RuntimeAssembly entryAssembly = null;
                        GetEntryAssembly(JitHelpers.GetObjectHandleOnStack(ref entryAssembly));
                        m_entryAssembly = entryAssembly;
                    }
                }
                return m_entryAssembly;
            }
        }

        internal static AppDomainManager CurrentAppDomainManager {
            [System.Security.SecurityCritical]  // auto-generated
            get {
                return AppDomain.CurrentDomain.DomainManager;
            }
        }

        public virtual bool CheckSecuritySettings (SecurityState state)
        {
            return false;
        }

#if FEATURE_APPDOMAINMANAGER_INITOPTIONS
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern bool HasHost();

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity]
        private static extern void RegisterWithHost(IntPtr appDomainManager);

        internal void RegisterWithHost() {
            if (HasHost()) {
                IntPtr punkAppDomainManager = IntPtr.Zero;

                RuntimeHelpers.PrepareConstrainedRegions();
                try {
                    punkAppDomainManager = Marshal.GetIUnknownForObject(this);
                    RegisterWithHost(punkAppDomainManager);
                }
                finally {
                    if (!punkAppDomainManager.IsNull()) {
                        Marshal.Release(punkAppDomainManager);
                    }
                }
            }
        }
#endif // FEATURE_APPDOMAINMANAGER_INITOPTIONS
    }
}
