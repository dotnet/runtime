// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'rotate_u2'// as "rotate_u2"
{

  //  .custom instance void ['mscorlib']System.Diagnostics.DebuggableAttribute::.ctor(bool,
  //                                                                                  bool) = ( 01 00 00 01 00 00 ) 
  
  
}
.assembly extern xunit.core {}
// MVID: {D13C2D0A-FC17-423E-926F-6A239416DD1C}
.namespace Rotate_rotate_u2_il
{
  .class public auto ansi App extends ['mscorlib']System.Object
  {
    .class auto ansi nested private App$Node extends ['mscorlib']System.Object
    {
      .field private class Rotate_rotate_u2_il.App/App$Node m_leftChild
      .field private unsigned int8 padding1
      .field private unsigned int8 padding2
      .field private unsigned int16 m_weight
      .field private unsigned int8 padding3
      .field private class Rotate_rotate_u2_il.App/App$Node m_rightChild
      .method public hidebysig specialname rtspecialname 
              instance void .ctor() il managed
      {
        // Code size       29 (0x1d)
        .maxstack  8
        IL_0000:  ldarg.0
        IL_0001:  call       instance void ['mscorlib']System.Object::.ctor()
        IL_0006:  ldarg.0
        IL_0007:  ldsfld     class ['mscorlib']System.Random Rotate_rotate_u2_il.App::s_randomGen
        IL_000c:  ldc.i4     0x2710
        IL_0011:  callvirt   instance int32 ['mscorlib']System.Random::Next(int32)
        IL_0016:  conv.u2
        IL_0017:  stfld      unsigned int16 Rotate_rotate_u2_il.App/App$Node::m_weight
        IL_001c:  ret
      } // end of method 'App$Node::.ctor'

      .method family hidebysig virtual instance void
              Finalize() il managed
      {
        // Code size       1 (0x1)
        .maxstack  8
        IL_0000:  ret
      } // end of method 'App$Node::Finalize'

      .method public hidebysig instance void 
              growTree(int32 maxHeight) il managed
      {
        // Code size       73 (0x49)
        .maxstack  4
        .locals (class Rotate_rotate_u2_il.App/App$Node V_0)
        IL_0000:  ldarg.1
        IL_0001:  ldc.i4.0
        IL_0002:  ble.s      IL_0038

        IL_0004:  ldarg.0
        IL_0005:  newobj     instance void Rotate_rotate_u2_il.App/App$Node::.ctor()
        IL_000a:  stfld      class Rotate_rotate_u2_il.App/App$Node Rotate_rotate_u2_il.App/App$Node::m_leftChild
        IL_000f:  ldarg.0
        IL_0010:  ldfld      class Rotate_rotate_u2_il.App/App$Node Rotate_rotate_u2_il.App/App$Node::m_leftChild
        IL_0015:  ldarg.1
        IL_0016:  ldc.i4.1
        IL_0017:  sub
        IL_0018:  call       instance void Rotate_rotate_u2_il.App/App$Node::growTree(int32)
        IL_001d:  ldarg.0
        IL_001e:  newobj     instance void Rotate_rotate_u2_il.App/App$Node::.ctor()
        IL_0023:  stfld      class Rotate_rotate_u2_il.App/App$Node Rotate_rotate_u2_il.App/App$Node::m_rightChild
        IL_0028:  ldarg.0
        IL_0029:  ldfld      class Rotate_rotate_u2_il.App/App$Node Rotate_rotate_u2_il.App/App$Node::m_rightChild
        IL_002e:  ldarg.1
        IL_002f:  ldc.i4.1
        IL_0030:  sub
        IL_0031:  call       instance void Rotate_rotate_u2_il.App/App$Node::growTree(int32)
        IL_0036:  br.s       IL_0048

        IL_0038:  ldarg.0
        IL_0039:  ldarg.0
        IL_003a:  ldnull
        IL_003b:  dup
        IL_003c:  stloc.0
        IL_003d:  stfld      class Rotate_rotate_u2_il.App/App$Node Rotate_rotate_u2_il.App/App$Node::m_rightChild
        IL_0042:  ldloc.0
        IL_0043:  stfld      class Rotate_rotate_u2_il.App/App$Node Rotate_rotate_u2_il.App/App$Node::m_leftChild
        IL_0048:  ret
      } // end of method 'App$Node::growTree'

      .method public hidebysig instance void
              rotateTree() il managed
      {
        // Code size       282 (0x11a)
        .maxstack  2
        .locals (class Rotate_rotate_u2_il.App/App$Node V_0,
                 class Rotate_rotate_u2_il.App/App$Node V_1,
                 unsigned int16 V_2,
                 unsigned int16 & V_3,
                 unsigned int16 & V_4)
        IL_0000:  ldnull
        IL_0001:  stloc.0
        IL_0002:  ldnull
        IL_0003:  stloc.1
        IL_0004:  ldc.i4.0
        IL_0005:  stloc.2
        IL_0006:  ldloca V_2 // ldc.i4     0xffff
        IL_000b:  stloc.3
        IL_000c:  ldloca V_2 // ldc.i4     0xffff
        IL_0011:  stloc.s    V_4
        IL_0013:  ldarg.0
        IL_0014:  ldfld      class Rotate_rotate_u2_il.App/App$Node Rotate_rotate_u2_il.App/App$Node::m_leftChild
        IL_0019:  brfalse.s  IL_0060

        IL_001b:  newobj     instance void Rotate_rotate_u2_il.App/App$Node::.ctor()
        IL_0020:  stloc.1
        IL_0021:  ldloc.1
        IL_0022:  ldarg.0
        IL_0023:  ldfld      class Rotate_rotate_u2_il.App/App$Node Rotate_rotate_u2_il.App/App$Node::m_leftChild
        IL_0028:  ldfld      class Rotate_rotate_u2_il.App/App$Node Rotate_rotate_u2_il.App/App$Node::m_leftChild
        IL_002d:  stfld      class Rotate_rotate_u2_il.App/App$Node Rotate_rotate_u2_il.App/App$Node::m_leftChild
        IL_0032:  ldloc.1
        IL_0033:  ldarg.0
        IL_0034:  ldfld      class Rotate_rotate_u2_il.App/App$Node Rotate_rotate_u2_il.App/App$Node::m_leftChild
        IL_0039:  ldfld      class Rotate_rotate_u2_il.App/App$Node Rotate_rotate_u2_il.App/App$Node::m_rightChild
        IL_003e:  stfld      class Rotate_rotate_u2_il.App/App$Node Rotate_rotate_u2_il.App/App$Node::m_rightChild
        IL_0043:  ldloc.1
        IL_0044:  ldarg.0
        IL_0045:  ldfld      class Rotate_rotate_u2_il.App/App$Node Rotate_rotate_u2_il.App/App$Node::m_leftChild
        IL_004a:  ldfld      unsigned int16 Rotate_rotate_u2_il.App/App$Node::m_weight
        IL_004f:  stfld      unsigned int16 Rotate_rotate_u2_il.App/App$Node::m_weight
        IL_0054:  ldarg.0
        IL_0055:  ldfld      class Rotate_rotate_u2_il.App/App$Node Rotate_rotate_u2_il.App/App$Node::m_leftChild
        IL_005a:  ldflda      unsigned int16 Rotate_rotate_u2_il.App/App$Node::m_weight
        IL_005f:  stloc.3
        IL_0060:  ldarg.0
        IL_0061:  ldfld      class Rotate_rotate_u2_il.App/App$Node Rotate_rotate_u2_il.App/App$Node::m_rightChild
        IL_0066:  brfalse.s  IL_00ae

        IL_0068:  newobj     instance void Rotate_rotate_u2_il.App/App$Node::.ctor()
        IL_006d:  stloc.0
        IL_006e:  ldloc.0
        IL_006f:  ldarg.0
        IL_0070:  ldfld      class Rotate_rotate_u2_il.App/App$Node Rotate_rotate_u2_il.App/App$Node::m_rightChild
        IL_0075:  ldfld      class Rotate_rotate_u2_il.App/App$Node Rotate_rotate_u2_il.App/App$Node::m_leftChild
        IL_007a:  stfld      class Rotate_rotate_u2_il.App/App$Node Rotate_rotate_u2_il.App/App$Node::m_leftChild
        IL_007f:  ldloc.0
        IL_0080:  ldarg.0
        IL_0081:  ldfld      class Rotate_rotate_u2_il.App/App$Node Rotate_rotate_u2_il.App/App$Node::m_rightChild
        IL_0086:  ldfld      class Rotate_rotate_u2_il.App/App$Node Rotate_rotate_u2_il.App/App$Node::m_rightChild
        IL_008b:  stfld      class Rotate_rotate_u2_il.App/App$Node Rotate_rotate_u2_il.App/App$Node::m_rightChild
        IL_0090:  ldloc.0
        IL_0091:  ldarg.0
        IL_0092:  ldfld      class Rotate_rotate_u2_il.App/App$Node Rotate_rotate_u2_il.App/App$Node::m_rightChild
        IL_0097:  ldfld      unsigned int16 Rotate_rotate_u2_il.App/App$Node::m_weight
        IL_009c:  stfld      unsigned int16 Rotate_rotate_u2_il.App/App$Node::m_weight
        IL_00a1:  ldarg.0
        IL_00a2:  ldfld      class Rotate_rotate_u2_il.App/App$Node Rotate_rotate_u2_il.App/App$Node::m_rightChild
        IL_00a7:  ldflda      unsigned int16 Rotate_rotate_u2_il.App/App$Node::m_weight
        IL_00ac:  stloc.s    V_4
        IL_00ae:  ldarg.0
        IL_00af:  ldloc.0
        IL_00b0:  stfld      class Rotate_rotate_u2_il.App/App$Node Rotate_rotate_u2_il.App/App$Node::m_leftChild
        IL_00b5:  ldarg.0
        IL_00b6:  ldloc.1
        IL_00b7:  stfld      class Rotate_rotate_u2_il.App/App$Node Rotate_rotate_u2_il.App/App$Node::m_rightChild
        IL_00bc:  call       void ['mscorlib']System.GC::Collect()
        IL_00c1:  call       void ['mscorlib']System.GC::WaitForPendingFinalizers()
        IL_00c6:  ldarg.0
        IL_00c7:  ldfld      class Rotate_rotate_u2_il.App/App$Node Rotate_rotate_u2_il.App/App$Node::m_rightChild
        IL_00cc:  brfalse.s  IL_00ef

        IL_00ce:  ldarg.0
        IL_00cf:  ldfld      class Rotate_rotate_u2_il.App/App$Node Rotate_rotate_u2_il.App/App$Node::m_rightChild
        IL_00d4:  call       instance void Rotate_rotate_u2_il.App/App$Node::rotateTree()
        IL_00d9:  ldloc.3
        			ldind.i2
        IL_00da:  conv.i4
        IL_00db:  ldarg.0
        IL_00dc:  ldfld      class Rotate_rotate_u2_il.App/App$Node Rotate_rotate_u2_il.App/App$Node::m_rightChild
        IL_00e1:  ldfld      unsigned int16 Rotate_rotate_u2_il.App/App$Node::m_weight
        IL_00e6:  conv.i4
        IL_00e7:  beq.s      IL_00ef

        IL_00e9:  newobj     instance void ['mscorlib']System.Exception::.ctor()
        IL_00ee:  throw

        IL_00ef:  ldarg.0
        IL_00f0:  ldfld      class Rotate_rotate_u2_il.App/App$Node Rotate_rotate_u2_il.App/App$Node::m_leftChild
        IL_00f5:  brfalse.s  IL_0119

        IL_00f7:  ldarg.0
        IL_00f8:  ldfld      class Rotate_rotate_u2_il.App/App$Node Rotate_rotate_u2_il.App/App$Node::m_leftChild
        IL_00fd:  call       instance void Rotate_rotate_u2_il.App/App$Node::rotateTree()
        IL_0102:  ldloc.s    V_4
        			ldind.i2
        IL_0104:  conv.i4
        IL_0105:  ldarg.0
        IL_0106:  ldfld      class Rotate_rotate_u2_il.App/App$Node Rotate_rotate_u2_il.App/App$Node::m_leftChild
        IL_010b:  ldfld      unsigned int16 Rotate_rotate_u2_il.App/App$Node::m_weight
        IL_0110:  conv.i4
        IL_0111:  beq.s      IL_0119

        IL_0113:  newobj     instance void ['mscorlib']System.Exception::.ctor()
        IL_0118:  throw

        IL_0119:  ret
      } // end of method 'App$Node::rotateTree'

    } // end of class 'App$Node'

    .field public static class ['mscorlib']System.Random s_randomGen
    .method public hidebysig static int32
            Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      // Code size       20 (0x14)
      .maxstack  2
      .locals (class Rotate_rotate_u2_il.App/App$Node V_0)
      IL_0000:  newobj     instance void Rotate_rotate_u2_il.App/App$Node::.ctor()
      IL_0005:  stloc.0
      IL_0006:  ldloc.0
      IL_0007:  ldc.i4.6
      IL_0008:  call       instance void Rotate_rotate_u2_il.App/App$Node::growTree(int32)
      IL_000d:  ldloc.0
      IL_000e:  call       instance void Rotate_rotate_u2_il.App/App$Node::rotateTree()
      IL_0013:  ldc.i4    0x64
      IL_0018:  ret
    } // end of method 'App::Main'

    .method public hidebysig specialname rtspecialname static 
            void .cctor() il managed
    {
      // Code size       11 (0xb)
      .maxstack  8
      IL_0000:  newobj     instance void ['mscorlib']System.Random::.ctor()
      IL_0005:  stsfld     class ['mscorlib']System.Random Rotate_rotate_u2_il.App::s_randomGen
      IL_000a:  ret
    } // end of method 'App::.cctor'

    .method public hidebysig specialname rtspecialname 
            instance void .ctor() il managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void ['mscorlib']System.Object::.ctor()
      IL_0006:  ret
    } // end of method 'App::.ctor'

  } // end of class 'App'

} // end of namespace 'Rotate_rotate_u2_il'

//*********** DISASSEMBLY COMPLETE ***********************
