// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern xunit.core {}
// this is regression test for VSW 452707
// Overriding MyS.ToString caused an AV since we were attempting to create precode 
// before the slot that the method desc points to has been filled in.

.assembly extern mscorlib {}

.assembly b452707 {}

.class private sequential ansi sealed beforefieldinit MyS
       extends [mscorlib]System.ValueType
{
  .method public hidebysig virtual instance string 
          Bla() cil managed
  {
    .override [mscorlib]System.ValueType::ToString

    .maxstack  1
    .locals init (string V_0)
    nop
    ldstr      "MyS.ToString"
    stloc.0
    ldloc.0
    ret
  } 
}


.class public auto ansi beforefieldinit Test_b452707
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
 
    .maxstack  1
    .locals init (valuetype MyS V_0)
    nop
    ldloca.s   V_0
    initobj    MyS
    ldloc.0
    box        MyS
    call       void [System.Console]System.Console::WriteLine(object)
    nop

    ldstr      "PASS"
    call       void [System.Console]System.Console::WriteLine(string)

    ldc.i4 100
    ret
  } 

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  8
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  } 

} 
