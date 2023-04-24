// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern xunit.core {}
// This is regression test for VSW 484837
// In this case, value__ is the last field as seen below (C# always generates value__ as the very first field.). 
// Because of a bug in the code, Module::ParseMetadataForStaticsIsTypeDefEnum() just looked at the very first // field and took it to be value. 
// This caused the function to return an incorrect return value (the type of field a) and test asserted.


.assembly extern mscorlib {}
.assembly Enum_ValueField {}


.class public auto ansi sealed MyEnum
       extends [mscorlib]System.Enum
{
  .field public static literal valuetype MyEnum a = int32(0x00000001)
  .field public static literal valuetype MyEnum b = int32(0x00000002)
  .field public specialname rtspecialname int32 value__
}


.class public auto ansi beforefieldinit MyClass1
       extends [mscorlib]System.Object
{
  .field public static valuetype MyEnum E

  .method public hidebysig static int32  Main() cil managed
  {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)

    .entrypoint
    .locals init (valuetype MyEnum V_0)
  
    ldc.i4 1
    stloc.0

    ldstr "PASS"
    call void [System.Console]System.Console::WriteLine(string)

    ldc.i4 100
    ret
  } // end of method MyClass1::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  8
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  } // end of method MyClass1::.ctor

  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    .maxstack  8
    ldc.i4.1
    stsfld     valuetype MyEnum MyClass1::E
    ret
  } // end of method MyClass1::.cctor

} // end of class MyClass1
