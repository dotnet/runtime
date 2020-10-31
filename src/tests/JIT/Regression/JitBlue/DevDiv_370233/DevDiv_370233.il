.assembly extern mscorlib{}
.assembly DevDiv_370233{}
.class C extends [mscorlib]System.Object
{
   .method static unsigned int32 M(bool, bool)
   {
      .maxstack  65535
      .locals init (unsigned int32, unsigned int64, unsigned int8, unsigned int8, native int, float64, char, unsigned int16, bool, char, bool)

      ldloc 0x0006
      ldc.i4 0x11467af6
      shr
      conv.ovf.u8
      dup
      sub
      ldloc.s 0x06
      ldloc.s 0x07
      neg
      rem.un
      conv.ovf.i8.un
      xor
      neg
      conv.ovf.u
      conv.r4
      ldloc 0x0005
      ldloc.s 0x05
      ckfinite
      ckfinite
      conv.r4
      ceq
      ldarg 0x0001
      conv.r4
      pop
      pop
      ldloc 0x0001
      ldloc 0x0001
      neg
      conv.i8
      ldloc.s 0x04
      shr
      clt.un
      conv.ovf.i1.un
      conv.i8
      not
      nop
      ldc.i8 0x16cb54ea7cfcd50d
      ldloc 0x0001
      clt
      ldloc.s 0x01
      conv.ovf.u1
      conv.ovf.i2.un
      cgt
      conv.ovf.i8
      ldc.i8 0x6be45c7b8c08f1d5
      conv.ovf.u8
      ldloc.s 0x01
      add.ovf
      not
      ldloc.s 0x03
      shr.un
      ldloc.s 0x01
      add
      not
      ldloc.s 0x03
      neg
      shr
      nop
      conv.u4
      neg
      shr.un
      ceq
      ldloc 0x0001
      conv.r4
      ldc.r8 float64(0x9b4c7410aec1e6d3)
      add
      ldloc.s 0x00
      ldarg.s 0x00
      xor
      ldloc.s 0x08
      mul
      not
      conv.r.un
      ldloc.s 0x02
      ldloc 0x0006
      add
      conv.r4
      mul
      cgt
      ldloc.s 0x05
      ldloc.s 0x05
      cgt.un
      ldarg 0x0001
      not
      shl
      div
      ldc.i8 0x5892c0a0bd150d05
      ldc.i8 0x8c33eb17e67e9772
      neg
      ceq
      not
      ldloc.s 0x00
      ldloc 0x0003
      ceq
      or
      pop
      xor
      conv.r.un
      ceq
      conv.u2
      ret
   }

   .method static int32 Main() cil managed
   {
       .entrypoint

       .try
       {
           ldc.i4     0
           ldc.i4.s   0
           call unsigned int32 C::M(bool, bool)
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
