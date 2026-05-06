// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}





// Metadata version: v4.0.amd64chk
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly 'smallFrame'
{

  // --- The following custom attribute is added automatically, do not uncomment -------
  //  .custom instance void [mscorlib]System.Diagnostics.DebuggableAttribute::.ctor(valuetype [mscorlib]System.Diagnostics.DebuggableAttribute/DebuggingModes) = ( 01 00 02 00 00 00 00 00 ) 

  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.assembly extern xunit.core {}
// MVID: {FD923469-B98F-4B21-ABCA-4105B0A8ED94}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x00000000002B0000


// =============== CLASS MEMBERS DECLARATION ===================

.class private sequential ansi sealed beforefieldinit RetBuff
       extends [mscorlib]System.ValueType
{
  .field public uint64 l1
  .field public uint64 l2
} // end of class RetBuff

.class public abstract auto ansi sealed beforefieldinit RetBufferBug
       extends [mscorlib]System.Object
{
  .field private static int32 s_i
  .field private static int32 s_j
  .field private static int32 s_k
  .field private static int32 s_l
  .field private static int32 s_m
  .field private static int32 s_n
  .field private static int32 s_o
  .field private static int32 s_p
  .method public hidebysig static int32 
          Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       408 (0x198)
    .maxstack  2
    .locals init ([0] int32 result,
             [1] int32 i,
             [2] int32 j,
             [3] int32 k,
             [4] int32 l,
             [5] int32 m,
             [6] int32 n,
             [7] int32 o,
             [8] int32 p)
    .language '{3F5162F8-07C6-11D3-9053-00C04FA302A1}', '{994B45C4-E6E9-11D2-903F-00C04FA302A1}', '{5A869D0B-6611-11D3-BD2A-0000F80849BD}'
    //.line 26,26 : 3,20 'c:\\tests\\dev10\\604601\\retbuf_bug.cs'
    IL_0000:  ldc.i4.s   100
    IL_0002:  stloc.0
    //.line 27,27 : 3,11 ''
    IL_0003:  ldc.i4.0
    IL_0004:  stsfld     int32 RetBufferBug::s_i
    //.line 28,28 : 3,11 ''
    IL_0009:  ldc.i4.0
    IL_000a:  stsfld     int32 RetBufferBug::s_j
    //.line 29,29 : 3,11 ''
    IL_000f:  ldc.i4.0
    IL_0010:  stsfld     int32 RetBufferBug::s_k
    //.line 30,30 : 8,18 ''
    IL_0015:  ldc.i4.0
    IL_0016:  stloc.1
    //.line 16707566,16707566 : 0,0 ''
    IL_0017:  br         IL_016e

    //.line 32,32 : 4,12 ''
    IL_001c:  ldloc.1
    IL_001d:  stsfld     int32 RetBufferBug::s_i
    //.line 33,33 : 9,19 ''
    IL_0022:  ldc.i4.0
    IL_0023:  stloc.2
    //.line 16707566,16707566 : 0,0 ''
    IL_0024:  br         IL_0163

    //.line 35,35 : 5,13 ''
    IL_0029:  ldloc.2
    IL_002a:  stsfld     int32 RetBufferBug::s_j
    //.line 36,36 : 10,20 ''
    IL_002f:  ldc.i4.0
    IL_0030:  stloc.3
    //.line 16707566,16707566 : 0,0 ''
    IL_0031:  br         IL_0158

    //.line 38,38 : 6,14 ''
    IL_0036:  ldloc.3
    IL_0037:  stsfld     int32 RetBufferBug::s_k
    //.line 39,39 : 8,18 ''
    IL_003c:  ldc.i4.0
    IL_003d:  stloc.s    l
    //.line 16707566,16707566 : 0,0 ''
    IL_003f:  br         IL_014c

    //.line 41,41 : 4,12 ''
    IL_0044:  ldloc.s    l
    IL_0046:  stsfld     int32 RetBufferBug::s_l
    //.line 42,42 : 9,19 ''
    IL_004b:  ldc.i4.0
    IL_004c:  stloc.s    m
    //.line 16707566,16707566 : 0,0 ''
    IL_004e:  br         IL_013e

    //.line 44,44 : 5,13 ''
    IL_0053:  ldloc.s    m
    IL_0055:  stsfld     int32 RetBufferBug::s_m
    //.line 45,45 : 10,20 ''
    IL_005a:  ldc.i4.0
    IL_005b:  stloc.s    n
    //.line 16707566,16707566 : 0,0 ''
    IL_005d:  br         IL_0130

    //.line 47,47 : 6,14 ''
    IL_0062:  ldloc.s    n
    IL_0064:  stsfld     int32 RetBufferBug::s_n
    //.line 48,48 : 8,18 ''
    IL_0069:  ldc.i4.0
    IL_006a:  stloc.s    o
    //.line 16707566,16707566 : 0,0 ''
    IL_006c:  br         IL_0122

    //.line 50,50 : 4,12 ''
    IL_0071:  ldloc.s    o
    IL_0073:  stsfld     int32 RetBufferBug::s_o
    //.line 51,51 : 9,19 ''
    IL_0078:  ldc.i4.0
    IL_0079:  stloc.s    p
    //.line 16707566,16707566 : 0,0 ''
    IL_007b:  br         IL_0114

    //.line 53,53 : 5,13 ''
    IL_0080:  ldloc.s    p
    IL_0082:  stsfld     int32 RetBufferBug::s_p
    //.line 54,54 : 5,18 ''
    IL_0087:  call       void RetBufferBug::TailCaller()
    //.line 55,55 : 5,18 ''
    IL_008c:  ldsfld     int32 RetBufferBug::s_i
    IL_0091:  ldloc.1
    IL_0092:  beq.s      IL_009c

    //.line 57,57 : 6,18 ''
    IL_0094:  ldc.i4.s   99
    IL_0096:  stloc.0
    //.line 58,58 : 6,16 ''
    IL_0097:  br         IL_0186

    //.line 60,60 : 5,18 ''
    IL_009c:  ldsfld     int32 RetBufferBug::s_j
    IL_00a1:  ldloc.2
    IL_00a2:  beq.s      IL_00ac

    //.line 62,62 : 6,18 ''
    IL_00a4:  ldc.i4.s   98
    IL_00a6:  stloc.0
    //.line 63,63 : 6,16 ''
    IL_00a7:  br         IL_0186

    //.line 65,65 : 5,18 ''
    IL_00ac:  ldsfld     int32 RetBufferBug::s_k
    IL_00b1:  ldloc.3
    IL_00b2:  beq.s      IL_00bc

    //.line 67,67 : 6,18 ''
    IL_00b4:  ldc.i4.s   97
    IL_00b6:  stloc.0
    //.line 68,68 : 6,16 ''
    IL_00b7:  br         IL_0186

    //.line 70,70 : 5,18 ''
    IL_00bc:  ldsfld     int32 RetBufferBug::s_l
    IL_00c1:  ldloc.s    l
    IL_00c3:  beq.s      IL_00cd

    //.line 72,72 : 6,18 ''
    IL_00c5:  ldc.i4.s   96
    IL_00c7:  stloc.0
    //.line 73,73 : 6,16 ''
    IL_00c8:  br         IL_0186

    //.line 75,75 : 5,18 ''
    IL_00cd:  ldsfld     int32 RetBufferBug::s_m
    IL_00d2:  ldloc.s    m
    IL_00d4:  beq.s      IL_00de

    //.line 77,77 : 6,18 ''
    IL_00d6:  ldc.i4.s   95
    IL_00d8:  stloc.0
    //.line 78,78 : 6,16 ''
    IL_00d9:  br         IL_0186

    //.line 80,80 : 5,18 ''
    IL_00de:  ldsfld     int32 RetBufferBug::s_n
    IL_00e3:  ldloc.s    n
    IL_00e5:  beq.s      IL_00ef

    //.line 82,82 : 6,18 ''
    IL_00e7:  ldc.i4.s   94
    IL_00e9:  stloc.0
    //.line 83,83 : 6,16 ''
    IL_00ea:  br         IL_0186

    //.line 85,85 : 5,18 ''
    IL_00ef:  ldsfld     int32 RetBufferBug::s_o
    IL_00f4:  ldloc.s    o
    IL_00f6:  beq.s      IL_0100

    //.line 87,87 : 6,18 ''
    IL_00f8:  ldc.i4.s   93
    IL_00fa:  stloc.0
    //.line 88,88 : 6,16 ''
    IL_00fb:  br         IL_0186

    //.line 90,90 : 5,18 ''
    IL_0100:  ldsfld     int32 RetBufferBug::s_p
    IL_0105:  ldloc.s    p
    IL_0107:  beq.s      IL_010e

    //.line 92,92 : 6,18 ''
    IL_0109:  ldc.i4.s   94
    IL_010b:  stloc.0
    //.line 93,93 : 6,16 ''
    IL_010c:  br.s       IL_0186

    //.line 51,51 : 27,30 ''
    IL_010e:  ldloc.s    p
    IL_0110:  ldc.i4.1
    IL_0111:  add
    IL_0112:  stloc.s    p
    //.line 51,51 : 20,25 ''
    IL_0114:  ldloc.s    p
    IL_0116:  ldc.i4.7
    IL_0117:  blt        IL_0080

    //.line 48,48 : 26,29 ''
    IL_011c:  ldloc.s    o
    IL_011e:  ldc.i4.1
    IL_011f:  add
    IL_0120:  stloc.s    o
    //.line 48,48 : 19,24 ''
    IL_0122:  ldloc.s    o
    IL_0124:  ldc.i4.7
    IL_0125:  blt        IL_0071

    //.line 45,45 : 28,31 ''
    IL_012a:  ldloc.s    n
    IL_012c:  ldc.i4.1
    IL_012d:  add
    IL_012e:  stloc.s    n
    //.line 45,45 : 21,26 ''
    IL_0130:  ldloc.s    n
    IL_0132:  ldc.i4.7
    IL_0133:  blt        IL_0062

    //.line 42,42 : 27,30 ''
    IL_0138:  ldloc.s    m
    IL_013a:  ldc.i4.1
    IL_013b:  add
    IL_013c:  stloc.s    m
    //.line 42,42 : 20,25 ''
    IL_013e:  ldloc.s    m
    IL_0140:  ldc.i4.7
    IL_0141:  blt        IL_0053

    //.line 39,39 : 26,29 ''
    IL_0146:  ldloc.s    l
    IL_0148:  ldc.i4.1
    IL_0149:  add
    IL_014a:  stloc.s    l
    //.line 39,39 : 19,24 ''
    IL_014c:  ldloc.s    l
    IL_014e:  ldc.i4.7
    IL_014f:  blt        IL_0044

    //.line 36,36 : 28,31 ''
    IL_0154:  ldloc.3
    IL_0155:  ldc.i4.1
    IL_0156:  add
    IL_0157:  stloc.3
    //.line 36,36 : 21,26 ''
    IL_0158:  ldloc.3
    IL_0159:  ldc.i4.7
    IL_015a:  blt        IL_0036

    //.line 33,33 : 27,30 ''
    IL_015f:  ldloc.2
    IL_0160:  ldc.i4.1
    IL_0161:  add
    IL_0162:  stloc.2
    //.line 33,33 : 20,25 ''
    IL_0163:  ldloc.2
    IL_0164:  ldc.i4.7
    IL_0165:  blt        IL_0029

    //.line 30,30 : 26,29 ''
    IL_016a:  ldloc.1
    IL_016b:  ldc.i4.1
    IL_016c:  add
    IL_016d:  stloc.1
    //.line 30,30 : 19,24 ''
    IL_016e:  ldloc.1
    IL_016f:  ldc.i4.7
    IL_0170:  blt        IL_001c

    //.line 103,103 : 3,21 ''
    IL_0175:  ldloc.0
    IL_0176:  ldc.i4.s   100
    IL_0178:  bne.un.s   IL_0186

    //.line 105,105 : 4,30 ''
    IL_017a:  ldstr      "Pass"
    IL_017f:  call       void [System.Console]System.Console::WriteLine(string)
    //.line 106,106 : 4,18 ''
    IL_0184:  ldloc.0
    IL_0185:  ret

    //.line 109,109 : 3,44 ''
    IL_0186:  ldstr      "Failed: {0}"
    IL_018b:  ldloc.0
    IL_018c:  box        [mscorlib]System.Int32
    IL_0191:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    //.line 110,110 : 3,17 ''
    IL_0196:  ldloc.0
    IL_0197:  ret
  } // end of method RetBufferBug::Main

  .method private hidebysig static void  TailCaller() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    //.line 114,114 : 3,16 ''
//            tail.      // tail.call, pop, ret sequence is never valid for .NET Core (but is accepted by .NET x64)
    IL_0000:  call       valuetype RetBuff RetBufferBug::TailCallee()
    IL_0005:  pop
    //.line 115,115 : 2,3 ''
    IL_0006:  ret
  } // end of method RetBufferBug::TailCaller

  .method private hidebysig static valuetype RetBuff 
          TailCallee() cil managed
  {
    // Code size       31 (0x1f)
    .maxstack  2
    .locals init ([0] valuetype RetBuff result)
    //.line 119,119 : 3,25 ''
    IL_0000:  ldloca.s   result
    IL_0002:  ldc.i4     0xbadf00d
    IL_0007:  conv.i8
    IL_0008:  stfld      uint64 RetBuff::l1
    //.line 120,120 : 3,36 ''
    IL_000d:  ldloca.s   result
    IL_000f:  ldc.i8     0x123456789abcdeef
    IL_0018:  stfld      uint64 RetBuff::l2
    //.line 121,121 : 3,17 ''
    IL_001d:  ldloc.0
    IL_001e:  ret
  } // end of method RetBufferBug::TailCallee

} // end of class RetBufferBug


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file retbuf_bug.res
