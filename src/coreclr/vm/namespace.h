// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// This file defines namespaces used by the runtime.
//
// Note: This file gets parsed by the Mono IL Linker (https://github.com/dotnet/runtime/tree/main/src/tools/illink) which may throw an exception during parsing.
// Specifically, this (https://github.com/dotnet/runtime/blob/main/src/tools/illink/src/ILLink.Tasks/CreateRuntimeRootDescriptorFile.cs) will try to
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
#define g_GlobalizationNS   g_SystemNS ".Globalization"
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
#define g_CryptographyNS    g_SecurityNS ".Cryptography"
#define g_SerializationNS   g_RuntimeNS ".Serialization"

#define g_MicrosoftNS       "Microsoft"

#define g_Win32NS           g_MicrosoftNS ".Win32"
#define g_SafeHandlesNS  g_Win32NS ".SafeHandles"

#define g_StubHelpersNS     g_SystemNS ".StubHelpers"

#define g_ExceptionServicesNS         g_RuntimeNS ".ExceptionServices"

#define g_LoaderNS         g_RuntimeNS ".Loader"
