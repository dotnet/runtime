// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Runtime
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )                         // .?_....:
  .ver 4:2:1:0
}
.assembly ExplicitTailCallNoSO
{
}
.class private abstract auto ansi sealed beforefieldinit ExplicitTailCallNoSO
       extends [System.Runtime]System.Object
{
  .method private hidebysig static int32 
          Main() cil managed
  {
    .entrypoint
    // Code size       44 (0x2c)
    .maxstack  2
    .locals init (int32 V_0,
             int32 V_1,
             int32 V_2)
    IL_0000:  ldc.i4     0xf4240 // 1_000_000
    IL_0005:  ldc.i4.0
    IL_0006:  call       int32 ExplicitTailCallNoSO::CalculateSumWithTailCall(int32,
                                                                              int32)
    IL_000b:  stloc.0
    IL_000c:  ldc.i4.0
    IL_000d:  stloc.1
    IL_000e:  ldc.i4.1
    IL_000f:  stloc.2
    IL_0010:  br.s       IL_001a

    IL_0012:  ldloc.1
    IL_0013:  ldloc.2
    IL_0014:  add
    IL_0015:  stloc.1
    IL_0016:  ldloc.2
    IL_0017:  ldc.i4.1
    IL_0018:  add
    IL_0019:  stloc.2
    IL_001a:  ldloc.2
    IL_001b:  ldc.i4     0xf4240 // 1_000_000
    IL_0020:  ble.s      IL_0012

    IL_0022:  ldloc.0
    IL_0023:  ldloc.1
    IL_0024:  beq.s      IL_0029

    IL_0026:  ldc.i4.s   101
    IL_0028:  ret

    IL_0029:  ldc.i4.s   100
    IL_002b:  ret
  } // end of method ExplicitTailCallNoSO::Main

  .method private hidebysig static int32 
          CalculateSumWithTailCall(int32 n,
                                   [opt] int32 sum) cil managed
  {
    .param [2] = int32(0x00000000)
    // Code size       27 (0x1b)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.0
    IL_0002:  bgt.s      IL_0006

    IL_0004:  ldc.i4.0
    IL_0005:  ret

    IL_0006:  ldarg.1
    IL_0007:  ldarg.0
    IL_0008:  add
    IL_0009:  starg.s    sum
    IL_000b:  ldarg.0
    IL_000c:  ldc.i4.1
    IL_000d:  bne.un.s   IL_0011

    IL_000f:  ldarg.1
    IL_0010:  ret

    IL_0011:  ldarg.0
    IL_0012:  ldc.i4.1
    IL_0013:  sub
    IL_0014:  ldarg.1
    IL_0015:  tail. call int32 ExplicitTailCallNoSO::CalculateSumWithTailCall(int32,
                                                                              int32)
    IL_001a:  ret
  } // end of method ExplicitTailCallNoSO::CalculateSumWithTailCall

} // end of class ExplicitTailCallNoSO
