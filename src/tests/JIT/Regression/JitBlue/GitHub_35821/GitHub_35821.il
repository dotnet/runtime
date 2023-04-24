// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// Metadata version: v4.0.30319
.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern System.Runtime.Intrinsics
{
  .publickeytoken = (CC 7B 13 FF CD 2D DD 51 )                         // .{...-.Q
  .ver 5:0:0:0
}
.assembly 'GitHub_35821' { }
.assembly extern xunit.core {}
// MVID: {379016DB-73C2-41D4-9E5F-5B727BC70E2C}
.custom instance void [System.Runtime]System.Security.UnverifiableCodeAttribute::.ctor() = ( 01 00 00 00 ) 
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x00000293F3DD0000


// =============== CLASS MEMBERS DECLARATION ===================
// This bug was found when passing Vector64<long> to a method such that
// the vector is on the evaluation stack. C# sometimes assign it the vector64
// to local variable before passing it to method. In such cases, the bug
// doesn't repro.
.class public auto ansi sealed beforefieldinit projs.GitHub_35821
       extends [System.Runtime]System.Object
{
  .method public hidebysig static int32 
          Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       48 (0x30)
    .maxstack  8
    IL_0000:  ldc.i4.s   23
    IL_0002:  conv.i8
    IL_0003:  call       valuetype [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Vector64`1<uint64> [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Vector64::Create(uint64)
    IL_0008:  call       void projs.GitHub_35821::Test1(valuetype [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Vector64`1<uint64>)
    IL_000d:  ldc.i4.s   23
    IL_000f:  conv.i8
    IL_0010:  call       valuetype [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Vector64`1<int64> [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Vector64::Create(int64)
    IL_0015:  call       void projs.GitHub_35821::Test2(valuetype [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Vector64`1<int64>)
    IL_001a:  ldc.r8     23.
    IL_0023:  call       valuetype [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Vector64`1<float64> [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Vector64::Create(float64)
    IL_0028:  call       void projs.GitHub_35821::Test3(valuetype [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Vector64`1<float64>)
    IL_002d:  ldc.i4.s   100
    IL_002f:  ret
  } // end of method GitHub_35821::Main

  .method public hidebysig static void  Test1(valuetype [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Vector64`1<uint64> data) cil managed noinlining
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method GitHub_35821::Test1

  .method public hidebysig static void  Test2(valuetype [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Vector64`1<int64> data) cil managed noinlining
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method GitHub_35821::Test2

  .method public hidebysig static void  Test3(valuetype [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Vector64`1<float64> data) cil managed noinlining
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method GitHub_35821::Test3

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  ret
  } // end of method GitHub_35821::.ctor

} // end of class projs.GitHub_35821


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
