// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern mscorlib { }
.assembly 'ldfld' { }
.class public auto ansi beforefieldinit Test
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32 Main() cil managed
  {
    .entrypoint
	.maxstack  8
	.locals (int64)
	.zeroinit
	ldloc.0
	ldloca 0
	ldc.i4.1
	ldc.i4 8
	initblk
	call void [System.Console]System.Console::WriteLine(int64)
	ldc.i4 100
	ret
  } 

  .method public hidebysig specialname rtspecialname 
          instance void .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } 

} 
