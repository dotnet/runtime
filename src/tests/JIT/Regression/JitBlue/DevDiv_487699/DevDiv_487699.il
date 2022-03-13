// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib {}
.assembly 'DevDiv_487699' {}
.assembly extern xunit.core {}

// This test originally triggered an error, that we do not reload shift index, if it was spilled to rcx.

.class ILGEN_CLASS
{
   .method static native int ILGEN_METHOD(unsigned int8, int64, native int, unsigned int16)
   {
        .maxstack  65535
        .locals init (int16, native unsigned int, int16)
        IL_0000: ldc.r8 float64(0xde9d0a0c75a00f8b)
        IL_0009: dup
        IL_000a: ckfinite
        IL_000b: clt.un
        IL_000d: ldarg 0x0000
        IL_0011: ldloc 0x0001
        IL_0015: shr.un
        IL_0016: conv.ovf.u.un
        IL_0017: ldloc.s 0x02
        IL_0019: shr.un
        IL_001a: not
        IL_001b: ldc.i4 0xfd0085da
        IL_0020: shr.un
        IL_0021: shl
        IL_0022: stloc.s 0x02
        IL_0024: ldc.r8 float64(0xbdd1f70e32c59bfe)
        IL_002d: conv.i
        IL_002e: ldc.r8 float64(0xc15125191104f4ea)
        IL_0037: conv.ovf.i
        IL_0038: div.un
        IL_0039: neg
        IL_003a: conv.u8
        IL_003b: conv.ovf.u1.un
        IL_003c: ldc.r8 float64(0xcfb61a530905ecef)
        IL_0045: neg
        IL_0046: conv.r4
        IL_0047: pop
        IL_0048: nop
        IL_0049: ldarg.s 0x00
        IL_004b: ldc.r8 float64(0x50c925e1a03907dd)
        IL_0054: ldc.r8 float64(0x74bc2b522b36723f)
        IL_005d: add
        IL_005e: ldc.r8 float64(0xba0092cde6096068)
        IL_0067: ldc.r8 float64(0xa1eb85bc740ae7fc)
        IL_0070: div
        IL_0071: ldc.r8 float64(0xc75125191104f4ea)
        IL_007a: rem
        IL_007b: mul
        IL_007c: ldc.i4 0x1b037daa
        IL_0081: ldarg 0x0002
        // Bad shift.
        IL_0085: shr.un 
        IL_0086: ldarg.s 0x03
        IL_0088: ldarg.s 0x00
        IL_008a: div.un
        IL_008b: ldc.r8 float64(0x5e96249027b7812a)
        IL_0094: conv.i
        IL_0095: rem.un
        IL_0096: shr.un
        IL_0097: conv.r4
        IL_0098: cgt.un
        IL_009a: conv.ovf.i2.un
        IL_009b: xor
        IL_009c: ldc.r8 float64(0x988d5a78f82eb3cc)
        IL_00a5: conv.ovf.i.un
        IL_00a6: cgt.un
        IL_00a8: bgt 
        IL_00b1
        IL_00ad: ldarg.s 0x01
        IL_00af: conv.ovf.u8
        IL_00b0: pop
        IL_00b1: ldloc.s 0x01
        IL_00b3: dup
        IL_00b4: starg.s 0x00
        IL_00b6: ldloc.s 0x00
        IL_00b8: ldc.r8 float64(0x800e3377f99a412a)
        IL_00c1: ldc.r8 float64(0x759b00dcc8a5c6b2)
        IL_00ca: clt.un
        IL_00cc: shl
        IL_00cd: neg
        IL_00ce: ldloc 0x0001
        IL_00d2: not
        IL_00d3: not
        IL_00d4: neg
        IL_00d5: not
        IL_00d6: ldc.r8 float64(0x530f7ba77c21072d)
        IL_00df: ldc.r8 float64(0x2a45ad526d01f262)
        IL_00e8: ceq
        IL_00ea: neg
        IL_00eb: shr
        IL_00ec: neg
        IL_00ed: ldarg.s 0x02
        IL_00ef: xor
        IL_00f0: conv.ovf.u8.un
        IL_00f1: ldarg 0x0001
        IL_00f5: conv.ovf.u8.un
        IL_00f6: clt
        IL_00f8: pop
        IL_00f9: ceq
        IL_00fb: ret   
    }
    
      .method private static int32 Main()
    {
        .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
            01 00 00 00
        )
        .entrypoint
        .maxstack  10
                
        ldc.i4 3
        conv.i1
        ldc.i8 2        
        ldc.i4 3
        conv.i
        ldc.i4 2        
        conv.ovf.u2

        call native int ILGEN_CLASS::ILGEN_METHOD(unsigned int8, int64, native int, unsigned int16)
        pop

        ldc.i4 100
        ret
    }
}
