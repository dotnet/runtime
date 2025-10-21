// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern xunit.core {}
.assembly extern Microsoft.DotNet.XUnitExtensions { .publickeytoken = (31 BF 38 56 AD 36 4E 35 ) }
.assembly extern TestLibrary {}
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
  // [ConditionalFact(typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.IsVarArgSupported))]
  .custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ConditionalFactAttribute::.ctor(class [System.Runtime]System.Type,
                                                                                                string[]) = ( 01 00 61 54 65 73 74 4C 69 62 72 61 72 79 2E 50   // ..aTestLibrary.P
                                                                                                              6C 61 74 66 6F 72 6D 44 65 74 65 63 74 69 6F 6E   // latformDetection
                                                                                                              2C 20 54 65 73 74 4C 69 62 72 61 72 79 2C 20 56   // , TestLibrary, V
                                                                                                              65 72 73 69 6F 6E 3D 30 2E 30 2E 30 2E 30 2C 20   // ersion=0.0.0.0, 
                                                                                                              43 75 6C 74 75 72 65 3D 6E 65 75 74 72 61 6C 2C   // Culture=neutral,
                                                                                                              20 50 75 62 6C 69 63 4B 65 79 54 6F 6B 65 6E 3D   //  PublicKeyToken=
                                                                                                              6E 75 6C 6C 01 00 00 00 11 49 73 56 61 72 41 72   // null.....IsVarAr
                                                                                                              67 53 75 70 70 6F 72 74 65 64 00 00 )             // gSupported..
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
