// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.





// Metadata version: v4.0.x86chk
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly 'twoEndFinallys'
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.assembly extern xunit.core {}
// MVID: {932CB6F6-63A8-4E1D-B39F-197DFCB35D3E}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x00350000


// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi beforefieldinit Test_twoEndFinallys
       extends [mscorlib]System.Object
{
  .field private int32 temp
  .field private static int32 result
  .method private hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       15 (0xf)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ldarg.0
    IL_0007:  ldc.i4.s   101
    IL_0009:  stfld      int32 Test_twoEndFinallys::temp
    IL_000e:  ret
  } // end of method Test::.ctor

  .method public hidebysig static int32 
          Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       23 (0x17)
    .maxstack  2
    .locals init (class Test_twoEndFinallys V_0)
              ldc.i4.s   0
    IL_0000:  newarr     [mscorlib]System.String
    IL_0001:  call       class Test_twoEndFinallys Test_twoEndFinallys::TwoEndFinallys(string[])
    IL_0006:  stloc.0
    IL_0007:  ldloc.0
    IL_0008:  ldfld      int32 Test_twoEndFinallys::temp
    IL_000d:  ldc.i4.s   101
    IL_000f:  bne.un.s   IL_0014

    IL_0011:  ldc.i4.s   100
    IL_0013:  ret

    IL_0014:  ldc.i4.s   101
    IL_0016:  ret
  } // end of method Test::Main

  .method private hidebysig static class Test_twoEndFinallys 
          TwoEndFinallys(string[] args) cil managed
  {
    // Code size       136 (0x88)
    .maxstack  3
    .locals init (class Test_twoEndFinallys V_0,
             class Test_twoEndFinallys V_1,
             int32 V_2,
             class Test_twoEndFinallys V_3,
             class [mscorlib]System.Exception V_4,
             class Test_twoEndFinallys V_5,
             int32 V_6)
    IL_0000:  ldnull
    IL_0001:  stloc.0
    IL_0002:  ldnull
    IL_0003:  stloc.1
    .try
    {
      IL_0004:  ldc.i4.0
      IL_0005:  stloc.2
      IL_0006:  ldnull
      IL_0007:  stloc.3
      IL_0008:  ldsfld     int32 Test_twoEndFinallys::result
      IL_000d:  ldc.i4     0x3e8
      IL_0012:  beq.s      IL_003e

      IL_0014:  ldloc.2
      IL_0015:  brfalse.s  IL_002e

      IL_0017:  newobj     instance void Test_twoEndFinallys::.ctor()
      IL_001c:  stloc.0
      IL_001d:  ldloc.0
      IL_001e:  ldloc.3
      IL_001f:  ldfld      int32 Test_twoEndFinallys::temp
      IL_0024:  stfld      int32 Test_twoEndFinallys::temp
      IL_0029:  ldloc.0
      IL_002a:  stloc.s    V_5
      IL_002c:  leave.s    IL_0085

      .try
      {
        IL_002e:  newobj     instance void Test_twoEndFinallys::.ctor()
        IL_0033:  stloc.s    V_5
        IL_0035:  leave.s    IL_0085

      }  // end .try
      catch [mscorlib]System.Exception 
      {
        IL_0037:  stloc.s    V_4
        IL_0039:  ldnull
        IL_003a:  stloc.s    V_5
        IL_003c:  leave.s    IL_0085

      }  // end handler
      IL_003e:  leave.s    IL_0083

    }  // end .try
    finally
    {
      IL_0040:  ldarg.0
      IL_0041:  ldlen
      IL_0042:  conv.i4
      IL_0043:  stloc.s    V_6
      IL_0045:  ldloc.s    V_6
      IL_0047:  ldc.i4.0
      IL_0048:  bne.un.s   IL_0065

      IL_004a:  ldc.i4.0
      IL_004b:  call       class Test_twoEndFinallys Test_twoEndFinallys::dummyCall(int32)
      IL_0050:  stloc.1
      IL_0051:  ldloc.1
      IL_0052:  brfalse.s  IL_0082

      IL_0054:  ldloc.1
      IL_0055:  dup
      IL_0056:  ldfld      int32 Test_twoEndFinallys::temp
      IL_005b:  ldc.i4.s   100
      IL_005d:  add
      IL_005e:  stfld      int32 Test_twoEndFinallys::temp
/* Made modifications below, added these three statements instead of the br.s*/
/*      IL_0063:  br.s       IL_0082 */

/*--------------------------Replaced above instruction with these----------*/
IL_0063: ldnull 
    L_0110: pop 
    L_0111: endfinally 
/*----------------End of Replaced above instruction with these------------*/

      IL_0065:  ldarg.0
      IL_0066:  ldc.i4.0
      IL_0067:  ldelem.ref
      IL_0068:  call       int32 [mscorlib]System.Convert::ToInt32(string)
      IL_006d:  call       class Test_twoEndFinallys Test_twoEndFinallys::dummyCall(int32)
      IL_0072:  stloc.0
      IL_0073:  ldloc.0
      IL_0074:  dup
      IL_0075:  ldfld      int32 Test_twoEndFinallys::temp
      IL_007a:  ldc.i4.s   101
      IL_007c:  add
      IL_007d:  stfld      int32 Test_twoEndFinallys::temp
      IL_0082:  endfinally
    }  // end handler
    IL_0083:  ldloc.1
    IL_0084:  ret

    IL_0085:  ldloc.s    V_5
    IL_0087:  ret
  } // end of method Test::TwoEndFinallys

  .method private hidebysig static class Test_twoEndFinallys 
          dummyCall(int32 input) cil managed noinlining
  {
    // Code size       17 (0x11)
    .maxstack  1
    .locals init (class Test_twoEndFinallys V_0)
    IL_0000:  ldarg.0
    IL_0001:  brfalse.s  IL_000b

    IL_0003:  newobj     instance void Test_twoEndFinallys::.ctor()
    IL_0008:  stloc.0
    IL_0009:  leave.s    IL_000f

    IL_000b:  ldnull
    IL_000c:  stloc.0
    IL_000d:  leave.s    IL_000f

    IL_000f:  ldloc.0
    IL_0010:  ret
  } // end of method Test::dummyCall

  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldc.i4.s   100
    IL_0002:  stsfld     int32 Test_twoEndFinallys::result
    IL_0007:  ret
  } // end of method Test::.cctor

} // end of class Test


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file o.res
