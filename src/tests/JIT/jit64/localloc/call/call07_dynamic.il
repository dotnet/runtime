// Test localloc with jump followed by tail call.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}
.assembly extern mscorlib{}
.assembly extern localloc_common{}
.assembly 'call07_dynamic'{}

.class public auto ansi beforefieldinit LocallocTest
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
    .locals (unsigned int64 local1,
             unsigned int64 local2,
             int32* ar1,
             class LocallocTest V_3,
             int32 retValue)
    IL_0000:  ldc.i8     0xfedcba9876543210
    IL_0009:  stloc.0
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  conv.i8
    IL_000d:  add
    IL_000e:  stloc.1
    IL_000f:  ldc.i4.4
    IL_0010:  ldsfld     int32 [localloc_common]LocallocTesting.Global::stackAllocSize
    IL_0015:  mul
    IL_0016:  localloc
    IL_0018:  stloc.2
    IL_0019:  ldsfld     int32 [localloc_common]LocallocTesting.Global::stackAllocSize
    IL_001e:  stsfld     int32 LocallocTest::locallocSize
    IL_0023:  ldloc.2
    IL_0024:  ldsfld     int32 LocallocTest::locallocSize
    IL_0029:  ldc.i4     1000
    IL_002e:  call       void [localloc_common]LocallocTesting.Global::initializeStack(int32*,
                                                                              int32,
                                                                              int32)
    IL_0033:  newobj     instance void LocallocTest::.ctor()
    IL_0038:  stloc.3
    IL_0039:  ldloc.3
    IL_003a:  ldc.i4.1
    IL_003b:  ldc.i4.2
    IL_003c:  ldc.i4.3
    IL_003d:  ldc.i4.4
    IL_003e:  ldc.i4.5
    IL_003f:  ldc.i4.6
    IL_0040:  ldc.i4.7
    IL_0041:  ldc.i4.8
    IL_0042:  ldloc.2
    IL_0043:  callvirt   instance bool LocallocTest::func0(int32,
                                                           int32,
                                                           int32,
                                                           int32,
                                                           int32,
                                                           int32,
                                                           int32,
                                                           int32,
                                                           int32*)
    IL_0048:  brtrue.s   IL_004f

    IL_004a:  ldc.i4.1
    IL_004b:  stloc.s    retValue
    IL_004d:  br.s       L_END

    IL_004f:  ldstr      "local1"
    IL_0054:  ldloc.0
    IL_0055:  ldc.i8     0xfedcba9876543210
    IL_005e:  call       bool [localloc_common]LocallocTesting.Global::verifyLocal(string,
                                                                          unsigned int64,
                                                                          unsigned int64)
    IL_0063:  brtrue.s   IL_006a

    IL_0065:  ldc.i4.1
    IL_0066:  stloc.s    retValue
    IL_0068:  br.s       L_END

    IL_006a:  ldstr      "local2"
    IL_006f:  ldloc.1
    IL_0070:  ldc.i8     0xfedcba9876543211
    IL_0079:  call       bool [localloc_common]LocallocTesting.Global::verifyLocal(string,
                                                                          unsigned int64,
                                                                          unsigned int64)
    IL_007e:  brtrue.s   IL_0085

    IL_0080:  ldc.i4.1
    IL_0081:  stloc.s    retValue
    IL_0083:  br.s       L_END

    IL_0085:  ldstr      "Passed\n"
    IL_008a:  call       void [System.Console]System.Console::WriteLine(string)
    IL_008f:  ldc.i4.s   100
    IL_0091:  stloc.s    retValue
    IL_0093:  br.s       L_END

    L_END:  ldloc.s    retValue
    IL_0097:  ret
  } 

  .method private hidebysig instance bool 
          func0(int32 i1,
                int32 i2,
                int32 i3,
                int32 i4,
                int32 i5,
                int32 i6,
                int32 i7,
                int32 i8,
                int32* ar1) cil managed
  {
    .maxstack  10
    .locals (bool retValue)
    IL_0000:  ldstr      "ar1"
    IL_0005:  ldarg.s    ar1
    IL_0007:  ldsfld     int32 LocallocTest::locallocSize
    IL_000c:  ldc.i4     1000
    IL_0011:  call       bool [localloc_common]LocallocTesting.Global::verifyStack(string,
                                                                          int32*,
                                                                          int32,
                                                                          int32)
    IL_0016:  brtrue.s   L_JUMP

    IL_0018:  ldc.i4.0
    IL_0019:  stloc.0
    IL_001a:  br.s       IL_002f
    L_JUMP:  jmp       instance bool LocallocTest::func1(int32,
                                                           int32,
                                                           int32,
                                                           int32,
                                                           int32,
                                                           int32,
                                                           int32,
                                                           int32,
                                                           int32*)
    IL_002c:  stloc.0
    IL_002d:  br.s       IL_002f

    IL_002f:  ldc.i4.1
    IL_0030:  ret
  } 

  .method private hidebysig instance bool 
          func1(int32 i1,
                int32 i2,
                int32 i3,
                int32 i4,
                int32 i5,
                int32 i6,
                int32 i7,
                int32 i8,
                int32* ar1) cil managed
  {
    .maxstack  3
    .locals (unsigned int64 local1,
             unsigned int64 local2,
             int32* ar2)
    IL_0000:  ldc.i8     0xfedcba9876543210
    IL_0009:  stloc.0
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  conv.i8
    IL_000d:  add
    IL_000e:  stloc.1
    IL_000f:  ldc.i4.4
    IL_0010:  ldsfld     int32 [localloc_common]LocallocTesting.Global::stackAllocSize
    IL_0015:  mul
    IL_0016:  localloc
    IL_0018:  stloc.2
    IL_0019:  ldloc.2
    IL_001a:  ldsfld     int32 LocallocTest::locallocSize
    IL_001f:  ldc.i4     2000
    IL_0024:  call       void [localloc_common]LocallocTesting.Global::initializeStack(int32*,
                                                                              int32,
                                                                              int32)
    IL_0029:  ldstr      "local1"
    IL_002e:  ldloc.0
    IL_002f:  ldc.i8     0xfedcba9876543210
    IL_0038:  call       bool [localloc_common]LocallocTesting.Global::verifyLocal(string,
                                                                          unsigned int64,
                                                                          unsigned int64)
    IL_003d:  brtrue.s   IL_0043

    IL_003f:  ldc.i4.0
    IL_0041:  br.s       L_END

    IL_0043:  ldstr      "local2"
    IL_0048:  ldloc.1
    IL_0049:  ldc.i8     0xfedcba9876543211
    IL_0052:  call       bool [localloc_common]LocallocTesting.Global::verifyLocal(string,
                                                                          unsigned int64,
                                                                          unsigned int64)
    IL_0057:  brtrue.s   IL_005d

    IL_0059:  ldc.i4.0
    IL_005b:  br.s       L_END
    IL_005d:  ldarg.0
    IL_005e:  ldarg.s    ar1
    IL_0060:  ldloc.2
    IL_0061:  call       instance bool LocallocTest::func2(int32*,
                                                           int32*)
    L_END:  ret
  } 

  .method private hidebysig instance bool 
          func2(int32* ar1,
                int32* ar2) cil managed
  {
    .maxstack  4
    .locals (unsigned int64 local1,
             unsigned int64 local2,
             int32* ar3,
             bool retValue)
    IL_0000:  ldc.i8     0xfedcba9876543210
    IL_0009:  stloc.0
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  conv.i8
    IL_000d:  add
    IL_000e:  stloc.1
    IL_000f:  ldc.i4.4
    IL_0010:  ldsfld     int32 [localloc_common]LocallocTesting.Global::stackAllocSize
    IL_0015:  mul
    IL_0016:  localloc
    IL_0018:  stloc.2
    IL_0019:  ldloc.2
    IL_001a:  ldsfld     int32 LocallocTest::locallocSize
    IL_001f:  ldc.i4     3000
    IL_0024:  call       void [localloc_common]LocallocTesting.Global::initializeStack(int32*,
                                                                              int32,
                                                                              int32)
    IL_0029:  ldstr      "local1"
    IL_002e:  ldloc.0
    IL_002f:  ldc.i8     0xfedcba9876543210
    IL_0038:  call       bool [localloc_common]LocallocTesting.Global::verifyLocal(string,
                                                                          unsigned int64,
                                                                          unsigned int64)
    IL_003d:  brtrue.s   IL_0043

    IL_003f:  ldc.i4.0
    IL_0040:  stloc.3
    IL_0041:  br         IL_00d3

    IL_0043:  ldstr      "local2"
    IL_0048:  ldloc.1
    IL_0049:  ldc.i8     0xfedcba9876543211
    IL_0052:  call       bool [localloc_common]LocallocTesting.Global::verifyLocal(string,
                                                                          unsigned int64,
                                                                          unsigned int64)
    IL_0057:  brtrue.s   IL_005d

    IL_0059:  ldc.i4.0
    IL_005a:  stloc.3
    IL_005b:  br         IL_00d3
    IL_005d:  ldstr      "ar1"
    IL_0062:  ldarg.1
    IL_0063:  ldsfld     int32 LocallocTest::locallocSize
    IL_0068:  ldc.i4     1000
    IL_006d:  call       bool [localloc_common]LocallocTesting.Global::verifyStack(string,
                                                                          int32*,
                                                                          int32,
                                                                          int32)
    IL_0072:  brtrue.s   IL_0078

    IL_0074:  ldc.i4.0
    IL_0075:  stloc.3
    IL_0076:  br         IL_00d3

    IL_0078:  ldstr      "ar2"
    IL_007d:  ldarg.2
    IL_007e:  ldsfld     int32 LocallocTest::locallocSize
    IL_0083:  ldc.i4     2000
    IL_0088:  call       bool [localloc_common]LocallocTesting.Global::verifyStack(string,
                                                                          int32*,
                                                                          int32,
                                                                          int32)
    IL_008d:  brtrue.s   IL_0093

    IL_008f:  ldc.i4.0
    IL_0090:  stloc.3
    IL_0091:  br         IL_00d3

    IL_0093:  ldstr      "ar3"
    IL_0098:  ldloc.2
    IL_0099:  ldsfld     int32 LocallocTest::locallocSize
    IL_009e:  ldc.i4     3000
    IL_00a3:  call       bool [localloc_common]LocallocTesting.Global::verifyStack(string,
                                                                          int32*,
                                                                          int32,
                                                                          int32)
    IL_00b4:  brtrue.s   IL_00bd
    
    IL_00b6:  ldc.i4.0
    IL_00b7:  stloc.3
    IL_00b8:  br         IL_00d3
    
    IL_00bd:  ldarg.0
    IL_00be:  ldc.i4     0x1f4
    IL_00c3:  ldarg      ar1
    IL_00c7:  ldarg      ar2
    IL_00cb:  call       instance bool LocallocTest::func3(int32,
                                                           int32*,
                                                           int32*)
    IL_00d0:  stloc.3
    IL_00d1:  br.s       IL_00d3

    IL_00d3:  ldloc.3
    IL_00d4:  ret
  } 

  .method private hidebysig instance bool 
          func3(int32 n,
                int32* ar1,
                int32* ar2) cil managed
  {
    .maxstack  4
    .locals init ([0] int32* ar3,
             [1] int32* ar4,
             [2] int32 iter)
     
    IL_0000:  ldarg.s    n
    IL_0002:  stloc.s    iter
    
    IL_0004:  ldloc.s    iter
    IL_0006:  ldc.i4.1
    IL_0007:  bgt.s      IL_000c

    IL_0009:  ldloc.s    iter
    IL_000b:  ret

    IL_000c:  ldc.i4.4
    IL_000d:  ldsfld     int32 [localloc_common]LocallocTesting.Global::stackAllocSize
    IL_0012:  mul
    IL_0013:  localloc
    IL_0015:  stloc      ar3
    IL_0019:  ldloc.s    ar3
    IL_001b:  ldsfld     int32 LocallocTest::locallocSize
    IL_0020:  ldc.i4     0xbb8
    IL_0025:  call       void [localloc_common]LocallocTesting.Global::initializeStack(int32*,
                                                                              int32,
                                                                              int32)
    IL_002a:  ldc.i4.4
    IL_002b:  ldsfld     int32 [localloc_common]LocallocTesting.Global::stackAllocSize
    IL_0030:  mul
    IL_0031:  localloc
    IL_0033:  stloc.s    ar4
    IL_0035:  ldloc.s    ar4
    IL_0037:  ldsfld     int32 LocallocTest::locallocSize
    IL_003c:  ldc.i4     0xfa0
    IL_0041:  call       void [localloc_common]LocallocTesting.Global::initializeStack(int32*,
                                                                              int32,
                                                                              int32)
    IL_0046:  ldstr      "ar1"
    IL_004b:  ldarg      ar1
    IL_004f:  ldsfld     int32 LocallocTest::locallocSize
    IL_0054:  ldc.i4     0x3e8
    IL_0059:  call       bool [localloc_common]LocallocTesting.Global::verifyStack(string,
                                                                          int32*,
                                                                          int32,
                                                                          int32)
    IL_005e:  brtrue     IL_006d

    IL_0063:  ldc.i4.0
    IL_0064:  stloc      iter
    IL_0068:  br         IL_00ec

    IL_006d:  ldstr      "ar2"
    IL_0072:  ldarg      ar2
    IL_0076:  ldsfld     int32 LocallocTest::locallocSize
    IL_007b:  ldc.i4     0x7d0
    IL_0080:  call       bool [localloc_common]LocallocTesting.Global::verifyStack(string,
                                                                          int32*,
                                                                          int32,
                                                                          int32)
    IL_0085:  brtrue     IL_0094

    IL_008a:  ldc.i4.0
    IL_008b:  stloc      iter
    IL_008f:  br         IL_00ec

    IL_0094:  ldstr      "ar3"
    IL_0099:  ldloc      ar3
    IL_009d:  ldsfld     int32 LocallocTest::locallocSize
    IL_00a2:  ldc.i4     0xbb8
    IL_00a7:  call       bool [localloc_common]LocallocTesting.Global::verifyStack(string,
                                                                          int32*,
                                                                          int32,
                                                                          int32)
    IL_00ac:  brtrue     IL_00bb

    IL_00b1:  ldc.i4.0
    IL_00b2:  stloc      iter
    IL_00b6:  br         IL_00ec

    IL_00bb:  ldstr      "ar4"
    IL_00c0:  ldloc      ar4
    IL_00c4:  ldsfld     int32 LocallocTest::locallocSize
    IL_00c9:  ldc.i4     0xfa0
    IL_00ce:  call       bool [localloc_common]LocallocTesting.Global::verifyStack(string,
                                                                          int32*,
                                                                          int32,
                                                                          int32)
    IL_00d3:  brtrue     IL_00e2

    IL_00d8:  ldc.i4.0
    IL_00d9:  stloc      iter
    IL_00dd:  br         IL_00ec

    IL_00e2:  ldc.i4.m1
    IL_00e3:  ldloc      iter
    IL_00e7:  add
    IL_00e8:  stloc      iter
    
    IL_00ec:  ldarg.0
    IL_00ed:  ldloc      iter
    IL_00f1:  ldarg      ar1
    IL_00f5:  ldarg      ar2
    IL_00f9:  tail.
    IL_00fb:  call       instance bool LocallocTest::func3(int32,
                                                           int32*,
                                                           int32*)
    IL_0100:  ret
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



