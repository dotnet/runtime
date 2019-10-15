// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly cprop_test {}
.assembly extern mscorlib{auto}
.class cprop_test {
.method static int32 Main() {
.entrypoint
.maxstack	5

     ldc.i4.2
     ldc.i4.0
     ldc.i4.2
    sub
     dup
     call       void [System.Console]System.Console::WriteLine(int32)


bne.un IL_01
// this is to make the errorlevel 100 if the answer is right ( ie 0)
IL_02:
    ldc.i4 1
    ret
IL_01: 
   ldstr "pass"
   call       void [System.Console]System.Console::WriteLine(string)
   ldc.i4 100

 ret
}
}
