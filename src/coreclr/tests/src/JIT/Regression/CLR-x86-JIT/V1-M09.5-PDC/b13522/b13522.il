// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
.assembly b13522
{
}
.class I2 {
 .method static int32 main() {
.entrypoint
.maxstack	11
.try {
 ldc.i8 0x7000000000000000
 ldc.i8 2
 mul.ovf
 ldc.i8 4
 mul
 conv.i4
 pop
 leave stop
}
catch [mscorlib]System.OverflowException {
 pop
 leave stop
}
stop:
 ldc.i4 100
 ret
}
}
