// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.




.file alignment 512

.assembly extern mscorlib
{
}
.assembly ASSEMBLY_NAME
{
}
.assembly extern xunit.core {}
.namespace JitTest_han3_il
{
  .class value private auto ansi sealed Ring
         extends [mscorlib]System.ValueType
  {
    .field public int32 size
  } // end of class Ring

  .class value public auto ansi sealed Column
         extends [mscorlib]System.ValueType
  {
    .field public value class JitTest_han3_il.Ring[] rings
    .field private int32[] heightPtr
    .method public hidebysig specialname 
            instance int32 get_height() il managed
    {
      .maxstack  2
      .locals (int32 V_0)
      IL_0000:  ldarg.0
      IL_0001:  ldfld      int32[] JitTest_han3_il.Column::heightPtr
      IL_0006:  ldc.i4.0
      IL_0007:  ldelem.i4
      IL_0008:  stloc.0
      IL_0009:  br.s       IL_000b

      IL_000b:  ldloc.0
      IL_000c:  ret
    } // end of method Column::get_height

    .method public hidebysig specialname 
            instance void  set_height(int32 'value') il managed
    {
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldfld      int32[] JitTest_han3_il.Column::heightPtr
      IL_0006:  ldc.i4.0
      IL_0007:  ldarg.1
      IL_0008:  stelem.i4
      IL_0009:  ret
    } // end of method Column::set_height

    .method public hidebysig instance void 
            Init(int32 maxHeight,
                 int32 curHeight) il managed
    {
      .maxstack  4
      .locals (int32 V_0,
               int32[] V_1)
      IL_0000:  ldarg.0
      IL_0001:  ldarg.1
      IL_0002:  newarr     JitTest_han3_il.Ring
      IL_0007:  stfld      value class JitTest_han3_il.Ring[] JitTest_han3_il.Column::rings
      IL_000c:  ldc.i4.0
      IL_000d:  stloc.0
      IL_000e:  br.s       IL_0028

      IL_0010:  ldarg.0
      IL_0011:  ldfld      value class JitTest_han3_il.Ring[] JitTest_han3_il.Column::rings
      IL_0016:  ldloc.0
      IL_0017:  ldelema    JitTest_han3_il.Ring
      IL_001c:  ldarg.1
      IL_001d:  ldloc.0
      IL_001e:  sub
      IL_001f:  stfld      int32 JitTest_han3_il.Ring::size
      IL_0024:  ldloc.0
      IL_0025:  ldc.i4.1
      IL_0026:  add
      IL_0027:  stloc.0
      IL_0028:  ldloc.0
      IL_0029:  ldarg.2
      IL_002a:  blt.s      IL_0010

      IL_002c:  ldarg.0
      IL_002d:  ldc.i4.1
      IL_002e:  newarr     [mscorlib]System.Int32
      IL_0033:  stloc.1
      IL_0034:  ldloc.1
      IL_0035:  ldc.i4.0
      IL_0036:  ldarg.2
      IL_0037:  stelem.i4
      IL_0038:  ldloc.1
      IL_0039:  stfld      int32[] JitTest_han3_il.Column::heightPtr
      IL_003e:  ret
    } // end of method Column::Init

    .method public hidebysig instance void
            Validate() il managed
    {
      .maxstack  3
      .locals (int32 V_0)
      IL_0000:  ldc.i4.1
      IL_0001:  stloc.0
      IL_0002:  br.s       IL_0034

      IL_0004:  ldarg.0
      IL_0005:  ldfld      value class JitTest_han3_il.Ring[] JitTest_han3_il.Column::rings
      IL_000a:  ldloc.0
      IL_000b:  ldc.i4.1
      IL_000c:  sub
      IL_000d:  ldelema    JitTest_han3_il.Ring
      IL_0012:  ldfld      int32 JitTest_han3_il.Ring::size
      IL_0017:  ldarg.0
      IL_0018:  ldfld      value class JitTest_han3_il.Ring[] JitTest_han3_il.Column::rings
      IL_001d:  ldloc.0
      IL_001e:  ldelema    JitTest_han3_il.Ring
      IL_0023:  ldfld      int32 JitTest_han3_il.Ring::size
      IL_0028:  bgt.s      IL_0030

      IL_002a:  newobj     instance void [mscorlib]System.Exception::.ctor()
      IL_002f:  throw

      IL_0030:  ldloc.0
      IL_0031:  ldc.i4.1
      IL_0032:  add
      IL_0033:  stloc.0
      IL_0034:  ldloc.0
      IL_0035:  ldarg.0
      IL_0036:  ldfld      int32[] JitTest_han3_il.Column::heightPtr
      IL_003b:  ldc.i4.0
      IL_003c:  ldelem.i4
      IL_003d:  blt.s      IL_0004

      IL_003f:  ret
    } // end of method Column::Validate

    .method private hidebysig static void 
            move1(value class JitTest_han3_il.Column from,
                  value class JitTest_han3_il.Column 'to') il managed
    {
      .maxstack  4
      IL_0000:  ldarga.s   'to'
      IL_0002:  ldfld      value class JitTest_han3_il.Ring[] JitTest_han3_il.Column::rings
      IL_0007:  ldarga.s   'to'
      IL_0009:  call       instance int32 JitTest_han3_il.Column::get_height()
      IL_000e:  ldelema    JitTest_han3_il.Ring
      IL_0013:  ldarga.s   from
      IL_0015:  ldfld      value class JitTest_han3_il.Ring[] JitTest_han3_il.Column::rings
      IL_001a:  ldarga.s   from
      IL_001c:  call       instance int32 JitTest_han3_il.Column::get_height()
      IL_0021:  ldc.i4.1
      IL_0022:  sub
      IL_0023:  ldelema    JitTest_han3_il.Ring
      IL_0028:  ldobj      JitTest_han3_il.Ring
      IL_002d:  stobj      JitTest_han3_il.Ring
      IL_0032:  ldarga.s   'to'
      IL_0034:  ldarga.s   'to'
      IL_0036:  call       instance int32 JitTest_han3_il.Column::get_height()
      IL_003b:  ldc.i4.1
      IL_003c:  add
      IL_003d:  call       instance void JitTest_han3_il.Column::set_height(int32)
      IL_0042:  ldarga.s   from
      IL_0044:  ldarga.s   from
      IL_0046:  call       instance int32 JitTest_han3_il.Column::get_height()
      IL_004b:  ldc.i4.1
      IL_004c:  sub
      IL_004d:  call       instance void JitTest_han3_il.Column::set_height(int32)
      IL_0052:  ldarga.s   'to'
      IL_0054:  call       instance void JitTest_han3_il.Column::Validate()
      IL_0059:  ldarga.s   from
      IL_005b:  call       instance void JitTest_han3_il.Column::Validate()
      IL_0060:  ret
    } // end of method Column::move1

    .method private hidebysig static int32 
            move(value class JitTest_han3_il.Column from,
                 value class JitTest_han3_il.Column 'to',
                 value class JitTest_han3_il.Column temp,
                 int32 num) il managed
    {
      .maxstack  6
      .locals (int32 V_0,
               int32 V_1)
      IL_0000:  ldc.i4.1
      IL_0001:  stloc.0
      IL_0002:  ldarg.3
      IL_0003:  ldc.i4.1
      IL_0004:  bne.un.s   IL_000f

      IL_0006:  ldarg.0
      IL_0007:  ldarg.1
      IL_0008:  call       void JitTest_han3_il.Column::move1(value class JitTest_han3_il.Column,
                                                      value class JitTest_han3_il.Column)
      IL_000d:  br.s       IL_0032

      IL_000f:  ldloc.0
      IL_0010:  ldarg.0
      IL_0011:  ldarg.2
      IL_0012:  ldarg.1
      IL_0013:  ldarg.3
      IL_0014:  ldc.i4.1
      IL_0015:  sub
      IL_0016:  call       int32 JitTest_han3_il.Column::move(value class JitTest_han3_il.Column,
                                                      value class JitTest_han3_il.Column,
                                                      value class JitTest_han3_il.Column,
                                                      int32)
      IL_001b:  add
      IL_001c:  stloc.0
      IL_001d:  ldarg.0
      IL_001e:  ldarg.1
      IL_001f:  call       void JitTest_han3_il.Column::move1(value class JitTest_han3_il.Column,
                                                      value class JitTest_han3_il.Column)
      IL_0024:  ldloc.0
      IL_0025:  ldarg.2
      IL_0026:  ldarg.1
      IL_0027:  ldarg.0
      IL_0028:  ldarg.3
      IL_0029:  ldc.i4.1
      IL_002a:  sub
      IL_002b:  call       int32 JitTest_han3_il.Column::move(value class JitTest_han3_il.Column,
                                                      value class JitTest_han3_il.Column,
                                                      value class JitTest_han3_il.Column,
                                                      int32)
      IL_0030:  add
      IL_0031:  stloc.0
      IL_0032:  ldloc.0
      IL_0033:  stloc.1
      IL_0034:  br.s       IL_0036

      IL_0036:  ldloc.1
      IL_0037:  ret
    } // end of method Column::move

    .method public hidebysig static int32
            Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      .maxstack  8
      .locals (value class JitTest_han3_il.Column V_X)
			ldloca.s   V_X
			initobj    JitTest_han3_il.Column
			
			ldloca.s   V_X
			ldc.i4.s   17
			dup
			call	instance void JitTest_han3_il.Column::Init(int32, int32)
			ldloc.0

			ldloca.s   V_X
			ldc.i4.s   17
			ldc.i4.0
			call	instance void JitTest_han3_il.Column::Init(int32, int32)
			ldloc.0

			ldloca.s   V_X
			ldc.i4.s   17
			ldc.i4.0
			call	instance void JitTest_han3_il.Column::Init(int32, int32)
			ldloc.0
			
			ldc.i4.s   17
			call       int32 JitTest_han3_il.Column::move(value class JitTest_han3_il.Column,
                                                      value class JitTest_han3_il.Column,
                                                      value class JitTest_han3_il.Column,
                                                      int32)
            ldc.i4		130971
            sub
			ret
    } // end of method Column::Main

    .property instance int32 height()
    {
      .get instance int32 JitTest_han3_il.Column::get_height()
      .set instance void JitTest_han3_il.Column::set_height(int32)
    } // end of property Column::height
  } // end of class Column

} // end of namespace JitTest_han3_il

