// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x2806523a {}
.class ILGEN_0x2806523a {
.method static int32 Main() {
.entrypoint
.maxstack	11
.try {
.locals (native int LOCAL_0x0)
 ldc.i4 0x512adef8 
 stloc LOCAL_0x0
Start_Orphan_0:
 ldloc LOCAL_0x0
 ldc.i4 0x54a09a98
 add.ovf
 ldloc LOCAL_0x0
 brtrue Branch_0x8
 ldloc LOCAL_0x0
 br Branch_0x9
Branch_0x8:
 ldloc LOCAL_0x0
Branch_0x9:
 clt
 newarr [mscorlib]System.Double
 pop
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
