// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.




.assembly extern legacy library mscorlib
{
}
.assembly legacy library cs_test_01
{


  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module cs_test_01.exe
.custom instance void [mscorlib]System.Security.UnverifiableCodeAttribute::.ctor() = ( 01 00 00 00 ) 
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       
.corflags 0x00000001    



.class public sequential ansi sealed beforefieldinit big
       extends [mscorlib]System.ValueType
{
  .pack 1
  .size 20
  .field public int32 dummy
} 

.class private auto ansi beforefieldinit Test
       extends [mscorlib]System.Object
{
  .method private hidebysig static int32 
          Main() cil managed
  {
    .entrypoint
    .maxstack  3
    .locals (valuetype big V_0,
             int32* V_1,
             int32 V_2,
             int32 V_3,
             int32* V_4)
    IL_0000:  ldloca.s   V_0
    IL_0002:  initobj    big
    IL_0008:  sizeof     big
    IL_000e:  ldc.i4.s   20
    IL_0010:  beq.s      IL_0014

    IL_0012:  ldc.i4.1
    IL_0013:  ret

    IL_0014:  ldloca.s   V_0
    IL_0016:  ldflda     int32 big::dummy
    IL_001b:  stloc.1
    IL_001c:  ldc.i4.0
    IL_001d:  stloc.2
    IL_001e:  br.s       IL_002b

    IL_0020:  ldloc.1
    IL_0021:  ldloc.2
    IL_0022:  stind.i4
    IL_0023:  ldloc.1
    IL_0024:  ldc.i4.4
    IL_0025:  add
    IL_0026:  stloc.1
    IL_0027:  ldloc.2
    IL_0028:  ldc.i4.1
    IL_0029:  add
    IL_002a:  stloc.2
    IL_002b:  ldloc.2
    IL_002c:  ldc.i4.5
    IL_002d:  blt.s      IL_0020

    IL_002f:  ldc.i4.0
    IL_0030:  stloc.3
    IL_0031:  ldloca.s   V_0
    IL_0033:  ldflda     int32 big::dummy
    IL_0038:  stloc.s    V_4
    IL_003a:  br.s       IL_004e

    IL_003c:  ldloc.3
    IL_003d:  dup
    IL_003e:  ldc.i4.1
    IL_003f:  add
    IL_0040:  stloc.3
    IL_0041:  ldloc.s    V_4
    IL_0043:  ldind.i4
    IL_0044:  beq.s      IL_0048

    IL_0046:  ldc.i4.1
    IL_0047:  ret

    IL_0048:  ldloc.s    V_4
    IL_004a:  ldc.i4.4
    IL_004b:  add
    IL_004c:  stloc.s    V_4
    IL_004e:  ldloc.s    V_4
    IL_0050:  ldloca.s   V_0
    IL_0052:  ldflda     int32 big::dummy
    IL_0057:  sizeof     big
    IL_005d:  add
    IL_005e:  blt.un.s   IL_003c

    IL_0060:  ldc.i4 100
    IL_0061:  ret
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
