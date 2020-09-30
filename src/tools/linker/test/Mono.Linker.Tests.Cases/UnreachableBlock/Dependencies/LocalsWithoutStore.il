.assembly extern mscorlib
{
}
.assembly 'library'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module library.dll

.namespace Mono.Linker.Tests.Cases.UnreachableBlock.Dependencies
{

  .class public auto ansi beforefieldinit ClassA
    extends [mscorlib]System.Object
  {

    .method public hidebysig specialname rtspecialname
           instance default void '.ctor' ()  cil managed
    {
      IL_0000:  ldarg.0
      IL_0001:  call instance void class [mscorlib]System.Object::'.ctor'()
      IL_0006:  ret
    }

    .method public static hidebysig object Method_1() cil managed
    {
      .locals init (object V_0,
          object V_1)
      ldloc.1 
      stloc.0
      ldnull
      ret
    }

    .method public static hidebysig object Method_2() cil managed
    {
      .maxstack 2
      .locals init (int32 A, int32 B)
      ldc.i4.1
      stloc.0
      ldloc.0
      ldloc.1
      ldnull
      ret
    }
  }
}