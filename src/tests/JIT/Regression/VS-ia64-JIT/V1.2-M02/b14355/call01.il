// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.





.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern mscorlib
{
}
.assembly extern common
{
  .ver 0:0:0:0
}
.assembly call01
{


  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.assembly extern xunit.core {}
.module call01.exe
.custom instance void [mscorlib]System.Security.UnverifiableCodeAttribute::.ctor() = ( 01 00 00 00 ) 
.imagebase 0x00400000
.subsystem 0x00000003
.file alignment 512
.corflags 0x00000001


.class private auto ansi beforefieldinit LocallocTest
       extends [mscorlib]System.Object
{
  .field private static int32 locallocSize
  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  10
    .locals (uint64 V_0,
             uint64 V_1,
             int32* V_2,
             int32* V_3,
             int32 V_4)
    IL_0000:  ldc.i8     0xfedcba9876543210
    IL_0009:  stloc.0
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  conv.i8
    IL_000d:  add
    IL_000e:  stloc.1
    IL_000f:  ldc.i4.4
    IL_0010:  ldsfld     int32 [common]LocallocTesting.Global::stackAllocSize
    IL_0015:  mul
    IL_0016:  localloc
    IL_0018:  stloc.2
    IL_0019:  ldc.i4.4
    IL_001a:  ldsfld     int32 [common]LocallocTesting.Global::stackAllocSize
    IL_001f:  mul
    IL_0020:  localloc
    IL_0022:  stloc.3
    IL_0023:  ldsfld     int32 [common]LocallocTesting.Global::stackAllocSize
    IL_0028:  stsfld     int32 LocallocTest::locallocSize
    IL_002d:  ldloc.2
    IL_002e:  ldsfld     int32 LocallocTest::locallocSize
    IL_0033:  ldc.i4     0x3e8
    IL_0038:  call       void [common]LocallocTesting.Global::initializeStack(int32*,
                                                                              int32,
                                                                              int32)
    IL_003d:  ldloc.3
    IL_003e:  ldsfld     int32 LocallocTest::locallocSize
    IL_0043:  ldc.i4     0x7d0
    IL_0048:  call       void [common]LocallocTesting.Global::initializeStack(int32*,
                                                                              int32,
                                                                              int32)
    IL_004d:  ldc.i4.1
    IL_004e:  ldc.i4.s   20
    IL_0050:  ldc.i4.3
    IL_0051:  ldc.i4.4
    IL_0052:  ldc.i4.5
    IL_0053:  ldc.i4.6
    IL_0054:  ldc.i4.7
    IL_0055:  ldc.i4.8
    IL_0056:  ldloc.2
    IL_0057:  ldloc.3
    IL_0058:  call       bool LocallocTest::func1(int32,
                                                  int32,
                                                  int32,
                                                  int32,
                                                  int32,
                                                  int32,
                                                  int32,
                                                  int32,
                                                  int32*,
                                                  int32*)
    IL_005d:  brtrue.s   IL_0064

    IL_005f:  ldc.i4.1
    IL_0060:  stloc.s    V_4
    IL_0062:  br.s       IL_00e2

    IL_0064:  ldstr      "intArray1"
    IL_0069:  ldloc.2
    IL_006a:  ldsfld     int32 LocallocTest::locallocSize
    IL_006f:  ldc.i4     0x3e8
    IL_0074:  call       bool [common]LocallocTesting.Global::verifyStack(string,
                                                                          int32*,
                                                                          int32,
                                                                          int32)
    IL_0079:  brtrue.s   IL_0080

    IL_007b:  ldc.i4.1
    IL_007c:  stloc.s    V_4
    IL_007e:  br.s       IL_00e2

    IL_0080:  ldstr      "intArray2"
    IL_0085:  ldloc.3
    IL_0086:  ldsfld     int32 LocallocTest::locallocSize
    IL_008b:  ldc.i4     0x7d0
    IL_0090:  call       bool [common]LocallocTesting.Global::verifyStack(string,
                                                                          int32*,
                                                                          int32,
                                                                          int32)
    IL_0095:  brtrue.s   IL_009c

    IL_0097:  ldc.i4.1
    IL_0098:  stloc.s    V_4
    IL_009a:  br.s       IL_00e2

    IL_009c:  ldstr      "local1"
    IL_00a1:  ldloc.0
    IL_00a2:  ldc.i8     0xfedcba9876543210
    IL_00ab:  call       bool [common]LocallocTesting.Global::verifyLocal(string,
                                                                          uint64,
                                                                          uint64)
    IL_00b0:  brtrue.s   IL_00b7

    IL_00b2:  ldc.i4.1
    IL_00b3:  stloc.s    V_4
    IL_00b5:  br.s       IL_00e2

    IL_00b7:  ldstr      "local2"
    IL_00bc:  ldloc.1
    IL_00bd:  ldc.i8     0xfedcba9876543211
    IL_00c6:  call       bool [common]LocallocTesting.Global::verifyLocal(string,
                                                                          uint64,
                                                                          uint64)
    IL_00cb:  brtrue.s   IL_00d2

    IL_00cd:  ldc.i4.1
    IL_00ce:  stloc.s    V_4
    IL_00d0:  br.s       IL_00e2

    IL_00d2:  ldstr      "Passed\n"
    IL_00d7:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00dc:  ldc.i4.s   100
    IL_00de:  stloc.s    V_4
    IL_00e0:  br.s       IL_00e2

    IL_00e2:  ldloc.s    V_4
    IL_00e4:  ret
  } 

  .method private hidebysig static bool  func1(int32 i1,
                                               int32 i2,
                                               int32 i3,
                                               int32 i4,
                                               int32 i5,
                                               int32 i6,
                                               int32 i7,
                                               int32 i8,
                                               int32* ar1,
                                               int32* ar2) cil managed
  {
    .maxstack  10
    .locals (int32* V_0,
             int32* V_1,
             bool V_2)
    IL_0000:  ldarg.1
    IL_0001:  ldc.i4.0
    IL_0002:  ble.s      IL_002a

    IL_0004:  ldc.i4.1
    IL_0005:  ldarg.1
    IL_0006:  ldc.i4.1
    IL_0007:  sub
    IL_0008:  ldc.i4.3
    IL_0009:  ldc.i4.4
    IL_000a:  ldc.i4.5
    IL_000b:  ldc.i4.6
    IL_000c:  ldc.i4.7
    IL_000d:  ldc.i4.8
    IL_000e:  ldarg.s    ar1
    IL_0010:  ldarg.s    ar2
    IL_0012:  call       bool LocallocTest::func1(int32,
                                                  int32,
                                                  int32,
                                                  int32,
                                                  int32,
                                                  int32,
                                                  int32,
                                                  int32,
                                                  int32*,
                                                  int32*)
    IL_0017:  brfalse.s  IL_0020

    IL_0019:  ldc.i4.1
    IL_001a:  stloc.2
    IL_001b:  br         IL_00d0

    IL_0020:  ldstr      "error\n"
    IL_0025:  call       void [System.Console]System.Console::WriteLine(string)
    IL_002a:  ldc.i4.4
    IL_002b:  ldsfld     int32 [common]LocallocTesting.Global::stackAllocSize
    IL_0030:  mul
    IL_0031:  localloc
    IL_0033:  stloc.0
    IL_0034:  ldc.i4.4
    IL_0035:  ldsfld     int32 [common]LocallocTesting.Global::stackAllocSize
    IL_003a:  mul
    IL_003b:  localloc
    IL_003d:  stloc.1
    IL_003e:  ldloc.0
    IL_003f:  ldsfld     int32 LocallocTest::locallocSize
    IL_0044:  ldc.i4     0xbb8
    IL_0049:  call       void [common]LocallocTesting.Global::initializeStack(int32*,
                                                                              int32,
                                                                              int32)
    IL_004e:  ldloc.1
    IL_004f:  ldsfld     int32 LocallocTest::locallocSize
    IL_0054:  ldc.i4     0xfa0
    IL_0059:  call       void [common]LocallocTesting.Global::initializeStack(int32*,
                                                                              int32,
                                                                              int32)
    IL_005e:  ldstr      "ar1"
    IL_0063:  ldarg.s    ar1
    IL_0065:  ldsfld     int32 LocallocTest::locallocSize
    IL_006a:  ldc.i4     0x3e8
    IL_006f:  call       bool [common]LocallocTesting.Global::verifyStack(string,
                                                                          int32*,
                                                                          int32,
                                                                          int32)
    IL_0074:  brtrue.s   IL_007a

    IL_0076:  ldc.i4.0
    IL_0077:  stloc.2
    IL_0078:  br.s       IL_00d0

    IL_007a:  ldstr      "ar2"
    IL_007f:  ldarg.s    ar2
    IL_0081:  ldsfld     int32 LocallocTest::locallocSize
    IL_0086:  ldc.i4     0x7d0
    IL_008b:  call       bool [common]LocallocTesting.Global::verifyStack(string,
                                                                          int32*,
                                                                          int32,
                                                                          int32)
    IL_0090:  brtrue.s   IL_0096

    IL_0092:  ldc.i4.0
    IL_0093:  stloc.2
    IL_0094:  br.s       IL_00d0

    IL_0096:  ldstr      "intArray1"
    IL_009b:  ldloc.0
    IL_009c:  ldsfld     int32 LocallocTest::locallocSize
    IL_00a1:  ldc.i4     0xbb8
    IL_00a6:  call       bool [common]LocallocTesting.Global::verifyStack(string,
                                                                          int32*,
                                                                          int32,
                                                                          int32)
    IL_00ab:  brtrue.s   IL_00b1

    IL_00ad:  ldc.i4.0
    IL_00ae:  stloc.2
    IL_00af:  br.s       IL_00d0

    IL_00b1:  ldstr      "intArray2"
    IL_00b6:  ldloc.1
    IL_00b7:  ldsfld     int32 LocallocTest::locallocSize
    IL_00bc:  ldc.i4     0xfa0
    IL_00c1:  call       bool [common]LocallocTesting.Global::verifyStack(string,
                                                                          int32*,
                                                                          int32,
                                                                          int32)
    IL_00c6:  brtrue.s   IL_00cc

    IL_00c8:  ldc.i4.0
    IL_00c9:  stloc.2
    IL_00ca:  br.s       IL_00d0

    IL_00cc:  ldc.i4.1
    IL_00cd:  stloc.2
    IL_00ce:  br.s       IL_00d0

    IL_00d0:  ldloc.2
    IL_00d1:  ret
  } 

  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  stsfld     int32 LocallocTest::locallocSize
    IL_0006:  ret
  } 

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } 

} 
