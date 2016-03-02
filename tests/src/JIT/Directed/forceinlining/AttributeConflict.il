// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly AttributeConflict
{
}

.module AttributeConflict.exe

.class public auto ansi beforefieldinit P
       extends [mscorlib]System.Object
{
  .field public static bool a
  .field public static bool b
  .field public static bool c
  .field private static int32 i
  .method public hidebysig static bool  A1(bool b1,
                                           bool b2,
                                           bool b3) cil managed
  {
    // Code size       237 (0xed)
    .maxstack  2
    .locals init (string V_0,
             bool V_1,
             bool V_2)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.0
    IL_0003:  ceq
    IL_0005:  stloc.2
    IL_0006:  ldloc.2
    IL_0007:  brtrue.s   IL_006b

    IL_0009:  nop
    IL_000a:  ldarg.1
    IL_000b:  ldc.i4.0
    IL_000c:  ceq
    IL_000e:  stloc.2
    IL_000f:  ldloc.2
    IL_0010:  brtrue.s   IL_003e

    IL_0012:  nop
    IL_0013:  ldarg.2
    IL_0014:  ldc.i4.0
    IL_0015:  ceq
    IL_0017:  stloc.2
    IL_0018:  ldloc.2
    IL_0019:  brtrue.s   IL_002c

    IL_001b:  nop
    IL_001c:  ldsfld     int32 P::i
    IL_0021:  ldc.i4.s   111
    IL_0023:  add
    IL_0024:  stsfld     int32 P::i
    IL_0029:  nop
    IL_002a:  br.s       IL_003b

    IL_002c:  nop
    IL_002d:  ldsfld     int32 P::i
    IL_0032:  ldc.i4.s   110
    IL_0034:  add
    IL_0035:  stsfld     int32 P::i
    IL_003a:  nop
    IL_003b:  nop
    IL_003c:  br.s       IL_0068

    IL_003e:  nop
    IL_003f:  ldarg.2
    IL_0040:  ldc.i4.0
    IL_0041:  ceq
    IL_0043:  stloc.2
    IL_0044:  ldloc.2
    IL_0045:  brtrue.s   IL_0058

    IL_0047:  nop
    IL_0048:  ldsfld     int32 P::i
    IL_004d:  ldc.i4.s   101
    IL_004f:  add
    IL_0050:  stsfld     int32 P::i
    IL_0055:  nop
    IL_0056:  br.s       IL_0067

    IL_0058:  nop
    IL_0059:  ldsfld     int32 P::i
    IL_005e:  ldc.i4.s   100
    IL_0060:  add
    IL_0061:  stsfld     int32 P::i
    IL_0066:  nop
    IL_0067:  nop
    IL_0068:  nop
    IL_0069:  br.s       IL_00c7

    IL_006b:  nop
    IL_006c:  ldarg.1
    IL_006d:  ldc.i4.0
    IL_006e:  ceq
    IL_0070:  stloc.2
    IL_0071:  ldloc.2
    IL_0072:  brtrue.s   IL_00a0

    IL_0074:  nop
    IL_0075:  ldarg.2
    IL_0076:  ldc.i4.0
    IL_0077:  ceq
    IL_0079:  stloc.2
    IL_007a:  ldloc.2
    IL_007b:  brtrue.s   IL_008e

    IL_007d:  nop
    IL_007e:  ldsfld     int32 P::i
    IL_0083:  ldc.i4.s   11
    IL_0085:  add
    IL_0086:  stsfld     int32 P::i
    IL_008b:  nop
    IL_008c:  br.s       IL_009d

    IL_008e:  nop
    IL_008f:  ldsfld     int32 P::i
    IL_0094:  ldc.i4.s   10
    IL_0096:  add
    IL_0097:  stsfld     int32 P::i
    IL_009c:  nop
    IL_009d:  nop
    IL_009e:  br.s       IL_00c6

    IL_00a0:  nop
    IL_00a1:  ldarg.2
    IL_00a2:  ldc.i4.0
    IL_00a3:  ceq
    IL_00a5:  stloc.2
    IL_00a6:  ldloc.2
    IL_00a7:  brtrue.s   IL_00b9

    IL_00a9:  nop
    IL_00aa:  ldsfld     int32 P::i
    IL_00af:  ldc.i4.1
    IL_00b0:  add
    IL_00b1:  stsfld     int32 P::i
    IL_00b6:  nop
    IL_00b7:  br.s       IL_00c5

    IL_00b9:  nop
    IL_00ba:  ldsfld     int32 P::i
    IL_00bf:  stsfld     int32 P::i
    IL_00c4:  nop
    IL_00c5:  nop
    IL_00c6:  nop
    IL_00c7:  newobj     instance void [mscorlib]System.Diagnostics.StackTrace::.ctor()
    IL_00cc:  ldc.i4.0
    IL_00cd:  callvirt   instance class [mscorlib]System.Diagnostics.StackFrame [mscorlib]System.Diagnostics.StackTrace::GetFrame(int32)
    IL_00d2:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_00d7:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_00dc:  stloc.0
    IL_00dd:  ldloc.0
    IL_00de:  ldstr      "A1"
    IL_00e3:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_00e8:  stloc.1
    IL_00e9:  br.s       IL_00eb

    IL_00eb:  ldloc.1
    IL_00ec:  ret
  } // end of method P::A1

  .method public hidebysig static bool  A2(bool b1,
                                           bool b2,
                                           bool b3) cil managed aggressiveinlining
  {
    // Code size       237 (0xed)
    .maxstack  2
    .locals init (string V_0,
             bool V_1,
             bool V_2)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.0
    IL_0003:  ceq
    IL_0005:  stloc.2
    IL_0006:  ldloc.2
    IL_0007:  brtrue.s   IL_006b

    IL_0009:  nop
    IL_000a:  ldarg.1
    IL_000b:  ldc.i4.0
    IL_000c:  ceq
    IL_000e:  stloc.2
    IL_000f:  ldloc.2
    IL_0010:  brtrue.s   IL_003e

    IL_0012:  nop
    IL_0013:  ldarg.2
    IL_0014:  ldc.i4.0
    IL_0015:  ceq
    IL_0017:  stloc.2
    IL_0018:  ldloc.2
    IL_0019:  brtrue.s   IL_002c

    IL_001b:  nop
    IL_001c:  ldsfld     int32 P::i
    IL_0021:  ldc.i4.s   111
    IL_0023:  add
    IL_0024:  stsfld     int32 P::i
    IL_0029:  nop
    IL_002a:  br.s       IL_003b

    IL_002c:  nop
    IL_002d:  ldsfld     int32 P::i
    IL_0032:  ldc.i4.s   110
    IL_0034:  add
    IL_0035:  stsfld     int32 P::i
    IL_003a:  nop
    IL_003b:  nop
    IL_003c:  br.s       IL_0068

    IL_003e:  nop
    IL_003f:  ldarg.2
    IL_0040:  ldc.i4.0
    IL_0041:  ceq
    IL_0043:  stloc.2
    IL_0044:  ldloc.2
    IL_0045:  brtrue.s   IL_0058

    IL_0047:  nop
    IL_0048:  ldsfld     int32 P::i
    IL_004d:  ldc.i4.s   101
    IL_004f:  add
    IL_0050:  stsfld     int32 P::i
    IL_0055:  nop
    IL_0056:  br.s       IL_0067

    IL_0058:  nop
    IL_0059:  ldsfld     int32 P::i
    IL_005e:  ldc.i4.s   100
    IL_0060:  add
    IL_0061:  stsfld     int32 P::i
    IL_0066:  nop
    IL_0067:  nop
    IL_0068:  nop
    IL_0069:  br.s       IL_00c7

    IL_006b:  nop
    IL_006c:  ldarg.1
    IL_006d:  ldc.i4.0
    IL_006e:  ceq
    IL_0070:  stloc.2
    IL_0071:  ldloc.2
    IL_0072:  brtrue.s   IL_00a0

    IL_0074:  nop
    IL_0075:  ldarg.2
    IL_0076:  ldc.i4.0
    IL_0077:  ceq
    IL_0079:  stloc.2
    IL_007a:  ldloc.2
    IL_007b:  brtrue.s   IL_008e

    IL_007d:  nop
    IL_007e:  ldsfld     int32 P::i
    IL_0083:  ldc.i4.s   11
    IL_0085:  add
    IL_0086:  stsfld     int32 P::i
    IL_008b:  nop
    IL_008c:  br.s       IL_009d

    IL_008e:  nop
    IL_008f:  ldsfld     int32 P::i
    IL_0094:  ldc.i4.s   10
    IL_0096:  add
    IL_0097:  stsfld     int32 P::i
    IL_009c:  nop
    IL_009d:  nop
    IL_009e:  br.s       IL_00c6

    IL_00a0:  nop
    IL_00a1:  ldarg.2
    IL_00a2:  ldc.i4.0
    IL_00a3:  ceq
    IL_00a5:  stloc.2
    IL_00a6:  ldloc.2
    IL_00a7:  brtrue.s   IL_00b9

    IL_00a9:  nop
    IL_00aa:  ldsfld     int32 P::i
    IL_00af:  ldc.i4.1
    IL_00b0:  add
    IL_00b1:  stsfld     int32 P::i
    IL_00b6:  nop
    IL_00b7:  br.s       IL_00c5

    IL_00b9:  nop
    IL_00ba:  ldsfld     int32 P::i
    IL_00bf:  stsfld     int32 P::i
    IL_00c4:  nop
    IL_00c5:  nop
    IL_00c6:  nop
    IL_00c7:  newobj     instance void [mscorlib]System.Diagnostics.StackTrace::.ctor()
    IL_00cc:  ldc.i4.0
    IL_00cd:  callvirt   instance class [mscorlib]System.Diagnostics.StackFrame [mscorlib]System.Diagnostics.StackTrace::GetFrame(int32)
    IL_00d2:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_00d7:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_00dc:  stloc.0
    IL_00dd:  ldloc.0
    IL_00de:  ldstr      "A2"
    IL_00e3:  call       bool [mscorlib]System.String::op_Inequality(string,
                                                                     string)
    IL_00e8:  stloc.1
    IL_00e9:  br.s       IL_00eb

    IL_00eb:  ldloc.1
    IL_00ec:  ret
  } // end of method P::A2

  .method public hidebysig static bool  A3(bool b1,
                                           bool b2,
                                           bool b3) cil managed noinlining
  {
    // Code size       237 (0xed)
    .maxstack  2
    .locals init (string V_0,
             bool V_1,
             bool V_2)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.0
    IL_0003:  ceq
    IL_0005:  stloc.2
    IL_0006:  ldloc.2
    IL_0007:  brtrue.s   IL_006b

    IL_0009:  nop
    IL_000a:  ldarg.1
    IL_000b:  ldc.i4.0
    IL_000c:  ceq
    IL_000e:  stloc.2
    IL_000f:  ldloc.2
    IL_0010:  brtrue.s   IL_003e

    IL_0012:  nop
    IL_0013:  ldarg.2
    IL_0014:  ldc.i4.0
    IL_0015:  ceq
    IL_0017:  stloc.2
    IL_0018:  ldloc.2
    IL_0019:  brtrue.s   IL_002c

    IL_001b:  nop
    IL_001c:  ldsfld     int32 P::i
    IL_0021:  ldc.i4.s   111
    IL_0023:  add
    IL_0024:  stsfld     int32 P::i
    IL_0029:  nop
    IL_002a:  br.s       IL_003b

    IL_002c:  nop
    IL_002d:  ldsfld     int32 P::i
    IL_0032:  ldc.i4.s   110
    IL_0034:  add
    IL_0035:  stsfld     int32 P::i
    IL_003a:  nop
    IL_003b:  nop
    IL_003c:  br.s       IL_0068

    IL_003e:  nop
    IL_003f:  ldarg.2
    IL_0040:  ldc.i4.0
    IL_0041:  ceq
    IL_0043:  stloc.2
    IL_0044:  ldloc.2
    IL_0045:  brtrue.s   IL_0058

    IL_0047:  nop
    IL_0048:  ldsfld     int32 P::i
    IL_004d:  ldc.i4.s   101
    IL_004f:  add
    IL_0050:  stsfld     int32 P::i
    IL_0055:  nop
    IL_0056:  br.s       IL_0067

    IL_0058:  nop
    IL_0059:  ldsfld     int32 P::i
    IL_005e:  ldc.i4.s   100
    IL_0060:  add
    IL_0061:  stsfld     int32 P::i
    IL_0066:  nop
    IL_0067:  nop
    IL_0068:  nop
    IL_0069:  br.s       IL_00c7

    IL_006b:  nop
    IL_006c:  ldarg.1
    IL_006d:  ldc.i4.0
    IL_006e:  ceq
    IL_0070:  stloc.2
    IL_0071:  ldloc.2
    IL_0072:  brtrue.s   IL_00a0

    IL_0074:  nop
    IL_0075:  ldarg.2
    IL_0076:  ldc.i4.0
    IL_0077:  ceq
    IL_0079:  stloc.2
    IL_007a:  ldloc.2
    IL_007b:  brtrue.s   IL_008e

    IL_007d:  nop
    IL_007e:  ldsfld     int32 P::i
    IL_0083:  ldc.i4.s   11
    IL_0085:  add
    IL_0086:  stsfld     int32 P::i
    IL_008b:  nop
    IL_008c:  br.s       IL_009d

    IL_008e:  nop
    IL_008f:  ldsfld     int32 P::i
    IL_0094:  ldc.i4.s   10
    IL_0096:  add
    IL_0097:  stsfld     int32 P::i
    IL_009c:  nop
    IL_009d:  nop
    IL_009e:  br.s       IL_00c6

    IL_00a0:  nop
    IL_00a1:  ldarg.2
    IL_00a2:  ldc.i4.0
    IL_00a3:  ceq
    IL_00a5:  stloc.2
    IL_00a6:  ldloc.2
    IL_00a7:  brtrue.s   IL_00b9

    IL_00a9:  nop
    IL_00aa:  ldsfld     int32 P::i
    IL_00af:  ldc.i4.1
    IL_00b0:  add
    IL_00b1:  stsfld     int32 P::i
    IL_00b6:  nop
    IL_00b7:  br.s       IL_00c5

    IL_00b9:  nop
    IL_00ba:  ldsfld     int32 P::i
    IL_00bf:  stsfld     int32 P::i
    IL_00c4:  nop
    IL_00c5:  nop
    IL_00c6:  nop
    IL_00c7:  newobj     instance void [mscorlib]System.Diagnostics.StackTrace::.ctor()
    IL_00cc:  ldc.i4.0
    IL_00cd:  callvirt   instance class [mscorlib]System.Diagnostics.StackFrame [mscorlib]System.Diagnostics.StackTrace::GetFrame(int32)
    IL_00d2:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_00d7:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_00dc:  stloc.0
    IL_00dd:  ldloc.0
    IL_00de:  ldstr      "A3"
    IL_00e3:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_00e8:  stloc.1
    IL_00e9:  br.s       IL_00eb

    IL_00eb:  ldloc.1
    IL_00ec:  ret
  } // end of method P::A3

  .method public hidebysig static bool  A4(bool b1,
                                           bool b2,
                                           bool b3) cil managed noinlining aggressiveinlining
  {
    // Code size       237 (0xed)
    .maxstack  2
    .locals init (string V_0,
             bool V_1,
             bool V_2)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldc.i4.0
    IL_0003:  ceq
    IL_0005:  stloc.2
    IL_0006:  ldloc.2
    IL_0007:  brtrue.s   IL_006b

    IL_0009:  nop
    IL_000a:  ldarg.1
    IL_000b:  ldc.i4.0
    IL_000c:  ceq
    IL_000e:  stloc.2
    IL_000f:  ldloc.2
    IL_0010:  brtrue.s   IL_003e

    IL_0012:  nop
    IL_0013:  ldarg.2
    IL_0014:  ldc.i4.0
    IL_0015:  ceq
    IL_0017:  stloc.2
    IL_0018:  ldloc.2
    IL_0019:  brtrue.s   IL_002c

    IL_001b:  nop
    IL_001c:  ldsfld     int32 P::i
    IL_0021:  ldc.i4.s   111
    IL_0023:  add
    IL_0024:  stsfld     int32 P::i
    IL_0029:  nop
    IL_002a:  br.s       IL_003b

    IL_002c:  nop
    IL_002d:  ldsfld     int32 P::i
    IL_0032:  ldc.i4.s   110
    IL_0034:  add
    IL_0035:  stsfld     int32 P::i
    IL_003a:  nop
    IL_003b:  nop
    IL_003c:  br.s       IL_0068

    IL_003e:  nop
    IL_003f:  ldarg.2
    IL_0040:  ldc.i4.0
    IL_0041:  ceq
    IL_0043:  stloc.2
    IL_0044:  ldloc.2
    IL_0045:  brtrue.s   IL_0058

    IL_0047:  nop
    IL_0048:  ldsfld     int32 P::i
    IL_004d:  ldc.i4.s   101
    IL_004f:  add
    IL_0050:  stsfld     int32 P::i
    IL_0055:  nop
    IL_0056:  br.s       IL_0067

    IL_0058:  nop
    IL_0059:  ldsfld     int32 P::i
    IL_005e:  ldc.i4.s   100
    IL_0060:  add
    IL_0061:  stsfld     int32 P::i
    IL_0066:  nop
    IL_0067:  nop
    IL_0068:  nop
    IL_0069:  br.s       IL_00c7

    IL_006b:  nop
    IL_006c:  ldarg.1
    IL_006d:  ldc.i4.0
    IL_006e:  ceq
    IL_0070:  stloc.2
    IL_0071:  ldloc.2
    IL_0072:  brtrue.s   IL_00a0

    IL_0074:  nop
    IL_0075:  ldarg.2
    IL_0076:  ldc.i4.0
    IL_0077:  ceq
    IL_0079:  stloc.2
    IL_007a:  ldloc.2
    IL_007b:  brtrue.s   IL_008e

    IL_007d:  nop
    IL_007e:  ldsfld     int32 P::i
    IL_0083:  ldc.i4.s   11
    IL_0085:  add
    IL_0086:  stsfld     int32 P::i
    IL_008b:  nop
    IL_008c:  br.s       IL_009d

    IL_008e:  nop
    IL_008f:  ldsfld     int32 P::i
    IL_0094:  ldc.i4.s   10
    IL_0096:  add
    IL_0097:  stsfld     int32 P::i
    IL_009c:  nop
    IL_009d:  nop
    IL_009e:  br.s       IL_00c6

    IL_00a0:  nop
    IL_00a1:  ldarg.2
    IL_00a2:  ldc.i4.0
    IL_00a3:  ceq
    IL_00a5:  stloc.2
    IL_00a6:  ldloc.2
    IL_00a7:  brtrue.s   IL_00b9

    IL_00a9:  nop
    IL_00aa:  ldsfld     int32 P::i
    IL_00af:  ldc.i4.1
    IL_00b0:  add
    IL_00b1:  stsfld     int32 P::i
    IL_00b6:  nop
    IL_00b7:  br.s       IL_00c5

    IL_00b9:  nop
    IL_00ba:  ldsfld     int32 P::i
    IL_00bf:  stsfld     int32 P::i
    IL_00c4:  nop
    IL_00c5:  nop
    IL_00c6:  nop
    IL_00c7:  newobj     instance void [mscorlib]System.Diagnostics.StackTrace::.ctor()
    IL_00cc:  ldc.i4.0
    IL_00cd:  callvirt   instance class [mscorlib]System.Diagnostics.StackFrame [mscorlib]System.Diagnostics.StackTrace::GetFrame(int32)
    IL_00d2:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_00d7:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_00dc:  stloc.0
    IL_00dd:  ldloc.0
    IL_00de:  ldstr      "A4"
    IL_00e3:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_00e8:  stloc.1
    IL_00e9:  br.s       IL_00eb

    IL_00eb:  ldloc.1
    IL_00ec:  ret
  } // end of method P::A4

  .method public hidebysig static bool  A5(bool b1,
                                           bool b2,
                                           bool b3) cil managed noinlining aggressiveinlining
  {
    // Code size       52 (0x34)
    .maxstack  2
    .locals init (string V_0,
             bool V_1)
    IL_0000:  nop
    IL_0001:  ldsfld     int32 P::i
    IL_0006:  ldc.i4.s   50
    IL_0008:  add
    IL_0009:  stsfld     int32 P::i
    IL_000e:  newobj     instance void [mscorlib]System.Diagnostics.StackTrace::.ctor()
    IL_0013:  ldc.i4.0
    IL_0014:  callvirt   instance class [mscorlib]System.Diagnostics.StackFrame [mscorlib]System.Diagnostics.StackTrace::GetFrame(int32)
    IL_0019:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_001e:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_0023:  stloc.0
    IL_0024:  ldloc.0
    IL_0025:  ldstr      "A5"
    IL_002a:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_002f:  stloc.1
    IL_0030:  br.s       IL_0032

    IL_0032:  ldloc.1
    IL_0033:  ret
  } // end of method P::A5

  .method public hidebysig static int32  Main() cil managed
  {
    .entrypoint
    // Code size       190 (0xbe)
    .maxstack  4
    .locals init (bool V_0,
             int32 V_1,
             bool V_2)
    IL_0000:  nop
    IL_0001:  ldstr      "This test confirms the behavior when a method has "
    + "conflicting attribution."
    IL_0006:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldstr      "We expect that A1, A3, and A4 will not be inlined."
    + " They are too complex or marked with NoInlining (which trumps Agressive"
    + "Inlining)."
    IL_0011:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0016:  nop
    IL_0017:  ldstr      "We also confirm that A2 (marked with AgressiveInli"
    + "ning) is properly inlined in spite of its complexity."
    IL_001c:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0021:  nop
    IL_0022:  ldc.i4.1
    IL_0023:  stloc.0
    IL_0024:  ldloc.0
    IL_0025:  ldsfld     bool P::a
    IL_002a:  ldsfld     bool P::b
    IL_002f:  ldsfld     bool P::c
    IL_0034:  call       bool P::A1(bool,
                                    bool,
                                    bool)
    IL_0039:  and
    IL_003a:  stloc.0
    IL_003b:  ldloc.0
    IL_003c:  ldsfld     bool P::a
    IL_0041:  ldsfld     bool P::b
    IL_0046:  ldsfld     bool P::c
    IL_004b:  call       bool P::A2(bool,
                                    bool,
                                    bool)
    IL_0050:  and
    IL_0051:  stloc.0
    IL_0052:  ldloc.0
    IL_0053:  ldsfld     bool P::a
    IL_0058:  ldsfld     bool P::b
    IL_005d:  ldsfld     bool P::c
    IL_0062:  call       bool P::A3(bool,
                                    bool,
                                    bool)
    IL_0067:  and
    IL_0068:  stloc.0
    IL_0069:  ldloc.0
    IL_006a:  ldsfld     bool P::a
    IL_006f:  ldsfld     bool P::b
    IL_0074:  ldsfld     bool P::c
    IL_0079:  call       bool P::A4(bool,
                                    bool,
                                    bool)
    IL_007e:  and
    IL_007f:  stloc.0
    IL_0080:  ldloc.0
    IL_0081:  ldsfld     bool P::a
    IL_0086:  ldsfld     bool P::b
    IL_008b:  ldsfld     bool P::c
    IL_0090:  call       bool P::A5(bool,
                                    bool,
                                    bool)
    IL_0095:  and
    IL_0096:  stloc.0
    IL_0097:  ldloc.0
    IL_0098:  stloc.2
    IL_0099:  ldloc.2
    IL_009a:  brtrue.s   IL_00ac

    IL_009c:  nop
    IL_009d:  ldstr      "FAIL"
    IL_00a2:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00a7:  nop
    IL_00a8:  ldc.i4.0
    IL_00a9:  stloc.1
    IL_00aa:  br.s       IL_00bc

    IL_00ac:  ldstr      "PASS"
    IL_00b1:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00b6:  nop
    IL_00b7:  ldc.i4.s   100
    IL_00b9:  stloc.1
    IL_00ba:  br.s       IL_00bc

    IL_00bc:  ldloc.1
    IL_00bd:  ret
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
    // Code size       25 (0x19)
    .maxstack  8
    IL_0000:  ldc.i4.1
    IL_0001:  stsfld     bool P::a
    IL_0006:  ldc.i4.0
    IL_0007:  stsfld     bool P::b
    IL_000c:  ldc.i4.1
    IL_000d:  stsfld     bool P::c
    IL_0012:  ldc.i4.0
    IL_0013:  stsfld     int32 P::i
    IL_0018:  ret
  } // end of method P::.cctor

} // end of class P
