// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
.assembly extern mscorlib {}
.assembly extern xunit.core {}
.assembly extern System {}
.assembly 'b16378' {}
.class public bbHndIndex
{
.method public static int32 f() cil managed
{
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack 500
 
start_try3: 
	leave OUT
end_try3: 

start_fault3: 
	br fault3_br
	start_finally1: 
		finally1_out: endfinally
	end_finally1: 

	fault3_br: 
		start_try4: 
			leave fault3_out
		end_try4: 

	TARGET_0_0:
	br	TARGET_0_0
	fault3_out: endfault
end_fault3: 
OUT: 
	  ldc.i4 100
ret

.try start_try4 to end_try4 finally handler start_finally1 to end_finally1
.try start_try3 to end_try3 fault handler start_fault3 to end_fault3
}

}
