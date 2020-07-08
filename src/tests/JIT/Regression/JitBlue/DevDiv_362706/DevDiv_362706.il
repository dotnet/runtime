// Reading from 'C:\SuperPMI\compiler_hpp__1784____Assertion_failed__lvRefCnt.mc' dumping raw IL for MC Indexes to console
// ProcessName - 'ILGEN'
.assembly extern mscorlib{}
.assembly DevDiv_362706{}
.class C extends [mscorlib]System.Object
{
   .method static unsigned int64 M(float32, int32) cil managed noinlining
   {
      .maxstack  65535
      .locals init (unsigned int8, native unsigned int)

      ldc.i8 0x085d689753da3de1
      ldc.i8 0xe61597946032e570
      clt.un
      conv.u8
      ldc.i8 0x7982285f972837bd
      ldc.i8 0xbff060a0e0e2eeba
      sub
      ldloc 0x0001
      shl
      mul.ovf
      ldarg.s 0x00
      starg.s 0x00
      brtrue IL_005f
      ldc.i8 0xbcb846d27e2968bf
      conv.r.un
      ckfinite
      pop
      ldarg.s 0x01
      conv.ovf.i8
      ldc.i8 0xe3b63c00f2954a6c
      ldc.i8 0x3332cfb7aa00a0b0
      div.un
      clt
      stloc 0x0001

IL_005f:
      ldloc 0x0000
      ldc.i4 0x1ef1d296
      ldc.r8 float64(0x157cd4c7a3c884f1)
      ldc.r8 float64(0xa5da6ecb57585851)
      add
      ckfinite
      neg
      conv.r.un
      conv.ovf.i4.un
      cgt.un
      not
      or
      ldarg.s 0x00
      conv.r.un
      ldarg 0x0000
      ldc.i8 0x9cb75a81b3443e9c
      conv.ovf.u8.un
      conv.i4
      pop
      neg
      cgt.un
      ldloc 0x0001
      ldc.i4 0x1bf905b2
      ldloc.s 0x00
      xor
      rem
      ldloc.s 0x00
      ldc.r8 float64(0x94424127b67875d0)
      ldc.i8 0x96e1b4664d5ad509
      conv.ovf.i2
      conv.r4
      add
      conv.ovf.u2.un
      shr.un
      shr
      ldc.i8 0x1919368cc08e5eb1
      ldc.i8 0x4b6a3f418d2e0236
      ceq
      ceq
      starg.s 0x01
      bgt IL_011a
      ldarg.s 0x00
      conv.ovf.i8.un
      ldloc 0x0000
      ldc.r8 float64(0x7e83ec1e8c2e0e94)
      conv.ovf.i2
      shl
      not
      not
      conv.i8
      conv.ovf.u8
      ldc.i4 0x8a3055bd
      neg
      ldarg 0x0001
      xor
      conv.i8
      ldc.i4 0xa2d0ea18
      shr.un
      ldc.i8 0x53ac012d34eb9f44
      xor
      neg
      add
      ldarg 0x0000
      conv.ovf.u4.un
      shr
      or
      conv.r8
      pop

IL_011a:
      ldarg 0x0000
      neg
      ldarg 0x0000
      neg
      div
      ldloc.s 0x01
      ldarg 0x0001
      ldloc 0x0000
      ldloc 0x0001
      and
      shl
      mul.ovf.un
      starg.s 0x01
      ldc.i4 0xc47fec23
      conv.r8
      div
      conv.ovf.i8.un
      not
      ldc.r8 float64(0x401dee505a2add2e)
      ldc.i8 0x0dc5a76cd1306317
      conv.i1
      conv.r8
      add
      conv.r4
      conv.ovf.i1.un
      nop
      stloc.s 0x00
      nop
      ret
   }

   .method static int32 Main() cil managed
   {
       .entrypoint

       .try
       {
           ldc.r4		float32(0xFF800000)
           ldc.i4.s   25
           call unsigned int64 C::M(float32, int32)
           pop
           leave.s done
       }
       catch [mscorlib]System.Exception
       {
           pop
           leave.s done
       }

   done:
       ldc.i4 100
       ret
   }
}
// Dumped 1
