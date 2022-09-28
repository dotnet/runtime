
//  Microsoft (R) .NET Framework IL Disassembler.  Version 4.0.30319.18408
//  Copyright (c) Microsoft Corporation.  All rights reserved.



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
.assembly RecursiveGeneric
{
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                                   63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.

  // 
  //  .custom instance void [System.Runtime]System.Diagnostics.DebuggableAttribute::.ctor(valuetype [System.Runtime]System.Diagnostics.DebuggableAttribute/DebuggingModes) = ( 01 00 07 01 00 00 00 00 ) 

  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module RecursiveGeneric.dll
// MVID: {10541B0F-16D6-4F9A-B0EB-E793F524F163}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x03000000


// =============== CLASS MEMBERS DECLARATION ===================

.class interface private abstract auto ansi IBase`1<T>
{
  .method public hidebysig static abstract virtual 
          void  Method() cil managed
  {
  } // end of method IBase`1::Method

} // end of class IBase`1

.class interface private abstract auto ansi IDerived`1<T>
       implements class IBase`1<!T>
{
  .method public hidebysig static void  Method() cil managed
  {
    .override  method void class IBase`1<!T>::Method()
    // 
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "Final.Method"
    IL_0006:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method Final::Method

} // end of class IDerived`1

.class private auto ansi beforefieldinit Final
       extends [System.Runtime]System.Object
       implements class IDerived`1<class Final>,
                  class IBase`1<class Final>
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // 
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method Final::.ctor

} // end of class Final

.class private auto ansi beforefieldinit Program
       extends [System.Runtime]System.Object
{
  .method private hidebysig static void  CallSVM<(class IBase`1<!!U>) T,U>() cil managed
  {
    // 
    .maxstack  8
    IL_0000:  nop
    IL_0001:  constrained. !!T
    IL_0007:  call       void class IBase`1<!!U>::Method()
    IL_000c:  nop
    IL_000d:  ret
  } // end of method Program::CallSVM

  .method private hidebysig static int32 
          Main() cil managed
  {
    .entrypoint
    // 
    .maxstack  1
    .locals init ([0] int32 V_0)
    IL_0000:  nop
    IL_0001:  call       void Program::CallSVM<class Final,class Final>()
    IL_0006:  nop
    IL_0007:  ldc.i4.s   100
    IL_0009:  stloc.0
    IL_000a:  br.s       IL_000c

    IL_000c:  ldloc.0
    IL_000d:  ret
  } // end of method Program::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // 
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method Program::.ctor

} // end of class Program


// =============================================================

// 
// 
