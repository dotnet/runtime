// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern mscorlib {}
.assembly TestCase0 {}


// =============== CLASS MEMBERS DECLARATION ===================
.class private auto ansi beforefieldinit A`1<U> 
		implements class A`1/I`1<!U>
{
  .class interface nested family abstract auto ansi I`1<T>
  { 
	  .method public hidebysig newslot abstract virtual instance string  Foo() cil managed {} 
	  .class interface nested public abstract auto ansi J
	  { 
	    .method public hidebysig newslot abstract virtual instance string  Bar1() cil managed {}
	    .method public hidebysig newslot abstract virtual instance string  Bar2() cil managed {}
	  }
  }
  .method public hidebysig newslot virtual instance string  Foo() cil managed
  {
    ldstr      "A::Foo"
    ret
  }
  .method public hidebysig newslot virtual instance string  Bar1() cil managed
  {
    .maxstack  8
    ldstr      "A::Bar1"
    ret
  }
  .method public hidebysig newslot virtual instance string  Bar2() cil managed
  {
    .maxstack  8
    ldstr      "A::Bar2"
    ret
  }
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
    ret
  }
}

.class private auto ansi beforefieldinit B`2<V,W>
       extends class A`1<!V>
       implements class A`1/I`1<!W>, A`1/I`1/J
{
  .method public hidebysig newslot virtual instance string  Foo() cil managed
  {
    ldstr      "B::Foo"
    ret
  }
  .method public hidebysig newslot virtual instance string  Bar1() cil managed
  {
    .maxstack  8
    ldstr      "B::Bar1"
    ret
  }
  .method public hidebysig newslot virtual instance string  Bar2() cil managed
  {
    .maxstack  8
    ldstr      "B::Bar2"
    ret
  }
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed
  {
    ret
  }
}

.class private auto ansi beforefieldinit C extends class B`2<class C,class C>
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed
  {
    ret
  } 
}

.class public auto ansi beforefieldinit Test extends [mscorlib]System.Object
{
  .method public hidebysig static int32  Main() cil managed
  {
    .entrypoint

    newobj     instance void C::.ctor()
	callvirt   instance string class C::Foo()
    ldstr      "B::Foo"
    call       bool [mscorlib]System.String::op_Inequality(string, string)
    brtrue     FAILURE

    newobj     instance void C::.ctor()
    callvirt   instance string class C::Bar1()
    ldstr      "B::Bar1"
    call       bool [mscorlib]System.String::op_Inequality(string,string)
    brtrue     FAILURE    
	
	newobj     instance void C::.ctor()
    callvirt   instance string class C::Bar2()
    ldstr      "B::Bar2"
    call       bool [mscorlib]System.String::op_Inequality(string,string)
    brtrue     FAILURE
	
	
PASS:
    ldstr      "Pass"
    call       void [mscorlib]System.Console::WriteLine(string)
    ldc.i4.s   100
    ret

FAILURE:
    ldstr      "Failed!"
    call       void [mscorlib]System.Console::WriteLine(string)
    ldc.i4.m1
    ret
  }
}
