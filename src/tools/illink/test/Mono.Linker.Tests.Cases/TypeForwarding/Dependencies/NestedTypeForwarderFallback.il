.assembly extern mscorlib
{
}

.assembly extern FallbackImplementation
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

.class extern forwarder System.Environment
{
  .assembly extern FallbackImplementation
}

.module FallbackForwarder.dll
