// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}

.class public Sub_I4 {
.method public static	int32 _sub(int32,int32) {
.maxstack		20
	ldarg	0
	ldarg	1
	sub
	ret
}
.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		20
	ldc.i4		0x80000000
	ldc.i4		0x80000000
	call	int32 Sub_I4::_sub(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
	ldc.i4		0x80000000
	ldc.i4		0xFFFFFFFF
	call	int32 Sub_I4::_sub(int32,int32)
	ldc.i4		0x80000001
	ceq
	brfalse		FAIL
	ldc.i4		0x80000000
	ldc.i4		0x00000000
	call	int32 Sub_I4::_sub(int32,int32)
	ldc.i4		0x80000000
	ceq
	brfalse		FAIL
	ldc.i4		0x80000000
	ldc.i4		0x00000001
	call	int32 Sub_I4::_sub(int32,int32)
	ldc.i4		0x7FFFFFFF
	ceq
	brfalse		FAIL
	ldc.i4		0x80000000
	ldc.i4		0x7FFFFFFF
	call	int32 Sub_I4::_sub(int32,int32)
	ldc.i4		0x00000001
	ceq
	brfalse		FAIL
	ldc.i4		0x80000000
	ldc.i4		0x55555555
	call	int32 Sub_I4::_sub(int32,int32)
	ldc.i4		0x2AAAAAAB
	ceq
	brfalse		FAIL
	ldc.i4		0x80000000
	ldc.i4		0xAAAAAAAA
	call	int32 Sub_I4::_sub(int32,int32)
	ldc.i4		0xD5555556
	ceq
	brfalse		FAIL
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x80000000
	call	int32 Sub_I4::_sub(int32,int32)
	ldc.i4		0x7FFFFFFF		
	ceq
	brfalse		FAIL
	ldc.i4		0xFFFFFFFF
	ldc.i4		0xFFFFFFFF
	call	int32 Sub_I4::_sub(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x00000000
	call	int32 Sub_I4::_sub(int32,int32)
	ldc.i4		0xFFFFFFFF
	ceq
	brfalse		FAIL
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x00000001
	call	int32 Sub_I4::_sub(int32,int32)
	ldc.i4		0xFFFFFFFE
	ceq
	brfalse		FAIL
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x7FFFFFFF
	call	int32 Sub_I4::_sub(int32,int32)
	ldc.i4		0x80000000
	ceq
	brfalse		FAIL
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x55555555
	call	int32 Sub_I4::_sub(int32,int32)
	ldc.i4		0xAAAAAAAA
	ceq
	brfalse		FAIL
	ldc.i4		0xFFFFFFFF
	ldc.i4		0xAAAAAAAA
	call	int32 Sub_I4::_sub(int32,int32)
	ldc.i4		0x55555555
	ceq
	brfalse		FAIL

	ldc.i4		0x00000000
	ldc.i4		0x80000000
	call	int32 Sub_I4::_sub(int32,int32)
	ldc.i4		0x80000000
	ceq
	brfalse		FAIL
	ldc.i4		0x00000000
	ldc.i4		0xFFFFFFFF
	call	int32 Sub_I4::_sub(int32,int32)
	ldc.i4		0x00000001
	ceq
	brfalse		FAIL
	ldc.i4		0x00000000
	ldc.i4		0x00000000
	call	int32 Sub_I4::_sub(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
	ldc.i4		0x00000000
	ldc.i4		0x00000001
	call	int32 Sub_I4::_sub(int32,int32)
	ldc.i4		0xFFFFFFFF
	ceq
	brfalse		FAIL
	ldc.i4		0x00000000
	ldc.i4		0x7FFFFFFF
	call	int32 Sub_I4::_sub(int32,int32)
	ldc.i4		0x80000001
	ceq
	brfalse		FAIL
	ldc.i4		0x00000000
	ldc.i4		0x55555555
	call	int32 Sub_I4::_sub(int32,int32)
	ldc.i4		0xAAAAAAAB
	ceq
	brfalse		FAIL
	ldc.i4		0x00000000
	ldc.i4		0xAAAAAAAA
	call	int32 Sub_I4::_sub(int32,int32)
	ldc.i4		0x55555556
	ceq
	brfalse		FAIL

	ldc.i4		0x00000001
	ldc.i4		0x80000000
	call	int32 Sub_I4::_sub(int32,int32)
	ldc.i4		0x80000001
	ceq
	brfalse		FAIL
	ldc.i4		0x00000001
	ldc.i4		0xFFFFFFFF
	call	int32 Sub_I4::_sub(int32,int32)
	ldc.i4		0x00000002
	ceq
	brfalse		FAIL
	ldc.i4		0x00000001
	ldc.i4		0x00000000
	call	int32 Sub_I4::_sub(int32,int32)
	ldc.i4		0x00000001
	ceq
	brfalse		FAIL
	ldc.i4		0x00000001
	ldc.i4		0x00000001
	call	int32 Sub_I4::_sub(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
	ldc.i4		0x00000001
	ldc.i4		0x7FFFFFFF
	call	int32 Sub_I4::_sub(int32,int32)
	ldc.i4		0x80000002
	ceq
	brfalse		FAIL
	ldc.i4		0x00000001
	ldc.i4		0x55555555
	call	int32 Sub_I4::_sub(int32,int32)
	ldc.i4		0xAAAAAAAC
	ceq
	brfalse		FAIL
	ldc.i4		0x00000001
	ldc.i4		0xAAAAAAAA
	call	int32 Sub_I4::_sub(int32,int32)
	ldc.i4		0x55555557
	ceq
	brfalse		FAIL

	ldc.i4		0x7FFFFFFF
	ldc.i4		0x80000000
	call	int32 Sub_I4::_sub(int32,int32)
	ldc.i4		0xFFFFFFFF
	ceq
	brfalse		FAIL
	ldc.i4		0x7FFFFFFF
	ldc.i4		0xFFFFFFFF
	call	int32 Sub_I4::_sub(int32,int32)
	ldc.i4		0x80000000
	ceq
	brfalse		FAIL
	ldc.i4		0x7FFFFFFF
	ldc.i4		0x00000000
	call	int32 Sub_I4::_sub(int32,int32)
	ldc.i4		0x7FFFFFFF
	ceq
	brfalse		FAIL
	ldc.i4		0x7FFFFFFF
	ldc.i4		0x00000001
	call	int32 Sub_I4::_sub(int32,int32)
	ldc.i4		0x7FFFFFFE
	ceq
	brfalse		FAIL
	ldc.i4		0x7FFFFFFF
	ldc.i4		0x7FFFFFFF
	call	int32 Sub_I4::_sub(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
	ldc.i4		0x7FFFFFFF
	ldc.i4		0x55555555
	call	int32 Sub_I4::_sub(int32,int32)
	ldc.i4		0x2AAAAAAA
	ceq
	brfalse		FAIL
	ldc.i4		0x7FFFFFFF
	ldc.i4		0xAAAAAAAA
	call	int32 Sub_I4::_sub(int32,int32)
	ldc.i4		0xD5555555
	ceq
	brfalse		FAIL

	ldc.i4		0x55555555
	ldc.i4		0x80000000
	call	int32 Sub_I4::_sub(int32,int32)
	ldc.i4		0xD5555555
	ceq
	brfalse		FAIL
	ldc.i4		0x55555555
	ldc.i4		0xFFFFFFFF
	call	int32 Sub_I4::_sub(int32,int32)
	ldc.i4		0x55555556
	ceq
	brfalse		FAIL
	ldc.i4		0x55555555
	ldc.i4		0x00000000
	call	int32 Sub_I4::_sub(int32,int32)
	ldc.i4		0x55555555
	ceq
	brfalse		FAIL
	ldc.i4		0x55555555
	ldc.i4		0x00000001
	call	int32 Sub_I4::_sub(int32,int32)
	ldc.i4		0x55555554
	ceq
	brfalse		FAIL
	ldc.i4		0x55555555
	ldc.i4		0x7FFFFFFF
	call	int32 Sub_I4::_sub(int32,int32)
	ldc.i4		0xD5555556
	ceq
	brfalse		FAIL
	ldc.i4		0x55555555
	ldc.i4		0x55555555
	call	int32 Sub_I4::_sub(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
	ldc.i4		0x55555555
	ldc.i4		0xAAAAAAAA
	call	int32 Sub_I4::_sub(int32,int32)
	ldc.i4		0xAAAAAAAB
	ceq
	brfalse		FAIL

	ldc.i4		0xAAAAAAAA
	ldc.i4		0x80000000
	call	int32 Sub_I4::_sub(int32,int32)
	ldc.i4		0x2AAAAAAA
	ceq
	brfalse		FAIL
	ldc.i4		0xAAAAAAAA
	ldc.i4		0xFFFFFFFF
	call	int32 Sub_I4::_sub(int32,int32)
	ldc.i4		0xAAAAAAAB
	ceq
	brfalse		FAIL
	ldc.i4		0xAAAAAAAA
	ldc.i4		0x00000000
	call	int32 Sub_I4::_sub(int32,int32)
	ldc.i4		0xAAAAAAAA
	ceq
	brfalse		FAIL
	ldc.i4		0xAAAAAAAA
	ldc.i4		0x00000001
	call	int32 Sub_I4::_sub(int32,int32)
	ldc.i4		0xAAAAAAA9
	ceq
	brfalse		FAIL
	ldc.i4		0xAAAAAAAA
	ldc.i4		0x7FFFFFFF
	call	int32 Sub_I4::_sub(int32,int32)
	ldc.i4		0x2AAAAAAB
	ceq
	brfalse		FAIL
	ldc.i4		0xAAAAAAAA
	ldc.i4		0x55555555
	call	int32 Sub_I4::_sub(int32,int32)
	ldc.i4		0x55555555
	ceq
	brfalse		FAIL
	ldc.i4		0xAAAAAAAA
	ldc.i4		0xAAAAAAAA
	call	int32 Sub_I4::_sub(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL


	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly sub_i4.exe{}
