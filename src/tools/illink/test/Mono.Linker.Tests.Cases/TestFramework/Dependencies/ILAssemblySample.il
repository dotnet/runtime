.assembly extern mscorlib
{
}

.assembly ILAssembly
{

  .hash algorithm 0x00008004
  .ver 0:0:0:0
}

.module ILAssembly.dll

// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi beforefieldinit Mono.Linker.Tests.Cases.TestFramework.Dependencies.ILAssemblySample
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method ILAssemblySample::.ctor

  .method public hidebysig instance string 
          GiveMeAValue() cil managed
  {
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "Bar"
    IL_0006:  stloc.0
    IL_0007:  br         IL_000c

    IL_000c:  ldloc.0
    IL_000d:  ret
  } // end of method ILAssemblySample::GiveMeAValue

}
