// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern mscorlib { }
.assembly modifiersdata { }

.class public BaseClass
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
    ldarg.0
    call instance void [mscorlib]System.Object::.ctor()
    ret
  }

  .method public hidebysig virtual newslot instance int32 Override(int32 modopt (FooModifier))
  {
    ldstr "In int32 BaseClass::Override(int32 modopt (FooModifier))"
    call void class [mscorlib]System.Console::WriteLine(string)
    ldc.i4.0
    ret
  }

  .method public hidebysig virtual newslot instance int32 Override(int32 modopt (BarModifier))
  {
    ldstr "In int32 BaseClass::Override(int32 modopt (BarModifier))"
    call void class [mscorlib]System.Console::WriteLine(string)
    ldc.i4.1
    ret
  }

  .method public hidebysig virtual newslot instance int32 Override(int32 modreq (FooModifier))
  {
    ldstr "In int32 BaseClass::Override(int32 modreq (FooModifier))"
    call void class [mscorlib]System.Console::WriteLine(string)
    ldc.i4.2
    ret
  }

  .method public hidebysig virtual newslot instance int32 Override(int32)
  {
    ldstr "In int32 BaseClass::Override(int32)"
    call void class [mscorlib]System.Console::WriteLine(string)
    ldc.i4.3
    ret
  }

  .method public hidebysig virtual newslot instance int32 modopt (FooModifier) Override(int32)
  {
    ldstr "In int32 modopt (FooModifier) BaseClass::Override(int32)"
    call void class [mscorlib]System.Console::WriteLine(string)
    ldc.i4.4
    ret
  }

  .method public hidebysig virtual newslot instance int32 Override(int32 modopt (FooModifier)[])
  {
    ldstr "In int32 BaseClass::Override(int32 modopt (FooModifier)[])"
    call void class [mscorlib]System.Console::WriteLine(string)
    ldc.i4.5
    ret
  }

  .method public hidebysig virtual newslot instance int32 Override(int32 modopt (BarModifier)[])
  {
    ldstr "In int32 BaseClass::Override(int32 modopt (BarModifier)[])"
    call void class [mscorlib]System.Console::WriteLine(string)
    ldc.i4.6
    ret
  }

}

.class public DerivedClass extends BaseClass
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
    ldarg.0
    call instance void BaseClass::.ctor()
    ret
  }

  .method public hidebysig virtual instance int32 Override(int32 modopt (FooModifier))
  {
    ldstr "In int32 DerivedClass::Override(int32 modopt (FooModifier))"
    call void class [mscorlib]System.Console::WriteLine(string)
    ldc.i4 100
    ret
  }

  .method public hidebysig virtual instance int32 Override(int32 modopt (BarModifier))
  {
    ldstr "In int32 DerivedClass::Override(int32 modopt (BarModifier))"
    call void class [mscorlib]System.Console::WriteLine(string)
    ldc.i4 101
    ret
  }

  .method public hidebysig virtual instance int32 Override(int32 modreq (FooModifier))
  {
    ldstr "In int32 DerivedClass::Override(int32 modreq (FooModifier))"
    call void class [mscorlib]System.Console::WriteLine(string)
    ldc.i4 102
    ret
  }

  .method public hidebysig virtual instance int32 Override(int32)
  {
    ldstr "In int32 DerivedClass::Override(int32)"
    call void class [mscorlib]System.Console::WriteLine(string)
    ldc.i4 103
    ret
  }

  .method public hidebysig virtual instance int32 modopt (FooModifier) Override(int32)
  {
    ldstr "In int32 modopt (FooModifier) DerivedClass::Override(int32)"
    call void class [mscorlib]System.Console::WriteLine(string)
    ldc.i4 104
    ret
  }

  .method public hidebysig virtual instance int32 Override(int32 modopt (FooModifier)[])
  {
    ldstr "In int32 DerivedClass::Override(int32 modopt (FooModifier)[])"
    call void class [mscorlib]System.Console::WriteLine(string)
    ldc.i4 105
    ret
  }

  .method public hidebysig virtual instance int32 Override(int32 modopt (BarModifier)[])
  {
    ldstr "In int32 DerivedClass::Override(int32 modopt (BarModifier)[])"
    call void class [mscorlib]System.Console::WriteLine(string)
    ldc.i4 106
    ret
  }
}

.class public GenericClass`1<T>
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
    ldarg.0
    call instance void [mscorlib]System.Object::.ctor()
    ret
  }
}

.class public value sequential GenericStruct`1<T> { .size 1 }

.class public Factory
{
  .method public static hidebysig object TryAllocWithModifiedArray()
  {
    newobj instance void class GenericClass`1<int32 modopt (FooModifier)[]>::.ctor()
    ret
  }

  .method public static hidebysig object TryBoxWithModifiedPointer()
  {
    .locals init (valuetype GenericStruct`1<int32 modreq (BarModifier)*[]>)
    ldloc 0
    box valuetype GenericStruct`1<int32 modreq (BarModifier)*[]>
    ret
  }
}

.class public FooModifier { }
.class public BarModifier { }
