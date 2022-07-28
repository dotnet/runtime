// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.class private abstract auto ansi beforefieldinit MethodImplOverride1.Base`2<T,U>
       extends [CoreTestAssembly]System.Object
{
  .method public hidebysig newslot virtual 
          instance string  Method([out] class [CoreTestAssembly]GenericTypes.GenericClass`1<!U>& y,
                                class [CoreTestAssembly]GenericTypes.GenericClass`1<!T>& x) cil managed
  {
    // Code size       16 (0x10)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  ldnull
    IL_0003:  stind.ref
    IL_0004:  ldstr      "Base<T, U>.Method(out List<U> y, ref List<T> x)"
    IL_000f:  ret
  } // end of method MethodImplOverride1.Base`2::Method

  .method public hidebysig newslot virtual 
          instance string  Method(class [CoreTestAssembly]GenericTypes.GenericClass`1<!T>& x,
                                [out] class [CoreTestAssembly]GenericTypes.GenericClass`1<!U>& y) cil managed
  {
    // Code size       16 (0x10)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.2
    IL_0002:  ldnull
    IL_0003:  stind.ref
    IL_0004:  ldstr      "Base<T, U>.Method(ref List<T> x, out List<U> y)"
    IL_000f:  ret
  } // end of method MethodImplOverride1.Base`2::Method

  .method public hidebysig newslot virtual 
          instance string  Method(class [CoreTestAssembly]GenericTypes.GenericClass`1<!U>& x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "Base<T, U>.Method(ref List<U> x)"
    IL_000c:  ret
  } // end of method MethodImplOverride1.Base`2::Method

  .method family hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [CoreTestAssembly]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method MethodImplOverride1.Base`2::.ctor

} // end of class MethodImplOverride1.Base`2

.class private auto ansi beforefieldinit MethodImplOverride1.Base2`2<A,B>
       extends class MethodImplOverride1.Base`2<!A,!B>
{
  .method public hidebysig newslot virtual 
          instance string  Method([out] class [CoreTestAssembly]GenericTypes.GenericClass`1<!A>& x) cil managed
  {
    // Code size       16 (0x10)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  ldnull
    IL_0003:  stind.ref
    IL_0004:  ldstr      "Base2<A, B>.Method(out List<A> x)"
    IL_000f:  ret
  } // end of method MethodImplOverride1.Base2`2::Method

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void class MethodImplOverride1.Base`2<!A,!B>::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method MethodImplOverride1.Base2`2::.ctor

} // end of class MethodImplOverride1.Base2`2

.class private auto ansi beforefieldinit MethodImplOverride1.Derived
       extends class MethodImplOverride1.Base2`2<int32,int32>
{
  .method public hidebysig newslot virtual 
          instance string  Method(class [CoreTestAssembly]GenericTypes.GenericClass`1<int32>& a,
                                [out] class [CoreTestAssembly]GenericTypes.GenericClass`1<int32>& b) cil managed
  {
    .override  method instance string class MethodImplOverride1.Base`2<int32,int32>::Method(class [CoreTestAssembly]GenericTypes.GenericClass`1<!0>&,
                                                                      class [CoreTestAssembly]GenericTypes.GenericClass`1<!1>&)
    // Code size       16 (0x10)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.2
    IL_0002:  ldnull
    IL_0003:  stind.ref
    IL_0004:  ldstr      "Derived.Method(ref List<int> a, out List<int> b)"
    IL_000f:  ret
  } // end of method MethodImplOverride1.Derived::Method

  .method public hidebysig newslot virtual 
          instance string  Method(class [CoreTestAssembly]GenericTypes.GenericClass`1<int32>& a) cil managed
  {
    .override  method instance string class MethodImplOverride1.Base`2<int32,int32>::Method(class [CoreTestAssembly]GenericTypes.GenericClass`1<!1>&)
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "Derived.Method(ref List<int> a)"
    IL_000c:  ret
  } // end of method MethodImplOverride1.Derived::Method

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void class MethodImplOverride1.Base2`2<int32,int32>::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method MethodImplOverride1.Derived::.ctor

} // end of class MethodImplOverride1.Derived


// TEST2 classes
.class private auto ansi beforefieldinit MethodImplOverride1.BaseTestGenericsShape`4<A,B,C,D>
      extends [CoreTestAssembly]System.Object
{
  .method public hidebysig newslot virtual 
          instance string  Method(!A a,
                                  !B b) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "BaseTestGenericsShape.Method(A a, B b)"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method MethodImplOverride1.BaseTestGenericsShape`4::Method

  .method public hidebysig newslot virtual 
          instance string  Method(!C modopt([CoreTestAssembly]System.Void) c,
                                  !D modopt([CoreTestAssembly]System.Object) d) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "BaseTestGenericsShape.Method(C c, D d)"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method MethodImplOverride1.BaseTestGenericsShape`4::Method

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [CoreTestAssembly]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method MethodImplOverride1.BaseTestGenericsShape`4::.ctor

} // end of class MethodImplOverride1.BaseTestGenericsShape`4

.class private auto ansi beforefieldinit MethodImplOverride1.IntermediateGenericsShape`2<E,F>
      extends class MethodImplOverride1.BaseTestGenericsShape`4<object,string,class [CoreTestAssembly]GenericTypes.GenericClass`1<!E>,!F[]>
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void class MethodImplOverride1.BaseTestGenericsShape`4<object,string,!E,!F>::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method MethodImplOverride1.IntermediateGenericsShape`2::.ctor

} // end of class MethodImplOverride1.IntermediateGenericsShape`2

.class private auto ansi beforefieldinit MethodImplOverride1.DerivedGenericsShape`1<G>
      extends class MethodImplOverride1.IntermediateGenericsShape`2<int32,!G>
{
  .method public hidebysig virtual instance string 
          MethodOnDerived(class [CoreTestAssembly]GenericTypes.GenericClass`1<int32> modopt([CoreTestAssembly]System.Void) e,
               !G[] modopt([CoreTestAssembly]System.Object) f) cil managed
  {
    .override  method instance string class MethodImplOverride1.IntermediateGenericsShape`2<int32,!G>::Method(class [CoreTestAssembly]GenericTypes.GenericClass`1<!0> modopt([CoreTestAssembly]System.Void), !1[]modopt([CoreTestAssembly]System.Object))
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "DerivedGenericsShape<G>.Method(int e, G f)"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method MethodImplOverride1.DerivedGenericsShape`1::Method

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void class MethodImplOverride1.IntermediateGenericsShape`2<int32,!G>::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method MethodImplOverride1.DerivedGenericsShape`1::.ctor

} // end of class MethodImplOverride1.DerivedGenericsShape`1

.class private auto ansi beforefieldinit MethodImplOverride1.DerivedGenericsShapeInvalidSubstitutedOverride
      extends class MethodImplOverride1.IntermediateGenericsShape`2<int32,int32>
{
  .method public hidebysig virtual instance string 
          MethodOnDerived(int32 e,
                int32 f) cil managed
  {
    .override  method instance string class  MethodImplOverride1.BaseTestGenericsShape`4<object,string, int32, int32>::Method(int32, int32)
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "DerivedGenericsShapeInvalidSubstitutedOverride.Method(int e, int f)"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method MethodImplOverride1.DerivedGenericsShapeInvalidSubstitutedOverride::Method

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void class MethodImplOverride1.IntermediateGenericsShape`2<int32,int32>::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method MethodImplOverride1.DerivedGenericsShapeInvalidSubstitutedOverride::.ctor

} // end of class MethodImplOverride1.DerivedGenericsShapeInvalidSubstitutedOverride


// Types for Test4
.class interface private abstract auto ansi MethodImplOverride1.IMultiGeneric`2<T,U>
{
  .method public hidebysig newslot abstract virtual 
          instance string  Func(!T t1,
                                !U u2) cil managed
  {
  } // end of method MethodImplOverride1.IMultiGeneric`2::Func

  .method public hidebysig newslot abstract virtual 
          instance string  Func(!U u1,
                                !T t2) cil managed
  {
  } // end of method MethodImplOverride1.IMultiGeneric`2::Func

} // end of class MethodImplOverride1.IMultiGeneric`2

.class private auto ansi beforefieldinit MethodImplOverride1.Implementor`2<A,B>
       extends [CoreTestAssembly]System.Object
       implements class MethodImplOverride1.IMultiGeneric`2<!A,!B>
{
  .method private hidebysig newslot virtual final 
          instance string  'IMultiGeneric<A,B>.Func'(!A t1,
                                                     !B u2) cil managed
  {
    .override  method instance string class MethodImplOverride1.IMultiGeneric`2<!A,!B>::Func(!0,
                                                                         !1)
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "Implementor<A,B>.IMultiGeneric<A, B>.Func(A t1, B "
    + "u2)"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method MethodImplOverride1.Implementor`2::'IMultiGeneric<A,B>.Func'

  .method private hidebysig newslot virtual final 
          instance string  'IMultiGeneric<A,B>.Func'(!B u1,
                                                     !A t2) cil managed
  {
    .override  method instance string class MethodImplOverride1.IMultiGeneric`2<!A,!B>::Func(!1,
                                                                         !0)
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "Implementor<A,B>.IMultiGeneric<A, B>.Func(B u1, A "
    + "t2)"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method MethodImplOverride1.Implementor`2::'IMultiGeneric<A,B>.Func'

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [CoreTestAssembly]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method MethodImplOverride1.Implementor`2::.ctor

} // end of class MethodImplOverride1.Implementor`2

.class private auto ansi beforefieldinit MethodImplOverride1.PartialIntImplementor`1<C>
       extends [CoreTestAssembly]System.Object
       implements class MethodImplOverride1.IMultiGeneric`2<!C,int32>
{
  .method private hidebysig newslot virtual final 
          instance string  'IMultiGeneric<C,System.Int32>.Func'(!C t1,
                                                                int32 u2) cil managed
  {
    .override  method instance string class MethodImplOverride1.IMultiGeneric`2<!C,int32>::Func(!0,
                                                                            !1)
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "PartialIntImplementor<C>.IMultiGeneric<C, int>.Fun"
    + "c(C t1, int u2)"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method MethodImplOverride1.PartialIntImplementor`1::'IMultiGeneric<C,System.Int32>.Func'

  .method private hidebysig newslot virtual final 
          instance string  'IMultiGeneric<C,System.Int32>.Func'(int32 u1,
                                                                !C t2) cil managed
  {
    .override  method instance string class MethodImplOverride1.IMultiGeneric`2<!C,int32>::Func(!1,
                                                                            !0)
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "PartialIntImplementor<C>.IMultiGeneric<C, int>.Fun"
    + "c(int u1, C t2)"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method MethodImplOverride1.PartialIntImplementor`1::'IMultiGeneric<C,System.Int32>.Func'

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [CoreTestAssembly]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method MethodImplOverride1.PartialIntImplementor`1::.ctor

} // end of class MethodImplOverride1.PartialIntImplementor`1

.class private auto ansi beforefieldinit MethodImplOverride1.IntImplementor
       extends [CoreTestAssembly]System.Object
       implements class MethodImplOverride1.IMultiGeneric`2<int32,int32>
{
  .method public hidebysig newslot virtual final 
          instance string  Func(int32 x,
                                int32 y) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "IntImplementor.Func(int x, int y)"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method MethodImplOverride1.IntImplementor::Func

  .method private hidebysig newslot virtual final 
          instance string  'IMultiGeneric<System.Int32,System.Int32>.Func(!0,!1)'(int32 u1,
                                                                           int32 t2) cil managed
  {
    .override  method instance string class MethodImplOverride1.IMultiGeneric`2<int32,int32>::Func(!0,
                                                                               !1)
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "IntImplementor.IMultiGeneric<int, int>.Func(!0, !1)"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method MethodImplOverride1.IntImplementor::'IMultiGeneric<System.Int32,System.Int32>.Func(!0,!1)'

  .method private hidebysig newslot virtual final 
          instance string  'IMultiGeneric<System.Int32,System.Int32>.Func(!1,!0)'(int32 u1,
                                                                           int32 t2) cil managed
  {
    .override  method instance string class MethodImplOverride1.IMultiGeneric`2<int32,int32>::Func(!1,
                                                                               !0)
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "IntImplementor.IMultiGeneric<int, int>.Func(!1, !0)"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method MethodImplOverride1.IntImplementor::'IMultiGeneric<System.Int32,System.Int32>.Func(!1,!0)'

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [CoreTestAssembly]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method MethodImplOverride1.IntImplementor::.ctor

} // end of class MethodImplOverride1.IntImplementor

// Types for Test5
.class interface private abstract auto ansi MethodImplOverride1.ISingleGeneric`1<T>
{
  .method public hidebysig newslot abstract virtual 
          instance string  Method(!T t) cil managed
  {
  } // end of method MethodImplOverride1.ISingleGeneric`1::Method

} // end of class MethodImplOverride1.ISingleGeneric`1
