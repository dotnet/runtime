// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//different ways to invoke method inside a loop (no tail. call or jmp/jmpi since they require empty stack)
.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'loop3' {}
.class private auto ansi beforefieldinit A
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot virtual 
          instance int32  f0(int32 i) 
  {
    // Code size       6 (0x6)
    .maxstack  1
    .locals (int32 V_0)
    IL_0000:  ldc.i4.1
    IL_0001:  stloc.0
    IL_0002:  br.s       IL_0004

    IL_0004:  ldloc.0
    IL_0005:  ret
  } // end of method A::f0

  .method public hidebysig specialname rtspecialname 
          instance void .ctor() 
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method A::.ctor

} // end of class A

.class public auto ansi beforefieldinit B
       extends A
{
  .method public hidebysig virtual instance int32 
          f0(int32 i) 
  {
    // Code size       6 (0x6)
    .maxstack  1
    .locals (int32 V_0)
    IL_0000:  ldarg.1
    IL_0001:  stloc.0
    IL_0002:  br.s       IL_0004

    IL_0004:  ldloc.0
    IL_0005:  ret
  } // end of method B::f0

  .method public hidebysig static int32  f1(int32& i) 
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals (int32 V_0)
    IL_0000:  ldarg.0
    IL_0001:  ldind.i4
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method B::f1

  .method public hidebysig instance int32 
          f(int32 i) 
  {
    // Code size       12 (0xc)
    .maxstack  1
    .locals (int32 V_0)
    IL_0000:  ldarga.s   i
    IL_0002:  call       int32 B::f1(int32&)
    IL_0007:  stloc.0
    IL_0008:  br.s       IL_000a

    IL_000a:  ldloc.0
    IL_000b:  ret
  } // end of method B::f

  .method public hidebysig static int32  F1downBy1ge(int32 amount) 
  {
    // Code size       38 (0x26)
    .maxstack  3
    .locals (int32 V_0,
             int32 V_1,
             class B V_2,
             int32 V_3)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.1
    IL_0002:  newobj     instance void B::.ctor()
    IL_0007:  stloc.2
    IL_0008:  ldc.i4.4
    IL_0009:  stloc.0
    IL_000a:  br.s       IL_001a

    IL_000c:  ldloc.1
    IL_000d:  ldloc.2
    IL_000e:  ldarg.0
    IL_000f:  callvirt   instance int32 B::f(int32)
    IL_0014:  add
    IL_0015:  stloc.1
    IL_0016:  ldloc.0
    IL_0017:  ldc.i4.1
    IL_0018:  sub
    IL_0019:  stloc.0
    IL_001a:  ldloc.0
    IL_001b:  ldc.i4.1
    IL_001c:  bge.s      IL_000c

    IL_001e:  ldloc.1
    IL_001f:  ldloc.0
    IL_0020:  add
    IL_0021:  stloc.3
    IL_0022:  br.s       IL_0024

    IL_0024:  ldloc.3
    IL_0025:  ret
  } // end of method B::F1downBy1ge

  .method public hidebysig static int32  F1downBy2ne(int32 amount) 
  {
    // Code size       38 (0x26)
    .maxstack  3
    .locals (int32 V_0,
             int32 V_1,
             class B V_2,
             int32 V_3)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.1
    IL_0002:  newobj     instance void B::.ctor()
    IL_0007:  stloc.2
    IL_0008:  ldc.i4.5
    IL_0009:  stloc.0
    IL_000a:  br.s       IL_001a

    IL_000c:  ldloc.1
    IL_000d:  ldloc.2
    IL_000e:  ldarg.0
    IL_000f:  callvirt   instance int32 B::f(int32)
    IL_0014:  add
    IL_0015:  stloc.1
    IL_0016:  ldloc.0
    IL_0017:  ldc.i4.2
    IL_0018:  sub
    IL_0019:  stloc.0
    IL_001a:  ldloc.0
    IL_001b:  ldc.i4.1
    IL_001c:  bne.un.s   IL_000c

    IL_001e:  ldloc.1
    IL_001f:  ldloc.0
    IL_0020:  add
    IL_0021:  stloc.3
    IL_0022:  br.s       IL_0024

    IL_0024:  ldloc.3
    IL_0025:  ret
  } // end of method B::F1downBy2ne

  .method public hidebysig static int32  F1upBy1le(int32 amount) 
  {
    // Code size       38 (0x26)
    .maxstack  3
    .locals (int32 V_0,
             int32 V_1,
             class B V_2,
             int32 V_3)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.1
    IL_0002:  newobj     instance void B::.ctor()
    IL_0007:  stloc.2
    IL_0008:  ldc.i4.1
    IL_0009:  stloc.0
    IL_000a:  br.s       IL_001a

    IL_000c:  ldloc.1
    IL_000d:  ldloc.2
    IL_000e:  ldarg.0
    IL_000f:  callvirt   instance int32 B::f(int32)
    IL_0014:  add
    IL_0015:  stloc.1
    IL_0016:  ldloc.0
    IL_0017:  ldc.i4.1
    IL_0018:  add
    IL_0019:  stloc.0
    IL_001a:  ldloc.0
    IL_001b:  ldc.i4.4
    IL_001c:  ble.s      IL_000c

    IL_001e:  ldloc.1
    IL_001f:  ldloc.0
    IL_0020:  add
    IL_0021:  stloc.3
    IL_0022:  br.s       IL_0024

    IL_0024:  ldloc.3
    IL_0025:  ret
  } // end of method B::F1upBy1le

  .method public hidebysig static int32  F1upBy1lt(int32 amount) 
  {
    // Code size       38 (0x26)
    .maxstack  3
    .locals (int32 V_0,
             int32 V_1,
             class B V_2,
             int32 V_3)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.1
    IL_0002:  newobj     instance void B::.ctor()
    IL_0007:  stloc.2
    IL_0008:  ldc.i4.1
    IL_0009:  stloc.0
    IL_000a:  br.s       IL_001a

    IL_000c:  ldloc.1
    IL_000d:  ldloc.2
    IL_000e:  ldarg.0
    IL_000f:  callvirt   instance int32 B::f(int32)
    IL_0014:  add
    IL_0015:  stloc.1
    IL_0016:  ldloc.0
    IL_0017:  ldc.i4.1
    IL_0018:  add
    IL_0019:  stloc.0
    IL_001a:  ldloc.0
    IL_001b:  ldc.i4.4
    IL_001c:  blt.s      IL_000c

    IL_001e:  ldloc.1
    IL_001f:  ldloc.0
    IL_0020:  add
    IL_0021:  stloc.3
    IL_0022:  br.s       IL_0024

    IL_0024:  ldloc.3
    IL_0025:  ret
  } // end of method B::F1upBy1lt

  .method public hidebysig static int32  F1downBy1gt(int32 amount) 
  {
    // Code size       38 (0x26)
    .maxstack  3
    .locals (int32 V_0,
             int32 V_1,
             class B V_2,
             int32 V_3)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.1
    IL_0002:  newobj     instance void B::.ctor()
    IL_0007:  stloc.2
    IL_0008:  ldc.i4.5
    IL_0009:  stloc.0
    IL_000a:  br.s       IL_001a

    IL_000c:  ldloc.1
    IL_000d:  ldloc.2
    IL_000e:  ldarg.0
    IL_000f:  callvirt   instance int32 B::f(int32)
    IL_0014:  add
    IL_0015:  stloc.1
    IL_0016:  ldloc.0
    IL_0017:  ldc.i4.1
    IL_0018:  sub
    IL_0019:  stloc.0
    IL_001a:  ldloc.0
    IL_001b:  ldc.i4.2
    IL_001c:  bgt.s      IL_000c

    IL_001e:  ldloc.1
    IL_001f:  ldloc.0
    IL_0020:  add
    IL_0021:  stloc.3
    IL_0022:  br.s       IL_0024

    IL_0024:  ldloc.3
    IL_0025:  ret
  } // end of method B::F1downBy1gt

  .method public hidebysig static int32  F1upBy2le(int32 amount) 
  {
    // Code size       38 (0x26)
    .maxstack  3
    .locals (int32 V_0,
             int32 V_1,
             class B V_2,
             int32 V_3)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.1
    IL_0002:  newobj     instance void B::.ctor()
    IL_0007:  stloc.2
    IL_0008:  ldc.i4.1
    IL_0009:  stloc.0
    IL_000a:  br.s       IL_001a

    IL_000c:  ldloc.1
    IL_000d:  ldloc.2
    IL_000e:  ldarg.0
    IL_000f:  callvirt   instance int32 B::f(int32)
    IL_0014:  add
    IL_0015:  stloc.1
    IL_0016:  ldloc.0
    IL_0017:  ldc.i4.2
    IL_0018:  add
    IL_0019:  stloc.0
    IL_001a:  ldloc.0
    IL_001b:  ldc.i4.5
    IL_001c:  ble.s      IL_000c

    IL_001e:  ldloc.1
    IL_001f:  ldloc.0
    IL_0020:  add
    IL_0021:  stloc.3
    IL_0022:  br.s       IL_0024

    IL_0024:  ldloc.3
    IL_0025:  ret
  } // end of method B::F1upBy2le

  .method public hidebysig static int32  F1downBy2ge(int32 amount) 
  {
    // Code size       38 (0x26)
    .maxstack  3
    .locals (int32 V_0,
             int32 V_1,
             class B V_2,
             int32 V_3)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.1
    IL_0002:  newobj     instance void B::.ctor()
    IL_0007:  stloc.2
    IL_0008:  ldc.i4.5
    IL_0009:  stloc.0
    IL_000a:  br.s       IL_001a

    IL_000c:  ldloc.1
    IL_000d:  ldloc.2
    IL_000e:  ldarg.0
    IL_000f:  callvirt   instance int32 B::f(int32)
    IL_0014:  add
    IL_0015:  stloc.1
    IL_0016:  ldloc.0
    IL_0017:  ldc.i4.2
    IL_0018:  sub
    IL_0019:  stloc.0
    IL_001a:  ldloc.0
    IL_001b:  ldc.i4.1
    IL_001c:  bge.s      IL_000c

    IL_001e:  ldloc.1
    IL_001f:  ldloc.0
    IL_0020:  add
    IL_0021:  stloc.3
    IL_0022:  br.s       IL_0024

    IL_0024:  ldloc.3
    IL_0025:  ret
  } // end of method B::F1downBy2ge

  .method public hidebysig static int32  F1upBy2lt(int32 amount) 
  {
    // Code size       38 (0x26)
    .maxstack  3
    .locals (int32 V_0,
             int32 V_1,
             class B V_2,
             int32 V_3)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.1
    IL_0002:  newobj     instance void B::.ctor()
    IL_0007:  stloc.2
    IL_0008:  ldc.i4.1
    IL_0009:  stloc.0
    IL_000a:  br.s       IL_001a

    IL_000c:  ldloc.1
    IL_000d:  ldloc.2
    IL_000e:  ldarg.0
    IL_000f:  callvirt   instance int32 B::f(int32)
    IL_0014:  add
    IL_0015:  stloc.1
    IL_0016:  ldloc.0
    IL_0017:  ldc.i4.2
    IL_0018:  add
    IL_0019:  stloc.0
    IL_001a:  ldloc.0
    IL_001b:  ldc.i4.5
    IL_001c:  blt.s      IL_000c

    IL_001e:  ldloc.1
    IL_001f:  ldloc.0
    IL_0020:  add
    IL_0021:  stloc.3
    IL_0022:  br.s       IL_0024

    IL_0024:  ldloc.3
    IL_0025:  ret
  } // end of method B::F1upBy2lt

  .method public hidebysig static int32  F1downBy2gt(int32 amount) 
  {
    // Code size       39 (0x27)
    .maxstack  3
    .locals (int32 V_0,
             int32 V_1,
             class B V_2,
             int32 V_3)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.1
    IL_0002:  newobj     instance void B::.ctor()
    IL_0007:  stloc.2
    IL_0008:  ldc.i4.s   10
    IL_000a:  stloc.0
    IL_000b:  br.s       IL_001b

    IL_000d:  ldloc.1
    IL_000e:  ldloc.2
    IL_000f:  ldarg.0
    IL_0010:  callvirt   instance int32 B::f(int32)
    IL_0015:  add
    IL_0016:  stloc.1
    IL_0017:  ldloc.0
    IL_0018:  ldc.i4.2
    IL_0019:  sub
    IL_001a:  stloc.0
    IL_001b:  ldloc.0
    IL_001c:  ldc.i4.2
    IL_001d:  bgt.s      IL_000d

    IL_001f:  ldloc.1
    IL_0020:  ldloc.0
    IL_0021:  add
    IL_0022:  stloc.3
    IL_0023:  br.s       IL_0025

    IL_0025:  ldloc.3
    IL_0026:  ret
  } // end of method B::F1downBy2gt

  .method public hidebysig static int32  F1upBy1ne(int32 amount) 
  {
    // Code size       38 (0x26)
    .maxstack  3
    .locals (int32 V_0,
             int32 V_1,
             class B V_2,
             int32 V_3)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.1
    IL_0002:  newobj     instance void B::.ctor()
    IL_0007:  stloc.2
    IL_0008:  ldc.i4.1
    IL_0009:  stloc.0
    IL_000a:  br.s       IL_001a

    IL_000c:  ldloc.1
    IL_000d:  ldloc.2
    IL_000e:  ldarg.0
    IL_000f:  callvirt   instance int32 B::f(int32)
    IL_0014:  add
    IL_0015:  stloc.1
    IL_0016:  ldloc.0
    IL_0017:  ldc.i4.1
    IL_0018:  add
    IL_0019:  stloc.0
    IL_001a:  ldloc.0
    IL_001b:  ldc.i4.4
    IL_001c:  bne.un.s   IL_000c

    IL_001e:  ldloc.1
    IL_001f:  ldloc.0
    IL_0020:  add
    IL_0021:  stloc.3
    IL_0022:  br.s       IL_0024

    IL_0024:  ldloc.3
    IL_0025:  ret
  } // end of method B::F1upBy1ne

  .method public hidebysig static int32  F1downBy1ne(int32 amount) 
  {
    // Code size       38 (0x26)
    .maxstack  3
    .locals (int32 V_0,
             int32 V_1,
             class B V_2,
             int32 V_3)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.1
    IL_0002:  newobj     instance void B::.ctor()
    IL_0007:  stloc.2
    IL_0008:  ldc.i4.5
    IL_0009:  stloc.0
    IL_000a:  br.s       IL_001a

    IL_000c:  ldloc.1
    IL_000d:  ldloc.2
    IL_000e:  ldarg.0
    IL_000f:  callvirt   instance int32 B::f(int32)
    IL_0014:  add
    IL_0015:  stloc.1
    IL_0016:  ldloc.0
    IL_0017:  ldc.i4.1
    IL_0018:  sub
    IL_0019:  stloc.0
    IL_001a:  ldloc.0
    IL_001b:  ldc.i4.2
    IL_001c:  bne.un.s   IL_000c

    IL_001e:  ldloc.1
    IL_001f:  ldloc.0
    IL_0020:  add
    IL_0021:  stloc.3
    IL_0022:  br.s       IL_0024

    IL_0024:  ldloc.3
    IL_0025:  ret
  } // end of method B::F1downBy1ne

  .method public hidebysig static int32  F1upBy2ne(int32 amount) 
  {
    // Code size       38 (0x26)
    .maxstack  3
    .locals (int32 V_0,
             int32 V_1,
             class B V_2,
             int32 V_3)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.1
    IL_0002:  newobj     instance void B::.ctor()
    IL_0007:  stloc.2
    IL_0008:  ldc.i4.1
    IL_0009:  stloc.0
    IL_000a:  br.s       IL_001a

    IL_000c:  ldloc.1
    IL_000d:  ldloc.2
    IL_000e:  ldarg.0
    IL_000f:  callvirt   instance int32 B::f(int32)
    IL_0014:  add
    IL_0015:  stloc.1
    IL_0016:  ldloc.0
    IL_0017:  ldc.i4.2
    IL_0018:  add
    IL_0019:  stloc.0
    IL_001a:  ldloc.0
    IL_001b:  ldc.i4.5
    IL_001c:  bne.un.s   IL_000c

    IL_001e:  ldloc.1
    IL_001f:  ldloc.0
    IL_0020:  add
    IL_0021:  stloc.3
    IL_0022:  br.s       IL_0024

    IL_0024:  ldloc.3
    IL_0025:  ret
  } // end of method B::F1upBy2ne

  .method public hidebysig static int32  F1upBy3neWrap(int32 amount) 
  {
    // Code size       39 (0x27)
    .maxstack  3
    .locals (int32 V_0,
             int32 V_1,
             class B V_2,
             int32 V_3)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.1
    IL_0002:  newobj     instance void B::.ctor()
    IL_0007:  stloc.2
    IL_0008:  ldc.i4.1
    IL_0009:  stloc.0
    IL_000a:  br.s       IL_001a

    IL_000c:  ldloc.1
    IL_000d:  ldloc.2
    IL_000e:  ldarg.0
    IL_000f:  callvirt   instance int32 B::f(int32)
    IL_0014:  add
    IL_0015:  stloc.1
    IL_0016:  ldloc.0
    IL_0017:  ldc.i4.3
    IL_0018:  add
    IL_0019:  stloc.0
    IL_001a:  ldloc.0
    IL_001b:  ldc.i4.s   10
    IL_001d:  bne.un.s   IL_000c

    IL_001f:  ldloc.1
    IL_0020:  ldloc.0
    IL_0021:  add
    IL_0022:  stloc.3
    IL_0023:  br.s       IL_0025

    IL_0025:  ldloc.3
    IL_0026:  ret
  } // end of method B::F1upBy3neWrap

  .method public hidebysig static int32  F1downBy3neWrap(int32 amount) 
  {
    // Code size       39 (0x27)
    .maxstack  3
    .locals (int32 V_0,
             int32 V_1,
             class B V_2,
             int32 V_3)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.1
    IL_0002:  newobj     instance void B::.ctor()
    IL_0007:  stloc.2
    IL_0008:  ldc.i4.s   10
    IL_000a:  stloc.0
    IL_000b:  br.s       IL_001b

    IL_000d:  ldloc.1
    IL_000e:  ldloc.2
    IL_000f:  ldarg.0
    IL_0010:  callvirt   instance int32 B::f(int32)
    IL_0015:  add
    IL_0016:  stloc.1
    IL_0017:  ldloc.0
    IL_0018:  ldc.i4.3
    IL_0019:  sub
    IL_001a:  stloc.0
    IL_001b:  ldloc.0
    IL_001c:  ldc.i4.1
    IL_001d:  bne.un.s   IL_000d

    IL_001f:  ldloc.1
    IL_0020:  ldloc.0
    IL_0021:  add
    IL_0022:  stloc.3
    IL_0023:  br.s       IL_0025

    IL_0025:  ldloc.3
    IL_0026:  ret
  } // end of method B::F1downBy3neWrap

  .method public hidebysig static int32  F2downBy1ge(int32 amount) 
  {
    // Code size       38 (0x26)
    .maxstack 4
    .locals (int32 V_0,
             int32 V_1,
             class B V_2,
             int32 V_3)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.1
    IL_0002:  newobj     instance void B::.ctor()
    IL_0007:  stloc.2
    IL_0008:  ldc.i4.4
    IL_0009:  stloc.0
    IL_000a:  br.s       IL_001a

    IL_000c:  ldloc.1
    IL_000d:  ldloc.2
    IL_000e:  ldarg.0
    IL_000f:  ldftn   instance int32 B::f(int32)
    		calli instance int32 (int32)
    IL_0014:  add
    IL_0015:  stloc.1
    IL_0016:  ldloc.0
    IL_0017:  ldc.i4.1
    IL_0018:  sub
    IL_0019:  stloc.0
    IL_001a:  ldloc.0
    IL_001b:  ldc.i4.1
    IL_001c:  bge.s      IL_000c

    IL_001e:  ldloc.1
    IL_001f:  ldloc.0
    IL_0020:  add
    IL_0021:  stloc.3
    IL_0022:  br.s       IL_0024

    IL_0024:  ldloc.3
    IL_0025:  ret
  } // end of method B::F2downBy1ge

  .method public hidebysig static int32  F2downBy2ne(int32 amount) 
  {
    // Code size       38 (0x26)
    .maxstack 4
    .locals (int32 V_0,
             int32 V_1,
             class B V_2,
             int32 V_3)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.1
    IL_0002:  newobj     instance void B::.ctor()
    IL_0007:  stloc.2
    IL_0008:  ldc.i4.5
    IL_0009:  stloc.0
    IL_000a:  br.s       IL_001a

    IL_000c:  ldloc.1
    IL_000d:  ldloc.2
    IL_000e:  ldarg.0
    IL_000f:  ldftn instance int32 B::f(int32)     calli instance int32 (int32)
    IL_0014:  add
    IL_0015:  stloc.1
    IL_0016:  ldloc.0
    IL_0017:  ldc.i4.2
    IL_0018:  sub
    IL_0019:  stloc.0
    IL_001a:  ldloc.0
    IL_001b:  ldc.i4.1
    IL_001c:  bne.un.s   IL_000c

    IL_001e:  ldloc.1
    IL_001f:  ldloc.0
    IL_0020:  add
    IL_0021:  stloc.3
    IL_0022:  br.s       IL_0024

    IL_0024:  ldloc.3
    IL_0025:  ret
  } // end of method B::F2downBy2ne

  .method public hidebysig static int32  F2upBy1le(int32 amount) 
  {
    // Code size       38 (0x26)
    .maxstack 4
    .locals (int32 V_0,
             int32 V_1,
             class B V_2,
             int32 V_3)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.1
    IL_0002:  newobj     instance void B::.ctor()
    IL_0007:  stloc.2
    IL_0008:  ldc.i4.1
    IL_0009:  stloc.0
    IL_000a:  br.s       IL_001a

    IL_000c:  ldloc.1
    IL_000d:  ldloc.2
    IL_000e:  ldarg.0
    IL_000f:  ldftn instance int32 B::f(int32)     calli instance int32 (int32)
    IL_0014:  add
    IL_0015:  stloc.1
    IL_0016:  ldloc.0
    IL_0017:  ldc.i4.1
    IL_0018:  add
    IL_0019:  stloc.0
    IL_001a:  ldloc.0
    IL_001b:  ldc.i4.4
    IL_001c:  ble.s      IL_000c

    IL_001e:  ldloc.1
    IL_001f:  ldloc.0
    IL_0020:  add
    IL_0021:  stloc.3
    IL_0022:  br.s       IL_0024

    IL_0024:  ldloc.3
    IL_0025:  ret
  } // end of method B::F2upBy1le

  .method public hidebysig static int32  F2upBy1lt(int32 amount) 
  {
    // Code size       38 (0x26)
    .maxstack 4
    .locals (int32 V_0,
             int32 V_1,
             class B V_2,
             int32 V_3)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.1
    IL_0002:  newobj     instance void B::.ctor()
    IL_0007:  stloc.2
    IL_0008:  ldc.i4.1
    IL_0009:  stloc.0
    IL_000a:  br.s       IL_001a

    IL_000c:  ldloc.1
    IL_000d:  ldloc.2
    IL_000e:  ldarg.0
    IL_000f:  ldftn instance int32 B::f(int32)     calli instance int32 (int32)
    IL_0014:  add
    IL_0015:  stloc.1
    IL_0016:  ldloc.0
    IL_0017:  ldc.i4.1
    IL_0018:  add
    IL_0019:  stloc.0
    IL_001a:  ldloc.0
    IL_001b:  ldc.i4.4
    IL_001c:  blt.s      IL_000c

    IL_001e:  ldloc.1
    IL_001f:  ldloc.0
    IL_0020:  add
    IL_0021:  stloc.3
    IL_0022:  br.s       IL_0024

    IL_0024:  ldloc.3
    IL_0025:  ret
  } // end of method B::F2upBy1lt

  .method public hidebysig static int32  F2downBy1gt(int32 amount) 
  {
    // Code size       38 (0x26)
    .maxstack 4
    .locals (int32 V_0,
             int32 V_1,
             class B V_2,
             int32 V_3)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.1
    IL_0002:  newobj     instance void B::.ctor()
    IL_0007:  stloc.2
    IL_0008:  ldc.i4.5
    IL_0009:  stloc.0
    IL_000a:  br.s       IL_001a

    IL_000c:  ldloc.1
    IL_000d:  ldloc.2
    IL_000e:  ldarg.0
    IL_000f:  ldftn instance int32 B::f(int32)     calli instance int32 (int32)
    IL_0014:  add
    IL_0015:  stloc.1
    IL_0016:  ldloc.0
    IL_0017:  ldc.i4.1
    IL_0018:  sub
    IL_0019:  stloc.0
    IL_001a:  ldloc.0
    IL_001b:  ldc.i4.2
    IL_001c:  bgt.s      IL_000c

    IL_001e:  ldloc.1
    IL_001f:  ldloc.0
    IL_0020:  add
    IL_0021:  stloc.3
    IL_0022:  br.s       IL_0024

    IL_0024:  ldloc.3
    IL_0025:  ret
  } // end of method B::F2downBy1gt

  .method public hidebysig static int32  F2upBy2le(int32 amount) 
  {
    // Code size       38 (0x26)
    .maxstack 4
    .locals (int32 V_0,
             int32 V_1,
             class B V_2,
             int32 V_3)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.1
    IL_0002:  newobj     instance void B::.ctor()
    IL_0007:  stloc.2
    IL_0008:  ldc.i4.1
    IL_0009:  stloc.0
    IL_000a:  br.s       IL_001a

    IL_000c:  ldloc.1
    IL_000d:  ldloc.2
    IL_000e:  ldarg.0
    IL_000f:  ldftn instance int32 B::f(int32)     calli instance int32 (int32)
    IL_0014:  add
    IL_0015:  stloc.1
    IL_0016:  ldloc.0
    IL_0017:  ldc.i4.2
    IL_0018:  add
    IL_0019:  stloc.0
    IL_001a:  ldloc.0
    IL_001b:  ldc.i4.5
    IL_001c:  ble.s      IL_000c

    IL_001e:  ldloc.1
    IL_001f:  ldloc.0
    IL_0020:  add
    IL_0021:  stloc.3
    IL_0022:  br.s       IL_0024

    IL_0024:  ldloc.3
    IL_0025:  ret
  } // end of method B::F2upBy2le

  .method public hidebysig static int32  F2downBy2ge(int32 amount) 
  {
    // Code size       38 (0x26)
    .maxstack 4
    .locals (int32 V_0,
             int32 V_1,
             class B V_2,
             int32 V_3)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.1
    IL_0002:  newobj     instance void B::.ctor()
    IL_0007:  stloc.2
    IL_0008:  ldc.i4.5
    IL_0009:  stloc.0
    IL_000a:  br.s       IL_001a

    IL_000c:  ldloc.1
    IL_000d:  ldloc.2
    IL_000e:  ldarg.0
    IL_000f:  ldftn instance int32 B::f(int32)     calli instance int32 (int32)
    IL_0014:  add
    IL_0015:  stloc.1
    IL_0016:  ldloc.0
    IL_0017:  ldc.i4.2
    IL_0018:  sub
    IL_0019:  stloc.0
    IL_001a:  ldloc.0
    IL_001b:  ldc.i4.1
    IL_001c:  bge.s      IL_000c

    IL_001e:  ldloc.1
    IL_001f:  ldloc.0
    IL_0020:  add
    IL_0021:  stloc.3
    IL_0022:  br.s       IL_0024

    IL_0024:  ldloc.3
    IL_0025:  ret
  } // end of method B::F2downBy2ge

  .method public hidebysig static int32  F2upBy2lt(int32 amount) 
  {
    // Code size       38 (0x26)
    .maxstack 4
    .locals (int32 V_0,
             int32 V_1,
             class B V_2,
             int32 V_3)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.1
    IL_0002:  newobj     instance void B::.ctor()
    IL_0007:  stloc.2
    IL_0008:  ldc.i4.1
    IL_0009:  stloc.0
    IL_000a:  br.s       IL_001a

    IL_000c:  ldloc.1
    IL_000d:  ldloc.2
    IL_000e:  ldarg.0
    IL_000f:  ldftn instance int32 B::f(int32)     calli instance int32 (int32)
    IL_0014:  add
    IL_0015:  stloc.1
    IL_0016:  ldloc.0
    IL_0017:  ldc.i4.2
    IL_0018:  add
    IL_0019:  stloc.0
    IL_001a:  ldloc.0
    IL_001b:  ldc.i4.5
    IL_001c:  blt.s      IL_000c

    IL_001e:  ldloc.1
    IL_001f:  ldloc.0
    IL_0020:  add
    IL_0021:  stloc.3
    IL_0022:  br.s       IL_0024

    IL_0024:  ldloc.3
    IL_0025:  ret
  } // end of method B::F2upBy2lt

  .method public hidebysig static int32  F2downBy2gt(int32 amount) 
  {
    // Code size       39 (0x27)
    .maxstack 4
    .locals (int32 V_0,
             int32 V_1,
             class B V_2,
             int32 V_3)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.1
    IL_0002:  newobj     instance void B::.ctor()
    IL_0007:  stloc.2
    IL_0008:  ldc.i4.s   10
    IL_000a:  stloc.0
    IL_000b:  br.s       IL_001b

    IL_000d:  ldloc.1
    IL_000e:  ldloc.2
    IL_000f:  ldarg.0
    IL_0010:  ldftn instance int32 B::f(int32)     calli instance int32 (int32)
    IL_0015:  add
    IL_0016:  stloc.1
    IL_0017:  ldloc.0
    IL_0018:  ldc.i4.2
    IL_0019:  sub
    IL_001a:  stloc.0
    IL_001b:  ldloc.0
    IL_001c:  ldc.i4.2
    IL_001d:  bgt.s      IL_000d

    IL_001f:  ldloc.1
    IL_0020:  ldloc.0
    IL_0021:  add
    IL_0022:  stloc.3
    IL_0023:  br.s       IL_0025

    IL_0025:  ldloc.3
    IL_0026:  ret
  } // end of method B::F2downBy2gt

  .method public hidebysig static int32  F2upBy1ne(int32 amount) 
  {
    // Code size       38 (0x26)
    .maxstack 4
    .locals (int32 V_0,
             int32 V_1,
             class B V_2,
             int32 V_3)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.1
    IL_0002:  newobj     instance void B::.ctor()
    IL_0007:  stloc.2
    IL_0008:  ldc.i4.1
    IL_0009:  stloc.0
    IL_000a:  br.s       IL_001a

    IL_000c:  ldloc.1
    IL_000d:  ldloc.2
    IL_000e:  ldarg.0
    IL_000f:  ldftn instance int32 B::f(int32)     calli instance int32 (int32)
    IL_0014:  add
    IL_0015:  stloc.1
    IL_0016:  ldloc.0
    IL_0017:  ldc.i4.1
    IL_0018:  add
    IL_0019:  stloc.0
    IL_001a:  ldloc.0
    IL_001b:  ldc.i4.4
    IL_001c:  bne.un.s   IL_000c

    IL_001e:  ldloc.1
    IL_001f:  ldloc.0
    IL_0020:  add
    IL_0021:  stloc.3
    IL_0022:  br.s       IL_0024

    IL_0024:  ldloc.3
    IL_0025:  ret
  } // end of method B::F2upBy1ne

  .method public hidebysig static int32  F2downBy1ne(int32 amount) 
  {
    // Code size       38 (0x26)
    .maxstack 4
    .locals (int32 V_0,
             int32 V_1,
             class B V_2,
             int32 V_3)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.1
    IL_0002:  newobj     instance void B::.ctor()
    IL_0007:  stloc.2
    IL_0008:  ldc.i4.5
    IL_0009:  stloc.0
    IL_000a:  br.s       IL_001a

    IL_000c:  ldloc.1
    IL_000d:  ldloc.2
    IL_000e:  ldarg.0
    IL_000f:  ldftn instance int32 B::f(int32)     calli instance int32 (int32)
    IL_0014:  add
    IL_0015:  stloc.1
    IL_0016:  ldloc.0
    IL_0017:  ldc.i4.1
    IL_0018:  sub
    IL_0019:  stloc.0
    IL_001a:  ldloc.0
    IL_001b:  ldc.i4.2
    IL_001c:  bne.un.s   IL_000c

    IL_001e:  ldloc.1
    IL_001f:  ldloc.0
    IL_0020:  add
    IL_0021:  stloc.3
    IL_0022:  br.s       IL_0024

    IL_0024:  ldloc.3
    IL_0025:  ret
  } // end of method B::F2downBy1ne

  .method public hidebysig static int32  F2upBy2ne(int32 amount) 
  {
    // Code size       38 (0x26)
    .maxstack 4
    .locals (int32 V_0,
             int32 V_1,
             class B V_2,
             int32 V_3)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.1
    IL_0002:  newobj     instance void B::.ctor()
    IL_0007:  stloc.2
    IL_0008:  ldc.i4.1
    IL_0009:  stloc.0
    IL_000a:  br.s       IL_001a

    IL_000c:  ldloc.1
    IL_000d:  ldloc.2
    IL_000e:  ldarg.0
    IL_000f:  ldftn instance int32 B::f(int32)     calli instance int32 (int32)
    IL_0014:  add
    IL_0015:  stloc.1
    IL_0016:  ldloc.0
    IL_0017:  ldc.i4.2
    IL_0018:  add
    IL_0019:  stloc.0
    IL_001a:  ldloc.0
    IL_001b:  ldc.i4.5
    IL_001c:  bne.un.s   IL_000c

    IL_001e:  ldloc.1
    IL_001f:  ldloc.0
    IL_0020:  add
    IL_0021:  stloc.3
    IL_0022:  br.s       IL_0024

    IL_0024:  ldloc.3
    IL_0025:  ret
  } // end of method B::F2upBy2ne

  .method public hidebysig static int32  F2upBy3neWrap(int32 amount) 
  {
    // Code size       39 (0x27)
    .maxstack 4
    .locals (int32 V_0,
             int32 V_1,
             class B V_2,
             int32 V_3)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.1
    IL_0002:  newobj     instance void B::.ctor()
    IL_0007:  stloc.2
    IL_0008:  ldc.i4.1
    IL_0009:  stloc.0
    IL_000a:  br.s       IL_001a

    IL_000c:  ldloc.1
    IL_000d:  ldloc.2
    IL_000e:  ldarg.0
    IL_000f:  ldftn instance int32 B::f(int32)     calli instance int32 (int32)
    IL_0014:  add
    IL_0015:  stloc.1
    IL_0016:  ldloc.0
    IL_0017:  ldc.i4.3
    IL_0018:  add
    IL_0019:  stloc.0
    IL_001a:  ldloc.0
    IL_001b:  ldc.i4.s   10
    IL_001d:  bne.un.s   IL_000c

    IL_001f:  ldloc.1
    IL_0020:  ldloc.0
    IL_0021:  add
    IL_0022:  stloc.3
    IL_0023:  br.s       IL_0025

    IL_0025:  ldloc.3
    IL_0026:  ret
  } // end of method B::F2upBy3neWrap

  .method public hidebysig static int32  F2downBy3neWrap(int32 amount) 
  {
    // Code size       39 (0x27)
    .maxstack 4
    .locals (int32 V_0,
             int32 V_1,
             class B V_2,
             int32 V_3)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.1
    IL_0002:  newobj     instance void B::.ctor()
    IL_0007:  stloc.2
    IL_0008:  ldc.i4.s   10
    IL_000a:  stloc.0
    IL_000b:  br.s       IL_001b

    IL_000d:  ldloc.1
    IL_000e:  ldloc.2
    IL_000f:  ldarg.0
    IL_0010:  ldftn instance int32 B::f(int32)     calli instance int32 (int32)
    IL_0015:  add
    IL_0016:  stloc.1
    IL_0017:  ldloc.0
    IL_0018:  ldc.i4.3
    IL_0019:  sub
    IL_001a:  stloc.0
    IL_001b:  ldloc.0
    IL_001c:  ldc.i4.1
    IL_001d:  bne.un.s   IL_000d

    IL_001f:  ldloc.1
    IL_0020:  ldloc.0
    IL_0021:  add
    IL_0022:  stloc.3
    IL_0023:  br.s       IL_0025

    IL_0025:  ldloc.3
    IL_0026:  ret
  } // end of method B::F2downBy3neWrap

  .method public hidebysig static int32  F3downBy1ge(int32 amount) 
  {
    // Code size       38 (0x26)
    .maxstack  4
    .locals (int32 V_0,
             int32 V_1,
             class B V_2,
             int32 V_3)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.1
    IL_0002:  newobj     instance void B::.ctor()
    IL_0007:  stloc.2
    IL_0008:  ldc.i4.4
    IL_0009:  stloc.0
    IL_000a:  br.s       IL_001a

    IL_000c:  ldloc.1
    IL_000d:  ldloc.2
    IL_000e:  ldarg.0
    		 ldloc.2
    IL_000f:  ldvirtftn   instance int32 A::f0(int32)
    		 calli instance int32 (int32)
    IL_0014:  add
    IL_0015:  stloc.1
    IL_0016:  ldloc.0
    IL_0017:  ldc.i4.1
    IL_0018:  sub
    IL_0019:  stloc.0
    IL_001a:  ldloc.0
    IL_001b:  ldc.i4.1
    IL_001c:  bge.s      IL_000c

    IL_001e:  ldloc.1
    IL_001f:  ldloc.0
    IL_0020:  add
    IL_0021:  stloc.3
    IL_0022:  br.s       IL_0024

    IL_0024:  ldloc.3
    IL_0025:  ret
  } // end of method B::F3downBy1ge

  .method public hidebysig static int32  F3downBy2ne(int32 amount) 
  {
    // Code size       38 (0x26)
    .maxstack 4
    .locals (int32 V_0,
             int32 V_1,
             class B V_2,
             int32 V_3)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.1
    IL_0002:  newobj     instance void B::.ctor()
    IL_0007:  stloc.2
    IL_0008:  ldc.i4.5
    IL_0009:  stloc.0
    IL_000a:  br.s       IL_001a

    IL_000c:  ldloc.1
    IL_000d:  ldloc.2
    IL_000e:  ldarg.0
    IL_000f:  ldloc.2      ldvirtftn  instance int32 A::f0(int32)      calli instance int32 (int32)
    IL_0014:  add
    IL_0015:  stloc.1
    IL_0016:  ldloc.0
    IL_0017:  ldc.i4.2
    IL_0018:  sub
    IL_0019:  stloc.0
    IL_001a:  ldloc.0
    IL_001b:  ldc.i4.1
    IL_001c:  bne.un.s   IL_000c

    IL_001e:  ldloc.1
    IL_001f:  ldloc.0
    IL_0020:  add
    IL_0021:  stloc.3
    IL_0022:  br.s       IL_0024

    IL_0024:  ldloc.3
    IL_0025:  ret
  } // end of method B::F3downBy2ne

  .method public hidebysig static int32  F3upBy1le(int32 amount) 
  {
    // Code size       38 (0x26)
    .maxstack 4
    .locals (int32 V_0,
             int32 V_1,
             class B V_2,
             int32 V_3)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.1
    IL_0002:  newobj     instance void B::.ctor()
    IL_0007:  stloc.2
    IL_0008:  ldc.i4.1
    IL_0009:  stloc.0
    IL_000a:  br.s       IL_001a

    IL_000c:  ldloc.1
    IL_000d:  ldloc.2
    IL_000e:  ldarg.0
    IL_000f:  ldloc.2      ldvirtftn  instance int32 A::f0(int32)      calli instance int32 (int32)
    IL_0014:  add
    IL_0015:  stloc.1
    IL_0016:  ldloc.0
    IL_0017:  ldc.i4.1
    IL_0018:  add
    IL_0019:  stloc.0
    IL_001a:  ldloc.0
    IL_001b:  ldc.i4.4
    IL_001c:  ble.s      IL_000c

    IL_001e:  ldloc.1
    IL_001f:  ldloc.0
    IL_0020:  add
    IL_0021:  stloc.3
    IL_0022:  br.s       IL_0024

    IL_0024:  ldloc.3
    IL_0025:  ret
  } // end of method B::F3upBy1le

  .method public hidebysig static int32  F3upBy1lt(int32 amount) 
  {
    // Code size       38 (0x26)
    .maxstack 4
    .locals (int32 V_0,
             int32 V_1,
             class B V_2,
             int32 V_3)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.1
    IL_0002:  newobj     instance void B::.ctor()
    IL_0007:  stloc.2
    IL_0008:  ldc.i4.1
    IL_0009:  stloc.0
    IL_000a:  br.s       IL_001a

    IL_000c:  ldloc.1
    IL_000d:  ldloc.2
    IL_000e:  ldarg.0
    IL_000f:  ldloc.2      ldvirtftn  instance int32 A::f0(int32)      calli instance int32 (int32)
    IL_0014:  add
    IL_0015:  stloc.1
    IL_0016:  ldloc.0
    IL_0017:  ldc.i4.1
    IL_0018:  add
    IL_0019:  stloc.0
    IL_001a:  ldloc.0
    IL_001b:  ldc.i4.4
    IL_001c:  blt.s      IL_000c

    IL_001e:  ldloc.1
    IL_001f:  ldloc.0
    IL_0020:  add
    IL_0021:  stloc.3
    IL_0022:  br.s       IL_0024

    IL_0024:  ldloc.3
    IL_0025:  ret
  } // end of method B::F3upBy1lt

  .method public hidebysig static int32  F3downBy1gt(int32 amount) 
  {
    // Code size       38 (0x26)
    .maxstack 4
    .locals (int32 V_0,
             int32 V_1,
             class B V_2,
             int32 V_3)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.1
    IL_0002:  newobj     instance void B::.ctor()
    IL_0007:  stloc.2
    IL_0008:  ldc.i4.5
    IL_0009:  stloc.0
    IL_000a:  br.s       IL_001a

    IL_000c:  ldloc.1
    IL_000d:  ldloc.2
    IL_000e:  ldarg.0
    IL_000f:  ldloc.2      ldvirtftn  instance int32 A::f0(int32)      calli instance int32 (int32)
    IL_0014:  add
    IL_0015:  stloc.1
    IL_0016:  ldloc.0
    IL_0017:  ldc.i4.1
    IL_0018:  sub
    IL_0019:  stloc.0
    IL_001a:  ldloc.0
    IL_001b:  ldc.i4.2
    IL_001c:  bgt.s      IL_000c

    IL_001e:  ldloc.1
    IL_001f:  ldloc.0
    IL_0020:  add
    IL_0021:  stloc.3
    IL_0022:  br.s       IL_0024

    IL_0024:  ldloc.3
    IL_0025:  ret
  } // end of method B::F3downBy1gt

  .method public hidebysig static int32  F3upBy2le(int32 amount) 
  {
    // Code size       38 (0x26)
    .maxstack 4
    .locals (int32 V_0,
             int32 V_1,
             class B V_2,
             int32 V_3)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.1
    IL_0002:  newobj     instance void B::.ctor()
    IL_0007:  stloc.2
    IL_0008:  ldc.i4.1
    IL_0009:  stloc.0
    IL_000a:  br.s       IL_001a

    IL_000c:  ldloc.1
    IL_000d:  ldloc.2
    IL_000e:  ldarg.0
    IL_000f:  ldloc.2      ldvirtftn  instance int32 A::f0(int32)      calli instance int32 (int32)
    IL_0014:  add
    IL_0015:  stloc.1
    IL_0016:  ldloc.0
    IL_0017:  ldc.i4.2
    IL_0018:  add
    IL_0019:  stloc.0
    IL_001a:  ldloc.0
    IL_001b:  ldc.i4.5
    IL_001c:  ble.s      IL_000c

    IL_001e:  ldloc.1
    IL_001f:  ldloc.0
    IL_0020:  add
    IL_0021:  stloc.3
    IL_0022:  br.s       IL_0024

    IL_0024:  ldloc.3
    IL_0025:  ret
  } // end of method B::F3upBy2le

  .method public hidebysig static int32  F3downBy2ge(int32 amount) 
  {
    // Code size       38 (0x26)
    .maxstack 4
    .locals (int32 V_0,
             int32 V_1,
             class B V_2,
             int32 V_3)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.1
    IL_0002:  newobj     instance void B::.ctor()
    IL_0007:  stloc.2
    IL_0008:  ldc.i4.5
    IL_0009:  stloc.0
    IL_000a:  br.s       IL_001a

    IL_000c:  ldloc.1
    IL_000d:  ldloc.2
    IL_000e:  ldarg.0
    IL_000f:  ldloc.2      ldvirtftn  instance int32 A::f0(int32)      calli instance int32 (int32)
    IL_0014:  add
    IL_0015:  stloc.1
    IL_0016:  ldloc.0
    IL_0017:  ldc.i4.2
    IL_0018:  sub
    IL_0019:  stloc.0
    IL_001a:  ldloc.0
    IL_001b:  ldc.i4.1
    IL_001c:  bge.s      IL_000c

    IL_001e:  ldloc.1
    IL_001f:  ldloc.0
    IL_0020:  add
    IL_0021:  stloc.3
    IL_0022:  br.s       IL_0024

    IL_0024:  ldloc.3
    IL_0025:  ret
  } // end of method B::F3downBy2ge

  .method public hidebysig static int32  F3upBy2lt(int32 amount) 
  {
    // Code size       38 (0x26)
    .maxstack 4
    .locals (int32 V_0,
             int32 V_1,
             class B V_2,
             int32 V_3)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.1
    IL_0002:  newobj     instance void B::.ctor()
    IL_0007:  stloc.2
    IL_0008:  ldc.i4.1
    IL_0009:  stloc.0
    IL_000a:  br.s       IL_001a

    IL_000c:  ldloc.1
    IL_000d:  ldloc.2
    IL_000e:  ldarg.0
    IL_000f:  ldloc.2      ldvirtftn  instance int32 A::f0(int32)      calli instance int32 (int32)
    IL_0014:  add
    IL_0015:  stloc.1
    IL_0016:  ldloc.0
    IL_0017:  ldc.i4.2
    IL_0018:  add
    IL_0019:  stloc.0
    IL_001a:  ldloc.0
    IL_001b:  ldc.i4.5
    IL_001c:  blt.s      IL_000c

    IL_001e:  ldloc.1
    IL_001f:  ldloc.0
    IL_0020:  add
    IL_0021:  stloc.3
    IL_0022:  br.s       IL_0024

    IL_0024:  ldloc.3
    IL_0025:  ret
  } // end of method B::F3upBy2lt

  .method public hidebysig static int32  F3downBy2gt(int32 amount) 
  {
    // Code size       39 (0x27)
    .maxstack 4
    .locals (int32 V_0,
             int32 V_1,
             class B V_2,
             int32 V_3)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.1
    IL_0002:  newobj     instance void B::.ctor()
    IL_0007:  stloc.2
    IL_0008:  ldc.i4.s   10
    IL_000a:  stloc.0
    IL_000b:  br.s       IL_001b

    IL_000d:  ldloc.1
    IL_000e:  ldloc.2
    IL_000f:  ldarg.0
    IL_0010:  ldloc.2      ldvirtftn  instance int32 A::f0(int32)      calli instance int32 (int32)
    IL_0015:  add
    IL_0016:  stloc.1
    IL_0017:  ldloc.0
    IL_0018:  ldc.i4.2
    IL_0019:  sub
    IL_001a:  stloc.0
    IL_001b:  ldloc.0
    IL_001c:  ldc.i4.2
    IL_001d:  bgt.s      IL_000d

    IL_001f:  ldloc.1
    IL_0020:  ldloc.0
    IL_0021:  add
    IL_0022:  stloc.3
    IL_0023:  br.s       IL_0025

    IL_0025:  ldloc.3
    IL_0026:  ret
  } // end of method B::F3downBy2gt

  .method public hidebysig static int32  F3upBy1ne(int32 amount) 
  {
    // Code size       38 (0x26)
    .maxstack 4
    .locals (int32 V_0,
             int32 V_1,
             class B V_2,
             int32 V_3)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.1
    IL_0002:  newobj     instance void B::.ctor()
    IL_0007:  stloc.2
    IL_0008:  ldc.i4.1
    IL_0009:  stloc.0
    IL_000a:  br.s       IL_001a

    IL_000c:  ldloc.1
    IL_000d:  ldloc.2
    IL_000e:  ldarg.0
    IL_000f:  ldloc.2      ldvirtftn  instance int32 A::f0(int32)      calli instance int32 (int32)
    IL_0014:  add
    IL_0015:  stloc.1
    IL_0016:  ldloc.0
    IL_0017:  ldc.i4.1
    IL_0018:  add
    IL_0019:  stloc.0
    IL_001a:  ldloc.0
    IL_001b:  ldc.i4.4
    IL_001c:  bne.un.s   IL_000c

    IL_001e:  ldloc.1
    IL_001f:  ldloc.0
    IL_0020:  add
    IL_0021:  stloc.3
    IL_0022:  br.s       IL_0024

    IL_0024:  ldloc.3
    IL_0025:  ret
  } // end of method B::F3upBy1ne

  .method public hidebysig static int32  F3downBy1ne(int32 amount) 
  {
    // Code size       38 (0x26)
    .maxstack 4
    .locals (int32 V_0,
             int32 V_1,
             class B V_2,
             int32 V_3)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.1
    IL_0002:  newobj     instance void B::.ctor()
    IL_0007:  stloc.2
    IL_0008:  ldc.i4.5
    IL_0009:  stloc.0
    IL_000a:  br.s       IL_001a

    IL_000c:  ldloc.1
    IL_000d:  ldloc.2
    IL_000e:  ldarg.0
    IL_000f:  ldloc.2      ldvirtftn  instance int32 A::f0(int32)      calli instance int32 (int32)
    IL_0014:  add
    IL_0015:  stloc.1
    IL_0016:  ldloc.0
    IL_0017:  ldc.i4.1
    IL_0018:  sub
    IL_0019:  stloc.0
    IL_001a:  ldloc.0
    IL_001b:  ldc.i4.2
    IL_001c:  bne.un.s   IL_000c

    IL_001e:  ldloc.1
    IL_001f:  ldloc.0
    IL_0020:  add
    IL_0021:  stloc.3
    IL_0022:  br.s       IL_0024

    IL_0024:  ldloc.3
    IL_0025:  ret
  } // end of method B::F3downBy1ne

  .method public hidebysig static int32  F3upBy2ne(int32 amount) 
  {
    // Code size       38 (0x26)
    .maxstack 4
    .locals (int32 V_0,
             int32 V_1,
             class B V_2,
             int32 V_3)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.1
    IL_0002:  newobj     instance void B::.ctor()
    IL_0007:  stloc.2
    IL_0008:  ldc.i4.1
    IL_0009:  stloc.0
    IL_000a:  br.s       IL_001a

    IL_000c:  ldloc.1
    IL_000d:  ldloc.2
    IL_000e:  ldarg.0
    IL_000f:  ldloc.2      ldvirtftn  instance int32 A::f0(int32)      calli instance int32 (int32)
    IL_0014:  add
    IL_0015:  stloc.1
    IL_0016:  ldloc.0
    IL_0017:  ldc.i4.2
    IL_0018:  add
    IL_0019:  stloc.0
    IL_001a:  ldloc.0
    IL_001b:  ldc.i4.5
    IL_001c:  bne.un.s   IL_000c

    IL_001e:  ldloc.1
    IL_001f:  ldloc.0
    IL_0020:  add
    IL_0021:  stloc.3
    IL_0022:  br.s       IL_0024

    IL_0024:  ldloc.3
    IL_0025:  ret
  } // end of method B::F3upBy2ne

  .method public hidebysig static int32  F3upBy3neWrap(int32 amount) 
  {
    // Code size       39 (0x27)
    .maxstack 4
    .locals (int32 V_0,
             int32 V_1,
             class B V_2,
             int32 V_3)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.1
    IL_0002:  newobj     instance void B::.ctor()
    IL_0007:  stloc.2
    IL_0008:  ldc.i4.1
    IL_0009:  stloc.0
    IL_000a:  br.s       IL_001a

    IL_000c:  ldloc.1
    IL_000d:  ldloc.2
    IL_000e:  ldarg.0
    IL_000f:  ldloc.2      ldvirtftn  instance int32 A::f0(int32)      calli instance int32 (int32)
    IL_0014:  add
    IL_0015:  stloc.1
    IL_0016:  ldloc.0
    IL_0017:  ldc.i4.3
    IL_0018:  add
    IL_0019:  stloc.0
    IL_001a:  ldloc.0
    IL_001b:  ldc.i4.s   10
    IL_001d:  bne.un.s   IL_000c

    IL_001f:  ldloc.1
    IL_0020:  ldloc.0
    IL_0021:  add
    IL_0022:  stloc.3
    IL_0023:  br.s       IL_0025

    IL_0025:  ldloc.3
    IL_0026:  ret
  } // end of method B::F3upBy3neWrap

  .method public hidebysig static int32  F3downBy3neWrap(int32 amount) 
  {
    // Code size       39 (0x27)
    .maxstack 4
    .locals (int32 V_0,
             int32 V_1,
             class B V_2,
             int32 V_3)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.1
    IL_0002:  newobj     instance void B::.ctor()
    IL_0007:  stloc.2
    IL_0008:  ldc.i4.s   10
    IL_000a:  stloc.0
    IL_000b:  br.s       IL_001b

    IL_000d:  ldloc.1
    IL_000e:  ldloc.2
    IL_000f:  ldarg.0
    IL_0010:  ldloc.2      ldvirtftn  instance int32 A::f0(int32)      calli instance int32 (int32)
    IL_0015:  add
    IL_0016:  stloc.1
    IL_0017:  ldloc.0
    IL_0018:  ldc.i4.3
    IL_0019:  sub
    IL_001a:  stloc.0
    IL_001b:  ldloc.0
    IL_001c:  ldc.i4.1
    IL_001d:  bne.un.s   IL_000d

    IL_001f:  ldloc.1
    IL_0020:  ldloc.0
    IL_0021:  add
    IL_0022:  stloc.3
    IL_0023:  br.s       IL_0025

    IL_0025:  ldloc.3
    IL_0026:  ret
  } // end of method B::F3downBy3neWrap

  .method public hidebysig static int32  Main()
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       1003 (0x3eb)
    .maxstack  2
    .locals (bool V_0,
             int32 V_1)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.0
    IL_0002:  ldc.i4.s   10
    IL_0004:  call       int32 B::F1upBy1le(int32)
    IL_0009:  ldc.i4.s   45
    IL_000b:  beq.s      IL_0019

    IL_000d:  ldstr      "F1upBy1le failed"
    IL_0012:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0017:  ldc.i4.1
    IL_0018:  stloc.0
    IL_0019:  ldc.i4.s   10
    IL_001b:  call       int32 B::F1downBy1ge(int32)
    IL_0020:  ldc.i4.s   40
    IL_0022:  beq.s      IL_0030

    IL_0024:  ldstr      "F1downBy1ge failed"
    IL_0029:  call       void [System.Console]System.Console::WriteLine(string)
    IL_002e:  ldc.i4.1
    IL_002f:  stloc.0
    IL_0030:  ldc.i4.s   10
    IL_0032:  call       int32 B::F1upBy1lt(int32)
    IL_0037:  ldc.i4.s   34
    IL_0039:  beq.s      IL_0047

    IL_003b:  ldstr      "F1upBy1lt failed"
    IL_0040:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0045:  ldc.i4.1
    IL_0046:  stloc.0
    IL_0047:  ldc.i4.s   10
    IL_0049:  call       int32 B::F1downBy1gt(int32)
    IL_004e:  ldc.i4.s   32
    IL_0050:  beq.s      IL_005e

    IL_0052:  ldstr      "F1downBy1gt failed"
    IL_0057:  call       void [System.Console]System.Console::WriteLine(string)
    IL_005c:  ldc.i4.1
    IL_005d:  stloc.0
    IL_005e:  ldc.i4.s   10
    IL_0060:  call       int32 B::F1upBy2le(int32)
    IL_0065:  ldc.i4.s   37
    IL_0067:  beq.s      IL_0075

    IL_0069:  ldstr      "F1upBy2le failed"
    IL_006e:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0073:  ldc.i4.1
    IL_0074:  stloc.0
    IL_0075:  ldc.i4.s   10
    IL_0077:  call       int32 B::F1downBy2ge(int32)
    IL_007c:  ldc.i4.s   29
    IL_007e:  beq.s      IL_008c

    IL_0080:  ldstr      "F1downBy2ge failed"
    IL_0085:  call       void [System.Console]System.Console::WriteLine(string)
    IL_008a:  ldc.i4.1
    IL_008b:  stloc.0
    IL_008c:  ldc.i4.s   10
    IL_008e:  call       int32 B::F1upBy2lt(int32)
    IL_0093:  ldc.i4.s   25
    IL_0095:  beq.s      IL_00a3

    IL_0097:  ldstr      "F1upBy2lt failed"
    IL_009c:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00a1:  ldc.i4.1
    IL_00a2:  stloc.0
    IL_00a3:  ldc.i4.s   10
    IL_00a5:  call       int32 B::F1downBy2gt(int32)
    IL_00aa:  ldc.i4.s   42
    IL_00ac:  beq.s      IL_00ba

    IL_00ae:  ldstr      "F1downBy2gt failed"
    IL_00b3:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00b8:  ldc.i4.1
    IL_00b9:  stloc.0
    IL_00ba:  ldc.i4.s   10
    IL_00bc:  call       int32 B::F1upBy1ne(int32)
    IL_00c1:  ldc.i4.s   34
    IL_00c3:  beq.s      IL_00d1

    IL_00c5:  ldstr      "F1upBy1ne failed"
    IL_00ca:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00cf:  ldc.i4.1
    IL_00d0:  stloc.0
    IL_00d1:  ldc.i4.s   10
    IL_00d3:  call       int32 B::F1downBy1ne(int32)
    IL_00d8:  ldc.i4.s   32
    IL_00da:  beq.s      IL_00e8

    IL_00dc:  ldstr      "F1downBy1ne failed"
    IL_00e1:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00e6:  ldc.i4.1
    IL_00e7:  stloc.0
    IL_00e8:  ldc.i4.s   10
    IL_00ea:  call       int32 B::F1upBy2ne(int32)
    IL_00ef:  ldc.i4.s   25
    IL_00f1:  beq.s      IL_00ff

    IL_00f3:  ldstr      "F1upBy2ne failed"
    IL_00f8:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00fd:  ldc.i4.1
    IL_00fe:  stloc.0
    IL_00ff:  ldc.i4.s   10
    IL_0101:  call       int32 B::F1downBy2ne(int32)
    IL_0106:  ldc.i4.s   21
    IL_0108:  beq.s      IL_0116

    IL_010a:  ldstr      "F1downBy2ne failed"
    IL_010f:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0114:  ldc.i4.1
    IL_0115:  stloc.0
    IL_0116:  ldc.i4.1
    IL_0117:  call       int32 B::F1upBy3neWrap(int32)
    IL_011c:  ldc.i4.s   13
    IL_011e:  beq.s      IL_012c

    IL_0120:  ldstr      "F1upBy3neWrap failed"
    IL_0125:  call       void [System.Console]System.Console::WriteLine(string)
    IL_012a:  ldc.i4.1
    IL_012b:  stloc.0
    IL_012c:  ldc.i4.1
    IL_012d:  call       int32 B::F1downBy3neWrap(int32)
    IL_0132:  ldc.i4.4
    IL_0133:  beq.s      IL_0141

    IL_0135:  ldstr      "F1downBy3neWrap failed"
    IL_013a:  call       void [System.Console]System.Console::WriteLine(string)
    IL_013f:  ldc.i4.1
    IL_0140:  stloc.0
    IL_0141:  ldc.i4.s   10
    IL_0143:  call       int32 B::F2upBy1le(int32)
    IL_0148:  ldc.i4.s   45
    IL_014a:  beq.s      IL_0158

    IL_014c:  ldstr      "F2upBy1le failed"
    IL_0151:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0156:  ldc.i4.1
    IL_0157:  stloc.0
    IL_0158:  ldc.i4.s   10
    IL_015a:  call       int32 B::F2downBy1ge(int32)
    IL_015f:  ldc.i4.s   40
    IL_0161:  beq.s      IL_016f

    IL_0163:  ldstr      "F2downBy1ge failed"
    IL_0168:  call       void [System.Console]System.Console::WriteLine(string)
    IL_016d:  ldc.i4.1
    IL_016e:  stloc.0
    IL_016f:  ldc.i4.s   10
    IL_0171:  call       int32 B::F2upBy1lt(int32)
    IL_0176:  ldc.i4.s   34
    IL_0178:  beq.s      IL_0186

    IL_017a:  ldstr      "F2upBy1lt failed"
    IL_017f:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0184:  ldc.i4.1
    IL_0185:  stloc.0
    IL_0186:  ldc.i4.s   10
    IL_0188:  call       int32 B::F2downBy1gt(int32)
    IL_018d:  ldc.i4.s   32
    IL_018f:  beq.s      IL_019d

    IL_0191:  ldstr      "F2downBy1gt failed"
    IL_0196:  call       void [System.Console]System.Console::WriteLine(string)
    IL_019b:  ldc.i4.1
    IL_019c:  stloc.0
    IL_019d:  ldc.i4.s   10
    IL_019f:  call       int32 B::F2upBy2le(int32)
    IL_01a4:  ldc.i4.s   37
    IL_01a6:  beq.s      IL_01b4

    IL_01a8:  ldstr      "F2upBy2le failed"
    IL_01ad:  call       void [System.Console]System.Console::WriteLine(string)
    IL_01b2:  ldc.i4.1
    IL_01b3:  stloc.0
    IL_01b4:  ldc.i4.s   10
    IL_01b6:  call       int32 B::F2downBy2ge(int32)
    IL_01bb:  ldc.i4.s   29
    IL_01bd:  beq.s      IL_01cb

    IL_01bf:  ldstr      "F2downBy2ge failed"
    IL_01c4:  call       void [System.Console]System.Console::WriteLine(string)
    IL_01c9:  ldc.i4.1
    IL_01ca:  stloc.0
    IL_01cb:  ldc.i4.s   10
    IL_01cd:  call       int32 B::F2upBy2lt(int32)
    IL_01d2:  ldc.i4.s   25
    IL_01d4:  beq.s      IL_01e2

    IL_01d6:  ldstr      "F2upBy2lt failed"
    IL_01db:  call       void [System.Console]System.Console::WriteLine(string)
    IL_01e0:  ldc.i4.1
    IL_01e1:  stloc.0
    IL_01e2:  ldc.i4.s   10
    IL_01e4:  call       int32 B::F2downBy2gt(int32)
    IL_01e9:  ldc.i4.s   42
    IL_01eb:  beq.s      IL_01f9

    IL_01ed:  ldstr      "F2downBy2gt failed"
    IL_01f2:  call       void [System.Console]System.Console::WriteLine(string)
    IL_01f7:  ldc.i4.1
    IL_01f8:  stloc.0
    IL_01f9:  ldc.i4.s   10
    IL_01fb:  call       int32 B::F2upBy1ne(int32)
    IL_0200:  ldc.i4.s   34
    IL_0202:  beq.s      IL_0210

    IL_0204:  ldstr      "F2upBy1ne failed"
    IL_0209:  call       void [System.Console]System.Console::WriteLine(string)
    IL_020e:  ldc.i4.1
    IL_020f:  stloc.0
    IL_0210:  ldc.i4.s   10
    IL_0212:  call       int32 B::F2downBy1ne(int32)
    IL_0217:  ldc.i4.s   32
    IL_0219:  beq.s      IL_0227

    IL_021b:  ldstr      "F2downBy1ne failed"
    IL_0220:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0225:  ldc.i4.1
    IL_0226:  stloc.0
    IL_0227:  ldc.i4.s   10
    IL_0229:  call       int32 B::F2upBy2ne(int32)
    IL_022e:  ldc.i4.s   25
    IL_0230:  beq.s      IL_023e

    IL_0232:  ldstr      "F2upBy2ne failed"
    IL_0237:  call       void [System.Console]System.Console::WriteLine(string)
    IL_023c:  ldc.i4.1
    IL_023d:  stloc.0
    IL_023e:  ldc.i4.s   10
    IL_0240:  call       int32 B::F2downBy2ne(int32)
    IL_0245:  ldc.i4.s   21
    IL_0247:  beq.s      IL_0255

    IL_0249:  ldstr      "F2downBy2ne failed"
    IL_024e:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0253:  ldc.i4.1
    IL_0254:  stloc.0
    IL_0255:  ldc.i4.1
    IL_0256:  call       int32 B::F2upBy3neWrap(int32)
    IL_025b:  ldc.i4.s   13
    IL_025d:  beq.s      IL_026b

    IL_025f:  ldstr      "F2upBy3neWrap failed"
    IL_0264:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0269:  ldc.i4.1
    IL_026a:  stloc.0
    IL_026b:  ldc.i4.1
    IL_026c:  call       int32 B::F2downBy3neWrap(int32)
    IL_0271:  ldc.i4.4
    IL_0272:  beq.s      IL_0280

    IL_0274:  ldstr      "F2downBy3neWrap failed"
    IL_0279:  call       void [System.Console]System.Console::WriteLine(string)
    IL_027e:  ldc.i4.1
    IL_027f:  stloc.0
    IL_0280:  ldc.i4.s   10
    IL_0282:  call       int32 B::F3upBy1le(int32)
    IL_0287:  ldc.i4.s   45
    IL_0289:  beq.s      IL_0297

    IL_028b:  ldstr      "F3upBy1le failed"
    IL_0290:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0295:  ldc.i4.1
    IL_0296:  stloc.0
    IL_0297:  ldc.i4.s   10
    IL_0299:  call       int32 B::F3downBy1ge(int32)
    IL_029e:  ldc.i4.s   40
    IL_02a0:  beq.s      IL_02ae

    IL_02a2:  ldstr      "F3downBy1ge failed"
    IL_02a7:  call       void [System.Console]System.Console::WriteLine(string)
    IL_02ac:  ldc.i4.1
    IL_02ad:  stloc.0
    IL_02ae:  ldc.i4.s   10
    IL_02b0:  call       int32 B::F3upBy1lt(int32)
    IL_02b5:  ldc.i4.s   34
    IL_02b7:  beq.s      IL_02c5

    IL_02b9:  ldstr      "F3upBy1lt failed"
    IL_02be:  call       void [System.Console]System.Console::WriteLine(string)
    IL_02c3:  ldc.i4.1
    IL_02c4:  stloc.0
    IL_02c5:  ldc.i4.s   10
    IL_02c7:  call       int32 B::F3downBy1gt(int32)
    IL_02cc:  ldc.i4.s   32
    IL_02ce:  beq.s      IL_02dc

    IL_02d0:  ldstr      "F3downBy1gt failed"
    IL_02d5:  call       void [System.Console]System.Console::WriteLine(string)
    IL_02da:  ldc.i4.1
    IL_02db:  stloc.0
    IL_02dc:  ldc.i4.s   10
    IL_02de:  call       int32 B::F3upBy2le(int32)
    IL_02e3:  ldc.i4.s   37
    IL_02e5:  beq.s      IL_02f3

    IL_02e7:  ldstr      "F3upBy2le failed"
    IL_02ec:  call       void [System.Console]System.Console::WriteLine(string)
    IL_02f1:  ldc.i4.1
    IL_02f2:  stloc.0
    IL_02f3:  ldc.i4.s   10
    IL_02f5:  call       int32 B::F3downBy2ge(int32)
    IL_02fa:  ldc.i4.s   29
    IL_02fc:  beq.s      IL_030a

    IL_02fe:  ldstr      "F3downBy2ge failed"
    IL_0303:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0308:  ldc.i4.1
    IL_0309:  stloc.0
    IL_030a:  ldc.i4.s   10
    IL_030c:  call       int32 B::F3upBy2lt(int32)
    IL_0311:  ldc.i4.s   25
    IL_0313:  beq.s      IL_0321

    IL_0315:  ldstr      "F3upBy2lt failed"
    IL_031a:  call       void [System.Console]System.Console::WriteLine(string)
    IL_031f:  ldc.i4.1
    IL_0320:  stloc.0
    IL_0321:  ldc.i4.s   10
    IL_0323:  call       int32 B::F3downBy2gt(int32)
    IL_0328:  ldc.i4.s   42
    IL_032a:  beq.s      IL_0338

    IL_032c:  ldstr      "F3downBy2gt failed"
    IL_0331:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0336:  ldc.i4.1
    IL_0337:  stloc.0
    IL_0338:  ldc.i4.s   10
    IL_033a:  call       int32 B::F3upBy1ne(int32)
    IL_033f:  ldc.i4.s   34
    IL_0341:  beq.s      IL_034f

    IL_0343:  ldstr      "F3upBy1ne failed"
    IL_0348:  call       void [System.Console]System.Console::WriteLine(string)
    IL_034d:  ldc.i4.1
    IL_034e:  stloc.0
    IL_034f:  ldc.i4.s   10
    IL_0351:  call       int32 B::F3downBy1ne(int32)
    IL_0356:  ldc.i4.s   32
    IL_0358:  beq.s      IL_0366

    IL_035a:  ldstr      "F3downBy1ne failed"
    IL_035f:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0364:  ldc.i4.1
    IL_0365:  stloc.0
    IL_0366:  ldc.i4.s   10
    IL_0368:  call       int32 B::F3upBy2ne(int32)
    IL_036d:  ldc.i4.s   25
    IL_036f:  beq.s      IL_037d

    IL_0371:  ldstr      "F3upBy2ne failed"
    IL_0376:  call       void [System.Console]System.Console::WriteLine(string)
    IL_037b:  ldc.i4.1
    IL_037c:  stloc.0
    IL_037d:  ldc.i4.s   10
    IL_037f:  call       int32 B::F3downBy2ne(int32)
    IL_0384:  ldc.i4.s   21
    IL_0386:  beq.s      IL_0394

    IL_0388:  ldstr      "F3downBy2ne failed"
    IL_038d:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0392:  ldc.i4.1
    IL_0393:  stloc.0
    IL_0394:  ldc.i4.1
    IL_0395:  call       int32 B::F3upBy3neWrap(int32)
    IL_039a:  ldc.i4.s   13
    IL_039c:  beq.s      IL_03aa

    IL_039e:  ldstr      "F3upBy3neWrap failed"
    IL_03a3:  call       void [System.Console]System.Console::WriteLine(string)
    IL_03a8:  ldc.i4.1
    IL_03a9:  stloc.0
    IL_03aa:  ldc.i4.1
    IL_03ab:  call       int32 B::F3downBy3neWrap(int32)
    IL_03b0:  ldc.i4.4
    IL_03b1:  beq.s      IL_03bf

    IL_03b3:  ldstr      "F3downBy3neWrap failed"
    IL_03b8:  call       void [System.Console]System.Console::WriteLine(string)
    IL_03bd:  ldc.i4.1
    IL_03be:  stloc.0
    IL_03bf:  ldloc.0
    IL_03c0:  brtrue.s   IL_03d6

    IL_03c2:  call       void [System.Console]System.Console::WriteLine()
    IL_03c7:  ldstr      "Passed"
    IL_03cc:  call       void [System.Console]System.Console::WriteLine(string)
    IL_03d1:  ldc.i4.s   100
    IL_03d3:  stloc.1
    IL_03d4:  br.s       IL_03e9

    IL_03d6:  call       void [System.Console]System.Console::WriteLine()
    IL_03db:  ldstr      "Failed"
    IL_03e0:  call       void [System.Console]System.Console::WriteLine(string)
    IL_03e5:  ldc.i4.1
    IL_03e6:  stloc.1
    IL_03e7:  br.s       IL_03e9

    IL_03e9:  ldloc.1
    IL_03ea:  ret
  } // end of method B::Main

  .method public hidebysig specialname rtspecialname 
          instance void .ctor() 
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void A::.ctor()
    IL_0006:  ret
  } // end of method B::.ctor

}
