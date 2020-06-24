// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This test covers the scenario of testing overrides of generic methods
// 1. Test1 covers testing cases where a MethodImpl Decl could
//    resolve to 2 different methods and the exact signature is needed to represent
//    which method is overriden.
// 2. Test2 covers testing the substitution chain handling scenario.

// Metadata version: v4.0.30319
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly Program
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.

  // --- The following custom attribute is added automatically, do not uncomment -------
  //  .custom instance void [mscorlib]System.Diagnostics.DebuggableAttribute::.ctor(valuetype [mscorlib]System.Diagnostics.DebuggableAttribute/DebuggingModes) = ( 01 00 07 01 00 00 00 00 ) 

  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module Program.exe
// MVID: {C6C266E9-7949-4242-B429-F9011402D996}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x04F20000


// =============== CLASS MEMBERS DECLARATION ===================

.class private abstract auto ansi beforefieldinit Base`2<T,U>
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot virtual 
          instance string  Method([out] class [mscorlib]System.Collections.Generic.List`1<!U>& y,
                                class [mscorlib]System.Collections.Generic.List`1<!T>& x) cil managed
  {
    // Code size       16 (0x10)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  ldnull
    IL_0003:  stind.ref
    IL_0004:  ldstr      "Base<T, U>.Method(out List<U> y, ref List<T> x)"
    IL_000f:  ret
  } // end of method Base`2::Method

  .method public hidebysig newslot virtual 
          instance string  Method(class [mscorlib]System.Collections.Generic.List`1<!T>& x,
                                [out] class [mscorlib]System.Collections.Generic.List`1<!U>& y) cil managed
  {
    // Code size       16 (0x10)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.2
    IL_0002:  ldnull
    IL_0003:  stind.ref
    IL_0004:  ldstr      "Base<T, U>.Method(ref List<T> x, out List<U> y)"
    IL_000f:  ret
  } // end of method Base`2::Method

  .method public hidebysig newslot virtual 
          instance string  Method(class [mscorlib]System.Collections.Generic.List`1<!U>& x) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "Base<T, U>.Method(ref List<U> x)"
    IL_000c:  ret
  } // end of method Base`2::Method

  .method family hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method Base`2::.ctor

} // end of class Base`2

.class private auto ansi beforefieldinit Base2`2<A,B>
       extends class Base`2<!A,!B>
{
  .method public hidebysig newslot virtual 
          instance string  Method([out] class [mscorlib]System.Collections.Generic.List`1<!A>& x) cil managed
  {
    // Code size       16 (0x10)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  ldnull
    IL_0003:  stind.ref
    IL_0004:  ldstr      "Base2<A, B>.Method(out List<A> x)"
    IL_000f:  ret
  } // end of method Base2`2::Method

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void class Base`2<!A,!B>::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method Base2`2::.ctor

} // end of class Base2`2

.class private auto ansi beforefieldinit Derived
       extends class Base2`2<int32,int32>
{
  .method public hidebysig newslot virtual 
          instance string  Method(class [mscorlib]System.Collections.Generic.List`1<int32>& a,
                                [out] class [mscorlib]System.Collections.Generic.List`1<int32>& b) cil managed
  {
    .override  method instance string class Base`2<int32,int32>::Method(class [mscorlib]System.Collections.Generic.List`1<!0>&,
                                                                      class [mscorlib]System.Collections.Generic.List`1<!1>&)
    // Code size       16 (0x10)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.2
    IL_0002:  ldnull
    IL_0003:  stind.ref
    IL_0004:  ldstr      "Derived.Method(ref List<int> a, out List<int> b)"
    IL_000f:  ret
  } // end of method Derived::Method

  .method public hidebysig newslot virtual 
          instance string  Method(class [mscorlib]System.Collections.Generic.List`1<int32>& a) cil managed
  {
    .override  method instance string class Base`2<int32,int32>::Method(class [mscorlib]System.Collections.Generic.List`1<!1>&)
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "Derived.Method(ref List<int> a)"
    IL_000c:  ret
  } // end of method Derived::Method

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void class Base2`2<int32,int32>::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method Derived::.ctor

} // end of class Derived


// TEST2 classes
.class private auto ansi beforefieldinit BaseTestGenericsShape`4<A,B,C,D>
      extends [mscorlib]System.Object
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
  } // end of method BaseTestGenericsShape`4::Method

  .method public hidebysig newslot virtual 
          instance string  Method(!C c,
                                  !D d) cil managed
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
  } // end of method BaseTestGenericsShape`4::Method

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method BaseTestGenericsShape`4::.ctor

} // end of class BaseTestGenericsShape`4

.class private auto ansi beforefieldinit IntermediateGenericsShape`2<E,F>
      extends class BaseTestGenericsShape`4<object,string,!E,!F>
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void class BaseTestGenericsShape`4<object,string,!E,!F>::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method IntermediateGenericsShape`2::.ctor

} // end of class IntermediateGenericsShape`2

.class private auto ansi beforefieldinit DerivedGenericsShape`1<G>
      extends class IntermediateGenericsShape`2<int32,!G>
{
  .method public hidebysig virtual instance string 
          MethodOnDerived(int32 e,
                !G f) cil managed
  {
    .override  method instance string class IntermediateGenericsShape`2<int32,!G>::Method(!0, !1)
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "DerivedGenericsShape<G>.Method(int e, G f)"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method DerivedGenericsShape`1::Method

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void class IntermediateGenericsShape`2<int32,!G>::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method DerivedGenericsShape`1::.ctor

} // end of class DerivedGenericsShape`1

.class private auto ansi beforefieldinit DerivedGenericsShapeInvalidSubstitutedOverride
      extends class IntermediateGenericsShape`2<int32,int32>
{
  .method public hidebysig virtual instance string 
          MethodOnDerived(int32 e,
                int32 f) cil managed
  {
    .override  method instance string class  BaseTestGenericsShape`4<object,string, int32, int32>::Method(int32, int32)
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "DerivedGenericsShapeInvalidSubstitutedOverride.Method(int e, int f)"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method DerivedGenericsShapeInvalidSubstitutedOverride::Method

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void class IntermediateGenericsShape`2<int32,int32>::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method DerivedGenericsShapeInvalidSubstitutedOverride::.ctor

} // end of class DerivedGenericsShapeInvalidSubstitutedOverride

.class private auto ansi beforefieldinit Program
       extends [mscorlib]System.Object
{
  .method private hidebysig static void Assert(string result, string expected, int32& failCount)
  {
    ldarg.s failCount

    ldarg.s result
    ldarg.s expected
    ceq
    ldc.i4.0
    ceq
    ldarg.s failCount
    ldind.i4
    add

    stind.i4

    ldstr "Result = "
    call       void [mscorlib]System.Console::Write(string)
    ldarg.s result
    call       void [mscorlib]System.Console::Write(string)
    ldstr " Expected = "
    call       void [mscorlib]System.Console::Write(string)
    ldarg.s expected
    call       void [mscorlib]System.Console::Write(string)
    ldstr " failCount = "
    call       void [mscorlib]System.Console::Write(string)
    ldarg.s failCount
    ldind.i4
    box [mscorlib]System.Int32
    callvirt   instance string [mscorlib]System.Object::ToString()
    call       void [mscorlib]System.Console::WriteLine(string)
    ret
  }

  .method private hidebysig static int32  Test1() cil managed
  {
    // Code size       137 (0x89)
    .maxstack  3
    .locals init ([0] class [mscorlib]System.Collections.Generic.List`1<int32> t,
             [1] class Derived d,
             [2] class Base2`2<int32,int32> b2,
             [3] class Base`2<int32,int32> b1,
             [4] int32 failCount)
              ldstr "---"
              call       void [mscorlib]System.Console::WriteLine(string)
              ldstr "TEST1 - Test generics override of specific method where substitutions are necessary to compute the right target to override"
              call       void [mscorlib]System.Console::WriteLine(string)
              ldstr "---"
              call       void [mscorlib]System.Console::WriteLine(string)
    IL_0000:  nop
    IL_0001:  ldnull
    IL_0002:  stloc.0
    IL_0003:  newobj     instance void Derived::.ctor()
    IL_0008:  stloc.1
    IL_0009:  ldloc.1
    IL_000a:  ldloca.s   t
    IL_000c:  ldloca.s   t
    IL_000e:  callvirt   instance string class Base`2<int32,int32>::Method(class [mscorlib]System.Collections.Generic.List`1<!0>&,
                                                                         class [mscorlib]System.Collections.Generic.List`1<!1>&)
              ldstr "Derived.Method(ref List<int> a, out List<int> b)"
              ldloca.s   failCount
              call void Program::Assert(string, string, int32&)
    IL_0013:  nop
    IL_0014:  ldloc.1
    IL_0015:  ldloca.s   t
    IL_0017:  ldloca.s   t
    IL_0019:  callvirt   instance string class Base`2<int32,int32>::Method(class [mscorlib]System.Collections.Generic.List`1<!1>&,
                                                                         class [mscorlib]System.Collections.Generic.List`1<!0>&)
              ldstr "Base<T, U>.Method(out List<U> y, ref List<T> x)"
              ldloca.s   failCount
              call void Program::Assert(string, string, int32&)
    IL_001e:  nop
    IL_001f:  ldloc.1
    IL_0020:  ldloca.s   t
    IL_0022:  callvirt   instance string class Base`2<int32,int32>::Method(class [mscorlib]System.Collections.Generic.List`1<!1>&)
              ldstr "Derived.Method(ref List<int> a)"
              ldloca.s   failCount
              call void Program::Assert(string, string, int32&)
    IL_0027:  nop
    IL_0028:  ldloc.1
    IL_0029:  ldloca.s   t
    IL_002b:  callvirt   instance string class Base2`2<int32,int32>::Method(class [mscorlib]System.Collections.Generic.List`1<!0>&)
              ldstr "Base2<A, B>.Method(out List<A> x)"
              ldloca.s   failCount
              call void Program::Assert(string, string, int32&)
    IL_0030:  nop
    IL_0031:  call       void [mscorlib]System.Console::WriteLine()
    IL_0036:  nop
    IL_0037:  ldloc.1
    IL_0038:  stloc.2
    IL_0039:  ldloc.2
    IL_003a:  ldloca.s   t
    IL_003c:  ldloca.s   t
    IL_003e:  callvirt   instance string class Base`2<int32,int32>::Method(class [mscorlib]System.Collections.Generic.List`1<!0>&,
                                                                         class [mscorlib]System.Collections.Generic.List`1<!1>&)
              ldstr "Derived.Method(ref List<int> a, out List<int> b)"
              ldloca.s   failCount
              call void Program::Assert(string, string, int32&)
    IL_0043:  nop
    IL_0044:  ldloc.2
    IL_0045:  ldloca.s   t
    IL_0047:  ldloca.s   t
    IL_0049:  callvirt   instance string class Base`2<int32,int32>::Method(class [mscorlib]System.Collections.Generic.List`1<!1>&,
                                                                         class [mscorlib]System.Collections.Generic.List`1<!0>&)
              ldstr "Base<T, U>.Method(out List<U> y, ref List<T> x)"
              ldloca.s   failCount
              call void Program::Assert(string, string, int32&)
    IL_004e:  nop
    IL_004f:  ldloc.2
    IL_0050:  ldloca.s   t
    IL_0052:  callvirt   instance string class Base`2<int32,int32>::Method(class [mscorlib]System.Collections.Generic.List`1<!1>&)
              ldstr "Derived.Method(ref List<int> a)"
              ldloca.s   failCount
              call void Program::Assert(string, string, int32&)
    IL_0057:  nop
    IL_0058:  ldloc.2
    IL_0059:  ldloca.s   t
    IL_005b:  callvirt   instance string class Base2`2<int32,int32>::Method(class [mscorlib]System.Collections.Generic.List`1<!0>&)
              ldstr "Base2<A, B>.Method(out List<A> x)"
              ldloca.s   failCount
              call void Program::Assert(string, string, int32&)
    IL_0060:  nop
    IL_0061:  call       void [mscorlib]System.Console::WriteLine()
    IL_0066:  nop
    IL_0067:  ldloc.1
    IL_0068:  stloc.3
    IL_0069:  ldloc.3
    IL_006a:  ldloca.s   t
    IL_006c:  ldloca.s   t
    IL_006e:  callvirt   instance string class Base`2<int32,int32>::Method(class [mscorlib]System.Collections.Generic.List`1<!0>&,
                                                                         class [mscorlib]System.Collections.Generic.List`1<!1>&)
              ldstr "Derived.Method(ref List<int> a, out List<int> b)"
              ldloca.s   failCount
              call void Program::Assert(string, string, int32&)
    IL_0073:  nop
    IL_0074:  ldloc.3
    IL_0075:  ldloca.s   t
    IL_0077:  ldloca.s   t
    IL_0079:  callvirt   instance string class Base`2<int32,int32>::Method(class [mscorlib]System.Collections.Generic.List`1<!1>&,
                                                                         class [mscorlib]System.Collections.Generic.List`1<!0>&)
              ldstr "Base<T, U>.Method(out List<U> y, ref List<T> x)"
              ldloca.s   failCount
              call void Program::Assert(string, string, int32&)
    IL_007e:  nop
    IL_007f:  ldloc.3
    IL_0080:  ldloca.s   t
    IL_0082:  callvirt   instance string class Base`2<int32,int32>::Method(class [mscorlib]System.Collections.Generic.List`1<!1>&)
              ldstr "Derived.Method(ref List<int> a)"
              ldloca.s   failCount
              call void Program::Assert(string, string, int32&)
    IL_0087:  nop

            // Add failCount to 100. If there are failures, then failCount will be != 100
            ldc.i4 100
            ldloc.s failCount
            add
    IL_0088:  ret
  } // end of method Program::Test1

  .method private hidebysig static int32  Test2() cil managed
  {
    // Code size       137 (0x89)
    .maxstack  3
    .locals init ([0] class DerivedGenericsShape`1<float64> d,
                  [1] int32 failCount)
              ldstr "---"
              call       void [mscorlib]System.Console::WriteLine(string)
              ldstr "TEST2 - Test generics class override of method where method has been moved from the type which previously declared the type to its base type"
              call       void [mscorlib]System.Console::WriteLine(string)
              ldstr "---"
              call       void [mscorlib]System.Console::WriteLine(string)

    IL_0000:  nop
    IL_0003:  newobj     instance void class DerivedGenericsShape`1<float64>::.ctor()
    IL_0008:  stloc.0
    IL_0009:  ldloc.0
              ldc.i4.1
    IL_000a:  ldc.r8 1.0
    IL_000e:  callvirt   instance string class BaseTestGenericsShape`4<object,string,int32,float64>::Method(!0, !1)
              ldstr "BaseTestGenericsShape.Method(A a, B b)"
              ldloca.s   failCount
              call void Program::Assert(string, string, int32&)
    IL_0013:  nop

              ldloc.0
              ldc.i4.1
              ldc.r8 1.0
              callvirt   instance string class BaseTestGenericsShape`4<object,string,int32,float64>::Method(!2, !3)
              ldstr "DerivedGenericsShape<G>.Method(int e, G f)"
              ldloca.s   failCount
              call void Program::Assert(string, string, int32&)
              nop

            // Add failCount to 100. If there are failures, then failCount will be != 100
            ldc.i4 100
            ldloc.s failCount
            add
    IL_0088:  ret
  } // end of method Program::Test2

  .method private hidebysig static int32  Test3Internal() cil managed noinlining
  {
    // Code size       137 (0x89)
    .maxstack  3
    .locals init ([0] class DerivedGenericsShapeInvalidSubstitutedOverride d,
                  [1] int32 failCount)
    IL_0000:  nop
    IL_0003:  newobj     instance void class DerivedGenericsShapeInvalidSubstitutedOverride::.ctor()
    IL_0008:  stloc.0
    IL_0009:  ldloc.0
              ldc.i4.1
    IL_000a:  ldc.r8 1.0
    IL_000e:  callvirt   instance string class BaseTestGenericsShape`4<object,string,int32,float64>::Method(!0, !1)
              ldstr "BaseTestGenericsShape.Method(A a, B b)"
              ldloca.s   failCount
              call void Program::Assert(string, string, int32&)
    IL_0013:  nop

              ldloc.0
              ldc.i4.1
              ldc.r8 1.0
              callvirt   instance string class BaseTestGenericsShape`4<object,string,int32,float64>::Method(!2, !3)
              // This shouldn't actually match, but it did before a bug was fixed. Instead the MethodImpl involved should
              // fail with a MissingMethodException
              ldstr "DerivedGenericsShapeInvalidSubstitutedOverride.Method(int e, int f)"
              ldloca.s   failCount
              call void Program::Assert(string, string, int32&)
              nop

            // Add failCount to 100. If there are failures, then failCount will be != 100
            ldc.i4 100
            ldloc.s failCount
            add
    IL_0088:  ret
  } // end of method Program::Test3Internal

  .method private hidebysig static int32  Test3() cil managed
  {
    // Code size       22 (0x16)
    .maxstack  1
    .locals init (int32 V_0)
              ldstr "---"
              call       void [mscorlib]System.Console::WriteLine(string)
              ldstr "TEST3 - Test generics class override of method with signature that is pre-substituted"
              call       void [mscorlib]System.Console::WriteLine(string)
              ldstr "---"
              call       void [mscorlib]System.Console::WriteLine(string)

    IL_0000:  nop
    .try
    {
      IL_0001:  nop
      IL_0002:  call       int32 Program::Test3Internal()
      IL_0007:  pop
      ldstr "MissingMethodException not thrown. This is a failure."
      call       void [mscorlib]System.Console::WriteLine(string)
      IL_0008:  ldc.i4.s   101
      IL_000a:  stloc.0
      IL_000b:  leave.s    IL_0014
    }  // end .try
    catch [mscorlib]System.MissingMethodException 
    {
      IL_000d:  pop
      ldstr "Caught expected MissingMethodException"
      call       void [mscorlib]System.Console::WriteLine(string)
      IL_000e:  nop
      IL_000f:  ldc.i4.s   100
      IL_0011:  stloc.0
      IL_0012:  leave.s    IL_0014
    }  // end handler
    IL_0014:  ldloc.0
    IL_0015:  ret
  } // end of method Program::Test3


  .method private hidebysig static int32  Main() cil managed
  {
    .entrypoint
    // Code size       174 (0xae)
    .maxstack  3
    .locals init (int32 V_0,
            int32 V_1,
            bool V_2,
            class [mscorlib]System.Exception V_3,
            bool V_4,
            class [mscorlib]System.Exception V_5,
            bool V_6,
            class [mscorlib]System.Exception V_7,
            int32 V_8)
    IL_0000:  nop
    IL_0001:  ldc.i4.0
    IL_0002:  stloc.0
    .try
    {
      IL_0003:  nop
      IL_0004:  call       int32 Program::Test1()
      IL_0009:  stloc.1
      IL_000a:  ldloc.1
      IL_000b:  ldc.i4.s   100
      IL_000d:  ceq
      IL_000f:  ldc.i4.0
      IL_0010:  ceq
      IL_0012:  stloc.2
      IL_0013:  ldloc.2
      IL_0014:  brfalse.s  IL_001d
      IL_0016:  ldloc.0
      IL_0017:  ldloc.1
      IL_0018:  ldc.i4.s   100
      IL_001a:  sub
      IL_001b:  add
      IL_001c:  stloc.0
      IL_001d:  nop
      IL_001e:  leave.s    IL_0035
    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0020:  stloc.3
      IL_0021:  nop
      IL_0022:  ldloc.3
      IL_0023:  callvirt   instance string [mscorlib]System.Object::ToString()
      IL_0028:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_002d:  nop
      IL_002e:  ldloc.0
      IL_002f:  ldc.i4.1
      IL_0030:  add
      IL_0031:  stloc.0
      IL_0032:  nop
      IL_0033:  leave.s    IL_0035
    }  // end handler
    IL_0035:  nop
    .try
    {
      IL_0036:  nop
      IL_0037:  call       int32 Program::Test2()
      IL_003c:  stloc.1
      IL_003d:  ldloc.1
      IL_003e:  ldc.i4.s   100
      IL_0040:  ceq
      IL_0042:  ldc.i4.0
      IL_0043:  ceq
      IL_0045:  stloc.s    V_4
      IL_0047:  ldloc.s    V_4
      IL_0049:  brfalse.s  IL_0052
      IL_004b:  ldloc.0
      IL_004c:  ldloc.1
      IL_004d:  ldc.i4.s   100
      IL_004f:  sub
      IL_0050:  add
      IL_0051:  stloc.0
      IL_0052:  nop
      IL_0053:  leave.s    IL_006c
    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_0055:  stloc.s    V_5
      IL_0057:  nop
      IL_0058:  ldloc.s    V_5
      IL_005a:  callvirt   instance string [mscorlib]System.Object::ToString()
      IL_005f:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0064:  nop
      IL_0065:  ldloc.0
      IL_0066:  ldc.i4.1
      IL_0067:  add
      IL_0068:  stloc.0
      IL_0069:  nop
      IL_006a:  leave.s    IL_006c
    }  // end handler
    IL_006c:  nop
    .try
    {
      IL_006d:  nop
      IL_006e:  call       int32 Program::Test3()
      IL_0073:  stloc.1
      IL_0074:  ldloc.1
      IL_0075:  ldc.i4.s   100
      IL_0077:  ceq
      IL_0079:  ldc.i4.0
      IL_007a:  ceq
      IL_007c:  stloc.s    V_6
      IL_007e:  ldloc.s    V_6
      IL_0080:  brfalse.s  IL_0089
      IL_0082:  ldloc.0
      IL_0083:  ldloc.1
      IL_0084:  ldc.i4.s   100
      IL_0086:  sub
      IL_0087:  add
      IL_0088:  stloc.0
      IL_0089:  nop
      IL_008a:  leave.s    IL_00a3
    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_008c:  stloc.s    V_7
      IL_008e:  nop
      IL_008f:  ldloc.s    V_7
      IL_0091:  callvirt   instance string [mscorlib]System.Object::ToString()
      IL_0096:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_009b:  nop
      IL_009c:  ldloc.0
      IL_009d:  ldc.i4.1
      IL_009e:  add
      IL_009f:  stloc.0
      IL_00a0:  nop
      IL_00a1:  leave.s    IL_00a3
    }  // end handler
    IL_00a3:  ldloc.0
    IL_00a4:  ldc.i4.s   100
    IL_00a6:  add
    IL_00a7:  stloc.s    V_8
    IL_00a9:  br.s       IL_00ab
    IL_00ab:  ldloc.s    V_8
    IL_00ad:  ret
  } // end of method Program::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method Program::.ctor

} // end of class Program
