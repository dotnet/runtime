// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern xunit.core {}

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 0:0:0:0
}
.assembly 'dev10_493135'
{
  .ver 0:0:0:0
}
// MVID: {EAE2209C-4B54-47C6-96C2-EC2352FC6386}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x00320000


// =============== CLASS MEMBERS DECLARATION ===================

.class private auto ansi beforefieldinit CustomMemStream
       extends [mscorlib]System.IO.MemoryStream
{
  .method public hidebysig virtual instance void 
          Close() cil managed
  {
    .override [mscorlib]System.IO.MemoryStream::Close
    // Code size       20 (0x14)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  call       instance void [mscorlib]System.IO.Stream::Close()
    IL_0007:  nop
    IL_0008:  ldstr      "Successfully called CustomStream.Close()"
    IL_000d:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0012:  nop
    IL_0013:  ret
  } // end of method CustomMemStream::Close

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.IO.MemoryStream::.ctor()
    IL_0006:  ret
  } // end of method CustomMemStream::.ctor

} // end of class CustomMemStream

.class public auto ansi beforefieldinit Program
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       15 (0xf)
    .maxstack  1
    .locals init (class [mscorlib]System.IO.MemoryStream V_0)
    IL_0000:  nop
    IL_0001:  newobj     instance void CustomMemStream::.ctor()
    IL_0006:  stloc.0
    IL_0007:  ldloc.0
    IL_0008:  callvirt   instance void [mscorlib]System.IO.Stream::Close()
    IL_000d:  nop
			  ldc.i4 100	
    IL_000e:  ret
  } // end of method Program::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Program::.ctor

} // end of class Program


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
