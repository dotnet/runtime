// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly Test {}
.class Test {
.method static int32 Main() {
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