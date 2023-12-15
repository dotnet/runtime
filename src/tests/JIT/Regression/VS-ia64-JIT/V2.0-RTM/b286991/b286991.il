// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}

.assembly extern mscorlib {}
.assembly 'b286991' {}

.class public auto ansi Test_b286991 extends [mscorlib] System.Object {
.method public static int32 Main()
{
   .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
       01 00 00 00
   )
   .entrypoint
 
   ldc.i4 0x40
   call int32 isupper(int32)
   
   call void [System.Console]System.Console::WriteLine(int32)

   ldstr "Test SUCCESS"
   call void [System.Console]System.Console::WriteLine(string)

   ldc.i4 100
   ret
}
}

.method public static pinvokeimpl("msvcrt.dll" cdecl)
    int32 isupper(int32) cil managed preservesig
{
   .custom instance void [mscorlib]System.Security.SuppressUnmanagedCodeSecurityAttribute::ctor() = (01 00 00 00)
}

