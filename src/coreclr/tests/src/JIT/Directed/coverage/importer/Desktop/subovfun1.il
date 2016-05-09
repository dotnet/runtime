// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


.assembly extern legacy library mscorlib {}
.assembly subovfun1 { }
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