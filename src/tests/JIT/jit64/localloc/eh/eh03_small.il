// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// Test localloc with filter + handler construct.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}
.assembly extern mscorlib{}
.assembly extern localloc_common {}
.assembly 'eh03_small'{}

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
    IL_0012:  ldc.i4.1
    IL_0017:  mul
    IL_0018:  localloc
    IL_001a:  stloc.3
    IL_001b:  ldc.i4.4
    IL_001c:  ldc.i4.1
    IL_0021:  mul
    IL_0022:  localloc
    IL_0024:  stloc.s    intArray2
    IL_0026:  ldc.i4.1
    IL_002b:  stsfld     int32 LocallocTest::locallocSize
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
    filter 
    {
      ldstr	"In filter block"
      call	void [System.Console]System.Console::WriteLine(string)
      IL_005c:  pop
      IL_005d:  ldstr      "intArray1"
      IL_0062:  ldloc.3
      IL_0063:  ldsfld     int32 LocallocTest::locallocSize
      IL_0068:  ldc.i4     1000
      IL_006d:  call       bool [localloc_common]LocallocTesting.Global::verifyStack(string,
                                                                            int32*,
                                                                            int32,
                                                                            int32)
      IL_0072:  brtrue.s   IL_0076

      IL_0074:  ldc.i4.0
      br.s L_ENDFILTER
      IL_0076:  ldstr      "intArray2"
      IL_007b:  ldloc.s    intArray2
      IL_007d:  ldsfld     int32 LocallocTest::locallocSize
      IL_0082:  ldc.i4     2000
      IL_0087:  call       bool [localloc_common]LocallocTesting.Global::verifyStack(string,
                                                                            int32*,
                                                                            int32,
                                                                            int32)
      IL_008c:  brtrue.s   IL_0090

      IL_008e:  ldc.i4.0
      br.s L_ENDFILTER
      IL_0090:  ldc.i4.1
      L_ENDFILTER: endfilter
    }		
    {		
      ldstr	"In handler block"
      call	void [System.Console]System.Console::WriteLine(string)
      pop
      ldstr      "intArray1"
      IL_0095:  ldloc.3
      IL_0096:  ldsfld     int32 LocallocTest::locallocSize
      IL_009b:  ldc.i4     1000
      IL_00a0:  call       bool [localloc_common]LocallocTesting.Global::verifyStack(string,
                                                                            int32*,
                                                                            int32,
                                                                            int32)
      IL_00a5:  brtrue.s   IL_00a9

      IL_00a7:  ldc.i4.0
      IL_00a8:  stloc.0
      IL_00a9:  ldstr      "intArray2"
      IL_00ae:  ldloc.s    intArray2
      IL_00b0:  ldsfld     int32 LocallocTest::locallocSize
      IL_00b5:  ldc.i4     2000
      IL_00ba:  call       bool [localloc_common]LocallocTesting.Global::verifyStack(string,
                                                                            int32*,
                                                                            int32,
                                                                            int32)
      IL_00bf:  brtrue.s   IL_00c3

      IL_00c1:  ldc.i4.0
      IL_00c2:  stloc.0
      IL_00c3:  leave.s    IL_00c5
    }  
    IL_00c5:  ldloc.0
    IL_00c6:  brtrue.s   IL_00cd

    IL_00c8:  ldc.i4.1
    IL_00c9:  stloc.s    retValue
    IL_00cb:  br.s       L_RET
    IL_00cd:  ldstr      "intArray1"
    IL_00d2:  ldloc.3
    IL_00d3:  ldsfld     int32 LocallocTest::locallocSize
    IL_00d8:  ldc.i4     1000
    IL_00dd:  call       bool [localloc_common]LocallocTesting.Global::verifyStack(string,
                                                                          int32*,
                                                                          int32,
                                                                          int32)
    IL_00e2:  brtrue.s   IL_00e9

    IL_00e4:  ldc.i4.1
    IL_00e5:  stloc.s    retValue
    IL_00e7:  br.s       L_RET

    IL_00e9:  ldstr      "intArray2"
    IL_00ee:  ldloc.s    intArray2
    IL_00f0:  ldsfld     int32 LocallocTest::locallocSize
    IL_00f5:  ldc.i4     2000
    IL_00fa:  call       bool [localloc_common]LocallocTesting.Global::verifyStack(string,
                                                                          int32*,
                                                                          int32,
                                                                          int32)
    IL_00ff:  brtrue.s   IL_0106

    IL_0101:  ldc.i4.1
    IL_0102:  stloc.s    retValue
    IL_0104:  br.s       L_RET
    IL_0106:  ldstr      "local1"
    IL_010b:  ldloc.1
    IL_010c:  ldc.i8     0xfedcba9876543210
    IL_0115:  call       bool [localloc_common]LocallocTesting.Global::verifyLocal(string,
                                                                          unsigned int64,
                                                                          unsigned int64)
    IL_011a:  brtrue.s   IL_0121

    IL_011c:  ldc.i4.1
    IL_011d:  stloc.s    retValue
    IL_011f:  br.s       L_RET

    IL_0121:  ldstr      "local2"
    IL_0126:  ldloc.2
    IL_0127:  ldc.i8     0xfedcba9876543211
    IL_0130:  call       bool [localloc_common]LocallocTesting.Global::verifyLocal(string,
                                                                          unsigned int64,
                                                                          unsigned int64)
    IL_0135:  brtrue.s   IL_013c

    IL_0137:  ldc.i4.1
    IL_0138:  stloc.s    retValue
    IL_013a:  br.s       L_RET

    IL_013c:  ldstr      "Passed\n"
    IL_0141:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0146:  ldc.i4.s   100
    IL_0148:  stloc.s    retValue

    L_RET:  ldloc.s    retValue
    IL_014e:  ret
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
