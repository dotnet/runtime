// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}





// Metadata version: v4.0.x86chk
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly t
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module t.exe
// MVID: {0BE09AE7-5603-4318-A13B-9A8950F8ED14}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x00760000


// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi sealed mynamespace.e_t
       extends [mscorlib]System.Enum
{
  .field public specialname rtspecialname int16 value__
  .field public static literal valuetype mynamespace.e_t one = int16(0x0000)
  .field public static literal valuetype mynamespace.e_t two = int16(0x0001)
  .field public static literal valuetype mynamespace.e_t three = int16(0x0002)
} // end of class mynamespace.e_t

.class private auto ansi beforefieldinit mynamespace.myclass
       extends [mscorlib]System.Object
{
  .method public hidebysig static int16  foo(int16& x) cil managed noinlining
  {
    // Code size       19 (0x13)
    .maxstack  8
    IL_0000:  ldstr      "{0}"
    IL_0005:  ldarg.0
    IL_0006:  ldind.i2
    IL_0007:  box        mynamespace.e_t
    IL_000c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_0011:  ldc.i4.2
    IL_0012:  ret
  } // end of method myclass::foo

  .method public hidebysig static int32 Main() cil managed
  {
    .entrypoint
    // Code size       16 (0x10)
    .maxstack  1
    .locals init (valuetype mynamespace.e_t V_0)
    IL_0000:  ldc.i4.1
    IL_0001:  stloc.0
    IL_0002:  ldloca.s   V_0
    IL_0004:  ldflda     int16 mynamespace.e_t::value__
    IL_0009:  call       int16 mynamespace.myclass::foo(int16&)
    IL_000e:  pop
ldc.i4 100
    IL_000f:  ret
  } // end of method myclass::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method myclass::.ctor

} // end of class mynamespace.myclass


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
