// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}











// Metadata version: v2.0.40621
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 2:0:3600:0
}
.assembly 'test1_333007'
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
// MVID: {13BD850E-1B6A-467D-8081-5F9C3901672A}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x0000000003300000


// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi beforefieldinit Test_test1
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       22 (0x16)
    .maxstack  8

    IL_0000:  ldc.i4.0
    IL_0001:  call       int32 Test_test1::sw(int32)
    IL_0006:  pop

    IL_0007:  ldc.i4.0
    IL_0008:  call       int32 Test_test1::sw(int32)

    IL_00ff:  ldc.i4 100
    IL_0100:  bne.un FAIL

    ldstr     "PASS"
    call      void [System.Console]System.Console::WriteLine(string)	   	
    ldc.i4 100
    IL_0015:  ret 

    FAIL:
    ldstr     "FAIL"
    call      void [System.Console]System.Console::WriteLine(string)	    	
    ldc.i4.1
    IL_000d:  ret 

  } // end of method Test::Main

  .method public hidebysig static int32  sw(int32 x) cil managed
  {
    // Code size       65 (0x41)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  ldarg.0
    IL_0001:  stloc.0
    IL_0002:  ldloc.0
    IL_0003:  ldc.i4.3
    IL_0004:  sub
    IL_0009:  switch     ( 
                          IL_0037,
                          IL_0037)

    IL_001a:  ldloc.0
    IL_001b:  ldc.i4.s   13
    IL_001d:  sub
    IL_0022:  switch     ( 
                          IL_0037,
                          IL_0037)
    IL_002f:  br.s       IL_003b

    IL_0037:  ldc.i4.1
    ret

    IL_003b:  ldc.i4 100   
    ret
  } // end of method Test::sw

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Test::.ctor

} // end of class Test


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file foo.res
