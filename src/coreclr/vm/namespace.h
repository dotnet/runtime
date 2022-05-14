// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// This file defines namespaces used by the runtime.
//
// Note: This file gets parsed by the Mono IL Linker (https://github.com/mono/linker/) which may throw an exception during parsing.
// Specifically, this (https://github.com/mono/linker/blob/main/corebuild/integration/ILLink.Tasks/CreateRuntimeRootDescriptorFile.cs) will try to
// parse this header, and it may throw an exception while doing that. If you edit this file and get a build failure on msbuild.exe D:\repos\coreclr\build.proj
// you might want to check out the parser linked above.
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
#define g_InternalInteropServicesNS  "Internal.Runtime.InteropServices"
#define g_ReflectionNS      g_SystemNS ".Reflection"
#define g_ReflectionEmitNS  g_ReflectionNS ".Emit"

#define g_InteropNS         g_RuntimeNS ".InteropServices"
#define g_ObjectiveCNS      g_InteropNS ".ObjectiveC"
#define g_MarshallingNS     g_InteropNS ".Marshalling"

#define g_IntrinsicsNS g_RuntimeNS ".Intrinsics"
#define g_NumericsNS   g_SystemNS  ".Numerics"

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

#define g_ExceptionServicesNS         g_RuntimeNS ".ExceptionServices"

#define g_LoaderNS         g_RuntimeNS ".Loader"
