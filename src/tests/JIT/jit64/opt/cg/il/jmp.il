// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly ASSEMBLY_NAME {
  //This byte field requests that this assembly not be verified at run time and corresponds to this C# declaration:
  //[assembly:System.Security.Permissions.SecurityPermissionAttribute( [mscorlib]System.Security.Permissions.SecurityAction.RequestMinimum, Flags=System.Security.Permissions.SecurityPermissionFlag.SkipVerification )]
}

.class public auto ansi jmp_ {
.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.locals	()
.maxstack	2
	ldc.i4	0x0
	newarr	[mscorlib]System.String
	call	int32 _jmp::jmpTest1(class [mscorlib]System.String[])
	ret
}
}

.class public	_jmp	 {
.method public	void _jmp() {
.maxstack	4
	ret
}

.method public static int32 jmpTest1(class [mscorlib]System.String[]) {
.maxstack	4
	jmp	int32 _jmp::jmpTest2(class [mscorlib]System.String[])
	ldc.i4	0x0
	ret
}

.method public static int32 jmpTest2(class [mscorlib]System.String[]) {
.maxstack	4
	ldc.i4	100
	ret
}

}
