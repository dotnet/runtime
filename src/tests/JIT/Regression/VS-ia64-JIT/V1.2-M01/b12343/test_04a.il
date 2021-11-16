// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// endfinally after finally
// expect throw System.InvalidProgramException

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly test {}
.assembly extern xunit.core
{
    .publickeytoken = (
        8d 05 b1 bb 7a 6f db 6c
     )
     .ver 2:4:2:0
}
.module test_04a.exe

.class private auto ansi beforefieldinit test extends [mscorlib]System.Object
{
  .method public static int32 Main() cil managed
  {
   .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
       01 00 00 00
   )
   .entrypoint
   .try
   {
	call void test::f()
	leave FAIL
   }
   catch [mscorlib]System.NullReferenceException
   {
        leave PASS
   }
   PASS:
	ldc.i4 100
	ret
   FAIL:
	ldc.i4 1
	ret
  }

  .method public static void f()
  {

   .locals (int32* V_0, int32 V_1)

	ldc.i4 0
	conv.u
	ldind.i4
	pop
	ret
  }
}
