// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.




.file alignment 512

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly vtret // as "vtret"
{
}
.assembly extern xunit.core {}
.module vtret.exe
// MVID: {DAD31C3D-EF49-47EA-945E-72055ACE0D58}
.namespace MS
{
  .class value private auto ansi sealed VT
         extends [mscorlib]System.ValueType
  {
    .field private int32 m_n
    .field private value class MS.VT[] m_dummyGCRef
    .method private hidebysig specialname rtspecialname 
            instance void  .ctor(int32 n) il managed
    {
      // Code size       21 (0x15)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldarg.1
      IL_0002:  stfld      int32 MS.VT::m_n
      IL_0007:  ldarg.0
      IL_0008:  ldc.i4.s   10
      IL_000a:  newarr     MS.VT
      IL_000f:  stfld      value class MS.VT[] MS.VT::m_dummyGCRef
      IL_0014:  ret
    } // end of method VT::.ctor

    .method private hidebysig static value class MS.VT 
            'add'(value class MS.VT me,
                  value class MS.VT what) il managed
    {
      // Code size       27 (0x1b)
      .maxstack  8
      IL_0000:  ldarga.s   me
      IL_0002:  dup
      IL_0003:  ldfld      int32 MS.VT::m_n
      IL_0008:  ldarga.s   what
      IL_000a:  ldfld      int32 MS.VT::m_n
      IL_000f:  add
      IL_0010:  stfld      int32 MS.VT::m_n
      IL_0019:  ldarg.0
      IL_001a:  ret
    } // end of method VT::'add'

    .method private hidebysig static value class MS.VT 
            'sub'(value class MS.VT me,
                  value class MS.VT what) il managed
    {
      // Code size       27 (0x1b)
      			.maxstack  8
      			ldarga.s what
      			dup
      			ldfld int32 MS.VT::m_n
      			neg
      			stfld int32 MS.VT::m_n
      			ldarg.0
      			ldarg.1
      			tail. call value class MS.VT MS.VT::'add'(value class MS.VT me, value class MS.VT what)
      			ret
    } // end of method VT::'sub'

    .method private hidebysig static int32
            Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      // Code size       46 (0x2e)
      .maxstack  2
      .locals (value class MS.VT V_0,
               value class MS.VT V_1,
               int32 V_2)
      IL_0000:  ldloca.s   V_0
      IL_0002:  ldc.i4.s   100
      IL_0004:  call       instance void MS.VT::.ctor(int32)
      IL_0009:  ldloca.s   V_1
      IL_000b:  ldc.i4.s   99
      IL_000d:  call       instance void MS.VT::.ctor(int32)
      IL_0012:  ldloc.0
      IL_0013:  ldloc.1
      IL_0014:  call       value class MS.VT MS.VT::'add'(value class MS.VT,
                                                          value class MS.VT)
      IL_0019:  stloc.0
      IL_001a:  ldloc.0
      IL_001b:  ldloc.1
      IL_001c:  call       value class MS.VT MS.VT::'sub'(value class MS.VT,
                                                          value class MS.VT)
      IL_0021:  stloc.0
      IL_0022:  ldloca.s   V_0
      IL_0024:  ldfld      int32 MS.VT::m_n
      IL_0029:  stloc.2
      IL_002a:  br.s       IL_002c

      IL_002c:  ldloc.2
      IL_002d:  ret
    } // end of method VT::Main

  } // end of class VT

} // end of namespace MS

//*********** DISASSEMBLY COMPLETE ***********************
