// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly extern mscorlib{}

.assembly b471305
{

    // This makes JIT to compile the debuggable code.
    .custom instance void [mscorlib]System.Diagnostics.DebuggableAttribute::.ctor(valuetype [mscorlib]System.Diagnostics.DebuggableAttribute/DebuggingModes) = ( 01 00 01 01 00 00 00 00 ) 
}
.module b471305.exe

.class sealed Program
{
   .field static int32 foo

  .method void  Test() noinlining
  {
    .maxstack  2
    .locals init (object& V_0, object V_1)

    newobj instance void object::.ctor()
    stloc.1 

    // This causes the address of a local var to be taken.
    // As a result, JIT won't enregister this local var.
    ldloca V_1
    stloc.0 

    // Now do a "byref = ref"
    ldloc.1
    stloc.0

    // Use loc.0 more so it can be enregistered.   
    ldloc.0
    stsfld int32 Program::foo
    ldsfld int32 Program::foo
    call       void [System.Console]System.Console::WriteLine(int32)
  
    ret
  } 

 
  .method static int32 Main(string[] args)
  {
    .entrypoint
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  nop
            newobj instance void Program::.ctor()
    IL_0001:  call       instance void Program::Test()
    IL_0006:  nop
    IL_0007:  ldstr      "Test SUCCESS"
    IL_000c:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0011:  nop
    IL_0012:  ldc.i4.s   100
    IL_0014:  stloc.0
    IL_0015:  br.s       IL_0017 

    IL_0017:  ldloc.0
    IL_0018:  ret
  } 

  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
       .maxstack 8
       ldarg.0 
       call instance void object::.ctor()
       ret
  }
}
