// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.




.file alignment 512

.assembly extern mscorlib
{
}
.assembly han3_ref // as "han3_ref"
{
}
.assembly extern xunit.core {}
.namespace JitTest_han3_ref_il
{
  .class value private auto ansi sealed Ring
         extends [mscorlib]System.ValueType
  {
    .field public int32 size
  } // end of class Ring

  .class value public auto ansi sealed Column
         extends [mscorlib]System.ValueType
  {
    .field public value class JitTest_han3_ref_il.Ring[] rings
    .field private int32[] heightPtr
    .method public hidebysig specialname 
            instance int32 get_height() il managed
    {
      .maxstack  2
      .locals (int32 V_0)
      IL_0000:  ldarg.0
      IL_0001:  ldfld      int32[] JitTest_han3_ref_il.Column::heightPtr
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
      IL_0001:  ldfld      int32[] JitTest_han3_ref_il.Column::heightPtr
      IL_0006:  ldc.i4.0
      IL_0007:  ldarg.1
      IL_0008:  stelem.i4
      IL_0009:  ret
    } // end of method Column::set_height

    .method public hidebysig specialname rtspecialname 
            instance void  .ctor(int32 maxHeight,
                                 int32 curHeight) il managed
    {
      .maxstack  4
      .locals (int32 V_0,
               int32[] V_1)
      IL_0000:  ldarg.0
      IL_0001:  ldarg.1
      IL_0002:  newarr     JitTest_han3_ref_il.Ring
      IL_0007:  stfld      value class JitTest_han3_ref_il.Ring[] JitTest_han3_ref_il.Column::rings
      IL_000c:  ldc.i4.0
      IL_000d:  stloc.0
      IL_000e:  br.s       IL_0028

      IL_0010:  ldarg.0
      IL_0011:  ldfld      value class JitTest_han3_ref_il.Ring[] JitTest_han3_ref_il.Column::rings
      IL_0016:  ldloc.0
      IL_0017:  ldelema    JitTest_han3_ref_il.Ring
      IL_001c:  ldarg.1
      IL_001d:  ldloc.0
      IL_001e:  sub
      IL_001f:  stfld      int32 JitTest_han3_ref_il.Ring::size
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
      IL_0039:  stfld      int32[] JitTest_han3_ref_il.Column::heightPtr
      IL_003e:  ret
    } // end of method Column::.ctor

    .method public hidebysig instance void
            Validate() il managed
    {
      .maxstack  3
      .locals (int32 V_0)
      IL_0000:  ldc.i4.1
      IL_0001:  stloc.0
      IL_0002:  br.s       IL_0034

      IL_0004:  ldarg.0
      IL_0005:  ldfld      value class JitTest_han3_ref_il.Ring[] JitTest_han3_ref_il.Column::rings
      IL_000a:  ldloc.0
      IL_000b:  ldc.i4.1
      IL_000c:  sub
      IL_000d:  ldelema    JitTest_han3_ref_il.Ring
      IL_0012:  ldfld      int32 JitTest_han3_ref_il.Ring::size
      IL_0017:  ldarg.0
      IL_0018:  ldfld      value class JitTest_han3_ref_il.Ring[] JitTest_han3_ref_il.Column::rings
      IL_001d:  ldloc.0
      IL_001e:  ldelema    JitTest_han3_ref_il.Ring
      IL_0023:  ldfld      int32 JitTest_han3_ref_il.Ring::size
      IL_0028:  bgt.s      IL_0030

      IL_002a:  newobj     instance void [mscorlib]System.Exception::.ctor()
      IL_002f:  throw

      IL_0030:  ldloc.0
      IL_0031:  ldc.i4.1
      IL_0032:  add
      IL_0033:  stloc.0
      IL_0034:  ldloc.0
      IL_0035:  ldarg.0
      IL_0036:  ldfld      int32[] JitTest_han3_ref_il.Column::heightPtr
      IL_003b:  ldc.i4.0
      IL_003c:  ldelem.i4
      IL_003d:  blt.s      IL_0004

      IL_003f:  ret
    } // end of method Column::Validate

    .method private hidebysig static void 
            move1(value class JitTest_han3_ref_il.Column& from,
                  value class JitTest_han3_ref_il.Column& 'to') il managed
    {
      .maxstack  4
      IL_0000:  ldarg.1
      IL_0001:  ldfld      value class JitTest_han3_ref_il.Ring[] JitTest_han3_ref_il.Column::rings
      IL_0006:  ldarg.1
      IL_0007:  call       instance int32 JitTest_han3_ref_il.Column::get_height()
      IL_000c:  ldelema    JitTest_han3_ref_il.Ring
      IL_0011:  ldarg.0
      IL_0012:  ldfld      value class JitTest_han3_ref_il.Ring[] JitTest_han3_ref_il.Column::rings
      IL_0017:  ldarg.0
      IL_0018:  call       instance int32 JitTest_han3_ref_il.Column::get_height()
      IL_001d:  ldc.i4.1
      IL_001e:  sub
      IL_001f:  ldelema    JitTest_han3_ref_il.Ring
      IL_0024:  ldobj      JitTest_han3_ref_il.Ring
      IL_0029:  stobj      JitTest_han3_ref_il.Ring
      IL_002e:  ldarg.1
      IL_002f:  ldarg.1
      IL_0030:  call       instance int32 JitTest_han3_ref_il.Column::get_height()
      IL_0035:  ldc.i4.1
      IL_0036:  add
      IL_0037:  call       instance void JitTest_han3_ref_il.Column::set_height(int32)
      IL_003c:  ldarg.0
      IL_003d:  ldarg.0
      IL_003e:  call       instance int32 JitTest_han3_ref_il.Column::get_height()
      IL_0043:  ldc.i4.1
      IL_0044:  sub
      IL_0045:  call       instance void JitTest_han3_ref_il.Column::set_height(int32)
      IL_004a:  ldarg.1
      IL_004b:  call       instance void JitTest_han3_ref_il.Column::Validate()
      IL_0050:  ldarg.0
      IL_0051:  call       instance void JitTest_han3_ref_il.Column::Validate()
      IL_0056:  ret
    } // end of method Column::move1

    .method private hidebysig static int32 
            move(value class JitTest_han3_ref_il.Column& from,
                 value class JitTest_han3_ref_il.Column& 'to',
                 value class JitTest_han3_ref_il.Column& temp,
                 int32 num) il managed
    {
      .maxstack  6
      .locals (int32 V_0,
               int32 V_1)
      IL_0000:  ldarg.1
      IL_0001:  call       instance void JitTest_han3_ref_il.Column::Validate()
      IL_0006:  ldarg.0
      IL_0007:  call       instance void JitTest_han3_ref_il.Column::Validate()
      IL_000c:  ldarg.2
      IL_000d:  call       instance void JitTest_han3_ref_il.Column::Validate()
      IL_0012:  ldc.i4.1
      IL_0013:  stloc.0
      IL_0014:  ldarg.3
      IL_0015:  ldc.i4.1
      IL_0016:  bne.un.s   IL_0021

      IL_0018:  ldarg.0
      IL_0019:  ldarg.1
      IL_001a:  call       void JitTest_han3_ref_il.Column::move1(value class JitTest_han3_ref_il.Column&,
                                                      value class JitTest_han3_ref_il.Column&)
      IL_001f:  br.s       IL_0044

      IL_0021:  ldloc.0
      IL_0022:  ldarg.0
      IL_0023:  ldarg.2
      IL_0024:  ldarg.1
      IL_0025:  ldarg.3
      IL_0026:  ldc.i4.1
      IL_0027:  sub
      IL_0028:  call       int32 JitTest_han3_ref_il.Column::move(value class JitTest_han3_ref_il.Column&,
                                                      value class JitTest_han3_ref_il.Column&,
                                                      value class JitTest_han3_ref_il.Column&,
                                                      int32)
      IL_002d:  add
      IL_002e:  stloc.0
      IL_002f:  ldarg.0
      IL_0030:  ldarg.1
      IL_0031:  call       void JitTest_han3_ref_il.Column::move1(value class JitTest_han3_ref_il.Column&,
                                                      value class JitTest_han3_ref_il.Column&)
      IL_0036:  ldloc.0
      IL_0037:  ldarg.2
      IL_0038:  ldarg.1
      IL_0039:  ldarg.0
      IL_003a:  ldarg.3
      IL_003b:  ldc.i4.1
      IL_003c:  sub
      IL_003d:  call       int32 JitTest_han3_ref_il.Column::move(value class JitTest_han3_ref_il.Column&,
                                                      value class JitTest_han3_ref_il.Column&,
                                                      value class JitTest_han3_ref_il.Column&,
                                                      int32)
      IL_0042:  add
      IL_0043:  stloc.0
      IL_0044:  ldloc.0
      IL_0045:  stloc.1
      IL_0046:  br.s       IL_0048

      IL_0048:  ldloc.1
      IL_0049:  ret
    } // end of method Column::move

    .method public hidebysig static int32
            Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      .maxstack  4
      .locals (value class JitTest_han3_ref_il.Column V_0,
               value class JitTest_han3_ref_il.Column V_1,
               value class JitTest_han3_ref_il.Column V_2,
               int32 V_3,
               int32 V_4)
      IL_0000:  ldloca.s   V_0
      IL_0002:  ldc.i4.s   17
      IL_0004:  ldc.i4.s   17
      IL_0006:  call       instance void JitTest_han3_ref_il.Column::.ctor(int32,
                                                               int32)
      IL_000b:  ldloca.s   V_1
      IL_000d:  ldc.i4.s   17
      IL_000f:  ldc.i4.0
      IL_0010:  call       instance void JitTest_han3_ref_il.Column::.ctor(int32,
                                                               int32)
      IL_0015:  ldloca.s   V_2
      IL_0017:  ldc.i4.s   17
      IL_0019:  ldc.i4.0
      IL_001a:  call       instance void JitTest_han3_ref_il.Column::.ctor(int32,
                                                               int32)
      IL_001f:  ldloca.s   V_0
      IL_0021:  ldloca.s   V_1
      IL_0023:  ldloca.s   V_2
      IL_0025:  ldc.i4.s   17
      IL_0027:  call       int32 JitTest_han3_ref_il.Column::move(value class JitTest_han3_ref_il.Column&,
                                                      value class JitTest_han3_ref_il.Column&,
                                                      value class JitTest_han3_ref_il.Column&,
                                                      int32)
      IL_002c:  ldc.i4     0x1ff9b
      IL_0031:  sub
      IL_0032:  stloc.3
      IL_0033:  ldloca.s   V_0
      IL_0035:  call       instance void JitTest_han3_ref_il.Column::Validate()
      IL_003a:  ldloca.s   V_1
      IL_003c:  call       instance void JitTest_han3_ref_il.Column::Validate()
      IL_0041:  ldloca.s   V_2
      IL_0043:  call       instance void JitTest_han3_ref_il.Column::Validate()
      IL_0048:  ldloc.3
      IL_0049:  stloc.s    V_4
      IL_004b:  br.s       IL_004d

      IL_004d:  ldloc.s    V_4
      IL_004f:  ret
    } // end of method Column::Main

    .property instance int32 height()
    {
      .get instance int32 JitTest_han3_ref_il.Column::get_height()
      .set instance void JitTest_han3_ref_il.Column::set_height(int32)
    } // end of property Column::height
  } // end of class Column

} // end of namespace JitTest_han3_ref_il

