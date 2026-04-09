.assembly extern mscorlib {}

.assembly 'newobj-abstract' {}

.class private auto ansi beforefieldinit Program
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32  Main() cil managed
  {
    .entrypoint
    .locals init (int32 V_0)

    ldc.i4.1
    stloc.0
    .try
    {
      call       void Program::NewAbstract()
      leave.s    leavetarget

    }  // end .try
    catch [mscorlib]System.MemberAccessException
    {
      pop
      ldc.i4.0
      stloc.0
      leave.s    leavetarget
    }
leavetarget:
    ldloc.0
    ret
  } // end of method Program::Main

  .method public hidebysig static void  NewAbstract() cil managed
  {
    newobj     instance void Foo::.ctor()
    call       void [mscorlib]System.GC::KeepAlive(object)
    ret
  }

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    ldarg.0
    call instance void [mscorlib]System.Object::.ctor()
    ret
  }

}

.class private abstract auto ansi beforefieldinit Foo
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    ldarg.0
    call instance void [mscorlib]System.Object::.ctor()
    ret
  }

}
