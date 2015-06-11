// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern mscorlib{}
.assembly delegate26{}
.module delegate26.exe

.class public auto ansi beforefieldinit Client extends [mscorlib]System.Object
{
  .method public hidebysig static int32  Main() cil managed
  {
	.entrypoint
	.locals init (int32)

	ldc.i4.0
	stloc.0
	ldloca 0
	call int32 Client::Hello(int32&)
	ret
  }

  .method public hidebysig static int32 Hello(int32&) cil managed
  {
	ldarg.0
	ldc.i4.0
	stind.i4
	ldc.i4 100
	ret
  }
}
