// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// While this is all expressible in C#, we need a deterministic IL to test the disassembler, hence
// this is in IL.

.class private sequential ansi sealed beforefieldinit ILDisassembler.TestStruct
       extends [CoreTestAssembly]System.ValueType
{
  .pack 0
  .size 1
} // end of class ILDisassembler.TestStruct

.class private auto ansi beforefieldinit ILDisassembler.TestClass
       extends [CoreTestAssembly]System.Object
{
  .field public static valuetype ILDisassembler.TestStruct somefield
  .method public hidebysig static void  NonGenericMethod() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    ret
  } // end of method TestClass::NonGenericMethod

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    ldarg.0
    call       instance void [CoreTestAssembly]System.Object::.ctor()
    ret
  } // end of method TestClass::.ctor

} // end of class ILDisassembler.TestClass

.class private auto ansi beforefieldinit ILDisassembler.TestGenericClass`1<TClassParam>
       extends [CoreTestAssembly]System.Object
{
  .class auto ansi nested public beforefieldinit Nested<TClassParam>
         extends [CoreTestAssembly]System.Object
  {
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      ldarg.0
      call       instance void [CoreTestAssembly]System.Object::.ctor()
      ret
    } // end of method Nested::.ctor

  } // end of class Nested

  .field private !TClassParam somefield
  .field private class ILDisassembler.TestClass otherfield
  .field private class ILDisassembler.TestGenericClass`1<class ILDisassembler.TestGenericClass`1<class ILDisassembler.TestClass>> genericfield
  .field private !TClassParam[] arrayfield
  .method public hidebysig instance void 
          VoidGenericMethod<TMethodParam1,MethodParam2>(!!TMethodParam1 p,
                                                        int32 x,
                                                        native int y,
                                                        class ILDisassembler.TestClass& z) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    ret
  } // end of method TestGenericClass`1::VoidGenericMethod

  .method public hidebysig static !!TMethodParam 
          MethodParamGenericMethod<TMethodParam>(class ILDisassembler.TestGenericClass`1<!!TMethodParam> x,
                                                 class ILDisassembler.TestGenericClass`1/Nested<!TClassParam> y,
                                                 valuetype ILDisassembler.TestStruct*[] z,
                                                 !TClassParam w) cil managed
  {
    // Code size       10 (0xa)
    .maxstack  1
    .locals init ([0] !!TMethodParam V_0)
    ldloca.s   V_0
    initobj    !!TMethodParam
    ldloc.0
    ret
  } // end of method TestGenericClass`1::MethodParamGenericMethod

  .method public hidebysig instance valuetype ILDisassembler.TestStruct 
          NonGenericMethod(float64 x,
                           float32 y,
                           int16 z) cil managed
  {
    // Code size       10 (0xa)
    .maxstack  1
    .locals init ([0] valuetype ILDisassembler.TestStruct V_0)
    ldloca.s   V_0
    initobj    ILDisassembler.TestStruct
    ldloc.0
    ret
  } // end of method TestGenericClass`1::NonGenericMethod

  .method public hidebysig instance void 
          TestMethod() cil managed
  {
    // Code size       162 (0xa2)
    .maxstack  5
    .locals init ([0] class ILDisassembler.TestClass t,
             [1] !TClassParam V_1)
    ldnull
    stloc.0
    ldarg.0
    ldstr      "Hello \"World\"!\n"
    ldc.i4.0
    ldc.i4.0
    conv.i
    ldloca.s   t
    call       instance void class ILDisassembler.TestGenericClass`1<!TClassParam>::VoidGenericMethod<string,valuetype ILDisassembler.TestStruct>(!!0,
                                                                                                                                                 int32,
                                                                                                                                                 native int,
                                                                                                                                                 class ILDisassembler.TestClass&)
    ldnull
    ldnull
    ldnull
    ldloca.s   V_1
    initobj    !TClassParam
    ldloc.1
    call       !!0 class ILDisassembler.TestGenericClass`1<!TClassParam>::MethodParamGenericMethod<class ILDisassembler.TestClass>(class ILDisassembler.TestGenericClass`1<!!0>,
                                                                                                                                  class ILDisassembler.TestGenericClass`1/Nested<!0>,
                                                                                                                                  valuetype ILDisassembler.TestStruct*[],
                                                                                                                                  !0)
    pop
    ldnull
    ldnull
    ldnull
    ldloca.s   V_1
    initobj    !TClassParam
    ldloc.1
    call       !!0 class ILDisassembler.TestGenericClass`1<!TClassParam>::MethodParamGenericMethod<!0>(class ILDisassembler.TestGenericClass`1<!!0>,
                                                                                                      class ILDisassembler.TestGenericClass`1/Nested<!0>,
                                                                                                      valuetype ILDisassembler.TestStruct*[],
                                                                                                      !0)
    pop
    ldtoken    !TClassParam
    pop
    ldtoken    valuetype [CoreTestAssembly]System.Nullable`1<int32>
    pop
    ldarg.0
    ldc.r8     3.1400000000000001
    ldc.r4     1.6799999
    ldc.i4.s   42
    call       instance valuetype ILDisassembler.TestStruct class ILDisassembler.TestGenericClass`1<!TClassParam>::NonGenericMethod(float64,
                                                                                                                                   float32,
                                                                                                                                   int16)
    pop
    ldarg.0
    ldflda     !0 class ILDisassembler.TestGenericClass`1<!TClassParam>::somefield
    initobj    !TClassParam
    ldarg.0
    ldnull
    stfld      class ILDisassembler.TestClass class ILDisassembler.TestGenericClass`1<!TClassParam>::otherfield
    ldarg.0
    ldnull
    stfld      class ILDisassembler.TestGenericClass`1<class ILDisassembler.TestGenericClass`1<class ILDisassembler.TestClass>> class ILDisassembler.TestGenericClass`1<!TClassParam>::genericfield
    ldarg.0
    ldnull
    stfld      !0[] class ILDisassembler.TestGenericClass`1<!TClassParam>::arrayfield
    call       void ILDisassembler.TestClass::NonGenericMethod()
    ldsflda    valuetype ILDisassembler.TestStruct ILDisassembler.TestClass::somefield
    initobj    ILDisassembler.TestStruct
    ret
  } // end of method TestGenericClass`1::TestMethod

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    ldarg.0
    call       instance void [CoreTestAssembly]System.Object::.ctor()
    ret
  } // end of method TestGenericClass`1::.ctor

} // end of class ILDisassembler.TestGenericClass`1
