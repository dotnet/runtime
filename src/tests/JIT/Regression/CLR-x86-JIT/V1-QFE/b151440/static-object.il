// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}





// Metadata version: v2.0.40604
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 2:0:3600:0
}
.assembly 'static-object'
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 )
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.assembly extern xunit.core {}
// MVID: {01E12E66-85BE-40B5-9C1D-CEDDD038DB2C}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x03D60000


// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi beforefieldinit Test_static_object
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32  Main(string[] args) cil managed noinlining
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       90 (0x5a)
    .maxstack  3
    .locals init (string[] V_0,
             string V_1,
             int32 V_2,
             string[] V_3,
             bool V_4)
    IL_0000:  ldc.i4.2
    IL_0001:  newarr     [mscorlib]System.String
    IL_0006:  stloc.3
    IL_0007:  ldloc.3
    IL_0008:  ldc.i4.0
    IL_0009:  ldstr      "hello"
    IL_000e:  stelem.ref
    IL_000f:  ldloc.3
    IL_0010:  ldc.i4.1
    IL_0011:  ldstr      "world"
    IL_0016:  stelem.ref
    IL_0017:  ldloc.3
    IL_0018:  stloc.0
    IL_0019:  ldstr      ", "
    IL_001e:  ldloc.0
    IL_001f:  ldftn       string [mscorlib]System.String::Join(string,
                                                              string[])
		IL_0020:  calli       string (string,
                                                              string[])
    IL_0024:  stloc.1
    IL_0025:  ldloc.1
    IL_0026:  ldstr      "hello, world"
    IL_002b:  callvirt   instance bool [mscorlib]System.String::Equals(string)
    IL_0030:  ldc.i4.0
    IL_0031:  ceq
    IL_0033:  stloc.s    V_4
    IL_0035:  ldloc.s    V_4
    IL_0037:  brtrue.s   IL_0049

    IL_0039:  ldstr      "passed"
    IL_003e:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0043:  nop
    IL_0044:  ldc.i4.s   100
    IL_0046:  stloc.2
    IL_0047:  br.s       IL_0058

    IL_0049:  ldstr      "failed"
    IL_004e:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0053:  nop
    IL_0054:  ldc.i4.1
    IL_0055:  stloc.2
    IL_0056:  br.s       IL_0058

    IL_0058:  ldloc.2
    IL_0059:  ret
  } // end of method Test::Main

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
// WARNING: Created Win32 resource file static-object.res
