// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.class public auto ansi beforefieldinit ModOptTester
       extends [CoreTestAssembly]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [CoreTestAssembly]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method I::.ctor

  .method public hidebysig instance int32 modopt([CoreTestAssembly]System.Void) modopt([CoreTestAssembly]System.Char) Method(int32 modopt(FooModifier)) cil managed
  {
     ret
  }

  .method public hidebysig instance int32 modopt([CoreTestAssembly]System.Void) & modopt([CoreTestAssembly]System.Char) Method2(int32 modopt(FooModifier)) cil managed
  {
     ret
  }

  .method public hidebysig instance int32 modopt([CoreTestAssembly]System.Void) & Method3(int32 modopt(FooModifier)*, int32 modopt(FooModifier)*) cil managed
  {
     ret
  }

  .method public hidebysig instance int32 modopt([CoreTestAssembly]System.Void) [3,1 ... 4] Method4(int32 modopt(FooModifier)[2 ...,0...8], int32 modopt(FooModifier)[0...,...], int32 [,,,]) cil managed
  {
     ret
  }
}

.class private auto ansi beforefieldinit Atom
       extends [CoreTestAssembly]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [CoreTestAssembly]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method I::.ctor

} // end of class Atom

.class private auto ansi beforefieldinit A`1<U>
       extends [CoreTestAssembly]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [CoreTestAssembly]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method A`1::.ctor

} // end of class A`1

.class private auto ansi beforefieldinit BaseClass`2<U,T>
       extends [CoreTestAssembly]System.Object
{
  .method public hidebysig newslot virtual 
          instance void  Method(!U u,
                                !T modopt (FooModifier) t) cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method BaseClass`2::Method

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [CoreTestAssembly]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method BaseClass`2::.ctor

} // end of class BaseClass`2

.class private auto ansi beforefieldinit DerivedClass
       extends class BaseClass`2<class A`1<class Atom>,class Atom>
{
  .method public hidebysig virtual instance void 
          Method(class A`1<class Atom> u,
                 class Atom modopt (FooModifier) t) cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method DerivedClass::Method

  .method public hidebysig virtual instance void 
          Method(class A`1<class Atom> u,
                 class Atom t) cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method DerivedClass::Method

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void class BaseClass`2<class A`1<class Atom>,class Atom>::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method DerivedClass::.ctor

} // end of DerivedClass

.class public FooModifier { }
.class public BarModifier { }
