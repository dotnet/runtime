// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }

.assembly extern System.Runtime.CompilerServices.Unsafe
{
}

.assembly extern System.Console
{
}

.assembly Runtime_41073
{
}
.assembly extern xunit.core {}

.class public auto ansi beforefieldinit Runtime_41073
       extends [System.Runtime]System.Object
{
  .method private hidebysig static float32 
          PassNullByref(float32 f) cil managed noinlining
  {
    // Code size       20 (0x14)
    .maxstack  8
    IL_0000:  ldarga.s   f
    IL_0007:  ldarga.s   f
    IL_000e:  ldind.i8
    IL_000f:  ldc.i8 123
    IL_0010:  add
    IL_0011:  stind.i8
    IL_0012:  ldarg.0
    IL_0013:  ret
  } // end of method Runtime_41073::PassNullByref

  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       27 (0x1b)
    .maxstack  1
    .locals init (float32 V_0)
    IL_0000:  ldc.r4     0.0
    IL_0005:  call       float32 Runtime_41073::PassNullByref(float32)
    IL_000a:  stloc.0
    IL_000b:  ldloca.s   V_0
    IL_000d:  call       !!1& [System.Runtime.CompilerServices.Unsafe]System.Runtime.CompilerServices.Unsafe::As<float32,int32>(!!0&)
    IL_0012:  ldind.i4
    IL_0013:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_0018:  ldc.i4.s   100
    IL_001a:  ret
  } // end of method Runtime_41073::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Runtime_41073::.ctor

} // end of class Runtime_41073
