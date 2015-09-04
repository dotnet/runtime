// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly box_unbox.exe{}
.class public value sealed valClass {
.field public int32 Field1
}
.class public Box_Unbox {
.field public static	value class valClass vc
.method public static int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack	10
	ldsfld 	value class valClass Box_Unbox::vc
        pop
	ldc.i4	100
	ret
}
}
