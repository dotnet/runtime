// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly rem_r4{}
.class public _rem {

.method public static int32 main(class [mscorlib]System.String[]) {

	.entrypoint

	ldc.i4.0
	dup

	switch(b1,b2)

b1:
	call void [System.Console]System.Console::WriteLine(int32)
	br end
b2:
	call void [System.Console]System.Console::WriteLine(int32)

end:
	//call void [System.Console]System.Console::WriteLine(int32)
	ldc.i4 100
	ret


// -- End Main ----------------------
}
//
// -- EOF ---------------------------
}
// ----------------------------------
