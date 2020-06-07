// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Runtime { }

.assembly provider { }

.class interface public abstract auto ansi I2
{
  .method public hidebysig newslot abstract virtual instance int32 M1()
  {
  }
}

.class interface public abstract auto ansi I4
{
  .method public hidebysig newslot abstract virtual instance int32 M1()
  {
  }
}

.class interface public abstract auto ansi I1
       implements I2,
                  I4
{
  .method private hidebysig virtual final instance int32 I2.M1()
  {
    .override I2::M1
    ldc.i4.1
    ret
  }

  .method private hidebysig virtual final instance int32 I4.M1()
  {
    .override I4::M1
    ldc.i4.2
    ret
  }
}

.class interface public abstract auto ansi IGen2`1<T>
{
  .method public hidebysig newslot abstract virtual instance int32 M1()
  {
  }
}

.class interface public abstract auto ansi IGen1`1<T>
       implements class IGen2`1<class [System.Runtime]System.ValueType>,
                  class IGen2`1<!T>
{
  .method private hidebysig virtual final instance int32 Foo()
  {
    .override class IGen2`1<class [System.Runtime]System.ValueType>::M1
    ldc.i4.3
    ret
  }

  .method private hidebysig virtual final instance int32 Bar()
  {
    .override class IGen2`1<!T>::M1
    ldc.i4.4
    ret
  }
}
