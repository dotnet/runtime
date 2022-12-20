// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly PositiveCases
{
}


.class public sequential ansi sealed beforefieldinit S
       extends [mscorlib]System.ValueType
{
  .pack 0
  .size 1
  .field public static int32 k
  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldc.i4.s   17
    IL_0002:  stsfld     int32 S::k
    IL_0007:  ret
  } // end of method S::.cctor

} // end of class S

.class public auto ansi beforefieldinit P
       extends [mscorlib]System.Object
{
  .field public static bool a
  .field public static bool b
  .field public static bool c
  .field private static int32 i
  .method public hidebysig static bool  A1(bool b1,
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
    IL_00de:  ldstr      "A1"
    IL_00e3:  call       bool [mscorlib]System.String::op_Inequality(string,
                                                                     string)
    IL_00e8:  stloc.1
    IL_00e9:  br.s       IL_00eb

    IL_00eb:  ldloc.1
    IL_00ec:  ret
  } // end of method P::A1

  .method public hidebysig static bool  A2(bool b1,
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
    IL_00de:  ldstr      "A2"
    IL_00e3:  call       bool [mscorlib]System.String::op_Inequality(string,
                                                                     string)
    IL_00e8:  stloc.1
    IL_00e9:  br.s       IL_00eb

    IL_00eb:  ldloc.1
    IL_00ec:  ret
  } // end of method P::A2

  .method public hidebysig static bool  A3() cil managed aggressiveinlining
  {
    // Code size       51 (0x33)
    .maxstack  2
    .locals init (string V_0,
             bool V_1)
    IL_0000:  nop
    IL_0001:  ldsfld     int32 S::k
    IL_0006:  ldc.i4.1
    IL_0007:  add
    IL_0008:  stsfld     int32 S::k
    IL_000d:  newobj     instance void [mscorlib]System.Diagnostics.StackTrace::.ctor()
    IL_0012:  ldc.i4.0
    IL_0013:  callvirt   instance class [mscorlib]System.Diagnostics.StackFrame [mscorlib]System.Diagnostics.StackTrace::GetFrame(int32)
    IL_0018:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_001d:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_0022:  stloc.0
    IL_0023:  ldloc.0
    IL_0024:  ldstr      "A3"
    IL_0029:  call       bool [mscorlib]System.String::op_Inequality(string,
                                                                     string)
    IL_002e:  stloc.1
    IL_002f:  br.s       IL_0031

    IL_0031:  ldloc.1
    IL_0032:  ret
  } // end of method P::A3

  .method public hidebysig static bool  A4() cil managed
  {
    // Code size       51 (0x33)
    .maxstack  2
    .locals init (string V_0,
             bool V_1)
    IL_0000:  nop
    IL_0001:  ldsfld     int32 S::k
    IL_0006:  ldc.i4.1
    IL_0007:  add
    IL_0008:  stsfld     int32 S::k
    IL_000d:  newobj     instance void [mscorlib]System.Diagnostics.StackTrace::.ctor()
    IL_0012:  ldc.i4.0
    IL_0013:  callvirt   instance class [mscorlib]System.Diagnostics.StackFrame [mscorlib]System.Diagnostics.StackTrace::GetFrame(int32)
    IL_0018:  callvirt   instance class [mscorlib]System.Reflection.MethodBase [mscorlib]System.Diagnostics.StackFrame::GetMethod()
    IL_001d:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_0022:  stloc.0
    IL_0023:  ldloc.0
    IL_0024:  ldstr      "A4"
    IL_0029:  call       bool [mscorlib]System.String::op_Inequality(string,
                                                                     string)
    IL_002e:  stloc.1
    IL_002f:  br.s       IL_0031

    IL_0031:  ldloc.1
    IL_0032:  ret
  } // end of method P::A4

  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       314 (0x13a)
    .maxstack  3
    .locals init (string V_0,
             int32 V_1,
             bool V_2)
    IL_0000:  nop
    IL_0001:  ldnull
    IL_0002:  stloc.0
    IL_0003:  ldsfld     bool P::a
    IL_0008:  ldsfld     bool P::b
    IL_000d:  ldsfld     bool P::c
    IL_0012:  call       bool P::A1(bool,
                                    bool,
                                    bool)
    IL_0017:  brfalse.s  IL_0032

    IL_0019:  ldsfld     bool P::a
    IL_001e:  ldsfld     bool P::b
    IL_0023:  ldsfld     bool P::c
    IL_0028:  call       bool P::A2(bool,
                                    bool,
                                    bool)
    IL_002d:  ldc.i4.0
    IL_002e:  ceq
    IL_0030:  br.s       IL_0033

    IL_0032:  ldc.i4.0
    IL_0033:  nop
    IL_0034:  stloc.2
    IL_0035:  ldloc.2
    IL_0036:  brtrue.s   IL_0040

    IL_0038:  nop
    IL_0039:  ldstr      "Method body"
    IL_003e:  stloc.0
    IL_003f:  nop
    .try
    {
      .try
      {
        IL_0040:  nop
        IL_0041:  ldsfld     bool P::a
        IL_0046:  ldsfld     bool P::b
        IL_004b:  ldsfld     bool P::c
        IL_0050:  call       bool P::A1(bool,
                                        bool,
                                        bool)
        IL_0055:  brfalse.s  IL_0070

        IL_0057:  ldsfld     bool P::a
        IL_005c:  ldsfld     bool P::b
        IL_0061:  ldsfld     bool P::c
        IL_0066:  call       bool P::A2(bool,
                                        bool,
                                        bool)
        IL_006b:  ldc.i4.0
        IL_006c:  ceq
        IL_006e:  br.s       IL_0071

        IL_0070:  ldc.i4.0
        IL_0071:  nop
        IL_0072:  stloc.2
        IL_0073:  ldloc.2
        IL_0074:  brtrue.s   IL_007e

        IL_0076:  nop
        IL_0077:  ldstr      "Try block"
        IL_007c:  stloc.0
        IL_007d:  nop
        IL_007e:  newobj     instance void [mscorlib]System.Exception::.ctor()
        IL_0083:  throw

      }  // end .try
      catch [mscorlib]System.Exception 
      {
        IL_0084:  pop
        IL_0085:  nop
        IL_0086:  ldsfld     bool P::a
        IL_008b:  ldsfld     bool P::b
        IL_0090:  ldsfld     bool P::c
        IL_0095:  call       bool P::A1(bool,
                                        bool,
                                        bool)
        IL_009a:  brfalse.s  IL_00b5

        IL_009c:  ldsfld     bool P::a
        IL_00a1:  ldsfld     bool P::b
        IL_00a6:  ldsfld     bool P::c
        IL_00ab:  call       bool P::A2(bool,
                                        bool,
                                        bool)
        IL_00b0:  ldc.i4.0
        IL_00b1:  ceq
        IL_00b3:  br.s       IL_00b6

        IL_00b5:  ldc.i4.0
        IL_00b6:  nop
        IL_00b7:  stloc.2
        IL_00b8:  ldloc.2
        IL_00b9:  brtrue.s   IL_00c3

        IL_00bb:  nop
        IL_00bc:  ldstr      "Catch block"
        IL_00c1:  stloc.0
        IL_00c2:  nop
        IL_00c3:  nop
        IL_00c4:  leave.s    IL_00c6

      }  // end handler
      IL_00c6:  nop
      IL_00c7:  leave.s    IL_0109

    }  // end .try
    finally
    {
      IL_00c9:  nop
      IL_00ca:  ldsfld     bool P::a
      IL_00cf:  ldsfld     bool P::b
      IL_00d4:  ldsfld     bool P::c
      IL_00d9:  call       bool P::A1(bool,
                                      bool,
                                      bool)
      IL_00de:  brfalse.s  IL_00f9

      IL_00e0:  ldsfld     bool P::a
      IL_00e5:  ldsfld     bool P::b
      IL_00ea:  ldsfld     bool P::c
      IL_00ef:  call       bool P::A2(bool,
                                      bool,
                                      bool)
      IL_00f4:  ldc.i4.0
      IL_00f5:  ceq
      IL_00f7:  br.s       IL_00fa

      IL_00f9:  ldc.i4.0
      IL_00fa:  nop
      IL_00fb:  stloc.2
      IL_00fc:  ldloc.2
      IL_00fd:  brtrue.s   IL_0107

      IL_00ff:  nop
      IL_0100:  ldstr      "Finally block"
      IL_0105:  stloc.0
      IL_0106:  nop
      IL_0107:  nop
      IL_0108:  endfinally
    }  // end handler
    IL_0109:  nop
    IL_010a:  ldloc.0
    IL_010b:  ldnull
    IL_010c:  ceq
    IL_010e:  stloc.2
    IL_010f:  ldloc.2
    IL_0110:  brtrue.s   IL_0128

    IL_0112:  nop
    IL_0113:  ldstr      "FAIL -- Unexpected inlining result in: "
    IL_0118:  ldloc.0
    IL_0119:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_011e:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0123:  nop
    IL_0124:  ldc.i4.0
    IL_0125:  stloc.1
    IL_0126:  br.s       IL_0138

    IL_0128:  ldstr      "PASS"
    IL_012d:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0132:  nop
    IL_0133:  ldc.i4.s   100
    IL_0135:  stloc.1
    IL_0136:  br.s       IL_0138

    IL_0138:  ldloc.1
    IL_0139:  ret
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
