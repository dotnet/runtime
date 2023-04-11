// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.




// Metadata version: v2.0.50727
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 2:0:0:0
}
.assembly extern xunit.core {}
.assembly extern System
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 2:0:0:0
}
.assembly 'b429039'
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
// MVID: {2F556831-42AA-482A-BFC1-EB0BA22B5FC9}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x02E10000



.class private explicit ansi sealed $compiler_internal$.$Arr$22
       extends [mscorlib]System.ValueType
{
  .pack 1
  .size 22
} // end of class $compiler_internal$.$Arr$22


.class public auto ansi sealed beforefieldinit repro.Units.repro
       extends [mscorlib]System.Object
{

  .field static assembly valuetype $compiler_internal$.$Arr$22 $Array$val$1 at I_0000650C


  .method public static int32  repro() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       151 (0x97)
    .maxstack  3
    .locals init (string V_0,
             char[] V_1,
             class [mscorlib]System.Exception V_2)
    .try
    {
      IL_000e:  ldloc.1
      IL_000f:  ldtoken    field valuetype $compiler_internal$.$Arr$22 repro.Units.repro::$Array$val$1
      IL_0014:  call       void [mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array,
                                                                                                          valuetype [mscorlib]System.RuntimeFieldHandle)
      IL_003c:  leave.s    IL_0096

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0045:  pop
      IL_0094:  leave.s    IL_0096

    }  // end handler
    IL_0096: ldc.i4.s   100
             ret
  } // end of method repro::repro
}


// =============================================================

.data cil I_0000650C = bytearray (
                 61 00 62 00 63 00 64 00 00 00 00 00 00 00 00 00  // a.b.c.d.........
                 00 00 00 00 00 00) 
// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file repro.res
