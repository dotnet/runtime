// ProcessName - 'ILGEN'
.assembly extern mscorlib{}
.assembly DevDiv_377155{}
.class C extends [mscorlib]System.Object
{
   .method static float32 M() cil managed noinlining
   {
      .maxstack  65535
      .locals init (float64, native unsigned int, native int, char, unsigned int64, bool)

      ldloc.s 0x04
      conv.ovf.u8.un
      conv.ovf.u2.un
      ldc.r8 float64(0x9eda41f595fa8ca8)
      conv.r4
      conv.u8
      pop
      ldc.i4 0x1de50978
      ldloc 0x0004
      ldloc.s 0x04
      or
      ldc.i8 0x2d9c5178675b7af5
      ldloc 0x0004
      ldloc 0x0004
      mul
      ldloc.s 0x04
      conv.ovf.u2.un
      shr.un
      not
      not
      or
      cgt.un
      ldc.i4 0xbdc937ef
      ldloc.s 0x05
      add.ovf.un
      clt.un
      shl
      not
      neg
      shr.un
      conv.r4
      ckfinite
      ldloc.s 0x00
      clt.un
      nop
      brtrue IL_0052
      nop
IL_0052:
      ldloc 0x0000
      ret
   }

   .method static int32 Main() cil managed
   {
       .entrypoint

       .try
       {
           call float32 C::M()
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
