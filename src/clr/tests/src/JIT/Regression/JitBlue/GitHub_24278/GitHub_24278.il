// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Runtime { }
.assembly GitHub_24278 { }
.module GitHub_24278.exe

// GitHub 24278: a bug is reproduced when
// 1) a register is used in the prolog for initializing GSSecurityCookie (i.e. the register contains a random non-zero value) and
// 2) the same register holds a must-init GC variable in the next basic block and
// 3) the variable is live at the beginning of this basic block.
// The register was not zeroed at the end of the prolog and this was causing segmentation fault during GC.Collect().
//
// This happens in MainMethod at IL_0013:
// V_0 is not initialized by IL instructions of one of the path to IL_0013 and must be zeroed in the prolog since it is live at IL_0013.

.class private auto ansi beforefieldinit GitHub_24278.Program
       extends [System.Runtime]System.Object
{
  .method private hidebysig static bool  True() cil managed noinlining
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  ldc.i4.1
    IL_0001:  ret
  } // end of method Program::True

  .method private hidebysig static void  Use(int32 arg0) cil managed noinlining
  {
    // Code size       6 (0x6)
    .maxstack  8
    IL_0000:  call       void [System.Runtime]System.GC::Collect()
    IL_0005:  ret
  } // end of method Program::Use

  .method private hidebysig static void  MainMethod() cil managed noinlining
  {
    //
    .maxstack  1
    .locals init (object V_0)
    IL_0000:  call       bool GitHub_24278.Program::True()
    IL_0005:  brtrue.s   IL_000d

    IL_0007:  newobj     instance void [System.Runtime]System.Object::.ctor()
    IL_000c:  stloc.0
    IL_000d:  ldc.i4.s   32
    IL_000f:  conv.u
    IL_0010:  localloc
    IL_0012:  ldind.i4
    IL_0013:  call       void GitHub_24278.Program::Use(int32)
    IL_0018:  ldloc.0
    IL_0019:  call       void [System.Runtime]System.GC::KeepAlive(object)
    IL_001e:  ret
  } // end of method Program::MainMethod

  .method private hidebysig static int32
          Main(string[] args) cil managed
  {
    .entrypoint
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  call       void GitHub_24278.Program::MainMethod()
    IL_0005:  ldc.i4.s   100
    IL_0007:  ret
  } // end of method Program::Main
}
