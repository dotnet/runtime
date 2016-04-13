// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console { }
// See test descriptions below

.assembly extern mscorlib {}

.assembly RunTestNeg2 {}


.class public auto ansi beforefieldinit A
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

.class public sequential ansi sealed beforefieldinit S
       extends [mscorlib]System.ValueType
       implements I
{
  .pack 0
  .size 1
} 




.class interface public abstract auto ansi I {} 



.class private auto ansi beforefieldinit GClass`1<class T>
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

.class public auto ansi beforefieldinit GStruct`1<valuetype T>
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


.class public auto ansi beforefieldinit GNew`1<.ctor T>
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

// ============================================================


.class private auto ansi beforefieldinit G1`1<valuetype P>
       extends class GClass`1<!P>
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


.class private auto ansi beforefieldinit G2`1<(I) P>
       extends class GClass`1<!P>
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

.class private auto ansi beforefieldinit G3`1<([mscorlib]System.Object) P>
       extends class GClass`1<!P>
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

.class private auto ansi beforefieldinit G4`1<([mscorlib]System.ValueType) P>
       extends class GClass`1<!P>
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


.class private auto ansi beforefieldinit G5`1<([mscorlib]System.Enum) P>
       extends class GClass`1<!P>
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

  // ============================================================


.class private auto ansi beforefieldinit G6`1<class P>
       extends class GStruct`1<!P>
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

.class private auto ansi beforefieldinit G7`1<(A) P>
       extends class GStruct`1<!P>
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


.class private auto ansi beforefieldinit G8`1<(I) P>
       extends class GStruct`1<!P>
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

.class private auto ansi beforefieldinit G12`1<(System.ValueType) P>
       extends class GStruct`1<!P>
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

.class private auto ansi beforefieldinit G13`1<(valuetype [mscorlib]System.'Nullable`1'<int32>) P>
       extends class GStruct`1<!P>
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


  // ============================================================

.class public auto ansi beforefieldinit G9`1<(A) P>
       extends class GNew`1<!P>
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

.class public auto ansi beforefieldinit G10`1<class P>
       extends class GNew`1<!P>
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

.class public auto ansi beforefieldinit G11`1<(I) P>
       extends class GNew`1<!P>
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

  // ============================================================

.class public auto ansi beforefieldinit RunTest
       extends [mscorlib]System.Object
{

  // TEST 1
  // NEGATIVE
  // T has 'class' constraint BUT
  // P has 'struct' constraint
  // use a class as type param
  .method public hidebysig static void  Test1() cil managed
  {
  
    .maxstack  1
    newobj     instance void class G1`1<class A>::.ctor()
    pop
    ret
  } 

  // =====================================================================

  // TEST 2
  // NEGATIVE
  // T has 'class' constraint BUT
  // P has 'struct' constraint
  // use a struct as type param
  .method public hidebysig static void  Test2() cil managed
  {
  
    .maxstack  1
    newobj     instance void class G1`1<valuetype S>::.ctor()
    pop
    ret
  } 

  // =====================================================================

  // TEST 3
  // NEGATIVE
  // T has 'class' constraint BUT
  // P has interface-type constraint
  // This is a secondary constraint - this tells us nothing about the eventual instantiation that
  // we can use here.

  .method public hidebysig static void  Test3() cil managed
  {
  
    .maxstack  1
    newobj     instance void class G2`1<class I>::.ctor()
    pop
    ret
  } 


  // =====================================================================

  // TEST 4
  // NEGATIVE
  // T has 'class' constraint BUT
  // P has System.Object constraint
   .method public hidebysig static void  Test4() cil managed
  {
  
    .maxstack  1
    newobj     instance void class G3`1<object>::.ctor()
    pop
    ret
  } 


  // =====================================================================

  // TEST 5
  // NEGATIVE
  // T has 'class' constraint BUT
  // P has System.ValueType constraint
   .method public hidebysig static void  Test5() cil managed
  {
  
    .maxstack  1
    newobj     instance void class G4`1<class [mscorlib]System.ValueType>::.ctor()
    pop
    ret
  }


  // =====================================================================

  // TEST 6
  // NEGATIVE
  // T has 'class' constraint BUT
  // P has System.Enum constraint
   .method public hidebysig static void  Test6() cil managed
  {
  
    .maxstack  1
    newobj     instance void class G5`1<class [mscorlib]System.Enum>::.ctor()
    pop
    ret
  }

  // =====================================================================

  // TEST 7
  // NEGATIVE
  // T has 'struct' constraint BUT
  // P has 'class' constraint
  // instantiated with a class
   .method public hidebysig static void  Test7() cil managed
  {
  
    .maxstack  1
    newobj     instance void class G6`1<class A>::.ctor()
    pop
    ret
  }

  // =====================================================================

  // TEST 8
  // NEGATIVE
  // T has 'struct' constraint BUT
  // P has 'class' constraint
  // instantiated with a struct
   .method public hidebysig static void  Test8() cil managed
  {
  
    .maxstack  1
    newobj     instance void class G6`1<valuetype S>::.ctor()
    pop
    ret
  }

  // =====================================================================

  // TEST 9
  // NEGATIVE
  // T has 'struct' constraint BUT
  // P has 'class-type' constraint
   .method public hidebysig static void  Test9() cil managed
  {
  
    .maxstack  1
    newobj     instance void class G7`1<class A>::.ctor()
    pop
    ret
  }

  // =====================================================================

  // TEST 10
  // NEGATIVE
  // T has 'struct' constraint BUT
  // P has 'interface-type' constraint
   .method public hidebysig static void  Test10() cil managed
  {
  
    .maxstack  1
    newobj     instance void class G8`1<class I>::.ctor()
    pop
    ret
  }

  // =====================================================================

  // TEST 11
  // NEGATIVE
  // T has 'new' constraint BUT
  // P has 'class-type' constraint
  // Since constraints are not inherited this is a negative test because P doesn't have new constraint
  // and since not all reference types have default ctor we do not accept this constraint.
   .method public hidebysig static void  Test11() cil managed
  {
  
    .maxstack  1
    newobj     instance void class G9`1<class A>::.ctor()
    pop
    ret
  }

  // =====================================================================

  // TEST 12
  // NEGATIVE
  // T has 'new' constraint BUT
  // P has 'class' constraint
   .method public hidebysig static void  Test12() cil managed
  {
  
    .maxstack  1
    newobj     instance void class G10`1<class A>::.ctor()
    pop
    ret
  }


 // =====================================================================

  // TEST 13
  // NEGATIVE
  // T has 'new' constraint BUT
  // P has 'interface-type' constraint
   .method public hidebysig static void  Test13() cil managed
  {
  
    .maxstack  1
    newobj     instance void class G11`1<class I>::.ctor()
    pop
    ret
  }

 // =====================================================================

  // TEST 14
  // NEGATIVE
  // T has 'struct' constraint BUT
  // P has 'System.ValueType' constraint
   .method public hidebysig static void  Test14() cil managed
  {
  
    .maxstack  1
    newobj     instance void class G12`1<class [mscorlib]System.ValueType>::.ctor()
    pop
    ret
  }

 // =====================================================================

  // TEST 15
  // NEGATIVE
  // T has 'struct'Nullable<int>' constraint
   .method public hidebysig static void  Test15() cil managed
  {
  
    .maxstack  1
    newobj     instance void class G12`1<valuetype [mscorlib]System.'Nullable`1'<int32>>::.ctor()
    pop
    ret
  }

} 

