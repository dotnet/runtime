// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib{}
.assembly 'test_07'{}
.assembly extern xunit.core {}

.class public auto ansi beforefieldinit Client extends [mscorlib]System.Object
{
  .method public hidebysig static int32  Main() cil managed
  {
	.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
	    01 00 00 00
	)
	.entrypoint
	.locals init (int32)

	ldc.i4.0
	stloc.0
	ldloca 0
	call int32 Client::Hello(int32&)
	ret
  }

  .method public hidebysig static int32 Hello(int32&) cil managed
  {
	ldarg.0
	ldc.i4.0
	stind.i4
	ldc.i4 100
	ret
  }
}
