// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}

.assembly conv_ovf{}

.class public conv_ovf {

.method public static int32 i4_i1(int32) {
.maxstack	5
try_start:
	ldarg	0
	conv.ovf.i1
	pop
	leave	try_end
try_end:
	ldc.i4	0x0
	ret
handler_start:
	pop
	leave	done
handler_end:
done:
	ldc.i4	0x1
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler handler_start to handler_end
}
.method public static int32 u4_u1(int32) {
.maxstack	5
try_start:
	ldarg	0
	conv.ovf.u1
	pop
	leave	try_end
try_end:
	ldc.i4	0x0
	ret
handler_start:
	pop
	leave	done
handler_end:
done:
	ldc.i4	0x1
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler handler_start to handler_end
}
.method public static int32 i4_i2(int32) {
.maxstack	5
try_start:
	ldarg	0
	conv.ovf.i2
	pop
	leave	try_end
try_end:
	ldc.i4	0x0
	ret
handler_start:
	pop
	leave	done
handler_end:
done:
	ldc.i4	0x1
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler handler_start to handler_end
}
.method public static int32 u4_u2(int32) {
.maxstack	5
try_start:
	ldarg	0
	conv.ovf.u2
	pop
	leave	try_end
try_end:
	ldc.i4	0x0
	ret
handler_start:
	pop
	leave	done
handler_end:
done:
	ldc.i4	0x1
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler handler_start to handler_end
}
.method public static int32 i8_i4(int64) {
.maxstack	5
try_start:
	ldarg	0
	conv.ovf.i4
	pop
	leave	try_end
try_end:
	ldc.i4	0x0
	ret
handler_start:
	pop
	leave	done
handler_end:
done:
	ldc.i4	0x1
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler handler_start to handler_end
}
.method public static int32 u8_u4(int64) {
.maxstack	5
try_start:
	ldarg	0
	conv.ovf.u4
	pop
	leave	try_end
try_end:
	ldc.i4	0x0
	ret
handler_start:
	pop
	leave	done
handler_end:
done:
	ldc.i4	0x1
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler handler_start to handler_end
}
.method public static int32 i4_u4(int32) {
.maxstack	5
try_start:
	ldarg	0
	conv.ovf.u4
	pop
	leave	try_end
try_end:
	ldc.i4	0x0
	ret
handler_start:
	pop
	leave	done
handler_end:
done:
	ldc.i4	0x1
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler handler_start to handler_end
}
.method public static int32 u4_i4(int32) {
.maxstack	5
try_start:
	ldarg	0
	conv.ovf.i4
	pop
	leave	try_end
try_end:
	ldc.i4	0x0
	ret
handler_start:
	pop
	leave	done
handler_end:
done:
	ldc.i4	0x1
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler handler_start to handler_end
}
.method public static int32 u8_i8(int64) {
.maxstack	5
try_start:
	ldarg	0
	conv.ovf.i8
	pop
	leave	try_end
try_end:
	ldc.i4	0x0
	ret
handler_start:
	pop
	leave	done
handler_end:
done:
	ldc.i4	0x1
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler handler_start to handler_end
}
.method public static int32 i8_u8(int64) {
.maxstack	5
try_start:
	ldarg	0
	conv.ovf.u8
	pop
	leave	try_end
try_end:
	ldc.i4	0x0
	ret
handler_start:
	pop
	leave	done
handler_end:
done:
	ldc.i4	0x1
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler handler_start to handler_end
}
.method public static int32 r8_i4(float64) {
.maxstack	5
try_start:
	ldarg	0
	conv.ovf.i4
	pop
	leave	try_end
try_end:
	ldc.i4	0x0
	ret
handler_start:
	pop
	leave	done
handler_end:
done:
	ldc.i4	0x1
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler handler_start to handler_end
}
.method public static int32 r8_i8(float64) {
.maxstack	5
try_start:
	ldarg	0
	conv.ovf.i8
	pop
	leave	try_end
try_end:
	ldc.i4	0x0
	ret
handler_start:
	pop
	leave	done
handler_end:
done:
	ldc.i4	0x1
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler handler_start to handler_end
}
.method public static int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack	10

	ldc.i4		0x00000100
	call	int32 conv_ovf::i4_i1(int32)
	brfalse		FAIL
	ldc.i4		0x00000100
	call	int32 conv_ovf::u4_u1(int32)
	brfalse		FAIL
	ldc.i4		0x00010000
	call	int32 conv_ovf::i4_i2(int32)
	brfalse		FAIL
	ldc.i4		0x00010000
	call	int32 conv_ovf::u4_u2(int32)
	brfalse		FAIL
	ldc.i8		0x7FFFFFFFFFFFFFFF
	call	int32 conv_ovf::i8_i4(int64)
	brfalse		FAIL
	ldc.i8		0xFFFFFFFFFFFFFFFF
	call	int32 conv_ovf::u8_u4(int64)
	brfalse		FAIL
	ldc.i4		0x80000000
	call	int32 conv_ovf::i4_u4(int32)
	brfalse		FAIL
	ldc.i8		0x8000000000000000
	call	int32 conv_ovf::i8_u8(int64)
	brfalse		FAIL
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	call	int32 conv_ovf::r8_i4(float64)
	brfalse		FAIL
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	call	int32 conv_ovf::r8_i8(float64)
	brfalse		FAIL

PASS:
	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
