
//  Microsoft (R) .NET Framework IL Disassembler.  Version 1.0.3705.0
//  Copyright (C) Microsoft Corporation 1998-2001. All rights reserved.

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 1:0:3300:0
}
.assembly 'qt-instance'
{
  // --- The following custom attribute is added automatically, do not uncomment -------
  //  .custom instance void [mscorlib]System.Diagnostics.DebuggableAttribute::.ctor(bool,
  //                                                                                bool) = ( 01 00 00 01 00 00 ) 
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module 'qt-instance.exe'
// MVID: {187EE85D-D172-46C8-B3FC-38A5DA20CC99}
.imagebase 0x00400000
.subsystem 0x00000003
.file alignment 512
.corflags 0x00000001
// Image base: 0x00a70000
//
// ============== CLASS STRUCTURE DECLARATION ==================
//
.class private sequential ansi sealed beforefieldinit QNull
       extends [mscorlib]System.ValueType
{
} // end of class QNull

.class public auto ansi beforefieldinit Testing
       extends [mscorlib]System.Object
{
} // end of class Testing


// =============================================================


// =============== GLOBAL FIELDS AND METHODS ===================


// =============================================================


// =============== CLASS MEMBERS DECLARATION ===================
//   note that class flags, 'extends' and 'implements' clauses
//          are provided here for information only

.class private sequential ansi sealed beforefieldinit QNull
       extends [mscorlib]System.ValueType
{
  .pack 0
  .size 1
  .field private static valuetype QNull 'instance'
  .method assembly hidebysig specialname static 
          valuetype QNull  get_Instance() cil managed
  {
    // Code size       10 (0xa)
    .maxstack  1
    .locals init (valuetype QNull V_0)
    IL_0000:  ldsfld     valuetype QNull QNull::'instance'
    IL_0005:  stloc.0
    IL_0006:  br.s       IL_0008

    IL_0008:  ldloc.0
    IL_0009:  ret
  } // end of method QNull::get_Instance

  .property valuetype QNull Instance()
  {
    .get valuetype QNull QNull::get_Instance()
  } // end of property QNull::Instance
} // end of class QNull

.class public auto ansi beforefieldinit Testing
       extends [mscorlib]System.Object
{
  .method public hidebysig static void  Main() cil managed
  {
    .entrypoint
    // Code size       12 (0xc)
    .maxstack  1
    .locals init (object V_0)
    IL_0000:  call       valuetype QNull QNull::get_Instance()
    IL_0005:  box        QNull
    IL_000a:  stloc.0
    IL_000b:  ret
  } // end of method Testing::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Testing::.ctor

} // end of class Testing


// =============================================================

//*********** DISASSEMBLY COMPLETE ***********************
