// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Optimizations that attempt to track values that are reinterpreted
// in different types require special handling.

.assembly extern legacy library mscorlib { }
.assembly extern xunit.core {}
.assembly DevDiv_216571
{
}

.class public auto ansi Test_DevDiv_216571 extends [mscorlib] System.Object {
.method public static int32  main() cil managed
{
  .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
      01 00 00 00
  )
  .entrypoint
  .maxstack  3
  .locals (float32 V_2,
           uint32 V_6)
  IL_0000:  ldc.i4     0x3f000000
  IL_0005:  stloc.s    V_6

  IL_000e:  ldloca.s   V_2
  IL_0010:  ldloca.s   V_6
  IL_0012:  ldc.i4.4
  IL_0013:  unaligned. 4
  IL_0016:  cpblk

  IL_0020:  ldloc.s    V_2              // 0.5 in FP representation
  IL_0021:  ldc.i4     0x3f000000
  IL_0022:  conv.r4                     // 1056964608 in integer representation
  IL_0023:  ceq
  IL_0024:  brtrue     IL_0031          // should not be equivalent

  IL_0027:  ldc.i4.s   100
  IL_0028:  ret

  IL_0031:  ldc.i4.s   101
  IL_0032:  ret

} // end of global method main
}
