// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { auto }
.assembly extern xunit.core {}
.assembly 'b409748' { }
.method public static vararg void Foo(int32)
{
   ret
}
.class public auto ansi Test_b409748 extends [mscorlib] System.Object {
.method public static int32 Exec()
{
   .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
       01 00 00 00
   )
   .entrypoint

   ldc.i4 1
   call mdtoken(0x06000001)

   ldc.i4 100
   ret
}
}
