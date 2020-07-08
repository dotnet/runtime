// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

//Constant Prop phase was not checking for overflow when propagating through a conv.ovf.
//Correct behavior expects the code within the try block to throw an exception.
//Included return code for failure if no exception is thrown.

.assembly Class_test {}
.assembly extern mscorlib{auto}
.class Class_test {
    .method static int32 Main() {
    .entrypoint
    .maxstack	5

    .try
    {

        ldc.i8 0xffffffffffffffff
        conv.u8
        conv.ovf.i1.un
        call       void [System.Console]System.Console::WriteLine(int32)
IL_000c:
        leave.s    IL_02
    }
    catch [mscorlib]System.OverflowException
    {
        pop
        ldstr "Pass"
        call       void [System.Console]System.Console::WriteLine(string)
        leave IL_01
    } 

// this is to make the errorlevel 100 if the answer is right ( ie 0)

IL_02:
    ldc.i4 101
    ret
IL_01: 
    ldc.i4 100
    ret
}  
}
