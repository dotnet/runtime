// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern xunit.core {}

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 2:0:3600:0
}
.assembly extern Test6_forwarder
{
  .ver 0:0:0:0
}
.assembly extern Test6_FriendPriInterface
{
  .ver 0:0:0:0
}
.assembly Test6_ImplementingClass
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
// MVID: {899F22E9-D0C0-4B61-9698-A96DF1D4E4BE}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x03E50000


// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi beforefieldinit Test_Test6_ImplementingClass
       extends [mscorlib]System.Object
{
  .method public hidebysig static void  method1() cil managed
  {
    // Code size       14 (0xe)
    .maxstack  1
    .locals init (class [Test6_FriendPriInterface]IA V_0)
    IL_0000:  newobj     instance void [Test6_forwarder]A::.ctor()
    IL_0005:  stloc.0
    IL_0006:  ldloc.0
    IL_0007:  callvirt   instance void [Test6_FriendPriInterface]IA::methodIA()
    IL_000c:  nop
    IL_000d:  ret
  } // end of method Test::method1

  .method public hidebysig static void  method2() cil managed
  {
    // Code size       14 (0xe)
    .maxstack  1
    .locals init (class [Test6_FriendPriInterface]'IB`1'<int32> V_0)
    IL_0000:  newobj     instance void [Test6_forwarder]B::.ctor()
    IL_0005:  stloc.0
    IL_0006:  ldloc.0
    IL_0007:  callvirt   instance void class [Test6_FriendPriInterface]'IB`1'<int32>::methodIB()
    IL_000c:  nop
    IL_000d:  ret
  } // end of method Test::method2

  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       130 (0x82)
    .maxstack  2
    .locals init ([0] int32 exitCode1,
             [1] int32 exitCode2,
             [2] class [mscorlib]System.Exception e,
             [3] int32 CS$1$0000,
             [4] bool CS$4$0001)
    .try
    {
      IL_0000:  call       void Test_Test6_ImplementingClass::method1()
      IL_0005:  nop
      IL_0006:  ldstr      "PASS at method1(): Loaded type A"
      IL_000b:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0010:  nop
      IL_0011:  ldc.i4.s   100
      IL_0013:  stloc.0
      IL_0014:  nop
      IL_0015:  leave.s    IL_002f

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0017:  stloc.2
      IL_0018:  ldstr      "FAIL at method1(): Caught unexpected exception: "
      IL_001d:  ldloc.2
      IL_001e:  call       string [mscorlib]System.String::Concat(object,
                                                                  object)
      IL_0023:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0028:  nop
      IL_0029:  ldc.i4.s   101
      IL_002b:  stloc.0
      IL_002c:  nop
      IL_002d:  leave.s    IL_002f

    }  // end handler
    IL_002f:  nop
    .try
    {
      IL_0030:  call       void Test_Test6_ImplementingClass::method2()
      IL_0035:  nop
      IL_0036:  ldstr      "PASS at method2(): Loaded type B"
      IL_003b:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0040:  nop
      IL_0041:  ldc.i4.s   100
      IL_0043:  stloc.1
      IL_0044:  nop
      IL_0045:  leave.s    IL_005f

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0047:  stloc.2
      IL_0048:  ldstr      "FAIL at method2(): Caught unexpected exception: "
      IL_004d:  ldloc.2
      IL_004e:  call       string [mscorlib]System.String::Concat(object,
                                                                  object)
      IL_0053:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0058:  nop
      IL_0059:  ldc.i4.s   101
      IL_005b:  stloc.1
      IL_005c:  nop
      IL_005d:  leave.s    IL_005f

    }  // end handler
    IL_005f:  nop
    IL_0060:  ldloc.0
    IL_0061:  ldc.i4.s   100
    IL_0063:  bne.un.s   IL_006f

    IL_0065:  ldloc.1
    IL_0066:  ldc.i4.s   100
    IL_0068:  ceq
    IL_006a:  ldc.i4.0
    IL_006b:  ceq
    IL_006d:  br.s       IL_0070

    IL_006f:  ldc.i4.1
    IL_0070:  stloc.s    CS$4$0001
    IL_0072:  ldloc.s    CS$4$0001
    IL_0074:  brtrue.s   IL_007b

    IL_0076:  ldc.i4.s   100
    IL_0078:  stloc.3
    IL_0079:  br.s       IL_0080

    IL_007b:  ldc.i4.s   101
    IL_007d:  stloc.3
    IL_007e:  br.s       IL_0080

    IL_0080:  ldloc.3
    IL_0081:  ret
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
// WARNING: Created Win32 resource file Test6_ImplementingClass.res
