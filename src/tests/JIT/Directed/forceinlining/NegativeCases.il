// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 4:0:0:0
}

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly NegativeCases
{
}

.module NegativeCases.exe

.class public auto ansi beforefieldinit P
       extends [mscorlib]System.Object
{
  .field private static int32 i
  .method public hidebysig static string 
          A1() cil managed aggressiveinlining
  {
    // Code size       55 (0x37)
    .maxstack  2
    .locals init (string V_0,
             string V_1)
    IL_0000:  nop
    IL_0001:  newobj     instance void [mscorlib]System.Diagnostics.StackTrace::.ctor()
    IL_0006:  ldc.i4.0
    IL_0007:  callvirt   instance class [mscorlib]System.Diagnostics.StackFrame [mscorlib]System.Diagnostics.StackTrace::GetFrame(int32)
    IL_000c:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_0011:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_0016:  stloc.0
    .try
    {
      IL_0017:  nop
      IL_0018:  ldsfld     int32 P::i
      IL_001d:  ldc.i4     0xf4240
      IL_0022:  add.ovf
      IL_0023:  stsfld     int32 P::i
      IL_0028:  nop
      IL_0029:  leave.s    IL_0030

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_002b:  pop
      IL_002c:  nop
      IL_002d:  nop
      IL_002e:  leave.s    IL_0030

    }  // end handler
    IL_0030:  nop
    IL_0031:  ldloc.0
    IL_0032:  stloc.1
    IL_0033:  br.s       IL_0035

    IL_0035:  ldloc.1
    IL_0036:  ret
  } // end of method P::A1

  .method public hidebysig static int32  Main() cil managed
  {
    .entrypoint
    // Code size       71 (0x47)
    .maxstack  2
    .locals init (string V_0,
             int32 V_1,
             bool V_2)
    IL_0000:  nop
    IL_0001:  ldstr      "We should not inline methods with EH regardless of"
    + " how much people suggest we do so."
    IL_0006:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  call       string P::A1()
    IL_0011:  stloc.0
    IL_0012:  ldloc.0
    IL_0013:  ldstr      "A1"
    IL_0018:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_001d:  ldc.i4.0
    IL_001e:  ceq
    IL_0020:  stloc.2
    IL_0021:  ldloc.2
    IL_0022:  brtrue.s   IL_0035

    IL_0024:  nop
    IL_0025:  ldstr      "PASS"
    IL_002a:  call       void [System.Console]System.Console::WriteLine(string)
    IL_002f:  nop
    IL_0030:  ldc.i4.s   100
    IL_0032:  stloc.1
    IL_0033:  br.s       IL_0045

    IL_0035:  ldstr      "!!! FAIL - it appears that code from A1 was run in"
    + " the frame of {0}"
    IL_003a:  ldloc.0
    IL_003b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_0040:  nop
    IL_0041:  ldc.i4.m1
    IL_0042:  stloc.1
    IL_0043:  br.s       IL_0045

    IL_0045:  ldloc.1
    IL_0046:  ret
  } // end of method P::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method P::.ctor

  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  stsfld     int32 P::i
    IL_0006:  ret
  } // end of method P::.cctor

} // end of class P
