// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b90129'
{
}

.class public auto ansi Bug222
{
  .method public hidebysig static int32 main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       22 (0x16)
    .maxstack  3
    .locals (int32 V_0)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.0
    IL_0002:  br.s       IL_0010

    IL_0004:  ldsfld     int32[] Bug222Interface::'array'
    IL_0009:  ldloc.0
    IL_000a:  ldc.i4.1
    IL_000b:  stelem.i4
    IL_000c:  ldloc.0
    IL_000d:  ldc.i4.1
    IL_000e:  add
    IL_000f:  stloc.0
    IL_0010:  ldloc.0
    IL_0011:  ldc.i4.s   6
    IL_0013:  blt.s      IL_0004

              ldc.i4 100
    IL_0015:  ret
  } // end of method Bug222::main

} // end of class Bug222

.class interface public abstract auto ansi Bug222Interface
{
  .field public static int32[] 'array'
  .method public hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       49 (0x31)
    .maxstack  2
    .locals (int32 V_0)
    IL_0000:  ldc.i4.s   6
    IL_0010:  newarr     [mscorlib]System.Int32
    IL_0015:  stsfld     int32[] Bug222Interface::'array'
    IL_0030:  ret
  } // end of method Bug222Interface::.cctor

} // end of class Bug222Interface
