// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib{}
.assembly jumper{}

  .class private sequential ansi sealed beforefieldinit VT
         extends [mscorlib]System.ValueType
  {
    .field private int32 m_n
    .field private valuetype VT[] m_dummyGCRef

    .method private hidebysig static int32 Main() cil managed
    {
      .entrypoint
      ldc.i4 100
	ret
    }
  } 
