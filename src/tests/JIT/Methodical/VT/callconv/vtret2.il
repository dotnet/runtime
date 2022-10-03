// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.




.file alignment 512

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'vtret2' // as "vtret"
{
}
.assembly extern xunit.core {}
// MVID: {DAD31C3D-EF49-47EA-945E-72055ACE0D58}
.namespace MS_vtret2_il
{
  .class value public auto ansi sealed VT
         extends [mscorlib]System.ValueType
  {
    .field private int32 m_n
    .field private value class MS_vtret2_il.VT[] m_dummyGCRef
    .method private hidebysig specialname rtspecialname 
            instance void  .ctor(int32 n) il managed
    {
      // Code size       21 (0x15)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldarg.1
      IL_0002:  stfld      int32 MS_vtret2_il.VT::m_n
      IL_0007:  ldarg.0
      IL_0008:  ldc.i4.s   10
      IL_000a:  newarr     MS_vtret2_il.VT
      IL_000f:  stfld      value class MS_vtret2_il.VT[] MS_vtret2_il.VT::m_dummyGCRef
      IL_0014:  ret
    } // end of method VT::.ctor

    .method private hidebysig static value class MS_vtret2_il.VT 
            'add'(value class MS_vtret2_il.VT me,
                  value class MS_vtret2_il.VT what) il managed
    {
      // Code size       27 (0x1b)
      .maxstack  8
      IL_0000:  ldarga.s   me
      IL_0002:  dup
      IL_0003:  ldfld      int32 MS_vtret2_il.VT::m_n
      IL_0008:  ldarga.s   what
      IL_000a:  ldfld      int32 MS_vtret2_il.VT::m_n
      IL_000f:  add
      IL_0010:  stfld      int32 MS_vtret2_il.VT::m_n
      IL_0019:  ldarg.0
      IL_001a:  ret
    } // end of method VT::'add'

    .method private hidebysig static value class MS_vtret2_il.VT 
            'sub'(value class MS_vtret2_il.VT me,
                  value class MS_vtret2_il.VT what) il managed
    {
      // Code size       27 (0x1b)
      			.maxstack  8
      			ldarg.0
      			ldarga.s what
      			dup
      			ldfld int32 MS_vtret2_il.VT::m_n
      			neg
      			stfld int32 MS_vtret2_il.VT::m_n
      			ldarg.1
      			ldftn value class MS_vtret2_il.VT MS_vtret2_il.VT::'add'(value class MS_vtret2_il.VT me, value class MS_vtret2_il.VT what)
      			tail. calli value class MS_vtret2_il.VT(value class MS_vtret2_il.VT me, value class MS_vtret2_il.VT what)
      			ret
    } // end of method VT::'sub'

    .method public hidebysig static int32
            Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      // Code size       46 (0x2e)
      .maxstack  2
      .locals (value class MS_vtret2_il.VT V_0,
               value class MS_vtret2_il.VT V_1,
               int32 V_2)
      IL_0000:  ldloca.s   V_0
      IL_0002:  ldc.i4.s   100
      IL_0004:  call       instance void MS_vtret2_il.VT::.ctor(int32)
      IL_0009:  ldloca.s   V_1
      IL_000b:  ldc.i4.s   99
      IL_000d:  call       instance void MS_vtret2_il.VT::.ctor(int32)
      IL_0012:  ldloc.0
      IL_0013:  ldloc.1
      IL_0014:  call       value class MS_vtret2_il.VT MS_vtret2_il.VT::'add'(value class MS_vtret2_il.VT,
                                                          value class MS_vtret2_il.VT)
      IL_0019:  stloc.0
      IL_001a:  ldloc.0
      IL_001b:  ldloc.1
      IL_001c:  call       value class MS_vtret2_il.VT MS_vtret2_il.VT::'sub'(value class MS_vtret2_il.VT,
                                                          value class MS_vtret2_il.VT)
      IL_0021:  stloc.0
      IL_0022:  ldloca.s   V_0
      IL_0024:  ldfld      int32 MS_vtret2_il.VT::m_n
      IL_0029:  stloc.2
      IL_002a:  br.s       IL_002c

      IL_002c:  ldloc.2
      IL_002d:  ret
    } // end of method VT::Main

  } // end of class VT

} // end of namespace MS_vtret2_il

//*********** DISASSEMBLY COMPLETE ***********************
