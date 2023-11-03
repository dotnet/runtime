// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//.corflags 0x00000002
.assembly extern mscorlib { }
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ASSEMBLY_NAME
{ }
.namespace Test_ptr
{
// MVID: {6048B3F7-DE21-4008-935A-6B43C5338841}
.class value private explicit ansi sealed $MultiByte$size$120
{
  .pack 1
  .size 120
} // end of class 'Test_ptr.$MultiByte$size$120'

.class value private explicit ansi sealed $MultiByte$size$7
{
  .pack 1
  .size 7
} // end of class 'Test_ptr.$MultiByte$size$7'

.class value private explicit ansi sealed $MultiByte$size$32000
{
  .pack 1
  .size 32000
} // end of class 'Test_ptr.$MultiByte$size$32000'

.class public AA {
.method public static void qsort(native unsigned int A_0,
              unsigned int32 A_1,
              unsigned int32 A_2,
              method int32 *(native unsigned int, native unsigned int) A_3) il managed
{
  .maxstack  4
  .locals (int8 * V_0,
           int32 V_1,
           int8 * V_2,
           int8 * V_3,
           int8 * V_4,
           native uint V_5,
           int8 * V_6,
           value class Test_ptr.$MultiByte$size$120 V_7,
           value class Test_ptr.$MultiByte$size$120 V_8)
  IL_0000:  ldarg.1
  IL_0001:  ldc.i4.2
  IL_0002:  blt.un.s   IL_0007

  IL_0004:  ldarg.2
  IL_0005:  brtrue.s   IL_000c

  IL_0007:  br         IL_010d

  IL_000c:  ldc.i4.0
  IL_000d:  stloc.1
  IL_000e:  ldarg.0
  IL_000f:  stloc.0
  IL_0010:  ldarg.0
  IL_0011:  ldarg.2
  IL_0012:  ldarg.1
  IL_0013:  ldc.i4.1
  IL_0014:  sub
  IL_0015:  mul
  IL_0016:  add
  IL_0017:  stloc.s    V_4
  IL_0019:  ldloc.s    V_4
  IL_001b:  ldloc.0
  IL_001c:  sub
  IL_001d:  ldarg.2
  IL_001e:  div.un
  IL_001f:  ldc.i4.1
  IL_0020:  add
  IL_0021:  stloc.s    V_5
  IL_0023:  ldloc.s    V_5
  IL_0025:  ldc.i4.8
  IL_0026:  bgt.un.s   IL_0037

  IL_0028:  ldloc.0
  IL_0029:  ldloc.s    V_4
  IL_002b:  ldarg.2
  IL_002c:  ldarg.3
  IL_002d:  call       void Test_ptr.AA::shortsort(native unsigned int, native unsigned int, unsigned int32, method int32 *(native unsigned int,native unsigned int))
  IL_0032:  br         IL_00ef

  IL_0037:  ldloc.0
  IL_0038:  ldloc.s    V_5
  IL_003a:  ldc.i4 2
  	    conv.u
  IL_003b:  div.un
  IL_003c:  ldarg.2
  IL_003d:  mul
  IL_003e:  add
  IL_003f:  stloc.s    V_6
  IL_0041:  ldloc.s    V_6
  IL_0043:  ldloc.0
  IL_0044:  ldarg.2
  IL_0045:  call       void  Test_ptr.AA::swap(native unsigned int, native unsigned int, unsigned int32)
  IL_004a:  ldloc.0
  IL_004b:  stloc.3
  IL_004c:  ldloc.s    V_4
  IL_004e:  ldarg.2
  IL_004f:  add
  IL_0050:  stloc.2
  IL_0051:  ldloc.3
  IL_0052:  ldarg.2
  IL_0053:  add
  IL_0054:  stloc.3
  IL_0055:  ldloc.3
  IL_0056:  ldloc.s    V_4
  IL_0058:  bgt.un.s   IL_0065

  IL_005a:  ldloc.3
  IL_005b:  ldloc.0
  IL_005c:  ldarg.3
  IL_005d:  calli      int32 (native unsigned int,native unsigned int)
  IL_0062:  ldc.i4.0
  IL_0063:  ble.s      IL_0051

  IL_0065:  ldloc.2
  IL_0066:  ldarg.2
  IL_0067:  sub
  IL_0068:  stloc.2
  IL_0069:  ldloc.2
  IL_006a:  ldloc.0
  IL_006b:  ble.un.s   IL_0078

  IL_006d:  ldloc.2
  IL_006e:  ldloc.0
  IL_006f:  ldarg.3
  IL_0070:  calli      int32 (native unsigned int,native unsigned int)
  IL_0075:  ldc.i4.0
  IL_0076:  bge.s      IL_0065

  IL_0078:  ldloc.2
  IL_0079:  ldloc.3
  IL_007a:  bge.un.s   IL_007e

  IL_007c:  br.s       IL_0088

  IL_007e:  ldloc.3
  IL_007f:  ldloc.2
  IL_0080:  ldarg.2
  IL_0081:  call       void Test_ptr.AA::swap(native unsigned int, native unsigned int, unsigned int32)
  IL_0086:  br.s       IL_0051

  IL_0088:  ldloc.0
  IL_0089:  ldloc.2
  IL_008a:  ldarg.2
  IL_008b:  call       void Test_ptr.AA::swap(native unsigned int, native unsigned int, unsigned int32)
  IL_0090:  ldloc.2
  IL_0091:  ldc.i4.1
  IL_0092:  sub
  IL_0093:  ldloc.0
  IL_0094:  sub
  IL_0095:  ldloc.s    V_4
  IL_0097:  ldloc.3
  IL_0098:  sub
  IL_0099:  blt.s      IL_00c5

  IL_009b:  ldloc.0
  IL_009c:  ldarg.2
  IL_009d:  add
  IL_009e:  ldloc.2
  IL_009f:  bge.un.s   IL_00b7

  IL_00a1:  ldloca.s   V_8
  IL_00a3:  ldloc.1
  IL_00a4:  ldc.i4.8
  IL_00a5:  mul
  IL_00a6:  add
  IL_00a7:  ldloc.0
  IL_00a8:  stind.i
  IL_00a9:  ldloca.s   V_7
  IL_00ab:  ldloc.1
  IL_00ac:  ldc.i4.8
  IL_00ad:  mul
  IL_00ae:  add
  IL_00af:  ldloc.2
  IL_00b0:  ldarg.2
  IL_00b1:  sub
  IL_00b2:  stind.i
  IL_00b3:  ldloc.1
  IL_00b4:  ldc.i4.1
  IL_00b5:  add
  IL_00b6:  stloc.1
  IL_00b7:  ldloc.3
  IL_00b8:  ldloc.s    V_4
  IL_00ba:  bge.un.s   IL_00c3

  IL_00bc:  ldloc.3
  IL_00bd:  stloc.0
  IL_00be:  br         IL_0019

  IL_00c3:  br.s       IL_00ef

  IL_00c5:  ldloc.3
  IL_00c6:  ldloc.s    V_4
  IL_00c8:  bge.un.s   IL_00df

  IL_00ca:  ldloca.s   V_8
  IL_00cc:  ldloc.1
  IL_00cd:  ldc.i4.8
  IL_00ce:  mul
  IL_00cf:  add
  IL_00d0:  ldloc.3
  IL_00d1:  stind.i
  IL_00d2:  ldloca.s   V_7
  IL_00d4:  ldloc.1
  IL_00d5:  ldc.i4.8
  IL_00d6:  mul
  IL_00d7:  add
  IL_00d8:  ldloc.s    V_4
  IL_00da:  stind.i
  IL_00db:  ldloc.1
  IL_00dc:  ldc.i4.1
  IL_00dd:  add
  IL_00de:  stloc.1
  IL_00df:  ldloc.0
  IL_00e0:  ldarg.2
  IL_00e1:  add
  IL_00e2:  ldloc.2
  IL_00e3:  bge.un.s   IL_00ef

  IL_00e5:  ldloc.2
  IL_00e6:  ldarg.2
  IL_00e7:  sub
  IL_00e8:  stloc.s    V_4
  IL_00ea:  br         IL_0019

  IL_00ef:  ldloc.1
  IL_00f0:  ldc.i4.1
  IL_00f1:  sub
  IL_00f2:  stloc.1
  IL_00f3:  ldloc.1
  IL_00f4:  ldc.i4.0
  IL_00f5:  blt.s      IL_010d

  IL_00f7:  ldloca.s   V_8
  IL_00f9:  ldloc.1
  IL_00fa:  ldc.i4.8
  IL_00fb:  mul
  IL_00fc:  add
  IL_00fd:  ldind.i
  IL_00fe:  stloc.0
  IL_00ff:  ldloca.s   V_7
  IL_0101:  ldloc.1
  IL_0102:  ldc.i4.8
  IL_0103:  mul
  IL_0104:  add
  IL_0105:  ldind.i
  IL_0106:  stloc.s    V_4
  IL_0108:  br         IL_0019

  IL_010d:  ret
} // end of global method 'qsort'

.method public static void  
        shortsort(native unsigned int A_0, native unsigned int A_1,
        		unsigned int32 A_2, method int32 *(native unsigned int,
                              native unsigned int) A_3) il managed
{
  .vtentry 2 : 1
  // Code size       51 (0x33)
  .maxstack  3
  .locals (int8 * V_0,
           int8 * V_1)
  IL_0000:  ldarg.1
  IL_0001:  ldarg.0
  IL_0002:  ble.un.s   IL_0032

  IL_0004:  ldarg.0
  IL_0005:  stloc.1
  IL_0006:  ldarg.0
  IL_0007:  ldarg.2
  IL_0008:  add
  IL_0009:  stloc.0
  IL_000a:  br.s       IL_0010

  IL_000c:  ldloc.0
  IL_000d:  ldarg.2
  IL_000e:  add
  IL_000f:  stloc.0
  IL_0010:  ldloc.0
  IL_0011:  ldarg.1
  IL_0012:  bgt.un.s   IL_0023

  IL_0014:  ldloc.0
  IL_0015:  ldloc.1
  IL_0016:  ldarg.3
  IL_0017:  calli      int32 (native unsigned int,native unsigned int)
  IL_001c:  ldc.i4.0
  IL_001d:  ble.s      IL_0021

  IL_001f:  ldloc.0
  IL_0020:  stloc.1
  IL_0021:  br.s       IL_000c

  IL_0023:  ldloc.1
  IL_0024:  ldarg.1
  IL_0025:  ldarg.2
  IL_0026:  call       void  Test_ptr.AA::swap(native unsigned int, native unsigned int, unsigned int32)
  IL_002b:  ldarg.1
  IL_002c:  ldarg.2
  IL_002d:  sub
  IL_002e:  starg.s    A_1
  IL_0030:  br.s       IL_0000

  IL_0032:  ret
} // end of global method 'shortsort'

.method public static void swap(native unsigned int A_0, native unsigned int A_1, unsigned int32 A_2) il managed
{
  .vtentry 3 : 1
  // Code size       35 (0x23)
  .maxstack  3
  .locals (int8 V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  beq.s      IL_0022

  IL_0004:  ldarg.2
  IL_0005:  ldarg.2
  IL_0006:  ldc.i4.1
  IL_0007:  sub
  IL_0008:  starg.s    A_2
  IL_000a:  brfalse.s  IL_0022

  IL_000c:  ldarg.0
  IL_000d:  ldind.i1
  IL_000e:  stloc.0
  IL_000f:  ldarg.0
  IL_0010:  ldarg.1
  IL_0011:  ldind.i1
  IL_0012:  stind.i1
  IL_0013:  ldarg.0
  IL_0014:  ldc.i4.1
  IL_0015:  add
  IL_0016:  starg.s    A_0
  IL_0018:  ldarg.1
  IL_0019:  ldloc.0
  IL_001a:  stind.i1
  IL_001b:  ldarg.1
  IL_001c:  ldc.i4.1
  IL_001d:  add
  IL_001e:  starg.s    A_1
  IL_0020:  br.s       IL_0004

  IL_0022:  ret
} // end of global method 'swap'

.method public static int32 mycomp(native unsigned int A_0, native unsigned int A_1) il managed
{
  .vtentry 4 : 1
  // Code size       27 (0x1b)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldind.i4
  IL_0002:  ldarg.1
  IL_0003:  ldind.i4
  IL_0004:  ble.un.s   IL_000b

  IL_0006:  ldc.i4.1
  IL_0007:  br.s       IL_001a

  IL_0009:  br.s       IL_0019

  IL_000b:  ldarg.0
  IL_000c:  ldind.i4
  IL_000d:  ldarg.1
  IL_000e:  ldind.i4
  IL_000f:  bge.un.s   IL_0016

  IL_0011:  ldc.i4.m1
  IL_0012:  br.s       IL_001a

  IL_0014:  br.s       IL_0019

  IL_0016:  ldc.i4.0
  IL_0017:  br.s       IL_001a

  IL_0019:  ldc.i4.0
  IL_001a:  ret
} // end of global method 'mycomp'

.method public static int32 main() il managed
{
  .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
      01 00 00 00
  )
  .entrypoint
  .maxstack  4
  .locals (int32 V_0,
           unsigned int32 V_1,
           value class Test_ptr.$MultiByte$size$32000 V_2,
           int32 V_3)
  IL_0000:  ldc.i4     0x1f40
  IL_0005:  stloc.3
  IL_0006:  ldc.i4     0xf404c0
  IL_000b:  stloc.1
  IL_000c:  ldc.i4.0
  IL_000d:  stloc.0
  IL_000e:  br.s       IL_0014

  IL_0010:  ldloc.0
  IL_0011:  ldc.i4.1
  IL_0012:  add
  IL_0013:  stloc.0
  IL_0014:  ldloc.0
  IL_0015:  ldc.i4     0x1f40
  IL_001a:  bge.s      IL_0034

  IL_001c:  ldloc.1
  IL_001d:  ldc.i4.s   19
  IL_001f:  add
  IL_0020:  ldc.i4.s   17
  IL_0022:  mul
  IL_0023:  ldc.i4     0x7cf
  IL_0028:  rem.un
  IL_0029:  stloc.1
  IL_002a:  ldloca.s   V_2
  IL_002c:  ldloc.0
  IL_002d:  ldc.i4.4
  IL_002e:  mul
  IL_002f:  add
  IL_0030:  ldloc.1
  IL_0031:  stind.i4
  IL_0032:  br.s       IL_0010

  IL_0034:  ldloca.s   V_2
  IL_0036:  ldc.i4     0x1f40
  IL_003b:  ldc.i4.4
  			ldftn  int32 Test_ptr.AA::mycomp(native unsigned int A_0, native unsigned int A_1)
  IL_0041:  call       void Test_ptr.AA::qsort(native unsigned int,unsigned int32,unsigned int32,
  							method int32 *(native unsigned int, native unsigned int))
  IL_0046:  ldc.i4.1
  IL_0047:  stloc.0
  IL_0048:  br.s       IL_004e

  IL_004a:  ldloc.0
  IL_004b:  ldc.i4.1
  IL_004c:  add
  IL_004d:  stloc.0
  IL_004e:  ldloc.0
  IL_004f:  ldc.i4     0x1f40
  IL_0054:  bge.s      IL_007e

  IL_0056:  ldloca.s   V_2
  IL_0058:  ldloc.0
  IL_0059:  ldc.i4.4
  IL_005a:  mul
  IL_005b:  add
  IL_005c:  ldind.i4
  IL_005d:  ldloca.s   V_2
  IL_005f:  ldloc.0
  IL_0060:  ldc.i4.1
  IL_0061:  sub
  IL_0062:  ldc.i4.4
  IL_0063:  mul
  IL_0064:  add
  IL_0065:  ldind.i4
  IL_0066:  bge.un.s   IL_007c

  IL_0068: 
			ldstr "failed"
  IL_0072:  call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
  IL_0077:  ldc.i4.1
  IL_0078:  br.s       IL_0091

  IL_007a:  br.s       IL_0090

  IL_007c:  br.s       IL_004a

  IL_007e:  
 	  		ldstr "passed"

  IL_0088:  call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
  IL_008d:  ldc.i4    0x64
  IL_008e:  br.s       IL_0091

  IL_0090:  ldc.i4.0
  IL_0091:  ret
} // end of global method 'main'
} //class Test_ptr.AA
}
