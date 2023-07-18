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
.assembly 'eh13_large'{}

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
    IL_0010:  ldc.i4	0x1000
    IL_0015:  mul
    IL_0016:  localloc
    IL_0018:  stloc.2
    IL_0019:  ldc.i4.4
    IL_001a:  ldc.i4	0x1000
    IL_001f:  mul
    IL_0020:  localloc
    IL_0022:  stloc.3
    IL_0023:  ldc.i4	0x1000
    IL_0028:  stsfld     int32 LocallocTest::locallocSize
    .try
    {
      IL_002d:  ldstr      "Test Exception"
      IL_0032:  newobj     instance void [mscorlib]System.Exception::.ctor(string)
      IL_0037:  throw

    }  // end .try
    filter
    {
		pop
		ldc.i4.1		// invoke handler block
		endfilter
    }
    {
      IL_0038:  pop
      ldstr "In outer Handler block"
      call       void [System.Console]System.Console::WriteLine(string)
      .try
      {
        IL_0039:  ldloc.2
        IL_003a:  ldsfld     int32 LocallocTest::locallocSize
        IL_003f:  ldc.i4     1000
        IL_0044:  call       void [localloc_common]LocallocTesting.Global::initializeStack(int32*,
                                                                                  int32,
                                                                                  int32)
        IL_0049:  ldloc.3
        IL_004a:  ldsfld     int32 LocallocTest::locallocSize
        IL_004f:  ldc.i4     2000
        IL_0054:  call       void [localloc_common]LocallocTesting.Global::initializeStack(int32*,
                                                                                  int32,
                                                                                  int32)
        IL_0059:  ldstr      "Test Exception"
        IL_005e:  newobj     instance void [mscorlib]System.Exception::.ctor(string)
        IL_0063:  throw

      }  // end .try
      filter
      {
		pop
		ldc.i4.1		// invoke handler block
		endfilter
      } 
      {
        IL_0064:  pop
        ldstr "In inner Handler block"
        call       void [System.Console]System.Console::WriteLine(string)
        IL_0065:  ldstr      "intArray1"
        IL_006a:  ldloc.2
        IL_006b:  ldsfld     int32 LocallocTest::locallocSize
        IL_0070:  ldc.i4     0x3e8
        IL_0075:  call       bool [localloc_common]LocallocTesting.Global::verifyStack(string,
                                                                              int32*,
                                                                              int32,
                                                                              int32)
        IL_007a:  brtrue.s   IL_0084

        IL_007c:  ldc.i4.1
        IL_007d:  stloc.s    retValue
        IL_007f:  leave      IL_0125

        IL_0084:  ldstr      "intArray2"
        IL_0089:  ldloc.3
        IL_008a:  ldsfld     int32 LocallocTest::locallocSize
        IL_008f:  ldc.i4     0x7d0
        IL_0094:  call       bool [localloc_common]LocallocTesting.Global::verifyStack(string,
                                                                              int32*,
                                                                              int32,
                                                                              int32)
        IL_0099:  brtrue.s   IL_00a3

        IL_009b:  ldc.i4.1
        IL_009c:  stloc.s    retValue
        IL_009e:  leave      IL_0125

        IL_00a3:  leave.s    IL_00a5

      }  // end handler
      IL_00a5:  leave.s    IL_00a7

    }  // end handler
    IL_00a7:  ldstr      "intArray1"
    IL_00ac:  ldloc.2
    IL_00ad:  ldsfld     int32 LocallocTest::locallocSize
    IL_00b2:  ldc.i4     0x3e8
    IL_00b7:  call       bool [localloc_common]LocallocTesting.Global::verifyStack(string,
                                                                          int32*,
                                                                          int32,
                                                                          int32)
    IL_00bc:  brtrue.s   IL_00c3

    IL_00be:  ldc.i4.1
    IL_00bf:  stloc.s    retValue
    IL_00c1:  br.s       IL_0125

    IL_00c3:  ldstr      "intArray2"
    IL_00c8:  ldloc.3
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
    IL_00e4:  ldloc.0
    IL_00e5:  ldc.i8     0xfedcba9876543210
    IL_00ee:  call       bool [localloc_common]LocallocTesting.Global::verifyLocal(string,
                                                                          unsigned int64,
                                                                          unsigned int64)
    IL_00f3:  brtrue.s   IL_00fa

    IL_00f5:  ldc.i4.1
    IL_00f6:  stloc.s    retValue
    IL_00f8:  br.s       IL_0125

    IL_00fa:  ldstr      "local2"
    IL_00ff:  ldloc.1
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
