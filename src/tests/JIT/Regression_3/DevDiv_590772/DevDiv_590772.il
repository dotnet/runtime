// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { auto }
.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern System.Console { auto }

.assembly DevDiv_590772 { }
.assembly extern xunit.core {}

.class public auto ansi beforefieldinit DevDiv_590772
       extends [System.Runtime]System.Object
{
  .method static int8 ILGEN_METHOD(int64, char, native unsigned int, unsigned int32, char, native int)
  {
    .maxstack  183
    .locals init (float64, int32, unsigned int32, native unsigned int, unsigned int32, int16, int64, native unsigned int, int32)
    IL_0000: ldarg.s 0x04
    IL_0002: pop
    IL_0003: ldloc 0x0006
    IL_0007: not
    IL_0008: ldarg.s 0x00
    IL_000a: ceq
    IL_000c: ldloc.s 0x07
    IL_000e: neg
    IL_000f: ldloc 0x0007
    IL_0013: conv.r4
    IL_0014: conv.u4
    IL_0015: ldarg 0x0001
    IL_0019: ldloc.s 0x01
    IL_001b: ldloc.s 0x03
    IL_001d: clt
    IL_001f: xor
    IL_0020: neg
    IL_0021: conv.u2
    IL_0022: ldloc.s 0x03
    IL_0024: not
    IL_0025: rem.un
    IL_0026: mul
    IL_0027: ldarg.s 0x04
    IL_0029: ldloc 0x0004
    IL_002d: clt.un
    IL_002f: dup
    IL_0030: xor
    IL_0031: dup
    IL_0032: ceq
    IL_0034: rem.un
    IL_0035: div
    IL_0036: not
    IL_0037: ldc.i4 0xb8807944
    IL_003c: ldloc.s 0x00
    IL_003e: conv.r8
    IL_003f: ldloc 0x0000
    IL_0043: cgt
    IL_0045: ldarg 0x0000
    IL_0049: ldloc.s 0x06
    IL_004b: neg
    IL_004c: clt.un
    IL_004e: add.ovf.un
    IL_004f: xor
    IL_0050: conv.ovf.u1
    IL_0051: rem.un
    IL_0052: rem.un
    IL_0053: nop
    IL_0054: ldarg 0x0000
    IL_0058: ldarg.s 0x00
    IL_005a: or
    IL_005b: conv.ovf.i8.un
    IL_005c: not
    IL_005d: ldarg.s 0x00
    IL_005f: ldarg 0x0000
    IL_0063: cgt.un
    IL_0065: not
    IL_0066: conv.ovf.u8.un
    IL_0067: ldarg 0x0003
    IL_006b: ldc.i4 0x61790215
    IL_0070: dup
    IL_0071: xor
    IL_0072: shl
    IL_0073: conv.i
    IL_0074: ldloc.s 0x08
    IL_0076: pop
    IL_0077: shl
    IL_0078: or
    IL_0079: not
    IL_007a: dup
    IL_007b: xor
    IL_007c: ldloc 0x0001
    IL_0080: conv.ovf.u2
    IL_0081: conv.i8
    IL_0082: cgt.un
    IL_0084: ceq
    IL_0086: not
    IL_0087: neg
    IL_0088: not
    IL_0089: ldloc 0x0006
    IL_008d: ldloc.s 0x05
    IL_008f: ldarg.s 0x05
    IL_0091: cgt
    IL_0093: conv.i8
    IL_0094: conv.ovf.u8.un
    IL_0095: ldarg.s 0x00
    IL_0097: ldc.i8 0x81fc8651e81eba9a
    IL_00a0: ldc.i4 0x4d3cd1ad
    IL_00a5: shr
    IL_00a6: mul.ovf.un
    IL_00a7: not
    IL_00a8: conv.ovf.u8.un
    IL_00a9: ceq
    IL_00ab: shl
    IL_00ac: conv.ovf.u8.un
    IL_00ad: pop
    IL_00ae: nop
    IL_00af: ldloc 0x0007
    IL_00b3: nop
    IL_00b4: or
    IL_00b5: neg
    IL_00b6: ret   
  }

  .method public hidebysig static int32 
          Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       34 (0x22)
    .maxstack  7
    .locals init (int32 V_0)
    IL_0000:  nop
    .try
    {
      IL_0001:  nop
      IL_0002:  ldc.i4.0
      IL_0003:  conv.i8
      IL_0004:  ldc.i4.s   49
      IL_0006:  ldc.i4.2
      IL_0007:  ldc.i4.3
      IL_0009:  ldc.i4.s   52
      IL_000b:  ldc.i4.5
      IL_000c:  call       int8 DevDiv_590772::ILGEN_METHOD(int64,
                                                            char,
                                                            native unsigned int,
                                                            uint32,
                                                            char,
                                                            native int)
      IL_0011:  pop
      IL_0012:  nop
      IL_0013:  leave.s    IL_001c

    }  // end .try
    catch [System.Runtime]System.Object 
    {
      IL_0015:  pop
      IL_0016:  nop
      IL_0017:  ldc.i4.s   100
      IL_0019:  stloc.0
      IL_001a:  leave.s    IL_0020

    }  // end handler
    IL_001c:  ldc.i4.m1
    IL_001d:  stloc.0
    IL_001e:  br.s       IL_0020

    IL_0020:  ldloc.0
    IL_0021:  ret
  } // end of method DevDiv_590772::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method DevDiv_590772::.ctor

} // end of class DevDiv_590772

