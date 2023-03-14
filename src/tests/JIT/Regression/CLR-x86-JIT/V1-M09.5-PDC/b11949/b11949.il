// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b11949
{
}
.namespace A
{
.class public auto ansi X
{
  .method public specialname rtspecialname instance void .ctor() il managed
  {
    // Code size       7 (0x7)
    .maxstack  1
	ldarg.0
	call       instance void [mscorlib]System.Object::.ctor()
	ret
  } // end of method 'X::.ctor'


//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
.method public static int32 main() il managed
{
  .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
      01 00 00 00
  )
  .maxstack  1
  .locals (class A.X px)
  .entrypoint

	ldnull
	stloc.0

try_begin:
	newobj     instance void [mscorlib]System.Exception::.ctor()
	throw
	leave.s    try_end

filter_begin:
	pop
	newobj     instance void A.X::.ctor()
	stloc.0
	ldc.i4.1
	endfilter

except_begin:
	pop
	leave.s    try_end

try_end:
	ldc.i4 100
	ret

  .try try_begin to filter_begin filter filter_begin handler except_begin to try_end
} // end of method 'main'
} // end of class 'X'
}
