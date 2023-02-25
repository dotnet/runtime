// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib {}
.assembly extern xunit.core {}
.assembly 'test_286991' {}

.class public auto ansi test {
.method public static int32 Main()
{
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)

.entrypoint

	ldc.i4 0x40
	call int32 isupper(int32)
	ldc.i4 100
	add
	ret
}
}

.method public static pinvokeimpl("msvcrt.dll" cdecl) 
          int32 isupper(int32) cil managed preservesig
{
  .custom instance void [mscorlib]System.Security.SuppressUnmanagedCodeSecurityAttribute::.ctor() = ( 01 00 00 00 ) 

}
