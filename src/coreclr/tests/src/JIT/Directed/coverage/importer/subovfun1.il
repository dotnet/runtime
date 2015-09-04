// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly subovfun1 {}
.method public static uint32 f(uint32 arg)
{
ldarg.0
ldc.i4 1
sub.ovf.un
ret
}
.method public static int32 Main()
{
.entrypoint
ldc.i4 101
call uint32 f(uint32)
conv.i4
ret
}
