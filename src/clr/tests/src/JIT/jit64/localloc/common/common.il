// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


.assembly extern System.Console { auto }
.assembly extern mscorlib { auto }

.assembly common
{
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module common.dll
.custom instance void [mscorlib]System.Security.UnverifiableCodeAttribute::.ctor() = ( 01 00 00 00 ) 
.imagebase 0x00400000
.subsystem 0x00000003
.file alignment 512
.corflags 0x00000001
.namespace LocallocTesting
{
  .class public auto ansi beforefieldinit Global
         extends [mscorlib]System.Object
  {
  } // end of class Global

  .class public auto ansi beforefieldinit TestClass
         extends [mscorlib]System.Object
  {
  } // end of class TestClass

} // end of namespace LocallocTesting









.namespace LocallocTesting
{
  .class public auto ansi beforefieldinit Global
         extends [mscorlib]System.Object
  {
    .field public static literal unsigned int64 INITIAL_VALUE = int64(0xFEDCBA9876543210)
    .field public static int32 stackAllocSize
    .method public hidebysig static bool 
            verifyLocal(string name,
                        unsigned int64 i1,
                        unsigned int64 i2) cil managed
    {
      .maxstack  3
      .locals (bool V_0,
               string[] V_1)
      IL_0000:  ldarg.1
      IL_0001:  ldarg.2
      IL_0002:  bne.un.s   IL_0008

      IL_0004:  ldc.i4.1
      IL_0005:  stloc.0
      IL_0006:  br.s       IL_004e

      IL_0008:  ldc.i4.6
      IL_0009:  newarr     [mscorlib]System.String
      IL_000e:  stloc.1
      IL_000f:  ldloc.1
      IL_0010:  ldc.i4.0
      IL_0011:  ldstr      "Local variable('"
      IL_0016:  stelem.ref
      IL_0017:  ldloc.1
      IL_0018:  ldc.i4.1
      IL_0019:  ldarg.0
      IL_001a:  stelem.ref
      IL_001b:  ldloc.1
      IL_001c:  ldc.i4.2
      IL_001d:  ldstr      "') contains "
      IL_0022:  stelem.ref
      IL_0023:  ldloc.1
      IL_0024:  ldc.i4.3
      IL_0025:  ldarga.s   i1
      IL_0027:  call       instance string [mscorlib]System.UInt64::ToString()
      IL_002c:  stelem.ref
      IL_002d:  ldloc.1
      IL_002e:  ldc.i4.4
      IL_002f:  ldstr      ". Expect="
      IL_0034:  stelem.ref
      IL_0035:  ldloc.1
      IL_0036:  ldc.i4.5
      IL_0037:  ldarga.s   i2
      IL_0039:  call       instance string [mscorlib]System.UInt64::ToString()
      IL_003e:  stelem.ref
      IL_003f:  ldloc.1
      IL_0040:  call       string [mscorlib]System.String::Concat(string[])
      IL_0045:  call       void [System.Console]System.Console::WriteLine(string)
      IL_004a:  ldc.i4.0
      IL_004b:  stloc.0
      IL_004c:  br.s       IL_004e

      IL_004e:  ldloc.0
      IL_004f:  ret
    } // end of method Global::verifyLocal

    .method public hidebysig static void 
            initializeStack(int32* pArray,
                            int32 size,
                            int32 initialValue) cil managed
    {
      .maxstack  3
      .locals (int32 V_0)
      IL_0000:  ldc.i4.0
      IL_0001:  stloc.0
      IL_0002:  br.s       IL_0012

      IL_0004:  ldarg.0
      IL_0005:  ldloc.0
      IL_0006:  conv.i
      IL_0007:  ldc.i4.4
      IL_0008:  mul
      IL_0009:  add
      IL_000a:  ldarg.2
      IL_000b:  ldloc.0
      IL_000c:  add
      IL_000d:  stind.i4
      IL_000e:  ldloc.0
      IL_000f:  ldc.i4.1
      IL_0010:  add
      IL_0011:  stloc.0
      IL_0012:  ldloc.0
      IL_0013:  ldarg.1
      IL_0014:  blt.s      IL_0004

      IL_0016:  ret
    } // end of method Global::initializeStack

    .method public hidebysig static bool 
            verifyStack(string name,
                        int32* pArray,
                        int32 size,
                        int32 initialValue) cil managed
    {
      .maxstack  5
      .locals (int32 V_0,
               bool V_1,
               object[] V_2)
      IL_0000:  ldc.i4.0
      IL_0001:  stloc.0
      IL_0002:  br.s       IL_0079

      IL_0004:  ldarg.1
      IL_0005:  ldloc.0
      IL_0006:  conv.i
      IL_0007:  ldc.i4.4
      IL_0008:  mul
      IL_0009:  add
      IL_000a:  ldind.i4
      IL_000b:  ldarg.3
      IL_000c:  ldloc.0
      IL_000d:  add
      IL_000e:  beq.s      IL_0075

      IL_0010:  ldc.i4.s   9
      IL_0012:  newarr     [mscorlib]System.Object
      IL_0017:  stloc.2
      IL_0018:  ldloc.2
      IL_0019:  ldc.i4.0
      IL_001a:  ldstr      "Stack('"
      IL_001f:  stelem.ref
      IL_0020:  ldloc.2
      IL_0021:  ldc.i4.1
      IL_0022:  ldarg.0
      IL_0023:  stelem.ref
      IL_0024:  ldloc.2
      IL_0025:  ldc.i4.2
      IL_0026:  ldstr      "') corrupted at index="
      IL_002b:  stelem.ref
      IL_002c:  ldloc.2
      IL_002d:  ldc.i4.3
      IL_002e:  ldloca.s   V_0
      IL_0030:  call       instance string [mscorlib]System.Int32::ToString()
      IL_0035:  stelem.ref
      IL_0036:  ldloc.2
      IL_0037:  ldc.i4.4
      IL_0038:  ldstr      ".  Value="
      IL_003d:  stelem.ref
      IL_003e:  ldloc.2
      IL_003f:  ldc.i4.5
      IL_0040:  ldarg.1
      IL_0041:  ldloc.0
      IL_0042:  conv.i
      IL_0043:  ldc.i4.4
      IL_0044:  mul
      IL_0045:  add
      IL_0046:  call       instance string [mscorlib]System.Int32::ToString()
      IL_004b:  stelem.ref
      IL_004c:  ldloc.2
      IL_004d:  ldc.i4.6
      IL_004e:  ldstr      ".  Expect="
      IL_0053:  stelem.ref
      IL_0054:  ldloc.2
      IL_0055:  ldc.i4.7
      IL_0056:  ldarg.3
      IL_0057:  box        [mscorlib]System.Int32
      IL_005c:  stelem.ref
      IL_005d:  ldloc.2
      IL_005e:  ldc.i4.8
      IL_005f:  ldloc.0
      IL_0060:  box        [mscorlib]System.Int32
      IL_0065:  stelem.ref
      IL_0066:  ldloc.2
      IL_0067:  call       string [mscorlib]System.String::Concat(object[])
      IL_006c:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0071:  ldc.i4.0
      IL_0072:  stloc.1
      IL_0073:  br.s       IL_0081

      IL_0075:  ldloc.0
      IL_0076:  ldc.i4.1
      IL_0077:  add
      IL_0078:  stloc.0
      IL_0079:  ldloc.0
      IL_007a:  ldarg.2
      IL_007b:  blt.s      IL_0004

      IL_007d:  ldc.i4.1
      IL_007e:  stloc.1
      IL_007f:  br.s       IL_0081

      IL_0081:  ldloc.1
      IL_0082:  ret
    } // end of method Global::verifyStack

    .method private hidebysig specialname rtspecialname static 
            void  .cctor() cil managed
    {
      .maxstack  8
      IL_0000:  ldc.i4.s   10
      IL_0002:  stsfld     int32 LocallocTesting.Global::stackAllocSize
      IL_0007:  ret
    } // end of method Global::.cctor

    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method Global::.ctor

  } // end of class Global

  .class public auto ansi beforefieldinit TestClass
         extends [mscorlib]System.Object
  {
    .field public int64 id
    .field public static bool flag
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor(int64 i) cil managed
    {
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ldarg.0
      IL_0007:  ldarg.1
      IL_0008:  stfld      int64 LocallocTesting.TestClass::id
      IL_000d:  ret
    } // end of method TestClass::.ctor

    .method family hidebysig virtual instance void 
            Finalize() cil managed
    {
      .maxstack  1
      .try
      {
        IL_0000:  ldc.i4.1
        IL_0001:  stsfld     bool LocallocTesting.TestClass::flag
        IL_0006:  leave.s    IL_000f

      }  // end .try
      finally
      {
        IL_0008:  ldarg.0
        IL_0009:  call       instance void [mscorlib]System.Object::Finalize()
        IL_000e:  endfinally
      }  // end handler
      IL_000f:  ret
    } // end of method TestClass::Finalize

    .method public hidebysig instance bool 
            ForceGC() cil managed
    {
      .maxstack  3
      .locals (int32 V_0,
               class LocallocTesting.TestClass V_1,
               bool V_2)
      IL_0000:  ldc.i4.0
      IL_0001:  stsfld     bool LocallocTesting.TestClass::flag
      IL_0006:  ldc.i4.0
      IL_0007:  stloc.0
      IL_0008:  br.s       IL_0016

      IL_000a:  ldloc.0
      IL_000b:  dup
      IL_000c:  ldc.i4.1
      IL_000d:  add
      IL_000e:  stloc.0
      IL_000f:  conv.i8
      IL_0010:  newobj     instance void LocallocTesting.TestClass::.ctor(int64)
      IL_0015:  stloc.1
      IL_0016:  ldsfld     bool LocallocTesting.TestClass::flag
      IL_001b:  brtrue.s   IL_0025

      IL_001d:  ldloc.0
      IL_001e:  ldc.i4     0x186a0
      IL_0023:  blt.s      IL_000a

      IL_0025:  call       void [mscorlib]System.GC::Collect()
      IL_002a:  call       void [mscorlib]System.GC::WaitForPendingFinalizers()
      IL_002f:  ldsfld     bool LocallocTesting.TestClass::flag
      IL_0034:  stloc.2
      IL_0035:  br.s       IL_0037

      IL_0037:  ldloc.2
      IL_0038:  ret
    } // end of method TestClass::ForceGC

    .method private hidebysig specialname rtspecialname static 
            void  .cctor() cil managed
    {
      .maxstack  8
      IL_0000:  ldc.i4.0
      IL_0001:  stsfld     bool LocallocTesting.TestClass::flag
      IL_0006:  ret
    } // end of method TestClass::.cctor

  } // end of class TestClass



} // end of namespace LocallocTesting

