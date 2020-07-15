// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib{}
.assembly base017{}
.module base017.exe


  .class public sequential ansi sealed beforefieldinit base017
         extends [mscorlib]System.ValueType
  {
    .size 1
    .method public instance void GetStr() cil managed
    {
	ldarg 0
	ldobj     base017
	pop
	ret
    }

    .method public hidebysig static int32 Main() cil managed
    {
	.entrypoint
	.locals init (valuetype base017)
	ldloca 0
	initobj    base017
	ldloca 0
	call       instance void base017::GetStr()
	ldc.i4 100
	ret
    }
  } 
