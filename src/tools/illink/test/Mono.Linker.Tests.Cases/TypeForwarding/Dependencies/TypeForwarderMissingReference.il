.assembly extern mscorlib
{
}
.assembly extern 'missing-reference'
{
  .ver 0:0:0:0
}

.assembly 'TypeForwarderMissingReference'
{
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}

.class extern forwarder C
{
  .assembly extern 'missing-reference'
}
.class extern forwarder G`1
{
  .assembly extern 'missing-reference'
}

.module 'TypeForwarderMissingReference.dll'


.class public auto ansi beforefieldinit DummyClass
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  }
}