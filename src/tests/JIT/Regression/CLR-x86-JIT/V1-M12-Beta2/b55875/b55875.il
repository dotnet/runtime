// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib{}
.assembly extern xunit.core {}
.assembly 'b55875'{}
.namespace H
{
  .class public H
  {
.method public static int32 'Perm'() il managed
    {
	.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
	    01 00 00 00
	)
	.maxstack 4
	.locals(int32)
    	.entrypoint
	ldc.i4.1
	stloc.0
	ldloc.0
	ldc.i4.5
	bgt	lb12
lb11:	// Loop header
	ldloc.0
	ldc.i4.1
	add
	stloc.0
	ldloc.0
	ldc.i4.5
	ble	lb11
lb12:	// Loop exit
	ldc.i4 100
	ret
    }
  }

}
