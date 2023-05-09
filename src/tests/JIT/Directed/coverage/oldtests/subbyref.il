// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Arithmetic operations are generally only allowed with
// primitive types, but certain operations are allowed
// with byrefs



.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly subbyref {}
.class public a extends [mscorlib]System.Object
{
.field static class ctest S_1
.method public static int32 main() cil managed
{
  .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
      01 00 00 00
  )
  .entrypoint
  .maxstack  2
  .locals init (class ctest V_1,
           class ctest V_2,
           int32 V_3)
  IL_0004:  newobj     instance void ctest::.ctor()
  IL_0009:  stloc.0
  IL_000a:  newobj     instance void ctest::.ctor()
	dup
	stsfld class ctest a::S_1
  IL_000f:  stloc.1

  // op1: byref, op2: byref
  IL_0010:  ldloca V_2
	ldloca V_1
	sub
	dup
	stloc.2
	call       void [System.Console]System.Console::WriteLine(int32)

  // op1: byref, op2: int
	ldloca V_2
	ldc.i4 1
	sub
	conv.i8
	call       void [System.Console]System.Console::WriteLine(int64)

ldc.i4 100
  IL_0038:  ret
}
}

.class private auto ansi ctest
       extends [mscorlib]System.Object
{
  .method public virtual instance int32  f() cil managed
  {
    // Code size       5 (0x5)
    .maxstack  1
    IL_0000:  ldc.i4.s   100
    IL_0002:  br.s       IL_0004

    IL_0004:  ret
  } // end of method ctest::f

  .method public specialname rtspecialname
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method ctest::.ctor

}
