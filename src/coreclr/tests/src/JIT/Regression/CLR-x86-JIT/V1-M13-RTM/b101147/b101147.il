// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}


.assembly Hello{}
.assembly extern mscorlib{}
.method public static int32 main()
{
  .entrypoint

.locals ( int32 JVI,
            int32 KVI,
            int32 K1,
            int32 K2)
.zeroinit            
.maxstack 16
  IL_0000:  ldc.i4.5
  IL_0001:  stloc.s    KVI
  IL_0003:  ldc.i4     0xfffffffd
  IL_0008:  stloc.s    JVI
  
  IL_000a:  ldloc.s    KVI
  IL_000c:  dup
  IL_000d:  ldc.i4     0x0
  IL_0012:  bge        IL_0018
  IL_0017:  neg
  IL_0018:  ldloc.s    JVI
  IL_001a:  ldc.i4     0x0
  IL_001f:  bge        IL_0025
  IL_0024:  neg
  IL_0025:  stloc.s    K1
  
  IL_0027:  ldloc.s    KVI
  IL_0029:  dup
  IL_002a:  ldc.i4     0x0
  IL_002f:  bge        IL_0035
  IL_0034:  neg
  IL_0035:  ldloc.s    JVI
  IL_0037:  ldc.i4     0x0
  IL_003c:  bge        IL_0042
  IL_0041:  neg
  IL_0042:  stloc.s    K2
  ldloc K1
  call void [System.Console]System.Console::WriteLine(int32)
  ldloc K2
  call void [System.Console]System.Console::WriteLine(int32)
  
  ldc.i4.s 100
  ret     
}  
