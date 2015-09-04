// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly extern mscorlib { auto }
.assembly b475598 {}
.module b475589.exe

.class Program
{
    .method static void Nasty()
    {
       .maxstack 2

       call void [mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::PrepareConstrainedRegions()
       br start_try

       start_catch:
           ldstr "Cathed!"
           call void [System.Console]System.Console::WriteLine(string)
           leave.s end
       end_catch:

       start_try:
           newobj instance void [mscorlib]System.Exception::.ctor()
           throw
       end_try:

       end:
       ret

       .try  start_try to end_try catch [mscorlib]System.Exception handler start_catch to end_catch    
    }
    .method static int32 Main(string[] args)
    {
        .maxstack 2
        .entrypoint


        start_try:

	call void Program::Nasty()
        leave.s end_failure

        end_try:
        start_catch:

        leave.s    end_success
        end_catch:

        end_success:
        ldstr "Test SUCCESS"
        call void [System.Console]System.Console::WriteLine(string)
        ldc.i4 100
        ret 

        end_failure:
        ldstr "Test FAILURE"
        call void [System.Console]System.Console::WriteLine(string)
        ldc.i4 -1
        ret 

	.try  start_try to end_try catch [mscorlib]System.NotSupportedException handler start_catch to end_catch    
    }
}