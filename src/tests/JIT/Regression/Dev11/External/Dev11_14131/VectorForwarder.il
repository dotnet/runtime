// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}





// Metadata version: v2.0.50727
.assembly extern mscorlib
{
  .publickeytoken = (7C EC 85 D7 BE A7 79 8E )                         // |.....y.
  .ver 2:0:5:0
}
.assembly VectorForwarder
{

  // --- The following custom attribute is added automatically, do not uncomment -------
  //  .custom instance void [mscorlib]System.Diagnostics.DebuggableAttribute::.ctor(valuetype [mscorlib]System.Diagnostics.DebuggableAttribute/DebuggingModes) = ( 01 00 03 00 00 00 00 00 ) 

  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .hash algorithm 0x00008004
  .ver 4:0:30513:0
}
.assembly extern xunit.core {}
.module VectorForwarder.exe
// MVID: {040B5898-B7A8-44DE-A50F-3465CCB7870F}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x001E0000


// =============== CLASS MEMBERS DECLARATION ===================

.class private abstract auto ansi sealed beforefieldinit VectorForwarder.App
       extends [mscorlib]System.Object
{
  .class auto ansi nested private beforefieldinit CallTarget
         extends [mscorlib]System.Object
  {
    .method public hidebysig instance void 
            Forward(int32 index0,
                    int32 index1,
                    int32 index2,
                    int32 index3,
                    int32 newValue) cil managed noinlining
    {
      // Code size       93 (0x5d)
      .maxstack  4
      .locals init (int32[] V_0)
      IL_0000:  ldstr      "RECEIVED"
      IL_0005:  ldc.i4.4
      IL_0006:  newarr     [mscorlib]System.Int32
      IL_000b:  stloc.0
      IL_000c:  ldloc.0
      IL_000d:  ldc.i4.0
      IL_000e:  ldarg.1
      IL_000f:  stelem.i4
      IL_0010:  ldloc.0
      IL_0011:  ldc.i4.1
      IL_0012:  ldarg.2
      IL_0013:  stelem.i4
      IL_0014:  ldloc.0
      IL_0015:  ldc.i4.2
      IL_0016:  ldarg.3
      IL_0017:  stelem.i4
      IL_0018:  ldloc.0
      IL_0019:  ldc.i4.3
      IL_001a:  ldarg.s    index3
      IL_001c:  stelem.i4
      IL_001d:  ldloc.0
      IL_001e:  call       void VectorForwarder.App::Print4Indices(string,
                                                                   int32[])
      IL_0023:  ldarg.1
      IL_0024:  ldsfld     int32[] VectorForwarder.App::IndicesBeingSent
      IL_0029:  ldc.i4.0
      IL_002a:  ldelem.i4
      IL_002b:  bne.un.s   IL_004c

      IL_002d:  ldarg.2
      IL_002e:  ldsfld     int32[] VectorForwarder.App::IndicesBeingSent
      IL_0033:  ldc.i4.1
      IL_0034:  ldelem.i4
      IL_0035:  bne.un.s   IL_004c

      IL_0037:  ldarg.3
      IL_0038:  ldsfld     int32[] VectorForwarder.App::IndicesBeingSent
      IL_003d:  ldc.i4.2
      IL_003e:  ldelem.i4
      IL_003f:  bne.un.s   IL_004c

      IL_0041:  ldarg.s    index3
      IL_0043:  ldsfld     int32[] VectorForwarder.App::IndicesBeingSent
      IL_0048:  ldc.i4.3
      IL_0049:  ldelem.i4
      IL_004a:  beq.s      IL_005c

      IL_004c:  ldstr      "Failed - received indices that don't match the one"
      + "s that were sent."
      IL_0051:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0056:  ldc.i4.1
      IL_0057:  stsfld     bool VectorForwarder.App::TestFailed
      IL_005c:  ret
    } // end of method CallTarget::Forward

    .method public hidebysig instance int32 
            Get(int32 index0,
                int32 index1,
                int32 index2,
                int32 index3) cil managed noinlining
    {
      // Code size       2 (0x2)
      .maxstack  8
      IL_0000:  ldc.i4.0
      IL_0001:  ret
    } // end of method CallTarget::Get

    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method CallTarget::.ctor

  } // end of class CallTarget

  .field private static int32[] IndicesBeingSent
  .field private static bool TestFailed
  .method private hidebysig static void  Print4Indices(string caption,
                                                       int32[] indices) cil managed
  {
    .param [2]
    .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       67 (0x43)
    .maxstack  5
    .locals init (object[] V_0)
    IL_0000:  ldstr      "{0}: [{1}, {2}, {3}, {4}]"
    IL_0005:  ldc.i4.5
    IL_0006:  newarr     [mscorlib]System.Object
    IL_000b:  stloc.0
    IL_000c:  ldloc.0
    IL_000d:  ldc.i4.0
    IL_000e:  ldarg.0
    IL_000f:  stelem.ref
    IL_0010:  ldloc.0
    IL_0011:  ldc.i4.1
    IL_0012:  ldarg.1
    IL_0013:  ldc.i4.0
    IL_0014:  ldelem.i4
    IL_0015:  box        [mscorlib]System.Int32
    IL_001a:  stelem.ref
    IL_001b:  ldloc.0
    IL_001c:  ldc.i4.2
    IL_001d:  ldarg.1
    IL_001e:  ldc.i4.1
    IL_001f:  ldelem.i4
    IL_0020:  box        [mscorlib]System.Int32
    IL_0025:  stelem.ref
    IL_0026:  ldloc.0
    IL_0027:  ldc.i4.3
    IL_0028:  ldarg.1
    IL_0029:  ldc.i4.2
    IL_002a:  ldelem.i4
    IL_002b:  box        [mscorlib]System.Int32
    IL_0030:  stelem.ref
    IL_0031:  ldloc.0
    IL_0032:  ldc.i4.4
    IL_0033:  ldarg.1
    IL_0034:  ldc.i4.3
    IL_0035:  ldelem.i4
    IL_0036:  box        [mscorlib]System.Int32
    IL_003b:  stelem.ref
    IL_003c:  ldloc.0
    IL_003d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object[])
    IL_0042:  ret
  } // end of method App::Print4Indices

  .method private hidebysig static void  ForwardIndices(class VectorForwarder.App/CallTarget object1,
                                                        class VectorForwarder.App/CallTarget object2,
                                                        int32[] indices) cil managed noinlining
  {
    // Code size       47 (0x2f)
    .maxstack  12
    IL_0000:  ldarg.0
    IL_0001:  ldarg.2
    IL_0002:  ldc.i4.0
    IL_0003:  ldelem.i4
    IL_0004:  ldarg.2
    IL_0005:  ldc.i4.1
    IL_0006:  ldelem.i4
    IL_0007:  ldarg.2
    IL_0008:  ldc.i4.2
    IL_0009:  ldelem.i4
    IL_000a:  ldarg.2
    IL_000b:  ldc.i4.3
    IL_000c:  ldelem.i4
    IL_000d:  ldc.i4.1
    IL_000e:  ldarg.0
    IL_000f:  ldarg.2
    IL_0010:  ldc.i4.0
    IL_0011:  ldelem.i4
    IL_0012:  ldc.i4.1
    IL_0013:  sub
    IL_0014:  ldarg.2
    IL_0015:  ldc.i4.1
    IL_0016:  ldelem.i4
    IL_0017:  ldc.i4.1
    IL_0018:  sub
    IL_0019:  ldarg.2
    IL_001a:  ldc.i4.2
    IL_001b:  ldelem.i4
    IL_001c:  ldc.i4.1
    IL_001d:  sub
    IL_001e:  ldarg.2
    IL_001f:  ldc.i4.3
    IL_0020:  ldelem.i4
    IL_0021:  ldc.i4.1
    IL_0022:  sub
    IL_0023:  callvirt   instance int32 VectorForwarder.App/CallTarget::Get(int32,
                                                                            int32,
                                                                            int32,
                                                                            int32)
    IL_0028:  add
    IL_0029:  callvirt   instance void VectorForwarder.App/CallTarget::Forward(int32,
                                                                               int32,
                                                                               int32,
                                                                               int32,
                                                                               int32)
    IL_002e:  ret
  } // end of method App::ForwardIndices

  .method private hidebysig static int32 
          Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       72 (0x48)
    .maxstack  3
    .locals init (class VectorForwarder.App/CallTarget V_0,
             class VectorForwarder.App/CallTarget V_1)
    IL_0000:  newobj     instance void VectorForwarder.App/CallTarget::.ctor()
    IL_0005:  stloc.0
    IL_0006:  newobj     instance void VectorForwarder.App/CallTarget::.ctor()
    IL_000b:  stloc.1
    IL_000c:  ldstr      "SENDING"
    IL_0011:  ldsfld     int32[] VectorForwarder.App::IndicesBeingSent
    IL_0016:  call       void VectorForwarder.App::Print4Indices(string,
                                                                 int32[])
    IL_001b:  ldloc.0
    IL_001c:  ldloc.1
    IL_001d:  ldsfld     int32[] VectorForwarder.App::IndicesBeingSent
    IL_0022:  call       void VectorForwarder.App::ForwardIndices(class VectorForwarder.App/CallTarget,
                                                                  class VectorForwarder.App/CallTarget,
                                                                  int32[])
    IL_0027:  ldsfld     bool VectorForwarder.App::TestFailed
    IL_002c:  brtrue.s   IL_003b

    IL_002e:  ldstr      "Passed."
    IL_0033:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0038:  ldc.i4.s   100
    IL_003a:  ret

    IL_003b:  ldstr      "Failed."
    IL_0040:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0045:  ldc.i4.s   101
    IL_0047:  ret
  } // end of method App::Main

  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       29 (0x1d)
    .maxstack  8
    IL_0000:  ldc.i4.4
    IL_0001:  newarr     [mscorlib]System.Int32
    IL_0006:  dup
    IL_0007:  ldtoken    field valuetype '<PrivateImplementationDetails>{040B5898-B7A8-44DE-A50F-3465CCB7870F}'/'__StaticArrayInitTypeSize=16' '<PrivateImplementationDetails>{040B5898-B7A8-44DE-A50F-3465CCB7870F}'::'$$method0x6000007-1'
    IL_000c:  call       void [mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array,
                                                                                                        valuetype [mscorlib]System.RuntimeFieldHandle)
    IL_0011:  stsfld     int32[] VectorForwarder.App::IndicesBeingSent
    IL_0016:  ldc.i4.0
    IL_0017:  stsfld     bool VectorForwarder.App::TestFailed
    IL_001c:  ret
  } // end of method App::.cctor

} // end of class VectorForwarder.App

.class private auto ansi '<PrivateImplementationDetails>{040B5898-B7A8-44DE-A50F-3465CCB7870F}'
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
  .class explicit ansi sealed nested private '__StaticArrayInitTypeSize=16'
         extends [mscorlib]System.ValueType
  {
    .pack 1
    .size 16
  } // end of class '__StaticArrayInitTypeSize=16'

  .field static assembly valuetype '<PrivateImplementationDetails>{040B5898-B7A8-44DE-A50F-3465CCB7870F}'/'__StaticArrayInitTypeSize=16' '$$method0x6000007-1' at I_00002130
} // end of class '<PrivateImplementationDetails>{040B5898-B7A8-44DE-A50F-3465CCB7870F}'


// =============================================================

.data cil I_00002130 = bytearray (
                 02 00 00 00 02 00 00 00 02 00 00 00 02 00 00 00) 
// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file IL\VectorForwarder.res
