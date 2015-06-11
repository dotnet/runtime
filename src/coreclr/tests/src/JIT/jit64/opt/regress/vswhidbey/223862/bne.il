// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
     call       void [mscorlib]System.Console::WriteLine(int32)


bne.un IL_01
// this is to make the errorlevel 100 if the answer is right ( ie 0)
IL_02:
    ldc.i4 1
    ret
IL_01: 
   ldstr "pass"
   call       void [mscorlib]System.Console::WriteLine(string)
   ldc.i4 100

 ret
}
}
