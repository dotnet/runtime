// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly volatilstind {}
.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.method public static void f()
{
.locals init (int32* V_0)
ldloc.0
ldc.i4 1
volatile. stind.i4
ret
}
.method public static int32 Main()
{
.entrypoint
.try
{
call void f()
leave FAIL
}
catch [mscorlib]System.NullReferenceException
{
leave PASS
}
FAIL:
ldc.i4 1
ret
PASS:
ldc.i4 100
ret
}
