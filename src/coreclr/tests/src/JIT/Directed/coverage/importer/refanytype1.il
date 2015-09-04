// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly refanytype1 {}
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
