// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x2706a1c4 {}
.class ILGEN_0x2706a1c4 {
.method static void Method_0x7d1b2a40(int32 Arg_0x3) {
.maxstack	7
 ldarga Arg_0x3
 pop
 ldarg Arg_0x3
 ldc.i4 8
 mul.ovf
 pop
 ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
.try {
ldc.i4 0x70000000
call void ILGEN_0x2706a1c4::Method_0x7d1b2a40(int32 Arg_0x3)
ldc.i4 100
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