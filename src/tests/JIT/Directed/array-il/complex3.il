// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}

.assembly extern legacy library mscorlib {}
.assembly '_Arrayscomplex3'
{
}

.class value public auto ansi sealed Yak
{
  .field public int32 a
  .field private string foo
  .field public int32 b
  .method public instance void Do_Something() il managed
  {
    .maxstack  8

    IL_0000:  ldarg.0
    IL_0001:  ldarg.0
    IL_0002:  ldflda     int32 Yak::a
    IL_0007:  call       instance string [mscorlib]System.Int32::ToString()
    IL_000c:  stfld      string Yak::foo
    IL_0011:  ldarg.0
    IL_0012:  dup
    IL_0013:  ldfld      int32 Yak::b
    IL_0018:  ldarg.0
    IL_0019:  ldfld      int32 Yak::a
    IL_001e:  add
    IL_001f:  stfld      int32 Yak::b
    IL_0024:  ret
  } // end of method 'Yak::Do_Something'

} // end of class 'Yak'

.class public auto ansi Complex2_Array_Test
{
   .method public static int32 TestRank(value class Yak[,,,,,,] Odd_Variable) il managed
  {
    .maxstack  3
    .locals (int32 V_0)

    IL_0000:  ldstr      "Rank is :"
    IL_0005:  ldarg.0
    IL_0006:  callvirt   instance int32 [mscorlib]System.Array::get_Rank()
    IL_000b:  stloc.0
    IL_000c:  ldloca.s   V_0
    IL_000e:  call       instance string [mscorlib]System.Int32::ToString()
    IL_0013:  call       class System.String [mscorlib]System.String::Concat(class System.String,class System.String)
    IL_0018:  call       void [System.Console]System.Console::WriteLine(string)
              ldloc 0
    IL_001d:  ret
  }

 .method public static void test(value class Yak[0...,0...,0...,0...,0...,0...,0...] Odd_Variable) il managed
  {
    .maxstack  8

    IL_0000:  ldarg.0
    IL_0001:  call   instance int32 [mscorlib]System.Array::get_Length()
    IL_0006:  call       void [System.Console]System.Console::Write(int32)
    IL_000b:  ret
  } // end of method 'Complex2_Array_Test::test'

  .method public static int32 Main() il managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  9
    .locals (int32 SIZE,
             int64 sum,
             value class Yak[0...,0...,0...,0...,0...,0...,0...] foo,
             int32 i,
             int32 j,
             int32 k,
             int32 l,
             int32 m,
             int32 n,
             int32 o,
             int32 V_10)

    IL_0000:  ldstr      "Starting..."
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.2
    IL_000b:  stloc.0
    IL_000c:  ldc.i4.0
    IL_000d:  conv.i8
    IL_000e:  stloc.1
    IL_000f:  ldloc.0
    IL_0010:  ldloc.0
    IL_0011:  ldloc.0
    IL_0012:  ldloc.0
    IL_0013:  ldloc.0
    IL_0014:  ldloc.0
    IL_0015:  ldloc.0
    IL_0016:  newobj     instance void valuetype Yak[,,,,,,]::.ctor(int32,int32,int32,int32,int32,int32,int32)
    IL_001b:  stloc.2
ldloc 2
call int32 Complex2_Array_Test::TestRank(value class Yak[,,,,,,])
ldc.i4 7
bne.un IL_0200
    IL_001c:  ldc.i4.0

    IL_001d:  stloc.3
    IL_001e:  br         IL_0113

    IL_0023:  ldc.i4.0
    IL_0024:  stloc.s    j
    IL_0026:  br         IL_0107

    IL_002b:  ldc.i4.0
    IL_002c:  stloc.s    k
    IL_002e:  br         IL_00f9

    IL_0033:  ldc.i4.0
    IL_0034:  stloc.s    l
    IL_0036:  br         IL_00eb

    IL_003b:  ldc.i4.0
    IL_003c:  stloc.s    m
    IL_003e:  br         IL_00dd

    IL_0043:  ldc.i4.0
    IL_0044:  stloc.s    n
    IL_0046:  br         IL_00cf

    IL_004b:  ldc.i4.0
    IL_004c:  stloc.s    o
    IL_004e:  br.s       IL_00c4

    IL_0050:  ldloc.2
    IL_0051:  ldloc.3
    IL_0052:  ldloc.s    j
    IL_0054:  ldloc.s    k
    IL_0056:  ldloc.s    l
    IL_0058:  ldloc.s    m
    IL_005a:  ldloc.s    n
    IL_005c:  ldloc.s    o
    IL_005e:  call       instance value class Yak& valuetype Yak[,,,,,,]::Address(int32,int32,int32,int32,int32,int32,int32)
    IL_0063:  ldloc.3
    IL_0064:  ldloc.s    j
    IL_0066:  mul
    IL_0067:  ldloc.s    k
    IL_0069:  mul
    IL_006a:  ldloc.s    l
    IL_006c:  mul
    IL_006d:  ldloc.s    m
    IL_006f:  mul
    IL_0070:  ldloc.s    n
    IL_0072:  mul
    IL_0073:  ldloc.s    o
    IL_0075:  mul
    IL_0076:  stfld      int32 Yak::a
    IL_007b:  ldloc.2
    IL_007c:  ldloc.3
    IL_007d:  ldloc.s    j
    IL_007f:  ldloc.s    k
    IL_0081:  ldloc.s    l
    IL_0083:  ldloc.s    m
    IL_0085:  ldloc.s    n
    IL_0087:  ldloc.s    o
    IL_0089:  call       instance value class Yak& valuetype Yak[,,,,,,]::Address(int32,int32,int32,int32,int32,int32,int32)
    IL_008e:  ldloc.3
    IL_008f:  ldloc.s    j
    IL_0091:  add
    IL_0092:  ldloc.s    k
    IL_0094:  add
    IL_0095:  ldloc.s    l
    IL_0097:  add
    IL_0098:  ldloc.s    m
    IL_009a:  add
    IL_009b:  ldloc.s    n
    IL_009d:  add
    IL_009e:  ldloc.s    o
    IL_00a0:  add
    IL_00a1:  stfld      int32 Yak::b
    IL_00a6:  ldloc.2
    IL_00a7:  ldloc.3
    IL_00a8:  ldloc.s    j
    IL_00aa:  ldloc.s    k
    IL_00ac:  ldloc.s    l
    IL_00ae:  ldloc.s    m
    IL_00b0:  ldloc.s    n
    IL_00b2:  ldloc.s    o
    IL_00b4:  call       instance value class Yak& valuetype Yak[,,,,,,]::Address(int32,int32,int32,int32,int32,int32,int32)
    IL_00b9:  call       instance void Yak::Do_Something()
    IL_00be:  ldloc.s    o
    IL_00c0:  ldc.i4.1
    IL_00c1:  add
    IL_00c2:  stloc.s    o
    IL_00c4:  ldloc.s    o
    IL_00c6:  ldloc.0
    IL_00c7:  blt.s      IL_0050

    IL_00c9:  ldloc.s    n
    IL_00cb:  ldc.i4.1
    IL_00cc:  add
    IL_00cd:  stloc.s    n
    IL_00cf:  ldloc.s    n
    IL_00d1:  ldloc.0
    IL_00d2:  blt        IL_004b

    IL_00d7:  ldloc.s    m
    IL_00d9:  ldc.i4.1
    IL_00da:  add
    IL_00db:  stloc.s    m
    IL_00dd:  ldloc.s    m
    IL_00df:  ldloc.0
    IL_00e0:  blt        IL_0043

    IL_00e5:  ldloc.s    l
    IL_00e7:  ldc.i4.1
    IL_00e8:  add
    IL_00e9:  stloc.s    l
    IL_00eb:  ldloc.s    l
    IL_00ed:  ldloc.0
    IL_00ee:  blt        IL_003b

    IL_00f3:  ldloc.s    k
    IL_00f5:  ldc.i4.1
    IL_00f6:  add
    IL_00f7:  stloc.s    k
    IL_00f9:  ldloc.s    k
    IL_00fb:  ldloc.0
    IL_00fc:  blt        IL_0033

    IL_0101:  ldloc.s    j
    IL_0103:  ldc.i4.1
    IL_0104:  add
    IL_0105:  stloc.s    j
    IL_0107:  ldloc.s    j
    IL_0109:  ldloc.0
    IL_010a:  blt        IL_002b

    IL_010f:  ldloc.3
    IL_0110:  ldc.i4.1
    IL_0111:  add
    IL_0112:  stloc.3
    IL_0113:  ldloc.3
    IL_0114:  ldloc.0
    IL_0115:  blt        IL_0023

    IL_011a:  ldc.i4.0
    IL_011b:  stloc.3
    IL_011c:  br         IL_01a1

    IL_0121:  ldc.i4.0
    IL_0122:  stloc.s    j
    IL_0124:  br.s       IL_0198

    IL_0126:  ldc.i4.0
    IL_0127:  stloc.s    k
    IL_0129:  br.s       IL_018d

    IL_012b:  ldc.i4.0
    IL_012c:  stloc.s    l
    IL_012e:  br.s       IL_0182

    IL_0130:  ldc.i4.0
    IL_0131:  stloc.s    m
    IL_0133:  br.s       IL_0177

    IL_0135:  ldc.i4.0
    IL_0136:  stloc.s    n
    IL_0138:  br.s       IL_016c

    IL_013a:  ldc.i4.0
    IL_013b:  stloc.s    o
    IL_013d:  br.s       IL_0161

    IL_013f:  ldloc.1
    IL_0140:  ldloc.2
    IL_0141:  ldloc.3
    IL_0142:  ldloc.s    j
    IL_0144:  ldloc.s    k
    IL_0146:  ldloc.s    l
    IL_0148:  ldloc.s    m
    IL_014a:  ldloc.s    n
    IL_014c:  ldloc.s    o
    IL_014e:  call       instance value class Yak& valuetype Yak[,,,,,,]::Address(int32,int32,int32,int32,int32,int32,int32)
    IL_0153:  ldfld      int32 Yak::b
    IL_0158:  conv.i8
    IL_0159:  add
    IL_015a:  stloc.1
    IL_015b:  ldloc.s    o
    IL_015d:  ldc.i4.1
    IL_015e:  add
    IL_015f:  stloc.s    o
    IL_0161:  ldloc.s    o
    IL_0163:  ldloc.0
    IL_0164:  blt.s      IL_013f

    IL_0166:  ldloc.s    n
    IL_0168:  ldc.i4.1
    IL_0169:  add
    IL_016a:  stloc.s    n
    IL_016c:  ldloc.s    n
    IL_016e:  ldloc.0
    IL_016f:  blt.s      IL_013a

    IL_0171:  ldloc.s    m
    IL_0173:  ldc.i4.1
    IL_0174:  add
    IL_0175:  stloc.s    m
    IL_0177:  ldloc.s    m
    IL_0179:  ldloc.0
    IL_017a:  blt.s      IL_0135

    IL_017c:  ldloc.s    l
    IL_017e:  ldc.i4.1
    IL_017f:  add
    IL_0180:  stloc.s    l
    IL_0182:  ldloc.s    l
    IL_0184:  ldloc.0
    IL_0185:  blt.s      IL_0130

    IL_0187:  ldloc.s    k
    IL_0189:  ldc.i4.1
    IL_018a:  add
    IL_018b:  stloc.s    k
    IL_018d:  ldloc.s    k
    IL_018f:  ldloc.0
    IL_0190:  blt.s      IL_012b

    IL_0192:  ldloc.s    j
    IL_0194:  ldc.i4.1
    IL_0195:  add
    IL_0196:  stloc.s    j
    IL_0198:  ldloc.s    j
    IL_019a:  ldloc.0
    IL_019b:  blt.s      IL_0126

    IL_019d:  ldloc.3
    IL_019e:  ldc.i4.1
    IL_019f:  add
    IL_01a0:  stloc.3
    IL_01a1:  ldloc.3
    IL_01a2:  ldloc.0
    IL_01a3:  blt        IL_0121

    IL_01a8:  ldstr      "\nTry to get count!"
    IL_01ad:  call       void [System.Console]System.Console::WriteLine(string)
    IL_01b2:  ldloc.2
    IL_01b3:  call       void Complex2_Array_Test::test(value class Yak[0...,0...,0...,0...,0...,0...,0...])
    IL_01b8:  ldloc.2
  call   instance int32 [mscorlib]System.Array::get_Length()
    IL_01be:  ldloc.0
    IL_01bf:  ldloc.0
    IL_01c0:  mul
    IL_01c1:  ldloc.0
    IL_01c2:  mul
    IL_01c3:  ldloc.0
    IL_01c4:  mul
    IL_01c5:  ldloc.0
    IL_01c6:  mul
    IL_01c7:  ldloc.0
    IL_01c8:  mul
    IL_01c9:  ldloc.0
    IL_01ca:  mul
    IL_01cb:  bne.un.s   IL_0200

    IL_01cd:  ldloc.1
    IL_01ce:  ldc.i4     0x1c1
    IL_01d3:  conv.i8
    IL_01d4:  bne.un.s   IL_0200

    IL_01d6:  ldstr      "Count is:"
    IL_01db:  ldloc.2
  call   instance int32 [mscorlib]System.Array::get_Length()
    IL_01e1:  stloc.s    V_10
    IL_01e3:  ldloca.s   V_10
    IL_01e5:  call       instance string [mscorlib]System.Int32::ToString()
    IL_01ea:  call       class System.String [mscorlib]System.String::Concat(class System.String,class System.String)
    IL_01ef:  call       void [System.Console]System.Console::Write(string)
    IL_01f4:  ldstr      "\nEverything Worked!"
    IL_01f9:  call       void [System.Console]System.Console::WriteLine(string)
    IL_01fe:  ldc.i4	0x64
    IL_01ff:  ret

    IL_0200:  ldstr      "Count is:"
    IL_0205:  ldloc.2
  call   instance int32 [mscorlib]System.Array::get_Length()
    IL_020b:  stloc.s    V_10
    IL_020d:  ldloca.s   V_10
    IL_020f:  call       instance string [mscorlib]System.Int32::ToString()
    IL_0214:  call       class System.String [mscorlib]System.String::Concat(class System.String,class System.String)
    IL_0219:  call       void [System.Console]System.Console::WriteLine(string)
    IL_021e:  ldstr      "Sum is:"
    IL_0223:  ldloca.s   sum
    IL_0225:  call       instance string [mscorlib]System.Int64::ToString()
    IL_022a:  call       class System.String [mscorlib]System.String::Concat(class System.String,class System.String)
    IL_022f:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0234:  ldstr      "\nEverything Didnt Work!"
    IL_0239:  call       void [System.Console]System.Console::WriteLine(string)
    IL_023e:  ldc.i4.1
    IL_023f:  ret
  } // end of method 'Complex2_Array_Test::Main'

  .method public specialname rtspecialname instance void .ctor() il managed
  {
    .maxstack  8

    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method 'Complex2_Array_Test::.ctor'

} // end of class 'Complex2_Array_Test'

