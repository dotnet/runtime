// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b102962c {}
.method public static void  
        foo(string s) cil managed
{
  .maxstack  2
  .locals init (string pinned  V_0,
           int16 * V_1)
  IL_0000:  ldnull
  IL_0001:  stloc.0
  IL_0002:  ldarg.0
  IL_0003:  stloc.0
  IL_0004:  ldloc.0
	conv.i
  IL_0005:  ldc.i4.s   12
  IL_0007:  add
  IL_0008:  stloc.1
  IL_0009:  ret
} // end of global method foo

.class public auto ansi Test_b102962c extends [mscorlib] System.Object {
.method public static int32  
        main() cil managed
{
  .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
      01 00 00 00
  )
  .entrypoint
  // Code size       14 (0xe)
  .maxstack  1
  IL_0000:  ldstr      "hello"
  IL_0005:  call       void  foo(string)
  IL_000a:  ldc.i4 100
  IL_000b:  br.s       IL_000d

  IL_000d:  ret
}
}
