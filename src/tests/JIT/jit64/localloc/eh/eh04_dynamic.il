// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}
.assembly extern mscorlib{}
.assembly extern localloc_common{}
.assembly 'eh04_dynamic'{}

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
    .locals (bool testPassed,
             unsigned int64 local1,
             unsigned int64 local2,
             int32* intArray1,
             int32* intArray2,
             int32 retValue)
    IL_0000:  ldc.i4.1
    IL_0001:  stloc.0
    IL_0002:  ldc.i8     0xfedcba9876543210
    IL_000b:  stloc.1
    IL_000c:  ldloc.1
    IL_000d:  ldc.i4.1
    IL_000e:  conv.i8
    IL_000f:  add
    IL_0010:  stloc.2
    IL_0011:  ldc.i4.4
    IL_0012:  ldsfld     int32 [localloc_common]LocallocTesting.Global::stackAllocSize
    IL_0017:  mul
    IL_0018:  localloc
    IL_001a:  stloc.3
    IL_001b:  ldc.i4.4
    IL_001c:  ldsfld     int32 [localloc_common]LocallocTesting.Global::stackAllocSize
    IL_0021:  mul
    IL_0022:  localloc
    IL_0024:  stloc.s    intArray2
    IL_0026:  ldsfld     int32 [localloc_common]LocallocTesting.Global::stackAllocSize
    IL_002b:  stsfld     int32 LocallocTest::locallocSize
    .try
    {
      .try
      {
        IL_0030:  ldloc.3
        IL_0031:  ldsfld     int32 LocallocTest::locallocSize
        IL_0036:  ldc.i4     1000
        IL_003b:  call       void [localloc_common]LocallocTesting.Global::initializeStack(int32*,
                                                                                  int32,
                                                                                  int32)
        IL_0040:  ldloc.s    intArray2
        IL_0042:  ldsfld     int32 LocallocTest::locallocSize
        IL_0047:  ldc.i4     2000
        IL_004c:  call       void [localloc_common]LocallocTesting.Global::initializeStack(int32*,
                                                                                  int32,
                                                                                  int32)
        IL_0051:  ldstr      "Test Exception"
        IL_0056:  newobj     instance void [mscorlib]System.Exception::.ctor(string)
        IL_005b:  throw

      }  
      fault
      {
        ldstr "In fault"
        call       void [System.Console]System.Console::WriteLine(string)
        IL_005c:  ldstr      "intArray1"
        IL_0061:  ldloc.3
        IL_0062:  ldsfld     int32 LocallocTest::locallocSize
        IL_0067:  ldc.i4     1000
        IL_006c:  call       bool [localloc_common]LocallocTesting.Global::verifyStack(string,
                                                                              int32*,
                                                                              int32,
                                                                              int32)
        IL_0071:  brtrue.s   IL_0075

        IL_0073:  ldc.i4.0
        IL_0074:  stloc.0
        IL_0075:  ldstr      "intArray2"
        IL_007a:  ldloc.s    intArray2
        IL_007c:  ldsfld     int32 LocallocTest::locallocSize
        IL_0081:  ldc.i4     2000
        IL_0086:  call       bool [localloc_common]LocallocTesting.Global::verifyStack(string,
                                                                              int32*,
                                                                              int32,
                                                                              int32)
        IL_008b:  brtrue.s   IL_008f

        IL_008d:  ldc.i4.0
        IL_008e:  stloc.0
        IL_008f:  endfault
      }  
    }  
    catch [mscorlib]System.Object 
    {
      IL_0090:  pop
      IL_0091:  leave.s    IL_0093

    }  
    IL_0093:  ldloc.0
    IL_0094:  brtrue.s   IL_009b

    IL_0096:  ldc.i4.1
    IL_0097:  stloc.s    retValue
    IL_0099:  br.s       IL_011a
    IL_009b:  ldstr      "intArray1"
    IL_00a0:  ldloc.3
    IL_00a1:  ldsfld     int32 LocallocTest::locallocSize
    IL_00a6:  ldc.i4     1000
    IL_00ab:  call       bool [localloc_common]LocallocTesting.Global::verifyStack(string,
                                                                          int32*,
                                                                          int32,
                                                                          int32)
    IL_00b0:  brtrue.s   IL_00b7

    IL_00b2:  ldc.i4.1
    IL_00b3:  stloc.s    retValue
    IL_00b5:  br.s       IL_011a

    IL_00b7:  ldstr      "intArray2"
    IL_00bc:  ldloc.s    intArray2
    IL_00be:  ldsfld     int32 LocallocTest::locallocSize
    IL_00c3:  ldc.i4     2000
    IL_00c8:  call       bool [localloc_common]LocallocTesting.Global::verifyStack(string,
                                                                          int32*,
                                                                          int32,
                                                                          int32)
    IL_00cd:  brtrue.s   IL_00d4

    IL_00cf:  ldc.i4.1
    IL_00d0:  stloc.s    retValue
    IL_00d2:  br.s       IL_011a
    IL_00d4:  ldstr      "local1"
    IL_00d9:  ldloc.1
    IL_00da:  ldc.i8     0xfedcba9876543210
    IL_00e3:  call       bool [localloc_common]LocallocTesting.Global::verifyLocal(string,
                                                                          unsigned int64,
                                                                          unsigned int64)
    IL_00e8:  brtrue.s   IL_00ef

    IL_00ea:  ldc.i4.1
    IL_00eb:  stloc.s    retValue
    IL_00ed:  br.s       IL_011a

    IL_00ef:  ldstr      "local2"
    IL_00f4:  ldloc.2
    IL_00f5:  ldc.i8     0xfedcba9876543211
    IL_00fe:  call       bool [localloc_common]LocallocTesting.Global::verifyLocal(string,
                                                                          unsigned int64,
                                                                          unsigned int64)
    IL_0103:  brtrue.s   IL_010a

    IL_0105:  ldc.i4.1
    IL_0106:  stloc.s    retValue
    IL_0108:  br.s       IL_011a

    IL_010a:  ldstr      "Passed\n"
    IL_010f:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0114:  ldc.i4.s   100
    IL_0116:  stloc.s    retValue
    IL_0118:  br.s       IL_011a

    IL_011a:  ldloc.s    retValue
    IL_011c:  ret
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
