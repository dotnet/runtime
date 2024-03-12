// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime { }
.assembly extern xunit.core {}

.assembly boring { }

.class interface private abstract auto ansi IFoo`1<T>
{
  .method public hidebysig newslot abstract virtual instance int32 Frob() cil managed
  {
  }

  .method public hidebysig newslot virtual instance int32 Bark() cil managed
  {
    ldc.i4.m1
    ret
  }
}

.class interface private abstract auto ansi IBar
       implements class IFoo`1<class [System.Runtime]System.String>,
                  class IFoo`1<class [System.Runtime]System.Object>,
                  class IFoo`1<class [System.Runtime]System.ValueType>
{
  .method public hidebysig newslot virtual final instance int32 FrobImpl1() cil managed
  {
    .override class IFoo`1<class [System.Runtime]System.String>::Frob
    .override class IFoo`1<class [System.Runtime]System.Object>::Frob
    ldc.i4.1
    ret
  }

  .method public hidebysig newslot virtual final instance int32 FrobImpl2() cil managed
  {
    .override class IFoo`1<class [System.Runtime]System.ValueType>::Frob
    ldc.i4.2
    ret
  }

  .method public hidebysig newslot virtual final instance int32 BarkImpl1() cil managed
  {
    .override class IFoo`1<class [System.Runtime]System.String>::Bark
    ldc.i4.3
    ret
  }

  .method public hidebysig newslot virtual final instance int32 BarkImpl2() cil managed
  {
    .override class IFoo`1<class [System.Runtime]System.ValueType>::Bark
    ldc.i4.4
    ret
  }
}

.class public auto ansi beforefieldinit Fooer
       extends [System.Runtime]System.Object
       implements IBar
{
  .method public hidebysig specialname rtspecialname
          instance void .ctor() cil managed
  {
    ldarg.0
    call instance void [System.Runtime]System.Object::.ctor()
    ret
  }
}

.class public auto ansi abstract sealed beforefieldinit boring
    extends [System.Runtime]System.Object
{
  .method public hidebysig static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint

    newobj instance void class Fooer::.ctor()
    callvirt instance int32 class IFoo`1<string>::Frob()
    ldc.i4.1
    ceq
    brtrue IFoo_String_Frob_Okay
    ldc.i4.1
    ret
  IFoo_String_Frob_Okay:

    newobj instance void class Fooer::.ctor()
    callvirt instance int32 class IFoo`1<object>::Frob()
    ldc.i4.1
    ceq
    brtrue IFoo_Object_Frob_Okay
    ldc.i4.2
    ret
  IFoo_Object_Frob_Okay:

    newobj instance void class Fooer::.ctor()
    callvirt instance int32 class IFoo`1<class [System.Runtime]System.ValueType>::Frob()
    ldc.i4.2
    ceq
    brtrue IFoo_ValueType_Frob_Okay
    ldc.i4.3
    ret
  IFoo_ValueType_Frob_Okay:

    newobj instance void class Fooer::.ctor()
    callvirt instance int32 class IFoo`1<string>::Bark()
    ldc.i4.3
    ceq
    brtrue IFoo_String_Bark_Okay
    ldc.i4.4
    ret
  IFoo_String_Bark_Okay:

    newobj instance void class Fooer::.ctor()
    callvirt instance int32 class IFoo`1<object>::Bark()
    ldc.i4.m1
    ceq
    brtrue IFoo_Object_Bark_Okay
    ldc.i4.5
    ret
  IFoo_Object_Bark_Okay:

    newobj instance void class Fooer::.ctor()
    callvirt instance int32 class IFoo`1<class [System.Runtime]System.ValueType>::Bark()
    ldc.i4.4
    ceq
    brtrue IFoo_ValueType_Bark_Okay
    ldc.i4.6
    ret
  IFoo_ValueType_Bark_Okay:

    ldc.i4 100
    ret
  }
}
