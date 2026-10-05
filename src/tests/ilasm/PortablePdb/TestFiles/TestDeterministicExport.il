// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime
{
}

.assembly TestDeterministicExport
{
}

.vtfixup [1] int32 fromunmanaged at VT_01
.data VT_01 = int32(0)

.class public auto ansi beforefieldinit TestDeterministicExportType
       extends [System.Runtime]System.Object
{
  .method public static void Exported() cil managed
  {
     .vtentry 1 : 1
     .export [1] as Exported
     ret
  }
}
