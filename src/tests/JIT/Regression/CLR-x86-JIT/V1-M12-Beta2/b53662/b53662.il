// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib{}
.assembly Recurse1 {}
.assembly extern xunit.core {}
.namespace Recurse1
{
  .class public Recurse1
  {
    .method public static int32  Main(class System.String[] A_0)
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      .maxstack  8
      .locals (int32 V_0)
      	  ldc.i4.1
      	  stloc.0
      	  ldloc.0
      	  ldc.i4.5
      	  bgt        IL_0025
IL_000f:  ldloc.0
      	  ldc.i4.1
      	  add
      	  stloc.0
      	  ldloc.0
      	  ldc.i4.5
      	  ble        IL_000f
IL_0025:  ldc.i4 100
      	  ret
    }
  }
}
