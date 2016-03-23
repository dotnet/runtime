// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Interface:  IAppDomain
** 
** 
**
**
** Purpose: Properties and methods exposed to COM
**
** 
===========================================================*/
namespace System {
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using SecurityManager = System.Security.SecurityManager;
    using System.Security.Permissions;
    using IEvidenceFactory = System.Security.IEvidenceFactory;
#if FEATURE_IMPERSONATION
    using System.Security.Principal;
#endif
    using System.Security.Policy;
    using System.Security;
    using System.Security.Util;
    using System.Collections;
    using System.Text;
    using System.Configuration.Assemblies;
    using System.Threading;
    using System.Runtime.InteropServices;
    using System.Runtime.Remoting;
    using System.Reflection.Emit;
    using CultureInfo = System.Globalization.CultureInfo;
    using System.IO;
    using System.Runtime.Versioning;

    [GuidAttribute("05F696DC-2B29-3663-AD8B-C4389CF2A713")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [CLSCompliant(false)]
[System.Runtime.InteropServices.ComVisible(true)]
    public interface _AppDomain
    {
#if !FEATURE_CORECLR
        void GetTypeInfoCount(out uint pcTInfo);

        void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);

        void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);

        void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);

        String ToString();

        bool Equals (Object other);

        int GetHashCode ();

        Type GetType ();

#if FEATURE_REMOTING        
        [System.Security.SecurityCritical]  // auto-generated_required
        Object InitializeLifetimeService ();

        [System.Security.SecurityCritical]  // auto-generated_required
        Object GetLifetimeService ();
#endif // FEATURE_REMOTING        

#if FEATURE_CAS_POLICY
        Evidence Evidence { get; }
#endif // FEATURE_CAS_POLICY
        event EventHandler DomainUnload;

        [method:System.Security.SecurityCritical]
        event AssemblyLoadEventHandler AssemblyLoad;

        event EventHandler ProcessExit;

        [method:System.Security.SecurityCritical]
        event ResolveEventHandler TypeResolve;

        [method:System.Security.SecurityCritical]
        event ResolveEventHandler ResourceResolve;

        [method:System.Security.SecurityCritical]
        event ResolveEventHandler AssemblyResolve;

        [method:System.Security.SecurityCritical]
        event UnhandledExceptionEventHandler UnhandledException;

        AssemblyBuilder DefineDynamicAssembly(AssemblyName            name,
                                              AssemblyBuilderAccess   access);

        AssemblyBuilder DefineDynamicAssembly(AssemblyName            name,
                                              AssemblyBuilderAccess   access,
                                              String                  dir);

        AssemblyBuilder DefineDynamicAssembly(AssemblyName            name,
                                              AssemblyBuilderAccess   access,
                                              Evidence                evidence);

        AssemblyBuilder DefineDynamicAssembly(AssemblyName            name,
                                              AssemblyBuilderAccess   access,
                                              PermissionSet           requiredPermissions,
                                              PermissionSet           optionalPermissions,
                                              PermissionSet           refusedPermissions);

        AssemblyBuilder DefineDynamicAssembly(AssemblyName            name,
                                              AssemblyBuilderAccess   access,
                                              String                  dir,
                                              Evidence                evidence);

        AssemblyBuilder DefineDynamicAssembly(AssemblyName            name,
                                              AssemblyBuilderAccess   access,
                                              String                  dir,
                                              PermissionSet           requiredPermissions,
                                              PermissionSet           optionalPermissions,
                                              PermissionSet           refusedPermissions);

        AssemblyBuilder DefineDynamicAssembly(AssemblyName            name,
                                              AssemblyBuilderAccess   access,
                                              Evidence                evidence,
                                              PermissionSet           requiredPermissions,
                                              PermissionSet           optionalPermissions,
                                              PermissionSet           refusedPermissions);

        AssemblyBuilder DefineDynamicAssembly(AssemblyName            name,
                                              AssemblyBuilderAccess   access,
                                              String                  dir,
                                              Evidence                evidence,
                                              PermissionSet           requiredPermissions,
                                              PermissionSet           optionalPermissions,
                                              PermissionSet           refusedPermissions);

        AssemblyBuilder DefineDynamicAssembly(AssemblyName            name,
                                              AssemblyBuilderAccess   access,
                                              String                  dir,
                                              Evidence                evidence,
                                              PermissionSet           requiredPermissions,
                                              PermissionSet           optionalPermissions,
                                              PermissionSet           refusedPermissions,
                                              bool                    isSynchronized);

        ObjectHandle CreateInstance(String assemblyName,
                                    String typeName);

                                         
        ObjectHandle CreateInstanceFrom(String assemblyFile,
                                        String typeName);

                                         
        ObjectHandle CreateInstance(String assemblyName,
                                    String typeName,
                                    Object[] activationAttributes);

        ObjectHandle CreateInstanceFrom(String assemblyFile,
                                        String typeName,
                                        Object[] activationAttributes);

       ObjectHandle CreateInstance(String assemblyName, 
                                   String typeName, 
                                   bool ignoreCase,
                                   BindingFlags bindingAttr, 
                                   Binder binder,
                                   Object[] args,
                                    CultureInfo culture,
                                   Object[] activationAttributes,
                                   Evidence securityAttributes);

       ObjectHandle CreateInstanceFrom(String assemblyFile,
                                       String typeName, 
                                       bool ignoreCase,
                                       BindingFlags bindingAttr, 
                                       Binder binder,
                                        Object[] args,
                                       CultureInfo culture,
                                       Object[] activationAttributes,
                                       Evidence securityAttributes);

        Assembly Load(AssemblyName assemblyRef);

        Assembly Load(String assemblyString);

        Assembly Load(byte[] rawAssembly);

        Assembly Load(byte[] rawAssembly,
                      byte[] rawSymbolStore);

        Assembly Load(byte[] rawAssembly,
                      byte[] rawSymbolStore,
                      Evidence securityEvidence);

        Assembly Load(AssemblyName assemblyRef, 
                      Evidence assemblySecurity);     

        Assembly Load(String assemblyString, 
                      Evidence assemblySecurity);

        int ExecuteAssembly(String assemblyFile, 
                            Evidence assemblySecurity);

        int ExecuteAssembly(String assemblyFile);

        int ExecuteAssembly(String assemblyFile, 
                            Evidence assemblySecurity, 
                            String[] args);

        String FriendlyName
        { get; }
#if FEATURE_FUSION
        String BaseDirectory
        {
            get;
        }

        String RelativeSearchPath
        { get; }

        bool ShadowCopyFiles
        { get; }
#endif
        Assembly[] GetAssemblies();
#if FEATURE_FUSION
        [System.Security.SecurityCritical]  // auto-generated_required
        void AppendPrivatePath(String path);

        [System.Security.SecurityCritical]  // auto-generated_required
        void ClearPrivatePath();

        [System.Security.SecurityCritical]  // auto-generated_required
        void SetShadowCopyPath (String s);

        [System.Security.SecurityCritical]  // auto-generated_required
        void ClearShadowCopyPath ( );

        [System.Security.SecurityCritical]  // auto-generated_required
        void SetCachePath (String s);
#endif //FEATURE_FUSION
        [System.Security.SecurityCritical]  // auto-generated_required
        void SetData(String name, Object data);

        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        Object GetData(string name);

#if FEATURE_CAS_POLICY        
        [System.Security.SecurityCritical]  // auto-generated_required
        void SetAppDomainPolicy(PolicyLevel domainPolicy);

#if FEATURE_IMPERSONATION
        void SetThreadPrincipal(IPrincipal principal);
#endif // FEATURE_IMPERSONATION

        void SetPrincipalPolicy(PrincipalPolicy policy);
#endif

#if FEATURE_REMOTING
        void DoCallBack(CrossAppDomainDelegate theDelegate);
#endif

        String DynamicDirectory
        { get; }
#endif
    }
}

