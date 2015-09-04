// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b13738
{
}
.class ILGEN_4 {
 .method static int32 main() {
.entrypoint
.maxstack	10
.try
{
 ldc.i4 2
 ldc.i4 0xF0430a02
 mul.ovf.un
 pop
 leave erro
}
catch [mscorlib]System.OverflowException
{
 pop
 leave ok
}

erro:
 ldc.i4 1
 br    exit
ok:
 ldc.i4 100
exit:
 ret
}
}
