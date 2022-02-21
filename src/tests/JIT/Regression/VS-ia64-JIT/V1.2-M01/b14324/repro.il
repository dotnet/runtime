// Test localloc with tail call followed by jump
.assembly extern legacy library mscorlib {}
.assembly extern localloc_common{}
.assembly extern legacy library mscorlib {}
.assembly 'repro'{}
.assembly extern xunit.core {}

.class private auto ansi beforefieldinit LocallocTest
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
             class LocallocTest V_2)
    IL_0000:  ldc.i4.4
    IL_0001:  ldc.i4.1
    IL_0002:  mul
    IL_0003:  localloc
    IL_0005:  stloc.0
    IL_000c:  ldc.i4.1
    IL_000d:  stsfld     int32 LocallocTest::locallocSize
    IL_0012:  ldloc.0
    IL_0013:  ldsfld     int32 LocallocTest::locallocSize
    IL_0018:  ldc.i4     1000
    IL_001d:  call       void [localloc_common]LocallocTesting.Global::initializeStack(int32*,
                                                                              int32,
                                                                              int32)
    IL_0032:  newobj     instance void LocallocTest::.ctor()
    IL_0037:  stloc.1
    IL_0038:  ldloc.1
    IL_0039:  ldc.i4.1
    IL_003a:  ldc.i4.2
    IL_003b:  ldc.i4.3
    IL_003c:  ldc.i4.4
    IL_003d:  ldc.i4.5
    IL_003e:  ldc.i4.6
    IL_003f:  ldc.i4.7
    IL_0040:  ldc.i4.8
    IL_0041:  ldloc.0
    IL_0043:  callvirt   instance int32 LocallocTest::func0(int32,
                                                            int32,
                                                            int32,
                                                            int32,
                                                            int32,
                                                            int32,
                                                            int32,
                                                            int32,
                                                            int32*)
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
                int32* ar1) cil managed
  {
    .maxstack  11
    .locals (int32 retValue)
    IL_001c:  ldstr      "ar1"
    IL_0021:  ldarg.s    ar1
    IL_0023:  ldsfld     int32 LocallocTest::locallocSize
    IL_0028:  ldc.i4     1000
    IL_002d:  call       bool [localloc_common]LocallocTesting.Global::verifyStack(string,
                                                                          int32*,
                                                                          int32,
                                                                          int32)
    IL_0032:  brtrue.s   L_JMP

    IL_0034:  ldc.i4.1
    IL_0035:  stloc.0
    IL_0036:  br.s       L_END

    L_JMP:    jmp       instance int32 LocallocTest::func1(int32,
                                                            int32,
                                                            int32,
                                                            int32,
                                                            int32,
                                                            int32,
                                                            int32,
                                                            int32,
                                                            int32*)
    IL_004a:  stloc.0

    L_END:  ldc.i4	99
    IL_004e:  ret
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
                int32* ar1) cil managed
  {
    .maxstack  4
    .locals (unsigned int64 local1,
             unsigned int64 local2,
             int32* intArray1,
             int32* intArray2,
             int32 retValue)

    IL_00f3:  ldc.i4.s   100
    IL_00fb:  ret
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



