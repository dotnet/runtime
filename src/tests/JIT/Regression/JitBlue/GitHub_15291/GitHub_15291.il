// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


// Test for casts of long shifts.
//
// Same test cases as in GitHub_15077, but without the extra
// and masking done by CSC.
.assembly extern System.Console { auto }
.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern mscorlib { auto }

.assembly 'GitHub_15291' {}
.assembly extern xunit.core {}

.class public auto ansi beforefieldinit P
       extends [mscorlib]System.Object
{
  .method public hidebysig static uint32
          G32() cil managed noinlining
  {
    IL_0003:  ldc.i4.1
    IL_0004:  conv.i8
    IL_0000:  ldc.i4.s   32
    IL_0009:  shl
    IL_000a:  ldc.i4.1
    IL_000b:  conv.i8
    IL_000c:  sub
    IL_000d:  conv.u4
    IL_000e:  ret
  }

  .method public hidebysig static uint32
          G31() cil managed noinlining
  {
    IL_0003:  ldc.i4.1
    IL_0004:  conv.i8
    IL_0000:  ldc.i4.s   31
    IL_0009:  shl
    IL_000a:  ldc.i4.1
    IL_000b:  conv.i8
    IL_000c:  sub
    IL_000d:  conv.u4
    IL_000e:  ret
  }

  .method public hidebysig static uint32
          G64() cil managed noinlining
  {
    IL_0003:  ldc.i4.1
    IL_0004:  conv.i8
    IL_0000:  ldc.i4.s   64
    IL_0009:  shl
    IL_000a:  ldc.i4.1
    IL_000b:  conv.i8
    IL_000c:  sub
    IL_000d:  conv.u4
    IL_000e:  ret
  }

  .method public hidebysig static uint32
          G63() cil managed noinlining
  {
    IL_0003:  ldc.i4.1
    IL_0004:  conv.i8
    IL_0000:  ldc.i4.s   63
    IL_0009:  shl
    IL_000a:  ldc.i4.1
    IL_000b:  conv.i8
    IL_000c:  sub
    IL_000d:  conv.u4
    IL_000e:  ret
  }

  .method public hidebysig static uint32
          GM1() cil managed noinlining
  {
    IL_0002:  ldc.i4.1
    IL_0003:  conv.i8
    IL_0000:  ldc.i4.m1
    IL_0008:  shl
    IL_0009:  ldc.i4.1
    IL_000a:  conv.i8
    IL_000b:  sub
    IL_000c:  conv.u4
    IL_000d:  ret
  }

  .method public hidebysig static uint32
          Gx(int32 q) cil managed noinlining
  {
    IL_0000:  ldc.i4.1
    IL_0001:  conv.i8
    IL_0002:  ldarg.0
    IL_0006:  shl
    IL_0007:  ldc.i4.1
    IL_0008:  conv.i8
    IL_0009:  sub
    IL_000a:  conv.u4
    IL_000b:  ret
  }

  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  3
    .locals init ([0] uint32 r64,
             [1] uint32 r63,
             [2] uint32 r32,
             [3] uint32 r31,
             [4] uint32 rm1,
             [5] uint32 r64a,
             [6] uint32 r63a,
             [7] uint32 r32a,
             [8] uint32 r31a,
             [9] uint32 rm1a,
             [10] bool b64,
             [11] bool b63,
             [12] bool b32,
             [13] bool b31,
             [14] bool bm1)
    IL_0000:  call       uint32 P::G64()
    IL_0005:  stloc.0
    IL_0006:  call       uint32 P::G63()
    IL_000b:  stloc.1
    IL_000c:  call       uint32 P::G32()
    IL_0011:  stloc.2
    IL_0012:  call       uint32 P::G31()
    IL_0017:  stloc.3
    IL_0018:  call       uint32 P::GM1()
    IL_001d:  stloc.s    rm1
    IL_001f:  ldc.i4.s   64
    IL_0021:  call       uint32 P::Gx(int32)
    IL_0026:  stloc.s    r64a
    IL_0028:  ldc.i4.s   63
    IL_002a:  call       uint32 P::Gx(int32)
    IL_002f:  stloc.s    r63a
    IL_0031:  ldc.i4.s   32
    IL_0033:  call       uint32 P::Gx(int32)
    IL_0038:  stloc.s    r32a
    IL_003a:  ldc.i4.s   31
    IL_003c:  call       uint32 P::Gx(int32)
    IL_0041:  stloc.s    r31a
    IL_0043:  ldc.i4.m1
    IL_0044:  call       uint32 P::Gx(int32)
    IL_0049:  stloc.s    rm1a
    IL_004b:  ldstr      "r64:{0,0:X8} r64a:{1,0:X8}"
    IL_0050:  ldloc.0
    IL_0051:  box        [System.Runtime]System.UInt32
    IL_0056:  ldloc.s    r64a
    IL_0058:  box        [System.Runtime]System.UInt32
    IL_005d:  call       string [System.Runtime]System.String::Format(string,
                                                                      object,
                                                                      object)
    IL_0062:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0067:  ldstr      "r63:{0,0:X8} r63a:{1,0:X8}"
    IL_006c:  ldloc.1
    IL_006d:  box        [System.Runtime]System.UInt32
    IL_0072:  ldloc.s    r63a
    IL_0074:  box        [System.Runtime]System.UInt32
    IL_0079:  call       string [System.Runtime]System.String::Format(string,
                                                                      object,
                                                                      object)
    IL_007e:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0083:  ldstr      "r32:{0,0:X8} r32a:{1,0:X8}"
    IL_0088:  ldloc.2
    IL_0089:  box        [System.Runtime]System.UInt32
    IL_008e:  ldloc.s    r32a
    IL_0090:  box        [System.Runtime]System.UInt32
    IL_0095:  call       string [System.Runtime]System.String::Format(string,
                                                                      object,
                                                                      object)
    IL_009a:  call       void [System.Console]System.Console::WriteLine(string)
    IL_009f:  ldstr      "r31:{0,0:X8} r31a:{1,0:X8}"
    IL_00a4:  ldloc.3
    IL_00a5:  box        [System.Runtime]System.UInt32
    IL_00aa:  ldloc.s    r31a
    IL_00ac:  box        [System.Runtime]System.UInt32
    IL_00b1:  call       string [System.Runtime]System.String::Format(string,
                                                                      object,
                                                                      object)
    IL_00b6:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00bb:  ldstr      "rm1:{0,0:X8} rm1a:{1,0:X8}"
    IL_00c0:  ldloc.s    rm1
    IL_00c2:  box        [System.Runtime]System.UInt32
    IL_00c7:  ldloc.s    rm1a
    IL_00c9:  box        [System.Runtime]System.UInt32
    IL_00ce:  call       string [System.Runtime]System.String::Format(string,
                                                                      object,
                                                                      object)
    IL_00d3:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00d8:  ldloc.0
    IL_00d9:  brtrue.s   IL_00e2

    IL_00db:  ldloc.s    r64a
    IL_00dd:  ldloc.0
    IL_00de:  ceq
    IL_00e0:  br.s       IL_00e3

    IL_00e2:  ldc.i4.0
    IL_00e3:  stloc.s    b64
    IL_00e5:  ldloc.1
    IL_00e6:  ldc.i4.m1
    IL_00e7:  bne.un.s   IL_00f0

    IL_00e9:  ldloc.s    r63a
    IL_00eb:  ldloc.1
    IL_00ec:  ceq
    IL_00ee:  br.s       IL_00f1

    IL_00f0:  ldc.i4.0
    IL_00f1:  stloc.s    b63
    IL_00f3:  ldloc.2
    IL_00f4:  ldc.i4.m1
    IL_00f5:  bne.un.s   IL_00fe

    IL_00f7:  ldloc.s    r32a
    IL_00f9:  ldloc.2
    IL_00fa:  ceq
    IL_00fc:  br.s       IL_00ff

    IL_00fe:  ldc.i4.0
    IL_00ff:  stloc.s    b32
    IL_0101:  ldloc.3
    IL_0102:  ldc.i4     0x7fffffff
    IL_0107:  bne.un.s   IL_0110

    IL_0109:  ldloc.s    r31a
    IL_010b:  ldloc.3
    IL_010c:  ceq
    IL_010e:  br.s       IL_0111

    IL_0110:  ldc.i4.0
    IL_0111:  stloc.s    b31
    IL_0113:  ldloc.s    rm1
    IL_0115:  ldc.i4.m1
    IL_0116:  bne.un.s   IL_0120

    IL_0118:  ldloc.s    rm1a
    IL_011a:  ldloc.s    rm1
    IL_011c:  ceq
    IL_011e:  br.s       IL_0121

    IL_0120:  ldc.i4.0
    IL_0121:  stloc.s    bm1
    IL_0123:  ldloc.s    b64
    IL_0125:  ldloc.s    b63
    IL_0127:  and
    IL_0128:  ldloc.s    b32
    IL_012a:  and
    IL_012b:  ldloc.s    b31
    IL_012d:  and
    IL_012e:  ldloc.s    bm1
    IL_0130:  and
    IL_0131:  brtrue.s   IL_0135

    IL_0133:  ldc.i4.0
    IL_0134:  ret

    IL_0135:  ldc.i4.s   100
    IL_0137:  ret
  }

}
