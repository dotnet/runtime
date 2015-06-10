// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern mscorlib{}
.assembly Recurse1 {}
.namespace Recurse1
{
  .class public Recurse1
  {
    .method public static int32  Main(class System.String[] A_0)
    {
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