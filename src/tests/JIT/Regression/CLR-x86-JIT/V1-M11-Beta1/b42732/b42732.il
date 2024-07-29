// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b42732'
{
  
}
.class public M
{
  .method public instance void .ctor()
  {
    .maxstack 10
    ldarg.0
    call instance void [mscorlib]System.Object::.ctor()
    ret
  }

  .method public static void test(class [mscorlib]System.Object)
  {
    .maxstack 3
    ldarg.0
    callvirt instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    callvirt instance class [mscorlib]System.String [mscorlib]System.Type::ToString()
    call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
    ret
  }
  .method public static void test2(class M)
  {
    .maxstack 3
    ldarg.0
    callvirt instance void M::Simple()
    ret
  }
  .method instance void Simple()
  {
    ldstr "Simple"
    call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
    ret
  }
  

  .method public static int32 main() 
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  10
//    ldstr "My string"
//    call void M::test(class [mscorlib]System.Object)
    newobj instance void M::.ctor()
    call void M::test2(class M)
    ldc.i4 100
    ret
  }

}




