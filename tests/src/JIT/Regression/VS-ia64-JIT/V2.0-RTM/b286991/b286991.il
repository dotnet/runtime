// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly extern mscorlib {}
.assembly test {}

.method public static int32 Main()
{
   .entrypoint
 
   ldc.i4 0x40
   call int32 isupper(int32)
   
   call void [System.Console]System.Console::WriteLine(int32)

   ldstr "Test SUCCESS"
   call void [System.Console]System.Console::WriteLine(string)

   ldc.i4 100
   ret
}

.method public static pinvokeimpl("msvcrt.dll" cdecl)
    int32 isupper(int32) cil managed preservesig
{
   .custom instance void [mscorlib]System.Security.SuppressUnmanagedCodeSecurityAttribute::ctor() = (01 00 00 00)
}

