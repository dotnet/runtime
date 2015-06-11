// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
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
