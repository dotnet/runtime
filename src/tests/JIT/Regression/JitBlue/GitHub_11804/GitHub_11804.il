// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern System.Numerics.Vectors { auto }
.assembly extern System.Runtime.CompilerServices.Unsafe { auto }
.assembly extern mscorlib { auto }

.assembly GitHub_11804
{
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 )
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                                           63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.

  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.assembly extern xunit.core {}
// MVID: {9CC2AC15-DDA3-4ADF-9566-89AB25A8CF63}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x01590000


.class public abstract auto ansi sealed beforefieldinit C
       extends [System.Runtime]System.Object
{
  .method private hidebysig static int32
          UnsafeGrab() cil managed noinlining
  {
    // Code size       18 (0x12)
    .maxstack  2
    .locals init (valuetype [System.Numerics.Vectors]System.Numerics.Vector`1<int32> V_0)
    IL_0000:  ldloca.s   V_0
    IL_0002:  ldc.i4.s   50
    IL_0004:  call       instance void valuetype [System.Numerics.Vectors]System.Numerics.Vector`1<int32>::.ctor(!0)
    IL_0009:  ldloca.s   V_0
    IL_000b:  call       !!1& [System.Runtime.CompilerServices.Unsafe]System.Runtime.CompilerServices.Unsafe::As<valuetype [System.Numerics.Vectors]System.Numerics.Vector`1<int32>,int32>(!!0&)
    IL_0010:  ldind.i4
    IL_0011:  ret
  } // end of method C::UnsafeGrab

  .method private hidebysig static int32
          IndexerGrab() cil managed noinlining
  {
    // Code size       18 (0x12)
    .maxstack  2
    .locals init (valuetype [System.Numerics.Vectors]System.Numerics.Vector`1<int32> V_0)
    IL_0000:  ldloca.s   V_0
    IL_0002:  ldc.i4.s   50
    IL_0004:  call       instance void valuetype [System.Numerics.Vectors]System.Numerics.Vector`1<int32>::.ctor(!0)
    IL_0009:  ldloca.s   V_0
    IL_000b:  ldc.i4.0
    IL_000c:  call       instance !0 valuetype [System.Numerics.Vectors]System.Numerics.Vector`1<int32>::get_Item(int32)
    IL_0011:  ret
  } // end of method C::IndexerGrab

  .method public hidebysig static int32
          Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       20 (0x14)
    .maxstack  8

    IL_0008:  call       int32 C::IndexerGrab()
    IL_000d:  call       int32 C::UnsafeGrab()
    IL_0012:  add
    IL_0013:  ret
  } // end of method C::Main

} // end of class C
