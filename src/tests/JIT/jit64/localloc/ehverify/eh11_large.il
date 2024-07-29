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
.assembly 'eh11_large'{}

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
    IL_0012:  ldc.i4	0x1000
    IL_0017:  mul
    IL_0018:  localloc
    IL_001a:  stloc.3
    IL_001b:  ldc.i4.4
    IL_001c:  ldc.i4	0x1000
    IL_0021:  mul
    IL_0022:  localloc
    IL_0024:  stloc.s    intArray2
    IL_0026:  ldc.i4	0x1000
    IL_002b:  stsfld     int32 LocallocTest::locallocSize
    .try
    {
      .try
      {
        IL_0030:  ldstr      "Test Exception"
        IL_0035:  newobj     instance void [mscorlib]System.Exception::.ctor(string)
        IL_003a:  throw

      }  // end .try
      fault
      {
        .try
        {
          IL_003b:  ldloc.3
          IL_003c:  ldsfld     int32 LocallocTest::locallocSize
          IL_0041:  ldc.i4     1000
          IL_0046:  call       void [localloc_common]LocallocTesting.Global::initializeStack(int32*,
                                                                                    int32,
                                                                                    int32)
          IL_004b:  ldloc.s    intArray2
          IL_004d:  ldsfld     int32 LocallocTest::locallocSize
          IL_0052:  ldc.i4     2000
          IL_0057:  call       void [localloc_common]LocallocTesting.Global::initializeStack(int32*,
                                                                                    int32,
                                                                                    int32)
          IL_005c:  ldstr      "Test Exception"
          IL_0061:  newobj     instance void [mscorlib]System.Exception::.ctor(string)
          IL_0066:  throw

        }  // end .try
        fault
        {
		  ldstr "In fault"
		  call       void [System.Console]System.Console::WriteLine(string)
          IL_0067:  ldstr      "intArray1"
          IL_006c:  ldloc.3
          IL_006d:  ldsfld     int32 LocallocTest::locallocSize
          IL_0072:  ldc.i4     1000
          IL_0077:  call       bool [localloc_common]LocallocTesting.Global::verifyStack(string,
                                                                                int32*,
                                                                                int32,
                                                                                int32)
          IL_007c:  brtrue.s   IL_0080

          IL_007e:  ldc.i4.0
          IL_007f:  stloc.0
          IL_0080:  ldstr      "intArray2"
          IL_0085:  ldloc.s    intArray2
          IL_0087:  ldsfld     int32 LocallocTest::locallocSize
          IL_008c:  ldc.i4     2000
          IL_0091:  call       bool [localloc_common]LocallocTesting.Global::verifyStack(string,
                                                                                int32*,
                                                                                int32,
                                                                                int32)
          IL_0096:  brtrue.s   IL_009a

          IL_0098:  ldc.i4.0
          IL_0099:  stloc.0
          IL_009a:  endfault
        }  // end handler
      }  // end handler
    }  // end .try
    catch [mscorlib]System.Object 
    {
      IL_009b:  pop
      IL_009c:  leave.s    IL_009e

    }  // end handler
    IL_009e:  ldloc.0
    IL_009f:  brtrue.s   IL_00a6
    IL_00a1:  ldc.i4.1
    IL_00a2:  stloc.s    retValue
    IL_00a4:  br.s       IL_0125

    IL_00a6:  ldstr      "intArray1"
    IL_00ab:  ldloc.3
    IL_00ac:  ldsfld     int32 LocallocTest::locallocSize
    IL_00b1:  ldc.i4     1000
    IL_00b6:  call       bool [localloc_common]LocallocTesting.Global::verifyStack(string,
                                                                          int32*,
                                                                          int32,
                                                                          int32)
    IL_00bb:  brtrue.s   IL_00c2

    IL_00bd:  ldc.i4.1
    IL_00be:  stloc.s    retValue
    IL_00c0:  br.s       IL_0125

    IL_00c2:  ldstr      "intArray2"
    IL_00c7:  ldloc.s    intArray2
    IL_00c9:  ldsfld     int32 LocallocTest::locallocSize
    IL_00ce:  ldc.i4     2000
    IL_00d3:  call       bool [localloc_common]LocallocTesting.Global::verifyStack(string,
                                                                          int32*,
                                                                          int32,
                                                                          int32)
    IL_00d8:  brtrue.s   IL_00df

    IL_00da:  ldc.i4.1
    IL_00db:  stloc.s    retValue
    IL_00dd:  br.s       IL_0125

    IL_00df:  ldstr      "local1"
    IL_00e4:  ldloc.1
    IL_00e5:  ldc.i8     0xfedcba9876543210
    IL_00ee:  call       bool [localloc_common]LocallocTesting.Global::verifyLocal(string,
                                                                          unsigned int64,
                                                                          unsigned int64)
    IL_00f3:  brtrue.s   IL_00fa

    IL_00f5:  ldc.i4.1
    IL_00f6:  stloc.s    retValue
    IL_00f8:  br.s       IL_0125

    IL_00fa:  ldstr      "local2"
    IL_00ff:  ldloc.2
    IL_0100:  ldc.i8     0xfedcba9876543211
    IL_0109:  call       bool [localloc_common]LocallocTesting.Global::verifyLocal(string,
                                                                          unsigned int64,
                                                                          unsigned int64)
    IL_010e:  brtrue.s   IL_0115

    IL_0110:  ldc.i4.1
    IL_0111:  stloc.s    retValue
    IL_0113:  br.s       IL_0125

    IL_0115:  ldstr      "Passed\n"
    IL_011a:  call       void [System.Console]System.Console::WriteLine(string)
    IL_011f:  ldc.i4.s   100
    IL_0121:  stloc.s    retValue
    IL_0123:  br.s       IL_0125

    IL_0125:  ldloc.s    retValue
    IL_0127:  ret
  } // end of method LocallocTest::Main

  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  stsfld     int32 LocallocTest::locallocSize
    IL_0006:  ret
  } // end of method LocallocTest::.cctor

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method LocallocTest::.ctor

} // end of class LocallocTest
