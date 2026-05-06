// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern mscorlib{}

.assembly extern A { }
.assembly extern B { }
.assembly extern C { }
.assembly extern Dictionary { }
.assembly extern GenRetType { }
.assembly extern GenDerive1 { }
.assembly extern GenDerive2 { }
.assembly extern GenDerive3 { }
.assembly extern NonGenericDerived1 { }
.assembly extern NonGenericDerived2 { }
.assembly extern NonGenericDerived3 { }
.assembly extern NonGenericDerived4 { }
.assembly extern GenToNonGen1 { }
.assembly extern GenToNonGen2 { }
.assembly extern GenToNonGen3 { }
.assembly extern NonGenThroughGen1 { }
.assembly extern NonGenThroughGen2 { }
.assembly extern NonGenThroughGen3 { }
.assembly extern NonGenThroughGen4 { }

.assembly extern GenBaseType { }

.assembly GenTestType { }

.class public auto ansi beforefieldinit GenTestType<U,V> extends class [GenBaseType]GenBaseType<!U,!V>
{
  .method public hidebysig newslot virtual instance class [NonGenThroughGen4]NonGenThroughGen4 NonGenThroughGenOverride(string& res)
  {
    .override method instance class [B]B class [GenBaseType]GenBaseType<!U,!V>::NonGenThroughGenFunc(string& res)
    ldarg.1
    ldstr "NonGenThroughGen4 TestType.NonGenThroughGenFunc()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class [GenToNonGen3]GenToNonGen3<!V,object> GenToNonGenOverride(string& res)
  {
    .override method instance class [B]B class [GenBaseType]GenBaseType<!U,!V>::GenToNonGen(string& res)
    ldarg.1
    ldstr "GenToNonGen3<!V,object> TestType.GenToNonGenOverride()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class [NonGenericDerived4]NonGenericDerived4 NewGenFunc1(string& res)
  {
    .override method instance class [GenRetType]GenRetType<int32,object> class [GenBaseType]GenBaseType<!U,!V>::MyGenFunc(string& res)
    ldarg.1
    ldstr "NonGenericDerived4 TestType.NewGenFunc1()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class [GenDerive3]GenDerive3<!U,!V> NewGenFunc2(string& res)
  {
    .override method instance class [GenRetType]GenRetType<!0, class [Dictionary]Dictionary<string,!1>> class [GenBaseType]GenBaseType<!U,!V>::MyGenFunc(string& res)
    ldarg.1
    ldstr "GenDerive3<!U,!V> TestType.NewGenFunc2()"
    stind.ref
    ldnull
    ret
  }
  
  .method public hidebysig newslot virtual instance class [GenRetType]GenRetType<!U,!V> NewFunc1(string& res)
  {
    .override method instance object class [GenBaseType]GenBaseType<!U,!V>::MyFunc(string& res)
    ldarg.1
    ldstr "GenRetType<!U,!V> TestType.NewFunc1()"
    stind.ref
    ldnull
    ret
  }
  
  .method public hidebysig newslot virtual instance class [C]C NewFunc2(string& res)
  {
    .override method instance class [B]B class [GenBaseType]GenBaseType<!U,!V>::MyFunc(string& res)
    ldarg.1
    ldstr "C TestType.NewFunc2()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed
  {
    .maxstack  8
    ldarg.0
    call       instance void class [GenBaseType]GenBaseType<!U,!V>::.ctor()
    ret
  }
}
