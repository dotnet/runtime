// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



// Metadata version: v2.0.50727
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 2:0:0:0
}

.module TailcallVerifyTransparentLibraryWithPrefix.dll
// MVID: {A0DB04B7-B678-4A57-9EEC-D55E6443C758}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x00F50000


// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi beforefieldinit TailcallVerify.Condition22
       extends [mscorlib]System.Object
{
  .field private static int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) zero
  .field private static int32 Result
  .method public hidebysig static int32  Test1() cil managed nooptimization
  {
    // Code size       95 (0x5f)
    .maxstack  3
    .locals init ([0] class [mscorlib]System.Exception e)
    IL_0000:  ldstr      "Executing Condition22.Test1  - Caller(SecurityTran"
    + "sparent): Arguments: None - ReturnType: void; Callee(SecurityCritical):"
    + " Arguments: 0 - ReturnType: Int32"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.s   100
    IL_000c:  stsfld     int32 TailcallVerify.Condition22::Result
    .try
    {
      IL_0011:  call       void TailcallVerify.Condition22::Caller1()
      IL_0016:  leave.s    IL_0035

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0018:  stloc.0
      IL_0019:  ldloc.0
      IL_001a:  isinst     [mscorlib]System.MethodAccessException
      IL_001f:  brfalse.s  IL_002a

      IL_0021:  ldc.i4.s   100
      IL_0023:  stsfld     int32 TailcallVerify.Condition22::Result
      IL_0028:  br.s       IL_0033

      IL_002a:  ldc.i4.s   101
      IL_002c:  stsfld     int32 TailcallVerify.Condition22::Result
      IL_0031:  rethrow
      IL_0033:  leave.s    IL_0035

    }  // end handler
    IL_0035:  ldstr      "Execution finished - Test "
    IL_003a:  ldsfld     int32 TailcallVerify.Condition22::Result
    IL_003f:  ldc.i4.s   100
    IL_0041:  beq.s      IL_004a

    IL_0043:  ldstr      "FAILED"
    IL_0048:  br.s       IL_004f

    IL_004a:  ldstr      "PASSED"
    IL_004f:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0054:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0059:  ldsfld     int32 TailcallVerify.Condition22::Result
    IL_005e:  ret
  } // end of method Condition22::Test1

  .method private hidebysig static void  Caller1() cil managed
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
    IL_0029:  stsfld     int32 TailcallVerify.Condition22::Result
    IL_002e:  tail. call       void TailcallVerify.Condition22::Callee1()
    IL_0034:  ret
  } // end of method Condition22::Caller1

  .method private hidebysig static void 
          Callee1() cil managed noinlining
  {
    .custom instance void [mscorlib]System.Security.SecurityCriticalAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       22 (0x16)
    .maxstack  2
    .locals init ([0] int32 i)
    IL_0000:  ldstr      "in Condition22.Callee1....."
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.1
    IL_000b:  volatile.
    IL_000d:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition22::zero
    IL_0012:  div
    IL_0013:  stloc.0
    IL_0015:  ret
  } // end of method Condition22::Callee1

  .method public hidebysig static int32  Test2() cil managed nooptimization
  {
    .custom instance void [mscorlib]System.Security.SecurityCriticalAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       153 (0x99)
    .maxstack  3
    .locals init ([0] class [mscorlib]System.Exception e)
    IL_0000:  ldstr      "Executing Condition22.Test2  - Caller: Arguments: "
    + "None - ReturnType: void; Callee: Arguments: 0 - ReturnType: Int32"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.s   100
    IL_000c:  stsfld     int32 TailcallVerify.Condition22::Result
    .try
    {
      IL_0011:  call       void TailcallVerify.Condition22::Caller2()
      IL_0016:  leave.s    IL_006f

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0018:  stloc.0
      IL_0019:  ldloc.0
      IL_001a:  isinst     [mscorlib]System.DivideByZeroException
      IL_001f:  brtrue.s   IL_002a

      IL_0021:  ldc.i4.s   101
      IL_0023:  stsfld     int32 TailcallVerify.Condition22::Result
      IL_0028:  rethrow
      IL_002a:  ldloc.0
      IL_002b:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_0030:  ldstr      "Caller"
      IL_0035:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
      IL_003a:  ldc.i4.m1
      IL_003b:  beq.s      IL_006d

      IL_003d:  ldstr      "FAILED: Found the word 'Caller' in the stacktrace."
      IL_0042:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0047:  ldstr      "------------------------------------------------"
      IL_004c:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0051:  call       void [mscorlib]System.Console::WriteLine()
      IL_0056:  ldloc.0
      IL_0057:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_005c:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0061:  call       void [mscorlib]System.Console::WriteLine()
      IL_0066:  ldc.i4.s   101
      IL_0068:  stsfld     int32 TailcallVerify.Condition22::Result
      IL_006d:  leave.s    IL_006f

    }  // end handler
    IL_006f:  ldstr      "Execution finished - Test "
    IL_0074:  ldsfld     int32 TailcallVerify.Condition22::Result
    IL_0079:  ldc.i4.s   100
    IL_007b:  beq.s      IL_0084

    IL_007d:  ldstr      "FAILED"
    IL_0082:  br.s       IL_0089

    IL_0084:  ldstr      "PASSED"
    IL_0089:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_008e:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0093:  ldsfld     int32 TailcallVerify.Condition22::Result
    IL_0098:  ret
  } // end of method Condition22::Test2

  .method private hidebysig static void  Caller2() cil managed
  {
    .custom instance void [mscorlib]System.Security.SecurityCriticalAttribute::.ctor() = ( 01 00 00 00 ) 
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
    IL_0029:  stsfld     int32 TailcallVerify.Condition22::Result
//            tail.      // tail.call, pop, ret sequence is never valid for .NET Core (but is accepted by .NET x64)
    IL_002e:  call       int32 TailcallVerify.Condition22::Callee2()
    IL_0033:  pop
    IL_0034:  ret
  } // end of method Condition22::Caller2

  .method private hidebysig static int32 
          Callee2() cil managed noinlining
  {
    // Code size       12 (0xc)
    .maxstack  2
    .locals init ([0] int32 i)
    IL_0000:  ldc.i4.1
    IL_0001:  volatile.
    IL_0003:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition22::zero
    IL_0008:  div
    IL_0009:  stloc.0
    IL_000a:  ldloc.0
    IL_000b:  ret
  } // end of method Condition22::Callee2

  .method public hidebysig static int32  Test3() cil managed nooptimization
  {
    // Code size       160 (0xa0)
    .maxstack  3
    .locals init ([0] class TailcallVerify.Condition22 con,
             [1] class [mscorlib]System.Exception e)
    IL_0000:  ldstr      "Executing Condition22.Test3  - Caller: Arguments: "
    + "None - ReturnType: void; Callee (virtual call): Arguments: 0 - ReturnTy"
    + "pe: Int32"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.s   100
    IL_000c:  stsfld     int32 TailcallVerify.Condition22::Result
    .try
    {
      IL_0011:  newobj     instance void TailcallVerify.Condition22::.ctor()
      IL_0016:  stloc.0
      IL_0017:  ldloc.0
      IL_0018:  callvirt   instance void TailcallVerify.Condition22::Caller3()
      IL_001d:  leave.s    IL_0076

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_001f:  stloc.1
      IL_0020:  ldloc.1
      IL_0021:  isinst     [mscorlib]System.DivideByZeroException
      IL_0026:  brtrue.s   IL_0031

      IL_0028:  ldc.i4.s   101
      IL_002a:  stsfld     int32 TailcallVerify.Condition22::Result
      IL_002f:  rethrow
      IL_0031:  ldloc.1
      IL_0032:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_0037:  ldstr      "Caller"
      IL_003c:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
      IL_0041:  ldc.i4.m1
      IL_0042:  bne.un.s   IL_0074

      IL_0044:  ldstr      "FAILED: Did not found the word 'Caller' in the sta"
      + "cktrace. Since the assembly is transparent, a tailcall should not be al"
      + "lowed."
      IL_0049:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_004e:  ldstr      "------------------------------------------------"
      IL_0053:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0058:  call       void [mscorlib]System.Console::WriteLine()
      IL_005d:  ldloc.1
      IL_005e:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_0063:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0068:  call       void [mscorlib]System.Console::WriteLine()
      IL_006d:  ldc.i4.s   101
      IL_006f:  stsfld     int32 TailcallVerify.Condition22::Result
      IL_0074:  leave.s    IL_0076

    }  // end handler
    IL_0076:  ldstr      "Execution finished - Test "
    IL_007b:  ldsfld     int32 TailcallVerify.Condition22::Result
    IL_0080:  ldc.i4.s   100
    IL_0082:  beq.s      IL_008b

    IL_0084:  ldstr      "FAILED"
    IL_0089:  br.s       IL_0090

    IL_008b:  ldstr      "PASSED"
    IL_0090:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0095:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_009a:  ldsfld     int32 TailcallVerify.Condition22::Result
    IL_009f:  ret
  } // end of method Condition22::Test3

  .method private hidebysig instance void 
          Caller3() cil managed
  {
    // Code size       54 (0x36)
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
    IL_0029:  stsfld     int32 TailcallVerify.Condition22::Result
    IL_002e:  ldarg.0
//            tail.      // tail.call, pop, ret sequence is never valid for .NET Core (but is accepted by .NET x64)
    IL_002f:  callvirt   instance int32 TailcallVerify.Condition22::Callee3()
    IL_0034:  pop
    IL_0035:  ret
  } // end of method Condition22::Caller3

  .method family hidebysig newslot virtual 
          instance int32  Callee3() cil managed noinlining
  {
    // Code size       12 (0xc)
    .maxstack  2
    .locals init ([0] int32 i)
    IL_0000:  ldc.i4.1
    IL_0001:  volatile.
    IL_0003:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition22::zero
    IL_0008:  div
    IL_0009:  stloc.0
    IL_000a:  ldloc.0
    IL_000b:  ret
  } // end of method Condition22::Callee3

  .method public hidebysig static int32  Test4() cil managed nooptimization
  {
    // Code size       160 (0xa0)
    .maxstack  3
    .locals init ([0] class TailcallVerify.Condition22 con,
             [1] class [mscorlib]System.Exception e)
    IL_0000:  ldstr      "Executing Condition22.Test4  - Caller: Arguments: "
    + "None - ReturnType: void; Callee(regular call instruction): Arguments: 0"
    + " - ReturnType: Int32"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.s   100
    IL_000c:  stsfld     int32 TailcallVerify.Condition22::Result
    .try
    {
      IL_0011:  newobj     instance void TailcallVerify.Condition22::.ctor()
      IL_0016:  stloc.0
      IL_0017:  ldloc.0
      IL_0018:  callvirt   instance void TailcallVerify.Condition22::Caller4()
      IL_001d:  leave.s    IL_0076

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_001f:  stloc.1
      IL_0020:  ldloc.1
      IL_0021:  isinst     [mscorlib]System.DivideByZeroException
      IL_0026:  brtrue.s   IL_0031

      IL_0028:  ldc.i4.s   101
      IL_002a:  stsfld     int32 TailcallVerify.Condition22::Result
      IL_002f:  rethrow
      IL_0031:  ldloc.1
      IL_0032:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_0037:  ldstr      "Caller"
      IL_003c:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
      IL_0041:  ldc.i4.m1
      IL_0042:  beq.s      IL_0074

      IL_0044:  ldstr      "FAILED: Found the word 'Caller' in the stacktrace."
      IL_0049:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_004e:  ldstr      "------------------------------------------------"
      IL_0053:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0058:  call       void [mscorlib]System.Console::WriteLine()
      IL_005d:  ldloc.1
      IL_005e:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
      IL_0063:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0068:  call       void [mscorlib]System.Console::WriteLine()
      IL_006d:  ldc.i4.s   101
      IL_006f:  stsfld     int32 TailcallVerify.Condition22::Result
      IL_0074:  leave.s    IL_0076

    }  // end handler
    IL_0076:  ldstr      "Execution finished - Test "
    IL_007b:  ldsfld     int32 TailcallVerify.Condition22::Result
    IL_0080:  ldc.i4.s   100
    IL_0082:  beq.s      IL_008b

    IL_0084:  ldstr      "FAILED"
    IL_0089:  br.s       IL_0090

    IL_008b:  ldstr      "PASSED"
    IL_0090:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0095:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_009a:  ldsfld     int32 TailcallVerify.Condition22::Result
    IL_009f:  ret
  } // end of method Condition22::Test4

  .method private hidebysig instance void 
          Caller4() cil managed
  {
    // Code size       54 (0x36)
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
    IL_0029:  stsfld     int32 TailcallVerify.Condition22::Result
    IL_002e:  ldarg.0
    IL_002f:  tail. call       instance void TailcallVerify.Condition22::Callee4()
    //IL_0034:  pop
    IL_0035:  ret
  } // end of method Condition22::Caller4

  .method private hidebysig instance void
          Callee4() cil managed noinlining
  {
    // Code size       12 (0xc)
    .maxstack  2
    .locals init ([0] int32 i)
    IL_0000:  ldc.i4.1
    IL_0001:  volatile.
    IL_0003:  ldsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition22::zero
    IL_0008:  div
    IL_0009:  stloc.0
    //IL_000a:  ldloc.0
    IL_000b:  ret
  } // end of method Condition22::Callee4

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Condition22::.ctor

  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       16 (0x10)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  volatile.
    IL_0003:  stsfld     int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile) TailcallVerify.Condition22::zero
    IL_0008:  ldc.i4.s   100
    IL_000a:  stsfld     int32 TailcallVerify.Condition22::Result
    IL_000f:  ret
  } // end of method Condition22::.cctor

} // end of class TailcallVerify.Condition22


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file TailcallVerifyTransparentLibraryWithPrefix.res
