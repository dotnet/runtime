// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'bug2' {}
.assembly extern xunit.core {}
.class ILGEN_0xf14ee849
{
 .method static int32 Main()
  {
   .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
       01 00 00 00
   )
   .entrypoint
   .maxstack	9
        ldc.i8 15
        ldc.i8 5
       div.un
       ldc.i8 33
      mul
      ldc.i8 5 
     clt
    pop 
  
   ldc.i4 100
   ret
  }

}
