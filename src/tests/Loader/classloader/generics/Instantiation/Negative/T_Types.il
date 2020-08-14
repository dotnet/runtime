// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
// loading class Foo<T> and struct Y<T> with invalid types of T

.assembly extern mscorlib {}

.assembly T_Types {}

.class public sequential ansi sealed beforefieldinit A
       extends [mscorlib]System.ValueType
{
  .pack 0
  .size 1
} 

.class public auto ansi beforefieldinit B
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  8
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }  
}



.class public auto ansi beforefieldinit Foo`1<T>
       extends [mscorlib]System.Object
{

  .field public static !T t

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  8
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()

    ldstr "Inside ctor"
    call void [System.Console]System.Console::WriteLine(string)
    ret
  }  


   .method public hidebysig virtual instance string 
        ToString() cil managed
   {
    ldstr "Hello"
    
    ret
   } // end of method S::ToString

}


.class public sequential ansi sealed beforefieldinit Y`1<T>
       extends [mscorlib]System.ValueType
{
  .pack 0
  .size 1

.method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  8
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  } 
} 


.class public auto ansi beforefieldinit RunTest
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  8
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  } 

    // CLASS X<T>

  .method public static void  LoadClass_T_ByRef() cil managed
  { 
    .locals init (class Foo`1<class B&> V_0)
 
    newobj     instance void class  Foo`1<class B&>::.ctor()
    stloc.0
    ret
  } 

  .method public static void  LoadClass_T_TypedByRef() cil managed noinlining
  {
    .locals init (class Foo`1<valuetype [mscorlib]System.TypedReference> V_0)
 
    newobj     instance void class  Foo`1<valuetype [mscorlib]System.TypedReference>::.ctor()
    stloc.0
    ret
  } 

  .method public static void  LoadClass_T_VOID() cil managed noinlining
  {
    .locals init (class Foo`1<valuetype [mscorlib]System.Void> V_0)
 
    newobj     instance void class  Foo`1<valuetype [mscorlib]System.Void>::.ctor()
    stloc.0
    ret
  } 

  .method public static void  LoadClass_T_PTR() cil managed noinlining
  {
    //.locals init (class Foo`1<valuetype A*> V_0)
	.locals init (class Foo`1<int32*> V_0) 
 
   // newobj     instance void class  Foo`1<valuetype A*>::.ctor()
  	 newobj     instance void class  Foo`1<int32*>::.ctor()
    stloc.0
    ret
  }

  .method public static void  LoadClass_T_FNTPTR() cil managed noinlining
  {
    .locals init (class Foo`1<method void *()> V_0) 
 
    newobj     instance void class  Foo`1<method void *()>::.ctor()
    stloc.0
    ret
  }


  .method public static void  LoadClass_T_StackPtr() cil managed noinlining
  {
    .locals init (class Foo`1<valuetype [mscorlib]System.RuntimeArgumentHandle> V_0)
 
    newobj     instance void class Foo`1<valuetype [mscorlib]System.RuntimeArgumentHandle>::.ctor()
    stloc.0
    ret
  }

  .method public static void  LoadClass_T_ArgIterator() cil managed noinlining
  {
    .locals init (class Foo`1<valuetype [mscorlib]System.ArgIterator> V_0)
 
    newobj     instance void class Foo`1<valuetype [mscorlib]System.ArgIterator>::.ctor()
    stloc.0
    ret
  }


  .method public static void  LoadClass_T_ComObject() cil managed noinlining
  {
    .locals init (class Foo`1<class [mscorlib]System.__ComObject> V_0)
 
    newobj     instance void class Foo`1<class [mscorlib]System.__ComObject>::.ctor()
    stloc.0
    ret
  }

  .method public static void  LoadClass_T_Canon() cil managed noinlining
  {
    .locals init (class Foo`1<class [mscorlib]System.__Canon> V_0)
 
    newobj     instance void class Foo`1<class [mscorlib]System.__Canon>::.ctor()
    stloc.0
    ret
  }

  .method public static void  LoadClass_T_System_TransparentProxy() cil managed noinlining
  {
    .locals init (class Foo`1<class [mscorlib]System.Runtime.Remoting.Proxies.__TransparentProxy> V_0)
 
    newobj     instance void class Foo`1<class [mscorlib]System.Runtime.Remoting.Proxies.__TransparentProxy>::.ctor()
    stloc.0
    ret
  }





   // VALUETYPE Y<T>

  .method public static void  LoadVal_T_ByRef() cil managed
  {
    .locals init (valuetype Y`1<valuetype A&> V_0)
 
    newobj     instance void valuetype Y`1<valuetype A&>::.ctor()
    stloc.0
    ret
  } 

  .method public static void  LoadVal_T_TypedByRef() cil managed noinlining
  {
    .locals init (valuetype Y`1<valuetype [mscorlib]System.TypedReference> V_0)
 
    newobj     instance void valuetype Y`1<valuetype [mscorlib]System.TypedReference>::.ctor()
    stloc.0
    ret
  } 

  .method public static void  LoadVal_T_VOID() cil managed noinlining
  {
    .locals init (valuetype Y`1<valuetype [mscorlib]System.Void> V_0)
 
    newobj     instance void valuetype Y`1<valuetype [mscorlib]System.Void>::.ctor()
    stloc.0
    ret
  } 

  .method public static void  LoadVal_T_PTR() cil managed noinlining
  {
    .locals init (valuetype Y`1<valuetype A*> V_0)
 
    newobj     instance void valuetype Y`1<valuetype A*>::.ctor()
    stloc.0
    ret
  }

  .method public static void  LoadVal_T_FNTPTR() cil managed noinlining
  {
    .locals init (valuetype Y`1<method void *()> V_0) 
 
    newobj     instance void valuetype Y`1<method void *()>::.ctor()
    stloc.0
    ret
  }

  .method public static void  LoadVal_T_StackPtr() cil managed noinlining
  {
    .locals init (valuetype Y`1<valuetype [mscorlib]System.RuntimeArgumentHandle> V_0)
 
    newobj     instance void valuetype Y`1<valuetype [mscorlib]System.RuntimeArgumentHandle>::.ctor()
    stloc.0
    ret
  }


  .method public static void  LoadVal_T_ArgIterator() cil managed noinlining
  {
    .locals init (valuetype Y`1<valuetype [mscorlib]System.ArgIterator> V_0)
 
    newobj     instance void valuetype Y`1<valuetype [mscorlib]System.ArgIterator>::.ctor()
    stloc.0
    ret
  }


  .method public static void  LoadVal_T_ComObject() cil managed noinlining
  {
    .locals init (valuetype Y`1<class [mscorlib]System.__ComObject> V_0)
 
    newobj     instance void valuetype Y`1<class [mscorlib]System.__ComObject>::.ctor()
    stloc.0
    ret
  }



  .method public static void  LoadVal_T_Canon() cil managed noinlining
  {
    .locals init (valuetype Y`1<class [mscorlib]System.__Canon> V_0)
 
    newobj     instance void valuetype Y`1<class [mscorlib]System.__Canon>::.ctor()
    stloc.0
    ret
  }

  .method public static void  LoadVal_T_System_TransparentProxy() cil managed noinlining
  {
    .locals init (valuetype Y`1<class [mscorlib]System.Runtime.Remoting.Proxies.__TransparentProxy> V_0)
 
    newobj     instance void valuetype Y`1<class [mscorlib]System.Runtime.Remoting.Proxies.__TransparentProxy>::.ctor()
    stloc.0
    ret
  }


}
