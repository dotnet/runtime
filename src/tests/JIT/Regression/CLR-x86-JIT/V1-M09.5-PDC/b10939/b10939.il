// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b10939
{
}
.method public static class [mscorlib]System.Object [0] foo(class [mscorlib]System.Object) {

  ldnull
  ret
}



.class public auto ansi Test_b10939 extends [mscorlib] System.Object {
 .method public static int32 main() {
  .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
      01 00 00 00
  )
  .entrypoint

  ldnull
  call class [mscorlib]System.Object [0] foo(class [mscorlib]System.Object)
  pop
  
  ldstr  "Passed."
  call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
  ldc.i4 100
  ret 
 }
}
