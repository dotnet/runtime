// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern xunit.core {}
//DDB187632: generic types and generic methods with more than 0xFFFF generic arguments should throw a TypeLoadException

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 2:0:0:0
}
.assembly extern genmeth
{
  .ver 0:0:0:0
}
.assembly extern gentype
{
  .ver 0:0:0:0
}
.assembly 'dev10_403582'
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
// MVID: {B6CD7E92-EDE7-4493-B78F-D3F743C68390}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x00900000


// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi beforefieldinit Client
       extends [mscorlib]System.Object
{
  .method public hidebysig static void  M1() cil managed noinlining
  {
    // Code size       62 (0x3e)
    .maxstack  2
    .locals init (class [genmeth]GenMeth V_0,
             class [mscorlib]System.Type V_1,
             class [mscorlib]System.Reflection.MethodInfo V_2,
             class [mscorlib]System.Type[] V_3)
    IL_0000:  nop
    IL_0001:  newobj     instance void [genmeth]GenMeth::.ctor()
    IL_0006:  stloc.0
    IL_0007:  ldloc.0
    IL_0008:  call       valuetype [mscorlib]System.RuntimeTypeHandle [mscorlib]System.Type::GetTypeHandle(object)
    IL_000d:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_0012:  stloc.1
    IL_0013:  ldloc.1
    IL_0014:  call       void [System.Console]System.Console::WriteLine(object)
    IL_0019:  nop
    IL_001a:  ldloc.1
    IL_001b:  ldstr      "foo"
    IL_0020:  callvirt   instance class [mscorlib]System.Reflection.MethodInfo [mscorlib]System.Type::GetMethod(string)
    IL_0025:  stloc.2
    IL_0026:  ldloc.2
    IL_0027:  call       void [System.Console]System.Console::WriteLine(object)
    IL_002c:  nop
    IL_002d:  ldloc.2
    IL_002e:  callvirt   instance class [mscorlib]System.Type[] [mscorlib]System.Reflection.MethodBase::GetGenericArguments()
    IL_0033:  stloc.3
    IL_0034:  ldloc.3
    IL_0035:  ldlen
    IL_0036:  conv.i4
    IL_0037:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_003c:  nop
    IL_003d:  ret
  } // end of method Client::M1

  .method public hidebysig static void  M2() cil managed noinlining
  {
    // Code size       36 (0x24)
    .maxstack  1
    .locals init (class [mscorlib]System.Type V_0,
             class [mscorlib]System.Type[] V_1)
    IL_1000:  nop
    IL_0000:  ldtoken    [gentype]GenType`1
    IL_0005:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_000b:  stloc.0
    IL_000c:  ldloc.0
    IL_000d:  call       void [System.Console]System.Console::WriteLine(object)
    IL_0012:  nop
    IL_0013:  ldloc.0
    IL_0014:  callvirt   instance class [mscorlib]System.Type[] [mscorlib]System.Type::GetGenericArguments()
    IL_0019:  stloc.1
    IL_001a:  ldloc.1
    IL_001b:  ldlen
    IL_001c:  conv.i4
    IL_001d:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_0022:  nop
    IL_0023:  ret
  } // end of method Client::M2

  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       155 (0x9b)
    .maxstack  2
    .locals init (class [mscorlib]System.TypeLoadException V_0,
             class [mscorlib]System.Exception V_1,
             int32 V_2)
    IL_0000:  nop
    .try
    {
      IL_0001:  nop
      IL_0002:  ldstr      "Testing huge generic methods..."
      IL_0007:  call       void [System.Console]System.Console::WriteLine(string)
      IL_000c:  nop
      IL_000d:  call       void Client::M1()
      IL_0012:  nop
      IL_0013:  ldstr      "Failed: did not catch any TypeLoadException!!"
      IL_0018:  call       void [System.Console]System.Console::WriteLine(string)
      IL_001d:  nop
      IL_001e:  ldc.i4.m1
      IL_001f:  stloc.2
      IL_0020:  leave.s    IL_0098

    }  // end .try
    catch [mscorlib]System.TypeLoadException 
    {
      IL_0022:  stloc.0
      IL_0023:  nop
      IL_0024:  ldstr      "Pass!"
      IL_0029:  call       void [System.Console]System.Console::WriteLine(string)
      IL_002e:  nop
      IL_002f:  nop
      IL_0030:  leave.s    IL_0049

    }  // end handler
    catch [mscorlib]System.Exception 
    {
      IL_0032:  stloc.1
      IL_0033:  nop
      IL_0034:  ldstr      "Failed: caught unexpected exception: "
      IL_0039:  ldloc.1
      IL_003a:  call       string [mscorlib]System.String::Concat(object,
                                                                  object)
      IL_003f:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0044:  nop
      IL_0045:  ldc.i4.1
      IL_0046:  stloc.2
      IL_0047:  leave.s    IL_0098

    }  // end handler
    IL_0049:  nop
    .try
    {
      IL_004a:  nop
      IL_004b:  ldstr      "Testing huge generic types..."
      IL_0050:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0055:  nop
      IL_0056:  call       void Client::M2()
      IL_005b:  nop
      IL_005c:  ldstr      "Failed: did not catch any TypeLoadException!!"
      IL_0061:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0066:  nop
      IL_0067:  ldc.i4.m1
      IL_0068:  stloc.2
      IL_0069:  leave.s    IL_0098

    }  // end .try
    catch [mscorlib]System.TypeLoadException 
    {
      IL_006b:  stloc.0
      IL_006c:  nop
      IL_006d:  ldstr      "Pass!"
      IL_0072:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0077:  nop
      IL_0078:  nop
      IL_0079:  leave.s    IL_0092

    }  // end handler
    catch [mscorlib]System.Exception 
    {
      IL_007b:  stloc.1
      IL_007c:  nop
      IL_007d:  ldstr      "Failed: caught unexpected exception: "
      IL_0082:  ldloc.1
      IL_0083:  call       string [mscorlib]System.String::Concat(object,
                                                                  object)
      IL_0088:  call       void [System.Console]System.Console::WriteLine(string)
      IL_008d:  nop
      IL_008e:  ldc.i4.1
      IL_008f:  stloc.2
      IL_0090:  leave.s    IL_0098

    }  // end handler
    IL_0092:  nop
    IL_0093:  ldc.i4.s   100
    IL_0095:  stloc.2
    IL_0096:  br.s       IL_0098

    IL_0098:  nop
    IL_0099:  ldloc.2
    IL_009a:  ret
  } // end of method Client::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Client::.ctor

} // end of class Client


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
