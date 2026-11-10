// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern xunit.core { }
.assembly extern System.Console { }

.assembly Runtime_73615 { }

.class public auto ansi beforefieldinit Runtime_73615
       extends [System.Runtime]System.Object
{
  .class auto ansi nested public beforefieldinit C
         extends [System.Runtime]System.Object
  {
    .field public int32 Result
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor(int32 result) cil managed
    {
      // Code size       14 (0xe)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
      IL_0006:  ldarg.0
      IL_0007:  ldarg.1
      IL_0008:  stfld      int32 Runtime_73615/C::Result
      IL_000d:  ret
    } // end of method C::.ctor

    .method public hidebysig instance void 
            Baz(int32 arg) cil managed noinlining
    {
      // Code size       12 (0xc)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldfld      int32 Runtime_73615/C::Result
      IL_0006:  stsfld     int32 Runtime_73615::Result
      IL_000b:  ret
    } // end of method C::Baz

  } // end of class C

  .field public static int32 Result
  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = ( 01 00 00 00 ) 
    .entrypoint
    // Code size       18 (0x12)
    .maxstack  8
    IL_0000:  ldc.i4.s   101
    IL_0002:  newobj     instance void Runtime_73615/C::.ctor(int32)
    IL_0007:  call       void Runtime_73615::Foo(class Runtime_73615/C)
    IL_000c:  ldsfld     int32 Runtime_73615::Result
    IL_0011:  ret
  } // end of method Runtime_73615::TestEntryPoint

  .method private hidebysig static void  Foo(class Runtime_73615/C arg) cil managed noinlining
  {
    // Code size       21 (0x15)
    .maxstack  2
    IL_0000:  ldarga.s   arg
    IL_0002:  ldarga.s   arg
    IL_0004:  call       int32 Runtime_73615::Bar(class Runtime_73615/C&)
    IL_0009:  constrained. Runtime_73615/C
    IL_000f:  callvirt   instance void Runtime_73615/C::Baz(int32)
    IL_0014:  ret
  } // end of method Runtime_73615::Foo

  .method private hidebysig static int32 
          Bar(class Runtime_73615/C& o) cil managed noinlining
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.s   100
    IL_0003:  newobj     instance void Runtime_73615/C::.ctor(int32)
    IL_0008:  stind.ref
    IL_0009:  ldc.i4.0
    IL_000a:  ret
  } // end of method Runtime_73615::Bar

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Runtime_73615::.ctor

} // end of class Runtime_73615
