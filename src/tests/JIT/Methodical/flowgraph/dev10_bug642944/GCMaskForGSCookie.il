// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

// Metadata version: v4.0.x86chk
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
}
.assembly 'GCMaskForGSCookie'
{
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.assembly extern xunit.core {}

// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi beforefieldinit JumpBug
       extends [mscorlib]System.Object
{
  .method private hidebysig instance string 
          JumpCallTarget() cil managed noinlining
  {
    // Code size       17 (0x11)
    .maxstack  8
    IL_0000:  ldstr      "In JumpCallTarget, this={0}"
    IL_0005:  ldarg.0
    IL_0006:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_000b:  ldstr      "JumpCall"
    IL_0010:  ret
  } // end of method JumpBug::JumpCallTarget

  .method private hidebysig instance string 
          BugRepro() cil managed noinlining
  {
    // Code size       5 (0x5)
    .maxstack  8
    IL_0000:  jmp       instance string JumpBug::JumpCallTarget()
  } // end of method JumpBug::BugRepro

  .method public hidebysig static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       20 (0x14)
    .maxstack  1
    .locals init (class JumpBug V_0,
             string V_1)
    IL_0000:  newobj     instance void JumpBug::.ctor()
    IL_0005:  stloc.0
    IL_0006:  ldloc.0
    IL_0007:  callvirt   instance string JumpBug::BugRepro()
    IL_000c:  stloc.1
    IL_000d:  ldloc.1
    IL_000e:  call       void [System.Console]System.Console::WriteLine(string)
ldc.i4 100
    IL_0013:  ret
  } // end of method JumpBug::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method JumpBug::.ctor

} // end of class JumpBug

