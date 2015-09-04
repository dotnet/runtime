// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b06435
{
}
.class _simple {

.method static int32 main() {
.entrypoint
.maxstack	100

.locals (int32, int64)
 ldc.i4 1
 stloc.0


try_start:
ldc.r8 float64(0x43E0000000000000)
conv.i8
ldc.i8 0x0f2146b2ec61bfe
not
add.ovf
stloc.1
leave try_end
try_end:
ldloc.1
conv.i4
ret
handler_start:
	pop
	leave	done
handler_end:
done:
	ldc.i4	100
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler handler_start to handler_end
}
}
