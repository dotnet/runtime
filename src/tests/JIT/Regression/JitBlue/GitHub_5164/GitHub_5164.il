// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// The tail call helper JIT_TailCall has an implicit assumption that all
// arguments of the tail call are allocated on the caller's frame.
// In this test A tail-calls B and B tail-calls A: Main --> A --> B --> A --> B --> A.
// In the last B --> A tail call the frame from the preceding A --> B tail call is resused.
// It's big enough because of the first B --> A tail call.
// The bug was that the code for B wasn't copying the incoming struct parameter to its frame.
// The struct lived on the caller's frame and when it was reused for the B --> A tail call,
// the struct was overwritten.

.assembly extern mscorlib { }

.assembly GitHub_5164 { }
.assembly extern xunit.core {}

.class public sequential ansi sealed beforefieldinit LargeStruct
       extends [mscorlib]System.ValueType
{
  .field public int64 l1
  .field public int64 l2
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(int64 l1,
                               int64 l2) cil managed
  {
    // Code size       15 (0xf)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  stfld      int64 LargeStruct::l1
    IL_0007:  ldarg.0
    IL_0008:  ldarg.2
    IL_0009:  stfld      int64 LargeStruct::l2
    IL_000e:  ret
  } // end of method LargeStruct::.ctor

} // end of class LargeStruct

.class private auto ansi beforefieldinit Test_GitHub_5164
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       48 (0x30)
    .maxstack  3
    .locals init (valuetype LargeStruct V_0,
             valuetype LargeStruct V_1,
             valuetype LargeStruct V_2)
    IL_0000:  ldloca.s   V_0
    IL_0002:  ldc.i4.1
    IL_0003:  conv.i8
    IL_0004:  ldc.i4.2
    IL_0005:  conv.i8
    IL_0006:  call       instance void LargeStruct::.ctor(int64,
                                                          int64)
    IL_000b:  ldloca.s   V_1
    IL_000d:  ldc.i4.3
    IL_000e:  conv.i8
    IL_000f:  ldc.i4.4
    IL_0010:  conv.i8
    IL_0011:  call       instance void LargeStruct::.ctor(int64,
                                                          int64)
    IL_0016:  ldc.i4.0
    IL_0017:  ldloc.0
    IL_0018:  ldloc.1
    IL_0019:  call       valuetype LargeStruct Test_GitHub_5164::A(int32,
                                                       valuetype LargeStruct,
                                                       valuetype LargeStruct)
    IL_001e:  stloc.2
    IL_001f:  ldloca.s   V_2
    IL_0021:  ldfld      int64 LargeStruct::l1
    IL_0026:  ldloca.s   V_2
    IL_0028:  ldfld      int64 LargeStruct::l2
    IL_002d:  add
    IL_002e:  conv.i4
    IL_002f:  ret
  } // end of method Test::Main

  .method public hidebysig static valuetype LargeStruct 
          A(int32 count,
            valuetype LargeStruct s1,
            valuetype LargeStruct s2) cil managed noinlining
  {
    // Code size       58 (0x3a)
    .maxstack  4
    .locals init (valuetype LargeStruct V_0)
    IL_0000:  ldloca.s   V_0
    IL_0002:  ldarga.s   s1
    IL_0004:  ldfld      int64 LargeStruct::l1
    IL_0009:  ldarga.s   s2
    IL_000b:  ldfld      int64 LargeStruct::l1
    IL_0010:  add
    IL_0011:  ldarga.s   s1
    IL_0013:  ldfld      int64 LargeStruct::l2
    IL_0018:  ldarga.s   s2
    IL_001a:  ldfld      int64 LargeStruct::l2
    IL_001f:  add
    IL_0020:  call       instance void LargeStruct::.ctor(int64,
                                                          int64)
    IL_0025:  ldarg.0
    IL_0026:  ldc.i4.3
    IL_0027:  bge.s      IL_0038

    IL_0029:  ldarg.0
    IL_002a:  ldc.i4.1
    IL_002b:  add
    IL_002c:  dup
    IL_002d:  starg.s    count
    IL_002f:  ldloc.0
    IL_0030:  tail.
    IL_0032:  call       valuetype LargeStruct Test_GitHub_5164::B(int32,
                                                       valuetype LargeStruct)
    IL_0037:  ret

    IL_0038:  ldloc.0
    IL_0039:  ret
  } // end of method Test::A

  .method public hidebysig static valuetype LargeStruct 
          B(int32 count,
            valuetype LargeStruct s) cil managed noinlining
  {
    // Code size       28 (0x1c)
    .maxstack  3
    .locals init (valuetype LargeStruct V_0)
    IL_0000:  ldloca.s   V_0
    IL_0002:  ldc.i4.1
    IL_0003:  conv.i8
    IL_0004:  ldc.i4.s   44
    IL_0006:  conv.i8
    IL_0007:  call       instance void LargeStruct::.ctor(int64,
                                                          int64)
    IL_000c:  ldarg.0
    IL_000d:  ldc.i4.1
    IL_000e:  add
    IL_000f:  dup
    IL_0010:  starg.s    count
    IL_0012:  ldloc.0
    IL_0013:  ldarg.1
    IL_0014:  tail.
    IL_0016:  call       valuetype LargeStruct Test_GitHub_5164::A(int32,
                                                       valuetype LargeStruct,
                                                       valuetype LargeStruct)
    IL_001b:  ret
  } // end of method Test::B

} // end of class Test

