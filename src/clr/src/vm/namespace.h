// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// This file defines namespaces used by the runtime.
//



#define g_SystemNS          "System"

#define g_RuntimeNS         g_SystemNS ".Runtime"
#define g_IONS              g_SystemNS ".IO"
#define g_ThreadingNS       g_SystemNS ".Threading"
#define g_CollectionsNS     g_SystemNS ".Collections"
#define g_ResourcesNS       g_SystemNS ".Resources"
#define g_DiagnosticsNS     g_SystemNS ".Diagnostics"
#define g_CodeContractsNS   g_DiagnosticsNS ".Contracts"
#define g_AssembliesNS      g_SystemNS ".Configuration.Assemblies"
#define g_GlobalizationNS   g_SystemNS ".Globalization"
#define g_IsolatedStorageNS g_SystemNS ".IO.IsolatedStorage"
#define g_TextNS            g_SystemNS ".Text"
#define g_CollectionsGenericNS g_SystemNS ".Collections.Generic"

#define g_InteropServicesNS g_SystemNS ".Runtime.InteropServices"
#define g_ReflectionNS      g_SystemNS ".Reflection"
#define g_ReflectionEmitNS  g_ReflectionNS ".Emit"

#define g_InteropNS         g_RuntimeNS ".InteropServices"
#define g_InteropTCENS      g_InteropNS ".TCEAdapterGen"
#define g_ExpandoNS         g_InteropNS ".Expando"
#ifdef FEATURE_COMINTEROP
#define g_WinRTNS           g_InteropNS ".WindowsRuntime"
#endif // FEATURE_COMINTEROP

#define g_IntrinsicsNS g_RuntimeNS ".Intrinsics"

#define g_InternalCompilerServicesNS "Internal.Runtime.CompilerServices"
#define g_CompilerServicesNS g_RuntimeNS ".CompilerServices"

#define g_ConstrainedExecutionNS g_RuntimeNS ".ConstrainedExecution"

#define g_SecurityNS        g_SystemNS ".Security"
#define g_UtilNS            g_SecurityNS ".Util"
#define g_PublicKeyNS       g_SecurityNS ".PublicKey"
#define g_PermissionsNS     g_SecurityNS ".Permissions"
#define g_PrincipalNS       g_SecurityNS ".Principal"
#define g_PolicyNS          g_SecurityNS ".Policy"
#define g_CryptographyNS    g_SecurityNS ".Cryptography"
#define g_X509NS            g_CryptographyNS ".X509Certificates"

#define g_SerializationNS   g_RuntimeNS ".Serialization"
#define g_RemotingNS        g_RuntimeNS ".Remoting"
#define g_ActivationNS      g_RemotingNS ".Activation"
#define g_ProxiesNS         g_RemotingNS ".Proxies"
#define g_ContextsNS        g_RemotingNS ".Contexts"
#define g_MessagingNS       g_RemotingNS ".Messaging"
#define g_RemotingServicesNS g_RemotingNS ".Services"
#define g_LifetimeNS        g_RemotingNS ".Lifetime"

#define g_MicrosoftNS       "Microsoft"

#define g_Win32NS           g_MicrosoftNS ".Win32"         
#define g_SafeHandlesNS  g_Win32NS ".SafeHandles"

#define g_StubHelpersNS     g_SystemNS ".StubHelpers"

// Jupiter support requires accessing types in the System.Windows namespace & children,
// but these types may show up in the Windows.UI.Xaml namespace.
#define g_SysWindowsNS      g_SystemNS ".Windows"

#define g_DirectUINS        "Windows.UI.Xaml"
#define g_AutomationNS      g_DirectUINS ".Automation"
#define g_MarkupNS          g_DirectUINS ".Markup"

#define g_WindowsFoundationDiagNS    "Windows.Foundation.Diagnostics"

#define g_ExceptionServicesNS         g_RuntimeNS ".ExceptionServices"

#define g_LoaderNS         g_RuntimeNS ".Loader" 
