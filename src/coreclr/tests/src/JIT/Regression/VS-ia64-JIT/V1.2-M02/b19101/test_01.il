// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern mscorlib{}
.assembly double_ulong_e_1{}

  .class public auto ansi beforefieldinit Driver extends [mscorlib]System.Object
  {
    .method public hidebysig static int32 Main() cil managed
    {
      .entrypoint

	ldc.r8     1.
	conv.u8
	dup
	call       void [System.Console]System.Console::WriteLine(int64)

	ldc.i4.1
	conv.i8
	bne.un.s   IL_1d

	ldstr      "Pass"
	call       void [System.Console]System.Console::WriteLine(string)
	ldc.i4 100
	ret
	
IL_1d:	ldstr      "Fail"
	call       void [System.Console]System.Console::WriteLine(string)
	ldc.i4 1
	ret
    }
  }
