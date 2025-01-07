// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime { }
.assembly extern xunit.core {}
.assembly extern System.Console { }
.assembly createdelegate { }

.class public auto ansi beforefieldinit A
       extends [System.Runtime]System.Object
       implements B,
                  C,
                  D
{
  .method private hidebysig newslot virtual final 
          instance char  B.F1(int32 pA) cil managed
  {
    .override B::F1
    ldc.i4.s   65
    ret
  }

  .method private hidebysig newslot virtual final 
          instance char  D.F1(int32 pA) cil managed
  {
    .override D::F1
    ldc.i4.s   65
    ret
  }

  .method private hidebysig instance int32
          Test() cil managed
  {
    ldtoken    class [System.Runtime]System.Func`2<int32,char>
    call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    ldarg.0
    ldtoken    method instance char D::F1(int32)
    call       class [System.Runtime]System.Reflection.MethodBase [System.Runtime]System.Reflection.MethodBase::GetMethodFromHandle(valuetype [System.Runtime]System.RuntimeMethodHandle)
    castclass  class [System.Runtime]System.Reflection.MethodInfo
    call       class [System.Runtime]System.Delegate [System.Runtime]System.Delegate::CreateDelegate(class [System.Runtime]System.Type, object, class [System.Runtime]System.Reflection.MethodInfo)
    castclass  class [System.Runtime]System.Func`2<int32,char>

    ldc.i4.1
    callvirt   instance !1 class [System.Runtime]System.Func`2<int32,char>::Invoke(!0)
    dup
    call       void [System.Console]System.Console::WriteLine(char)
    ldc.i4.s   65
    ceq
    brtrue A_F1_OK

    ldc.i4.3
    ret

A_F1_OK:

    ldc.i4.s   100

    ret
  }

  .method public hidebysig static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    newobj     instance void A::.ctor()
    call       instance int32 A::Test()
    ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [System.Runtime]System.Object::.ctor()
    ret
  }
}

.class interface private abstract auto ansi B
       implements C,
                  D
{
  .method public hidebysig newslot virtual 
          instance char  F1(int32 pB) cil managed
  {
    ldc.i4.s   66
    ret
  }
}

.class interface private abstract auto ansi C
       implements D
{
}

.class interface private abstract auto ansi D
{
  .method public hidebysig newslot virtual 
          instance char  F1(int32 pD) cil managed
  {
    ldc.i4.s   68
    ret
  }
}
