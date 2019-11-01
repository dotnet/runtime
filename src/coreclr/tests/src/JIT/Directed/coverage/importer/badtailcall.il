// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly badtailcall {}
.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.method public static int32 f()
{
ldc.i4 100
ret
}
.method public static int32 f0()
{
ldc.i4 1
tail. call int32 f()
ret
}
.method public static int32 Main()
{
.entrypoint
.try
{
call int32 f()
leave.s FAIL
}
catch [mscorlib]System.Security.VerificationException
{
leave.s PASS
}
FAIL:
ldc.i4 100
ret
PASS:
ldc.i4 100
ret
}
