// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.





.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern mscorlib
{
}
.assembly test3
{


  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module test3.exe
.custom instance void [mscorlib]System.Security.UnverifiableCodeAttribute::.ctor() = ( 01 00 00 00 ) 
.imagebase 0x00400000
.subsystem 0x00000003
.file alignment 512
.corflags 0x00000001


.class private auto ansi beforefieldinit T
       extends [mscorlib]System.Object
{
  .field public static int32 size_to_test
  .method public hidebysig static int32  Main() cil managed
  {
    .entrypoint
    .maxstack  4
    .locals init (bool V_0,
                  int32* V_1,
                  int32 V_2)
    IL_0000:  ldc.i4.1
    IL_0001:  stloc.0
    IL_0002:  call       void T::dirtyStack()
    IL_0007:  ldc.i4.4
    IL_0008:  ldsfld     int32 T::size_to_test
    IL_000d:  mul
    IL_000e:  localloc
    IL_0010:  stloc.1
    IL_0011:  ldc.i4.0
    IL_0012:  stloc.2
    IL_0013:  br.s       IL_0023

    IL_0015:  ldloc.1
    IL_0016:  ldc.i4.4
    IL_0017:  ldloc.2
    IL_0018:  mul
    IL_0019:  add
    IL_001a:  ldind.i4
    IL_001b:  brfalse.s  IL_001f

    IL_001d:  ldc.i4.0
    IL_001e:  stloc.0
    IL_001f:  ldloc.2
    IL_0020:  ldc.i4.1
    IL_0021:  add
    IL_0022:  stloc.2
    IL_0023:  ldloc.2
    IL_0024:  ldsfld     int32 T::size_to_test
    IL_0029:  blt.s      IL_0015

    IL_002b:  ldloc.0
    IL_002c:  brfalse.s  IL_003a

    IL_002e:  ldstr      "pass"
    IL_0033:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0038:  ldc.i4 100
		ret

    IL_003a:  ldstr      "fail"
    IL_003f:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0044:  ldc.i4 1
		ret
  } 

  .method public hidebysig static void  dirtyStack() cil managed
  {
    .maxstack  3
    .locals (int32* V_0,
             int32 V_1)
    IL_0000:  ldc.i4.4
    IL_0001:  ldsfld     int32 T::size_to_test
    IL_0006:  mul
    IL_0007:  localloc
    IL_0009:  stloc.0
    IL_000a:  ldc.i4.0
    IL_000b:  stloc.1
    IL_000c:  br.s       IL_0019

    IL_000e:  ldloc.0
    IL_000f:  ldc.i4.4
    IL_0010:  ldloc.1
    IL_0011:  mul
    IL_0012:  add
    IL_0013:  ldloc.1
    IL_0014:  stind.i4
    IL_0015:  ldloc.1
    IL_0016:  ldc.i4.1
    IL_0017:  add
    IL_0018:  stloc.1
    IL_0019:  ldloc.1
    IL_001a:  ldsfld     int32 T::size_to_test
    IL_001f:  blt.s      IL_000e

    IL_0021:  ret
  } 

  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldc.i4     0x7d0
    IL_0005:  stsfld     int32 T::size_to_test
    IL_000a:  ret
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
