// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}





// Metadata version: v2.0.20609
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 2:0:0:0
}
.assembly spill
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module spill.exe
// MVID: {7C50D91F-9C85-49FE-8B5A-900DF6F8E2CD}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x02570000


// =============== CLASS MEMBERS DECLARATION ===================

.class private auto ansi beforefieldinit MainApp
       extends [mscorlib]System.Object
{
  .field private static int32 s
  .method public hidebysig static int32  Foo(int32 i) cil managed
  {
    // Code size       5 (0x5)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.s   10
    IL_0003:  mul
    IL_0004:  ret
  } // end of method MainApp::Foo

  .method public hidebysig static int32 Main() cil managed
  {
    .entrypoint
    // Code size       44 (0x2c)
    .maxstack  3
    .locals init (int32 V_0,
             int32 V_1)
    IL_0000:  ldsfld     int32 MainApp::s
    IL_0005:  stloc.0

    IL_000a:  ldloc.0
    IL_000b:  call       int32 MainApp::Foo(int32)

    // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
    // These four lines have been moved.
    
    IL_0006:  ldloc.0
    IL_0007:  ldc.i4.1
    IL_0008:  add
    IL_0009:  stloc.0
    // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

    IL_0010:  stloc.1
    IL_0011:  ldloc.1
    IL_0012:  ldc.i4.s   10
    IL_0014:  bne.un.s   IL_0021

    IL_0016:  ldstr      "Pass because b==10"
    IL_001b:  call       void [System.Console]System.Console::WriteLine(string)
	      ldc.i4 100
    IL_0020:  ret

    IL_0021:  ldstr      "Fail because b!= 10"
    IL_0026:  call       void [System.Console]System.Console::WriteLine(string)
	      ldc.i4 101
    IL_002b:  ret
  } // end of method MainApp::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method MainApp::.ctor

  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldc.i4.1
    IL_0001:  stsfld     int32 MainApp::s
    IL_0006:  ret
  } // end of method MainApp::.cctor

} // end of class MainApp


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file spill.res
