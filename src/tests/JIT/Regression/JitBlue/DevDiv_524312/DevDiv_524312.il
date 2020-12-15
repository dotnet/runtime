
//  Microsoft (R) .NET Framework IL Disassembler.  Version 4.7.3018.0
//  Copyright (c) Microsoft Corporation.  All rights reserved.



.assembly extern mscorlib{}
.assembly ILGEN_MODULE{}

.class ILGEN_CLASS
{
  .method static int32 Main()
  {
    .entrypoint
    // Code size       19 (0x13)
    .maxstack  8
    .try
    {
        IL_0000:  nop
        IL_0001:  ldc.r8     0.0
        IL_000a:  ldc.i4.s   97
        IL_000c:  call       char ILGEN_CLASS::ILGEN_METHOD_ZERO(float64, char)
        IL_0011:  pop
        IL_0201:  ldc.r8     0.0
        IL_020a:  ldc.i4.s   97
        IL_0212:  call       char ILGEN_CLASS::ILGEN_METHOD_INF(float64, char)
        IL_0220:  pop
        IL_0320:  leave.s done
    }
    catch [mscorlib]System.Exception
    {
        leave.s done
    }
    done:
        ldc.i4 100
        ret
  } // end of method Program::Main

  .method private hidebysig static char  ILGEN_METHOD_ZERO(float64 d, char cc) cil managed
  {
    // Code size       7 (0x7)
      .maxstack  18
      .locals init (native int l0, unsigned int64 l1, unsigned int64 l2, float32 l3, float32 l4, float64 l5, int8 l6, float32 l7, bool l8, native int l9, unsigned int64 l10, unsigned int64 l11, int64 l12)
      IL_0000: ldloc 0x0006
      IL_0004: ldloc 0x0003
      IL_0008: ldc.r4 0x0 // ZERO
      IL_000a: dup
      IL_000b: ckfinite
      IL_000c: rem
      IL_000d: conv.r.un
      IL_000e: cgt.un
      IL_0010: pop
      IL_0011: ret
  } // end of method Program::ILGEN_METHOD_ZERO
  
  .method private hidebysig static char  ILGEN_METHOD_INF(float64 d, char cc) cil managed
  {
    // Code size       7 (0x7)
      .maxstack  18
      .locals init (native int l0, unsigned int64 l1, unsigned int64 l2, float32 l3, float32 l4, float64 l5, int8 l6, float32 l7, bool l8, native int l9, unsigned int64 l10, unsigned int64 l11, int64 l12)
      IL_0000: ldloc 0x0006
      IL_0004: ldloc 0x0003
      IL_0008: ldc.r4 0x7f800000 // INF
      IL_000a: dup
      IL_000b: ckfinite
      IL_000c: rem
      IL_000d: conv.r.un
      IL_000e: cgt.un
      IL_0010: pop
      IL_0011: ret
  } // end of method Program::ILGEN_METHOD_INF

} // end of class test.Program

