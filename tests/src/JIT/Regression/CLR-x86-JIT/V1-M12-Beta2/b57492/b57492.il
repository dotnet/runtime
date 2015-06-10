// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
.assembly ILGEN_0x2019eaf2 {}
.class ILGEN_0x2019eaf2 {
.field static	unsigned int64 field_0x4
.method static int32 Main() {
.entrypoint
.maxstack	20
.try {
.locals (int8 local_0x2)

 ldc.i4 0x69f07ea
 stloc local_0x2
 ldc.i8 5
 stsfld unsigned int64 ILGEN_0x2019eaf2::field_0x4
 ldc.i4.4
 ldc.i4 4
 blt  Branch_0x0
Branch_0x0:
 ldc.i8 0xe
 ldsfld unsigned int64 ILGEN_0x2019eaf2::field_0x4
 conv.i4
 shr
 ldloc local_0x2
 conv.ovf.u8
 ldc.i8 0x3
 ceq
 shr
 pop
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
