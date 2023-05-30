// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ASSEMBLY_NAME {}

.method public static int32 Foo()
{
	ldc.i4 100
	ret
}

.class public Test
{
.method public static int32 main()
{
	.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
	    01 00 00 00
	)
	.entrypoint
	.locals init (int32 V_0)
	ldc.i4 101
	stloc.0
	ldftn int32 Foo()
	calli int32()
	stloc.0
	ldloc.0
	ret
}
}
