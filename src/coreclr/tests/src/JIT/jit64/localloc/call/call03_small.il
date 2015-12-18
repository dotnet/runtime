// Tail call version of call01 test.


.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern mscorlib{}
.assembly extern common {}
.assembly call01_SMALL {}
.class private auto ansi beforefieldinit LocallocTest
       extends [mscorlib]System.Object
{
  .field private static int32 locallocSize
  .method public hidebysig static int32  Main() cil managed
  {
    .entrypoint
    .maxstack  10
    .locals (unsigned int64 local1,
             unsigned int64 local2,
             int32* intArray1,
             int32* intArray2)
    IL_0000:  ldc.i8     0xfedcba9876543210
    IL_0009:  stloc.0
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  conv.i8
    IL_000d:  add
    IL_000e:  stloc.1

    IL_000f:  ldc.i4.4
    IL_0010:  ldc.i4.1
    IL_0011:  mul
    IL_0012:  localloc
    IL_0014:  stloc.2
    IL_0015:  ldc.i4.4
    IL_0016:  ldc.i4.1
    IL_0017:  mul
    IL_0018:  localloc
    IL_001a:  stloc.3

    IL_001b:  ldc.i4.1
    IL_001c:  stsfld     int32 LocallocTest::locallocSize

    IL_0021:  ldloc.2
    IL_0022:  ldsfld     int32 LocallocTest::locallocSize
    IL_0027:  ldc.i4     1000
    IL_002c:  call       void [common]LocallocTesting.Global::initializeStack(int32*,
                                                                              int32,
                                                                              int32)
    IL_0031:  ldloc.3
    IL_0032:  ldsfld     int32 LocallocTest::locallocSize
    IL_0037:  ldc.i4     2000
    IL_003c:  call       void [common]LocallocTesting.Global::initializeStack(int32*,
                                                                              int32,
                                                                              int32)

	ldstr      "local1"
    ldloc.0
    ldc.i8     0xfedcba9876543210
    call       bool [common]LocallocTesting.Global::verifyLocal(string,
                                                                          unsigned int64,
                                                                          unsigned int64)
    brtrue.s   L_Verify1

    ldc.i4.1
    br.s       L_End
    L_Verify1:
	ldstr      "local2"
	ldloc.1
	ldc.i8     0xfedcba9876543211
	call       bool [common]LocallocTesting.Global::verifyLocal(string,
                                                                          unsigned int64,
                                                                          unsigned int64)
	brtrue.s   IL_0041

	ldc.i4.1
	br.s       L_End

    IL_0041:  ldc.i4.1
    IL_0042:  ldc.i4.2
    IL_0043:  ldc.i4.3
    IL_0044:  ldc.i4.4
    IL_0045:  ldc.i4.5
    IL_0046:  ldc.i4.6
    IL_0047:  ldc.i4.7
    IL_0048:  ldc.i4.8
    IL_0049:  ldloc.2
    IL_004a:  ldloc.3
    IL_004b:  tail. call       int32 LocallocTest::func1(int32,
                                                  int32,
                                                  int32,
                                                  int32,
                                                  int32,
                                                  int32,
                                                  int32,
                                                  int32,
                                                  int32*,
                                                  int32*)
    L_End:  ret
  } 

  .method private hidebysig static int32  func1(int32 i1,
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
    .locals (int32* intArray1,
             int32* intArray2,
             int32 RetValue)
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
    IL_0017:  call       void [common]LocallocTesting.Global::initializeStack(int32*,
                                                                              int32,
                                                                              int32)
    IL_001c:  ldloc.1
    IL_001d:  ldsfld     int32 LocallocTest::locallocSize
    IL_0022:  ldc.i4     0xfa0
    IL_0027:  call       void [common]LocallocTesting.Global::initializeStack(int32*,
                                                                              int32,
                                                                              int32)
    IL_002c:  ldstr      "ar1"
    IL_0031:  ldarg.s    ar1
    IL_0033:  ldsfld     int32 LocallocTest::locallocSize
    IL_0038:  ldc.i4     0x3e8
    IL_003d:  call       bool [common]LocallocTesting.Global::verifyStack(string,
                                                                          int32*,
                                                                          int32,
                                                                          int32)
    IL_0042:  brtrue.s   L_Verify1

    IL_0044:  ldc.i4.1
    IL_0045:  stloc.2
    IL_0046:  br.s       IL_009e

    L_Verify1:  ldstr      "ar2"
    IL_004d:  ldarg.s    ar2
    IL_004f:  ldsfld     int32 LocallocTest::locallocSize
    IL_0054:  ldc.i4     0x7d0
    IL_0059:  call       bool [common]LocallocTesting.Global::verifyStack(string,
                                                                          int32*,
                                                                          int32,
                                                                          int32)
    IL_005e:  brtrue.s   L_Verify2

    IL_0060:  ldc.i4.1
    IL_0061:  stloc.2
    IL_0062:  br.s       IL_009e

    L_Verify2:  ldstr      "intArray1"
    IL_0069:  ldloc.0
    IL_006a:  ldsfld     int32 LocallocTest::locallocSize
    IL_006f:  ldc.i4     0xbb8
    IL_0074:  call       bool [common]LocallocTesting.Global::verifyStack(string,
                                                                          int32*,
                                                                          int32,
                                                                          int32)
    IL_0079:  brtrue.s   L_Verify3

    IL_007b:  ldc.i4.1
    IL_007c:  stloc.2
    IL_007d:  br.s       IL_009e

    L_Verify3:  ldstr      "intArray2"
    IL_0084:  ldloc.1
    IL_0085:  ldsfld     int32 LocallocTest::locallocSize
    IL_008a:  ldc.i4     0xfa0
    IL_008f:  call       bool [common]LocallocTesting.Global::verifyStack(string,
                                                                          int32*,
                                                                          int32,
                                                                          int32)
    IL_0094:  brtrue.s   L_Passed

    IL_0096:  ldc.i4.1
    IL_0097:  stloc.2
    IL_0098:  br.s       IL_009e

    L_Passed:  ldstr      "Passed\n"
	call       void [System.Console]System.Console::WriteLine(string)
	ldc.i4.s	100
    IL_009b:  stloc.2
    IL_009c:  br.s       IL_009e

    IL_009e:  ldloc.2
    IL_009f:  ret
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
