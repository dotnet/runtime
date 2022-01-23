// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b115103' {}
.assembly extern xunit.core {}
.class Test_b115103 {
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	8

	ldc.i4 100
	ldc.i4.1
	ldc.i4.1

	switch (sum,fact)
	pop
	br stop

sum:
	add
	br stop

fact:
	mul

stop:
	ret

}
}
