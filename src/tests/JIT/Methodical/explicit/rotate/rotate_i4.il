// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ASSEMBLY_NAME
{

  //  .custom instance void ['mscorlib']System.Diagnostics.DebuggableAttribute::.ctor(bool,
  //                                                                                  bool) = ( 01 00 00 01 00 00 ) 
  
  
}
.assembly extern xunit.core {}
// MVID: {A112EBC7-3218-46C8-939E-D43B5ED4DE98}
.namespace Rotate_rotate_i4_il
{
  .class public auto ansi App extends ['mscorlib']System.Object
  {
    .class auto ansi nested private App$Node extends ['mscorlib']System.Object
    {
      .field private int32 m_weight
      .field private class Rotate_rotate_i4_il.App/App$Node m_leftChild
      .field private class Rotate_rotate_i4_il.App/App$Node m_rightChild
      .method public hidebysig specialname rtspecialname 
              instance void .ctor() il managed
      {
        // Code size       28 (0x1c)
        .maxstack  8
        IL_0000:  ldarg.0
        IL_0001:  call       instance void ['mscorlib']System.Object::.ctor()
        IL_0006:  ldarg.0
        IL_0007:  ldsfld     class ['mscorlib']System.Random Rotate_rotate_i4_il.App::s_randomGen
        IL_000c:  ldc.i4     0x2710
        IL_0011:  callvirt   instance int32 ['mscorlib']System.Random::Next(int32)
        IL_0016:  stfld      int32 Rotate_rotate_i4_il.App/App$Node::m_weight
        IL_001b:  ret
      } // end of method 'App$Node::.ctor'

      .method public hidebysig instance void 
              growTree(int32 maxHeight) il managed
      {
        // Code size       73 (0x49)
        .maxstack  4
        .locals (class Rotate_rotate_i4_il.App/App$Node V_0)
        IL_0000:  ldarg.1
        IL_0001:  ldc.i4.0
        IL_0002:  ble.s      IL_0038

        IL_0004:  ldarg.0
        IL_0005:  newobj     instance void Rotate_rotate_i4_il.App/App$Node::.ctor()
        IL_000a:  stfld      class Rotate_rotate_i4_il.App/App$Node Rotate_rotate_i4_il.App/App$Node::m_leftChild
        IL_000f:  ldarg.0
        IL_0010:  ldfld      class Rotate_rotate_i4_il.App/App$Node Rotate_rotate_i4_il.App/App$Node::m_leftChild
        IL_0015:  ldarg.1
        IL_0016:  ldc.i4.1
        IL_0017:  sub
        IL_0018:  call       instance void Rotate_rotate_i4_il.App/App$Node::growTree(int32)
        IL_001d:  ldarg.0
        IL_001e:  newobj     instance void Rotate_rotate_i4_il.App/App$Node::.ctor()
        IL_0023:  stfld      class Rotate_rotate_i4_il.App/App$Node Rotate_rotate_i4_il.App/App$Node::m_rightChild
        IL_0028:  ldarg.0
        IL_0029:  ldfld      class Rotate_rotate_i4_il.App/App$Node Rotate_rotate_i4_il.App/App$Node::m_rightChild
        IL_002e:  ldarg.1
        IL_002f:  ldc.i4.1
        IL_0030:  sub
        IL_0031:  call       instance void Rotate_rotate_i4_il.App/App$Node::growTree(int32)
        IL_0036:  br.s       IL_0048

        IL_0038:  ldarg.0
        IL_0039:  ldarg.0
        IL_003a:  ldnull
        IL_003b:  dup
        IL_003c:  stloc.0
        IL_003d:  stfld      class Rotate_rotate_i4_il.App/App$Node Rotate_rotate_i4_il.App/App$Node::m_rightChild
        IL_0042:  ldloc.0
        IL_0043:  stfld      class Rotate_rotate_i4_il.App/App$Node Rotate_rotate_i4_il.App/App$Node::m_leftChild
        IL_0048:  ret
      } // end of method 'App$Node::growTree'

      .method public hidebysig instance void
              rotateTree() il managed
      {
        // Code size       270 (0x10e)
        .maxstack  2
        .locals (class Rotate_rotate_i4_il.App/App$Node V_0,
                 class Rotate_rotate_i4_il.App/App$Node V_1,
                 int32 V_2,
                 int32 & V_3,
                 int32 & V_4)
        IL_0000:  ldnull
        IL_0001:  stloc.0
        IL_0002:  ldnull
        IL_0003:  stloc.1
        IL_0004:  ldc.i4.0
        IL_0005:  stloc.2
        IL_0006:  ldloca V_2 //ldc.i4.m1
        IL_0007:  stloc.3
        IL_0008:  ldloca V_2 //ldc.i4.m1
        IL_0009:  stloc.s    V_4
        IL_000b:  ldarg.0
        IL_000c:  ldfld      class Rotate_rotate_i4_il.App/App$Node Rotate_rotate_i4_il.App/App$Node::m_leftChild
        IL_0011:  brfalse.s  IL_0058

        IL_0013:  newobj     instance void Rotate_rotate_i4_il.App/App$Node::.ctor()
        IL_0018:  stloc.1
        IL_0019:  ldloc.1
        IL_001a:  ldarg.0
        IL_001b:  ldfld      class Rotate_rotate_i4_il.App/App$Node Rotate_rotate_i4_il.App/App$Node::m_leftChild
        IL_0020:  ldfld      class Rotate_rotate_i4_il.App/App$Node Rotate_rotate_i4_il.App/App$Node::m_leftChild
        IL_0025:  stfld      class Rotate_rotate_i4_il.App/App$Node Rotate_rotate_i4_il.App/App$Node::m_leftChild
        IL_002a:  ldloc.1
        IL_002b:  ldarg.0
        IL_002c:  ldfld      class Rotate_rotate_i4_il.App/App$Node Rotate_rotate_i4_il.App/App$Node::m_leftChild
        IL_0031:  ldfld      class Rotate_rotate_i4_il.App/App$Node Rotate_rotate_i4_il.App/App$Node::m_rightChild
        IL_0036:  stfld      class Rotate_rotate_i4_il.App/App$Node Rotate_rotate_i4_il.App/App$Node::m_rightChild
        IL_003b:  ldloc.1
        IL_003c:  ldarg.0
        IL_003d:  ldfld      class Rotate_rotate_i4_il.App/App$Node Rotate_rotate_i4_il.App/App$Node::m_leftChild
        IL_0042:  ldfld      int32 Rotate_rotate_i4_il.App/App$Node::m_weight
        IL_0047:  stfld      int32 Rotate_rotate_i4_il.App/App$Node::m_weight
        IL_004c:  ldarg.0
        IL_004d:  ldfld      class Rotate_rotate_i4_il.App/App$Node Rotate_rotate_i4_il.App/App$Node::m_leftChild
        IL_0052:  ldflda      int32 Rotate_rotate_i4_il.App/App$Node::m_weight
        IL_0057:  stloc.3
        IL_0058:  ldarg.0
        IL_0059:  ldfld      class Rotate_rotate_i4_il.App/App$Node Rotate_rotate_i4_il.App/App$Node::m_rightChild
        IL_005e:  brfalse.s  IL_00a6

        IL_0060:  newobj     instance void Rotate_rotate_i4_il.App/App$Node::.ctor()
        IL_0065:  stloc.0
        IL_0066:  ldloc.0
        IL_0067:  ldarg.0
        IL_0068:  ldfld      class Rotate_rotate_i4_il.App/App$Node Rotate_rotate_i4_il.App/App$Node::m_rightChild
        IL_006d:  ldfld      class Rotate_rotate_i4_il.App/App$Node Rotate_rotate_i4_il.App/App$Node::m_leftChild
        IL_0072:  stfld      class Rotate_rotate_i4_il.App/App$Node Rotate_rotate_i4_il.App/App$Node::m_leftChild
        IL_0077:  ldloc.0
        IL_0078:  ldarg.0
        IL_0079:  ldfld      class Rotate_rotate_i4_il.App/App$Node Rotate_rotate_i4_il.App/App$Node::m_rightChild
        IL_007e:  ldfld      class Rotate_rotate_i4_il.App/App$Node Rotate_rotate_i4_il.App/App$Node::m_rightChild
        IL_0083:  stfld      class Rotate_rotate_i4_il.App/App$Node Rotate_rotate_i4_il.App/App$Node::m_rightChild
        IL_0088:  ldloc.0
        IL_0089:  ldarg.0
        IL_008a:  ldfld      class Rotate_rotate_i4_il.App/App$Node Rotate_rotate_i4_il.App/App$Node::m_rightChild
        IL_008f:  ldfld      int32 Rotate_rotate_i4_il.App/App$Node::m_weight
        IL_0094:  stfld      int32 Rotate_rotate_i4_il.App/App$Node::m_weight
        IL_0099:  ldarg.0
        IL_009a:  ldfld      class Rotate_rotate_i4_il.App/App$Node Rotate_rotate_i4_il.App/App$Node::m_rightChild
        IL_009f:  ldflda      int32 Rotate_rotate_i4_il.App/App$Node::m_weight
        IL_00a4:  stloc.s    V_4
        IL_00a6:  ldarg.0
        IL_00a7:  ldloc.0
        IL_00a8:  stfld      class Rotate_rotate_i4_il.App/App$Node Rotate_rotate_i4_il.App/App$Node::m_leftChild
        IL_00ad:  ldarg.0
        IL_00ae:  ldloc.1
        IL_00af:  stfld      class Rotate_rotate_i4_il.App/App$Node Rotate_rotate_i4_il.App/App$Node::m_rightChild
        IL_00b4:  call       void ['mscorlib']System.GC::Collect()
        IL_00b9:  call       void ['mscorlib']System.GC::WaitForPendingFinalizers()
        IL_00be:  ldarg.0
        IL_00bf:  ldfld      class Rotate_rotate_i4_il.App/App$Node Rotate_rotate_i4_il.App/App$Node::m_rightChild
        IL_00c4:  brfalse.s  IL_00e5

        IL_00c6:  ldarg.0
        IL_00c7:  ldfld      class Rotate_rotate_i4_il.App/App$Node Rotate_rotate_i4_il.App/App$Node::m_rightChild
        IL_00cc:  call       instance void Rotate_rotate_i4_il.App/App$Node::rotateTree()
        IL_00d1:  ldloc.3
        			ldind.i4
        IL_00d2:  ldarg.0
        IL_00d3:  ldfld      class Rotate_rotate_i4_il.App/App$Node Rotate_rotate_i4_il.App/App$Node::m_rightChild
        IL_00d8:  ldfld      int32 Rotate_rotate_i4_il.App/App$Node::m_weight
        IL_00dd:  beq.s      IL_00e5

        IL_00df:  newobj     instance void ['mscorlib']System.Exception::.ctor()
        IL_00e4:  throw

        IL_00e5:  ldarg.0
        IL_00e6:  ldfld      class Rotate_rotate_i4_il.App/App$Node Rotate_rotate_i4_il.App/App$Node::m_leftChild
        IL_00eb:  brfalse.s  IL_010d

        IL_00ed:  ldarg.0
        IL_00ee:  ldfld      class Rotate_rotate_i4_il.App/App$Node Rotate_rotate_i4_il.App/App$Node::m_leftChild
        IL_00f3:  call       instance void Rotate_rotate_i4_il.App/App$Node::rotateTree()
        IL_00f8:  ldloc.s    V_4
        			ldind.i4
        IL_00fa:  ldarg.0
        IL_00fb:  ldfld      class Rotate_rotate_i4_il.App/App$Node Rotate_rotate_i4_il.App/App$Node::m_leftChild
        IL_0100:  ldfld      int32 Rotate_rotate_i4_il.App/App$Node::m_weight
        IL_0105:  beq.s      IL_010d

        IL_0107:  newobj     instance void ['mscorlib']System.Exception::.ctor()
        IL_010c:  throw

        IL_010d:  ret
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
      .locals (class Rotate_rotate_i4_il.App/App$Node V_0)
      IL_0000:  newobj     instance void Rotate_rotate_i4_il.App/App$Node::.ctor()
      IL_0005:  stloc.0
      IL_0006:  ldloc.0
      IL_0007:  ldc.i4.6
      IL_0008:  call       instance void Rotate_rotate_i4_il.App/App$Node::growTree(int32)
      IL_000d:  ldloc.0
      IL_000e:  call       instance void Rotate_rotate_i4_il.App/App$Node::rotateTree()
      IL_0013:  ldc.i4    0x64
      IL_0018:  ret
    } // end of method 'App::Main'

    .method public hidebysig specialname rtspecialname static 
            void .cctor() il managed
    {
      // Code size       11 (0xb)
      .maxstack  8
      IL_0000:  newobj     instance void ['mscorlib']System.Random::.ctor()
      IL_0005:  stsfld     class ['mscorlib']System.Random Rotate_rotate_i4_il.App::s_randomGen
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

} // end of namespace 'Rotate_rotate_i4_il'

//*********** DISASSEMBLY COMPLETE ***********************
