// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern mscorlib {}
.assembly volatile.exe{}
.class public _volatile
{
  .field public static native int PTR
  .method public static int32 main(class [mscorlib]System.String[])
  {
    .entrypoint
    .locals	()
    .maxstack	2
 	ldc.i4	0xFFAAFFAA
	conv.i
	stsfld	native int _volatile::PTR
	ldsflda	native int _volatile::PTR
	volatile.
	ldind.i
	ldc.i4	0xFFAAFFAA
	conv.i
	bne.un	FAIL
  PASS:
	ldc.i4 100
	ret
  FAIL:
	ldc.i4	0x0
	ret
  }
}
