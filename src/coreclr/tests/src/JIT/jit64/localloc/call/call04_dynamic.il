//  tail call version of call02.


.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern mscorlib{}
.assembly extern common {}

.assembly call02 {}

.class private auto ansi beforefieldinit LocallocTest
       extends [mscorlib]System.Object
{
  .field private static int32 locallocSize
  .method public hidebysig static int32  Main() cil managed
  {
    .entrypoint
    .maxstack  11
    .locals (unsigned int64 local1,
             unsigned int64 local2,
             int32* intArray1,
             int32* intArray2,
             class [common]LocallocTesting.TestClass obj)
    IL_0000:  ldc.i8     0xfedcba9876543210
    IL_0009:  stloc.0
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  conv.i8
    IL_000d:  add
    IL_000e:  stloc.1

    IL_000f:  ldc.i4.4
    IL_0010:  ldsfld     int32 [common]LocallocTesting.Global::stackAllocSize
    IL_0011:  mul
    IL_0012:  localloc
    IL_0014:  stloc.2
    IL_0015:  ldc.i4.4
    IL_0016:  ldsfld     int32 [common]LocallocTesting.Global::stackAllocSize
    IL_0017:  mul
    IL_0018:  localloc
    IL_001a:  stloc.3

    IL_001b:  ldsfld     int32 [common]LocallocTesting.Global::stackAllocSize
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

    IL_0041:  ldc.i4     9999
    IL_0046:  conv.i8
    IL_0047:  newobj     instance void [common]LocallocTesting.TestClass::.ctor(int64)
    IL_004c:  stloc.s    obj

    IL_004e:  ldc.i4.1
    IL_004f:  ldc.i4.2
    IL_0050:  ldc.i4.3
    IL_0051:  ldc.i4.4
    IL_0052:  ldc.i4.5
    IL_0053:  ldc.i4.6
    IL_0054:  ldc.i4.7
    IL_0055:  ldc.i4.8
    IL_0056:  ldloc.2
    IL_0057:  ldloc.s    obj
    IL_0059:  ldloc.3
    IL_005a:  tail. call       int32 LocallocTest::func1(int32,
                                                   int32,
                                                   int32,
                                                   int32,
                                                   int32,
                                                   int32,
                                                   int32,
                                                   int32,
                                                   int32*,
                                                   class [common]LocallocTesting.TestClass,
                                                   int32*)

    IL_0065:  ret
  } 

  .method private hidebysig static int32 
          func1(int32 i1,
                int32 i2,
                int32 i3,
                int32 i4,
                int32 i5,
                int32 i6,
                int32 i7,
                int32 i8,
                int32* ar1,
                class [common]LocallocTesting.TestClass obj,
                int32* ar2) cil managed
  {
    .maxstack  4
    .locals (int32* intArray1,
             int32* intArray2,
             int32 retValue)

    ldc.i4.4
    IL_0078:  ldsfld     int32 [common]LocallocTesting.Global::stackAllocSize
    IL_0079:  mul
    IL_007a:  localloc
    IL_007c:  stloc.s    intArray1
    IL_007e:  ldc.i4.4
    IL_007f:  ldsfld     int32 [common]LocallocTesting.Global::stackAllocSize
    IL_0080:  mul
    IL_0081:  localloc
    IL_0083:  stloc.s    intArray2

    IL_0085:  ldloc.s    intArray1
    IL_0087:  ldsfld     int32 LocallocTest::locallocSize
    IL_008c:  ldc.i4     3000
    IL_0091:  call       void [common]LocallocTesting.Global::initializeStack(int32*,
                                                                              int32,
                                                                              int32)
    IL_0096:  ldloc.s    intArray2
    IL_0098:  ldsfld     int32 LocallocTest::locallocSize
    IL_009d:  ldc.i4     4000
    IL_00a2:  call       void [common]LocallocTesting.Global::initializeStack(int32*,
                                                                              int32,
                                                                              int32)
    IL_0036:  ldarg.s    obj
    IL_0037:  callvirt   instance bool [common]LocallocTesting.TestClass::ForceGC()
    IL_003c:  brtrue.s   L_Forced
    IL_003d:  ldstr      "Failed to force GC!"
    IL_0042:  call       void [System.Console]System.Console::WriteLine(string)
    IL_004e:  ldc.i4.1
    IL_004f:  stloc.s    retValue
    IL_0051:  br         L_END

    L_Forced:  ldarg.s    obj
    IL_00a9:  ldfld      int64 [common]LocallocTesting.TestClass::id
    IL_00ae:  ldc.i4     0x270f
    IL_00b3:  conv.i8
    IL_00b4:  beq.s      L_Verify1

    IL_00b6:  ldstr      "obj.id is corrupted."
    IL_00bb:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00c0:  ldc.i4.1
    IL_00c1:  stloc.s    retValue
    IL_00c3:  br         L_END

    L_Verify1: ldstr      "ar1"
    IL_00cd:  ldarg.s    ar1
    IL_00cf:  ldsfld     int32 LocallocTest::locallocSize
    IL_00d4:  ldc.i4     1000
    IL_00d9:  call       bool [common]LocallocTesting.Global::verifyStack(string,
                                                                          int32*,
                                                                          int32,
                                                                          int32)
    IL_00de:  brtrue.s   L_Verify2

    IL_00e0:  ldc.i4.1
    IL_00e1:  stloc.s    retValue
    IL_00e3:  br.s       L_END

    L_Verify2:  ldstr      "ar2"
    IL_00ea:  ldarg.s    ar2
    IL_00ec:  ldsfld     int32 LocallocTest::locallocSize
    IL_00f1:  ldc.i4     2000
    IL_00f6:  call       bool [common]LocallocTesting.Global::verifyStack(string,
                                                                          int32*,
                                                                          int32,
                                                                          int32)
    IL_00fb:  brtrue.s   L_Verify3

    IL_00fd:  ldc.i4.1
    IL_00fe:  stloc.s    retValue
    IL_0100:  br.s       L_END

    L_Verify3:  ldstr      "intArray1"
    IL_0107:  ldloc.s    intArray1
    IL_0109:  ldsfld     int32 LocallocTest::locallocSize
    IL_010e:  ldc.i4     3000
    IL_0113:  call       bool [common]LocallocTesting.Global::verifyStack(string,
                                                                          int32*,
                                                                          int32,
                                                                          int32)
    IL_0118:  brtrue.s   L_Verify4

    IL_011a:  ldc.i4.1
    IL_011b:  stloc.s    retValue
    IL_011d:  br.s       L_END

    L_Verify4: ldstr      "intArray2"
    IL_0124:  ldloc.s    intArray2
    IL_0126:  ldsfld     int32 LocallocTest::locallocSize
    IL_012b:  ldc.i4     4000
    IL_0130:  call       bool [common]LocallocTesting.Global::verifyStack(string,
                                                                          int32*,
                                                                          int32,
                                                                          int32)
    IL_0135:  brtrue.s   L_Passed

    IL_0137:  ldc.i4.1
    IL_0138:  stloc.s    retValue
    IL_013a:  br.s       L_END

    L_Passed:  ldstr      "Passed"
    IL_0141:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0146:  ldc.i4.s   100
    IL_0148:  stloc.s    retValue
    IL_014a:  br.s       L_END

    L_END:    ldloc.s    retValue
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



