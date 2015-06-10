// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly cprop_test {}
.assembly extern mscorlib{auto}
.class cprop_test {
.method static int32 Main() {
.entrypoint
.maxstack	5

     ldc.i4.7
     ldc.i4.3
     ldc.i4.0
    sub
     dup
     call       void [mscorlib]System.Console::WriteLine(int32)


   mul.ovf.un
     dup
     call       void [mscorlib]System.Console::WriteLine(int32)
// this is to make the errorlevel 100 if the answer is right ( ie 21)
   ldc.i4 79
   add

 ret
}
}
