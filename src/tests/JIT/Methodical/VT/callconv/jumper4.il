// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.




.file alignment 512

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'jumper4' // as "jumper"
{
 //This byte field requests that this assembly not be verified at run time and corresponds to this C# declaration:
  //[assembly:System.Security.Permissions.SecurityPermissionAttribute( [mscorlib]System.Security.Permissions.SecurityAction.RequestMinimum, Flags=System.Security.Permissions.SecurityPermissionFlag.SkipVerification )]
                        }
.assembly extern xunit.core {}
// MVID: {2E4A2D77-5614-4325-A9DC-CBB90842669E}
.namespace MS_jumper4_il
{
  .class value public auto ansi sealed VT
         extends [mscorlib]System.ValueType
  {
    .field private int32 m_n
    .field private value class MS_jumper4_il.VT[] m_dummyGCRef
    .method private hidebysig specialname rtspecialname 
            instance void  .ctor(int32 n) il managed
    {
      // Code size       21 (0x15)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldarg.1
      IL_0002:  stfld      int32 MS_jumper4_il.VT::m_n
      IL_0007:  ldarg.0
      IL_0008:  ldc.i4.s   10
      IL_000a:  newarr     MS_jumper4_il.VT
      IL_000f:  stfld      value class MS_jumper4_il.VT[] MS_jumper4_il.VT::m_dummyGCRef
      IL_0014:  ret
    } // end of method VT::.ctor

    .method private hidebysig instance value class MS_jumper4_il.VT 
            'add'(value class MS_jumper4_il.VT what) il managed
    {
		.maxstack  8
		.locals (value class MS_jumper4_il.VT V_0)
		ldarg.0
		dup
		ldfld      int32 MS_jumper4_il.VT::m_n
		ldarga.s   what
		ldfld      int32 MS_jumper4_il.VT::m_n
		add
		stfld      int32 MS_jumper4_il.VT::m_n
		ldarg.0
		ldobj      MS_jumper4_il.VT
		ret
    } // end of method VT::'add'

    .method private hidebysig instance value class MS_jumper4_il.VT 
            'sub'(value class MS_jumper4_il.VT what) il managed
    {
		.maxstack  8
		.locals (value class MS_jumper4_il.VT V_0)
		ldarga.s   what
		dup
		ldfld      int32 MS_jumper4_il.VT::m_n
		neg
		stfld      int32 MS_jumper4_il.VT::m_n
		jmp instance value class MS_jumper4_il.VT MS_jumper4_il.VT::'add'(value class MS_jumper4_il.VT)
    } // end of method VT::'sub'

    .method public hidebysig static int32
            Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      // Code size       96 (0x60)
      .maxstack  2
      .locals (value class MS_jumper4_il.VT V_0,
               value class MS_jumper4_il.VT V_1,
               value class MS_jumper4_il.VT V_2,
               int32 V_3)
      IL_0000:  ldloca.s   V_0
      IL_0002:  ldc.i4.s   100
      IL_0004:  call       instance void MS_jumper4_il.VT::.ctor(int32)
      IL_0009:  ldloca.s   V_1
      IL_000b:  ldc.i4.s   99
      IL_000d:  call       instance void MS_jumper4_il.VT::.ctor(int32)
      IL_0012:  ldloca.s   V_0
      IL_0014:  ldloc.1
      IL_0015:  call       instance value class MS_jumper4_il.VT MS_jumper4_il.VT::'add'(value class MS_jumper4_il.VT)
      IL_001a:  stloc.0
      IL_001b:  ldloca.s   V_0
      IL_001d:  ldloc.1
      IL_001e:  call       instance value class MS_jumper4_il.VT MS_jumper4_il.VT::'sub'(value class MS_jumper4_il.VT)
      IL_0023:  stloc.0
      IL_0024:  ldloca.s   V_0
      IL_0026:  ldfld      int32 MS_jumper4_il.VT::m_n
      IL_002b:  ldc.i4.s   100
      IL_002d:  beq.s      IL_0039

      IL_002f:  ldloca.s   V_0
      IL_0031:  ldfld      int32 MS_jumper4_il.VT::m_n
      IL_0036:  stloc.3
      IL_0037:  br.s       IL_005e

      IL_0039:  ldloca.s   V_2
      IL_003b:  ldc.i4.s   96
      IL_003d:  call       instance void MS_jumper4_il.VT::.ctor(int32)
      IL_0042:  ldloca.s   V_0
      IL_0044:  ldloc.2
      IL_0045:  call       instance value class MS_jumper4_il.VT MS_jumper4_il.VT::'add'(value class MS_jumper4_il.VT)
      IL_004a:  pop
      IL_004b:  ldloca.s   V_0
      IL_004d:  ldloc.2
      IL_004e:  call       instance value class MS_jumper4_il.VT MS_jumper4_il.VT::'sub'(value class MS_jumper4_il.VT)
      IL_0053:  pop
      IL_0054:  ldloca.s   V_0
      IL_0056:  ldfld      int32 MS_jumper4_il.VT::m_n
      IL_005b:  stloc.3
      IL_005c:  br.s       IL_005e

      IL_005e:  ldloc.3
      IL_005f:  ret
    } // end of method VT::Main

  } // end of class VT

} // end of namespace MS_jumper4_il

//*********** DISASSEMBLY COMPLETE ***********************
