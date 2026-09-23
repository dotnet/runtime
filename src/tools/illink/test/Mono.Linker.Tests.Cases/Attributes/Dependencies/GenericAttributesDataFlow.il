.assembly extern System.Runtime
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
}
.assembly 'GenericAttributesDataFlow'
{
  .hash algorithm 0x00008004
  .ver 1:0:0:0
}
.module GenericAttributesDataFlow.dll

.namespace Mono.Linker.Tests.Cases.Attributes.Dependencies
{
  // A generic attribute whose type parameter requires DynamicallyAccessedMemberTypes.PublicMethods.
  .class public auto ansi beforefieldinit DynamicallyAccessedMembersGenericAttribute`1<T>
    extends [System.Runtime]System.Attribute
  {
    .param type T
    .custom instance void [System.Runtime]System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute::.ctor(valuetype [System.Runtime]System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes) = (
         01 00 08 00 00 00 00 00
    )

    .method public hidebysig specialname rtspecialname
           instance default void '.ctor' () cil managed
    {
      .maxstack 8
      IL_0000: ldarg.0
      IL_0001: call instance void class [System.Runtime]System.Attribute::'.ctor'()
      IL_0006: ret
    }
  }

  // The attribute is applied with the class's own generic parameter as the generic argument. This
  // construct cannot be expressed in C# (CS8968: an attribute type argument cannot use type
  // parameters), so it is provided in IL. The trimmer must analyze the generic-argument data flow of
  // the attribute without crashing and warn (IL2091) because the unannotated generic argument does
  // not satisfy the attribute's PublicMethods requirement.
  .class public auto ansi beforefieldinit ClassWithUnannotatedTypeParameter`1<T>
    extends [System.Runtime]System.Object
  {
    .custom instance void class Mono.Linker.Tests.Cases.Attributes.Dependencies.DynamicallyAccessedMembersGenericAttribute`1<!T>::'.ctor'() = (01 00 00 00)

    .method public hidebysig specialname rtspecialname
           instance default void '.ctor' () cil managed
    {
      .maxstack 8
      IL_0000: ldarg.0
      IL_0001: call instance void class [System.Runtime]System.Object::'.ctor'()
      IL_0006: ret
    }
  }
}
