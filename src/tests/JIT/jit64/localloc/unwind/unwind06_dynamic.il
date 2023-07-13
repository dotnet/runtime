// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// Unwind from Handler block.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}
.assembly extern mscorlib{}
.assembly extern localloc_common{}
.assembly 'unwind06_dynamic'{}

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
      IL_003a:  leave.s    IL_009d

    }  
    catch [mscorlib]System.Object 
    {
      IL_003c:  pop
      IL_003d:  leave.s    IL_003f

    }  
    IL_003f:  ldstr      "intArray1"
    IL_0044:  ldloc.2
    IL_0045:  ldsfld     int32 LocallocTest::locallocSize
    IL_004a:  ldc.i4     1000
    IL_004f:  call       bool [localloc_common]LocallocTesting.Global::verifyStack(string,
                                                                          int32*,
                                                                          int32,
                                                                          int32)
    IL_0054:  brtrue.s   IL_005a

    IL_0056:  ldc.i4.1
    IL_0057:  stloc.3
    IL_0058:  br.s       IL_009d
    IL_005a:  ldstr      "local1"
    IL_005f:  ldloc.0
    IL_0060:  ldc.i8     0xfedcba9876543210
    IL_0069:  call       bool [localloc_common]LocallocTesting.Global::verifyLocal(string,
                                                                          unsigned int64,
                                                                          unsigned int64)
    IL_006e:  brtrue.s   IL_0074

    IL_0070:  ldc.i4.1
    IL_0071:  stloc.3
    IL_0072:  br.s       IL_009d

    IL_0074:  ldstr      "local2"
    IL_0079:  ldloc.1
    IL_007a:  ldc.i8     0xfedcba9876543211
    IL_0083:  call       bool [localloc_common]LocallocTesting.Global::verifyLocal(string,
                                                                          unsigned int64,
                                                                          unsigned int64)
    IL_0088:  brtrue.s   IL_008e

    IL_008a:  ldc.i4.1
    IL_008b:  stloc.3
    IL_008c:  br.s       IL_009d
    IL_008e:  ldstr      "Passed\n"
    IL_0093:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0098:  ldc.i4.s   100
    IL_009a:  stloc.3
    IL_009b:  br.s       IL_009d

    IL_009d:  ldloc.3
    IL_009e:  ret
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
      IL_0030:  leave.s    IL_0058

    }  
    filter
    {
		pop
		ldc.i4.1		
		endfilter
	}
    {
      IL_0032:  pop
      IL_0033:  ldstr      "Handler block in unwindTest1.\n"
      IL_0038:  call       void [System.Console]System.Console::WriteLine(string)
      IL_003d:  ldstr      "intArray2"
      IL_0042:  ldloc.0
      IL_0043:  ldsfld     int32 LocallocTest::locallocSize
      IL_0048:  ldc.i4     2000
      IL_004d:  call       bool [localloc_common]LocallocTesting.Global::verifyStack(string,
                                                                            int32*,
                                                                            int32,
                                                                            int32)
      IL_0052:  brfalse.s  IL_0056

      IL_0054:  rethrow				
      IL_0056:  leave.s    IL_0058

    }  
    IL_0058:  br.s       IL_005a

    IL_005a:  ret
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
    .try
    {
      IL_001a:  ldstr      "Test Exception"
      IL_001f:  newobj     instance void [mscorlib]System.Exception::.ctor(string)
      IL_0024:  throw

    }  
    filter
    {
		pop
		ldc.i4.1		
		endfilter
	}
    {
      IL_0025:  pop
      IL_0026:  ldstr      "Handler block in unwindTest2.\n"
      IL_002b:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0030:  ldarg.s    i9
      IL_0032:  ldc.i4.s   9
      IL_0034:  beq.s      IL_0042

      IL_0036:  ldstr      "Parameters on stack is corrupted\n"
      IL_003b:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0040:  leave.s    IL_005d
      IL_0042:  ldstr      "intArray3"
      IL_0047:  ldloc.0
      IL_0048:  ldsfld     int32 LocallocTest::locallocSize
      IL_004d:  ldc.i4     3000
      IL_0052:  call       bool [localloc_common]LocallocTesting.Global::verifyStack(string,
                                                                            int32*,
                                                                            int32,
                                                                            int32)
      IL_0057:  brfalse.s  IL_005b

      IL_0059:  rethrow			
      IL_005b:  leave.s    IL_005d

    }  
    IL_005d:  ret
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
