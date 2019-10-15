// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}

.assembly mul_ovf.exe{}

.class public mul_ovf {

.method public static	int32 i1(int32,int32) {
.maxstack	5
try_start:
	ldarg	0
	ldarg	1
	mul.ovf
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
.method public static	int32 u1(int32,int32) {
.maxstack	5
try_start:
	ldarg	0
	ldarg	1
	mul.ovf.un
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
.method public static	int32 i2(int32,int32) {
.maxstack	5
try_start:
	ldarg	0
	ldarg	1
	mul.ovf
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
.method public static	int32 u2(int32,int32) {
.maxstack	5
try_start:
	ldarg	0
	ldarg	1
	mul.ovf.un
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
.method public static	int32 i4(int32,int32) {
.maxstack	5
try_start:
	ldarg	0
	ldarg	1
	mul.ovf
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
.method public static	int32 u4(int32,int32) {
.maxstack	5
try_start:
	ldarg	0
	ldarg	1
	mul.ovf.un
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
.method public static	int32 i8(int64,int64) {
.maxstack	5
try_start:
	ldarg	0
	ldarg	1
	mul.ovf
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
.method public static	int32 u8(int64,int64) {
.maxstack	5
try_start:
	ldarg	0
	ldarg	1
	mul.ovf.un
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

.method public static int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack	10
	ldc.i4		0x0000007F
	ldc.i4		0x00000002
	call	int32 mul_ovf::i1(int32,int32)
	brfalse		FAIL1			// fail if no exception was generated
	ldc.i4		0x00000080
	ldc.i4		0x00000002
	call	int32 mul_ovf::u1(int32,int32)
	brfalse		FAIL2
	ldc.i4		0x00007F00
	ldc.i4		0x00000002
	call	int32 mul_ovf::i2(int32,int32)
	brfalse		FAIL3
	ldc.i4		0x00008000
	ldc.i4		0x00000002
	call	int32 mul_ovf::u2(int32,int32)
	brfalse		FAIL4
	ldc.i4		0x80000000
	ldc.i4		0x00000002
	call	int32 mul_ovf::i4(int32,int32)
	brfalse		FAIL5
	ldc.i4		0x80000000
	ldc.i4		0x00000002
	call	int32 mul_ovf::u4(int32,int32)
	brfalse		FAIL6
	ldc.i8		0x8000000000000000
	ldc.i8		0x0000000000000002
	call	int32 mul_ovf::i8(int64,int64)
	brfalse		FAIL7
	ldc.i8		0x8000000000000000
	ldc.i8		0x0000000000000002
	call	int32 mul_ovf::u8(int64,int64)
	brfalse		FAIL8

PASS:
	ldc.i4 100
	ret
FAIL1:	
		ldc.i4	0x1
		br	FAILOUT
FAIL2:	
		ldc.i4	0x2
		br	FAILOUT
FAIL3:	
		ldc.i4	0x3
		br	FAILOUT
FAIL4:	
		ldc.i4	0x4
		br	FAILOUT
FAIL5:	
		ldc.i4	0x5
		br	FAILOUT
FAIL6:	
		ldc.i4	0x6
		br	FAILOUT
FAIL7:	
		ldc.i4	0x7
		br	FAILOUT
FAIL8:	
		ldc.i4	0x8
FAILOUT:
		ret
FAIL:
	ldc.i4	0x0
	ret
}
}
