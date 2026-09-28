// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { auto }
.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern xunit.core {}
.assembly 'b409748' { }
.assembly extern Microsoft.DotNet.XUnitExtensions { .publickeytoken = (31 BF 38 56 AD 36 4E 35 ) }
.assembly extern TestLibrary { .ver 0:0:0:0 }

.method public static vararg void Foo(int32)
{
   ret
}
.class public auto ansi Test_b409748 extends [mscorlib] System.Object {
.method public static int32 Exec()
{
   .custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ConditionalFactAttribute::.ctor(class [System.Runtime]System.Type, string[]) = {
       type([TestLibrary]TestLibrary.PlatformDetection)
       string[1] ('IsVarArgSupported')
   }
   .entrypoint

   ldc.i4 1
   call mdtoken(0x06000001)

   ldc.i4 100
   ret
}
}
