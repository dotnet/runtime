// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern mscorlib{auto}
.assembly IL_DLL //as "IL_DLL.dll"
{
//  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.namespace IL_DLL
{
  .class public auto ansi IL
  {
//    .comtype public IL_DLL.IL { }
    .method public static class [mscorlib]System.String Report() il managed
    {
      // Code size       6 (0x6)
      .maxstack  8

      IL_0000:  ldstr      "IL"
      IL_0005:  ret
    } // end of method 'IL::Report'

    .method public specialname instance void .ctor() il managed
    {
      // Code size       7 (0x7)
      .maxstack  8

      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method 'IL::.ctor'

  } // end of class 'IL'

} // end of namespace 'IL_DLL'
