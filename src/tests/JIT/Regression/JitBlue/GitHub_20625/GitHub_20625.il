// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// GitHub 20625: bug with LowerFastTailCall on platforms where fast tail call is supported.
// Lower fast tail call can replace local variables (holding Caller stack arguments) with new temps
// in order to set up Callee stack arguments correctly.
// This involves patching corresponding LCL_VAR and LCL_VAR_ADDR nodes and replacing them with the location
// of a new temp. This was not done for control expression which continued pointing to the old location
// and could contain a Callee argument.
// In this example, in Der1:M during tail call Caller arg6 points to the same location as Callee arg6
// (which is null) and causes NullReferenceException.

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }

.assembly GitHub_20625
{
}
.assembly extern xunit.core {}

.class private abstract auto ansi beforefieldinit Base
       extends [System.Runtime]System.Object
{
  .method public hidebysig newslot abstract virtual
          instance void  M(int32 arg1, int32 arg2, int32 arg3, int32 arg4, int32 arg5, class Base arg6) cil managed
  {
  }

  .method family hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  ret
  }
}

.class private auto ansi beforefieldinit Der1
       extends Base
{
  .method public hidebysig virtual instance void
          M(int32 arg1, int32 arg2, int32 arg3, int32 arg4, int32 arg5, class Base arg6) cil managed nooptimization
  {
    .maxstack  8
    IL_0000:  ldarg.s    arg6
    IL_0002:  ldarg.1
    IL_0003:  ldarg.2
    IL_0004:  ldarg.3
    IL_0005:  ldarg.s    arg4
    IL_0007:  ldarg.s    arg5
    IL_0009:  ldnull
    IL_000a:  tail.
              callvirt   instance void Base::M(int32, int32, int32, int32, int32, class Base)
    IL_000f:  ret
  }

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void Base::.ctor()
    IL_0006:  ret
  }

}

.class private auto ansi beforefieldinit Der2
       extends Base
{
  .method public hidebysig virtual instance void
          M(int32 arg1, int32 arg2, int32 arg3, int32 arg4, int32 arg5, class Base arg6) cil managed
  {
    .maxstack  8
    IL_0000:  ret
  }

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void Base::.ctor()
    IL_0006:  ret
  }
}

.class public auto ansi beforefieldinit Program
       extends [System.Runtime]System.Object
{
  .method private hidebysig static int32
          Run(class Base der1, class Base der2) cil managed noinlining
  {
    .maxstack  7
    .locals init (int32 V_0)
    .try
    {
      IL_0000:  ldarg.0
      IL_0001:  ldc.i4.2
      IL_0002:  ldc.i4.0
      IL_0003:  ldc.i4.6
      IL_0004:  ldc.i4.2
      IL_0005:  ldc.i4.5
      IL_0006:  ldarg.1
      IL_0007:  callvirt   instance void Base::M(int32, int32, int32, int32, int32, class Base)
      IL_000c:  ldc.i4.s   100
      IL_000e:  stloc.0
      IL_000f:  leave.s    IL_0016

    }
    catch [System.Runtime]System.NullReferenceException
    {
      IL_0011:  pop
      IL_0012:  ldc.i4.1
      IL_0013:  stloc.0
      IL_0014:  leave.s    IL_0016
    }
    IL_0016:  ldloc.0
    IL_0017:  ret
  }

  .method public hidebysig static int32
          Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint

    .maxstack  2
    .locals init (class Der2 V_0)
    IL_0000:  newobj     instance void Der1::.ctor()
    IL_0005:  newobj     instance void Der2::.ctor()
    IL_000a:  stloc.0
    IL_000b:  ldloc.0
    IL_000c:  call       int32 Program::Run(class Base, class Base)
    IL_0011:  ret
  }

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  ret
  }
}
