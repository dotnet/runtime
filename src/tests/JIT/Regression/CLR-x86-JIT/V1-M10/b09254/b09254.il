// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b09254
{
}

.method static int32 main(class System.String[]) {
	.entrypoint
	.locals (int32)
	.maxstack		20
	ldc.i4	0xABBA
	stloc 0
	ldloca 0
	mkrefany int32
	refanytype
	pop
	ldc.i4 100
	ret
}
