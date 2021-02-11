// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly refanytype1 { }
.method public static int32 f()
{
ldc.i4 100
ret
}
.method public static int32 Main()
{
.entrypoint
call int32 f()
mkrefany int32
refanytype
pop
ldc.i4 100
ret
}
