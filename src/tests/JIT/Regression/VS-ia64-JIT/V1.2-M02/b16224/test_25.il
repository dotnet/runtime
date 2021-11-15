// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib{}
.assembly jumper{}
.assembly extern xunit.core
{
    .publickeytoken = (
        8d 05 b1 bb 7a 6f db 6c
     )
     .ver 2:4:2:0
}

  .class private sequential ansi sealed beforefieldinit VT
         extends [mscorlib]System.ValueType
  {
    .field private int32 m_n
    .field private valuetype VT[] m_dummyGCRef

    .method private hidebysig static int32 Main() cil managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      ldc.i4 100
	ret
    }
  } 
