// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}


.assembly ILStackAllocRepro {}
.assembly extern xunit.core {}
.assembly extern mscorlib{auto}

.class public auto ansi beforefieldinit Foo
       extends [mscorlib]System.Object
{
  .field private static int32* buffer
  .method public hidebysig static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       20 (0x14)
    .maxstack  2
    .locals init (int32* V_0)
    IL_0000:  ldc.i4.8
    IL_0001:  localloc
    IL_0003:  stsfld     int32* Foo::buffer
    IL_0008:  ldsfld     int32* Foo::buffer
    IL_000d:  ldind.i4
    IL_000e:  call       void [System.Console]System.Console::WriteLine(int32)
ldc.i4 100
    IL_0013:  ret
  } // end of method Foo::Main

} // end of class Foo

