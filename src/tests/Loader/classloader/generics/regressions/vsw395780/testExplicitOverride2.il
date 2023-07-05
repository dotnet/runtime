// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern xunit.core {}
// this test is also regression test for VSW 395780. The difference is that
// .override directive in this case is in the class scope, whereas in testExplicitOverride
// .override directive is in method scope.

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 2:0:0:0
}
.assembly testExplicitOverride2
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
// MVID: {8B3E6954-BAFA-445B-A5E1-CECA62B7DCE6}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x03090000


// =============== CLASS MEMBERS DECLARATION ===================

.class interface public abstract auto ansi I`1<T>
{
  .method public hidebysig newslot abstract virtual 
          instance int32  M(!0 t) cil managed
  {
  } // end of method I`1::M

} // end of class I`1

.class public auto ansi beforefieldinit C
       extends [mscorlib]System.Object
       implements class I`1<string>
{

  .override  method instance int32 class I`1<string>::M(!0) with method instance int32 class C::MInC(string)

  .method public hidebysig newslot virtual final 
          instance int32  MInC(string t) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldc.i4.3
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method C::MInC

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method C::.ctor

} // end of class C

.class public auto ansi beforefieldinit Test_testExplicitOverride2
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       105 (0x69)
    .maxstack  2
    .locals init (class I`1<string> V_0,
             int32 V_1,
             class [mscorlib]System.Exception V_2,
             int32 V_3,
             bool V_4)
    IL_0000:  nop
    .try
    {
      IL_0001:  nop
      IL_0002:  newobj     instance void C::.ctor()
      IL_0007:  stloc.0
      IL_0008:  ldloc.0
      IL_0009:  ldstr      "Hello"
      IL_000e:  callvirt   instance int32 class I`1<string>::M(!0)
      IL_0013:  stloc.1
      IL_0014:  ldloc.1
      IL_0015:  ldc.i4.3
      IL_0016:  ceq
      IL_0018:  ldc.i4.0
      IL_0019:  ceq
      IL_001b:  stloc.s    V_4
      IL_001d:  ldloc.s    V_4
      IL_001f:  brtrue.s   IL_0032

      IL_0021:  nop
      IL_0022:  ldstr      "PASS"
      IL_0027:  call       void [System.Console]System.Console::WriteLine(string)
      IL_002c:  nop
      IL_002d:  ldc.i4.s   100
      IL_002f:  stloc.3
      IL_0030:  leave.s    IL_0066

      IL_0032:  nop
      IL_0033:  ldstr      "FAIL: Incorrect method was invoked. Ret ="
      IL_0038:  ldloc.1
      IL_0039:  box        [mscorlib]System.Int32
      IL_003e:  call       string [mscorlib]System.String::Concat(object,
                                                                  object)
      IL_0043:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0048:  nop
      IL_0049:  ldc.i4.s   101
      IL_004b:  stloc.3
      IL_004c:  leave.s    IL_0066

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_004e:  stloc.2
      IL_004f:  nop
      IL_0050:  ldstr      "FAIL: Caugh unexpected exception: "
      IL_0055:  ldloc.2
      IL_0056:  call       string [mscorlib]System.String::Concat(object,
                                                                  object)
      IL_005b:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0060:  nop
      IL_0061:  ldc.i4.s   101
      IL_0063:  stloc.3
      IL_0064:  leave.s    IL_0066

    }  // end handler
    IL_0066:  nop
    IL_0067:  ldloc.3
    IL_0068:  ret
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
// WARNING: Created Win32 resource file test2.res
