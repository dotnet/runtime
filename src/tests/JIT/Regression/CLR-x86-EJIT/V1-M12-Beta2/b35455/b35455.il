// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x917e8626 {}
.class ILGEN_0x917e8626 {
	.method static int32 Main() {
	.entrypoint
	.maxstack	20
	ldc.i4 0xFFFFFFF
	dup
	conv.r4
	conv.i4
	sub
	ldc.i4 101
	add
	ret
}
}
