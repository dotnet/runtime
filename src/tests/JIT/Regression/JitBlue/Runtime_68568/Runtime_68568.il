// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Metadata version: v4.0.30319
.assembly extern System.Runtime
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )                         // .?_....:
  .ver 7:0:0:0
}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )                         // .?_....:
  .ver 7:0:0:0
}
.assembly playground
{
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 )
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                                   63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.

  // --- The following custom attribute is added automatically, do not uncomment -------
  //  .custom instance void [System.Runtime]System.Diagnostics.DebuggableAttribute::.ctor(valuetype [System.Runtime]System.Diagnostics.DebuggableAttribute/DebuggingModes) = ( 01 00 02 00 00 00 00 00 )

  .custom instance void [System.Runtime]System.Runtime.Versioning.TargetFrameworkAttribute::.ctor(string) = ( 01 00 18 2E 4E 45 54 43 6F 72 65 41 70 70 2C 56   // ....NETCoreApp,V
                                                                                                              65 72 73 69 6F 6E 3D 76 37 2E 30 01 00 54 0E 14   // ersion=v7.0..T..
                                                                                                              46 72 61 6D 65 77 6F 72 6B 44 69 73 70 6C 61 79   // FrameworkDisplay
                                                                                                              4E 61 6D 65 00 )                                  // Name.
  .custom instance void [System.Runtime]System.Reflection.AssemblyCompanyAttribute::.ctor(string) = ( 01 00 0A 70 6C 61 79 67 72 6F 75 6E 64 00 00 )    // ...playground..
  .custom instance void [System.Runtime]System.Reflection.AssemblyConfigurationAttribute::.ctor(string) = ( 01 00 07 52 65 6C 65 61 73 65 00 00 )             // ...Release..
  .custom instance void [System.Runtime]System.Reflection.AssemblyFileVersionAttribute::.ctor(string) = ( 01 00 07 31 2E 30 2E 30 2E 30 00 00 )             // ...1.0.0.0..
  .custom instance void [System.Runtime]System.Reflection.AssemblyInformationalVersionAttribute::.ctor(string) = ( 01 00 05 31 2E 30 2E 30 00 00 )                   // ...1.0.0..
  .custom instance void [System.Runtime]System.Reflection.AssemblyProductAttribute::.ctor(string) = ( 01 00 0A 70 6C 61 79 67 72 6F 75 6E 64 00 00 )    // ...playground..
  .custom instance void [System.Runtime]System.Reflection.AssemblyTitleAttribute::.ctor(string) = ( 01 00 0A 70 6C 61 79 67 72 6F 75 6E 64 00 00 )    // ...playground..
  .custom instance void [System.Runtime]System.Runtime.Versioning.RequiresPreviewFeaturesAttribute::.ctor() = ( 01 00 00 00 )
  .permissionset reqmin
             = {class 'System.Security.Permissions.SecurityPermissionAttribute, System.Runtime, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a' = {property bool 'SkipVerification' = bool(true)}}
  .hash algorithm 0x00008004
  .ver 1:0:0:0
}
.module playground.dll
// MVID: {1DAF595C-D7E2-42F2-A742-8EC64C4236DD}
.custom instance void [System.Runtime]System.Security.UnverifiableCodeAttribute::.ctor() = ( 01 00 00 00 )
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x000001B5504A0000


// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi beforefieldinit Runtime_68568
       extends [System.Runtime]System.Object
{
  .method public hidebysig static int32  Main() cil managed
  {
    .entrypoint
    // Code size       79 (0x4f)
    .maxstack  2
    .locals init (bool V_0)

// Code equivalent to the following, except without the
// int cast that C# adds on switch cases.
//
// switch (unchecked((nuint)0x100000000))
// {
//     case 0:
//         pass = sizeof(nint) == 4;
//         break;
//     case 1:
//         pass = false;
//         break;
//     default:
//         pass = sizeof(nint) == 8;
//         break;
// }
//
// if (pass)
// {
//     Console.WriteLine("PASS");
//     return 100;
// }
//
// Console.WriteLine("FAIL");
// return -1;


    IL_0000:  ldc.i4.0
    IL_0001:  stloc.0
    IL_0002:  ldc.i8     0x100000000
    IL_000b:  conv.u
    IL_000c:  switch     (
                          IL_0025,
                          IL_0031)
    IL_0019:  sizeof     [System.Runtime]System.IntPtr
    IL_001f:  ldc.i4.8
    IL_0020:  ceq
    IL_0022:  stloc.0
    IL_0023:  br.s       IL_0033

    IL_0025:  sizeof     [System.Runtime]System.IntPtr
    IL_002b:  ldc.i4.4
    IL_002c:  ceq
    IL_002e:  stloc.0
    IL_002f:  br.s       IL_0033

    IL_0031:  ldc.i4.0
    IL_0032:  stloc.0
    IL_0033:  ldloc.0
    IL_0034:  brfalse.s  IL_0043

    IL_0036:  ldstr      "PASS"
    IL_003b:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0040:  ldc.i4.s   100
    IL_0042:  ret

    IL_0043:  ldstr      "FAIL"
    IL_0048:  call       void [System.Console]System.Console::WriteLine(string)
    IL_004d:  ldc.i4.m1
    IL_004e:  ret
  } // end of method Runtime_68568::Main

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Runtime_68568::.ctor

} // end of class Runtime_68568


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
