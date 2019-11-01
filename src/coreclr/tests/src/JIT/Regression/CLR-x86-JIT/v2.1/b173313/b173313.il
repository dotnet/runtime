// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.





// Metadata version: v2.0.50727
.assembly extern mscorlib
{
}
.assembly a
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module a.exe
// MVID: {36FD279A-AF08-423E-A45C-4915EAB873CD}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x00B30000


// =============== CLASS MEMBERS DECLARATION ===================

.class private auto ansi beforefieldinit A
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method A::.ctor

} // end of class A

.class private auto ansi beforefieldinit B
       extends A
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void A::.ctor()
    IL_0006:  ret
  } // end of method B::.ctor

} // end of class B

.class private auto ansi beforefieldinit My
       extends B
{
  .method private hidebysig static object 
          foo<T>(bool b,
                 object o) cil managed
  {
    // Code size       28 (0x1c)
    .maxstack  2
    .locals init (object V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  brtrue.s   IL_0011

    IL_0004:  ldarg.1
    IL_0005:  unbox.any  !!T
    IL_000a:  box        !!T
    IL_000f:  br.s       IL_001b

    IL_0011:  ldarg.1
    IL_0012:  castclass  !!T[]

    IL_001b:  ret
  } // end of method My::foo

  .method private hidebysig static int32 Main() cil managed
  {
    .entrypoint
    // Code size       10 (0xa)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldc.i4.0
    IL_0002:  ldnull
    IL_0003:  call       object My::foo<class A>(bool,
                                                 object)
    IL_0008:  pop
    IL_0009:  ldc.i4 100
    IL_000a:  ret
  } // end of method My::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void B::.ctor()
    IL_0006:  ret
  } // end of method My::.ctor

} // end of class My


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file a.res
