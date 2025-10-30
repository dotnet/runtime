// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { auto }
.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern xunit.core {}
.assembly 'b409748' { }
.assembly extern TestLibrary { .ver 0:0:0:0 }

.method public static vararg void Foo(int32)
{
   ret
}
.class public auto ansi Test_b409748 extends [mscorlib] System.Object {
.method public static int32 Main()
{
   .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
      01 00 00 00
   )
   .entrypoint
   .maxstack  1
   call       bool [TestLibrary]TestLibrary.PlatformDetection::get_IsVarArgSupported()
   brtrue.s   DoTest
   ldc.i4     100
   ret

DoTest:
   call       int32 Test_b409748::Test()
   ret
}

.method public static int32 Test() il managed
{
   ldc.i4 1
   call mdtoken(0x06000001)

   ldc.i4 100
   ret
}
}
