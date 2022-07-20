// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern xunit.core {}

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly HelloWorld
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
// MVID: {EE5EBACC-D919-44C6-9FC2-1636154C3772}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x00890000


// =============== CLASS MEMBERS DECLARATION ===================

.class private auto ansi beforefieldinit HelloWorld
       extends [mscorlib]System.Object
{
  .method private hidebysig static int32 
          Main(string[] args) cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       171 (0xab)
    .maxstack  3
    .locals init (bool V_0,
             class [mscorlib]System.Reflection.MethodInfo V_1,
             class [mscorlib]System.Type V_2,
             int32 V_3,
             class [mscorlib]System.Reflection.MethodInfo[] V_4,
             int32 V_5,
             bool V_6)
    IL_0000:  nop
    IL_0001:  ldc.i4.1
    IL_0002:  stloc.0
    IL_0003:  nop
    IL_0004:  ldtoken    HelloWorld
    IL_0009:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_000e:  ldc.i4.s   24
    IL_0010:  callvirt   instance class [mscorlib]System.Reflection.MethodInfo[] [mscorlib]System.Type::GetMethods(valuetype [mscorlib]System.Reflection.BindingFlags)
    IL_0015:  stloc.s    V_4
    IL_0017:  ldc.i4.0
    IL_0018:  stloc.s    V_5
    IL_001a:  br.s       IL_0088

    IL_001c:  ldloc.s    V_4
    IL_001e:  ldloc.s    V_5
    IL_0020:  ldelem.ref
    IL_0021:  stloc.1
    IL_0022:  nop
    IL_0023:  ldstr      "Test {0}:"
    IL_0028:  ldloc.1
    IL_0029:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_002e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_0033:  nop
    .try
    {
      IL_0034:  nop
      IL_0035:  ldloc.1
      IL_0036:  ldnull
      IL_0037:  ldc.i4.0
      IL_0038:  newarr     [mscorlib]System.Object
      IL_003d:  callvirt   instance object [mscorlib]System.Reflection.MethodBase::Invoke(object,
                                                                                          object[])
      IL_0042:  castclass  [mscorlib]System.Type
      IL_0047:  stloc.2
      IL_0048:  ldstr      "    + TYPE LOADED"
      IL_004d:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0052:  nop
      IL_0053:  ldstr      "    + TEST FAILED\n\n"
      IL_0058:  call       void [System.Console]System.Console::WriteLine(string)
      IL_005d:  nop
      IL_005e:  ldc.i4.0
      IL_005f:  stloc.0
      IL_0060:  nop
      IL_0061:  leave.s    IL_0080

    }  // end .try
    catch [mscorlib]System.Object 
    {
      IL_0063:  pop
      IL_0064:  nop
      IL_0065:  ldstr      "    + TYPE NOT LOADED"
      IL_006a:  call       void [System.Console]System.Console::WriteLine(string)
      IL_006f:  nop
      IL_0070:  ldstr      "    + TEST PASSED\n\n"
      IL_0075:  call       void [System.Console]System.Console::WriteLine(string)
      IL_007a:  nop
      IL_007e:  leave.s    IL_0080

    }  // end handler
    IL_0080:  nop
    IL_0081:  nop
    IL_0082:  ldloc.s    V_5
    IL_0084:  ldc.i4.1
    IL_0085:  add
    IL_0086:  stloc.s    V_5
    IL_0088:  ldloc.s    V_5
    IL_008a:  ldloc.s    V_4
    IL_008c:  ldlen
    IL_008d:  conv.i4
    IL_008e:  clt
    IL_0090:  stloc.s    V_6
    IL_0092:  ldloc.s    V_6
    IL_0094:  brtrue.s   IL_001c

    IL_0096:  ldloc.0
    IL_0097:  ldc.i4.0
    IL_0098:  ceq
    IL_009a:  stloc.s    V_6
    IL_009c:  ldloc.s    V_6
    IL_009e:  brtrue.s   IL_00a5

    IL_00a0:  ldc.i4.s   100
    IL_00a2:  stloc.3
    IL_00a3:  br.s       IL_00a9

    IL_00a5:  ldc.i4.m1
    IL_00a6:  stloc.3
    IL_00a7:  br.s       IL_00a9

    IL_00a9:  ldloc.3
    IL_00aa:  ret
  } // end of method HelloWorld::Main

  .method public hidebysig static class [mscorlib]System.Type 
          Test_B() cil managed
  {
    // Code size       16 (0x10)
    .maxstack  1
    .locals init (class [mscorlib]System.Type V_0)
    IL_0000:  nop
    IL_0001:  ldtoken    B
    IL_0006:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_000e

    IL_000e:  ldloc.0
    IL_000f:  ret
  } // end of method HelloWorld::Test_B

  .method public hidebysig static class [mscorlib]System.Type 
          Test_C() cil managed
  {
    // Code size       16 (0x10)
    .maxstack  1
    .locals init (class [mscorlib]System.Type V_0)
    IL_0000:  nop
    IL_0001:  ldtoken    C
    IL_0006:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_000e

    IL_000e:  ldloc.0
    IL_000f:  ret
  } // end of method HelloWorld::Test_C

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method HelloWorld::.ctor

} // end of class HelloWorld

.class private auto ansi beforefieldinit A
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot virtual 
          instance void  Print() cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "A.Print"
    IL_0006:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method A::Print

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method A::.ctor

} // end of class A

.class private auto ansi beforefieldinit B
       extends A
{
  .method public hidebysig newslot virtual 
          instance void  Print1() cil managed
  {
	.override A::Print
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "B.Print1"
    IL_0006:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method B::Print1

  .method public hidebysig newslot virtual 
          instance void  Print2() cil managed
  {
	.override A::Print
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "B.Print2"
    IL_0006:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method B::Print2

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void A::.ctor()
    IL_0006:  ret
  } // end of method B::.ctor

} // end of class B

.class private auto ansi beforefieldinit CBase
       extends A
{
  .method public hidebysig virtual instance void 
          Print() cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "CBase.Print"
    IL_0006:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method CBase::Print

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void A::.ctor()
    IL_0006:  ret
  } // end of method CBase::.ctor

} // end of class CBase

.class private auto ansi beforefieldinit C
       extends CBase
{
  .method public hidebysig newslot virtual 
          instance void  Print1() cil managed
  {
	.override A::Print
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "C.Print1"
    IL_0006:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method C::Print1

  .method public hidebysig newslot virtual 
          instance void  Print2() cil managed
  {
	.override CBase::Print
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "C.Print2"
    IL_0006:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ret
  } // end of method C::Print2

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void CBase::.ctor()
    IL_0006:  ret
  } // end of method C::.ctor

} // end of class C


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file HelloWorld.res
