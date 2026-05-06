// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )                         // .?_....:
  .ver 4:2:2:0
}
.assembly TestMethodDebugInformation
{
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                                   63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.

  // --- The following custom attribute is added automatically, do not uncomment -------
  //  .custom instance void [System.Runtime]System.Diagnostics.DebuggableAttribute::.ctor(valuetype [System.Runtime]System.Diagnostics.DebuggableAttribute/DebuggingModes) = ( 01 00 07 01 00 00 00 00 ) 

  .custom instance void [System.Runtime]System.Runtime.Versioning.TargetFrameworkAttribute::.ctor(string) = ( 01 00 18 2E 4E 45 54 43 6F 72 65 41 70 70 2C 56   // ....NETCoreApp,V
                                                                                                              65 72 73 69 6F 6E 3D 76 33 2E 31 01 00 54 0E 14   // ersion=v3.1..T..
                                                                                                              46 72 61 6D 65 77 6F 72 6B 44 69 73 70 6C 61 79   // FrameworkDisplay
                                                                                                              4E 61 6D 65 00 )                                  // Name.
  .custom instance void [System.Runtime]System.Reflection.AssemblyCompanyAttribute::.ctor(string) = ( 01 00 1A 54 65 73 74 4D 65 74 68 6F 64 44 65 62   // ...TestMethodDeb
                                                                                                      75 67 49 6E 66 6F 72 6D 61 74 69 6F 6E 00 00 )    // ugInformation..
  .custom instance void [System.Runtime]System.Reflection.AssemblyConfigurationAttribute::.ctor(string) = ( 01 00 05 44 65 62 75 67 00 00 )                   // ...Debug..
  .custom instance void [System.Runtime]System.Reflection.AssemblyFileVersionAttribute::.ctor(string) = ( 01 00 07 31 2E 30 2E 30 2E 30 00 00 )             // ...1.0.0.0..
  .custom instance void [System.Runtime]System.Reflection.AssemblyInformationalVersionAttribute::.ctor(string) = ( 01 00 05 31 2E 30 2E 30 00 00 )                   // ...1.0.0..
  .custom instance void [System.Runtime]System.Reflection.AssemblyProductAttribute::.ctor(string) = ( 01 00 1A 54 65 73 74 4D 65 74 68 6F 64 44 65 62   // ...TestMethodDeb
                                                                                                      75 67 49 6E 66 6F 72 6D 61 74 69 6F 6E 00 00 )    // ugInformation..
  .custom instance void [System.Runtime]System.Reflection.AssemblyTitleAttribute::.ctor(string) = ( 01 00 1A 54 65 73 74 4D 65 74 68 6F 64 44 65 62   // ...TestMethodDeb
                                                                                                    75 67 49 6E 66 6F 72 6D 61 74 69 6F 6E 00 00 )    // ugInformation..
  .hash algorithm 0x00008004
  .ver 1:0:0:0
}
.module TestMethodDebugInformation.dll
// MVID: {D57673DB-1B1D-4407-A641-E2495207E45F}
.imagebase 0x0000000180000000
.file alignment 0x00000200
.stackreserve 0x0000000000400000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x000002083D3E0000


// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi beforefieldinit TestMethodDebugInformation.SimpleMath
       extends [System.Runtime]System.Object
{
  .field private int32 _dummy
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(int32 dummy) cil managed
  {
    // Code size       16 (0x10)
    .maxstack  8
    .language '{3F5162F8-07C6-11D3-9053-00C04FA302A1}', '{994B45C4-E6E9-11D2-903F-00C04FA302A1}', '{5A869D0B-6611-11D3-BD2A-0000F80849BD}'
    .line 6,6 : 9,37 '/tmp/TestMethodDebugInformation/SimpleMath.cs'
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  nop
    .line 7,7 : 9,10 ''
    IL_0007:  nop
    .line 8,8 : 13,28 ''
    IL_0008:  ldarg.0
    IL_0009:  ldarg.1
    IL_000a:  stfld      int32 TestMethodDebugInformation.SimpleMath::_dummy
    .line 9,9 : 9,10 ''
    IL_000f:  ret
  } // end of method SimpleMath::.ctor

  .method public hidebysig instance int32 
          Pow(int32 n,
              int32 d) cil managed
  {
    // Code size       41 (0x29)
    .maxstack  2
    .locals init ([0] int32 i,
             [1] int32 j,
             [2] int32 res,
             [3] bool V_3,
             [4] int32 V_4)
    .line 6,6 : 9,10 '/tmp/TestMethodDebugInformation/SimpleMathMethods.cs'
    IL_0000:  nop
    .line 7,7 : 13,23 ''
    IL_0001:  ldc.i4.0
    IL_0002:  stloc.0
    .line 8,8 : 13,23 ''
    IL_0003:  ldc.i4.0
    IL_0004:  stloc.1
    .line 9,9 : 13,25 ''
    IL_0005:  ldc.i4.1
    IL_0006:  stloc.2
    .line 10,10 : 18,23 ''
    IL_0007:  ldc.i4.0
    IL_0008:  stloc.1
    .line 16707566,16707566 : 0,0 ''
    IL_0009:  br.s       IL_0019

    .line 11,11 : 13,14 ''
    IL_000b:  nop
    .line 12,12 : 17,26 ''
    IL_000c:  ldloc.2
    IL_000d:  ldarg.1
    IL_000e:  mul
    IL_000f:  stloc.2
    .line 13,13 : 17,21 ''
    IL_0010:  ldloc.0
    IL_0011:  ldc.i4.1
    IL_0012:  add
    IL_0013:  stloc.0
    .line 14,14 : 13,14 ''
    IL_0014:  nop
    .line 10,10 : 32,35 ''
    IL_0015:  ldloc.1
    IL_0016:  ldc.i4.1
    IL_0017:  add
    IL_0018:  stloc.1
    .line 10,10 : 25,30 ''
    IL_0019:  ldloc.1
    IL_001a:  ldarg.2
    IL_001b:  clt
    IL_001d:  stloc.3
    .line 16707566,16707566 : 0,0 ''
    IL_001e:  ldloc.3
    IL_001f:  brtrue.s   IL_000b

    .line 15,15 : 13,24 ''
    IL_0021:  ldloc.2
    IL_0022:  stloc.s    V_4
    IL_0024:  br.s       IL_0026

    .line 16,16 : 9,10 ''
    IL_0026:  ldloc.s    V_4
    IL_0028:  ret
  } // end of method SimpleMath::Pow

} // end of class TestMethodDebugInformation.SimpleMath


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file .\..\..\TestFiles\TestMethodDebugInformation_win.res
