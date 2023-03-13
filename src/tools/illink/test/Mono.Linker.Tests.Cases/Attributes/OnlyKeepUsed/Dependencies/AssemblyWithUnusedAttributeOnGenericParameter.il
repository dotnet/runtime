.assembly extern mscorlib
{
}
.assembly 'library'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module library.dll


.namespace Mono.Linker.Tests.Cases.Attributes.OnlyKeepUsed.Dependencies
{

  .class public auto ansi beforefieldinit FooAttribute
    extends [mscorlib]System.Attribute
  {

    .method public hidebysig specialname rtspecialname
           instance default void '.ctor' ()  cil managed
    {
    .maxstack 8
    IL_0000:  ldarg.0
    IL_0001:  call instance void class [mscorlib]System.Attribute::'.ctor'()
    IL_0006:  ret
    }

  }

  .class public auto ansi beforefieldinit GenericType`1<T>
    extends [mscorlib]System.Object
  {
    .param type T
    .custom instance void class Mono.Linker.Tests.Cases.Attributes.OnlyKeepUsed.Dependencies.FooAttribute::'.ctor'() =  (01 00 00 00 ) // ....

    .field private !T input

    .method public hidebysig specialname rtspecialname
          instance default void '.ctor' (!T input)  cil managed
    {
    .maxstack 8
    IL_0000:  ldarg.0
    IL_0001:  call instance void class [mscorlib]System.Object::'.ctor'()
    IL_0006:  ldarg.0
    IL_0007:  ldarg.1
    IL_000a:  stfld  !0 class Mono.Linker.Tests.Cases.Attributes.OnlyKeepUsed.Dependencies.GenericType`1<!T>::input
    IL_000f:  ret
    }

    .method public hidebysig instance !T Method() cil managed
    {
    .maxstack 8
    IL_0000:  ldarg.0
    IL_0001:  ldfld !0 class Mono.Linker.Tests.Cases.Attributes.OnlyKeepUsed.Dependencies.GenericType`1<!T>::input
    IL_0006:  ret
    }

  }

  .class public auto ansi beforefieldinit TypeWithGenericMethod
    extends [mscorlib]System.Object
  {
    .method public hidebysig specialname rtspecialname
            instance void '.ctor' () cil managed
    {
    .maxstack 8
    IL_0000:  ldarg.0
    IL_0001:  call instance void class [mscorlib]System.Object::'.ctor'()
    IL_0006:  ret
    }

    .method public hidebysig instance !!T GenericMethod<T> (!!T input) cil managed
    {
    .param type T
    .custom instance void class Mono.Linker.Tests.Cases.Attributes.OnlyKeepUsed.Dependencies.FooAttribute::'.ctor'() =  (01 00 00 00 ) // ....
    .maxstack 8
    IL_0000:  ldarg.1
    IL_0001:  ret
    }
  }

}
