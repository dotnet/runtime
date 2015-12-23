// Test localloc with tail call followed by jump

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern mscorlib{}
.assembly extern common{}
.assembly call06{}

.class private auto ansi beforefieldinit LocallocTest
       extends [mscorlib]System.Object
{
  .field private static int32 locallocSize
  .method public hidebysig static int32  Main() cil managed
  {
    .entrypoint
    .maxstack  11
    .locals (int32* intArray1,
             int32* intArray2,
             class LocallocTest V_2)
    IL_0000:  ldc.i4.4
    IL_0001:  ldc.i4     0x1000
    IL_0002:  mul
    IL_0003:  localloc
    IL_0005:  stloc.0
    IL_0006:  ldc.i4.4
    IL_0007:  ldc.i4     0x1000
    IL_0008:  mul
    IL_0009:  localloc
    IL_000b:  stloc.1
    IL_000c:  ldc.i4     0x1000
    IL_000d:  stsfld     int32 LocallocTest::locallocSize
    IL_0012:  ldloc.0
    IL_0013:  ldsfld     int32 LocallocTest::locallocSize
    IL_0018:  ldc.i4     1000
    IL_001d:  call       void [common]LocallocTesting.Global::initializeStack(int32*,
                                                                              int32,
                                                                              int32)
    IL_0022:  ldloc.1
    IL_0023:  ldsfld     int32 LocallocTest::locallocSize
    IL_0028:  ldc.i4     2000
    IL_002d:  call       void [common]LocallocTesting.Global::initializeStack(int32*,
                                                                              int32,
                                                                              int32)
    IL_0032:  newobj     instance void LocallocTest::.ctor()
    IL_0037:  stloc.2
    IL_0038:  ldloc.2
    IL_0039:  ldc.i4.1
    IL_003a:  ldc.i4.2
    IL_003b:  ldc.i4.3
    IL_003c:  ldc.i4.4
    IL_003d:  ldc.i4.5
    IL_003e:  ldc.i4.6
    IL_003f:  ldc.i4.7
    IL_0040:  ldc.i4.8
    IL_0041:  ldloc.0
    IL_0042:  ldloc.1
    IL_0043:  tail. callvirt   instance int32 LocallocTest::func0(int32,
                                                            int32,
                                                            int32,
                                                            int32,
                                                            int32,
                                                            int32,
                                                            int32,
                                                            int32,
                                                            int32*,
                                                            int32*)
    IL_004c:  ret
  } 

  .method private hidebysig instance int32 
          func0(int32 i1,
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
    .maxstack  11
    .locals (int32 retValue)
    IL_0000:  ldstr      "ar1"
    IL_0005:  ldarg.s    ar1
    IL_0007:  ldsfld     int32 LocallocTest::locallocSize
    IL_000c:  ldc.i4     1000
    IL_0011:  call       bool [common]LocallocTesting.Global::verifyStack(string,
                                                                          int32*,
                                                                          int32,
                                                                          int32)
    IL_0016:  brtrue.s   IL_001c

    IL_0018:  ldc.i4.1
    IL_0019:  stloc.0
    IL_001a:  br.s       L_END

    IL_001c:  ldstr      "ar2"
    IL_0021:  ldarg.s    ar2
    IL_0023:  ldsfld     int32 LocallocTest::locallocSize
    IL_0028:  ldc.i4     2000
    IL_002d:  call       bool [common]LocallocTesting.Global::verifyStack(string,
                                                                          int32*,
                                                                          int32,
                                                                          int32)
    IL_0032:  brtrue.s   L_JMP

    IL_0034:  ldc.i4.1
    IL_0035:  stloc.0
    IL_0036:  br.s       L_END

    L_JMP:    jmp       instance int32 LocallocTest::func1(int32,
                                                            int32,
                                                            int32,
                                                            int32,
                                                            int32,
                                                            int32,
                                                            int32,
                                                            int32,
                                                            int32*,
                                                            int32*)
    IL_004a:  stloc.0

    L_END:  ldc.i4.1
    IL_004e:  ret
  } 

  .method private hidebysig instance int32 
          func1(int32 i1,
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
    .maxstack  4
    .locals (unsigned int64 local1,
             unsigned int64 local2,
             int32* intArray1,
             int32* intArray2,
             int32 retValue)
    IL_0000:  ldc.i8     0xfedcba9876543210
    IL_0009:  stloc.0
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  conv.i8
    IL_000d:  add
    IL_000e:  stloc.1
    IL_000f:  ldc.i4.4
    IL_0010:  ldc.i4     0x1000
    IL_0011:  mul
    IL_0012:  localloc
    IL_0014:  stloc.2
    IL_0015:  ldc.i4.4
    IL_0016:  ldc.i4     0x1000
    IL_0017:  mul
    IL_0018:  localloc
    IL_001a:  stloc.3
    IL_001b:  ldloc.2
    IL_001c:  ldsfld     int32 LocallocTest::locallocSize
    IL_0021:  ldc.i4     3000
    IL_0026:  call       void [common]LocallocTesting.Global::initializeStack(int32*,
                                                                              int32,
                                                                              int32)
    IL_002b:  ldloc.3
    IL_002c:  ldsfld     int32 LocallocTest::locallocSize
    IL_0031:  ldc.i4     4000
    IL_0036:  call       void [common]LocallocTesting.Global::initializeStack(int32*,
                                                                              int32,
                                                                              int32)
    IL_003b:  ldstr      "local1"
    IL_0040:  ldloc.0
    IL_0041:  ldc.i8     0xfedcba9876543210
    IL_004a:  call       bool [common]LocallocTesting.Global::verifyLocal(string,
                                                                          unsigned int64,
                                                                          unsigned int64)
    IL_004f:  brtrue.s   IL_0059

    IL_0051:  ldc.i4.1
    IL_0052:  stloc.s    retValue
    IL_0054:  br         L_END

    IL_0059:  ldstr      "local2"
    IL_005e:  ldloc.1
    IL_005f:  ldc.i8     0xfedcba9876543211
    IL_0068:  call       bool [common]LocallocTesting.Global::verifyLocal(string,
                                                                          unsigned int64,
                                                                          unsigned int64)
    IL_006d:  brtrue.s   IL_0077
    IL_006f:  ldc.i4.1
    IL_0070:  stloc.s    retValue
    IL_0072:  br         L_END

    IL_0077:  ldstr      "ar1"
    IL_007c:  ldarg.s    ar1
    IL_007e:  ldsfld     int32 LocallocTest::locallocSize
    IL_0083:  ldc.i4     1000
    IL_0088:  call       bool [common]LocallocTesting.Global::verifyStack(string,
                                                                          int32*,
                                                                          int32,
                                                                          int32)
    IL_008d:  brtrue.s   IL_0094

    IL_008f:  ldc.i4.1
    IL_0090:  stloc.s    retValue
    IL_0092:  br.s       L_END

    IL_0094:  ldstr      "ar2"
    IL_0099:  ldarg.s    ar2
    IL_009b:  ldsfld     int32 LocallocTest::locallocSize
    IL_00a0:  ldc.i4     2000
    IL_00a5:  call       bool [common]LocallocTesting.Global::verifyStack(string,
                                                                          int32*,
                                                                          int32,
                                                                          int32)
    IL_00aa:  brtrue.s   IL_00b1

    IL_00ac:  ldc.i4.1
    IL_00ad:  stloc.s    retValue
    IL_00af:  br.s       L_END

    IL_00b1:  ldstr      "intArray1"
    IL_00b6:  ldloc.2
    IL_00b7:  ldsfld     int32 LocallocTest::locallocSize
    IL_00bc:  ldc.i4     3000
    IL_00c1:  call       bool [common]LocallocTesting.Global::verifyStack(string,
                                                                          int32*,
                                                                          int32,
                                                                          int32)
    IL_00c6:  brtrue.s   IL_00cd

    IL_00c8:  ldc.i4.1
    IL_00c9:  stloc.s    retValue
    IL_00cb:  br.s       L_END

    IL_00cd:  ldstr      "intArray2"
    IL_00d2:  ldloc.3
    IL_00d3:  ldsfld     int32 LocallocTest::locallocSize
    IL_00d8:  ldc.i4     4000
    IL_00dd:  call       bool [common]LocallocTesting.Global::verifyStack(string,
                                                                          int32*,
                                                                          int32,
                                                                          int32)
    IL_00e2:  brtrue.s   L_Passed

    IL_00e4:  ldc.i4.1
    IL_00e5:  stloc.s    retValue
    IL_00e7:  br.s       L_END

    L_Passed:  ldstr      "Passed\n"
    IL_00ee:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00f3:  ldc.i4.s   100
    IL_00f5:  stloc.s    retValue

    L_END:  ldloc.s    retValue
    IL_00fb:  ret
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



