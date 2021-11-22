// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern xunit.core {}
.assembly extern mscorlib {}
.assembly extern RefX1 {}
.assembly extern ValX1 {}

.assembly Test_Test {}

.class public sequential ansi sealed beforefieldinit Gen<T>
       extends [mscorlib]System.ValueType
{
  .field public !T Fld1
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(!T fld1) cil managed
  {
    .maxstack  8
    nop
    ldarg.0
    ldarg.1
    stfld      !0 valuetype Gen<!T>::Fld1
    ret
  } 
}


// the below lines cause an AV
// new Gen<ValX1<string>[][][]>(new ValX1<string>[1][][]);
// new Gen<RefX1<RefX1<RefX1<string>>>>(new RefX1<RefX1<RefX1<string>>>());

.class public auto ansi beforefieldinit Test_Test
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  8
    nop
    ldc.i4.1
    newarr     valuetype [ValX1]ValX1<string>[][]
    newobj     instance void valuetype Gen<valuetype [ValX1]ValX1<string>[][][]>::.ctor(!0)
    pop
    newobj     instance void class [RefX1]RefX1<class [RefX1]RefX1<class [RefX1]RefX1<string>>>::.ctor()
    newobj     instance void valuetype Gen<class [RefX1]RefX1<class [RefX1]RefX1<class [RefX1]RefX1<string>>>>::.ctor(!0)
    pop

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
