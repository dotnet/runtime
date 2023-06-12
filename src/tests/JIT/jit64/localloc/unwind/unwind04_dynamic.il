// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// Unwind from fault block

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}
.assembly extern mscorlib{}
.assembly extern localloc_common{}
.assembly 'unwind04_dynamic'{}

.class public auto ansi beforefieldinit LocallocTest
       extends [mscorlib]System.Object
{
  .field private static bool testFailed
  .field private static int32 locallocSize
  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  4
    .locals (unsigned int64 local1,
             unsigned int64 local2,
             int32* intArray1,
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
    .try
    {
      IL_0023:  ldloc.2
      IL_0024:  ldsfld     int32 LocallocTest::locallocSize
      IL_0029:  ldc.i4     1000
      IL_002e:  call       void [localloc_common]LocallocTesting.Global::initializeStack(int32*,
                                                                                int32,
                                                                                int32)
      IL_0033:  call       void LocallocTest::unwindTest1()
      IL_0038:  ldc.i4.1
      IL_0039:  stloc.3
      IL_003a:  leave.s    IL_00a8

    }  
    catch [mscorlib]System.Object 
    {
      IL_003c:  pop
      IL_003d:  leave.s    IL_003f

    }  
    IL_003f:  ldsfld     bool LocallocTest::testFailed
    IL_0044:  brfalse.s  IL_004a

    IL_0046:  ldc.i4.1
    IL_0047:  stloc.3
    IL_0048:  br.s       IL_00a8
    IL_004a:  ldstr      "intArray1"
    IL_004f:  ldloc.2
    IL_0050:  ldsfld     int32 LocallocTest::locallocSize
    IL_0055:  ldc.i4     1000
    IL_005a:  call       bool [localloc_common]LocallocTesting.Global::verifyStack(string,
                                                                          int32*,
                                                                          int32,
                                                                          int32)
    IL_005f:  brtrue.s   IL_0065

    IL_0061:  ldc.i4.1
    IL_0062:  stloc.3
    IL_0063:  br.s       IL_00a8
    IL_0065:  ldstr      "local1"
    IL_006a:  ldloc.0
    IL_006b:  ldc.i8     0xfedcba9876543210
    IL_0074:  call       bool [localloc_common]LocallocTesting.Global::verifyLocal(string,
                                                                          unsigned int64,
                                                                          unsigned int64)
    IL_0079:  brtrue.s   IL_007f

    IL_007b:  ldc.i4.1
    IL_007c:  stloc.3
    IL_007d:  br.s       IL_00a8

    IL_007f:  ldstr      "local2"
    IL_0084:  ldloc.1
    IL_0085:  ldc.i8     0xfedcba9876543211
    IL_008e:  call       bool [localloc_common]LocallocTesting.Global::verifyLocal(string,
                                                                          unsigned int64,
                                                                          unsigned int64)
    IL_0093:  brtrue.s   IL_0099

    IL_0095:  ldc.i4.1
    IL_0096:  stloc.3
    IL_0097:  br.s       IL_00a8
    IL_0099:  ldstr      "Passed\n"
    IL_009e:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00a3:  ldc.i4.s   100
    IL_00a5:  stloc.3
    IL_00a6:  br.s       IL_00a8

    IL_00a8:  ldloc.3
    IL_00a9:  ret
  } 

  .method public hidebysig static void  unwindTest1() cil managed
  {
    .maxstack  10
    .locals (int32* intArray2,
             class LocallocTest testObj)
    IL_0000:  ldc.i4.4
    IL_0001:  ldsfld     int32 [localloc_common]LocallocTesting.Global::stackAllocSize
    IL_0006:  mul
    IL_0007:  localloc
    IL_0009:  stloc.0
    IL_000a:  ldloc.0
    IL_000b:  ldsfld     int32 LocallocTest::locallocSize
    IL_0010:  ldc.i4     2000
    IL_0015:  call       void [localloc_common]LocallocTesting.Global::initializeStack(int32*,
                                                                              int32,
                                                                              int32)
    .try
    {
      IL_001a:  newobj     instance void LocallocTest::.ctor()
      IL_001f:  stloc.1
      IL_0020:  ldloc.1
      IL_0021:  ldc.i4.1
      IL_0022:  ldc.i4.2
      IL_0023:  ldc.i4.3
      IL_0024:  ldc.i4.4
      IL_0025:  ldc.i4.5
      IL_0026:  ldc.i4.6
      IL_0027:  ldc.i4.7
      IL_0028:  ldc.i4.8
      IL_0029:  ldc.i4.s   9
      IL_002b:  callvirt   instance void LocallocTest::unwindTest2(int32,
                                                                   int32,
                                                                   int32,
                                                                   int32,
                                                                   int32,
                                                                   int32,
                                                                   int32,
                                                                   int32,
                                                                   int32)
      IL_0030:  leave.s    IL_0061

    }  
    fault
    {
      IL_0032:  ldstr      "fault block in unwindTest1.\n"
      IL_0037:  call       void [System.Console]System.Console::WriteLine(string)
      IL_003c:  ldsfld     bool LocallocTest::testFailed
      IL_0041:  brtrue.s   IL_0060
      IL_0043:  ldstr      "intArray2"
      IL_0048:  ldloc.0
      IL_0049:  ldsfld     int32 LocallocTest::locallocSize
      IL_004e:  ldc.i4     2000
      IL_0053:  call       bool [localloc_common]LocallocTesting.Global::verifyStack(string,
                                                                            int32*,
                                                                            int32,
                                                                            int32)
      IL_0058:  ldc.i4.0
      IL_0059:  ceq
      IL_005b:  stsfld     bool LocallocTest::testFailed
      IL_0060:  endfault
    }  
    IL_0061:  br.s       IL_0063

    IL_0063:  ret
  } 

  .method private hidebysig instance void 
          unwindTest2(int32 i1,
                      int32 i2,
                      int32 i3,
                      int32 i4,
                      int32 i5,
                      int32 i6,
                      int32 i7,
                      int32 i8,
                      int32 i9) cil managed
  {
    .maxstack  4
    .locals (int32* intArray3)
    IL_0000:  ldc.i4.4
    IL_0001:  ldsfld     int32 [localloc_common]LocallocTesting.Global::stackAllocSize
    IL_0006:  mul
    IL_0007:  localloc
    IL_0009:  stloc.0
    IL_000a:  ldloc.0
    IL_000b:  ldsfld     int32 LocallocTest::locallocSize
    IL_0010:  ldc.i4     3000
    IL_0015:  call       void [localloc_common]LocallocTesting.Global::initializeStack(int32*,
                                                                              int32,
                                                                              int32)
    .try{
		IL_001a:  ldstr      "Test Exception"
		IL_001f:  newobj     instance void [mscorlib]System.Exception::.ctor(string)
		IL_0024:  throw
	}
	fault
	{
		IL_0025:  ldstr      "fault block in unwindTest2.\n"
		IL_002a:  call       void [System.Console]System.Console::WriteLine(string)
		IL_002f:  ldarg.s    i9
		IL_0031:  ldc.i4.s   9
		IL_0033:  beq.s      IL_0047

		IL_0035:  ldstr      "Parameters on stack is corrupted\n"
		IL_003a:  call       void [System.Console]System.Console::WriteLine(string)
		IL_003f:  ldc.i4.1
		IL_0040:  stsfld     bool LocallocTest::testFailed
		IL_0045:  br.s       IL_0064
		IL_0047:  ldstr      "intArray3"
		IL_004c:  ldloc.0
		IL_004d:  ldsfld     int32 LocallocTest::locallocSize
		IL_0052:  ldc.i4     3000
		IL_0057:  call       bool [localloc_common]LocallocTesting.Global::verifyStack(string,
																			int32*,
																			int32,
																			int32)
		IL_005c:  ldc.i4.0
		IL_005d:  ceq
		IL_005f:  stsfld     bool LocallocTest::testFailed
		IL_0064:  endfault
	}
    IL_0065:  
  } 

  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  stsfld     bool LocallocTest::testFailed
    IL_0006:  ldc.i4.0
    IL_0007:  stsfld     int32 LocallocTest::locallocSize
    IL_000c:  ret
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
