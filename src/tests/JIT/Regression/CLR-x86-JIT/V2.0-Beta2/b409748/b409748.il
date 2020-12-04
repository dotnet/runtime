// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { auto }
.assembly calldef { }
.module calldef.exe
.method public static vararg void Foo(int32)
{
   ret
}
.method public static int32 Exec()
{
   .entrypoint

   ldc.i4 1
   call mdtoken(0x06000001)

   ldc.i4 100
   ret
}
