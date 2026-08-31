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

  .class public auto ansi beforefieldinit EndScopeOnMethod
    extends [mscorlib]System.Object
  {

    .method public hidebysig specialname rtspecialname
           instance default void '.ctor' ()  cil managed
    {
      IL_0000:  ldarg.0
      IL_0001:  call instance void class [mscorlib]System.Object::'.ctor'()
      IL_0006:  ret
    }

    .method public static hidebysig object TryFinally() cil managed
    {
      .try
      {
        ldnull
        ret
      }
      finally
      {
        ldnull
        ret
      }
    }
  }
}