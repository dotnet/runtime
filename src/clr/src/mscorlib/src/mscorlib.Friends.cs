// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Runtime.CompilerServices;

// For now we are only moving to using this file over AssemblyAttributes.cspp in CoreSys, ideally we would move away from the centralized 
// AssemblyAttributes.cspp model for the other build types at a future point in time.
#if FEATURE_CORESYSTEM

// System and System.Core depend on all kinds of things.
[assembly: InternalsVisibleTo("System, PublicKey=" + _InternalsVisibleToKeys.SilverlightPlatformPublicKeyFull, AllInternalsVisible=false)]
[assembly: InternalsVisibleTo("System.Core, PublicKey=" + _InternalsVisibleToKeys.SilverlightPlatformPublicKeyFull, AllInternalsVisible=false)]

// Depends on things like SuppressUnmanagedCodeAttribute and WindowsRuntimeImportAttribute
[assembly: InternalsVisibleTo("System.Runtime.WindowsRuntime, PublicKey=" + _InternalsVisibleToKeys.EcmaPublicKeyFull, AllInternalsVisible=false)]

// Depends on WindowsRuntimeImportAttribute
[assembly: InternalsVisibleTo("System.Runtime.WindowsRuntime.UI.Xaml, PublicKey=" + _InternalsVisibleToKeys.EcmaPublicKeyFull, AllInternalsVisible=false)]

// Depends on things like FormattingServices and System.Reflection.Pointer
// For mango appcompat also needs access to System.Version..ctor() in order to deserialize Version 
[assembly: InternalsVisibleTo("System.Runtime.Serialization, PublicKey=" + _InternalsVisibleToKeys.SilverlightPlatformPublicKeyFull, AllInternalsVisible=false)]

// Depends on things like System.Number
[assembly: InternalsVisibleTo("System.Numerics, PublicKey=" + _InternalsVisibleToKeys.SilverlightPublicKeyFull, AllInternalsVisible=false)]

// Depends on things like internal constructor on TypeInfo as well as few other internal reflection members
[assembly: InternalsVisibleTo("System.Reflection.Context, PublicKey=" + _InternalsVisibleToKeys.SilverlightPlatformPublicKeyFull, AllInternalsVisible=false)]

// Depends on things like EncoderExptionFallback and EncoderFallback
[assembly: InternalsVisibleTo("System.Xml, PublicKey=" + _InternalsVisibleToKeys.SilverlightPlatformPublicKeyFull, AllInternalsVisible=false)]

// See ndp\fx\src\ReferenceAssemblies\WindowsPhone\v8.0\mscorlib\mscorlib.Private.cs for the exact set of internal API's
// System.Windows, System.Windows.RuntimeHost, and System.Net use.
[assembly: InternalsVisibleTo("System.Windows, PublicKey=" + _InternalsVisibleToKeys.SilverlightPlatformPublicKeyFull, AllInternalsVisible=false)]
[assembly: InternalsVisibleTo("System.Windows.RuntimeHost, PublicKey=" + _InternalsVisibleToKeys.SilverlightPlatformPublicKeyFull, AllInternalsVisible=false)]
[assembly: InternalsVisibleTo("System.Net, PublicKey=" + _InternalsVisibleToKeys.SilverlightPlatformPublicKeyFull, AllInternalsVisible=false)]

internal class _InternalsVisibleToKeys
{
  // Token = b77a5c561934e089
  internal const string EcmaPublicKeyFull = "00000000000000000400000000000000";
  
  // Token = b03f5f7f11d50a3a
  internal const string MicrosoftPublicKeyFull = "002400000480000094000000060200000024000052534131000400000100010007D1FA57C4AED9F0A32E84AA0FAEFD0DE9E8FD6AEC8F87FB03766C834C99921EB23BE79AD9D5DCC1DD9AD236132102900B723CF980957FC4E177108FC607774F29E8320E92EA05ECE4E821C0A5EFE8F1645C4C0C93C1AB99285D622CAA652C1DFAD63D745D6F2DE5F17E5EAF0FC4963D261C8A12436518206DC093344D5AD293";
  
  // Token = 7cec85d7bea7798e
  internal const string SilverlightPlatformPublicKeyFull= "00240000048000009400000006020000002400005253413100040000010001008D56C76F9E8649383049F383C44BE0EC204181822A6C31CF5EB7EF486944D032188EA1D3920763712CCB12D75FB77E9811149E6148E5D32FBAAB37611C1878DDC19E20EF135D0CB2CFF2BFEC3D115810C3D9069638FE4BE215DBF795861920E5AB6F7DB2E2CEEF136AC23D5DD2BF031700AEC232F6C6B1C785B4305C123B37AB";
  
  // Token = 31bf3856ad364e35 (also known as SharedLibPublicKey)
  internal const string SilverlightPublicKeyFull = "0024000004800000940000000602000000240000525341310004000001000100B5FC90E7027F67871E773A8FDE8938C81DD402BA65B9201D60593E96C492651E889CC13F1415EBB53FAC1131AE0BD333C5EE6021672D9718EA31A8AEBD0DA0072F25D87DBA6FC90FFD598ED4DA35E44C398C454307E8E33B8426143DAEC9F596836F97C8F74750E5975C64E2189F45DEF46B2A2B1247ADC3652BF5C308055DA9";
}

#endif // FEATURE_CORESYS
