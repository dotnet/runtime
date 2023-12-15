// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern mscorlib {}
.assembly ASSEMBLY_NAME {}
.assembly extern xunit.core {}
.namespace Test_25param2c_il
{
.class private sequential ansi sealed beforefieldinit VT
       extends [mscorlib]System.ValueType
{
  .field public int32 m
} 

.class public auto ansi beforefieldinit CL
       extends [mscorlib]System.Object
{
  .field public int32 n
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(int32 a) cil managed
  {
    .maxstack  2
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ldarg.0
    IL_0007:  ldarg.1
    IL_0008:  stfld      int32 Test_25param2c_il.CL::n
    IL_000d:  ret
  } 

} 

.class public auto ansi beforefieldinit test
       extends [mscorlib]System.Object
{
  .method private hidebysig static int32 
          f1(int16 a1,
             uint16 a2,
             int32 a3,
             uint32 a4,
             int64 a5,
             uint64 a6,
             uint8 a7,
             int8 a8,
             valuetype [mscorlib]System.Decimal a9,
             int32[] a10,
             valuetype Test_25param2c_il.VT a11,
             class Test_25param2c_il.CL a12,
             int32 a13,
             int32 a14,
             int32 a15,
             int32 a16,
             int32 a17,
             int32 a18,
             int32 a19,
             int32 a20,
             int32 a21,
             int32 a22,
             int32 a23,
             int32 a24,
             int32 a25) cil managed
  {
    .maxstack  3
    .locals init ([0] int32 sum,
             [1] int32 CS$00000003$00000000)
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  add
    IL_0003:  ldarg.2
    IL_0004:  add
    IL_0005:  conv.i8
    IL_0006:  ldarg.3
    IL_0007:  conv.u8
    IL_0008:  add
    IL_0009:  ldarg.s    a5
    IL_000b:  conv.i4
    IL_000c:  conv.i8
    IL_000d:  add
    IL_000e:  ldarg.s    a6
    IL_0010:  conv.i4
    IL_0011:  conv.i8
    IL_0012:  add
    IL_0013:  ldarg.s    a7
    IL_0015:  conv.u8
    IL_0016:  add
    IL_0017:  ldarg.s    a8
    IL_0019:  conv.i8
    IL_001a:  add
    IL_001b:  ldarg.s    a9
    IL_001d:  call       int32 [mscorlib]System.Decimal::op_Explicit(valuetype [mscorlib]System.Decimal)
    IL_0022:  conv.i8
    IL_0023:  add
    IL_0024:  ldarg.s    a10
    IL_0026:  ldc.i4.0
    IL_0027:  ldelem.i4
    IL_0028:  conv.i8
    IL_0029:  add
    IL_002a:  ldarga.s   a11
    IL_002c:  ldfld      int32 Test_25param2c_il.VT::m
    IL_0031:  conv.i8
    IL_0032:  add
    IL_0033:  ldarg.s    a12
    IL_0035:  ldfld      int32 Test_25param2c_il.CL::n
    IL_003a:  conv.i8
    IL_003b:  add
    IL_003c:  ldarg.s    a13
    IL_003e:  conv.i8
    IL_003f:  add
    IL_0040:  ldarg.s    a14
    IL_0042:  conv.i8
    IL_0043:  add
    IL_0044:  ldarg.s    a15
    IL_0046:  conv.i8
    IL_0047:  add
    IL_0048:  ldarg.s    a16
    IL_004a:  conv.i8
    IL_004b:  add
    IL_004c:  ldarg.s    a17
    IL_004e:  conv.i8
    IL_004f:  add
    IL_0050:  ldarg.s    a18
    IL_0052:  conv.i8
    IL_0053:  add
    IL_0054:  ldarg.s    a19
    IL_0056:  conv.i8
    IL_0057:  add
    IL_0058:  ldarg.s    a20
    IL_005a:  conv.i8
    IL_005b:  add
    IL_005c:  ldarg.s    a21
    IL_005e:  conv.i8
    IL_005f:  add
    IL_0060:  ldarg.s    a22
    IL_0062:  conv.i8
    IL_0063:  add
    IL_0064:  ldarg.s    a23
    IL_0066:  conv.i8
    IL_0067:  add
    IL_0068:  ldarg.s    a24
    IL_006a:  conv.i8
    IL_006b:  add
    IL_006c:  ldarg.s    a25
    IL_006e:  conv.i8
    IL_006f:  add
    IL_0070:  conv.i4
    IL_0071:  stloc.0
    IL_0072:  ldstr      "The sum is {0}"
    IL_0077:  ldloc.0
    IL_0078:  box        [mscorlib]System.Int32
    IL_007d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_0082:  ldloc.0
    IL_0083:  stloc.1
    IL_0084:  br.s       IL_0086

    IL_0086:  ldloc.1
    IL_0087:  ret
  } 

  .method private hidebysig static int32 
          f(int16 a1,
            uint16 a2,
            int32 a3,
            uint32 a4,
            int64 a5,
            uint64 a6,
            uint8 a7,
            int8 a8,
            valuetype [mscorlib]System.Decimal a9,
            int32[] a10,
            valuetype Test_25param2c_il.VT a11,
            class Test_25param2c_il.CL a12,
            int32 a13,
            int32 a14,
            int32 a15,
            int32 a16,
            int32 a17,
            int32 a18,
            int32 a19,
            int32 a20,
            int32 a21,
            int32 a22,
            int32 a23,
            int32 a24,
            int32 a25) cil managed
  {
    .maxstack  25
    .locals init ([0] int32 sum,
             [1] int32 CS$00000003$00000000)
    IL_0000:  ldarg.0
    IL_0001:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_0006:  ldarg.1
    IL_0007:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_000c:  ldarg.2
    IL_000d:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_0012:  ldarg.3
    IL_0013:  call       void [System.Console]System.Console::WriteLine(uint32)
    IL_0018:  ldarg.s    a5
    IL_001a:  call       void [System.Console]System.Console::WriteLine(int64)
    IL_001f:  ldarg.s    a6
    IL_0021:  call       void [System.Console]System.Console::WriteLine(uint64)
    IL_0026:  ldarg.s    a7
    IL_0028:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_002d:  ldarg.s    a8
    IL_002f:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_0034:  ldarg.s    a9
    IL_0036:  call       void [System.Console]System.Console::WriteLine(valuetype [mscorlib]System.Decimal)
    IL_003b:  ldarg.s    a10
    IL_003d:  ldc.i4.0
    IL_003e:  ldelem.i4
    IL_003f:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_0044:  ldarga.s   a11
    IL_0046:  ldfld      int32 Test_25param2c_il.VT::m
    IL_004b:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_0050:  ldarg.s    a12
    IL_0052:  ldfld      int32 Test_25param2c_il.CL::n
    IL_0057:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_005c:  ldarg.s    a13
    IL_005e:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_0063:  ldarg.s    a14
    IL_0065:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_006a:  ldarg.s    a15
    IL_006c:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_0071:  ldarg.s    a16
    IL_0073:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_0078:  ldarg.s    a17
    IL_007a:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_007f:  ldarg.s    a18
    IL_0081:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_0086:  ldarg.s    a19
    IL_0088:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_008d:  ldarg.s    a20
    IL_008f:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_0094:  ldarg.s    a21
    IL_0096:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_009b:  ldarg.s    a22
    IL_009d:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_00a2:  ldarg.s    a23
    IL_00a4:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_00a9:  ldarg.s    a24
    IL_00ab:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_00b0:  ldarg.s    a25
    IL_00b2:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_00b7:  ldarg.0
    IL_00b8:  ldarg.1
    IL_00b9:  ldarg.2
    IL_00ba:  ldarg.3
    IL_00bb:  ldarg.s    a5
    IL_00bd:  ldarg.s    a6
    IL_00bf:  ldarg.s    a7
    IL_00c1:  ldarg.s    a8
    IL_00c3:  ldarg.s    a9
    IL_00c5:  ldarg.s    a10
    IL_00c7:  ldarg.s    a11
    IL_00c9:  ldarg.s    a12
    IL_00cb:  ldarg.s    a13
    IL_00cd:  ldarg.s    a14
    IL_00cf:  ldarg.s    a15
    IL_00d1:  ldarg.s    a16
    IL_00d3:  ldarg.s    a17
    IL_00d5:  ldarg.s    a18
    IL_00d7:  ldarg.s    a19
    IL_00d9:  ldarg.s    a20
    IL_00db:  ldarg.s    a21
    IL_00dd:  ldarg.s    a22
    IL_00df:  ldarg.s    a23
    IL_00e1:  ldarg.s    a24
    IL_00e3:  ldarg.s    a25
    tail.  call       int32 Test_25param2c_il.test::f1(int16,
                                        uint16,
                                        int32,
                                        uint32,
                                        int64,
                                        uint64,
                                        uint8,
                                        int8,
                                        valuetype [mscorlib]System.Decimal,
                                        int32[],
                                        valuetype Test_25param2c_il.VT,
                                        class Test_25param2c_il.CL,
                                        int32,
                                        int32,
                                        int32,
                                        int32,
                                        int32,
                                        int32,
                                        int32,
                                        int32,
                                        int32,
                                        int32,
                                        int32,
                                        int32,
                                        int32)
    IL_00f0:  ret
  } 

  .method public hidebysig static int32 
          Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  25
    .locals init ([0] valuetype Test_25param2c_il.VT vt,
             [1] class Test_25param2c_il.CL cl,
             [2] int32 sum,
             [3] int32 CS$00000003$00000000,
             [4] int32[] CS$00000002$00000001)
ldstr "Testing method of 25 parameters, mixed data type, tail.call"
call void [System.Console]System.Console::WriteLine(string)
    IL_0000:  ldloca.s   vt
    IL_0002:  initobj    Test_25param2c_il.VT
    IL_0008:  ldloca.s   vt
    IL_000a:  ldc.i4.s   11
    IL_000c:  stfld      int32 Test_25param2c_il.VT::m
    IL_0011:  ldc.i4.s   12
    IL_0013:  newobj     instance void Test_25param2c_il.CL::.ctor(int32)
    IL_0018:  stloc.1
    IL_0019:  ldc.i4.1
    IL_001a:  ldc.i4.2
    IL_001b:  ldc.i4.3
    IL_001c:  ldc.i4.4
    IL_001d:  ldc.i4.5
    IL_001e:  conv.i8
    IL_001f:  ldc.i4.6
    IL_0020:  conv.i8
    IL_0021:  ldc.i4.7
    IL_0022:  ldc.i4.8
    IL_0023:  ldc.i4.s   9
    IL_0025:  newobj     instance void [mscorlib]System.Decimal::.ctor(int32)
    IL_002a:  ldc.i4.1
    IL_002b:  newarr     [mscorlib]System.Int32
    IL_0030:  stloc.s    CS$00000002$00000001
    IL_0032:  ldloc.s    CS$00000002$00000001
    IL_0034:  ldc.i4.0
    IL_0035:  ldc.i4.s   10
    IL_0037:  stelem.i4
    IL_0038:  ldloc.s    CS$00000002$00000001
    IL_003a:  ldloc.0
    IL_003b:  ldloc.1
    IL_003c:  ldc.i4.s   13
    IL_003e:  ldc.i4.s   14
    IL_0040:  ldc.i4.s   15
    IL_0042:  ldc.i4.s   16
    IL_0044:  ldc.i4.s   17
    IL_0046:  ldc.i4.s   18
    IL_0048:  ldc.i4.s   19
    IL_004a:  ldc.i4.s   20
    IL_004c:  ldc.i4.s   21
    IL_004e:  ldc.i4.s   22
    IL_0050:  ldc.i4.s   23
    IL_0052:  ldc.i4.s   24
    IL_0054:  ldc.i4.s   25
    IL_0056:  call       int32 Test_25param2c_il.test::f(int16,
                                       uint16,
                                       int32,
                                       uint32,
                                       int64,
                                       uint64,
                                       uint8,
                                       int8,
                                       valuetype [mscorlib]System.Decimal,
                                       int32[],
                                       valuetype Test_25param2c_il.VT,
                                       class Test_25param2c_il.CL,
                                       int32,
                                       int32,
                                       int32,
                                       int32,
                                       int32,
                                       int32,
                                       int32,
                                       int32,
                                       int32,
                                       int32,
                                       int32,
                                       int32,
                                       int32)
    IL_005b:  stloc.2
    IL_005c:  ldloc.2
    IL_005d:  ldc.i4     0x145
    IL_0062:  bne.un.s   IL_0069

    IL_0064:  ldc.i4.s   100
    IL_0066:  stloc.3
    IL_0067:  br.s       IL_006d

    IL_0069:  ldc.i4.1
    IL_006a:  stloc.3
    IL_006b:  br.s       IL_006d

    IL_006d:  ldloc.3
    IL_006e:  ret
  } 

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } 
}
}
