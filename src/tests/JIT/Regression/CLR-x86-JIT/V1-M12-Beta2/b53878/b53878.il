// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0xab09d49 {}
.class ILGEN_0xab09d49 {
.method static int32 Main() {
.entrypoint
.maxstack	20
.try {
 .locals (int32 local_0x1)

 ldc.i4 0x137b8cfe 
 stloc local_0x1

Start_Orphan_0:
 ldc.i4 0x137b8cfe 
brtrue  Branch_0x2
 Branch_0x2:

Start_Orphan_20:
 ldloc local_0x1
 conv.ovf.u1
 newarr [mscorlib]System.Int32
 ldc.i4.4
 ldc.i4 55
 stelem.i4
End_Orphan_20:
End_Orphan_0:
ldc.i4 55
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
