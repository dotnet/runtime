// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}


.assembly add_ovf.exe{}


.class public add_ovf {

.field public static	int32 temp32a
.field public static	int64 temp64a
.field public static	int32 temp32b
.field public static	int64 temp64b


    .data ptr = int32(0)

.method public static int32 i1() {
.locals(class [mscorlib]System.Exception)
.maxstack	5
try_start:
	ldc.i4	0x0000007E
	ldc.i4	0x00000001
	add.ovf
	conv.ovf.i1
	stsfld	int32 add_ovf::temp32a
	ldc.i4	0x0000007F
	ldc.i4	0x00000001
	add.ovf
	conv.ovf.i1
	stsfld	int32 add_ovf::temp32b
	leave	done
try_end:
	br		FAIL
handler_start:
	stloc	0
	leave	done
handler_end:
done:
	ldloc	0
	isinst	[mscorlib]System.OverflowException
	brfalse	FAIL
	ldc.i4	0x0000007F
	ldsfld	int32 add_ovf::temp32a
	ceq
	brfalse	FAIL
PASS:
	ldc.i4	0x00000001
	ret
FAIL:
	ldc.i4	0x00000000
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler handler_start to handler_end
}

.method public static int32 u1() {
.locals(class [mscorlib]System.Exception)
.maxstack	5
try_start:
	ldc.i4	0x000000FE
	ldc.i4	0x00000001
	add.ovf.un
	conv.ovf.u1
	stsfld	int32 add_ovf::temp32a
	ldc.i4	0x000000FF
	ldc.i4	0x00000001
	add.ovf.un
	conv.ovf.u1
	stsfld	int32 add_ovf::temp32b
	leave	done
try_end:
	br		FAIL
handler_start:
	stloc	0
	leave	done
handler_end:
done:
	ldloc	0
	isinst	[mscorlib]System.OverflowException
	brfalse	FAIL
	ldc.i4	0x000000FF
	ldsfld	int32 add_ovf::temp32a
	ceq
	brfalse	FAIL
PASS:
	ldc.i4	0x00000001
	ret
FAIL:
	ldc.i4	0x00000000
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler handler_start to handler_end
}

.method public static int32 i2() {
.locals(class [mscorlib]System.Exception)
.maxstack	5
try_start:
	ldc.i4	0x00007FFE
	ldc.i4	0x00000001
	add.ovf
	conv.ovf.i2
	stsfld	int32 add_ovf::temp32a
	ldc.i4	0x00007FFF
	ldc.i4	0x00000001
	add.ovf
	conv.ovf.i2
	stsfld	int32 add_ovf::temp32b
	leave	done
try_end:
	br		FAIL
handler_start:
	stloc	0
	leave	done
handler_end:
done:
	ldloc	0
	isinst	[mscorlib]System.OverflowException
	brfalse	FAIL
	ldc.i4	0x00007FFF
	ldsfld	int32 add_ovf::temp32a
	ceq
	brfalse	FAIL
PASS:
	ldc.i4	0x00000001
	ret
FAIL:
	ldc.i4	0x00000000
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler handler_start to handler_end
}

.method public static int32 u2() {
.locals(class [mscorlib]System.Exception)
.maxstack	5
try_start:
	ldc.i4	0x0000FFFE
	ldc.i4	0x00000001
	add.ovf.un
	conv.ovf.u2
	stsfld	int32 add_ovf::temp32a
	ldc.i4	0x0000FFFF
	ldc.i4	0x00000001
	add.ovf.un
	conv.ovf.u2
	stsfld	int32 add_ovf::temp32b
	leave	done
try_end:
	br		FAIL
handler_start:
	stloc	0
	leave	done
handler_end:
done:
	ldloc	0
	isinst	[mscorlib]System.OverflowException
	brfalse	FAIL
	ldc.i4	0x0000FFFF
	ldsfld	int32 add_ovf::temp32a
	ceq
	brfalse	FAIL
PASS:
	ldc.i4	0x00000001
	ret
FAIL:
	ldc.i4	0x00000000
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler handler_start to handler_end
}

.method public static int32 i4() {
.locals(class [mscorlib]System.Exception)
.maxstack	5
try_start:
	ldc.i4	0x7FFFFFFE
	ldc.i4	0x00000001
	add.ovf
	conv.ovf.i4
	stsfld	int32 add_ovf::temp32a
	ldc.i4	0x7FFFFFFF
	ldc.i4	0x00000001
	add.ovf
	conv.ovf.i4
	stsfld	int32 add_ovf::temp32b
	leave	done
try_end:
	br		FAIL
handler_start:
	stloc	0
	leave	done
handler_end:
done:
	ldloc	0
	isinst	[mscorlib]System.OverflowException
	brfalse	FAIL
	ldc.i4	0x7FFFFFFF
	ldsfld	int32 add_ovf::temp32a
	ceq
	brfalse	FAIL
PASS:
	ldc.i4	0x00000001
	ret
FAIL:
	ldc.i4	0x00000000
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler handler_start to handler_end
}

.method public static int32 u4() {
.locals(class [mscorlib]System.Exception)
.maxstack	5
try_start:
	ldc.i4	0xFFFFFFFE
	ldc.i4	0x00000001
	add.ovf.un
	stsfld	int32 add_ovf::temp32a
	ldc.i4	0xFFFFFFFF
	ldc.i4	0x00000001
	add.ovf.un
	conv.ovf.u4
	stsfld	int32 add_ovf::temp32b
	leave	done
try_end:
	br		FAIL
handler_start:
	stloc	0
	leave	done
handler_end:
done:
	ldloc	0
	isinst	[mscorlib]System.OverflowException
	brfalse	FAIL
	ldc.i4	0xFFFFFFFF
	ldsfld	int32 add_ovf::temp32a
	ceq
	brfalse	FAIL
PASS:
	ldc.i4	0x00000001
	ret
FAIL:
	ldc.i4	0x00000000
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler handler_start to handler_end
}

.method public static int32 i8() {
.locals(class [mscorlib]System.Exception)
.maxstack	5
try_start:
	ldc.i8	0x7FFFFFFFFFFFFFFE
	ldc.i8	0x0000000000000001
	add.ovf
	conv.ovf.i8
	stsfld	int64 add_ovf::temp64a
	ldc.i8	0x7FFFFFFFFFFFFFFF
	ldc.i8	0x0000000000000001
	add.ovf
	conv.ovf.i8
	stsfld	int64 add_ovf::temp64b
	leave	done
try_end:
	br		FAIL
handler_start:
	stloc	0
	leave	done
handler_end:
done:
	ldloc	0
	isinst	[mscorlib]System.OverflowException
	brfalse	FAIL
	ldc.i8	0x7FFFFFFFFFFFFFFF
	ldsfld	int64 add_ovf::temp64a
	ceq
	brfalse	FAIL
PASS:
	ldc.i4	0x00000001
	ret
FAIL:
	ldc.i4	0x00000000
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler handler_start to handler_end
}

.method public static int32 u8() {
.locals(class [mscorlib]System.Exception)
.maxstack	5
try_start:
	ldc.i8	0xFFFFFFFFFFFFFFFE
	ldc.i8	0x0000000000000001
	add.ovf.un
	stsfld	int64 add_ovf::temp64a
	ldc.i8	0xFFFFFFFFFFFFFFFF
	ldc.i8	0x0000000000000001
	add.ovf.un
	conv.ovf.u8
	stsfld	int64 add_ovf::temp64b
	leave	done
try_end:
	br		FAIL
handler_start:
	stloc	0
	leave	done
handler_end:
done:
	ldloc	0
	isinst	[mscorlib]System.OverflowException
	brfalse	FAIL
	ldc.i8	0xFFFFFFFFFFFFFFFF
	ldsfld	int64 add_ovf::temp64a
	ceq
	brfalse	FAIL
PASS:
	ldc.i4	0x00000001
	ret
FAIL:
	ldc.i4	0x00000000
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler handler_start to handler_end
}


.method public static int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack	10
	call		int32 add_ovf::i1()
	brfalse		FAIL
	call		int32 add_ovf::u1()
	brfalse		FAIL
 	call		int32 add_ovf::i2()
	brfalse		FAIL
	call		int32 add_ovf::u2()
	brfalse		FAIL
	call		int32 add_ovf::i4()
	brfalse		FAIL
	call		int32 add_ovf::u4()
	brfalse		FAIL
	call		int32 add_ovf::i8()
	brfalse		FAIL
	call		int32 add_ovf::u8()
	brfalse		FAIL

PASS:
	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
