// localloc test with jmp


.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}
.assembly extern mscorlib{}
.assembly extern localloc_common{}
.assembly 'call05_small'{}

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
    .maxstack  11
    .locals (int32* intArray1,
             int32* intArray2,
             class LocallocTest V_2,
             int32 V_3)
    IL_0000:  ldc.i4.4
    IL_0001:  ldc.i4.1
    IL_0002:  mul
    IL_0003:  localloc
    IL_0005:  stloc.0
    IL_0006:  ldc.i4.4
    IL_0007:  ldc.i4.1
    IL_0008:  mul
    IL_0009:  localloc
    IL_000b:  stloc.1
    
    IL_000c:  ldc.i4.1
    IL_000d:  stsfld     int32 LocallocTest::locallocSize
    
    IL_0012:  ldloc.0
    IL_0013:  ldsfld     int32 LocallocTest::locallocSize
    IL_0018:  ldc.i4     1000
    IL_001d:  call       void [localloc_common]LocallocTesting.Global::initializeStack(int32*,
                                                                              int32,
                                                                              int32)
    IL_0022:  ldloc.1
    IL_0023:  ldsfld     int32 LocallocTest::locallocSize
    IL_0028:  ldc.i4     2000
    IL_002d:  call       void [localloc_common]LocallocTesting.Global::initializeStack(int32*,
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
    IL_0043:  callvirt   instance int32 LocallocTest::func0(int32,
                                                            int32,
                                                            int32,
                                                            int32,
                                                            int32,
                                                            int32,
                                                            int32,
                                                            int32,
                                                            int32*,
                                                            int32*)
    IL_0048:  stloc.3
    IL_004b:  ldloc.3
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
    .locals (unsigned int64 local1,
             unsigned int64 local2,
             int32 retValue)
    IL_0000:  ldc.i8     0xfedcba9876543210
    IL_0009:  stloc.0
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  conv.i8
    IL_000d:  add
    IL_000e:  stloc.1
    
    IL_0043:  jmp       instance int32 LocallocTest::func1(int32,
                                                            int32,
                                                            int32,
                                                            int32,
                                                            int32,
                                                            int32,
                                                            int32,
                                                            int32,
                                                            int32*,
                                                            int32*)
    IL_0055:  stloc.2
    IL_0058:  ldc.i4.1
    IL_0059:  ret
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
    .locals (int32* intArray1,
             int32* intArray2,
             int32 V_2)
    IL_0000:  ldc.i4.4
    IL_0001:  ldc.i4.1
    IL_0002:  mul
    IL_0003:  localloc
    IL_0005:  stloc.0
    IL_0006:  ldc.i4.4
    IL_0007:  ldc.i4.1
    IL_0008:  mul
    IL_0009:  localloc
    IL_000b:  stloc.1
    IL_000c:  ldloc.0
    IL_000d:  ldsfld     int32 LocallocTest::locallocSize
    IL_0012:  ldc.i4     0xbb8
    IL_0017:  call       void [localloc_common]LocallocTesting.Global::initializeStack(int32*,
                                                                              int32,
                                                                              int32)
    IL_001c:  ldloc.1
    IL_001d:  ldsfld     int32 LocallocTest::locallocSize
    IL_0022:  ldc.i4     0xfa0
    IL_0027:  call       void [localloc_common]LocallocTesting.Global::initializeStack(int32*,
                                                                              int32,
                                                                              int32)
    IL_002c:  ldstr      "ar1"
    IL_0031:  ldarg.s    ar1
    IL_0033:  ldsfld     int32 LocallocTest::locallocSize
    IL_0038:  ldc.i4     1000
    IL_003d:  call       bool [localloc_common]LocallocTesting.Global::verifyStack(string,
                                                                          int32*,
                                                                          int32,
                                                                          int32)
    IL_0042:  brtrue.s   IL_0048

    IL_0044:  ldc.i4.1
    IL_0045:  stloc.2
    IL_0046:  br.s       L_END

    IL_0048:  ldstr      "ar2"
    IL_004d:  ldarg.s    ar2
    IL_004f:  ldsfld     int32 LocallocTest::locallocSize
    IL_0054:  ldc.i4     2000
    IL_0059:  call       bool [localloc_common]LocallocTesting.Global::verifyStack(string,
                                                                          int32*,
                                                                          int32,
                                                                          int32)
    IL_005e:  brtrue.s   IL_0064

    IL_0060:  ldc.i4.1
    IL_0061:  stloc.2
    IL_0062:  br.s       L_END

    IL_0064:  ldstr      "intArray1"
    IL_0069:  ldloc.0
    IL_006a:  ldsfld     int32 LocallocTest::locallocSize
    IL_006f:  ldc.i4     3000
    IL_0074:  call       bool [localloc_common]LocallocTesting.Global::verifyStack(string,
                                                                          int32*,
                                                                          int32,
                                                                          int32)
    IL_0079:  brtrue.s   IL_007f

    IL_007b:  ldc.i4.1
    IL_007c:  stloc.2
    IL_007d:  br.s       L_END

    IL_007f:  ldstr      "intArray2"
    IL_0084:  ldloc.1
    IL_0085:  ldsfld     int32 LocallocTest::locallocSize
    IL_008a:  ldc.i4     4000
    IL_008f:  call       bool [localloc_common]LocallocTesting.Global::verifyStack(string,
                                                                          int32*,
                                                                          int32,
                                                                          int32)
    IL_0094:  brtrue.s   L_PASS

    IL_0096:  ldc.i4.1
    IL_0097:  stloc.2
    IL_0098:  br.s       L_END

    L_PASS:  ldstr      "Passed\n"
    IL_009f:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00a4:  ldc.i4.s   100
    IL_00a6:  stloc.2

    L_END:  ldloc.2
    IL_00aa:  ret
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



