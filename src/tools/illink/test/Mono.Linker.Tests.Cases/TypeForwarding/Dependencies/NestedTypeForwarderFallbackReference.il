.assembly extern mscorlib
{
}

.assembly FallbackForwarder
{
}

.class public auto ansi beforefieldinit Fallback.Generic`1<T>
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
    ldarg.0
    call instance void [mscorlib]System.Object::.ctor()
    ret
  }
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

.module FallbackForwarder.dll
