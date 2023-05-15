// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly Runtime_45250
{
}
.assembly extern xunit.core {}

.class public auto ansi beforefieldinit Runtime_45250.Program
       extends [System.Runtime]System.Object
{
  .class auto ansi nested private beforefieldinit Func`1<T>
         extends [System.Runtime]System.Object
  {
    .method public hidebysig newslot virtual
            instance void  Run() cil managed noinlining
    {
      .maxstack  8
      IL_0000:  ret
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

  .class auto ansi nested private beforefieldinit FuncGetter
         extends [System.Runtime]System.Object
  {
    .field public int32 counter

    .method public hidebysig instance class Runtime_45250.Program/Func`1<!!T>
            Get<T>() cil managed noinlining
    {
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldarg.0
      IL_0002:  ldfld      int32 Runtime_45250.Program/FuncGetter::counter
      IL_0007:  ldc.i4.1
      IL_0008:  add
      IL_0009:  stfld      int32 Runtime_45250.Program/FuncGetter::counter
      IL_000e:  newobj     instance void class Runtime_45250.Program/Func`1<!!T>::.ctor()
      IL_0013:  ret
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

  .method private hidebysig static void  Run(class Runtime_45250.Program/FuncGetter funcGetter) cil managed noinlining
  {
    .maxstack  8
    IL_0000:  ldc.i4     45250
    IL_0002:  localloc
    IL_0004:  pop
    IL_0005:  ldarg.0
    IL_0006:  call       instance class Runtime_45250.Program/Func`1<!!0> Runtime_45250.Program/FuncGetter::Get<string>()
    IL_000b:  tail.
    IL_000d:  callvirt   instance void class Runtime_45250.Program/Func`1<string>::Run()
    IL_0012:  ret
  }

  .method public hidebysig static int32
          Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  8
    IL_0000:  newobj     instance void Runtime_45250.Program/FuncGetter::.ctor()
    IL_0005:  dup
    IL_0006:  call       void Runtime_45250.Program::Run(class Runtime_45250.Program/FuncGetter)
    IL_000b:  ldfld      int32 Runtime_45250.Program/FuncGetter::counter
    IL_0010:  ldc.i4.1
    IL_0011:  beq.s      IL_0015
    IL_0013:  ldc.i4.1
    IL_0014:  ret
    IL_0015:  ldc.i4.s   100
    IL_0017:  ret
  }
}
