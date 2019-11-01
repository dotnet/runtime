// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 2:0:3600:0
}
.assembly template
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 )
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}

.class public auto ansi beforefieldinit Test
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32  Main() cil managed noinlining
  {
    .entrypoint
    // Code size       143 (0x8f)
    .maxstack  10
    .locals init (int32 V_0,
             int32[] V_1,
             int32[] V_2,
             int32 V_3,
             bool V_4,
             int32 V_5,
             bool V_6)
    IL_0000:  ldc.i4.s   100
    IL_0002:  stloc.0
    IL_0003:  ldloc.0
    IL_0004:  newarr     [mscorlib]System.Int32
    IL_0009:  stloc.1
    IL_000a:  ldloc.0
    IL_000b:  newarr     [mscorlib]System.Int32
    IL_0010:  stloc.2
    IL_0011:  ldc.i4.0
    IL_0012:  stloc.3
    IL_0013:  br.s       IL_0022

    IL_0015:  ldloc.1
    IL_0016:  ldloc.3
    IL_0017:  ldloc.3
    IL_0018:  stelem.i4
    IL_0019:  ldloc.2
    IL_001a:  ldloc.3
    IL_001b:  ldc.i4.0
    IL_001c:  stelem.i4
    IL_001d:  nop
    IL_001e:  ldloc.3
    IL_001f:  ldc.i4.1
    IL_0020:  add
    IL_0021:  stloc.3
    IL_0022:  ldloc.3
    IL_0023:  ldloc.0
    IL_0024:  clt
    IL_0026:  stloc.s    V_6
    IL_0028:  ldloc.s    V_6
    IL_002a:  brtrue.s   IL_0015

    IL_002c:  ldloc.1
    IL_002d:  ldc.i4.0
    IL_002e:  ldloc.2
    IL_002f:  ldc.i4.0
    IL_0030:  ldloc.0
    IL_0031:  ldftn       void [mscorlib]System.Array::Copy(class [mscorlib]System.Array,
                                                           int32,
                                                           class [mscorlib]System.Array,
                                                           int32,
                                                           int32)
    IL_0036:  calli	void(class [mscorlib]System.Array,
                                                           int32,
                                                           class [mscorlib]System.Array,
                                                           int32,
                                                           int32)
    IL_0037:  ldc.i4.1
    IL_0038:  stloc.s    V_4
    IL_003a:  ldc.i4.0
    IL_003b:  stloc.3
    IL_003c:  br.s       IL_0056

    IL_003e:  ldloc.1
    IL_003f:  ldloc.3
    IL_0040:  ldelem.i4
    IL_0041:  ldloc.2
    IL_0042:  ldloc.3
    IL_0043:  ldelem.i4
    IL_0044:  ceq
    IL_0046:  stloc.s    V_6
    IL_0048:  ldloc.s    V_6
    IL_004a:  brtrue.s   IL_0051

    IL_004c:  ldc.i4.0
    IL_004d:  stloc.s    V_4
    IL_004f:  br.s       IL_0060

    IL_0051:  nop
    IL_0052:  ldloc.3
    IL_0053:  ldc.i4.1
    IL_0054:  add
    IL_0055:  stloc.3
    IL_0056:  ldloc.3
    IL_0057:  ldloc.0
    IL_0058:  clt
    IL_005a:  stloc.s    V_6
    IL_005c:  ldloc.s    V_6
    IL_005e:  brtrue.s   IL_003e

    IL_0060:  ldloc.s    V_4
    IL_0062:  ldc.i4.0
    IL_0063:  ceq
    IL_0065:  stloc.s    V_6
    IL_0067:  ldloc.s    V_6
    IL_0069:  brtrue.s   IL_007c

    IL_006b:  ldstr      "Passed"
    IL_0070:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0075:  nop
    IL_0076:  ldc.i4.s   100
    IL_0078:  stloc.s    V_5
    IL_007a:  br.s       IL_008c

    IL_007c:  ldstr      "Failed"
    IL_0081:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0086:  nop
    IL_0087:  ldc.i4.1
    IL_0088:  stloc.s    V_5
    IL_008a:  br.s       IL_008c

    IL_008c:  ldloc.s    V_5
    IL_008e:  ret
  } // end of method Test::Main

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Test::.ctor

} // end of class Test
