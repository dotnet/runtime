// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly fib
{

  // --- The following custom attribute is added automatically, do not uncomment -------
  //  .custom instance void [mscorlib]System.Diagnostics.DebuggableAttribute::.ctor(bool,
  //                                                                                bool) = ( 01 00 00 01 00 00 ) 

  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module fib.exe
// MVID: {F244F6A1-918F-4DD8-ABB5-5917EA8526B2}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY


// =============== CLASS MEMBERS DECLARATION ===================

.class public explicit sealed spacer
       extends [mscorlib]System.ValueType
{
.size 8
}


.class public auto ansi beforefieldinit Tests.Fib
       extends [mscorlib]System.Object
{

  .method public hidebysig static void Foo(class System.String&) cil managed
  {
     ldarg.0
     ldind.ref
     call       void [System.Console]System.Console::WriteLine(string)
     ret
  }

.method public hidebysig static int32  Main() cil managed
  {
    .entrypoint
    // Code size       54 (0x36)
    .maxstack  2
    .locals init (class Tests.Fib V_0,
             int32 V_1,
	     class System.String v_2,
	     class System.String &v_3,
             int32 v_4
)
    IL_0006:  ldc.i4.0
    IL_0007:  stloc.1
    ldstr "test"
    stloc v_2
	
.try
{
	ldloca v_2
	stloc v_3
	leave abcd
}
fault
{
	nop
	endfinally
}
abcd:
	          ldloc v_3
	          call void Tests.Fib::Foo(class System.String &)
	IL_0033:  ldc.i4.s 100
        IL_0034:  stloc v_4
        IL_0035:  ldloc v_4
        IL_0036:  ret
  } // end of method Fib::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Fib::.ctor

} // end of class Tests.Fib


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
