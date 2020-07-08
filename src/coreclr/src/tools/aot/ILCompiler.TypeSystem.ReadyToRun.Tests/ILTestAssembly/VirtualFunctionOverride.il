// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.class public auto ansi beforefieldinit VirtualFunctionOverride.ExplicitOverride
       extends [CoreTestAssembly]System.Object
{
  .method public virtual int32 MyGetHashCode()
  {
    .override [CoreTestAssembly]System.Object::GetHashCode

    ldc.i4 42
    ret
  }
}

.class private auto ansi beforefieldinit VirtualFunctionOverride.MyBase
       extends [CoreTestAssembly]System.Object
{
  .method public hidebysig newslot specialname virtual 
          instance int32  get_foo() cil managed
  {
    ldc.i4 42
    ret
  }

  .property instance int32 foo()
  {
    .get instance int32 VirtualFunctionOverride.MyBase::get_foo()
  }
}

.class private auto ansi beforefieldinit VirtualFunctionOverride.MyDerived2
       extends VirtualFunctionOverride.MyBase
{
  .method public hidebysig newslot specialname virtual 
          instance int32  get_foo() cil managed
  {
    .override VirtualFunctionOverride.MyBase::get_foo
    
    ldc.i4 42
    ret
  }

  .property instance int32 foo()
  {
    .get instance int32 VirtualFunctionOverride.MyDerived2::get_foo()
  }
}
