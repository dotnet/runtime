// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

// This test covers creation and use of a ReadOnlySpan<T> which points at constant data
// using the CreateSpan api.

// Metadata version: v4.0.30319
.assembly extern System.Runtime
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )                         // .?_....:
  .ver 7:0:0:0
}
.assembly extern xunit.core {}
.assembly 'CreateSpan_il'
{
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                                   63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.

  // --- The following custom attribute is added automatically, do not uncomment -------
  //  .custom instance void [System.Runtime]System.Diagnostics.DebuggableAttribute::.ctor(valuetype [System.Runtime]System.Diagnostics.DebuggableAttribute/DebuggingModes) = ( 01 00 02 00 00 00 00 00 ) 

  .permissionset reqmin
             = {[System.Runtime]System.Security.Permissions.SecurityPermissionAttribute = {property bool 'SkipVerification' = bool(true)}}
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
// MVID: {E6E30CA2-CC29-4014-B26A-79C94A13BF58}
.custom instance void [System.Runtime]System.Security.UnverifiableCodeAttribute::.ctor() = ( 01 00 00 00 ) 
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x000001ADF9680000


// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi beforefieldinit CreateSpanTest
       extends [System.Runtime]System.Object
{
  .method public hidebysig static int32 
          Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       47 (0x2f)
    .maxstack  2
    .locals init (int32 V_0,
             valuetype [System.Runtime]System.ReadOnlySpan`1<int32> V_1,
             int32 V_2,
             int32 V_3)
    IL_0000:  ldtoken    field valuetype '<PrivateImplementationDetails>'/'__StaticArrayInitTypeSize=16' '<PrivateImplementationDetails>'::'8A4C0D07C79596FFAE679AE1790CCA491B4E0D51EE259857EC635CA222073650'
    IL_0005:  call       valuetype [System.Runtime]System.ReadOnlySpan`1<!!0> [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::CreateSpan<int32>(valuetype [System.Runtime]System.RuntimeFieldHandle)
    IL_000a:  ldc.i4.0
    IL_000b:  stloc.0
    IL_000c:  stloc.1
    IL_000d:  ldc.i4.0
    IL_000e:  stloc.2
    IL_000f:  br.s       IL_0023

    IL_0011:  ldloca.s   V_1
    IL_0013:  ldloc.2
    IL_0014:  call       instance !0& modreq([System.Runtime]System.Runtime.InteropServices.InAttribute) valuetype [System.Runtime]System.ReadOnlySpan`1<int32>::get_Item(int32)
    IL_0019:  ldind.i4
    IL_001a:  stloc.3
    IL_001b:  ldloc.0
    IL_001c:  ldloc.3
    IL_001d:  add
    IL_001e:  stloc.0
    IL_001f:  ldloc.2
    IL_0020:  ldc.i4.1
    IL_0021:  add
    IL_0022:  stloc.2
    IL_0023:  ldloc.2
    IL_0024:  ldloca.s   V_1
    IL_0026:  call       instance int32 valuetype [System.Runtime]System.ReadOnlySpan`1<int32>::get_Length()
    IL_002b:  blt.s      IL_0011

    IL_002d:  ldloc.0
    IL_002e:  ret
  } // end of method CreateSpanTest::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  ret
  } // end of method CreateSpanTest::.ctor

} // end of class CreateSpanTest

.class private auto ansi sealed '<PrivateImplementationDetails>'
       extends [System.Runtime]System.Object
{
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
  .class explicit ansi sealed nested private '__StaticArrayInitTypeSize=16'
         extends [System.Runtime]System.ValueType
  {
    .pack 4
    .size 16
  } // end of class '__StaticArrayInitTypeSize=16'

  .field static assembly initonly valuetype '<PrivateImplementationDetails>'/'__StaticArrayInitTypeSize=16' '8A4C0D07C79596FFAE679AE1790CCA491B4E0D51EE259857EC635CA222073650' at I_000026F8
} // end of class '<PrivateImplementationDetails>'


// =============================================================

.data cil I_000026F8 = bytearray (
                 19 00 00 00 0F 00 00 00 23 00 00 00 19 00 00 00) // ........#.......
// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file CreateSpan_il.res
