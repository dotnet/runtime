// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.





// Metadata version: v4.0.31215
.assembly extern mscorlib
{
  .ver 0:0:0:0
}
.assembly jumper
{
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module jumper.exe
// MVID: {01883E3F-A0D8-4DEC-B879-E60FC8CF5843}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x001D0000


// =============== CLASS MEMBERS DECLARATION ===================

.class private auto ansi sealed MS.VT
       extends [mscorlib]System.ValueType
{
  .field private int32 m_n
  .field private valuetype MS.VT[] m_dummyGCRef
  .method private hidebysig specialname rtspecialname 
          instance void  .ctor(int32 n) cil managed
  {
    // Code size       21 (0x15)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  stfld      int32 MS.VT::m_n
    IL_0007:  ldarg.0
    IL_0008:  ldc.i4.s   10
    IL_000a:  newarr     MS.VT
    IL_000f:  stfld      valuetype MS.VT[] MS.VT::m_dummyGCRef
    IL_0014:  ret
  } // end of method VT::.ctor
  

  
  
  
  
  


  .method private hidebysig instance vararg valuetype MS.VT 
          'add'(valuetype MS.VT what) cil managed
  {
    // Code size       27 (0x1b)
    .maxstack  8
    .locals (valuetype MS.VT V_0)
    IL_0000:  ldarg.0
    IL_0001:  dup
    IL_0002:  ldfld      int32 MS.VT::m_n
    IL_0007:  ldarga.s   what
    IL_0009:  ldfld      int32 MS.VT::m_n
    IL_000e:  add
    IL_000f:  stfld      int32 MS.VT::m_n
    IL_0014:  ldarg.0
    IL_0015:  ldobj      MS.VT
    IL_001a:  ret
  } // end of method VT::'add'

  .method private hidebysig instance vararg valuetype MS.VT 
          'sub'(valuetype MS.VT what) cil managed
  {
    // Code size       22 (0x16)
    .maxstack  8
    .locals (valuetype MS.VT V_0)
    IL_0000:  ldarga.s   what
    IL_0002:  dup
    IL_0003:  ldfld      int32 MS.VT::m_n
    IL_0008:  neg
    IL_0009:  stfld      int32 MS.VT::m_n
    IL_000e:  ldarg.0
    IL_000f:  ldarg.1
    IL_0010:  call       instance vararg valuetype MS.VT MS.VT::'add'(valuetype MS.VT)
    IL_0015:  ret
  } // end of method VT::'sub'

  .method private hidebysig static int32 
          Main() cil managed
  {
    .entrypoint
    // Code size       96 (0x60)
    .maxstack  2
    .locals init(valuetype MS.VT V_0,
             valuetype MS.VT V_1,
             valuetype MS.VT V_2,
             int32 V_3)
    IL_0000:  ldloca.s   V_0
    IL_0002:  ldc.i4.s   100
    IL_0004:  call       instance void MS.VT::.ctor(int32)
    IL_0009:  ldloca.s   V_1
    IL_000b:  ldc.i4.s   99
    IL_000d:  call       instance void MS.VT::.ctor(int32)
    IL_0012:  ldloca.s   V_0
    IL_0014:  ldloc.1
    IL_0015:  call       instance vararg valuetype MS.VT MS.VT::'add'(valuetype MS.VT)
    IL_001a:  stloc.0
    IL_001b:  ldloca.s   V_0
    IL_001d:  ldloc.1
    IL_001e:  call       instance vararg valuetype MS.VT MS.VT::'sub'(valuetype MS.VT)
    IL_0023:  stloc.0
    IL_0024:  ldloca.s   V_0
    IL_0026:  ldfld      int32 MS.VT::m_n
    IL_002b:  ldc.i4.s   100
    IL_002d:  beq.s      IL_0039

    IL_002f:  ldloca.s   V_0
    IL_0031:  ldfld      int32 MS.VT::m_n
    IL_0036:  stloc.3
    IL_0037:  br.s       IL_005e

    IL_0039:  ldloca.s   V_2
    IL_003b:  ldc.i4.s   96
    IL_003d:  call       instance void MS.VT::.ctor(int32)
    IL_0042:  ldloca.s   V_0
    IL_0044:  ldloc.2
    IL_0045:  call       instance vararg valuetype MS.VT MS.VT::'add'(valuetype MS.VT)
    IL_004a:  pop
    IL_004b:  ldloca.s   V_0
    IL_004d:  ldloc.2
    IL_004e:  call       instance vararg valuetype MS.VT MS.VT::'sub'(valuetype MS.VT)
    IL_0053:  pop
    IL_0054:  ldloca.s   V_0
    IL_0056:  ldfld      int32 MS.VT::m_n
    IL_005b:  stloc.3
    IL_005c:  br.s       IL_005e

    IL_005e:  ldloc.3
    IL_005f:  ret
  } // end of method VT::Main

} // end of class MS.VT


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
