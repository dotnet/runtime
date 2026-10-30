.assembly extern mscorlib
{
}

.assembly FallbackImplementation
{
}

.class public auto ansi beforefieldinit System.Environment
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
    ldarg.0
    call instance void [mscorlib]System.Object::.ctor()
    ret
  }
}

.module FallbackImplementation.dll
