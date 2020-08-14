// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0xbbdad33b {}
.class ILGEN_0xbbdad33b {
.method static int32 Main() {
.entrypoint
.maxstack	5
.try {
   ldc.i4 0x760be285
  conv.ovf.i1
  ldc.i4.0
ldc.i4 0x760be285
 sub.ovf.un
 bgt.un Branch_0x2
  ldc.i4 1
 br Branch_0x3
Branch_0x2:
 ldc.i4.m1
Branch_0x3:
 pop
leave END
} catch [mscorlib]System.OverflowException {
pop
leave END
}
END:
ldc.i4 100
ret

}
}
