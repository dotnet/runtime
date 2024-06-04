// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

.assembly extern System.Runtime { }
.assembly extern xunit.core {}
.assembly extern System.Console { }

.assembly debug3 { }

.class interface public abstract auto ansi I1
{
  .method public hidebysig newslot virtual abstract
          instance int32 AddImpl(int32 x) cil managed
  {
  }

  .method public hidebysig newslot virtual
          instance int32  Add(int32 x) cil managed
  {
    ldarg.0
    ldarg.1
    ldc.i4.1
    add
    callvirt instance int32 class I1::AddImpl(int32 x)
    ret
  }
}

.class private sequential ansi sealed beforefieldinit Test1
       extends [System.Runtime]System.ValueType
       implements I1
{
  .field public int32 Value

  .method public hidebysig newslot virtual
          instance int32 AddImpl(int32 x) cil managed
  {
      ldarg.0
      ldfld int32 Test1::Value
      ldarg.1
      add
      ret
  }
}

.class interface public abstract auto ansi I2`1<T>
{
  .method public hidebysig newslot virtual abstract
          instance int32 AddImpl(int32 x) cil managed
  {
  }

  .method public hidebysig newslot virtual
          instance int32  Add(int32 x) cil managed
  {
    ldarg.0

    ldtoken !T
    call class [System.Runtime]System.Type class [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    callvirt instance string [System.Runtime]System.Object::ToString()
    call instance int32 [System.Runtime]System.String::get_Length()
    ldarg.1
    add

    ldc.i4.1
    add

    callvirt instance int32 class I2`1<!T>::AddImpl(int32 x)
    ret
  }
}

.class public sequential ansi sealed beforefieldinit Test2`1<T>
       extends [System.Runtime]System.ValueType
       implements class I2`1<!T>
{
  .field public int32 Value

  .method public hidebysig newslot virtual
          instance int32 AddImpl(int32 x) cil managed
  {
      ldarg.0
      ldfld int32 Test1::Value
      ldarg.1
      add

      ldtoken !T[]
      call class [System.Runtime]System.Type class [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
      callvirt instance string [System.Runtime]System.Object::ToString()
      call instance int32 [System.Runtime]System.String::get_Length()
      add

      ret
  }
}


.class public auto ansi abstract sealed beforefieldinit debug3
    extends [System.Runtime]System.Object
{
  .method public hidebysig static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .locals init (class I1 V_0,
              valuetype Test1 V_1,
              class [System.Runtime]System.Func`2<int32, int32> V_2,
              class I2`1<class [System.Runtime]System.String> V_3,
              valuetype Test2`1<class [System.Runtime]System.String> V_4)

    //
    // Nongeneric case
    //

    ldloca.s   V_1
    initobj    Test1

    // V_1.Value = 30
    ldloca.s   V_1
    ldc.i4 30
    stfld int32 Test1::Value

    // V_0 = (I1)V_1;
    ldloc.1
    box        Test1
    stloc.0

    // V_2 = V_0.Add
    ldloc.0
    dup
    ldvirtftn  instance int32 I1::Add(int32)
    newobj     instance void class [System.Runtime]System.Func`2<int32, int32>::.ctor(object, native int)
    stloc.2

    // V_2(2)
    ldloc.2
    ldc.i4.2
    callvirt   instance !1 class [System.Runtime]System.Func`2<int32, int32>::Invoke(!0)

    //
    // We expect 33 on the stack now
    //

    //
    // Generic case
    //

    ldloca.s   V_4
    initobj    valuetype Test2`1<class [System.Runtime]System.String>

    // V_4.Value = 36
    ldloca.s   V_4
    ldc.i4 36
    stfld int32 valuetype Test2`1<class [System.Runtime]System.String>::Value

    // V_3 = (I1)V_4;
    ldloc.s    4
    box        valuetype Test2`1<class [System.Runtime]System.String>
    stloc.3

    // V_2 = V_3.Add
    ldloc.3
    dup
    ldvirtftn  instance int32 class I2`1<class [System.Runtime]System.String>::Add(int32)
    newobj     instance void class [System.Runtime]System.Func`2<int32, int32>::.ctor(object, native int)
    stloc.2

    ldloc.2
    ldc.i4.2
    callvirt   instance !1 class [System.Runtime]System.Func`2<int32, int32>::Invoke(!0)

    //
    // We expect this return 67
    //

    //
    // Add the two results - sum should be 100
    //

    add

    ret
  }
}