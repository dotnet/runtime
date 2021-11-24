// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'ble'{}
.method public	static int32 main(class [mscorlib]System.String[]) {
.locals		(class [mscorlib]System.String)
.entrypoint

	ldc.r4		float32(0x7FC00000)
	ldc.r4		float32(0x3F800000)
	ble.un		pass
	ldc.i4		0x5
	ret
pass:
	ldc.i4		100
	ret
}

