// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}

// Test zero initialization of localloc
.assembly extern mscorlib{}
.assembly 'zeroInit01_small_'{}

.class public auto ansi beforefieldinit T
       extends [mscorlib]System.Object
{
  .field public static int32 size_to_test
  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       79 (0x4f)
    .maxstack  3
    .locals init(bool passed,
             int32* buffer,
             int32 n)
    // Initialize 'passed' to true.
    IL_0000:  ldc.i4.1
    IL_0001:  stloc.0
    
    IL_0002:  call       void T::dirtyStack()
    // localloc
    IL_0007:  ldc.i4.4
    IL_0008:  ldc.i4.1
    IL_000d:  mul
    IL_000e:  localloc
    IL_0010:  stloc.1
    // Set size of localloc
    IL_0011:  ldc.i4.1
    IL_0016:  stsfld     int32 T::size_to_test
    
    IL_001b:  ldc.i4.0
    IL_001c:  stloc.2
    IL_001d:  br.s       L_LOOP2

    L_LOOP1:  ldloc.1
    IL_0020:  ldc.i4.4
    IL_0021:  ldloc.2
    IL_0022:  mul
    IL_0023:  add
    IL_0024:  ldind.i4
    IL_0025:  brfalse.s  IL_0029
	// Test failed. (!= zero)
    IL_0027:  ldc.i4.0
    IL_0028:  stloc.0
    // n = n+1
    IL_0029:  ldloc.2
    IL_002a:  ldc.i4.1
    IL_002b:  add
    IL_002c:  stloc.2
    L_LOOP2:  ldloc.2
    IL_002e:  ldsfld     int32 T::size_to_test
    IL_0033:  blt.s      L_LOOP1

    IL_0035:  ldloc.0
    IL_0036:  brfalse.s  L_FAIL

    IL_0038:  ldstr      "pass"
    IL_003d:  call       void [System.Console]System.Console::WriteLine(string)
    ldc.i4	100
    IL_0042:  br.s       IL_004e

    L_FAIL:   ldstr      "fail"
    IL_0049:  call       void [System.Console]System.Console::WriteLine(string)
    ldc.i4.1
    IL_004e:  ret
  } // end of method T::Main

  .method public hidebysig static void  dirtyStack() cil managed
  {
    // Code size       34 (0x22)
    .maxstack  3
    .locals (int32* buffer,
             int32 n)
    IL_0000:  ldc.i4.4
    IL_0001:  ldsfld     int32 T::size_to_test
    IL_0006:  mul
    IL_0007:  localloc
    IL_0009:  stloc.0
    IL_000a:  ldc.i4.0
    IL_000b:  stloc.1
    IL_000c:  br.s       IL_0019

    IL_000e:  ldloc.0
    IL_000f:  ldc.i4.4
    IL_0010:  ldloc.1
    IL_0011:  mul
    IL_0012:  add
    IL_0013:  ldloc.1
    IL_0014:  stind.i4
    IL_0015:  ldloc.1
    IL_0016:  ldc.i4.1
    IL_0017:  add
    IL_0018:  stloc.1
    IL_0019:  ldloc.1
    IL_001a:  ldsfld     int32 T::size_to_test
    IL_001f:  blt.s      IL_000e

    IL_0021:  ret
  } // end of method T::dirtyStack

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method T::.ctor

} // end of class T
