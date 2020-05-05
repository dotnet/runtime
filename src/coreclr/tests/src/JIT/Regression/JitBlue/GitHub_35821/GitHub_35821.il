
//  Microsoft (R) .NET IL Disassembler.  Version 5.0.0-dev



// Metadata version: v4.0.30319
.assembly extern System.Runtime
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )                         // .?_....:
  .ver 5:0:0:0
}
.assembly extern System.Runtime.Intrinsics
{
  .publickeytoken = (CC 7B 13 FF CD 2D DD 51 )                         // .{...-.Q
  .ver 5:0:0:0
}
.assembly projs
{
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                                   63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.

  // --- The following custom attribute is added automatically, do not uncomment -------
  //  .custom instance void [System.Runtime]System.Diagnostics.DebuggableAttribute::.ctor(valuetype [System.Runtime]System.Diagnostics.DebuggableAttribute/DebuggingModes) = ( 01 00 02 00 00 00 00 00 ) 

  .custom instance void [System.Runtime]System.Runtime.Versioning.TargetFrameworkAttribute::.ctor(string) = ( 01 00 18 2E 4E 45 54 43 6F 72 65 41 70 70 2C 56   // ....NETCoreApp,V
                                                                                                              65 72 73 69 6F 6E 3D 76 35 2E 30 01 00 54 0E 14   // ersion=v5.0..T..
                                                                                                              46 72 61 6D 65 77 6F 72 6B 44 69 73 70 6C 61 79   // FrameworkDisplay
                                                                                                              4E 61 6D 65 00 )                                  // Name.
  .custom instance void [System.Runtime]System.Reflection.AssemblyCompanyAttribute::.ctor(string) = ( 01 00 05 70 72 6F 6A 73 00 00 )                   // ...projs..
  .custom instance void [System.Runtime]System.Reflection.AssemblyConfigurationAttribute::.ctor(string) = ( 01 00 07 52 65 6C 65 61 73 65 00 00 )             // ...Release..
  .custom instance void [System.Runtime]System.Reflection.AssemblyFileVersionAttribute::.ctor(string) = ( 01 00 07 31 2E 30 2E 30 2E 30 00 00 )             // ...1.0.0.0..
  .custom instance void [System.Runtime]System.Reflection.AssemblyInformationalVersionAttribute::.ctor(string) = ( 01 00 05 31 2E 30 2E 30 00 00 )                   // ...1.0.0..
  .custom instance void [System.Runtime]System.Reflection.AssemblyProductAttribute::.ctor(string) = ( 01 00 05 70 72 6F 6A 73 00 00 )                   // ...projs..
  .custom instance void [System.Runtime]System.Reflection.AssemblyTitleAttribute::.ctor(string) = ( 01 00 05 70 72 6F 6A 73 00 00 )                   // ...projs..
  .permissionset reqmin
             = {[System.Runtime]System.Security.Permissions.SecurityPermissionAttribute = {property bool 'SkipVerification' = bool(true)}}
  .hash algorithm 0x00008004
  .ver 1:0:0:0
}
.module projs.dll
// MVID: {068BB2C4-ED08-496E-A30C-4B131CAC4B27}
.custom instance void [System.Runtime]System.Security.UnverifiableCodeAttribute::.ctor() = ( 01 00 00 00 ) 
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x000001E655FA0000


// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi sealed beforefieldinit projs.PopCountTester
       extends [System.Runtime]System.Object
{
  .method private hidebysig static void  Main(string[] args) cil managed
  {
    .entrypoint
    // Code size       46 (0x2e)
    .maxstack  8
    IL_0000:  ldc.i4.s   23
    IL_0002:  conv.i8
    IL_0003:  call       valuetype [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Vector64`1<uint64> [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Vector64::Create(uint64)
    IL_0008:  call       void projs.PopCountTester::Test1(valuetype [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Vector64`1<uint64>)
    IL_000d:  ldc.i4.s   23
    IL_000f:  conv.i8
    IL_0010:  call       valuetype [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Vector64`1<int64> [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Vector64::Create(int64)
    IL_0015:  call       void projs.PopCountTester::Test2(valuetype [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Vector64`1<int64>)
    IL_001a:  ldc.r8     23.
    IL_0023:  call       valuetype [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Vector64`1<float64> [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Vector64::Create(float64)
    IL_0028:  call       void projs.PopCountTester::Test3(valuetype [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Vector64`1<float64>)
    IL_002d:  ret
  } // end of method PopCountTester::Main

  .method public hidebysig static void  Test1(valuetype [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Vector64`1<uint64> data) cil managed noinlining
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method PopCountTester::Test1

  .method public hidebysig static void  Test2(valuetype [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Vector64`1<int64> data) cil managed noinlining
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method PopCountTester::Test2

  .method public hidebysig static void  Test3(valuetype [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Vector64`1<float64> data) cil managed noinlining
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method PopCountTester::Test3

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  ret
  } // end of method PopCountTester::.ctor

} // end of class projs.PopCountTester


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
