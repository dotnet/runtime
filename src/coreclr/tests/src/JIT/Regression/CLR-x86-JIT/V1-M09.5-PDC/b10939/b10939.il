// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
.assembly b10939
{
}
.method public static class [mscorlib]System.Object [0] foo(class [mscorlib]System.Object) {

  ldnull
  ret
}



 .method public static int32 main() {
  .entrypoint

  ldnull
  call class [mscorlib]System.Object [0] foo(class [mscorlib]System.Object)
  pop
  
  ldstr  "Passed."
  call void [mscorlib]System.Console::WriteLine(class [mscorlib]System.String)
  ldc.i4 100
  ret 
 }
