// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


// This test shows an issue  with tail call optimization of an interface call through Virtual Dispatch Stub.
// The optimized VSD checks that `this` method table is equal to the cached method table.
// If `this` is `null` then the stub throws `Access Violation` exception that VM should
// transform into the NullReference exception. VM is not able to do that when the first frame on the stack is
// unmanaged, in this case it is JIT_TailCallHelperStub_ReturnAddress from tail call optimization.
// To repro this issue on amd64/arm64 it also requires to reject fast tail call, for this puprose
// `Interface::InterfaceMethodWithStackArgs` has several stack arguments.

// The fix is to always generate explicit null check before calling VSD with tail call from tail call frame.


.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern System.Runtime.Extensions
{
}
.assembly GitHub_17585
{
}
.assembly extern xunit.core {}


// =============== CLASS MEMBERS DECLARATION ===================

.class interface private abstract auto ansi Interface
{
  .method public hidebysig newslot abstract virtual
          instance int32  InterfaceMethodWithStackArgs(int32 a,
                                                       int32 b,
                                                       int32 c,
                                                       int32 d,
                                                       int32 e,
                                                       int32 f,
                                                       int32 g,
                                                       int32 h,
                                                       int32 i) cil managed
  {
  } // end of method Interface::InterfaceMethodWithStackArgs

} // end of class Interface

.class private auto ansi beforefieldinit InterfaceImplementation
       extends [System.Runtime]System.Object
       implements Interface
{
  .method public hidebysig newslot virtual final
          instance int32  InterfaceMethodWithStackArgs(int32 a,
                                                       int32 b,
                                                       int32 c,
                                                       int32 d,
                                                       int32 e,
                                                       int32 f,
                                                       int32 g,
                                                       int32 h,
                                                       int32 i) cil managed noinlining
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  ldc.i4.1
    IL_0001:  ret
  } // end of method InterfaceImplementation::InterfaceMethodWithStackArgs

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  ret
  } // end of method InterfaceImplementation::.ctor

} // end of class InterfaceImplementation

.class public auto ansi beforefieldinit Test_GitHub_17585
       extends [System.Runtime]System.Object
{
  .method public hidebysig static int32  CallInterfaceMethod(class Interface iface) cil managed noinlining
  {
    // Code size       17 (0x11)
    .maxstack  10
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.1
    IL_0002:  ldc.i4.2
    IL_0003:  ldc.i4.3
    IL_0004:  ldc.i4.4
    IL_0005:  ldc.i4.5
    IL_0006:  ldc.i4.6
    IL_0007:  ldc.i4.7
    IL_0008:  ldc.i4.8
    IL_0009:  ldc.i4.s   9
    IL_000b:  tail. callvirt   instance int32 Interface::InterfaceMethodWithStackArgs(int32,
                                                                                int32,
                                                                                int32,
                                                                                int32,
                                                                                int32,
                                                                                int32,
                                                                                int32,
                                                                                int32,
                                                                                int32)
    IL_0010:  ret
  } // end of method Test::CallInterfaceMethod

  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       35 (0x23)
    .maxstack  1
    .locals init ([0] class InterfaceImplementation iface)
    IL_0000:  newobj     instance void InterfaceImplementation::.ctor()
    IL_0005:  stloc.0
    IL_0006:  ldloc.0
    IL_0007:  call       int32 Test_GitHub_17585::CallInterfaceMethod(class Interface)
    IL_000c:  pop
    IL_000d:  ldloc.0
    IL_000e:  call       int32 Test_GitHub_17585::CallInterfaceMethod(class Interface)
    IL_0013:  pop
    .try
    {
      IL_0014:  ldnull
      IL_0015:  call       int32 Test_GitHub_17585::CallInterfaceMethod(class Interface)
      IL_001a:  pop
      IL_001b:  leave.s    IL_0020

    }  // end .try
    catch [System.Runtime]System.NullReferenceException
    {
      IL_001d:  pop
      IL_001e:  leave.s    IL_0020

    }  // end handler
    IL_0020:  ldc.i4.s   100
    IL_0022:  ret
  } // end of method Test::Main

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Test::.ctor

} // end of class Test
