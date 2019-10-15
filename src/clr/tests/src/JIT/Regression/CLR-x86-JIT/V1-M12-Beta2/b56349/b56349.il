// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0xd62c9aaa {}
.class ILGEN_0xd62c9aaa {

.field static	int64[] field_0x0
.field static	int32 field_0x1
.field static	unsigned int64[] field_0x2
.method static int32 Main() {
.entrypoint
.maxstack	13

 .locals (int64[] local_0x3)
 .try {
 ldc.i4 255
 newarr [mscorlib]System.Int64 
 stloc local_0x3

 ldc.i4 255
 newarr [mscorlib]System.Int64 
 stsfld int64[] ILGEN_0xd62c9aaa::field_0x0

 ldc.i4.6
 conv.u8
 ldc.i4.3
 conv.ovf.u8.un
 xor
 conv.i1
Start_Orphan_e:
 ldsfld int64[] ILGEN_0xd62c9aaa::field_0x0
 stloc local_0x3
End_Orphan_e:
 ldc.i4.4
 conv.ovf.u8
 ldc.i8 0x8814df2d98fb77ef
 conv.ovf.u4
 shl
 ldc.r8 float64(0x27f283e598b23a3d)
 conv.ovf.u
 conv.ovf.i8
 div
 conv.ovf.u.un
 div.un
 pop
 Branch_0x0:

 ldc.i4.0
 Branch_0x1:
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
