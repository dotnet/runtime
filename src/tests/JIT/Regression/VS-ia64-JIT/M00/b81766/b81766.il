// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly l.exe{}
.method public public static int32 t() {
	ldc.i4		100
	ret 
}
.class public ldftn_calli {
.method public static int32 main() {
.entrypoint
.maxstack	10
ldftn		int32 t()
calli		default int32 ()
ret
}
}
