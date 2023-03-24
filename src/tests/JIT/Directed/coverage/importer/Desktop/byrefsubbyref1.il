// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.





.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}
.assembly extern legacy library mscorlib {}
.assembly byrefsubbyref1 { }
.class public a extends [mscorlib]System.Object
{
.field static class ctest S_1
.method public static native int byrefsubbyref(class ctest& V_1, class ctest& V_2)
{
ldarg 0
ldarg 1
sub
ret
}
.method public static native int byrefsubi4(class ctest& V_1, int32 V_2)
{
ldarg 0
ldarg 1
sub
ret
}
.method public static native int i4subbyref(int32, class ctest& V_2)
{
ldarg 0
ldarg 1
sub
ret
}
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

  IL_0010:
	ldloca V_2
	ldloca V_1
	call native int a::byrefsubbyref(class ctest&, class ctest&)
	dup
	stloc.2
	call       void [System.Console]System.Console::WriteLine(int32)

	ldloca V_2
	ldc.i4 1
	call native int a::byrefsubi4(class ctest&, int32)
	call       void [System.Console]System.Console::WriteLine(int32)

	ldc.i4 1
	ldloca V_1
	call native int a::i4subbyref(int32, class ctest&)
	call       void [System.Console]System.Console::WriteLine(int32)

	newobj     instance void ctest::.ctor()
	stloc.0
	ldloca V_1
	ldsflda class ctest a::S_1
	call native int a::byrefsubbyref(class ctest&, class ctest&)
	newobj     instance void ctest::.ctor()
	stloc.0
	ldloca V_1
	call native int a::byrefsubbyref(class ctest&, class ctest&)
	ldsflda class ctest a::S_1
	call native int a::byrefsubbyref(class ctest&, class ctest&)
	newobj     instance void ctest::.ctor()
	stsfld class ctest a::S_1
	ldsflda class ctest a::S_1
	call native int a::byrefsubbyref(class ctest&, class ctest&)
	ldsflda class ctest a::S_1
	call native int a::byrefsubbyref(class ctest&, class ctest&)
	newobj     instance void ctest::.ctor()
	stloc.0
	ldloca V_1
	call native int a::byrefsubbyref(class ctest&, class ctest&)
	ldsflda class ctest a::S_1
	call native int a::byrefsubbyref(class ctest&, class ctest&)
	newobj     instance void ctest::.ctor()
	stloc.0
	ldloca V_1
	call native int a::byrefsubbyref(class ctest&, class ctest&)
	ldsflda class ctest a::S_1
	call native int a::byrefsubbyref(class ctest&, class ctest&)
	newobj     instance void ctest::.ctor()
	stloc.0
	ldloca V_1
	call native int a::byrefsubbyref(class ctest&, class ctest&)
	call       void [System.Console]System.Console::WriteLine(int32)

ldc.i4 100
  IL_0038:  ret
}
}

.class private auto ansi ctest
       extends [mscorlib]System.Object
{
  .method public virtual instance int32  f() cil managed
  {
    .maxstack  1
    IL_0000:  ldc.i4.s   100
    IL_0002:  br.s       IL_0004

    IL_0004:  ret
  } 

  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } 
}
