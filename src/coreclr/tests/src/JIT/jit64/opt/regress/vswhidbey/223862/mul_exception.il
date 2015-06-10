// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly cprop_test {}
.assembly extern mscorlib{auto}
.class cprop_test {
.method static int32 Main() {
.entrypoint
.maxstack	5

	.try
	{
     ldc.i4.7
     ldc.i4.0
     ldc.i4.3
    sub
     dup
     call       void [mscorlib]System.Console::WriteLine(int32)


   mul.ovf.un
     dup
     call       void [mscorlib]System.Console::WriteLine(int32)
     leave IL_02
   }
	catch [mscorlib]System.OverflowException
	{
		pop
		ldstr "Pass"
		call       void [mscorlib]System.Console::WriteLine(string)
		leave IL_01
	} 
// this is to make the errorlevel 100 if the answer is right ( ie 0)
IL_02:
    ldc.i4 1
    ret
IL_01: // pass
   ldc.i4 100

 ret
}
}
