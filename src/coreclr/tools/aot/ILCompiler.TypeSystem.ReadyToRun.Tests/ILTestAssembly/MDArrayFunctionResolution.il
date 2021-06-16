// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.class public auto ansi beforefieldinit MDArrayFunctionResolution
       extends [CoreTestAssembly]System.Object
{
  .method private hidebysig static void  MethodWithUseOfMDArrayFunctions() cil managed
  {
    .entrypoint
    // Code size       44 (0x2c)
    .maxstack  5
    .locals init (int32[0...,0...][0...,0...][0...,0...] V_0)
    IL_0000:  ldc.i4.1
    IL_0001:  ldc.i4.1
    IL_0002:  newobj     instance void int32[0...,0...][0...,0...][0...,0...]::.ctor(int32,
                                                                                   int32)
    IL_0007:  stloc.0
    IL_0008:  ldloc.0
    IL_0009:  ldc.i4.0
    IL_000a:  ldc.i4.0
    IL_000b:  ldc.i4.s   123
    IL_000d:  ldc.i4.2
    IL_000e:  newobj     instance void int32[0...,0...][0...,0...]::.ctor(int32,
                                                                        int32)
    IL_0013:  call       instance void int32[0...,0...][0...,0...][0...,0...]::Set(int32,
                                                                                 int32,
                                                                                 int32[0...,0...][0...,0...])
    IL_0018:  ldstr      "Hello world"
    IL_001d:  ldloc.0
    IL_001e:  ldc.i4.0
    IL_001f:  ldc.i4.0
    IL_0020:  call       instance int32[0...,0...][0...,0...] int32[0...,0...][0...,0...][0...,0...]::Get(int32,
                                                                                                        int32)
pop
pop
    IL_002a:  nop
    IL_002b:  ret
} // end of method MethodWithUseOfMDArrayFunctions


}