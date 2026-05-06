// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// It is a non-stress repro for the issue #14455.
// growTree is optimized as fast tail call and converted to a loop.
// `starg.s 0` forces the jit to create a temp for the modifiable this.
// Before the fix the jit zero-initialized all long-live locals in the end of the loop,
// on the last iteration it set `this` to `null` and the return block was trying to use
// `null` to write `m_rightChild` in, that caused a runtime exception.


.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly GitHub_14455 {}
.assembly extern xunit.core {}

.class public auto ansi beforefieldinit Rotate.Node
       extends [System.Runtime]System.Object
{
  .field public class Rotate.Node m_leftChild
  .field public class Rotate.Node m_rightChild
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Node::.ctor

  .method family hidebysig virtual instance void 
          Finalize() cil managed
  {
    .override [System.Runtime]System.Object::Finalize
    // Code size       10 (0xa)
    .maxstack  1
    .try
    {
      IL_0000:  leave.s    IL_0009

    }  // end .try
    finally
    {
      IL_0002:  ldarg.0
      IL_0003:  call       instance void [System.Runtime]System.Object::Finalize()
      IL_0008:  endfinally
    }  // end handler
    IL_0009:  ret
  } // end of method Node::Finalize

  .method public hidebysig instance void 
          growTree(int32 maxHeight) cil managed
  {
    // Code size       72 (0x48)
    .maxstack  4
    .locals init (class Rotate.Node V_0)
    IL_0000:  ldarg.1
    IL_0001:  ldc.i4.0
    IL_0002:  ble.s      IL_0037

    IL_0004:  ldarg.0
    IL_0005:  newobj     instance void Rotate.Node::.ctor()
    IL_000a:  stfld      class Rotate.Node Rotate.Node::m_leftChild
    IL_000f:  ldarg.0
    IL_0010:  ldfld      class Rotate.Node Rotate.Node::m_leftChild
    IL_0015:  ldarg.1
    IL_0016:  ldc.i4.1
    IL_0017:  sub
    IL_0018:  callvirt   instance void Rotate.Node::growTree(int32)
    IL_001d:  ldarg.0
    IL_001e:  newobj     instance void Rotate.Node::.ctor()
    IL_0023:  stfld      class Rotate.Node Rotate.Node::m_rightChild
    IL_0028:  ldarg.0
    IL_0029:  ldfld      class Rotate.Node Rotate.Node::m_rightChild
    IL_002e:  ldarg.1
    IL_002f:  ldc.i4.1
    IL_0030:  sub
    IL_0031:  callvirt   instance void Rotate.Node::growTree(int32)
    IL_0036:  ret
			  ldarg.0 
		      starg.s 0 // Force to create a temp for the modifiable this.
    IL_0037:  ldarg.0
    IL_0038:  ldarg.0
    IL_0039:  ldnull
    IL_003a:  dup
    IL_003b:  stloc.0
    IL_003c:  stfld      class Rotate.Node Rotate.Node::m_rightChild
    IL_0041:  ldloc.0
    IL_0042:  stfld      class Rotate.Node Rotate.Node::m_leftChild
    IL_0047:  ret
  } // end of method Node::growTree

  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  newobj     instance void Rotate.Node::.ctor()
    IL_0005:  ldc.i4.4
    IL_0006:  callvirt   instance void Rotate.Node::growTree(int32)
    IL_000b:  ldc.i4.s   100
    IL_000d:  ret
  } // end of method Node::Main

} // end of class Rotate.Node
