// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// See more_tailcalls.cs for the source of this IL.


// Metadata version: v4.0.30319
.assembly extern System.Runtime
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )                         // .?_....:
  .ver 5:0:0:0
}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )                         // .?_....:
  .ver 5:0:0:0
}
.assembly more_tailcalls
{
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                                   63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.

  // --- The following custom attribute is added automatically, do not uncomment -------
  //  .custom instance void [System.Runtime]System.Diagnostics.DebuggableAttribute::.ctor(valuetype [System.Runtime]System.Diagnostics.DebuggableAttribute/DebuggingModes) = ( 01 00 02 00 00 00 00 00 ) 

  .custom instance void [System.Runtime]System.Runtime.Versioning.TargetFrameworkAttribute::.ctor(string) = ( 01 00 18 2E 4E 45 54 43 6F 72 65 41 70 70 2C 56   // ....NETCoreApp,V
                                                                                                              65 72 73 69 6F 6E 3D 76 35 2E 30 01 00 54 0E 14   // ersion=v5.0..T..
                                                                                                              46 72 61 6D 65 77 6F 72 6B 44 69 73 70 6C 61 79   // FrameworkDisplay
                                                                                                              4E 61 6D 65 00 )                                  // Name.
  .custom instance void [System.Runtime]System.Reflection.AssemblyCompanyAttribute::.ctor(string) = ( 01 00 0E 6D 6F 72 65 5F 74 61 69 6C 63 61 6C 6C   // ...more_tailcall
                                                                                                      73 00 00 )                                        // s..
  .custom instance void [System.Runtime]System.Reflection.AssemblyConfigurationAttribute::.ctor(string) = ( 01 00 07 52 65 6C 65 61 73 65 00 00 )             // ...Release..
  .custom instance void [System.Runtime]System.Reflection.AssemblyFileVersionAttribute::.ctor(string) = ( 01 00 07 31 2E 30 2E 30 2E 30 00 00 )             // ...1.0.0.0..
  .custom instance void [System.Runtime]System.Reflection.AssemblyInformationalVersionAttribute::.ctor(string) = ( 01 00 05 31 2E 30 2E 30 00 00 )                   // ...1.0.0..
  .custom instance void [System.Runtime]System.Reflection.AssemblyProductAttribute::.ctor(string) = ( 01 00 0E 6D 6F 72 65 5F 74 61 69 6C 63 61 6C 6C   // ...more_tailcall
                                                                                                      73 00 00 )                                        // s..
  .custom instance void [System.Runtime]System.Reflection.AssemblyTitleAttribute::.ctor(string) = ( 01 00 0E 6D 6F 72 65 5F 74 61 69 6C 63 61 6C 6C   // ...more_tailcall
                                                                                                    73 00 00 )                                        // s..
  .permissionset reqmin
             = {class 'System.Security.Permissions.SecurityPermissionAttribute, System.Runtime, Version=5.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a' = {property bool 'SkipVerification' = bool(true)}}
  .hash algorithm 0x00008004
  .ver 1:0:0:0
}
.module more_tailcalls.dll
// MVID: {2945C9C4-EED0-49EA-8A87-27475136401B}
.custom instance void [System.Runtime]System.Security.UnverifiableCodeAttribute::.ctor() = ( 01 00 00 00 ) 
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x00000273609B0000


// =============== CLASS MEMBERS DECLARATION ===================

.class private sequential ansi sealed beforefieldinit S16
       extends [System.Runtime]System.ValueType
{
  .field public int64 A
  .field public int64 B
  .method public hidebysig virtual instance string 
          ToString() cil managed
  {
    // Code size       33 (0x21)
    .maxstack  8
    IL_0000:  ldstr      "{0}, {1}"
    IL_0005:  ldarg.0
    IL_0006:  ldfld      int64 S16::A
    IL_000b:  box        [System.Runtime]System.Int64
    IL_0010:  ldarg.0
    IL_0011:  ldfld      int64 S16::B
    IL_0016:  box        [System.Runtime]System.Int64
    IL_001b:  call       string [System.Runtime]System.String::Format(string,
                                                                      object,
                                                                      object)
    IL_0020:  ret
  } // end of method S16::ToString

  .method public hidebysig instance string 
          InstanceMethod() cil managed noinlining
  {
    // Code size       6 (0x6)
    .maxstack  8
    IL_0000:  ldstr      "Instance method"
    IL_0005:  ret
  } // end of method S16::InstanceMethod

} // end of class S16

.class private sequential ansi sealed beforefieldinit S32
       extends [System.Runtime]System.ValueType
{
  .field public int64 A
  .field public int64 B
  .field public int64 C
  .field public int64 D
  .method public hidebysig virtual instance string 
          ToString() cil managed
  {
    // Code size       73 (0x49)
    .maxstack  5
    IL_0000:  ldstr      "{0}, {1}, {2}, {3}"
    IL_0005:  ldc.i4.4
    IL_0006:  newarr     [System.Runtime]System.Object
    IL_000b:  dup
    IL_000c:  ldc.i4.0
    IL_000d:  ldarg.0
    IL_000e:  ldfld      int64 S32::A
    IL_0013:  box        [System.Runtime]System.Int64
    IL_0018:  stelem.ref
    IL_0019:  dup
    IL_001a:  ldc.i4.1
    IL_001b:  ldarg.0
    IL_001c:  ldfld      int64 S32::B
    IL_0021:  box        [System.Runtime]System.Int64
    IL_0026:  stelem.ref
    IL_0027:  dup
    IL_0028:  ldc.i4.2
    IL_0029:  ldarg.0
    IL_002a:  ldfld      int64 S32::C
    IL_002f:  box        [System.Runtime]System.Int64
    IL_0034:  stelem.ref
    IL_0035:  dup
    IL_0036:  ldc.i4.3
    IL_0037:  ldarg.0
    IL_0038:  ldfld      int64 S32::D
    IL_003d:  box        [System.Runtime]System.Int64
    IL_0042:  stelem.ref
    IL_0043:  call       string [System.Runtime]System.String::Format(string,
                                                                      object[])
    IL_0048:  ret
  } // end of method S32::ToString

} // end of class S32

.class private sequential ansi sealed beforefieldinit SGC
       extends [System.Runtime]System.ValueType
{
  .field public object A
  .field public object B
  .field public string C
  .field public string D
  .method public hidebysig virtual instance string 
          ToString() cil managed
  {
    // Code size       53 (0x35)
    .maxstack  8
    IL_0000:  ldstr      "{0}, {1}, {2}, {3}"
    IL_0005:  ldc.i4.4
    IL_0006:  newarr     [System.Runtime]System.Object
    IL_000b:  dup
    IL_000c:  ldc.i4.0
    IL_000d:  ldarg.0
    IL_000e:  ldfld      object SGC::A
    IL_0013:  stelem.ref
    IL_0014:  dup
    IL_0015:  ldc.i4.1
    IL_0016:  ldarg.0
    IL_0017:  ldfld      object SGC::B
    IL_001c:  stelem.ref
    IL_001d:  dup
    IL_001e:  ldc.i4.2
    IL_001f:  ldarg.0
    IL_0020:  ldfld      string SGC::C
    IL_0025:  stelem.ref
    IL_0026:  dup
    IL_0027:  ldc.i4.3
    IL_0028:  ldarg.0
    IL_0029:  ldfld      string SGC::D
    IL_002e:  stelem.ref
    IL_002f:  call       string [System.Runtime]System.String::Format(string,
                                                                      object[])
    IL_0034:  ret
  } // end of method SGC::ToString

} // end of class SGC

.class private sequential ansi sealed beforefieldinit SGC2
       extends [System.Runtime]System.ValueType
{
  .field public int32 A
  .field public valuetype SGC B
  .field public object C
  .field public int32 D
  .method public hidebysig virtual instance string 
          ToString() cil managed
  {
    // Code size       68 (0x44)
    .maxstack  5
    IL_0000:  ldstr      "{0}, ({1}), {2}, {3}"
    IL_0005:  ldc.i4.4
    IL_0006:  newarr     [System.Runtime]System.Object
    IL_000b:  dup
    IL_000c:  ldc.i4.0
    IL_000d:  ldarg.0
    IL_000e:  ldfld      int32 SGC2::A
    IL_0013:  box        [System.Runtime]System.Int32
    IL_0018:  stelem.ref
    IL_0019:  dup
    IL_001a:  ldc.i4.1
    IL_001b:  ldarg.0
    IL_001c:  ldfld      valuetype SGC SGC2::B
    IL_0021:  box        SGC
    IL_0026:  stelem.ref
    IL_0027:  dup
    IL_0028:  ldc.i4.2
    IL_0029:  ldarg.0
    IL_002a:  ldfld      object SGC2::C
    IL_002f:  stelem.ref
    IL_0030:  dup
    IL_0031:  ldc.i4.3
    IL_0032:  ldarg.0
    IL_0033:  ldfld      int32 SGC2::D
    IL_0038:  box        [System.Runtime]System.Int32
    IL_003d:  stelem.ref
    IL_003e:  call       string [System.Runtime]System.String::Format(string,
                                                                      object[])
    IL_0043:  ret
  } // end of method SGC2::ToString

} // end of class SGC2

.class private auto ansi beforefieldinit HeapInt
       extends [System.Runtime]System.Object
{
  .field public int32 Value
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(int32 val) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  ldarg.0
    IL_0007:  ldarg.1
    IL_0008:  stfld      int32 HeapInt::Value
    IL_000d:  ret
  } // end of method HeapInt::.ctor

  .method public hidebysig virtual instance string 
          ToString() cil managed
  {
    // Code size       22 (0x16)
    .maxstack  8
    IL_0000:  ldstr      "{0}"
    IL_0005:  ldarg.0
    IL_0006:  ldfld      int32 HeapInt::Value
    IL_000b:  box        [System.Runtime]System.Int32
    IL_0010:  call       string [System.Runtime]System.String::Format(string,
                                                                      object)
    IL_0015:  ret
  } // end of method HeapInt::ToString

} // end of class HeapInt

.class private auto ansi Program
       extends [System.Runtime]System.Object
{
  .class auto ansi sealed nested private beforefieldinit '<>c__DisplayClass7_0'
         extends [System.Runtime]System.Object
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    .field public bool result
    .field public int32 ten
    .field public class GenInstance`2<string,int32> g
    .field public class IGenInterface`2<string,int32> ig
    .field public class IGenInterface`2<string,object> ig2
    .field public class GenAbstractImpl`1<string> ga1
    .field public class GenAbstractImpl`1<int32> ga2
    .field public int32[] a
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
      IL_0006:  ret
    } // end of method '<>c__DisplayClass7_0'::.ctor

    .method assembly hidebysig instance void 
            '<Main>g__Test|0'<T>(class [System.Runtime]System.Func`1<!!T> f,
                                 !!T expected,
                                 string name) cil managed
    {
      // Code size       105 (0x69)
      .maxstack  4
      .locals init (class [System.Runtime]System.Diagnostics.Stopwatch V_0,
               !!T V_1)
      IL_0000:  ldstr      "{0}: "
      IL_0005:  ldarg.3
      IL_0006:  call       void [System.Console]System.Console::Write(string,
                                                                      object)
      IL_000b:  call       class [System.Runtime]System.Diagnostics.Stopwatch [System.Runtime]System.Diagnostics.Stopwatch::StartNew()
      IL_0010:  stloc.0
      IL_0011:  ldarg.1
      IL_0012:  callvirt   instance !0 class [System.Runtime]System.Func`1<!!T>::Invoke()
      IL_0017:  stloc.1
      IL_0018:  ldloc.0
      IL_0019:  callvirt   instance void [System.Runtime]System.Diagnostics.Stopwatch::Stop()
      IL_001e:  ldloca.s   V_1
      IL_0020:  ldarg.2
      IL_0021:  box        !!T
      IL_0026:  constrained. !!T
      IL_002c:  callvirt   instance bool [System.Runtime]System.Object::Equals(object)
      IL_0031:  brfalse.s  IL_004a

      IL_0033:  ldstr      "OK in {1} ms"
      IL_0038:  ldarg.3
      IL_0039:  ldloc.0
      IL_003a:  callvirt   instance int64 [System.Runtime]System.Diagnostics.Stopwatch::get_ElapsedMilliseconds()
      IL_003f:  box        [System.Runtime]System.Int64
      IL_0044:  call       void [System.Console]System.Console::WriteLine(string,
                                                                          object,
                                                                          object)
      IL_0049:  ret

      IL_004a:  ldstr      "FAIL (expected {1}, got {2})"
      IL_004f:  ldarg.3
      IL_0050:  ldarg.2
      IL_0051:  box        !!T
      IL_0056:  ldloc.1
      IL_0057:  box        !!T
      IL_005c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                          object,
                                                                          object,
                                                                          object)
      IL_0061:  ldarg.0
      IL_0062:  ldc.i4.0
      IL_0063:  stfld      bool Program/'<>c__DisplayClass7_0'::result
      IL_0068:  ret
    } // end of method '<>c__DisplayClass7_0'::'<Main>g__Test|0'

    .method assembly hidebysig instance void 
            '<Main>g__TestCalc|1'<T>(class [System.Runtime]System.Func`3<int32,int32,!!T> f,
                                     !!T expected,
                                     string name) cil managed
    {
      // Code size       34 (0x22)
      .maxstack  4
      .locals init (class Program/'<>c__DisplayClass7_1`1'<!!T> V_0)
      IL_0000:  newobj     instance void class Program/'<>c__DisplayClass7_1`1'<!!T>::.ctor()
      IL_0005:  stloc.0
      IL_0006:  ldloc.0
      IL_0007:  ldarg.1
      IL_0008:  stfld      class [System.Runtime]System.Func`3<int32,int32,!0> class Program/'<>c__DisplayClass7_1`1'<!!T>::f
      IL_000d:  ldarg.0
      IL_000e:  ldloc.0
      IL_000f:  ldftn      instance !0 class Program/'<>c__DisplayClass7_1`1'<!!T>::'<Main>b__30'()
      IL_0015:  newobj     instance void class [System.Runtime]System.Func`1<!!T>::.ctor(object,
                                                                                         native int)
      IL_001a:  ldarg.2
      IL_001b:  ldarg.3
      IL_001c:  call       instance void Program/'<>c__DisplayClass7_0'::'<Main>g__Test|0'<!!0>(class [System.Runtime]System.Func`1<!!0>,
                                                                                                !!0,
                                                                                                string)
      IL_0021:  ret
    } // end of method '<>c__DisplayClass7_0'::'<Main>g__TestCalc|1'

    .method assembly hidebysig instance string 
            '<Main>b__7'() cil managed
    {
      // Code size       51 (0x33)
      .maxstack  10
      IL_0000:  newobj     instance void Instance::.ctor()
      IL_0005:  ldstr      "2"
      IL_000a:  ldc.i4.3
      IL_000b:  ldstr      "4"
      IL_0010:  ldc.i4.5
      IL_0011:  box        [System.Runtime]System.Int32
      IL_0016:  ldstr      "6"
      IL_001b:  ldstr      "7"
      IL_0020:  ldstr      "8"
      IL_0025:  ldc.i4.s   9
      IL_0027:  ldarg.0
      IL_0028:  ldflda     int32 Program/'<>c__DisplayClass7_0'::ten
      IL_002d:  call       instance string Instance::GC(object,
                                                        int32,
                                                        object,
                                                        object,
                                                        string,
                                                        string,
                                                        object,
                                                        int32,
                                                        int32&)
      IL_0032:  ret
    } // end of method '<>c__DisplayClass7_0'::'<Main>b__7'

    .method assembly hidebysig instance string 
            '<Main>b__19'() cil managed
    {
      // Code size       28 (0x1c)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldfld      class GenInstance`2<string,int32> Program/'<>c__DisplayClass7_0'::g
      IL_0006:  ldstr      "a"
      IL_000b:  ldc.i4.5
      IL_000c:  ldstr      "b"
      IL_0011:  ldstr      "c"
      IL_0016:  callvirt   instance string class GenInstance`2<string,int32>::NonVirtForward<object,string>(!0,
                                                                                                            !1,
                                                                                                            !!0,
                                                                                                            !!1)
      IL_001b:  ret
    } // end of method '<>c__DisplayClass7_0'::'<Main>b__19'

    .method assembly hidebysig instance string 
            '<Main>b__20'() cil managed
    {
      // Code size       28 (0x1c)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldfld      class GenInstance`2<string,int32> Program/'<>c__DisplayClass7_0'::g
      IL_0006:  ldstr      "a"
      IL_000b:  ldc.i4.5
      IL_000c:  ldstr      "b"
      IL_0011:  ldstr      "c"
      IL_0016:  callvirt   instance string class GenInstance`2<string,int32>::VirtForward<object,string>(!0,
                                                                                                         !1,
                                                                                                         !!0,
                                                                                                         !!1)
      IL_001b:  ret
    } // end of method '<>c__DisplayClass7_0'::'<Main>b__20'

    .method assembly hidebysig instance string 
            '<Main>b__21'() cil managed
    {
      // Code size       28 (0x1c)
      .maxstack  8
      IL_0000:  ldstr      "a"
      IL_0005:  ldc.i4.5
      IL_0006:  ldstr      "c"
      IL_000b:  ldstr      "d"
      IL_0010:  ldarg.0
      IL_0011:  ldfld      class IGenInterface`2<string,int32> Program/'<>c__DisplayClass7_0'::ig
      IL_0016:  call       string Program::GenInterfaceForwardF<string,int32,string,object>(!!0,
                                                                                            !!1,
                                                                                            !!2,
                                                                                            !!3,
                                                                                            class IGenInterface`2<!!0,!!1>)
      IL_001b:  ret
    } // end of method '<>c__DisplayClass7_0'::'<Main>b__21'

    .method assembly hidebysig instance string 
            '<Main>b__22'() cil managed
    {
      // Code size       18 (0x12)
      .maxstack  8
      IL_0000:  ldstr      "a"
      IL_0005:  ldc.i4.5
      IL_0006:  ldarg.0
      IL_0007:  ldfld      class IGenInterface`2<string,int32> Program/'<>c__DisplayClass7_0'::ig
      IL_000c:  call       string Program::GenInterfaceForwardG<string,int32>(!!0,
                                                                              !!1,
                                                                              class IGenInterface`2<!!0,!!1>)
      IL_0011:  ret
    } // end of method '<>c__DisplayClass7_0'::'<Main>b__22'

    .method assembly hidebysig instance string 
            '<Main>b__23'() cil managed
    {
      // Code size       28 (0x1c)
      .maxstack  8
      IL_0000:  ldstr      "a"
      IL_0005:  ldstr      "b"
      IL_000a:  ldc.i4.5
      IL_000b:  ldstr      "d"
      IL_0010:  ldarg.0
      IL_0011:  ldfld      class IGenInterface`2<string,object> Program/'<>c__DisplayClass7_0'::ig2
      IL_0016:  call       string Program::GenInterfaceForwardNone(string,
                                                                   object,
                                                                   int32,
                                                                   object,
                                                                   class IGenInterface`2<string,object>)
      IL_001b:  ret
    } // end of method '<>c__DisplayClass7_0'::'<Main>b__23'

    .method assembly hidebysig instance string 
            '<Main>b__24'() cil managed
    {
      // Code size       22 (0x16)
      .maxstack  8
      IL_0000:  ldstr      "a"
      IL_0005:  ldstr      "b"
      IL_000a:  ldarg.0
      IL_000b:  ldfld      class IGenInterface`2<string,object> Program/'<>c__DisplayClass7_0'::ig2
      IL_0010:  call       string Program::GenInterfaceForward2(string,
                                                                object,
                                                                class IGenInterface`2<string,object>)
      IL_0015:  ret
    } // end of method '<>c__DisplayClass7_0'::'<Main>b__24'

    .method assembly hidebysig instance string 
            '<Main>b__25'() cil managed
    {
      // Code size       12 (0xc)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldfld      class GenAbstractImpl`1<string> Program/'<>c__DisplayClass7_0'::ga1
      IL_0006:  call       string Program::GenAbstractFString(class GenAbstract`1<string>)
      IL_000b:  ret
    } // end of method '<>c__DisplayClass7_0'::'<Main>b__25'

    .method assembly hidebysig instance string 
            '<Main>b__26'() cil managed
    {
      // Code size       12 (0xc)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldfld      class GenAbstractImpl`1<int32> Program/'<>c__DisplayClass7_0'::ga2
      IL_0006:  call       string Program::GenAbstractFInt(class GenAbstract`1<int32>)
      IL_000b:  ret
    } // end of method '<>c__DisplayClass7_0'::'<Main>b__26'

    .method assembly hidebysig instance string 
            '<Main>b__27'() cil managed
    {
      // Code size       12 (0xc)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldfld      class GenAbstractImpl`1<string> Program/'<>c__DisplayClass7_0'::ga1
      IL_0006:  call       string Program::GenAbstractGString(class GenAbstract`1<string>)
      IL_000b:  ret
    } // end of method '<>c__DisplayClass7_0'::'<Main>b__27'

    .method assembly hidebysig instance string 
            '<Main>b__28'() cil managed
    {
      // Code size       12 (0xc)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldfld      class GenAbstractImpl`1<int32> Program/'<>c__DisplayClass7_0'::ga2
      IL_0006:  call       string Program::GenAbstractGInt(class GenAbstract`1<int32>)
      IL_000b:  ret
    } // end of method '<>c__DisplayClass7_0'::'<Main>b__28'

    .method assembly hidebysig instance int32 
            '<Main>b__29'() cil managed
    {
      // Code size       24 (0x18)
      .maxstack  8
      IL_0000:  ldc.i4.0
      IL_0001:  ldc.i4.0
      IL_0002:  ldstr      "string"
      IL_0007:  ldarg.0
      IL_0008:  ldfld      int32[] Program/'<>c__DisplayClass7_0'::a
      IL_000d:  call       valuetype [System.Runtime]System.Span`1<!0> valuetype [System.Runtime]System.Span`1<int32>::op_Implicit(!0[])
      IL_0012:  call       int32 Program::InstantiatingStub1<string>(int32,
                                                                     int32,
                                                                     !!0,
                                                                     valuetype [System.Runtime]System.Span`1<int32>)
      IL_0017:  ret
    } // end of method '<>c__DisplayClass7_0'::'<Main>b__29'

  } // end of class '<>c__DisplayClass7_0'

  .class auto ansi sealed nested private beforefieldinit '<>c__DisplayClass7_1`1'<T>
         extends [System.Runtime]System.Object
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    .field public class [System.Runtime]System.Func`3<int32,int32,!T> f
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
      IL_0006:  ret
    } // end of method '<>c__DisplayClass7_1`1'::.ctor

    .method assembly hidebysig instance !T 
            '<Main>b__30'() cil managed
    {
      // Code size       18 (0x12)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldfld      class [System.Runtime]System.Func`3<int32,int32,!0> class Program/'<>c__DisplayClass7_1`1'<!T>::f
      IL_0006:  ldc.i4     0xf4240
      IL_000b:  ldc.i4.0
      IL_000c:  callvirt   instance !2 class [System.Runtime]System.Func`3<int32,int32,!T>::Invoke(!0,
                                                                                                   !1)
      IL_0011:  ret
    } // end of method '<>c__DisplayClass7_1`1'::'<Main>b__30'

  } // end of class '<>c__DisplayClass7_1`1'

  .class auto ansi serializable sealed nested private beforefieldinit '<>c'
         extends [System.Runtime]System.Object
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    .field public static initonly class Program/'<>c' '<>9'
    .field public static class [System.Runtime]System.Func`3<int32,int32,int32> '<>9__7_2'
    .field public static class [System.Runtime]System.Func`1<string> '<>9__7_3'
    .field public static class [System.Runtime]System.Func`1<string> '<>9__7_4'
    .field public static class [System.Runtime]System.Func`1<string> '<>9__7_5'
    .field public static class [System.Runtime]System.Func`1<int32> '<>9__7_6'
    .field public static class [System.Runtime]System.Func`1<int32> '<>9__7_8'
    .field public static class [System.Runtime]System.Func`1<int32> '<>9__7_9'
    .field public static class [System.Runtime]System.Func`1<string> '<>9__7_10'
    .field public static class [System.Runtime]System.Func`1<string> '<>9__7_11'
    .field public static class [System.Runtime]System.Func`1<string> '<>9__7_12'
    .field public static class [System.Runtime]System.Func`1<string> '<>9__7_13'
    .field public static class [System.Runtime]System.Func`1<string> '<>9__7_14'
    .field public static class [System.Runtime]System.Func`1<string> '<>9__7_15'
    .field public static class [System.Runtime]System.Func`1<string> '<>9__7_16'
    .field public static class [System.Runtime]System.Func`1<string> '<>9__7_17'
    .field public static class [System.Runtime]System.Func`1<string> '<>9__7_18'
    .method private hidebysig specialname rtspecialname static 
            void  .cctor() cil managed
    {
      // Code size       11 (0xb)
      .maxstack  8
      IL_0000:  newobj     instance void Program/'<>c'::.ctor()
      IL_0005:  stsfld     class Program/'<>c' Program/'<>c'::'<>9'
      IL_000a:  ret
    } // end of method '<>c'::.cctor

    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
      IL_0006:  ret
    } // end of method '<>c'::.ctor

    .method assembly hidebysig instance int32 
            '<Main>b__7_2'(int32 x,
                           int32 s) cil managed
    {
      // Code size       13 (0xd)
      .maxstack  8
      IL_0000:  ldarg.1
      IL_0001:  ldarg.2
      IL_0002:  call       void Program::CalcStaticVoid(int32,
                                                        int32)
      IL_0007:  ldsfld     int32 Program::s_result
      IL_000c:  ret
    } // end of method '<>c'::'<Main>b__7_2'

    .method assembly hidebysig instance string 
            '<Main>b__7_3'() cil managed
    {
      // Code size       6 (0x6)
      .maxstack  8
      IL_0000:  call       string Program::EmptyCalli()
      IL_0005:  ret
    } // end of method '<>c'::'<Main>b__7_3'

    .method assembly hidebysig instance string 
            '<Main>b__7_4'() cil managed
    {
      // Code size       6 (0x6)
      .maxstack  8
      IL_0000:  call       string Program::ValueTypeInstanceMethodCalli()
      IL_0005:  ret
    } // end of method '<>c'::'<Main>b__7_4'

    .method assembly hidebysig instance string 
            '<Main>b__7_5'() cil managed
    {
      // Code size       6 (0x6)
      .maxstack  8
      IL_0000:  call       string Program::ValueTypeExplicitThisInstanceMethodCalli()
      IL_0005:  ret
    } // end of method '<>c'::'<Main>b__7_5'

    .method assembly hidebysig instance int32 
            '<Main>b__7_6'() cil managed
    {
      // Code size       27 (0x1b)
      .maxstack  2
      .locals init (valuetype InstanceValueType V_0)
      IL_0000:  ldloca.s   V_0
      IL_0002:  initobj    InstanceValueType
      IL_0008:  ldloca.s   V_0
      IL_000a:  ldc.i4     0xf4240
      IL_000f:  call       instance void InstanceValueType::CountUp(int32)
      IL_0014:  ldloc.0
      IL_0015:  ldfld      int32 InstanceValueType::Count
      IL_001a:  ret
    } // end of method '<>c'::'<Main>b__7_6'

    .method assembly hidebysig instance int32 
            '<Main>b__7_8'() cil managed
    {
      // Code size       17 (0x11)
      .maxstack  8
      IL_0000:  ldc.i4     0xf4240
      IL_0005:  ldc.i4.0
      IL_0006:  newobj     instance void HeapInt::.ctor(int32)
      IL_000b:  call       int32 Program::CountUpHeap(int32,
                                                      class HeapInt)
      IL_0010:  ret
    } // end of method '<>c'::'<Main>b__7_8'

    .method assembly hidebysig instance int32 
            '<Main>b__7_9'() cil managed
    {
      // Code size       28 (0x1c)
      .maxstack  3
      .locals init (int32[] V_0)
      IL_0000:  ldc.i4.1
      IL_0001:  newarr     [System.Runtime]System.Int32
      IL_0006:  stloc.0
      IL_0007:  ldc.i4     0xf4240
      IL_000c:  ldloc.0
      IL_000d:  ldc.i4.0
      IL_000e:  ldelema    [System.Runtime]System.Int32
      IL_0013:  call       void Program::CountUpRef(int32,
                                                    int32&)
      IL_0018:  ldloc.0
      IL_0019:  ldc.i4.0
      IL_001a:  ldelem.i4
      IL_001b:  ret
    } // end of method '<>c'::'<Main>b__7_9'

    .method assembly hidebysig instance string 
            '<Main>b__7_10'() cil managed
    {
      // Code size       11 (0xb)
      .maxstack  8
      IL_0000:  ldstr      "hello"
      IL_0005:  call       string Program::GenName1Forward<string>(!!0)
      IL_000a:  ret
    } // end of method '<>c'::'<Main>b__7_10'

    .method assembly hidebysig instance string 
            '<Main>b__7_11'() cil managed
    {
      // Code size       11 (0xb)
      .maxstack  8
      IL_0000:  ldstr      "hello"
      IL_0005:  call       string Program::GenName1Forward<object>(!!0)
      IL_000a:  ret
    } // end of method '<>c'::'<Main>b__7_11'

    .method assembly hidebysig instance string 
            '<Main>b__7_12'() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldc.i4.5
      IL_0001:  call       string Program::GenName1Forward<int32>(!!0)
      IL_0006:  ret
    } // end of method '<>c'::'<Main>b__7_12'

    .method assembly hidebysig instance string 
            '<Main>b__7_13'() cil managed
    {
      // Code size       16 (0x10)
      .maxstack  8
      IL_0000:  ldstr      "hello"
      IL_0005:  ldstr      "hello2"
      IL_000a:  call       string Program::GenName2ForwardBoth<string,object>(!!0,
                                                                              !!1)
      IL_000f:  ret
    } // end of method '<>c'::'<Main>b__7_13'

    .method assembly hidebysig instance string 
            '<Main>b__7_14'() cil managed
    {
      // Code size       12 (0xc)
      .maxstack  8
      IL_0000:  ldstr      "hello"
      IL_0005:  ldc.i4.5
      IL_0006:  call       string Program::GenName2ForwardBoth<string,int32>(!!0,
                                                                             !!1)
      IL_000b:  ret
    } // end of method '<>c'::'<Main>b__7_14'

    .method assembly hidebysig instance string 
            '<Main>b__7_15'() cil managed
    {
      // Code size       16 (0x10)
      .maxstack  8
      IL_0000:  ldstr      "hello"
      IL_0005:  ldstr      "hello2"
      IL_000a:  call       string Program::GenName2ForwardOne<string>(!!0,
                                                                      string)
      IL_000f:  ret
    } // end of method '<>c'::'<Main>b__7_15'

    .method assembly hidebysig instance string 
            '<Main>b__7_16'() cil managed
    {
      // Code size       16 (0x10)
      .maxstack  8
      IL_0000:  ldstr      "hello"
      IL_0005:  ldstr      "hello2"
      IL_000a:  call       string Program::GenName2ForwardOne<object>(!!0,
                                                                      string)
      IL_000f:  ret
    } // end of method '<>c'::'<Main>b__7_16'

    .method assembly hidebysig instance string 
            '<Main>b__7_17'() cil managed
    {
      // Code size       12 (0xc)
      .maxstack  8
      IL_0000:  ldc.i4.5
      IL_0001:  ldstr      "hello2"
      IL_0006:  call       string Program::GenName2ForwardOne<int32>(!!0,
                                                                     string)
      IL_000b:  ret
    } // end of method '<>c'::'<Main>b__7_17'

    .method assembly hidebysig instance string 
            '<Main>b__7_18'() cil managed
    {
      // Code size       16 (0x10)
      .maxstack  8
      IL_0000:  ldstr      "hello"
      IL_0005:  ldstr      "hello2"
      IL_000a:  call       string Program::GenName2ForwardNone(object,
                                                               string)
      IL_000f:  ret
    } // end of method '<>c'::'<Main>b__7_18'

  } // end of class '<>c'

  .field private static initonly native int s_calcStaticCalli
  .field private static initonly native int s_calcStaticCalliOther
  .field private static initonly native int s_calcStaticCalliRetbuf
  .field private static initonly native int s_calcStaticCalliRetbufOther
  .field private static initonly native int s_emptyCalliOther
  .field private static initonly native int s_instanceMethodOnValueType
  .field static assembly int32 s_result
  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       83 (0x53)
    .maxstack  1
    .locals init (native int V_0,
             native int V_1,
             native int V_2,
             native int V_3,
             native int V_4,
             native int V_5)
    IL_0000:  ldftn      int32 Program::CalcStaticCalli(int32,
                                                        int32)
    IL_0006:  stloc.0
    IL_0007:  ldloc.0
    IL_0008:  stsfld     native int Program::s_calcStaticCalli
    IL_000d:  ldftn      int32 Program::CalcStaticCalliOther(int32,
                                                             valuetype S32,
                                                             int32)
    IL_0013:  stloc.1
    IL_0014:  ldloc.1
    IL_0015:  stsfld     native int Program::s_calcStaticCalliOther
    IL_001a:  ldftn      valuetype S32 Program::CalcStaticCalliRetbuf(int32,
                                                                      int32)
    IL_0020:  stloc.2
    IL_0021:  ldloc.2
    IL_0022:  stsfld     native int Program::s_calcStaticCalliRetbuf
    IL_0027:  ldftn      valuetype S32 Program::CalcStaticCalliRetbufOther(int32,
                                                                           valuetype S32,
                                                                           int32)
    IL_002d:  stloc.3
    IL_002e:  ldloc.3
    IL_002f:  stsfld     native int Program::s_calcStaticCalliRetbufOther
    IL_0034:  ldftn      string Program::EmptyCalliOther()
    IL_003a:  stloc.s    V_4
    IL_003c:  ldloc.s    V_4
    IL_003e:  stsfld     native int Program::s_emptyCalliOther
    IL_0043:  ldftn      instance string S16::InstanceMethod()
    IL_0049:  stloc.s    V_5
    IL_004b:  ldloc.s    V_5
    IL_004d:  stsfld     native int Program::s_instanceMethodOnValueType
    IL_0052:  ret
  } // end of method Program::.cctor

  .method private hidebysig static int32 
          Main() cil managed
  {
    .entrypoint
    // Code size       1792 (0x700)
    .maxstack  4
    .locals init (class Program/'<>c__DisplayClass7_0' V_0,
             int32 V_1,
             valuetype S32 V_2,
             int32 V_3,
             class ClassImpl V_4,
             class InterfaceImpl V_5,
             int64 V_6,
             valuetype S16 V_7,
             valuetype S32 V_8,
             valuetype S16 V_9,
             valuetype S32 V_10)
    IL_0000:  newobj     instance void Program/'<>c__DisplayClass7_0'::.ctor()
    IL_0005:  stloc.0
    IL_0006:  ldc.i4     0xf4240
    IL_000b:  stloc.1
    IL_000c:  ldloca.s   V_2
    IL_000e:  initobj    S32
    IL_0014:  ldc.i4.0
    IL_0015:  stloc.3
    IL_0016:  br.s       IL_0030

    IL_0018:  ldloc.1
    IL_0019:  ldc.i4.2
    IL_001a:  rem
    IL_001b:  brtrue.s   IL_0025

    IL_001d:  ldloca.s   V_2
    IL_001f:  initobj    S32
    IL_0025:  ldloca.s   V_1
    IL_0027:  ldloca.s   V_2
    IL_0029:  ldloca.s   V_3
    IL_002b:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_0030:  ldloc.1
    IL_0031:  brtrue.s   IL_0018

    IL_0033:  ldloc.0
    IL_0034:  ldc.i4.1
    IL_0035:  stfld      bool Program/'<>c__DisplayClass7_0'::result
    IL_003a:  newobj     instance void ClassImpl::.ctor()
    IL_003f:  stloc.s    V_4
    IL_0041:  ldloc.s    V_4
    IL_0043:  ldloc.s    V_4
    IL_0045:  callvirt   instance void ClassImpl::set_Other(class BaseClass)
    IL_004a:  newobj     instance void InterfaceImpl::.ctor()
    IL_004f:  stloc.s    V_5
    IL_0051:  ldloc.s    V_5
    IL_0053:  ldloc.s    V_5
    IL_0055:  callvirt   instance void InterfaceImpl::set_Other(class IInterface)
    IL_005a:  ldloc.0
    IL_005b:  newobj     instance void class GenInstance`2<string,int32>::.ctor()
    IL_0060:  stfld      class GenInstance`2<string,int32> Program/'<>c__DisplayClass7_0'::g
    IL_0065:  ldloc.0
    IL_0066:  newobj     instance void class GenInterfaceImpl`2<string,int32>::.ctor()
    IL_006b:  stfld      class IGenInterface`2<string,int32> Program/'<>c__DisplayClass7_0'::ig
    IL_0070:  ldloc.0
    IL_0071:  newobj     instance void class GenInterfaceImpl`2<string,object>::.ctor()
    IL_0076:  stfld      class IGenInterface`2<string,object> Program/'<>c__DisplayClass7_0'::ig2
    IL_007b:  ldloc.0
    IL_007c:  newobj     instance void class GenAbstractImpl`1<string>::.ctor()
    IL_0081:  stfld      class GenAbstractImpl`1<string> Program/'<>c__DisplayClass7_0'::ga1
    IL_0086:  ldloc.0
    IL_0087:  newobj     instance void class GenAbstractImpl`1<int32>::.ctor()
    IL_008c:  stfld      class GenAbstractImpl`1<int32> Program/'<>c__DisplayClass7_0'::ga2
    IL_0091:  ldloc.3
    IL_0092:  conv.u8
    IL_0093:  ldc.i4.s   32
    IL_0095:  shl
    IL_0096:  ldloc.3
    IL_0097:  conv.u8
    IL_0098:  or
    IL_0099:  stloc.s    V_6
    IL_009b:  ldloca.s   V_9
    IL_009d:  initobj    S16
    IL_00a3:  ldloca.s   V_9
    IL_00a5:  ldloc.3
    IL_00a6:  conv.i8
    IL_00a7:  stfld      int64 S16::A
    IL_00ac:  ldloca.s   V_9
    IL_00ae:  ldloc.3
    IL_00af:  conv.i8
    IL_00b0:  stfld      int64 S16::B
    IL_00b5:  ldloc.s    V_9
    IL_00b7:  stloc.s    V_7
    IL_00b9:  ldloca.s   V_10
    IL_00bb:  initobj    S32
    IL_00c1:  ldloca.s   V_10
    IL_00c3:  ldloc.3
    IL_00c4:  conv.i8
    IL_00c5:  stfld      int64 S32::A
    IL_00ca:  ldloca.s   V_10
    IL_00cc:  ldloc.3
    IL_00cd:  conv.i8
    IL_00ce:  stfld      int64 S32::B
    IL_00d3:  ldloca.s   V_10
    IL_00d5:  ldloc.3
    IL_00d6:  conv.i8
    IL_00d7:  stfld      int64 S32::C
    IL_00dc:  ldloca.s   V_10
    IL_00de:  ldloc.3
    IL_00df:  conv.i8
    IL_00e0:  stfld      int64 S32::D
    IL_00e5:  ldloc.s    V_10
    IL_00e7:  stloc.s    V_8
    IL_00e9:  ldloc.0
    IL_00ea:  ldc.i4.s   10
    IL_00ec:  stfld      int32 Program/'<>c__DisplayClass7_0'::ten
    IL_00f1:  ldloc.0
    IL_00f2:  ldnull
    IL_00f3:  ldftn      int32 Program::CalcStatic(int32,
                                                   int32)
    IL_00f9:  newobj     instance void class [System.Runtime]System.Func`3<int32,int32,int32>::.ctor(object,
                                                                                                     native int)
    IL_00fe:  ldloc.3
    IL_00ff:  ldstr      "Static non-generic"
    IL_0104:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__TestCalc|1'<int32>(class [System.Runtime]System.Func`3<int32,int32,!!0>,
                                                                                                    !!0,
                                                                                                    string)
    IL_0109:  ldloc.0
    IL_010a:  ldnull
    IL_010b:  ldftn      uint8 Program::CalcStaticSmall(int32,
                                                        int32)
    IL_0111:  newobj     instance void class [System.Runtime]System.Func`3<int32,int32,uint8>::.ctor(object,
                                                                                                     native int)
    IL_0116:  ldloc.3
    IL_0117:  conv.u1
    IL_0118:  ldstr      "Static non-generic small"
    IL_011d:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__TestCalc|1'<uint8>(class [System.Runtime]System.Func`3<int32,int32,!!0>,
                                                                                                    !!0,
                                                                                                    string)
    IL_0122:  ldloc.0
    IL_0123:  ldnull
    IL_0124:  ldftn      valuetype S32 Program::CalcStaticRetbuf(int32,
                                                                 int32)
    IL_012a:  newobj     instance void class [System.Runtime]System.Func`3<int32,int32,valuetype S32>::.ctor(object,
                                                                                                             native int)
    IL_012f:  ldloc.s    V_8
    IL_0131:  ldstr      "Static non-generic retbuf"
    IL_0136:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__TestCalc|1'<valuetype S32>(class [System.Runtime]System.Func`3<int32,int32,!!0>,
                                                                                                            !!0,
                                                                                                            string)
    IL_013b:  ldloc.0
    IL_013c:  ldnull
    IL_013d:  ldftn      int64 Program::CalcStaticLong(int32,
                                                       int32)
    IL_0143:  newobj     instance void class [System.Runtime]System.Func`3<int32,int32,int64>::.ctor(object,
                                                                                                     native int)
    IL_0148:  ldloc.s    V_6
    IL_014a:  ldstr      "Static non-generic long"
    IL_014f:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__TestCalc|1'<int64>(class [System.Runtime]System.Func`3<int32,int32,!!0>,
                                                                                                    !!0,
                                                                                                    string)
    IL_0154:  ldloc.0
    IL_0155:  ldnull
    IL_0156:  ldftn      valuetype S16 Program::CalcStaticS16(int32,
                                                              int32)
    IL_015c:  newobj     instance void class [System.Runtime]System.Func`3<int32,int32,valuetype S16>::.ctor(object,
                                                                                                             native int)
    IL_0161:  ldloc.s    V_7
    IL_0163:  ldstr      "Static non-generic S16"
    IL_0168:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__TestCalc|1'<valuetype S16>(class [System.Runtime]System.Func`3<int32,int32,!!0>,
                                                                                                            !!0,
                                                                                                            string)
    IL_016d:  ldloc.0
    IL_016e:  ldsfld     class [System.Runtime]System.Func`3<int32,int32,int32> Program/'<>c'::'<>9__7_2'
    IL_0173:  dup
    IL_0174:  brtrue.s   IL_018d

    IL_0176:  pop
    IL_0177:  ldsfld     class Program/'<>c' Program/'<>c'::'<>9'
    IL_017c:  ldftn      instance int32 Program/'<>c'::'<Main>b__7_2'(int32,
                                                                      int32)
    IL_0182:  newobj     instance void class [System.Runtime]System.Func`3<int32,int32,int32>::.ctor(object,
                                                                                                     native int)
    IL_0187:  dup
    IL_0188:  stsfld     class [System.Runtime]System.Func`3<int32,int32,int32> Program/'<>c'::'<>9__7_2'
    IL_018d:  ldloc.3
    IL_018e:  ldstr      "Static void"
    IL_0193:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__TestCalc|1'<int32>(class [System.Runtime]System.Func`3<int32,int32,!!0>,
                                                                                                    !!0,
                                                                                                    string)
    IL_0198:  ldloc.0
    IL_0199:  newobj     instance void Instance::.ctor()
    IL_019e:  ldftn      instance int32 Instance::CalcInstance(int32,
                                                               int32)
    IL_01a4:  newobj     instance void class [System.Runtime]System.Func`3<int32,int32,int32>::.ctor(object,
                                                                                                     native int)
    IL_01a9:  ldloc.3
    IL_01aa:  ldstr      "Instance non-generic"
    IL_01af:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__TestCalc|1'<int32>(class [System.Runtime]System.Func`3<int32,int32,!!0>,
                                                                                                    !!0,
                                                                                                    string)
    IL_01b4:  ldloc.0
    IL_01b5:  newobj     instance void Instance::.ctor()
    IL_01ba:  ldftn      instance valuetype S32 Instance::CalcInstanceRetbuf(int32,
                                                                             int32)
    IL_01c0:  newobj     instance void class [System.Runtime]System.Func`3<int32,int32,valuetype S32>::.ctor(object,
                                                                                                             native int)
    IL_01c5:  ldloc.s    V_8
    IL_01c7:  ldstr      "Instance non-generic retbuf"
    IL_01cc:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__TestCalc|1'<valuetype S32>(class [System.Runtime]System.Func`3<int32,int32,!!0>,
                                                                                                            !!0,
                                                                                                            string)
    IL_01d1:  ldloc.0
    IL_01d2:  ldloc.s    V_4
    IL_01d4:  dup
    IL_01d5:  ldvirtftn  instance int32 BaseClass::CalcAbstract(int32,
                                                                int32)
    IL_01db:  newobj     instance void class [System.Runtime]System.Func`3<int32,int32,int32>::.ctor(object,
                                                                                                     native int)
    IL_01e0:  ldloc.3
    IL_01e1:  ldstr      "Abstract class non-generic"
    IL_01e6:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__TestCalc|1'<int32>(class [System.Runtime]System.Func`3<int32,int32,!!0>,
                                                                                                    !!0,
                                                                                                    string)
    IL_01eb:  ldloc.0
    IL_01ec:  ldloc.s    V_4
    IL_01ee:  dup
    IL_01ef:  ldvirtftn  instance valuetype S32 BaseClass::CalcAbstractRetbuf(int32,
                                                                              int32)
    IL_01f5:  newobj     instance void class [System.Runtime]System.Func`3<int32,int32,valuetype S32>::.ctor(object,
                                                                                                             native int)
    IL_01fa:  ldloc.s    V_8
    IL_01fc:  ldstr      "Abstract class non-generic retbuf"
    IL_0201:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__TestCalc|1'<valuetype S32>(class [System.Runtime]System.Func`3<int32,int32,!!0>,
                                                                                                            !!0,
                                                                                                            string)
    IL_0206:  ldloc.0
    IL_0207:  ldloc.s    V_5
    IL_0209:  dup
    IL_020a:  ldvirtftn  instance int32 InterfaceImpl::CalcInterface(int32,
                                                                     int32)
    IL_0210:  newobj     instance void class [System.Runtime]System.Func`3<int32,int32,int32>::.ctor(object,
                                                                                                     native int)
    IL_0215:  ldloc.3
    IL_0216:  ldstr      "Interface non-generic"
    IL_021b:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__TestCalc|1'<int32>(class [System.Runtime]System.Func`3<int32,int32,!!0>,
                                                                                                    !!0,
                                                                                                    string)
    IL_0220:  ldloc.0
    IL_0221:  ldloc.s    V_5
    IL_0223:  dup
    IL_0224:  ldvirtftn  instance valuetype S32 InterfaceImpl::CalcInterfaceRetbuf(int32,
                                                                                   int32)
    IL_022a:  newobj     instance void class [System.Runtime]System.Func`3<int32,int32,valuetype S32>::.ctor(object,
                                                                                                             native int)
    IL_022f:  ldloc.s    V_8
    IL_0231:  ldstr      "Interface non-generic retbuf"
    IL_0236:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__TestCalc|1'<valuetype S32>(class [System.Runtime]System.Func`3<int32,int32,!!0>,
                                                                                                            !!0,
                                                                                                            string)
    IL_023b:  ldloc.0
    IL_023c:  ldnull
    IL_023d:  ldftn      int32 Program::CalcStaticCalli(int32,
                                                        int32)
    IL_0243:  newobj     instance void class [System.Runtime]System.Func`3<int32,int32,int32>::.ctor(object,
                                                                                                     native int)
    IL_0248:  ldloc.3
    IL_0249:  ldstr      "Static calli"
    IL_024e:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__TestCalc|1'<int32>(class [System.Runtime]System.Func`3<int32,int32,!!0>,
                                                                                                    !!0,
                                                                                                    string)
    IL_0253:  ldloc.0
    IL_0254:  ldnull
    IL_0255:  ldftn      valuetype S32 Program::CalcStaticCalliRetbuf(int32,
                                                                      int32)
    IL_025b:  newobj     instance void class [System.Runtime]System.Func`3<int32,int32,valuetype S32>::.ctor(object,
                                                                                                             native int)
    IL_0260:  ldloc.s    V_8
    IL_0262:  ldstr      "Static calli retbuf"
    IL_0267:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__TestCalc|1'<valuetype S32>(class [System.Runtime]System.Func`3<int32,int32,!!0>,
                                                                                                            !!0,
                                                                                                            string)
    IL_026c:  ldloc.0
    IL_026d:  newobj     instance void Instance::.ctor()
    IL_0272:  ldftn      instance int32 Instance::CalcInstanceCalli(int32,
                                                                    int32)
    IL_0278:  newobj     instance void class [System.Runtime]System.Func`3<int32,int32,int32>::.ctor(object,
                                                                                                     native int)
    IL_027d:  ldloc.3
    IL_027e:  ldstr      "Instance calli"
    IL_0283:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__TestCalc|1'<int32>(class [System.Runtime]System.Func`3<int32,int32,!!0>,
                                                                                                    !!0,
                                                                                                    string)
    IL_0288:  ldloc.0
    IL_0289:  newobj     instance void Instance::.ctor()
    IL_028e:  ldftn      instance valuetype S32 Instance::CalcInstanceCalliRetbuf(int32,
                                                                                  int32)
    IL_0294:  newobj     instance void class [System.Runtime]System.Func`3<int32,int32,valuetype S32>::.ctor(object,
                                                                                                             native int)
    IL_0299:  ldloc.s    V_8
    IL_029b:  ldstr      "Instance calli retbuf"
    IL_02a0:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__TestCalc|1'<valuetype S32>(class [System.Runtime]System.Func`3<int32,int32,!!0>,
                                                                                                            !!0,
                                                                                                            string)
    IL_02a5:  ldloc.0
    IL_02a6:  ldsfld     class [System.Runtime]System.Func`1<string> Program/'<>c'::'<>9__7_3'
    IL_02ab:  dup
    IL_02ac:  brtrue.s   IL_02c5

    IL_02ae:  pop
    IL_02af:  ldsfld     class Program/'<>c' Program/'<>c'::'<>9'
    IL_02b4:  ldftn      instance string Program/'<>c'::'<Main>b__7_3'()
    IL_02ba:  newobj     instance void class [System.Runtime]System.Func`1<string>::.ctor(object,
                                                                                          native int)
    IL_02bf:  dup
    IL_02c0:  stsfld     class [System.Runtime]System.Func`1<string> Program/'<>c'::'<>9__7_3'
    IL_02c5:  ldstr      "Empty calli"
    IL_02ca:  ldstr      "Static calli without args"
    IL_02cf:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__Test|0'<string>(class [System.Runtime]System.Func`1<!!0>,
                                                                                                 !!0,
                                                                                                 string)
    IL_02d4:  ldloc.0
    IL_02d5:  ldsfld     class [System.Runtime]System.Func`1<string> Program/'<>c'::'<>9__7_4'
    IL_02da:  dup
    IL_02db:  brtrue.s   IL_02f4

    IL_02dd:  pop
    IL_02de:  ldsfld     class Program/'<>c' Program/'<>c'::'<>9'
    IL_02e3:  ldftn      instance string Program/'<>c'::'<Main>b__7_4'()
    IL_02e9:  newobj     instance void class [System.Runtime]System.Func`1<string>::.ctor(object,
                                                                                          native int)
    IL_02ee:  dup
    IL_02ef:  stsfld     class [System.Runtime]System.Func`1<string> Program/'<>c'::'<>9__7_4'
    IL_02f4:  ldstr      "Instance method"
    IL_02f9:  ldstr      "calli to an instance method on a value type"
    IL_02fe:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__Test|0'<string>(class [System.Runtime]System.Func`1<!!0>,
                                                                                                 !!0,
                                                                                                 string)
    IL_0303:  ldloc.0
    IL_0304:  ldsfld     class [System.Runtime]System.Func`1<string> Program/'<>c'::'<>9__7_5'
    IL_0309:  dup
    IL_030a:  brtrue.s   IL_0323

    IL_030c:  pop
    IL_030d:  ldsfld     class Program/'<>c' Program/'<>c'::'<>9'
    IL_0312:  ldftn      instance string Program/'<>c'::'<Main>b__7_5'()
    IL_0318:  newobj     instance void class [System.Runtime]System.Func`1<string>::.ctor(object,
                                                                                          native int)
    IL_031d:  dup
    IL_031e:  stsfld     class [System.Runtime]System.Func`1<string> Program/'<>c'::'<>9__7_5'
    IL_0323:  ldstr      "Instance method"
    IL_0328:  ldstr      "calli to an instance method on a value type with e"
    + "xplicit this"
    IL_032d:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__Test|0'<string>(class [System.Runtime]System.Func`1<!!0>,
                                                                                                 !!0,
                                                                                                 string)
    IL_0332:  ldloc.0
    IL_0333:  ldsfld     class [System.Runtime]System.Func`1<int32> Program/'<>c'::'<>9__7_6'
    IL_0338:  dup
    IL_0339:  brtrue.s   IL_0352

    IL_033b:  pop
    IL_033c:  ldsfld     class Program/'<>c' Program/'<>c'::'<>9'
    IL_0341:  ldftn      instance int32 Program/'<>c'::'<Main>b__7_6'()
    IL_0347:  newobj     instance void class [System.Runtime]System.Func`1<int32>::.ctor(object,
                                                                                         native int)
    IL_034c:  dup
    IL_034d:  stsfld     class [System.Runtime]System.Func`1<int32> Program/'<>c'::'<>9__7_6'
    IL_0352:  ldc.i4     0xf4240
    IL_0357:  ldstr      "Value type instance call"
    IL_035c:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__Test|0'<int32>(class [System.Runtime]System.Func`1<!!0>,
                                                                                                !!0,
                                                                                                string)
    IL_0361:  ldloc.0
    IL_0362:  ldloc.0
    IL_0363:  ldftn      instance string Program/'<>c__DisplayClass7_0'::'<Main>b__7'()
    IL_0369:  newobj     instance void class [System.Runtime]System.Func`1<string>::.ctor(object,
                                                                                          native int)
    IL_036e:  ldstr      "2 3 4 5 6 7 8 9 10"
    IL_0373:  ldstr      "Instance with GC"
    IL_0378:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__Test|0'<string>(class [System.Runtime]System.Func`1<!!0>,
                                                                                                 !!0,
                                                                                                 string)
    IL_037d:  ldloc.0
    IL_037e:  ldsfld     class [System.Runtime]System.Func`1<int32> Program/'<>c'::'<>9__7_8'
    IL_0383:  dup
    IL_0384:  brtrue.s   IL_039d

    IL_0386:  pop
    IL_0387:  ldsfld     class Program/'<>c' Program/'<>c'::'<>9'
    IL_038c:  ldftn      instance int32 Program/'<>c'::'<Main>b__7_8'()
    IL_0392:  newobj     instance void class [System.Runtime]System.Func`1<int32>::.ctor(object,
                                                                                         native int)
    IL_0397:  dup
    IL_0398:  stsfld     class [System.Runtime]System.Func`1<int32> Program/'<>c'::'<>9__7_8'
    IL_039d:  ldc.i4     0xf4240
    IL_03a2:  ldstr      "Count up with heap int"
    IL_03a7:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__Test|0'<int32>(class [System.Runtime]System.Func`1<!!0>,
                                                                                                !!0,
                                                                                                string)
    IL_03ac:  ldloc.0
    IL_03ad:  ldsfld     class [System.Runtime]System.Func`1<int32> Program/'<>c'::'<>9__7_9'
    IL_03b2:  dup
    IL_03b3:  brtrue.s   IL_03cc

    IL_03b5:  pop
    IL_03b6:  ldsfld     class Program/'<>c' Program/'<>c'::'<>9'
    IL_03bb:  ldftn      instance int32 Program/'<>c'::'<Main>b__7_9'()
    IL_03c1:  newobj     instance void class [System.Runtime]System.Func`1<int32>::.ctor(object,
                                                                                         native int)
    IL_03c6:  dup
    IL_03c7:  stsfld     class [System.Runtime]System.Func`1<int32> Program/'<>c'::'<>9__7_9'
    IL_03cc:  ldc.i4     0xf4240
    IL_03d1:  ldstr      "Count up with byref to heap"
    IL_03d6:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__Test|0'<int32>(class [System.Runtime]System.Func`1<!!0>,
                                                                                                !!0,
                                                                                                string)
    IL_03db:  ldloc.0
    IL_03dc:  ldsfld     class [System.Runtime]System.Func`1<string> Program/'<>c'::'<>9__7_10'
    IL_03e1:  dup
    IL_03e2:  brtrue.s   IL_03fb

    IL_03e4:  pop
    IL_03e5:  ldsfld     class Program/'<>c' Program/'<>c'::'<>9'
    IL_03ea:  ldftn      instance string Program/'<>c'::'<Main>b__7_10'()
    IL_03f0:  newobj     instance void class [System.Runtime]System.Func`1<string>::.ctor(object,
                                                                                          native int)
    IL_03f5:  dup
    IL_03f6:  stsfld     class [System.Runtime]System.Func`1<string> Program/'<>c'::'<>9__7_10'
    IL_03fb:  ldstr      "System.String hello"
    IL_0400:  ldstr      "Static generic string"
    IL_0405:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__Test|0'<string>(class [System.Runtime]System.Func`1<!!0>,
                                                                                                 !!0,
                                                                                                 string)
    IL_040a:  ldloc.0
    IL_040b:  ldsfld     class [System.Runtime]System.Func`1<string> Program/'<>c'::'<>9__7_11'
    IL_0410:  dup
    IL_0411:  brtrue.s   IL_042a

    IL_0413:  pop
    IL_0414:  ldsfld     class Program/'<>c' Program/'<>c'::'<>9'
    IL_0419:  ldftn      instance string Program/'<>c'::'<Main>b__7_11'()
    IL_041f:  newobj     instance void class [System.Runtime]System.Func`1<string>::.ctor(object,
                                                                                          native int)
    IL_0424:  dup
    IL_0425:  stsfld     class [System.Runtime]System.Func`1<string> Program/'<>c'::'<>9__7_11'
    IL_042a:  ldstr      "System.Object hello"
    IL_042f:  ldstr      "Static generic object"
    IL_0434:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__Test|0'<string>(class [System.Runtime]System.Func`1<!!0>,
                                                                                                 !!0,
                                                                                                 string)
    IL_0439:  ldloc.0
    IL_043a:  ldsfld     class [System.Runtime]System.Func`1<string> Program/'<>c'::'<>9__7_12'
    IL_043f:  dup
    IL_0440:  brtrue.s   IL_0459

    IL_0442:  pop
    IL_0443:  ldsfld     class Program/'<>c' Program/'<>c'::'<>9'
    IL_0448:  ldftn      instance string Program/'<>c'::'<Main>b__7_12'()
    IL_044e:  newobj     instance void class [System.Runtime]System.Func`1<string>::.ctor(object,
                                                                                          native int)
    IL_0453:  dup
    IL_0454:  stsfld     class [System.Runtime]System.Func`1<string> Program/'<>c'::'<>9__7_12'
    IL_0459:  ldstr      "System.Int32 5"
    IL_045e:  ldstr      "Static generic int"
    IL_0463:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__Test|0'<string>(class [System.Runtime]System.Func`1<!!0>,
                                                                                                 !!0,
                                                                                                 string)
    IL_0468:  ldloc.0
    IL_0469:  ldsfld     class [System.Runtime]System.Func`1<string> Program/'<>c'::'<>9__7_13'
    IL_046e:  dup
    IL_046f:  brtrue.s   IL_0488

    IL_0471:  pop
    IL_0472:  ldsfld     class Program/'<>c' Program/'<>c'::'<>9'
    IL_0477:  ldftn      instance string Program/'<>c'::'<Main>b__7_13'()
    IL_047d:  newobj     instance void class [System.Runtime]System.Func`1<string>::.ctor(object,
                                                                                          native int)
    IL_0482:  dup
    IL_0483:  stsfld     class [System.Runtime]System.Func`1<string> Program/'<>c'::'<>9__7_13'
    IL_0488:  ldstr      "System.String System.Object hello hello2"
    IL_048d:  ldstr      "Static generic 2 string object"
    IL_0492:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__Test|0'<string>(class [System.Runtime]System.Func`1<!!0>,
                                                                                                 !!0,
                                                                                                 string)
    IL_0497:  ldloc.0
    IL_0498:  ldsfld     class [System.Runtime]System.Func`1<string> Program/'<>c'::'<>9__7_14'
    IL_049d:  dup
    IL_049e:  brtrue.s   IL_04b7

    IL_04a0:  pop
    IL_04a1:  ldsfld     class Program/'<>c' Program/'<>c'::'<>9'
    IL_04a6:  ldftn      instance string Program/'<>c'::'<Main>b__7_14'()
    IL_04ac:  newobj     instance void class [System.Runtime]System.Func`1<string>::.ctor(object,
                                                                                          native int)
    IL_04b1:  dup
    IL_04b2:  stsfld     class [System.Runtime]System.Func`1<string> Program/'<>c'::'<>9__7_14'
    IL_04b7:  ldstr      "System.String System.Int32 hello 5"
    IL_04bc:  ldstr      "Static generic 2 string int"
    IL_04c1:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__Test|0'<string>(class [System.Runtime]System.Func`1<!!0>,
                                                                                                 !!0,
                                                                                                 string)
    IL_04c6:  ldloc.0
    IL_04c7:  ldsfld     class [System.Runtime]System.Func`1<string> Program/'<>c'::'<>9__7_15'
    IL_04cc:  dup
    IL_04cd:  brtrue.s   IL_04e6

    IL_04cf:  pop
    IL_04d0:  ldsfld     class Program/'<>c' Program/'<>c'::'<>9'
    IL_04d5:  ldftn      instance string Program/'<>c'::'<Main>b__7_15'()
    IL_04db:  newobj     instance void class [System.Runtime]System.Func`1<string>::.ctor(object,
                                                                                          native int)
    IL_04e0:  dup
    IL_04e1:  stsfld     class [System.Runtime]System.Func`1<string> Program/'<>c'::'<>9__7_15'
    IL_04e6:  ldstr      "System.String System.String hello hello2"
    IL_04eb:  ldstr      "Static generic 1 string"
    IL_04f0:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__Test|0'<string>(class [System.Runtime]System.Func`1<!!0>,
                                                                                                 !!0,
                                                                                                 string)
    IL_04f5:  ldloc.0
    IL_04f6:  ldsfld     class [System.Runtime]System.Func`1<string> Program/'<>c'::'<>9__7_16'
    IL_04fb:  dup
    IL_04fc:  brtrue.s   IL_0515

    IL_04fe:  pop
    IL_04ff:  ldsfld     class Program/'<>c' Program/'<>c'::'<>9'
    IL_0504:  ldftn      instance string Program/'<>c'::'<Main>b__7_16'()
    IL_050a:  newobj     instance void class [System.Runtime]System.Func`1<string>::.ctor(object,
                                                                                          native int)
    IL_050f:  dup
    IL_0510:  stsfld     class [System.Runtime]System.Func`1<string> Program/'<>c'::'<>9__7_16'
    IL_0515:  ldstr      "System.Object System.String hello hello2"
    IL_051a:  ldstr      "Static generic 1 object"
    IL_051f:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__Test|0'<string>(class [System.Runtime]System.Func`1<!!0>,
                                                                                                 !!0,
                                                                                                 string)
    IL_0524:  ldloc.0
    IL_0525:  ldsfld     class [System.Runtime]System.Func`1<string> Program/'<>c'::'<>9__7_17'
    IL_052a:  dup
    IL_052b:  brtrue.s   IL_0544

    IL_052d:  pop
    IL_052e:  ldsfld     class Program/'<>c' Program/'<>c'::'<>9'
    IL_0533:  ldftn      instance string Program/'<>c'::'<Main>b__7_17'()
    IL_0539:  newobj     instance void class [System.Runtime]System.Func`1<string>::.ctor(object,
                                                                                          native int)
    IL_053e:  dup
    IL_053f:  stsfld     class [System.Runtime]System.Func`1<string> Program/'<>c'::'<>9__7_17'
    IL_0544:  ldstr      "System.Int32 System.String 5 hello2"
    IL_0549:  ldstr      "Static generic 1 int"
    IL_054e:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__Test|0'<string>(class [System.Runtime]System.Func`1<!!0>,
                                                                                                 !!0,
                                                                                                 string)
    IL_0553:  ldloc.0
    IL_0554:  ldsfld     class [System.Runtime]System.Func`1<string> Program/'<>c'::'<>9__7_18'
    IL_0559:  dup
    IL_055a:  brtrue.s   IL_0573

    IL_055c:  pop
    IL_055d:  ldsfld     class Program/'<>c' Program/'<>c'::'<>9'
    IL_0562:  ldftn      instance string Program/'<>c'::'<Main>b__7_18'()
    IL_0568:  newobj     instance void class [System.Runtime]System.Func`1<string>::.ctor(object,
                                                                                          native int)
    IL_056d:  dup
    IL_056e:  stsfld     class [System.Runtime]System.Func`1<string> Program/'<>c'::'<>9__7_18'
    IL_0573:  ldstr      "System.Object System.String hello hello2"
    IL_0578:  ldstr      "Static generic 0"
    IL_057d:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__Test|0'<string>(class [System.Runtime]System.Func`1<!!0>,
                                                                                                 !!0,
                                                                                                 string)
    IL_0582:  ldloc.0
    IL_0583:  ldloc.0
    IL_0584:  ldftn      instance string Program/'<>c__DisplayClass7_0'::'<Main>b__19'()
    IL_058a:  newobj     instance void class [System.Runtime]System.Func`1<string>::.ctor(object,
                                                                                          native int)
    IL_058f:  ldstr      "System.String System.Int32 System.Object System.St"
    + "ring a 5 b c"
    IL_0594:  ldstr      "Instance generic 4"
    IL_0599:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__Test|0'<string>(class [System.Runtime]System.Func`1<!!0>,
                                                                                                 !!0,
                                                                                                 string)
    IL_059e:  ldloc.0
    IL_059f:  ldloc.0
    IL_05a0:  ldftn      instance string Program/'<>c__DisplayClass7_0'::'<Main>b__20'()
    IL_05a6:  newobj     instance void class [System.Runtime]System.Func`1<string>::.ctor(object,
                                                                                          native int)
    IL_05ab:  ldstr      "System.String System.Int32 System.Object System.St"
    + "ring a 5 b c"
    IL_05b0:  ldstr      "Virtual instance generic 4"
    IL_05b5:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__Test|0'<string>(class [System.Runtime]System.Func`1<!!0>,
                                                                                                 !!0,
                                                                                                 string)
    IL_05ba:  ldloc.0
    IL_05bb:  ldloc.0
    IL_05bc:  ldftn      instance string Program/'<>c__DisplayClass7_0'::'<Main>b__21'()
    IL_05c2:  newobj     instance void class [System.Runtime]System.Func`1<string>::.ctor(object,
                                                                                          native int)
    IL_05c7:  ldstr      "System.String System.Int32 System.String System.Ob"
    + "ject a 5 c d"
    IL_05cc:  ldstr      "Interface generic 4"
    IL_05d1:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__Test|0'<string>(class [System.Runtime]System.Func`1<!!0>,
                                                                                                 !!0,
                                                                                                 string)
    IL_05d6:  ldloc.0
    IL_05d7:  ldloc.0
    IL_05d8:  ldftn      instance string Program/'<>c__DisplayClass7_0'::'<Main>b__22'()
    IL_05de:  newobj     instance void class [System.Runtime]System.Func`1<string>::.ctor(object,
                                                                                          native int)
    IL_05e3:  ldstr      "System.String System.Int32 a 5"
    IL_05e8:  ldstr      "Interface generic forward G"
    IL_05ed:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__Test|0'<string>(class [System.Runtime]System.Func`1<!!0>,
                                                                                                 !!0,
                                                                                                 string)
    IL_05f2:  ldloc.0
    IL_05f3:  ldloc.0
    IL_05f4:  ldftn      instance string Program/'<>c__DisplayClass7_0'::'<Main>b__23'()
    IL_05fa:  newobj     instance void class [System.Runtime]System.Func`1<string>::.ctor(object,
                                                                                          native int)
    IL_05ff:  ldstr      "System.String System.Object System.Int32 System.Ob"
    + "ject a b 5 d"
    IL_0604:  ldstr      "Interface generic 0"
    IL_0609:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__Test|0'<string>(class [System.Runtime]System.Func`1<!!0>,
                                                                                                 !!0,
                                                                                                 string)
    IL_060e:  ldloc.0
    IL_060f:  ldloc.0
    IL_0610:  ldftn      instance string Program/'<>c__DisplayClass7_0'::'<Main>b__24'()
    IL_0616:  newobj     instance void class [System.Runtime]System.Func`1<string>::.ctor(object,
                                                                                          native int)
    IL_061b:  ldstr      "System.String System.Object a b"
    IL_0620:  ldstr      "Interface generic without generics on method"
    IL_0625:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__Test|0'<string>(class [System.Runtime]System.Func`1<!!0>,
                                                                                                 !!0,
                                                                                                 string)
    IL_062a:  ldloc.0
    IL_062b:  ldloc.0
    IL_062c:  ldftn      instance string Program/'<>c__DisplayClass7_0'::'<Main>b__25'()
    IL_0632:  newobj     instance void class [System.Runtime]System.Func`1<string>::.ctor(object,
                                                                                          native int)
    IL_0637:  ldstr      "System.String System.Object"
    IL_063c:  ldstr      "Abstract generic with generic on method 1"
    IL_0641:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__Test|0'<string>(class [System.Runtime]System.Func`1<!!0>,
                                                                                                 !!0,
                                                                                                 string)
    IL_0646:  ldloc.0
    IL_0647:  ldloc.0
    IL_0648:  ldftn      instance string Program/'<>c__DisplayClass7_0'::'<Main>b__26'()
    IL_064e:  newobj     instance void class [System.Runtime]System.Func`1<string>::.ctor(object,
                                                                                          native int)
    IL_0653:  ldstr      "System.Int32 System.Object"
    IL_0658:  ldstr      "Abstract generic with generic on method 2"
    IL_065d:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__Test|0'<string>(class [System.Runtime]System.Func`1<!!0>,
                                                                                                 !!0,
                                                                                                 string)
    IL_0662:  ldloc.0
    IL_0663:  ldloc.0
    IL_0664:  ldftn      instance string Program/'<>c__DisplayClass7_0'::'<Main>b__27'()
    IL_066a:  newobj     instance void class [System.Runtime]System.Func`1<string>::.ctor(object,
                                                                                          native int)
    IL_066f:  ldstr      "System.String"
    IL_0674:  ldstr      "Abstract generic without generic on method 1"
    IL_0679:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__Test|0'<string>(class [System.Runtime]System.Func`1<!!0>,
                                                                                                 !!0,
                                                                                                 string)
    IL_067e:  ldloc.0
    IL_067f:  ldloc.0
    IL_0680:  ldftn      instance string Program/'<>c__DisplayClass7_0'::'<Main>b__28'()
    IL_0686:  newobj     instance void class [System.Runtime]System.Func`1<string>::.ctor(object,
                                                                                          native int)
    IL_068b:  ldstr      "System.Int32"
    IL_0690:  ldstr      "Abstract generic without generic on method 2"
    IL_0695:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__Test|0'<string>(class [System.Runtime]System.Func`1<!!0>,
                                                                                                 !!0,
                                                                                                 string)
    IL_069a:  ldloc.0
    IL_069b:  ldc.i4     0xf4240
    IL_06a0:  newarr     [System.Runtime]System.Int32
    IL_06a5:  stfld      int32[] Program/'<>c__DisplayClass7_0'::a
    IL_06aa:  ldloc.0
    IL_06ab:  ldfld      int32[] Program/'<>c__DisplayClass7_0'::a
    IL_06b0:  ldc.i4.s   99
    IL_06b2:  ldc.i4.1
    IL_06b3:  stelem.i4
    IL_06b4:  ldloc.0
    IL_06b5:  ldloc.0
    IL_06b6:  ldftn      instance int32 Program/'<>c__DisplayClass7_0'::'<Main>b__29'()
    IL_06bc:  newobj     instance void class [System.Runtime]System.Func`1<int32>::.ctor(object,
                                                                                         native int)
    IL_06c1:  ldloc.0
    IL_06c2:  ldfld      int32[] Program/'<>c__DisplayClass7_0'::a
    IL_06c7:  ldlen
    IL_06c8:  conv.i4
    IL_06c9:  ldc.i4.1
    IL_06ca:  add
    IL_06cb:  ldstr      "Instantiating stub direct"
    IL_06d0:  callvirt   instance void Program/'<>c__DisplayClass7_0'::'<Main>g__Test|0'<int32>(class [System.Runtime]System.Func`1<!!0>,
                                                                                                !!0,
                                                                                                string)
    IL_06d5:  ldloc.0
    IL_06d6:  ldfld      bool Program/'<>c__DisplayClass7_0'::result
    IL_06db:  brfalse.s  IL_06e9

    IL_06dd:  ldstr      "All tailcall-via-help succeeded"
    IL_06e2:  call       void [System.Console]System.Console::WriteLine(string)
    IL_06e7:  br.s       IL_06f3

    IL_06e9:  ldstr      "One or more failures in tailcall-via-help test"
    IL_06ee:  call       void [System.Console]System.Console::WriteLine(string)
    IL_06f3:  ldloc.0
    IL_06f4:  ldfld      bool Program/'<>c__DisplayClass7_0'::result
    IL_06f9:  brtrue.s   IL_06fd

    IL_06fb:  ldc.i4.1
    IL_06fc:  ret

    IL_06fd:  ldc.i4.s   100
    IL_06ff:  ret
  } // end of method Program::Main

  .method public hidebysig static void  Calc(int32& x,
                                             valuetype S32& s,
                                             int32& acc) cil managed
  {
    // Code size       166 (0xa6)
    .maxstack  5
    IL_0000:  ldarg.0
    IL_0001:  ldind.i4
    IL_0002:  ldc.i4.2
    IL_0003:  rem
    IL_0004:  brtrue.s   IL_003d

    IL_0006:  ldarg.2
    IL_0007:  ldarg.2
    IL_0008:  ldind.i4
    IL_0009:  ldarg.0
    IL_000a:  ldind.i4
    IL_000b:  ldc.i4.3
    IL_000c:  mul
    IL_000d:  conv.i8
    IL_000e:  ldarg.1
    IL_000f:  ldfld      int64 S32::A
    IL_0014:  ldc.i4.7
    IL_0015:  conv.i8
    IL_0016:  mul
    IL_0017:  add
    IL_0018:  ldarg.1
    IL_0019:  ldfld      int64 S32::B
    IL_001e:  ldc.i4.s   9
    IL_0020:  conv.i8
    IL_0021:  mul
    IL_0022:  add
    IL_0023:  ldarg.1
    IL_0024:  ldfld      int64 S32::C
    IL_0029:  ldc.i4.s   -3
    IL_002b:  conv.i8
    IL_002c:  mul
    IL_002d:  add
    IL_002e:  ldarg.1
    IL_002f:  ldfld      int64 S32::D
    IL_0034:  ldc.i4.4
    IL_0035:  conv.i8
    IL_0036:  mul
    IL_0037:  add
    IL_0038:  conv.i4
    IL_0039:  add
    IL_003a:  stind.i4
    IL_003b:  br.s       IL_0070

    IL_003d:  ldarg.2
    IL_003e:  ldarg.2
    IL_003f:  ldind.i4
    IL_0040:  ldarg.0
    IL_0041:  ldind.i4
    IL_0042:  conv.i8
    IL_0043:  ldarg.1
    IL_0044:  ldfld      int64 S32::A
    IL_0049:  ldc.i4.s   9
    IL_004b:  conv.i8
    IL_004c:  mul
    IL_004d:  add
    IL_004e:  ldarg.1
    IL_004f:  ldfld      int64 S32::B
    IL_0054:  ldc.i4.3
    IL_0055:  conv.i8
    IL_0056:  mul
    IL_0057:  add
    IL_0058:  ldarg.1
    IL_0059:  ldfld      int64 S32::C
    IL_005e:  ldc.i4.s   -4
    IL_0060:  conv.i8
    IL_0061:  mul
    IL_0062:  add
    IL_0063:  ldarg.1
    IL_0064:  ldfld      int64 S32::D
    IL_0069:  ldc.i4.5
    IL_006a:  conv.i8
    IL_006b:  mul
    IL_006c:  add
    IL_006d:  conv.i4
    IL_006e:  add
    IL_006f:  stind.i4
    IL_0070:  ldarg.0
    IL_0071:  ldarg.0
    IL_0072:  ldind.i4
    IL_0073:  ldc.i4.1
    IL_0074:  sub
    IL_0075:  stind.i4
    IL_0076:  ldarg.1
    IL_0077:  ldc.i4.s   11
    IL_0079:  ldarg.0
    IL_007a:  ldind.i4
    IL_007b:  mul
    IL_007c:  conv.i8
    IL_007d:  stfld      int64 S32::A
    IL_0082:  ldarg.1
    IL_0083:  ldc.i4.s   14
    IL_0085:  ldarg.0
    IL_0086:  ldind.i4
    IL_0087:  mul
    IL_0088:  conv.i8
    IL_0089:  stfld      int64 S32::B
    IL_008e:  ldarg.1
    IL_008f:  ldc.i4.s   -14
    IL_0091:  ldarg.0
    IL_0092:  ldind.i4
    IL_0093:  mul
    IL_0094:  conv.i8
    IL_0095:  stfld      int64 S32::C
    IL_009a:  ldarg.1
    IL_009b:  ldc.i4.3
    IL_009c:  ldarg.0
    IL_009d:  ldind.i4
    IL_009e:  mul
    IL_009f:  conv.i8
    IL_00a0:  stfld      int64 S32::D
    IL_00a5:  ret
  } // end of method Program::Calc

  .method private hidebysig static int32 
          CalcStatic(int32 x,
                     int32 acc) cil managed noinlining
  {
    // Code size       35 (0x23)
    .maxstack  3
    .locals init (valuetype S32 V_0)
    IL_0000:  ldarg.0
    IL_0001:  brtrue.s   IL_0005

    IL_0003:  ldarg.1
    IL_0004:  ret

    IL_0005:  ldloca.s   V_0
    IL_0007:  initobj    S32
    IL_000d:  ldarga.s   x
    IL_000f:  ldloca.s   V_0
    IL_0011:  ldarga.s   acc
    IL_0013:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_0018:  ldarg.0
    IL_0019:  ldloc.0
    IL_001a:  ldarg.1
    IL_001b:  tail.
    IL_001d:  call       int32 Program::CalcStaticOther(int32,
                                                        valuetype S32,
                                                        int32)
    IL_0022:  ret
  } // end of method Program::CalcStatic

  .method private hidebysig static int32 
          CalcStaticOther(int32 x,
                          valuetype S32 large,
                          int32 acc) cil managed noinlining
  {
    // Code size       21 (0x15)
    .maxstack  3
    IL_0000:  ldarga.s   x
    IL_0002:  ldarga.s   large
    IL_0004:  ldarga.s   acc
    IL_0006:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_000b:  ldarg.0
    IL_000c:  ldarg.2
    IL_000d:  tail.
    IL_000f:  call       int32 Program::CalcStatic(int32,
                                                   int32)
    IL_0014:  ret
  } // end of method Program::CalcStaticOther

  .method private hidebysig static uint8 
          CalcStaticSmall(int32 x,
                          int32 acc) cil managed noinlining
  {
    // Code size       36 (0x24)
    .maxstack  3
    .locals init (valuetype S32 V_0)
    IL_0000:  ldarg.0
    IL_0001:  brtrue.s   IL_0006

    IL_0003:  ldarg.1
    IL_0004:  conv.u1
    IL_0005:  ret

    IL_0006:  ldloca.s   V_0
    IL_0008:  initobj    S32
    IL_000e:  ldarga.s   x
    IL_0010:  ldloca.s   V_0
    IL_0012:  ldarga.s   acc
    IL_0014:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_0019:  ldarg.0
    IL_001a:  ldloc.0
    IL_001b:  ldarg.1
    IL_001c:  tail.
    IL_001e:  call       uint8 Program::CalcStaticSmallOther(int32,
                                                             valuetype S32,
                                                             int32)
    IL_0023:  ret
  } // end of method Program::CalcStaticSmall

  .method private hidebysig static uint8 
          CalcStaticSmallOther(int32 x,
                               valuetype S32 large,
                               int32 acc) cil managed noinlining
  {
    // Code size       21 (0x15)
    .maxstack  3
    IL_0000:  ldarga.s   x
    IL_0002:  ldarga.s   large
    IL_0004:  ldarga.s   acc
    IL_0006:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_000b:  ldarg.0
    IL_000c:  ldarg.2
    IL_000d:  tail.
    IL_000f:  call       uint8 Program::CalcStaticSmall(int32,
                                                        int32)
    IL_0014:  ret
  } // end of method Program::CalcStaticSmallOther

  .method private hidebysig static valuetype S32 
          CalcStaticRetbuf(int32 x,
                           int32 acc) cil managed noinlining
  {
    // Code size       79 (0x4f)
    .maxstack  3
    .locals init (valuetype S32 V_0,
             valuetype S32 V_1)
    IL_0000:  ldarg.0
    IL_0001:  brtrue.s   IL_0031

    IL_0003:  ldloca.s   V_1
    IL_0005:  initobj    S32
    IL_000b:  ldloca.s   V_1
    IL_000d:  ldarg.1
    IL_000e:  conv.i8
    IL_000f:  stfld      int64 S32::A
    IL_0014:  ldloca.s   V_1
    IL_0016:  ldarg.1
    IL_0017:  conv.i8
    IL_0018:  stfld      int64 S32::B
    IL_001d:  ldloca.s   V_1
    IL_001f:  ldarg.1
    IL_0020:  conv.i8
    IL_0021:  stfld      int64 S32::C
    IL_0026:  ldloca.s   V_1
    IL_0028:  ldarg.1
    IL_0029:  conv.i8
    IL_002a:  stfld      int64 S32::D
    IL_002f:  ldloc.1
    IL_0030:  ret

    IL_0031:  ldloca.s   V_0
    IL_0033:  initobj    S32
    IL_0039:  ldarga.s   x
    IL_003b:  ldloca.s   V_0
    IL_003d:  ldarga.s   acc
    IL_003f:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_0044:  ldarg.0
    IL_0045:  ldloc.0
    IL_0046:  ldarg.1
    IL_0047:  tail.
    IL_0049:  call       valuetype S32 Program::CalcStaticRetbufOther(int32,
                                                                      valuetype S32,
                                                                      int32)
    IL_004e:  ret
  } // end of method Program::CalcStaticRetbuf

  .method private hidebysig static valuetype S32 
          CalcStaticRetbufOther(int32 x,
                                valuetype S32 large,
                                int32 acc) cil managed noinlining
  {
    // Code size       21 (0x15)
    .maxstack  3
    IL_0000:  ldarga.s   x
    IL_0002:  ldarga.s   large
    IL_0004:  ldarga.s   acc
    IL_0006:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_000b:  ldarg.0
    IL_000c:  ldarg.2
    IL_000d:  tail.
    IL_000f:  call       valuetype S32 Program::CalcStaticRetbuf(int32,
                                                                 int32)
    IL_0014:  ret
  } // end of method Program::CalcStaticRetbufOther

  .method private hidebysig static int64 
          CalcStaticLong(int32 x,
                         int32 acc) cil managed noinlining
  {
    // Code size       42 (0x2a)
    .maxstack  3
    .locals init (valuetype S32 V_0)
    IL_0000:  ldarg.0
    IL_0001:  brtrue.s   IL_000c

    IL_0003:  ldarg.1
    IL_0004:  conv.u8
    IL_0005:  ldc.i4.s   32
    IL_0007:  shl
    IL_0008:  ldarg.1
    IL_0009:  conv.u8
    IL_000a:  or
    IL_000b:  ret

    IL_000c:  ldloca.s   V_0
    IL_000e:  initobj    S32
    IL_0014:  ldarga.s   x
    IL_0016:  ldloca.s   V_0
    IL_0018:  ldarga.s   acc
    IL_001a:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_001f:  ldarg.0
    IL_0020:  ldloc.0
    IL_0021:  ldarg.1
    IL_0022:  tail.
    IL_0024:  call       int64 Program::CalcStaticLongOther(int32,
                                                            valuetype S32,
                                                            int32)
    IL_0029:  ret
  } // end of method Program::CalcStaticLong

  .method private hidebysig static int64 
          CalcStaticLongOther(int32 x,
                              valuetype S32 large,
                              int32 acc) cil managed noinlining
  {
    // Code size       21 (0x15)
    .maxstack  3
    IL_0000:  ldarga.s   x
    IL_0002:  ldarga.s   large
    IL_0004:  ldarga.s   acc
    IL_0006:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_000b:  ldarg.0
    IL_000c:  ldarg.2
    IL_000d:  tail.
    IL_000f:  call       int64 Program::CalcStaticLong(int32,
                                                       int32)
    IL_0014:  ret
  } // end of method Program::CalcStaticLongOther

  .method private hidebysig static valuetype S16 
          CalcStaticS16(int32 x,
                        int32 acc) cil managed noinlining
  {
    // Code size       61 (0x3d)
    .maxstack  3
    .locals init (valuetype S32 V_0,
             valuetype S16 V_1)
    IL_0000:  ldarg.0
    IL_0001:  brtrue.s   IL_001f

    IL_0003:  ldloca.s   V_1
    IL_0005:  initobj    S16
    IL_000b:  ldloca.s   V_1
    IL_000d:  ldarg.1
    IL_000e:  conv.i8
    IL_000f:  stfld      int64 S16::A
    IL_0014:  ldloca.s   V_1
    IL_0016:  ldarg.1
    IL_0017:  conv.i8
    IL_0018:  stfld      int64 S16::B
    IL_001d:  ldloc.1
    IL_001e:  ret

    IL_001f:  ldloca.s   V_0
    IL_0021:  initobj    S32
    IL_0027:  ldarga.s   x
    IL_0029:  ldloca.s   V_0
    IL_002b:  ldarga.s   acc
    IL_002d:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_0032:  ldarg.0
    IL_0033:  ldloc.0
    IL_0034:  ldarg.1
    IL_0035:  tail.
    IL_0037:  call       valuetype S16 Program::CalcStaticS16Other(int32,
                                                                   valuetype S32,
                                                                   int32)
    IL_003c:  ret
  } // end of method Program::CalcStaticS16

  .method private hidebysig static valuetype S16 
          CalcStaticS16Other(int32 x,
                             valuetype S32 large,
                             int32 acc) cil managed noinlining
  {
    // Code size       21 (0x15)
    .maxstack  3
    IL_0000:  ldarga.s   x
    IL_0002:  ldarga.s   large
    IL_0004:  ldarga.s   acc
    IL_0006:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_000b:  ldarg.0
    IL_000c:  ldarg.2
    IL_000d:  tail.
    IL_000f:  call       valuetype S16 Program::CalcStaticS16(int32,
                                                              int32)
    IL_0014:  ret
  } // end of method Program::CalcStaticS16Other

  .method private hidebysig static int32 
          CalcStaticCalli(int32 x,
                          int32 acc) cil managed noinlining
  {
    // Code size       40 (0x28)
    .maxstack  4
    .locals init (valuetype S32 V_0)
    IL_0000:  ldarg.0
    IL_0001:  brtrue.s   IL_0005

    IL_0003:  ldarg.1
    IL_0004:  ret

    IL_0005:  ldloca.s   V_0
    IL_0007:  initobj    S32
    IL_000d:  ldarga.s   x
    IL_000f:  ldloca.s   V_0
    IL_0011:  ldarga.s   acc
    IL_0013:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_0018:  ldarg.0
    IL_0019:  ldloc.0
    IL_001a:  ldarg.1
    IL_001b:  ldsfld     native int Program::s_calcStaticCalliOther
    IL_0020:  tail.
    IL_0022:  calli      int32(int32,valuetype S32,int32)
    IL_0027:  ret
  } // end of method Program::CalcStaticCalli

  .method private hidebysig static int32 
          CalcStaticCalliOther(int32 x,
                               valuetype S32 large,
                               int32 acc) cil managed noinlining
  {
    // Code size       26 (0x1a)
    .maxstack  3
    IL_0000:  ldarga.s   x
    IL_0002:  ldarga.s   large
    IL_0004:  ldarga.s   acc
    IL_0006:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_000b:  ldarg.0
    IL_000c:  ldarg.2
    IL_000d:  ldsfld     native int Program::s_calcStaticCalli
    IL_0012:  tail.
    IL_0014:  calli      int32(int32,int32)
    IL_0019:  ret
  } // end of method Program::CalcStaticCalliOther

  .method private hidebysig static string 
          EmptyCalli() cil managed noinlining
  {
    // Code size       41 (0x29)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  call       int32 [System.Runtime]System.Environment::get_TickCount()
    IL_0005:  ldc.i4.0
    IL_0006:  blt.s      IL_000c

    IL_0008:  ldc.i4.s   40
    IL_000a:  br.s       IL_000e

    IL_000c:  ldc.i4.s   30
    IL_000e:  stloc.0
    IL_000f:  ldloc.0
    IL_0010:  conv.u
    IL_0011:  ldc.i4.4
    IL_0012:  mul.ovf.un
    IL_0013:  localloc
    IL_0015:  ldloc.0
    IL_0016:  newobj     instance void valuetype [System.Runtime]System.Span`1<int32>::.ctor(void*,
                                                                                             int32)
    IL_001b:  pop
    IL_001c:  ldsfld     native int Program::s_emptyCalliOther
    IL_0021:  tail.
    IL_0023:  calli      string()
    IL_0028:  ret
  } // end of method Program::EmptyCalli

  .method private hidebysig static string 
          ValueTypeInstanceMethodCalli() cil managed noinlining
  {
    // Code size       51 (0x33)
    .maxstack  2
    .locals init (valuetype S16 V_0,
             int32 V_1)
    IL_0000:  call       int32 [System.Runtime]System.Environment::get_TickCount()
    IL_0005:  ldc.i4.0
    IL_0006:  blt.s      IL_000c

    IL_0008:  ldc.i4.s   40
    IL_000a:  br.s       IL_000e

    IL_000c:  ldc.i4.s   30
    IL_000e:  stloc.1
    IL_000f:  ldloc.1
    IL_0010:  conv.u
    IL_0011:  ldc.i4.4
    IL_0012:  mul.ovf.un
    IL_0013:  localloc
    IL_0015:  ldloc.1
    IL_0016:  newobj     instance void valuetype [System.Runtime]System.Span`1<int32>::.ctor(void*,
                                                                                             int32)
    IL_001b:  pop
    IL_001c:  ldloca.s   V_0
    IL_001e:  initobj    S16
    IL_0024:  ldloca.s   V_0
    IL_0026:  ldsfld     native int Program::s_instanceMethodOnValueType
    IL_002b:  tail.
    IL_002d:  calli      instance string()
    IL_0032:  ret
  } // end of method Program::ValueTypeInstanceMethodCalli

  .method private hidebysig static string 
          ValueTypeExplicitThisInstanceMethodCalli() cil managed noinlining
  {
    // Code size       51 (0x33)
    .maxstack  2
    .locals init (valuetype S16 V_0,
             int32 V_1)
    IL_0000:  call       int32 [System.Runtime]System.Environment::get_TickCount()
    IL_0005:  ldc.i4.0
    IL_0006:  blt.s      IL_000c

    IL_0008:  ldc.i4.s   40
    IL_000a:  br.s       IL_000e

    IL_000c:  ldc.i4.s   30
    IL_000e:  stloc.1
    IL_000f:  ldloc.1
    IL_0010:  conv.u
    IL_0011:  ldc.i4.4
    IL_0012:  mul.ovf.un
    IL_0013:  localloc
    IL_0015:  ldloc.1
    IL_0016:  newobj     instance void valuetype [System.Runtime]System.Span`1<int32>::.ctor(void*,
                                                                                             int32)
    IL_001b:  pop
    IL_001c:  ldloca.s   V_0
    IL_001e:  initobj    S16
    IL_0024:  ldloca.s   V_0
    IL_0026:  ldsfld     native int Program::s_instanceMethodOnValueType
    IL_002b:  tail.
    IL_002d:  calli      explicit instance string(valuetype S16&)
    IL_0032:  ret
  } // end of method Program::ValueTypeExplicitThisInstanceMethodCalli

  .method private hidebysig static string 
          EmptyCalliOther() cil managed noinlining
  {
    // Code size       6 (0x6)
    .maxstack  8
    IL_0000:  ldstr      "Empty calli"
    IL_0005:  ret
  } // end of method Program::EmptyCalliOther

  .method private hidebysig static valuetype S32 
          CalcStaticCalliRetbuf(int32 x,
                                int32 acc) cil managed noinlining
  {
    // Code size       84 (0x54)
    .maxstack  4
    .locals init (valuetype S32 V_0,
             valuetype S32 V_1)
    IL_0000:  ldarg.0
    IL_0001:  brtrue.s   IL_0031

    IL_0003:  ldloca.s   V_1
    IL_0005:  initobj    S32
    IL_000b:  ldloca.s   V_1
    IL_000d:  ldarg.1
    IL_000e:  conv.i8
    IL_000f:  stfld      int64 S32::A
    IL_0014:  ldloca.s   V_1
    IL_0016:  ldarg.1
    IL_0017:  conv.i8
    IL_0018:  stfld      int64 S32::B
    IL_001d:  ldloca.s   V_1
    IL_001f:  ldarg.1
    IL_0020:  conv.i8
    IL_0021:  stfld      int64 S32::C
    IL_0026:  ldloca.s   V_1
    IL_0028:  ldarg.1
    IL_0029:  conv.i8
    IL_002a:  stfld      int64 S32::D
    IL_002f:  ldloc.1
    IL_0030:  ret

    IL_0031:  ldloca.s   V_0
    IL_0033:  initobj    S32
    IL_0039:  ldarga.s   x
    IL_003b:  ldloca.s   V_0
    IL_003d:  ldarga.s   acc
    IL_003f:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_0044:  ldarg.0
    IL_0045:  ldloc.0
    IL_0046:  ldarg.1
    IL_0047:  ldsfld     native int Program::s_calcStaticCalliRetbufOther
    IL_004c:  tail.
    IL_004e:  calli      valuetype S32(int32,valuetype S32,int32)
    IL_0053:  ret
  } // end of method Program::CalcStaticCalliRetbuf

  .method private hidebysig static valuetype S32 
          CalcStaticCalliRetbufOther(int32 x,
                                     valuetype S32 large,
                                     int32 acc) cil managed noinlining
  {
    // Code size       26 (0x1a)
    .maxstack  3
    IL_0000:  ldarga.s   x
    IL_0002:  ldarga.s   large
    IL_0004:  ldarga.s   acc
    IL_0006:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_000b:  ldarg.0
    IL_000c:  ldarg.2
    IL_000d:  ldsfld     native int Program::s_calcStaticCalliRetbuf
    IL_0012:  tail.
    IL_0014:  calli      valuetype S32(int32,int32)
    IL_0019:  ret
  } // end of method Program::CalcStaticCalliRetbufOther

  .method private hidebysig static void  CalcStaticVoid(int32 x,
                                                        int32 acc) cil managed noinlining
  {
    // Code size       40 (0x28)
    .maxstack  3
    .locals init (valuetype S32 V_0)
    IL_0000:  ldarg.0
    IL_0001:  brtrue.s   IL_000a

    IL_0003:  ldarg.1
    IL_0004:  stsfld     int32 Program::s_result
    IL_0009:  ret

    IL_000a:  ldloca.s   V_0
    IL_000c:  initobj    S32
    IL_0012:  ldarga.s   x
    IL_0014:  ldloca.s   V_0
    IL_0016:  ldarga.s   acc
    IL_0018:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_001d:  ldarg.0
    IL_001e:  ldloc.0
    IL_001f:  ldarg.1
    IL_0020:  tail.
    IL_0022:  call       void Program::CalcStaticVoidOther(int32,
                                                           valuetype S32,
                                                           int32)
    IL_0027:  ret
  } // end of method Program::CalcStaticVoid

  .method private hidebysig static void  CalcStaticVoidOther(int32 x,
                                                             valuetype S32 large,
                                                             int32 acc) cil managed noinlining
  {
    // Code size       21 (0x15)
    .maxstack  8
    IL_0000:  ldarga.s   x
    IL_0002:  ldarga.s   large
    IL_0004:  ldarga.s   acc
    IL_0006:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_000b:  ldarg.0
    IL_000c:  ldarg.2
    IL_000d:  tail.
    IL_000f:  call       void Program::CalcStaticVoid(int32,
                                                      int32)
    IL_0014:  ret
  } // end of method Program::CalcStaticVoidOther

  .method private hidebysig static int32 
          CountUpHeap(int32 left,
                      class HeapInt counter) cil managed noinlining
  {
    // Code size       43 (0x2b)
    .maxstack  4
    .locals init (valuetype S32 V_0)
    IL_0000:  ldarg.0
    IL_0001:  brtrue.s   IL_000a

    IL_0003:  ldarg.1
    IL_0004:  ldfld      int32 HeapInt::Value
    IL_0009:  ret

    IL_000a:  ldarg.0
    IL_000b:  ldc.i4.1
    IL_000c:  sub
    IL_000d:  ldloca.s   V_0
    IL_000f:  initobj    S32
    IL_0015:  ldloc.0
    IL_0016:  ldarg.1
    IL_0017:  ldfld      int32 HeapInt::Value
    IL_001c:  ldc.i4.1
    IL_001d:  add
    IL_001e:  newobj     instance void HeapInt::.ctor(int32)
    IL_0023:  tail.
    IL_0025:  call       int32 Program::CountUpHeapOther(int32,
                                                         valuetype S32,
                                                         class HeapInt)
    IL_002a:  ret
  } // end of method Program::CountUpHeap

  .method private hidebysig static int32 
          CountUpHeapOther(int32 left,
                           valuetype S32 s,
                           class HeapInt counter) cil managed noinlining
  {
    // Code size       34 (0x22)
    .maxstack  3
    IL_0000:  ldarg.0
    IL_0001:  brtrue.s   IL_000a

    IL_0003:  ldarg.2
    IL_0004:  ldfld      int32 HeapInt::Value
    IL_0009:  ret

    IL_000a:  ldarg.0
    IL_000b:  ldc.i4.1
    IL_000c:  sub
    IL_000d:  ldarg.2
    IL_000e:  ldfld      int32 HeapInt::Value
    IL_0013:  ldc.i4.1
    IL_0014:  add
    IL_0015:  newobj     instance void HeapInt::.ctor(int32)
    IL_001a:  tail.
    IL_001c:  call       int32 Program::CountUpHeap(int32,
                                                    class HeapInt)
    IL_0021:  ret
  } // end of method Program::CountUpHeapOther

  .method private hidebysig static void  CountUpRef(int32 left,
                                                    int32& counter) cil managed
  {
    // Code size       31 (0x1f)
    .maxstack  3
    .locals init (valuetype S32 V_0)
    IL_0000:  ldarg.0
    IL_0001:  brtrue.s   IL_0004

    IL_0003:  ret

    IL_0004:  ldarg.1
    IL_0005:  ldarg.1
    IL_0006:  ldind.i4
    IL_0007:  ldc.i4.1
    IL_0008:  add
    IL_0009:  stind.i4
    IL_000a:  ldarg.0
    IL_000b:  ldc.i4.1
    IL_000c:  sub
    IL_000d:  ldloca.s   V_0
    IL_000f:  initobj    S32
    IL_0015:  ldloc.0
    IL_0016:  ldarg.1
    IL_0017:  tail.
    IL_0019:  call       void Program::CountUpRefOther(int32,
                                                       valuetype S32,
                                                       int32&)
    IL_001e:  ret
  } // end of method Program::CountUpRef

  .method private hidebysig static void  CountUpRefOther(int32 left,
                                                         valuetype S32 s,
                                                         int32& counter) cil managed
  {
    // Code size       22 (0x16)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  brtrue.s   IL_0004

    IL_0003:  ret

    IL_0004:  ldarg.2
    IL_0005:  ldarg.2
    IL_0006:  ldind.i4
    IL_0007:  ldc.i4.1
    IL_0008:  add
    IL_0009:  stind.i4
    IL_000a:  ldarg.0
    IL_000b:  ldc.i4.1
    IL_000c:  sub
    IL_000d:  ldarg.2
    IL_000e:  tail.
    IL_0010:  call       void Program::CountUpRef(int32,
                                                  int32&)
    IL_0015:  ret
  } // end of method Program::CountUpRefOther

  .method private hidebysig static string 
          GenName1Forward<T>(!!T x) cil managed noinlining
  {
    // Code size       18 (0x12)
    .maxstack  2
    .locals init (valuetype S32 V_0)
    IL_0000:  ldloca.s   V_0
    IL_0002:  initobj    S32
    IL_0008:  ldloc.0
    IL_0009:  ldarg.0
    IL_000a:  tail.
    IL_000c:  call       string Program::GenName1<!!0>(valuetype S32,
                                                       !!0)
    IL_0011:  ret
  } // end of method Program::GenName1Forward

  .method private hidebysig static string 
          GenName1<T>(valuetype S32 s,
                      !!T x) cil managed noinlining
  {
    // Code size       32 (0x20)
    .maxstack  8
    IL_0000:  ldstr      "{0} {1}"
    IL_0005:  ldtoken    !!T
    IL_000a:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_000f:  callvirt   instance string [System.Runtime]System.Type::get_FullName()
    IL_0014:  ldarg.1
    IL_0015:  box        !!T
    IL_001a:  call       string [System.Runtime]System.String::Format(string,
                                                                      object,
                                                                      object)
    IL_001f:  ret
  } // end of method Program::GenName1

  .method private hidebysig static string 
          GenName2ForwardBoth<T1,T2>(!!T1 x,
                                     !!T2 y) cil managed noinlining
  {
    // Code size       19 (0x13)
    .maxstack  3
    .locals init (valuetype S32 V_0)
    IL_0000:  ldloca.s   V_0
    IL_0002:  initobj    S32
    IL_0008:  ldloc.0
    IL_0009:  ldarg.0
    IL_000a:  ldarg.1
    IL_000b:  tail.
    IL_000d:  call       string Program::GenName2<!!0,!!1>(valuetype S32,
                                                           !!0,
                                                           !!1)
    IL_0012:  ret
  } // end of method Program::GenName2ForwardBoth

  .method private hidebysig static string 
          GenName2ForwardOne<T>(!!T x,
                                string y) cil managed noinlining
  {
    // Code size       19 (0x13)
    .maxstack  3
    .locals init (valuetype S32 V_0)
    IL_0000:  ldloca.s   V_0
    IL_0002:  initobj    S32
    IL_0008:  ldloc.0
    IL_0009:  ldarg.0
    IL_000a:  ldarg.1
    IL_000b:  tail.
    IL_000d:  call       string Program::GenName2<!!0,string>(valuetype S32,
                                                              !!0,
                                                              !!1)
    IL_0012:  ret
  } // end of method Program::GenName2ForwardOne

  .method private hidebysig static string 
          GenName2ForwardNone(object x,
                              string y) cil managed noinlining
  {
    // Code size       19 (0x13)
    .maxstack  3
    .locals init (valuetype S32 V_0)
    IL_0000:  ldloca.s   V_0
    IL_0002:  initobj    S32
    IL_0008:  ldloc.0
    IL_0009:  ldarg.0
    IL_000a:  ldarg.1
    IL_000b:  tail.
    IL_000d:  call       string Program::GenName2<object,string>(valuetype S32,
                                                                 !!0,
                                                                 !!1)
    IL_0012:  ret
  } // end of method Program::GenName2ForwardNone

  .method private hidebysig static string 
          GenName2<T1,T2>(valuetype S32 s,
                          !!T1 a,
                          !!T2 b) cil managed noinlining
  {
    // Code size       71 (0x47)
    .maxstack  5
    IL_0000:  ldstr      "{0} {1} {2} {3}"
    IL_0005:  ldc.i4.4
    IL_0006:  newarr     [System.Runtime]System.Object
    IL_000b:  dup
    IL_000c:  ldc.i4.0
    IL_000d:  ldtoken    !!T1
    IL_0012:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_0017:  callvirt   instance string [System.Runtime]System.Type::get_FullName()
    IL_001c:  stelem.ref
    IL_001d:  dup
    IL_001e:  ldc.i4.1
    IL_001f:  ldtoken    !!T2
    IL_0024:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_0029:  callvirt   instance string [System.Runtime]System.Type::get_FullName()
    IL_002e:  stelem.ref
    IL_002f:  dup
    IL_0030:  ldc.i4.2
    IL_0031:  ldarg.1
    IL_0032:  box        !!T1
    IL_0037:  stelem.ref
    IL_0038:  dup
    IL_0039:  ldc.i4.3
    IL_003a:  ldarg.2
    IL_003b:  box        !!T2
    IL_0040:  stelem.ref
    IL_0041:  call       string [System.Runtime]System.String::Format(string,
                                                                      object[])
    IL_0046:  ret
  } // end of method Program::GenName2

  .method private hidebysig static string 
          GenInterfaceForwardF<T1,T2,T3,T4>(!!T1 a,
                                            !!T2 b,
                                            !!T3 c,
                                            !!T4 d,
                                            class IGenInterface`2<!!T1,!!T2> igen) cil managed noinlining
  {
    // Code size       23 (0x17)
    .maxstack  6
    .locals init (valuetype S32 V_0)
    IL_0000:  ldarg.s    igen
    IL_0002:  ldloca.s   V_0
    IL_0004:  initobj    S32
    IL_000a:  ldloc.0
    IL_000b:  ldarg.0
    IL_000c:  ldarg.1
    IL_000d:  ldarg.2
    IL_000e:  ldarg.3
    IL_000f:  tail.
    IL_0011:  callvirt   instance string class IGenInterface`2<!!0,!!1>::F<!!2,!!3>(valuetype S32,
                                                                                    !0,
                                                                                    !1,
                                                                                    !!0,
                                                                                    !!1)
    IL_0016:  ret
  } // end of method Program::GenInterfaceForwardF

  .method private hidebysig static string 
          GenInterfaceForwardG<T1,T2>(!!T1 a,
                                      !!T2 b,
                                      class IGenInterface`2<!!T1,!!T2> igen) cil managed noinlining
  {
    // Code size       20 (0x14)
    .maxstack  4
    .locals init (valuetype S32 V_0)
    IL_0000:  ldarg.2
    IL_0001:  ldloca.s   V_0
    IL_0003:  initobj    S32
    IL_0009:  ldloc.0
    IL_000a:  ldarg.0
    IL_000b:  ldarg.1
    IL_000c:  tail.
    IL_000e:  callvirt   instance string class IGenInterface`2<!!T1,!!T2>::G(valuetype S32,
                                                                             !0,
                                                                             !1)
    IL_0013:  ret
  } // end of method Program::GenInterfaceForwardG

  .method private hidebysig static string 
          GenInterfaceForwardNone(string a,
                                  object b,
                                  int32 c,
                                  object d,
                                  class IGenInterface`2<string,object> igen) cil managed noinlining
  {
    // Code size       23 (0x17)
    .maxstack  6
    .locals init (valuetype S32 V_0)
    IL_0000:  ldarg.s    igen
    IL_0002:  ldloca.s   V_0
    IL_0004:  initobj    S32
    IL_000a:  ldloc.0
    IL_000b:  ldarg.0
    IL_000c:  ldarg.1
    IL_000d:  ldarg.2
    IL_000e:  ldarg.3
    IL_000f:  tail.
    IL_0011:  callvirt   instance string class IGenInterface`2<string,object>::F<int32,object>(valuetype S32,
                                                                                               !0,
                                                                                               !1,
                                                                                               !!0,
                                                                                               !!1)
    IL_0016:  ret
  } // end of method Program::GenInterfaceForwardNone

  .method private hidebysig static string 
          GenInterfaceForward2(string a,
                               object b,
                               class IGenInterface`2<string,object> igen) cil managed noinlining
  {
    // Code size       20 (0x14)
    .maxstack  4
    .locals init (valuetype S32 V_0)
    IL_0000:  ldarg.2
    IL_0001:  ldloca.s   V_0
    IL_0003:  initobj    S32
    IL_0009:  ldloc.0
    IL_000a:  ldarg.0
    IL_000b:  ldarg.1
    IL_000c:  tail.
    IL_000e:  callvirt   instance string class IGenInterface`2<string,object>::G(valuetype S32,
                                                                                 !0,
                                                                                 !1)
    IL_0013:  ret
  } // end of method Program::GenInterfaceForward2

  .method private hidebysig static string 
          GenAbstractFString(class GenAbstract`1<string> ga) cil managed noinlining
  {
    // Code size       9 (0x9)
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  tail.
    IL_0003:  callvirt   instance string class GenAbstract`1<string>::F<object>()
    IL_0008:  ret
  } // end of method Program::GenAbstractFString

  .method private hidebysig static string 
          GenAbstractGString(class GenAbstract`1<string> ga) cil managed noinlining
  {
    // Code size       9 (0x9)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  tail.
    IL_0003:  callvirt   instance string class GenAbstract`1<string>::G()
    IL_0008:  ret
  } // end of method Program::GenAbstractGString

  .method private hidebysig static string 
          GenAbstractFInt(class GenAbstract`1<int32> ga) cil managed noinlining
  {
    // Code size       9 (0x9)
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  tail.
    IL_0003:  callvirt   instance string class GenAbstract`1<int32>::F<object>()
    IL_0008:  ret
  } // end of method Program::GenAbstractFInt

  .method private hidebysig static string 
          GenAbstractGInt(class GenAbstract`1<int32> ga) cil managed noinlining
  {
    // Code size       9 (0x9)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  tail.
    IL_0003:  callvirt   instance string class GenAbstract`1<int32>::G()
    IL_0008:  ret
  } // end of method Program::GenAbstractGInt

  .method private hidebysig static int32 
          InstantiatingStub1<T>(int32 a,
                                int32 r,
                                !!T c,
                                valuetype [System.Runtime]System.Span`1<int32> d) cil managed noinlining
  {
    // Code size       31 (0x1f)
    .maxstack  14
    IL_0000:  ldarg.2
    IL_0001:  ldarg.2
    IL_0002:  ldarg.2
    IL_0003:  ldarg.2
    IL_0004:  ldarg.2
    IL_0005:  ldarg.2
    IL_0006:  ldarg.2
    IL_0007:  ldarg.2
    IL_0008:  ldarg.0
    IL_0009:  ldarg.1
    IL_000a:  ldarg.3
    IL_000b:  ldarg.1
    IL_000c:  ldarga.s   d
    IL_000e:  ldc.i4.s   99
    IL_0010:  call       instance !0& valuetype [System.Runtime]System.Span`1<int32>::get_Item(int32)
    IL_0015:  ldind.i4
    IL_0016:  add
    IL_0017:  tail.
    IL_0019:  call       int32 Program::InstantiatingStub1Other<!!0>(!!0,
                                                                     !!0,
                                                                     !!0,
                                                                     !!0,
                                                                     !!0,
                                                                     !!0,
                                                                     !!0,
                                                                     !!0,
                                                                     int32,
                                                                     int32,
                                                                     valuetype [System.Runtime]System.Span`1<int32>,
                                                                     int32)
    IL_001e:  ret
  } // end of method Program::InstantiatingStub1

  .method private hidebysig static int32 
          InstantiatingStub1Other<T>(!!T c0,
                                     !!T c1,
                                     !!T c2,
                                     !!T c3,
                                     !!T c4,
                                     !!T c5,
                                     !!T c6,
                                     !!T c7,
                                     int32 a,
                                     int32 r,
                                     valuetype [System.Runtime]System.Span`1<int32> d,
                                     int32 result) cil managed noinlining
  {
    // Code size       31 (0x1f)
    .maxstack  4
    IL_0000:  ldarg.s    a
    IL_0002:  ldarga.s   d
    IL_0004:  call       instance int32 valuetype [System.Runtime]System.Span`1<int32>::get_Length()
    IL_0009:  bne.un.s   IL_000e

    IL_000b:  ldarg.s    result
    IL_000d:  ret

    IL_000e:  ldarg.s    a
    IL_0010:  ldc.i4.1
    IL_0011:  add
    IL_0012:  ldarg.s    result
    IL_0014:  ldarg.0
    IL_0015:  ldarg.s    d
    IL_0017:  tail.
    IL_0019:  call       int32 Program::InstantiatingStub1<!!0>(int32,
                                                                int32,
                                                                !!0,
                                                                valuetype [System.Runtime]System.Span`1<int32>)
    IL_001e:  ret
  } // end of method Program::InstantiatingStub1Other

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Program::.ctor

} // end of class Program

.class private auto ansi Instance
       extends [System.Runtime]System.Object
{
  .field private static initonly native int s_calcInstanceCalli
  .field private static initonly native int s_calcInstanceCalliOther
  .field private static initonly native int s_calcInstanceCalliRetbuf
  .field private static initonly native int s_calcInstanceCalliRetbufOther
  .field private int32 _x
  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       53 (0x35)
    .maxstack  1
    .locals init (native int V_0,
             native int V_1,
             native int V_2,
             native int V_3)
    IL_0000:  ldftn      instance int32 Instance::CalcInstanceCalli(int32,
                                                                    int32)
    IL_0006:  stloc.0
    IL_0007:  ldftn      instance int32 Instance::CalcInstanceCalliOther(int32,
                                                                         valuetype S32,
                                                                         int32)
    IL_000d:  stloc.1
    IL_000e:  ldftn      instance valuetype S32 Instance::CalcInstanceCalliRetbuf(int32,
                                                                                  int32)
    IL_0014:  stloc.2
    IL_0015:  ldftn      instance valuetype S32 Instance::CalcInstanceCalliRetbufOther(int32,
                                                                                       valuetype S32,
                                                                                       int32)
    IL_001b:  stloc.3
    IL_001c:  ldloc.0
    IL_001d:  stsfld     native int Instance::s_calcInstanceCalli
    IL_0022:  ldloc.1
    IL_0023:  stsfld     native int Instance::s_calcInstanceCalliOther
    IL_0028:  ldloc.2
    IL_0029:  stsfld     native int Instance::s_calcInstanceCalliRetbuf
    IL_002e:  ldloc.3
    IL_002f:  stsfld     native int Instance::s_calcInstanceCalliRetbufOther
    IL_0034:  ret
  } // end of method Instance::.cctor

  .method public hidebysig instance int32 
          CalcInstance(int32 x,
                       int32 acc) cil managed noinlining
  {
    // Code size       64 (0x40)
    .maxstack  4
    .locals init (valuetype S32 V_0)
    IL_0000:  ldarg.1
    IL_0001:  ldc.i4     0xffed2979
    IL_0006:  beq.s      IL_000f

    IL_0008:  ldarg.0
    IL_0009:  ldarg.1
    IL_000a:  stfld      int32 Instance::_x
    IL_000f:  ldarg.0
    IL_0010:  ldfld      int32 Instance::_x
    IL_0015:  brtrue.s   IL_0019

    IL_0017:  ldarg.2
    IL_0018:  ret

    IL_0019:  ldloca.s   V_0
    IL_001b:  initobj    S32
    IL_0021:  ldarg.0
    IL_0022:  ldflda     int32 Instance::_x
    IL_0027:  ldloca.s   V_0
    IL_0029:  ldarga.s   acc
    IL_002b:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_0030:  ldarg.0
    IL_0031:  ldc.i4     0xffed2979
    IL_0036:  ldloc.0
    IL_0037:  ldarg.2
    IL_0038:  tail.
    IL_003a:  callvirt   instance int32 Instance::CalcInstanceOther(int32,
                                                                    valuetype S32,
                                                                    int32)
    IL_003f:  ret
  } // end of method Instance::CalcInstance

  .method public hidebysig instance int32 
          CalcInstanceOther(int32 x,
                            valuetype S32 large,
                            int32 acc) cil managed noinlining
  {
    // Code size       30 (0x1e)
    .maxstack  3
    IL_0000:  ldarg.0
    IL_0001:  ldflda     int32 Instance::_x
    IL_0006:  ldarga.s   large
    IL_0008:  ldarga.s   acc
    IL_000a:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_000f:  ldarg.0
    IL_0010:  ldc.i4     0xffed2979
    IL_0015:  ldarg.3
    IL_0016:  tail.
    IL_0018:  callvirt   instance int32 Instance::CalcInstance(int32,
                                                               int32)
    IL_001d:  ret
  } // end of method Instance::CalcInstanceOther

  .method public hidebysig instance valuetype S32 
          CalcInstanceRetbuf(int32 x,
                             int32 acc) cil managed noinlining
  {
    // Code size       108 (0x6c)
    .maxstack  4
    .locals init (valuetype S32 V_0,
             valuetype S32 V_1)
    IL_0000:  ldarg.1
    IL_0001:  ldc.i4     0xffed2979
    IL_0006:  beq.s      IL_000f

    IL_0008:  ldarg.0
    IL_0009:  ldarg.1
    IL_000a:  stfld      int32 Instance::_x
    IL_000f:  ldarg.0
    IL_0010:  ldfld      int32 Instance::_x
    IL_0015:  brtrue.s   IL_0045

    IL_0017:  ldloca.s   V_1
    IL_0019:  initobj    S32
    IL_001f:  ldloca.s   V_1
    IL_0021:  ldarg.2
    IL_0022:  conv.i8
    IL_0023:  stfld      int64 S32::A
    IL_0028:  ldloca.s   V_1
    IL_002a:  ldarg.2
    IL_002b:  conv.i8
    IL_002c:  stfld      int64 S32::B
    IL_0031:  ldloca.s   V_1
    IL_0033:  ldarg.2
    IL_0034:  conv.i8
    IL_0035:  stfld      int64 S32::C
    IL_003a:  ldloca.s   V_1
    IL_003c:  ldarg.2
    IL_003d:  conv.i8
    IL_003e:  stfld      int64 S32::D
    IL_0043:  ldloc.1
    IL_0044:  ret

    IL_0045:  ldloca.s   V_0
    IL_0047:  initobj    S32
    IL_004d:  ldarg.0
    IL_004e:  ldflda     int32 Instance::_x
    IL_0053:  ldloca.s   V_0
    IL_0055:  ldarga.s   acc
    IL_0057:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_005c:  ldarg.0
    IL_005d:  ldc.i4     0xffed2979
    IL_0062:  ldloc.0
    IL_0063:  ldarg.2
    IL_0064:  tail.
    IL_0066:  callvirt   instance valuetype S32 Instance::CalcInstanceRetbufOther(int32,
                                                                                  valuetype S32,
                                                                                  int32)
    IL_006b:  ret
  } // end of method Instance::CalcInstanceRetbuf

  .method public hidebysig instance valuetype S32 
          CalcInstanceRetbufOther(int32 x,
                                  valuetype S32 large,
                                  int32 acc) cil managed noinlining
  {
    // Code size       30 (0x1e)
    .maxstack  3
    IL_0000:  ldarg.0
    IL_0001:  ldflda     int32 Instance::_x
    IL_0006:  ldarga.s   large
    IL_0008:  ldarga.s   acc
    IL_000a:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_000f:  ldarg.0
    IL_0010:  ldc.i4     0xffed2979
    IL_0015:  ldarg.3
    IL_0016:  tail.
    IL_0018:  callvirt   instance valuetype S32 Instance::CalcInstanceRetbuf(int32,
                                                                             int32)
    IL_001d:  ret
  } // end of method Instance::CalcInstanceRetbufOther

  .method public hidebysig instance int32 
          CalcInstanceCalli(int32 x,
                            int32 acc) cil managed noinlining
  {
    // Code size       69 (0x45)
    .maxstack  5
    .locals init (valuetype S32 V_0)
    IL_0000:  ldarg.1
    IL_0001:  ldc.i4     0xffed2979
    IL_0006:  beq.s      IL_000f

    IL_0008:  ldarg.0
    IL_0009:  ldarg.1
    IL_000a:  stfld      int32 Instance::_x
    IL_000f:  ldarg.0
    IL_0010:  ldfld      int32 Instance::_x
    IL_0015:  brtrue.s   IL_0019

    IL_0017:  ldarg.2
    IL_0018:  ret

    IL_0019:  ldloca.s   V_0
    IL_001b:  initobj    S32
    IL_0021:  ldarg.0
    IL_0022:  ldflda     int32 Instance::_x
    IL_0027:  ldloca.s   V_0
    IL_0029:  ldarga.s   acc
    IL_002b:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_0030:  ldarg.0
    IL_0031:  ldc.i4     0xffed2979
    IL_0036:  ldloc.0
    IL_0037:  ldarg.2
    IL_0038:  ldsfld     native int Instance::s_calcInstanceCalliOther
    IL_003d:  tail.
    IL_003f:  calli      instance int32(int32,valuetype S32,int32)
    IL_0044:  ret
  } // end of method Instance::CalcInstanceCalli

  .method public hidebysig instance int32 
          CalcInstanceCalliOther(int32 x,
                                 valuetype S32 large,
                                 int32 acc) cil managed noinlining
  {
    // Code size       35 (0x23)
    .maxstack  4
    IL_0000:  ldarg.0
    IL_0001:  ldflda     int32 Instance::_x
    IL_0006:  ldarga.s   large
    IL_0008:  ldarga.s   acc
    IL_000a:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_000f:  ldarg.0
    IL_0010:  ldc.i4     0xffed2979
    IL_0015:  ldarg.3
    IL_0016:  ldsfld     native int Instance::s_calcInstanceCalli
    IL_001b:  tail.
    IL_001d:  calli      instance int32(int32,int32)
    IL_0022:  ret
  } // end of method Instance::CalcInstanceCalliOther

  .method public hidebysig instance valuetype S32 
          CalcInstanceCalliRetbuf(int32 x,
                                  int32 acc) cil managed noinlining
  {
    // Code size       113 (0x71)
    .maxstack  5
    .locals init (valuetype S32 V_0,
             valuetype S32 V_1)
    IL_0000:  ldarg.1
    IL_0001:  ldc.i4     0xffed2979
    IL_0006:  beq.s      IL_000f

    IL_0008:  ldarg.0
    IL_0009:  ldarg.1
    IL_000a:  stfld      int32 Instance::_x
    IL_000f:  ldarg.0
    IL_0010:  ldfld      int32 Instance::_x
    IL_0015:  brtrue.s   IL_0045

    IL_0017:  ldloca.s   V_1
    IL_0019:  initobj    S32
    IL_001f:  ldloca.s   V_1
    IL_0021:  ldarg.2
    IL_0022:  conv.i8
    IL_0023:  stfld      int64 S32::A
    IL_0028:  ldloca.s   V_1
    IL_002a:  ldarg.2
    IL_002b:  conv.i8
    IL_002c:  stfld      int64 S32::B
    IL_0031:  ldloca.s   V_1
    IL_0033:  ldarg.2
    IL_0034:  conv.i8
    IL_0035:  stfld      int64 S32::C
    IL_003a:  ldloca.s   V_1
    IL_003c:  ldarg.2
    IL_003d:  conv.i8
    IL_003e:  stfld      int64 S32::D
    IL_0043:  ldloc.1
    IL_0044:  ret

    IL_0045:  ldloca.s   V_0
    IL_0047:  initobj    S32
    IL_004d:  ldarg.0
    IL_004e:  ldflda     int32 Instance::_x
    IL_0053:  ldloca.s   V_0
    IL_0055:  ldarga.s   acc
    IL_0057:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_005c:  ldarg.0
    IL_005d:  ldc.i4     0xffed2979
    IL_0062:  ldloc.0
    IL_0063:  ldarg.2
    IL_0064:  ldsfld     native int Instance::s_calcInstanceCalliRetbufOther
    IL_0069:  tail.
    IL_006b:  calli      instance valuetype S32(int32,valuetype S32,int32)
    IL_0070:  ret
  } // end of method Instance::CalcInstanceCalliRetbuf

  .method public hidebysig instance valuetype S32 
          CalcInstanceCalliRetbufOther(int32 x,
                                       valuetype S32 large,
                                       int32 acc) cil managed noinlining
  {
    // Code size       35 (0x23)
    .maxstack  4
    IL_0000:  ldarg.0
    IL_0001:  ldflda     int32 Instance::_x
    IL_0006:  ldarga.s   large
    IL_0008:  ldarga.s   acc
    IL_000a:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_000f:  ldarg.0
    IL_0010:  ldc.i4     0xffed2979
    IL_0015:  ldarg.3
    IL_0016:  ldsfld     native int Instance::s_calcInstanceCalliRetbuf
    IL_001b:  tail.
    IL_001d:  calli      instance valuetype S32(int32,int32)
    IL_0022:  ret
  } // end of method Instance::CalcInstanceCalliRetbufOther

  .method public hidebysig instance string 
          GC(object a,
             int32 b,
             object c,
             object d,
             string e,
             string f,
             object g,
             int32 h,
             int32& interior) cil managed noinlining
  {
    // Code size       111 (0x6f)
    .maxstack  5
    .locals init (valuetype S32 V_0,
             valuetype SGC2 V_1)
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  ldloca.s   V_0
    IL_0004:  initobj    S32
    IL_000a:  ldloc.0
    IL_000b:  ldloca.s   V_1
    IL_000d:  initobj    SGC2
    IL_0013:  ldloca.s   V_1
    IL_0015:  ldarg.2
    IL_0016:  stfld      int32 SGC2::A
    IL_001b:  ldloca.s   V_1
    IL_001d:  ldflda     valuetype SGC SGC2::B
    IL_0022:  ldarg.3
    IL_0023:  stfld      object SGC::A
    IL_0028:  ldloca.s   V_1
    IL_002a:  ldflda     valuetype SGC SGC2::B
    IL_002f:  ldarg.s    d
    IL_0031:  stfld      object SGC::B
    IL_0036:  ldloca.s   V_1
    IL_0038:  ldflda     valuetype SGC SGC2::B
    IL_003d:  ldarg.s    e
    IL_003f:  stfld      string SGC::C
    IL_0044:  ldloca.s   V_1
    IL_0046:  ldflda     valuetype SGC SGC2::B
    IL_004b:  ldarg.s    f
    IL_004d:  stfld      string SGC::D
    IL_0052:  ldloca.s   V_1
    IL_0054:  ldarg.s    g
    IL_0056:  stfld      object SGC2::C
    IL_005b:  ldloca.s   V_1
    IL_005d:  ldarg.s    h
    IL_005f:  stfld      int32 SGC2::D
    IL_0064:  ldloc.1
    IL_0065:  ldarg.s    interior
    IL_0067:  tail.
    IL_0069:  call       instance string Instance::GCOther(object,
                                                           valuetype S32,
                                                           valuetype SGC2,
                                                           int32&)
    IL_006e:  ret
  } // end of method Instance::GC

  .method private hidebysig instance string 
          GCOther(object a,
                  valuetype S32 s,
                  valuetype SGC2 gc,
                  int32& interior) cil managed noinlining
  {
    // Code size       126 (0x7e)
    .maxstack  5
    IL_0000:  ldstr      "{0} {1} {2} {3} {4} {5} {6} {7} {8}"
    IL_0005:  ldc.i4.s   9
    IL_0007:  newarr     [System.Runtime]System.Object
    IL_000c:  dup
    IL_000d:  ldc.i4.0
    IL_000e:  ldarg.1
    IL_000f:  stelem.ref
    IL_0010:  dup
    IL_0011:  ldc.i4.1
    IL_0012:  ldarg.3
    IL_0013:  ldfld      int32 SGC2::A
    IL_0018:  box        [System.Runtime]System.Int32
    IL_001d:  stelem.ref
    IL_001e:  dup
    IL_001f:  ldc.i4.2
    IL_0020:  ldarg.3
    IL_0021:  ldfld      valuetype SGC SGC2::B
    IL_0026:  ldfld      object SGC::A
    IL_002b:  stelem.ref
    IL_002c:  dup
    IL_002d:  ldc.i4.3
    IL_002e:  ldarg.3
    IL_002f:  ldfld      valuetype SGC SGC2::B
    IL_0034:  ldfld      object SGC::B
    IL_0039:  stelem.ref
    IL_003a:  dup
    IL_003b:  ldc.i4.4
    IL_003c:  ldarg.3
    IL_003d:  ldfld      valuetype SGC SGC2::B
    IL_0042:  ldfld      string SGC::C
    IL_0047:  stelem.ref
    IL_0048:  dup
    IL_0049:  ldc.i4.5
    IL_004a:  ldarg.3
    IL_004b:  ldfld      valuetype SGC SGC2::B
    IL_0050:  ldfld      string SGC::D
    IL_0055:  stelem.ref
    IL_0056:  dup
    IL_0057:  ldc.i4.6
    IL_0058:  ldarg.3
    IL_0059:  ldfld      object SGC2::C
    IL_005e:  stelem.ref
    IL_005f:  dup
    IL_0060:  ldc.i4.7
    IL_0061:  ldarg.3
    IL_0062:  ldfld      int32 SGC2::D
    IL_0067:  box        [System.Runtime]System.Int32
    IL_006c:  stelem.ref
    IL_006d:  dup
    IL_006e:  ldc.i4.8
    IL_006f:  ldarg.s    interior
    IL_0071:  ldind.i4
    IL_0072:  box        [System.Runtime]System.Int32
    IL_0077:  stelem.ref
    IL_0078:  call       string [System.Runtime]System.String::Format(string,
                                                                      object[])
    IL_007d:  ret
  } // end of method Instance::GCOther

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Instance::.ctor

} // end of class Instance

.class private sequential ansi sealed beforefieldinit InstanceValueType
       extends [System.Runtime]System.ValueType
{
  .field public int32 Count
  .method public hidebysig instance void 
          CountUp(int32 left) cil managed
  {
    // Code size       39 (0x27)
    .maxstack  3
    .locals init (valuetype S32 V_0)
    IL_0000:  ldarg.1
    IL_0001:  brtrue.s   IL_0004

    IL_0003:  ret

    IL_0004:  ldarg.0
    IL_0005:  ldarg.0
    IL_0006:  ldfld      int32 InstanceValueType::Count
    IL_000b:  ldc.i4.1
    IL_000c:  add
    IL_000d:  stfld      int32 InstanceValueType::Count
    IL_0012:  ldarg.0
    IL_0013:  ldarg.1
    IL_0014:  ldc.i4.1
    IL_0015:  sub
    IL_0016:  ldloca.s   V_0
    IL_0018:  initobj    S32
    IL_001e:  ldloc.0
    IL_001f:  tail.
    IL_0021:  call       instance void InstanceValueType::CountUpOther(int32,
                                                                       valuetype S32)
    IL_0026:  ret
  } // end of method InstanceValueType::CountUp

  .method private hidebysig instance void 
          CountUpOther(int32 left,
                       valuetype S32 s) cil managed
  {
    // Code size       30 (0x1e)
    .maxstack  3
    IL_0000:  ldarg.1
    IL_0001:  brtrue.s   IL_0004

    IL_0003:  ret

    IL_0004:  ldarg.0
    IL_0005:  ldarg.0
    IL_0006:  ldfld      int32 InstanceValueType::Count
    IL_000b:  ldc.i4.1
    IL_000c:  add
    IL_000d:  stfld      int32 InstanceValueType::Count
    IL_0012:  ldarg.0
    IL_0013:  ldarg.1
    IL_0014:  ldc.i4.1
    IL_0015:  sub
    IL_0016:  tail.
    IL_0018:  call       instance void InstanceValueType::CountUp(int32)
    IL_001d:  ret
  } // end of method InstanceValueType::CountUpOther

} // end of class InstanceValueType

.class private abstract auto ansi beforefieldinit BaseClass
       extends [System.Runtime]System.Object
{
  .method public hidebysig newslot abstract virtual 
          instance int32  CalcAbstract(int32 x,
                                       int32 acc) cil managed
  {
  } // end of method BaseClass::CalcAbstract

  .method public hidebysig newslot abstract virtual 
          instance int32  CalcAbstractOther(int32 x,
                                            valuetype S32 large,
                                            int32 acc) cil managed
  {
  } // end of method BaseClass::CalcAbstractOther

  .method public hidebysig newslot abstract virtual 
          instance valuetype S32  CalcAbstractRetbuf(int32 x,
                                                     int32 acc) cil managed
  {
  } // end of method BaseClass::CalcAbstractRetbuf

  .method public hidebysig newslot abstract virtual 
          instance valuetype S32  CalcAbstractRetbufOther(int32 x,
                                                          valuetype S32 large,
                                                          int32 acc) cil managed
  {
  } // end of method BaseClass::CalcAbstractRetbufOther

  .method family hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  ret
  } // end of method BaseClass::.ctor

} // end of class BaseClass

.class private auto ansi beforefieldinit ClassImpl
       extends BaseClass
{
  .field private int32 _x
  .field private class BaseClass '<Other>k__BackingField'
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
  .method public hidebysig specialname instance class BaseClass 
          get_Other() cil managed
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      class BaseClass ClassImpl::'<Other>k__BackingField'
    IL_0006:  ret
  } // end of method ClassImpl::get_Other

  .method public hidebysig specialname instance void 
          set_Other(class BaseClass 'value') cil managed
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  stfld      class BaseClass ClassImpl::'<Other>k__BackingField'
    IL_0007:  ret
  } // end of method ClassImpl::set_Other

  .method public hidebysig virtual instance int32 
          CalcAbstract(int32 x,
                       int32 acc) cil managed noinlining
  {
    // Code size       69 (0x45)
    .maxstack  4
    .locals init (valuetype S32 V_0)
    IL_0000:  ldarg.1
    IL_0001:  ldc.i4     0xffed2979
    IL_0006:  beq.s      IL_000f

    IL_0008:  ldarg.0
    IL_0009:  ldarg.1
    IL_000a:  stfld      int32 ClassImpl::_x
    IL_000f:  ldarg.0
    IL_0010:  ldfld      int32 ClassImpl::_x
    IL_0015:  brtrue.s   IL_0019

    IL_0017:  ldarg.2
    IL_0018:  ret

    IL_0019:  ldloca.s   V_0
    IL_001b:  initobj    S32
    IL_0021:  ldarg.0
    IL_0022:  ldflda     int32 ClassImpl::_x
    IL_0027:  ldloca.s   V_0
    IL_0029:  ldarga.s   acc
    IL_002b:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_0030:  ldarg.0
    IL_0031:  call       instance class BaseClass ClassImpl::get_Other()
    IL_0036:  ldc.i4     0xffed2979
    IL_003b:  ldloc.0
    IL_003c:  ldarg.2
    IL_003d:  tail.
    IL_003f:  callvirt   instance int32 ClassImpl::CalcAbstractOther(int32,
                                                                     valuetype S32,
                                                                     int32)
    IL_0044:  ret
  } // end of method ClassImpl::CalcAbstract

  .method public hidebysig virtual instance int32 
          CalcAbstractOther(int32 x,
                            valuetype S32 large,
                            int32 acc) cil managed noinlining
  {
    // Code size       35 (0x23)
    .maxstack  3
    IL_0000:  ldarg.0
    IL_0001:  ldflda     int32 ClassImpl::_x
    IL_0006:  ldarga.s   large
    IL_0008:  ldarga.s   acc
    IL_000a:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_000f:  ldarg.0
    IL_0010:  call       instance class BaseClass ClassImpl::get_Other()
    IL_0015:  ldc.i4     0xffed2979
    IL_001a:  ldarg.3
    IL_001b:  tail.
    IL_001d:  callvirt   instance int32 ClassImpl::CalcAbstract(int32,
                                                                int32)
    IL_0022:  ret
  } // end of method ClassImpl::CalcAbstractOther

  .method public hidebysig virtual instance valuetype S32 
          CalcAbstractRetbuf(int32 x,
                             int32 acc) cil managed noinlining
  {
    // Code size       113 (0x71)
    .maxstack  4
    .locals init (valuetype S32 V_0,
             valuetype S32 V_1)
    IL_0000:  ldarg.1
    IL_0001:  ldc.i4     0xffed2979
    IL_0006:  beq.s      IL_000f

    IL_0008:  ldarg.0
    IL_0009:  ldarg.1
    IL_000a:  stfld      int32 ClassImpl::_x
    IL_000f:  ldarg.0
    IL_0010:  ldfld      int32 ClassImpl::_x
    IL_0015:  brtrue.s   IL_0045

    IL_0017:  ldloca.s   V_1
    IL_0019:  initobj    S32
    IL_001f:  ldloca.s   V_1
    IL_0021:  ldarg.2
    IL_0022:  conv.i8
    IL_0023:  stfld      int64 S32::A
    IL_0028:  ldloca.s   V_1
    IL_002a:  ldarg.2
    IL_002b:  conv.i8
    IL_002c:  stfld      int64 S32::B
    IL_0031:  ldloca.s   V_1
    IL_0033:  ldarg.2
    IL_0034:  conv.i8
    IL_0035:  stfld      int64 S32::C
    IL_003a:  ldloca.s   V_1
    IL_003c:  ldarg.2
    IL_003d:  conv.i8
    IL_003e:  stfld      int64 S32::D
    IL_0043:  ldloc.1
    IL_0044:  ret

    IL_0045:  ldloca.s   V_0
    IL_0047:  initobj    S32
    IL_004d:  ldarg.0
    IL_004e:  ldflda     int32 ClassImpl::_x
    IL_0053:  ldloca.s   V_0
    IL_0055:  ldarga.s   acc
    IL_0057:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_005c:  ldarg.0
    IL_005d:  call       instance class BaseClass ClassImpl::get_Other()
    IL_0062:  ldc.i4     0xffed2979
    IL_0067:  ldloc.0
    IL_0068:  ldarg.2
    IL_0069:  tail.
    IL_006b:  callvirt   instance valuetype S32 ClassImpl::CalcAbstractRetbufOther(int32,
                                                                                   valuetype S32,
                                                                                   int32)
    IL_0070:  ret
  } // end of method ClassImpl::CalcAbstractRetbuf

  .method public hidebysig virtual instance valuetype S32 
          CalcAbstractRetbufOther(int32 x,
                                  valuetype S32 large,
                                  int32 acc) cil managed noinlining
  {
    // Code size       35 (0x23)
    .maxstack  3
    IL_0000:  ldarg.0
    IL_0001:  ldflda     int32 ClassImpl::_x
    IL_0006:  ldarga.s   large
    IL_0008:  ldarga.s   acc
    IL_000a:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_000f:  ldarg.0
    IL_0010:  call       instance class BaseClass ClassImpl::get_Other()
    IL_0015:  ldc.i4     0xffed2979
    IL_001a:  ldarg.3
    IL_001b:  tail.
    IL_001d:  callvirt   instance valuetype S32 ClassImpl::CalcAbstractRetbuf(int32,
                                                                              int32)
    IL_0022:  ret
  } // end of method ClassImpl::CalcAbstractRetbufOther

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void BaseClass::.ctor()
    IL_0006:  ret
  } // end of method ClassImpl::.ctor

  .property instance class BaseClass Other()
  {
    .get instance class BaseClass ClassImpl::get_Other()
    .set instance void ClassImpl::set_Other(class BaseClass)
  } // end of property ClassImpl::Other
} // end of class ClassImpl

.class interface private abstract auto ansi IInterface
{
  .method public hidebysig newslot abstract virtual 
          instance int32  CalcInterface(int32 x,
                                        int32 acc) cil managed
  {
  } // end of method IInterface::CalcInterface

  .method public hidebysig newslot abstract virtual 
          instance int32  CalcInterfaceOther(int32 x,
                                             valuetype S32 large,
                                             int32 acc) cil managed
  {
  } // end of method IInterface::CalcInterfaceOther

  .method public hidebysig newslot abstract virtual 
          instance valuetype S32  CalcInterfaceRetbuf(int32 x,
                                                      int32 acc) cil managed
  {
  } // end of method IInterface::CalcInterfaceRetbuf

  .method public hidebysig newslot abstract virtual 
          instance valuetype S32  CalcInterfaceRetbufOther(int32 x,
                                                           valuetype S32 large,
                                                           int32 acc) cil managed
  {
  } // end of method IInterface::CalcInterfaceRetbufOther

} // end of class IInterface

.class private auto ansi beforefieldinit InterfaceImpl
       extends [System.Runtime]System.Object
       implements IInterface
{
  .field private int32 _x
  .field private class IInterface '<Other>k__BackingField'
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
  .method public hidebysig specialname instance class IInterface 
          get_Other() cil managed
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      class IInterface InterfaceImpl::'<Other>k__BackingField'
    IL_0006:  ret
  } // end of method InterfaceImpl::get_Other

  .method public hidebysig specialname instance void 
          set_Other(class IInterface 'value') cil managed
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  stfld      class IInterface InterfaceImpl::'<Other>k__BackingField'
    IL_0007:  ret
  } // end of method InterfaceImpl::set_Other

  .method public hidebysig newslot virtual final 
          instance int32  CalcInterface(int32 x,
                                        int32 acc) cil managed noinlining
  {
    // Code size       69 (0x45)
    .maxstack  4
    .locals init (valuetype S32 V_0)
    IL_0000:  ldarg.1
    IL_0001:  ldc.i4     0xffed2979
    IL_0006:  beq.s      IL_000f

    IL_0008:  ldarg.0
    IL_0009:  ldarg.1
    IL_000a:  stfld      int32 InterfaceImpl::_x
    IL_000f:  ldarg.0
    IL_0010:  ldfld      int32 InterfaceImpl::_x
    IL_0015:  brtrue.s   IL_0019

    IL_0017:  ldarg.2
    IL_0018:  ret

    IL_0019:  ldloca.s   V_0
    IL_001b:  initobj    S32
    IL_0021:  ldarg.0
    IL_0022:  ldflda     int32 InterfaceImpl::_x
    IL_0027:  ldloca.s   V_0
    IL_0029:  ldarga.s   acc
    IL_002b:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_0030:  ldarg.0
    IL_0031:  call       instance class IInterface InterfaceImpl::get_Other()
    IL_0036:  ldc.i4     0xffed2979
    IL_003b:  ldloc.0
    IL_003c:  ldarg.2
    IL_003d:  tail.
    IL_003f:  callvirt   instance int32 IInterface::CalcInterfaceOther(int32,
                                                                       valuetype S32,
                                                                       int32)
    IL_0044:  ret
  } // end of method InterfaceImpl::CalcInterface

  .method public hidebysig newslot virtual final 
          instance int32  CalcInterfaceOther(int32 x,
                                             valuetype S32 large,
                                             int32 acc) cil managed noinlining
  {
    // Code size       35 (0x23)
    .maxstack  3
    IL_0000:  ldarg.0
    IL_0001:  ldflda     int32 InterfaceImpl::_x
    IL_0006:  ldarga.s   large
    IL_0008:  ldarga.s   acc
    IL_000a:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_000f:  ldarg.0
    IL_0010:  call       instance class IInterface InterfaceImpl::get_Other()
    IL_0015:  ldc.i4     0xffed2979
    IL_001a:  ldarg.3
    IL_001b:  tail.
    IL_001d:  callvirt   instance int32 IInterface::CalcInterface(int32,
                                                                  int32)
    IL_0022:  ret
  } // end of method InterfaceImpl::CalcInterfaceOther

  .method public hidebysig newslot virtual final 
          instance valuetype S32  CalcInterfaceRetbuf(int32 x,
                                                      int32 acc) cil managed noinlining
  {
    // Code size       113 (0x71)
    .maxstack  4
    .locals init (valuetype S32 V_0,
             valuetype S32 V_1)
    IL_0000:  ldarg.1
    IL_0001:  ldc.i4     0xffed2979
    IL_0006:  beq.s      IL_000f

    IL_0008:  ldarg.0
    IL_0009:  ldarg.1
    IL_000a:  stfld      int32 InterfaceImpl::_x
    IL_000f:  ldarg.0
    IL_0010:  ldfld      int32 InterfaceImpl::_x
    IL_0015:  brtrue.s   IL_0045

    IL_0017:  ldloca.s   V_1
    IL_0019:  initobj    S32
    IL_001f:  ldloca.s   V_1
    IL_0021:  ldarg.2
    IL_0022:  conv.i8
    IL_0023:  stfld      int64 S32::A
    IL_0028:  ldloca.s   V_1
    IL_002a:  ldarg.2
    IL_002b:  conv.i8
    IL_002c:  stfld      int64 S32::B
    IL_0031:  ldloca.s   V_1
    IL_0033:  ldarg.2
    IL_0034:  conv.i8
    IL_0035:  stfld      int64 S32::C
    IL_003a:  ldloca.s   V_1
    IL_003c:  ldarg.2
    IL_003d:  conv.i8
    IL_003e:  stfld      int64 S32::D
    IL_0043:  ldloc.1
    IL_0044:  ret

    IL_0045:  ldloca.s   V_0
    IL_0047:  initobj    S32
    IL_004d:  ldarg.0
    IL_004e:  ldflda     int32 InterfaceImpl::_x
    IL_0053:  ldloca.s   V_0
    IL_0055:  ldarga.s   acc
    IL_0057:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_005c:  ldarg.0
    IL_005d:  call       instance class IInterface InterfaceImpl::get_Other()
    IL_0062:  ldc.i4     0xffed2979
    IL_0067:  ldloc.0
    IL_0068:  ldarg.2
    IL_0069:  tail.
    IL_006b:  callvirt   instance valuetype S32 IInterface::CalcInterfaceRetbufOther(int32,
                                                                                     valuetype S32,
                                                                                     int32)
    IL_0070:  ret
  } // end of method InterfaceImpl::CalcInterfaceRetbuf

  .method public hidebysig newslot virtual final 
          instance valuetype S32  CalcInterfaceRetbufOther(int32 x,
                                                           valuetype S32 large,
                                                           int32 acc) cil managed noinlining
  {
    // Code size       35 (0x23)
    .maxstack  3
    IL_0000:  ldarg.0
    IL_0001:  ldflda     int32 InterfaceImpl::_x
    IL_0006:  ldarga.s   large
    IL_0008:  ldarga.s   acc
    IL_000a:  call       void Program::Calc(int32&,
                                            valuetype S32&,
                                            int32&)
    IL_000f:  ldarg.0
    IL_0010:  call       instance class IInterface InterfaceImpl::get_Other()
    IL_0015:  ldc.i4     0xffed2979
    IL_001a:  ldarg.3
    IL_001b:  tail.
    IL_001d:  callvirt   instance valuetype S32 IInterface::CalcInterfaceRetbuf(int32,
                                                                                int32)
    IL_0022:  ret
  } // end of method InterfaceImpl::CalcInterfaceRetbufOther

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  ret
  } // end of method InterfaceImpl::.ctor

  .property instance class IInterface Other()
  {
    .get instance class IInterface InterfaceImpl::get_Other()
    .set instance void InterfaceImpl::set_Other(class IInterface)
  } // end of property InterfaceImpl::Other
} // end of class InterfaceImpl

.class private auto ansi beforefieldinit GenInstance`2<T1,T2>
       extends [System.Runtime]System.Object
{
  .method public hidebysig instance string 
          NonVirtForward<T3,T4>(!T1 a,
                                !T2 b,
                                !!T3 c,
                                !!T4 d) cil managed noinlining
  {
    // Code size       23 (0x17)
    .maxstack  6
    .locals init (valuetype S32 V_0)
    IL_0000:  ldarg.0
    IL_0001:  ldloca.s   V_0
    IL_0003:  initobj    S32
    IL_0009:  ldloc.0
    IL_000a:  ldarg.1
    IL_000b:  ldarg.2
    IL_000c:  ldarg.3
    IL_000d:  ldarg.s    d
    IL_000f:  tail.
    IL_0011:  callvirt   instance string class GenInstance`2<!T1,!T2>::NonVirt<!!0,!!1>(valuetype S32,
                                                                                        !0,
                                                                                        !1,
                                                                                        !!0,
                                                                                        !!1)
    IL_0016:  ret
  } // end of method GenInstance`2::NonVirtForward

  .method public hidebysig instance string 
          VirtForward<T3,T4>(!T1 a,
                             !T2 b,
                             !!T3 c,
                             !!T4 d) cil managed noinlining
  {
    // Code size       23 (0x17)
    .maxstack  6
    .locals init (valuetype S32 V_0)
    IL_0000:  ldarg.0
    IL_0001:  ldloca.s   V_0
    IL_0003:  initobj    S32
    IL_0009:  ldloc.0
    IL_000a:  ldarg.1
    IL_000b:  ldarg.2
    IL_000c:  ldarg.3
    IL_000d:  ldarg.s    d
    IL_000f:  tail.
    IL_0011:  callvirt   instance string class GenInstance`2<!T1,!T2>::Virt<!!0,!!1>(valuetype S32,
                                                                                     !0,
                                                                                     !1,
                                                                                     !!0,
                                                                                     !!1)
    IL_0016:  ret
  } // end of method GenInstance`2::VirtForward

  .method public hidebysig instance string 
          NonVirt<T3,T4>(valuetype S32 s,
                         !T1 a,
                         !T2 b,
                         !!T3 c,
                         !!T4 d) cil managed
  {
    // Code size       127 (0x7f)
    .maxstack  5
    IL_0000:  ldstr      "{0} {1} {2} {3} {4} {5} {6} {7}"
    IL_0005:  ldc.i4.8
    IL_0006:  newarr     [System.Runtime]System.Object
    IL_000b:  dup
    IL_000c:  ldc.i4.0
    IL_000d:  ldtoken    !T1
    IL_0012:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_0017:  callvirt   instance string [System.Runtime]System.Type::get_FullName()
    IL_001c:  stelem.ref
    IL_001d:  dup
    IL_001e:  ldc.i4.1
    IL_001f:  ldtoken    !T2
    IL_0024:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_0029:  callvirt   instance string [System.Runtime]System.Type::get_FullName()
    IL_002e:  stelem.ref
    IL_002f:  dup
    IL_0030:  ldc.i4.2
    IL_0031:  ldtoken    !!T3
    IL_0036:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_003b:  callvirt   instance string [System.Runtime]System.Type::get_FullName()
    IL_0040:  stelem.ref
    IL_0041:  dup
    IL_0042:  ldc.i4.3
    IL_0043:  ldtoken    !!T4
    IL_0048:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_004d:  callvirt   instance string [System.Runtime]System.Type::get_FullName()
    IL_0052:  stelem.ref
    IL_0053:  dup
    IL_0054:  ldc.i4.4
    IL_0055:  ldarg.2
    IL_0056:  box        !T1
    IL_005b:  stelem.ref
    IL_005c:  dup
    IL_005d:  ldc.i4.5
    IL_005e:  ldarg.3
    IL_005f:  box        !T2
    IL_0064:  stelem.ref
    IL_0065:  dup
    IL_0066:  ldc.i4.6
    IL_0067:  ldarg.s    c
    IL_0069:  box        !!T3
    IL_006e:  stelem.ref
    IL_006f:  dup
    IL_0070:  ldc.i4.7
    IL_0071:  ldarg.s    d
    IL_0073:  box        !!T4
    IL_0078:  stelem.ref
    IL_0079:  call       string [System.Runtime]System.String::Format(string,
                                                                      object[])
    IL_007e:  ret
  } // end of method GenInstance`2::NonVirt

  .method public hidebysig newslot virtual 
          instance string  Virt<T3,T4>(valuetype S32 s,
                                       !T1 a,
                                       !T2 b,
                                       !!T3 c,
                                       !!T4 d) cil managed
  {
    // Code size       127 (0x7f)
    .maxstack  5
    IL_0000:  ldstr      "{0} {1} {2} {3} {4} {5} {6} {7}"
    IL_0005:  ldc.i4.8
    IL_0006:  newarr     [System.Runtime]System.Object
    IL_000b:  dup
    IL_000c:  ldc.i4.0
    IL_000d:  ldtoken    !T1
    IL_0012:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_0017:  callvirt   instance string [System.Runtime]System.Type::get_FullName()
    IL_001c:  stelem.ref
    IL_001d:  dup
    IL_001e:  ldc.i4.1
    IL_001f:  ldtoken    !T2
    IL_0024:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_0029:  callvirt   instance string [System.Runtime]System.Type::get_FullName()
    IL_002e:  stelem.ref
    IL_002f:  dup
    IL_0030:  ldc.i4.2
    IL_0031:  ldtoken    !!T3
    IL_0036:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_003b:  callvirt   instance string [System.Runtime]System.Type::get_FullName()
    IL_0040:  stelem.ref
    IL_0041:  dup
    IL_0042:  ldc.i4.3
    IL_0043:  ldtoken    !!T4
    IL_0048:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_004d:  callvirt   instance string [System.Runtime]System.Type::get_FullName()
    IL_0052:  stelem.ref
    IL_0053:  dup
    IL_0054:  ldc.i4.4
    IL_0055:  ldarg.2
    IL_0056:  box        !T1
    IL_005b:  stelem.ref
    IL_005c:  dup
    IL_005d:  ldc.i4.5
    IL_005e:  ldarg.3
    IL_005f:  box        !T2
    IL_0064:  stelem.ref
    IL_0065:  dup
    IL_0066:  ldc.i4.6
    IL_0067:  ldarg.s    c
    IL_0069:  box        !!T3
    IL_006e:  stelem.ref
    IL_006f:  dup
    IL_0070:  ldc.i4.7
    IL_0071:  ldarg.s    d
    IL_0073:  box        !!T4
    IL_0078:  stelem.ref
    IL_0079:  call       string [System.Runtime]System.String::Format(string,
                                                                      object[])
    IL_007e:  ret
  } // end of method GenInstance`2::Virt

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  ret
  } // end of method GenInstance`2::.ctor

} // end of class GenInstance`2

.class interface private abstract auto ansi IGenInterface`2<T1,T2>
{
  .method public hidebysig newslot abstract virtual 
          instance string  F<T3,T4>(valuetype S32 s,
                                    !T1 a,
                                    !T2 b,
                                    !!T3 c,
                                    !!T4 d) cil managed
  {
  } // end of method IGenInterface`2::F

  .method public hidebysig newslot abstract virtual 
          instance string  G(valuetype S32 s,
                             !T1 a,
                             !T2 b) cil managed
  {
  } // end of method IGenInterface`2::G

} // end of class IGenInterface`2

.class private auto ansi beforefieldinit GenInterfaceImpl`2<T1,T2>
       extends [System.Runtime]System.Object
       implements class IGenInterface`2<!T1,!T2>
{
  .method public hidebysig newslot virtual final 
          instance string  F<T3,T4>(valuetype S32 s,
                                    !T1 a,
                                    !T2 b,
                                    !!T3 c,
                                    !!T4 d) cil managed
  {
    // Code size       127 (0x7f)
    .maxstack  5
    IL_0000:  ldstr      "{0} {1} {2} {3} {4} {5} {6} {7}"
    IL_0005:  ldc.i4.8
    IL_0006:  newarr     [System.Runtime]System.Object
    IL_000b:  dup
    IL_000c:  ldc.i4.0
    IL_000d:  ldtoken    !T1
    IL_0012:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_0017:  callvirt   instance string [System.Runtime]System.Type::get_FullName()
    IL_001c:  stelem.ref
    IL_001d:  dup
    IL_001e:  ldc.i4.1
    IL_001f:  ldtoken    !T2
    IL_0024:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_0029:  callvirt   instance string [System.Runtime]System.Type::get_FullName()
    IL_002e:  stelem.ref
    IL_002f:  dup
    IL_0030:  ldc.i4.2
    IL_0031:  ldtoken    !!T3
    IL_0036:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_003b:  callvirt   instance string [System.Runtime]System.Type::get_FullName()
    IL_0040:  stelem.ref
    IL_0041:  dup
    IL_0042:  ldc.i4.3
    IL_0043:  ldtoken    !!T4
    IL_0048:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_004d:  callvirt   instance string [System.Runtime]System.Type::get_FullName()
    IL_0052:  stelem.ref
    IL_0053:  dup
    IL_0054:  ldc.i4.4
    IL_0055:  ldarg.2
    IL_0056:  box        !T1
    IL_005b:  stelem.ref
    IL_005c:  dup
    IL_005d:  ldc.i4.5
    IL_005e:  ldarg.3
    IL_005f:  box        !T2
    IL_0064:  stelem.ref
    IL_0065:  dup
    IL_0066:  ldc.i4.6
    IL_0067:  ldarg.s    c
    IL_0069:  box        !!T3
    IL_006e:  stelem.ref
    IL_006f:  dup
    IL_0070:  ldc.i4.7
    IL_0071:  ldarg.s    d
    IL_0073:  box        !!T4
    IL_0078:  stelem.ref
    IL_0079:  call       string [System.Runtime]System.String::Format(string,
                                                                      object[])
    IL_007e:  ret
  } // end of method GenInterfaceImpl`2::F

  .method public hidebysig newslot virtual final 
          instance string  G(valuetype S32 s,
                             !T1 a,
                             !T2 b) cil managed
  {
    // Code size       71 (0x47)
    .maxstack  5
    IL_0000:  ldstr      "{0} {1} {2} {3}"
    IL_0005:  ldc.i4.4
    IL_0006:  newarr     [System.Runtime]System.Object
    IL_000b:  dup
    IL_000c:  ldc.i4.0
    IL_000d:  ldtoken    !T1
    IL_0012:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_0017:  callvirt   instance string [System.Runtime]System.Type::get_FullName()
    IL_001c:  stelem.ref
    IL_001d:  dup
    IL_001e:  ldc.i4.1
    IL_001f:  ldtoken    !T2
    IL_0024:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_0029:  callvirt   instance string [System.Runtime]System.Type::get_FullName()
    IL_002e:  stelem.ref
    IL_002f:  dup
    IL_0030:  ldc.i4.2
    IL_0031:  ldarg.2
    IL_0032:  box        !T1
    IL_0037:  stelem.ref
    IL_0038:  dup
    IL_0039:  ldc.i4.3
    IL_003a:  ldarg.3
    IL_003b:  box        !T2
    IL_0040:  stelem.ref
    IL_0041:  call       string [System.Runtime]System.String::Format(string,
                                                                      object[])
    IL_0046:  ret
  } // end of method GenInterfaceImpl`2::G

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  ret
  } // end of method GenInterfaceImpl`2::.ctor

} // end of class GenInterfaceImpl`2

.class private abstract auto ansi beforefieldinit GenAbstract`1<T1>
       extends [System.Runtime]System.Object
{
  .method public hidebysig newslot abstract virtual 
          instance string  F<T2>() cil managed
  {
  } // end of method GenAbstract`1::F

  .method public hidebysig newslot abstract virtual 
          instance string  G() cil managed
  {
  } // end of method GenAbstract`1::G

  .method family hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  ret
  } // end of method GenAbstract`1::.ctor

} // end of class GenAbstract`1

.class private auto ansi beforefieldinit GenAbstractImpl`1<T1>
       extends class GenAbstract`1<!T1>
{
  .method public hidebysig virtual instance string 
          F<T2>() cil managed
  {
    // Code size       41 (0x29)
    .maxstack  8
    IL_0000:  ldtoken    !T1
    IL_0005:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_000a:  callvirt   instance string [System.Runtime]System.Type::get_FullName()
    IL_000f:  ldstr      " "
    IL_0014:  ldtoken    !!T2
    IL_0019:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_001e:  callvirt   instance string [System.Runtime]System.Type::get_FullName()
    IL_0023:  call       string [System.Runtime]System.String::Concat(string,
                                                                      string,
                                                                      string)
    IL_0028:  ret
  } // end of method GenAbstractImpl`1::F

  .method public hidebysig virtual instance string 
          G() cil managed
  {
    // Code size       25 (0x19)
    .maxstack  8
    IL_0000:  ldtoken    !T1
    IL_0005:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_000a:  callvirt   instance string [System.Runtime]System.Type::get_FullName()
    IL_000f:  dup
    IL_0010:  brtrue.s   IL_0018

    IL_0012:  pop
    IL_0013:  ldstr      ""
    IL_0018:  ret
  } // end of method GenAbstractImpl`1::G

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void class GenAbstract`1<!T1>::.ctor()
    IL_0006:  ret
  } // end of method GenAbstractImpl`1::.ctor

} // end of class GenAbstractImpl`1

.class private auto ansi more_tailcalls_ProcessedByFody
       extends [System.Runtime]System.Object
{
  .field static assembly literal string FodyVersion = "6.2.4.0"
  .field static assembly literal string InlineIL = "1.5.0.0"
} // end of class more_tailcalls_ProcessedByFody


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file more_tailcalls.res
