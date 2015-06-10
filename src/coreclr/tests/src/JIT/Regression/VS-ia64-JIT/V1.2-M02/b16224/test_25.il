// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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