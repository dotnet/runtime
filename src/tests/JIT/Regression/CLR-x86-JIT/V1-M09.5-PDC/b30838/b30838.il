// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



// MVID: {5E222DAB-74B5-4FA9-A101-AE45EE636213}
.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b30838'
{
  
}
//Global methods
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
.method public static vararg class [mscorlib]System.Object[0] 
        foo(class [mscorlib]System.Object A_0) il managed
{
  // Code size       2 (0x2)
  .maxstack  8
  IL_0000:  ldnull
  IL_0001:  ret
} // end of global method 'foo'

.class public auto ansi Test_b30838 extends [mscorlib] System.Object {
.method public static int32 main() il managed
{
  .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
      01 00 00 00
  )
  .entrypoint
  // Code size       19 (0x13)
  .maxstack  8
  IL_0000:  ldnull
  IL_0001:  call       vararg class [mscorlib]System.Object[0] foo(class [mscorlib]System.Object)
  IL_0006:  pop
  IL_0007:  ldstr      "Passed."
  IL_000c:  call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
  IL_0011:  ldc.i4.0
ldc.i4 100
add
  IL_0012:  ret
} // end of global method 'main'
}

//*********** DISASSEMBLY COMPLETE ***********************
