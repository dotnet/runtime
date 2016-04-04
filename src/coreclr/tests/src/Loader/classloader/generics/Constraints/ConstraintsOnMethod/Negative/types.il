// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern mscorlib{}

.assembly Types {}

// class with public nullary ctor
.class public auto ansi beforefieldinit ClassWithCtor
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

// generic class with public nullary ctor
.class public auto ansi beforefieldinit GenClassWithCtor<T>
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


// class without public nullary ctor
.class public auto ansi beforefieldinit ClassNoCtor
       extends [mscorlib]System.Object
{
} 

// generic class without public nullary ctor
.class public auto ansi beforefieldinit GenClassNoCtor<T>
       extends [mscorlib]System.Object
{
} 


// struct (valuetypes have public nullary ctors by default)
.class public sequential ansi sealed beforefieldinit NonGenStruct
       extends [mscorlib]System.ValueType
{
  .pack 0
  .size 1
} 



// generic struct (valuetypes have public nullary ctors by default)
.class public sequential ansi sealed beforefieldinit GenStruct<T>
       extends [mscorlib]System.ValueType
{
  .pack 0
  .size 1
}

// interface without public nullary ctor
.class public abstract interface auto ansi beforefieldinit NonGenInterface
{
} 

// interface without public nullary ctor
.class public abstract interface auto ansi beforefieldinit GenInterface<T>
{
} 


.class public auto ansi sealed Enum1
       extends [mscorlib]System.Enum
{
  .field public specialname rtspecialname int32 value__
  .field public static literal valuetype Enum1 One = int32(0x00000000)
}


// doesn't have public parameterless constructor
.class public auto ansi sealed Delegate1
       extends [mscorlib]System.MulticastDelegate
{

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(object 'object',
                               native int 'method') runtime managed
  {
  }

  .method public hidebysig newslot virtual 
          instance void  Invoke() runtime managed
  {
  } 

  .method public hidebysig newslot virtual 
          instance class [mscorlib]System.IAsyncResult 
          BeginInvoke(class [mscorlib]System.AsyncCallback callback,
                      object 'object') runtime managed
  {
  } 

  .method public hidebysig newslot virtual 
          instance void  EndInvoke(class [mscorlib]System.IAsyncResult result) runtime managed
  {
  } 
}
