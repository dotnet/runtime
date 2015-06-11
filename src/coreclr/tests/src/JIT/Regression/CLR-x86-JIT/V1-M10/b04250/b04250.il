// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
.assembly b04250
{
}
.class _simple {

.method static int32 main(class System.String[]) {
.entrypoint
.maxstack	100

	ldc.i4 1
	ldc.i4 -1
	neg
	ceq
	ldc.i4 0x1
	sub
	
	ldc.i4 -1
	ldc.i4 1
	neg
	ceq
	ldc.i4 -1
	add

	or

ldc.i4 100
add
	ret
}
}
