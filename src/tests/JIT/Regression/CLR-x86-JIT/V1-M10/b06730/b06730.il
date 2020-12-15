// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b06730
{
}

.class ILGEN_939524508 {
.method static int32 main(class System.String[]) {
.entrypoint
.maxstack	7
 .locals ( float32 l0, int32 l1, int32 l2, int32 l3, float32 l4, int32 l5,
 float32 l6, float32 l7, int32 l8, int64 l9, int64 l10, int64 l11, float64 l12)

//ldc.i8 5
//stloc 9

      ldloc 12
      ldloc 12
      ldloc 9
     stloc 10
    add
  conv.u1
      ldloc 2
      ldloc 2
     add
    conv.i8
      ldc.i8 0x5757a9976481e71
      ldloc 10
    and
   mul
      ldloc 9
     conv.i8
  cgt
 xor
conv.i4
pop
ldc.i4 100
ret
}
}
