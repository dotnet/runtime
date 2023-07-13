// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}
.assembly extern mscorlib
{
}
.assembly 'b102637'
{
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.imagebase 0x00400000
.subsystem 0x00000003
.file alignment 4096
.corflags 0x0000001


.class public explicit ansi sealed $ArrayType$2$6
       extends [mscorlib]System.ValueType
{
  .pack 1
  .size 8
} 



.data D_00008030 = bytearray (
                 00 00 00 00 01 00 00 00) 
.data D_00008038 = bytearray (
                 0A 00 00 00 0B 00 00 00) 
.data D_00008040 = bytearray (
                 02 00 00 06) 
.data D_00008D58 = bytearray (
                 00 00 00 00 00 00 00 00) 


.field public static valuetype $ArrayType$2$6 g1 at D_00008030
.field public static valuetype $ArrayType$2$6 g2 at D_00008038
.field public static valuetype $ArrayType$2$6 g3 at D_00008D58
.method public static int32** modopt([mscorlib]System.Runtime.CompilerServices.CallConvCdecl) 
        ModPP(int32** A_0,
              int32* A_1,
              int32* A_2) cil managed
{
  .maxstack  3
  .locals (int32* V_0,
           int32* V_1,
           int32* V_2,
           int32 V_3,
           int32* V_4,
           int32 V_5)
  IL_0000:  ldarg.1
  IL_0001:  stloc.1
  IL_0002:  ldarg.2
  IL_0003:  stloc.0
  IL_0004:  ldarg.0
  IL_0005:  ldind.i
  IL_0006:  stloc.2
  IL_0007:  ldc.i4.0
  IL_0008:  stloc.s    V_5
  IL_000a:  ldc.i4.1
  IL_000b:  ldloc.s    V_5
  IL_000d:  ble.s      IL_0032

  IL_000f:  ldloc.0
  IL_0010:  dup
  IL_0011:  ldc.i4.4
  IL_0012:  add
  IL_0013:  stloc.0
  IL_0014:  ldind.i4
  IL_0015:  stloc.3
  IL_0016:  ldloc.1
  IL_0017:  stloc.s    V_4
  IL_0019:  ldloc.s    V_4
  IL_001b:  ldc.i4.4
  IL_001c:  add
  IL_001d:  stloc.1
  IL_001e:  ldloc.2
  IL_001f:  ldloc.s    V_4
  IL_0021:  ldind.i4
  IL_0022:  ldc.i4.4
  IL_0023:  mul
  IL_0024:  add
  IL_0025:  ldloc.3
  IL_0026:  stind.i4
  IL_0027:  ldloc.s    V_5
  IL_0029:  ldc.i4.1
  IL_002a:  add
  IL_002b:  stloc.s    V_5
  IL_002d:  ldc.i4.1
  IL_002e:  ldloc.s    V_5
  IL_0030:  bgt.s      IL_000f

  IL_0032:  ldarg.0
  IL_0033:  ret
} 

.method public static int32 modopt([mscorlib]System.Runtime.CompilerServices.CallConvCdecl) 
        main() cil managed
{
  .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
      01 00 00 00
  )
  .entrypoint
  .maxstack  3
  .locals (int32* V_0)
  IL_0000:  ldsflda    valuetype $ArrayType$2$6 g3
  IL_0005:  stloc.0
  IL_0006:  ldloca.s   V_0
  IL_0008:  ldsflda    valuetype $ArrayType$2$6 g1
  IL_000d:  ldsflda    valuetype $ArrayType$2$6 g2
  IL_0012:  call       int32** modopt([mscorlib]System.Runtime.CompilerServices.CallConvCdecl) ModPP(int32**,
                                                                                                     int32*,
                                                                                                     int32*)
  IL_0017:  pop

            ldstr      "Pass"
            call       void [System.Console]System.Console::WriteLine(string)

  IL_0018:  ldc.i4 100
  IL_0019:  ret
} 
