// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


// Metadata version: v2.0.50727
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 2:0:0:0
}

.module TailcallVerifyVerifiableLibraryWithPrefix.dll
// MVID: {F8D503A0-BC73-40CC-B951-B9AA2545825B}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x00F50000


// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi beforefieldinit TailcallVerify.Condition23
       extends [mscorlib]System.Object
{
  .field private static int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) zero
  .field private static int32 Result
  .method public hidebysig static int32  Test1() cil managed nooptimization
  {
    // Code size       161 (0xa1)
    .maxstack  3
    .locals init ([0] class TailcallVerify.Condition23 con,
             [1] class [mscorlib]System.Exception e)
    IL_0000:  ldstr      "Executing Condition23.Test1 - Caller: Arguments: N"
    + "one - ReturnType: void; Callee (virtual call): Arguments: 0 - ReturnTyp"
    + "e: Int32"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.s   100
    IL_000c:  stsfld     int32 TailcallVerify.Condition23::Result
    .try
    {
      IL_0011:  newobj     instance void TailcallVerify.Condition23::.ctor()
      IL_0016:  stloc.0
      IL_0017:  ldloc.0
      IL_0018:  callvirt   instance int32 TailcallVerify.Condition23::Caller1()
      IL_001d:  pop
      IL_001e:  leave.s    IL_0077

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0020:  stloc.1
      IL_0021:  ldloc.1
      IL_0022:  isinst     [mscorlib]System.DivideByZeroException
      IL_0027:  brtrue.s   IL_0032

      IL_0029:  ldc.i4.s   101
      IL_002b:  stsfld     int32 TailcallVerify.Condition23::Result
      IL_0030:  rethrow
      IL_0032:  ldloc.1
      IL_0033:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_0038:  ldstr      "Caller"
      IL_003d:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
      IL_0042:  ldc.i4.m1
      IL_0043:  bne.un.s   IL_0075

      IL_0045:  ldstr      "FAILED: Did not found the word 'Caller' in the sta"
      + "cktrace. Since the assembly is transparent, a tailcall should not be al"
      + "lowed."
      IL_004a:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_004f:  ldstr      "------------------------------------------------"
      IL_0054:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0059:  call       void [mscorlib]System.Console::WriteLine()
      IL_005e:  ldloc.1
      IL_005f:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_0064:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0069:  call       void [mscorlib]System.Console::WriteLine()
      IL_006e:  ldc.i4.s   101
      IL_0070:  stsfld     int32 TailcallVerify.Condition23::Result
      IL_0075:  leave.s    IL_0077

    }  // end handler
    IL_0077:  ldstr      "Execution finished - Test "
    IL_007c:  ldsfld     int32 TailcallVerify.Condition23::Result
    IL_0081:  ldc.i4.s   100
    IL_0083:  beq.s      IL_008c

    IL_0085:  ldstr      "FAILED"
    IL_008a:  br.s       IL_0091

    IL_008c:  ldstr      "PASSED"
    IL_0091:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0096:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_009b:  ldsfld     int32 TailcallVerify.Condition23::Result
    IL_00a0:  ret
  } // end of method Condition23::Test1

  .method private hidebysig instance int32 
          Caller1() cil managed
  {
    // Code size       53 (0x35)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  newobj     instance void [mscorlib]System.Diagnostics.StackFrame::.ctor(bool)
    IL_0006:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_000b:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_0010:  ldstr      "Caller"
    IL_0015:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_001a:  ldc.i4.m1
    IL_001b:  bne.un.s   IL_002e

    IL_001d:  ldstr      "Failed, Method was inlined..."
    IL_0022:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0027:  ldc.i4.s   101
    IL_0029:  stsfld     int32 TailcallVerify.Condition23::Result
    IL_002e:  ldarg.0
    IL_002f:  tail. callvirt   instance int32 TailcallVerify.Condition23::Callee1()
    IL_0034:  ret
  } // end of method Condition23::Caller1

  .method family hidebysig newslot virtual 
          instance int32  Callee1() cil managed noinlining
  {
    // Code size       12 (0xc)
    .maxstack  2
    .locals init ([0] int32 i)
    IL_0000:  ldc.i4.1
    IL_0001:  volatile.
    IL_0003:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition23::zero
    IL_0008:  div
    IL_0009:  stloc.0
    IL_000a:  ldloc.0
    IL_000b:  ret
  } // end of method Condition23::Callee1

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Condition23::.ctor

  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       16 (0x10)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  volatile.
    IL_0003:  stsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition23::zero
    IL_0008:  ldc.i4.s   100
    IL_000a:  stsfld     int32 TailcallVerify.Condition23::Result
    IL_000f:  ret
  } // end of method Condition23::.cctor

} // end of class TailcallVerify.Condition23


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file TailcallVerifyVerifiableLibraryWithPrefix.res
