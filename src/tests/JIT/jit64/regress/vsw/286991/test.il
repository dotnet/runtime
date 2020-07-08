// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern mscorlib {}
.assembly test {}

.method public static int32 Main()
{

.entrypoint

	ldc.i4 0x40
	call int32 isupper(int32)
	ldc.i4 100
	add
	ret
}

.method public static pinvokeimpl("msvcrt.dll" cdecl) 
          int32 isupper(int32) cil managed preservesig
{
  .custom instance void [mscorlib]System.Security.SuppressUnmanagedCodeSecurityAttribute::.ctor() = ( 01 00 00 00 ) 

}
