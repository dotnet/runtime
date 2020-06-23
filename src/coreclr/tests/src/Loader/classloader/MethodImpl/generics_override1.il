// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


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

  .method private hidebysig static int32  Main() cil managed
  {
    .entrypoint
    // Code size       137 (0x89)
    .maxstack  3
    .locals init ([0] class [mscorlib]System.Collections.Generic.List`1<int32> t,
             [1] class Derived d,
             [2] class Base2`2<int32,int32> b2,
             [3] class Base`2<int32,int32> b1,
             [4] int32 failCount)
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
