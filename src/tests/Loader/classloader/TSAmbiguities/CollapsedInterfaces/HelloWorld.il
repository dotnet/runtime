// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern xunit.core {}

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly HelloWorld
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
// MVID: {ED3BD1E6-3FBD-4590-8CC8-2B84D3A177C9}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x00990000


// =============== CLASS MEMBERS DECLARATION ===================

.class private auto ansi beforefieldinit HelloWorld
       extends [mscorlib]System.Object
{
  .method private hidebysig static int32 
          Main(string[] args) cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       337 (0x151)
    .maxstack  3
    .locals init (bool V_0,
             class [mscorlib]System.Reflection.MethodInfo V_1,
             object[] V_2,
             class TypeTestAttribute V_3,
             class [mscorlib]System.Type V_4,
             int32 V_5,
             class [mscorlib]System.Reflection.MethodInfo[] V_6,
             int32 V_7,
             bool V_8)
    IL_0000:  nop
    IL_0001:  ldc.i4.1
    IL_0002:  stloc.0
    IL_0003:  nop
    IL_0004:  ldtoken    HelloWorld
    IL_0009:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_000e:  ldc.i4.s   24
    IL_0010:  callvirt   instance class [mscorlib]System.Reflection.MethodInfo[] [mscorlib]System.Type::GetMethods(valuetype [mscorlib]System.Reflection.BindingFlags)
    IL_0015:  stloc.s    V_6
    IL_0017:  ldc.i4.0
    IL_0018:  stloc.s    V_7
    IL_001a:  br         IL_0128

    IL_001f:  ldloc.s    V_6
    IL_0021:  ldloc.s    V_7
    IL_0023:  ldelem.ref
    IL_0024:  stloc.1
    IL_0025:  nop
    IL_0026:  ldloc.1
    IL_0027:  ldtoken    TypeTestAttribute
    IL_002c:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_0031:  ldc.i4.0
    IL_0032:  callvirt   instance object[] [mscorlib]System.Reflection.MemberInfo::GetCustomAttributes(class [mscorlib]System.Type,
                                                                                                       bool)
    IL_0037:  stloc.2
    IL_0038:  ldloc.2
    IL_0039:  ldlen
    IL_003a:  conv.i4
    IL_003b:  ldc.i4.1
    IL_003c:  clt
    IL_003e:  stloc.s    V_8
    IL_0040:  ldloc.s    V_8
    IL_0042:  brtrue     IL_0121

    IL_0047:  nop
    IL_0048:  ldloc.2
    IL_0049:  ldc.i4.0
    IL_004a:  ldelem.ref
    IL_004b:  castclass  TypeTestAttribute
    IL_0050:  stloc.3
    IL_0051:  ldstr      "TEST {0}:"
    IL_0056:  ldloc.1
    IL_0057:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
    IL_005c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_0061:  nop
    IL_0062:  ldstr      "     -> Instantiation : "
    IL_0067:  ldloc.3
    IL_0068:  ldfld      string TypeTestAttribute::instantiation
    IL_006d:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0072:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0077:  nop
    IL_0078:  ldstr      "     -> Should fail   : "
    IL_007d:  ldloc.3
    IL_007e:  ldfld      bool TypeTestAttribute::shouldFail
    IL_0083:  box        [mscorlib]System.Boolean
    IL_0088:  call       string [mscorlib]System.String::Concat(object,
                                                                object)
    IL_008d:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0092:  nop
    .try
    {
      IL_0093:  nop
      IL_0094:  ldloc.1
      IL_0095:  ldnull
      IL_0096:  ldc.i4.0
      IL_0097:  newarr     [mscorlib]System.Object
      IL_009c:  callvirt   instance object [mscorlib]System.Reflection.MethodBase::Invoke(object,
                                                                                          object[])
      IL_00a1:  castclass  [mscorlib]System.Type
      IL_00a6:  stloc.s    V_4
      IL_00a8:  ldstr      " + TYPE LOADED"
      IL_00ad:  call       void [System.Console]System.Console::WriteLine(string)
      IL_00b2:  nop
      IL_00b3:  ldloc.3
      IL_00b4:  ldfld      bool TypeTestAttribute::shouldFail
      IL_00b9:  ldc.i4.0
      IL_00ba:  ceq
      IL_00bc:  stloc.s    V_8
      IL_00be:  ldloc.s    V_8
      IL_00c0:  brtrue.s   IL_00d3

      IL_00c2:  nop
      IL_00c3:  ldstr      " + TEST FAILED!\n\n"
      IL_00c8:  call       void [System.Console]System.Console::WriteLine(string)
      IL_00cd:  nop
      IL_00ce:  ldc.i4.0
      IL_00cf:  stloc.0
      IL_00d0:  nop
      IL_00d1:  br.s       IL_00de

      IL_00d3:  ldstr      " + TEST PASSED!\n\n"
      IL_00d8:  call       void [System.Console]System.Console::WriteLine(string)
      IL_00dd:  nop
      IL_00de:  nop
      IL_00df:  leave.s    IL_0119

    }  // end .try
    catch [mscorlib]System.Object 
    {
      IL_00e1:  pop
      IL_00e2:  nop
      IL_00e3:  ldstr      " + TYPE NOT LOADED"
      IL_00e8:  call       void [System.Console]System.Console::WriteLine(string)
      IL_00ed:  nop
      IL_00ee:  ldloc.3
      IL_00ef:  ldfld      bool TypeTestAttribute::shouldFail
      IL_00f4:  stloc.s    V_8
      IL_00f6:  ldloc.s    V_8
      IL_00f8:  brtrue.s   IL_010b

      IL_00fa:  nop
      IL_00fb:  ldstr      " + TEST FAILED!\n\n"
      IL_0100:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0105:  nop
      IL_0106:  ldc.i4.0
      IL_0107:  stloc.0
      IL_0108:  nop
      IL_0109:  br.s       IL_0116

      IL_010b:  ldstr      " + TEST PASSED!\n\n"
      IL_0110:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0115:  nop
      IL_0116:  nop
      IL_0117:  leave.s    IL_0119

    }  // end handler
    IL_0119:  nop
    IL_011a:  call       void [System.Console]System.Console::WriteLine()
    IL_011f:  nop
    IL_0120:  nop
    IL_0121:  nop
    IL_0122:  ldloc.s    V_7
    IL_0124:  ldc.i4.1
    IL_0125:  add
    IL_0126:  stloc.s    V_7
    IL_0128:  ldloc.s    V_7
    IL_012a:  ldloc.s    V_6
    IL_012c:  ldlen
    IL_012d:  conv.i4
    IL_012e:  clt
    IL_0130:  stloc.s    V_8
    IL_0132:  ldloc.s    V_8
    IL_0134:  brtrue     IL_001f

    IL_0139:  ldloc.0
    IL_013a:  ldc.i4.0
    IL_013b:  ceq
    IL_013d:  stloc.s    V_8
    IL_013f:  ldloc.s    V_8
    IL_0141:  brtrue.s   IL_0149

    IL_0143:  ldc.i4.s   100
    IL_0145:  stloc.s    V_5
    IL_0147:  br.s       IL_014e

    IL_0149:  ldc.i4.m1
    IL_014a:  stloc.s    V_5
    IL_014c:  br.s       IL_014e

    IL_014e:  ldloc.s    V_5
    IL_0150:  ret
  } // end of method HelloWorld::Main

  .method public hidebysig static class [mscorlib]System.Type 
          Test_A_int_byte() cil managed
  {
    .custom instance void TypeTestAttribute::.ctor(string,
                                                   class [mscorlib]System.Type,
                                                   bool) = ( 01 00 0C 41 3C 69 6E 74 2C 20 62 79 74 65 3E 03   // ...A<int, byte>.
                                                             41 60 32 00 00 00 )                               // A`2...
    // Code size       16 (0x10)
    .maxstack  1
    .locals init (class [mscorlib]System.Type V_0)
    IL_0000:  nop
    IL_0001:  ldtoken    class A`2<int32,uint8>
    IL_0006:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_000e

    IL_000e:  ldloc.0
    IL_000f:  ret
  } // end of method HelloWorld::Test_A_int_byte

  .method public hidebysig static class [mscorlib]System.Type 
          Test_A_int_int() cil managed
  {
    .custom instance void TypeTestAttribute::.ctor(string,
                                                   class [mscorlib]System.Type,
                                                   bool) = ( 01 00 0B 41 3C 69 6E 74 2C 20 69 6E 74 3E 03 41   // ...A<int, int>.A
                                                             60 32 01 00 00 )                                  // `2...
    // Code size       16 (0x10)
    .maxstack  1
    .locals init (class [mscorlib]System.Type V_0)
    IL_0000:  nop
    IL_0001:  ldtoken    class A`2<int32,int32>
    IL_0006:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_000e

    IL_000e:  ldloc.0
    IL_000f:  ret
  } // end of method HelloWorld::Test_A_int_int

  .method public hidebysig static class [mscorlib]System.Type 
          Test_A_string_object() cil managed
  {
    .custom instance void TypeTestAttribute::.ctor(string,
                                                   class [mscorlib]System.Type,
                                                   bool) = ( 01 00 11 41 3C 73 74 72 69 6E 67 2C 20 6F 62 6A   // ...A<string, obj
                                                             65 63 74 3E 03 41 60 32 00 00 00 )                // ect>.A`2...
    // Code size       16 (0x10)
    .maxstack  1
    .locals init (class [mscorlib]System.Type V_0)
    IL_0000:  nop
    IL_0001:  ldtoken    class A`2<string,object>
    IL_0006:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_000e

    IL_000e:  ldloc.0
    IL_000f:  ret
  } // end of method HelloWorld::Test_A_string_object

  .method public hidebysig static class [mscorlib]System.Type 
          Test_A_string_string() cil managed
  {
    .custom instance void TypeTestAttribute::.ctor(string,
                                                   class [mscorlib]System.Type,
                                                   bool) = ( 01 00 11 41 3C 73 74 72 69 6E 67 2C 20 73 74 72   // ...A<string, str
                                                             69 6E 67 3E 03 41 60 32 01 00 00 )                // ing>.A`2...
    // Code size       16 (0x10)
    .maxstack  1
    .locals init (class [mscorlib]System.Type V_0)
    IL_0000:  nop
    IL_0001:  ldtoken    class A`2<string,string>
    IL_0006:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_000e

    IL_000e:  ldloc.0
    IL_000f:  ret
  } // end of method HelloWorld::Test_A_string_string

  .method public hidebysig static class [mscorlib]System.Type 
          Test_B_int_byte() cil managed
  {
    .custom instance void TypeTestAttribute::.ctor(string,
                                                   class [mscorlib]System.Type,
                                                   bool) = ( 01 00 0C 42 3C 69 6E 74 2C 20 62 79 74 65 3E 03   // ...B<int, byte>.
                                                             42 60 32 00 00 00 )                               // B`2...
    // Code size       16 (0x10)
    .maxstack  1
    .locals init (class [mscorlib]System.Type V_0)
    IL_0000:  nop
    IL_0001:  ldtoken    class B`2<int32,uint8>
    IL_0006:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_000e

    IL_000e:  ldloc.0
    IL_000f:  ret
  } // end of method HelloWorld::Test_B_int_byte

  .method public hidebysig static class [mscorlib]System.Type 
          Test_B_int_int() cil managed
  {
    .custom instance void TypeTestAttribute::.ctor(string,
                                                   class [mscorlib]System.Type,
                                                   bool) = ( 01 00 0B 42 3C 69 6E 74 2C 20 69 6E 74 3E 03 42   // ...B<int, int>.B
                                                             60 32 01 00 00 )                                  // `2...
    // Code size       16 (0x10)
    .maxstack  1
    .locals init (class [mscorlib]System.Type V_0)
    IL_0000:  nop
    IL_0001:  ldtoken    class B`2<int32,int32>
    IL_0006:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_000e

    IL_000e:  ldloc.0
    IL_000f:  ret
  } // end of method HelloWorld::Test_B_int_int

  .method public hidebysig static class [mscorlib]System.Type 
          Test_B_string_object() cil managed
  {
    .custom instance void TypeTestAttribute::.ctor(string,
                                                   class [mscorlib]System.Type,
                                                   bool) = ( 01 00 11 42 3C 73 74 72 69 6E 67 2C 20 6F 62 6A   // ...B<string, obj
                                                             65 63 74 3E 03 42 60 32 00 00 00 )                // ect>.B`2...
    // Code size       16 (0x10)
    .maxstack  1
    .locals init (class [mscorlib]System.Type V_0)
    IL_0000:  nop
    IL_0001:  ldtoken    class B`2<string,object>
    IL_0006:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_000e

    IL_000e:  ldloc.0
    IL_000f:  ret
  } // end of method HelloWorld::Test_B_string_object

  .method public hidebysig static class [mscorlib]System.Type 
          Test_B_string_string() cil managed
  {
    .custom instance void TypeTestAttribute::.ctor(string,
                                                   class [mscorlib]System.Type,
                                                   bool) = ( 01 00 11 42 3C 73 74 72 69 6E 67 2C 20 73 74 72   // ...B<string, str
                                                             69 6E 67 3E 03 42 60 32 01 00 00 )                // ing>.B`2...
    // Code size       16 (0x10)
    .maxstack  1
    .locals init (class [mscorlib]System.Type V_0)
    IL_0000:  nop
    IL_0001:  ldtoken    class B`2<string,string>
    IL_0006:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_000e

    IL_000e:  ldloc.0
    IL_000f:  ret
  } // end of method HelloWorld::Test_B_string_string

  .method public hidebysig static class [mscorlib]System.Type 
          Test_C_int_byte() cil managed
  {
    .custom instance void TypeTestAttribute::.ctor(string,
                                                   class [mscorlib]System.Type,
                                                   bool) = ( 01 00 0C 43 3C 69 6E 74 2C 20 62 79 74 65 3E 03   // ...C<int, byte>.
                                                             43 60 32 00 00 00 )                               // C`2...
    // Code size       16 (0x10)
    .maxstack  1
    .locals init (class [mscorlib]System.Type V_0)
    IL_0000:  nop
    IL_0001:  ldtoken    class C`2<int32,uint8>
    IL_0006:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_000e

    IL_000e:  ldloc.0
    IL_000f:  ret
  } // end of method HelloWorld::Test_C_int_byte

  .method public hidebysig static class [mscorlib]System.Type 
          Test_C_string_string() cil managed
  {
    .custom instance void TypeTestAttribute::.ctor(string,
                                                   class [mscorlib]System.Type,
                                                   bool) = ( 01 00 11 43 3C 73 74 72 69 6E 67 2C 20 73 74 72   // ...C<string, str
                                                             69 6E 67 3E 03 43 60 32 00 00 00 )                // ing>.C`2...
    // Code size       16 (0x10)
    .maxstack  1
    .locals init (class [mscorlib]System.Type V_0)
    IL_0000:  nop
    IL_0001:  ldtoken    class C`2<string,string>
    IL_0006:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_000e

    IL_000e:  ldloc.0
    IL_000f:  ret
  } // end of method HelloWorld::Test_C_string_string

  .method public hidebysig static class [mscorlib]System.Type 
          Test_C_ListInt_byte() cil managed
  {
    .custom instance void TypeTestAttribute::.ctor(string,
                                                   class [mscorlib]System.Type,
                                                   bool) = ( 01 00 12 43 3C 4C 69 73 74 3C 69 6E 74 3E 2C 20   // ...C<List<int>, 
                                                             62 79 74 65 3E 03 43 60 32 00 00 00 )             // byte>.C`2...
    // Code size       16 (0x10)
    .maxstack  1
    .locals init (class [mscorlib]System.Type V_0)
    IL_0000:  nop
    IL_0001:  ldtoken    class C`2<class [mscorlib]System.Collections.Generic.List`1<int32>,uint8>
    IL_0006:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_000e

    IL_000e:  ldloc.0
    IL_000f:  ret
  } // end of method HelloWorld::Test_C_ListInt_byte

  .method public hidebysig static class [mscorlib]System.Type 
          Test_C_ListInt_int() cil managed
  {
    .custom instance void TypeTestAttribute::.ctor(string,
                                                   class [mscorlib]System.Type,
                                                   bool) = ( 01 00 11 43 3C 4C 69 73 74 3C 69 6E 74 3E 2C 20   // ...C<List<int>, 
                                                             69 6E 74 3E 03 43 60 32 01 00 00 )                // int>.C`2...
    // Code size       16 (0x10)
    .maxstack  1
    .locals init (class [mscorlib]System.Type V_0)
    IL_0000:  nop
    IL_0001:  ldtoken    class C`2<class [mscorlib]System.Collections.Generic.List`1<int32>,int32>
    IL_0006:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_000e

    IL_000e:  ldloc.0
    IL_000f:  ret
  } // end of method HelloWorld::Test_C_ListInt_int

  .method public hidebysig static class [mscorlib]System.Type 
          Test_C_ListString_object() cil managed
  {
    .custom instance void TypeTestAttribute::.ctor(string,
                                                   class [mscorlib]System.Type,
                                                   bool) = ( 01 00 17 43 3C 4C 69 73 74 3C 73 74 72 69 6E 67   // ...C<List<string
                                                             3E 2C 20 6F 62 6A 65 63 74 3E 03 43 60 32 00 00   // >, object>.C`2..
                                                             00 ) 
    // Code size       16 (0x10)
    .maxstack  1
    .locals init (class [mscorlib]System.Type V_0)
    IL_0000:  nop
    IL_0001:  ldtoken    class C`2<class [mscorlib]System.Collections.Generic.List`1<string>,object>
    IL_0006:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_000e

    IL_000e:  ldloc.0
    IL_000f:  ret
  } // end of method HelloWorld::Test_C_ListString_object

  .method public hidebysig static class [mscorlib]System.Type 
          Test_C_ListString_string() cil managed
  {
    .custom instance void TypeTestAttribute::.ctor(string,
                                                   class [mscorlib]System.Type,
                                                   bool) = ( 01 00 17 43 3C 4C 69 73 74 3C 73 74 72 69 6E 67   // ...C<List<string
                                                             3E 2C 20 73 74 72 69 6E 67 3E 03 43 60 32 01 00   // >, string>.C`2..
                                                             00 ) 
    // Code size       16 (0x10)
    .maxstack  1
    .locals init (class [mscorlib]System.Type V_0)
    IL_0000:  nop
    IL_0001:  ldtoken    class C`2<class [mscorlib]System.Collections.Generic.List`1<string>,string>
    IL_0006:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_000e

    IL_000e:  ldloc.0
    IL_000f:  ret
  } // end of method HelloWorld::Test_C_ListString_string

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method HelloWorld::.ctor

} // end of class HelloWorld

.class private auto ansi beforefieldinit TypeTestAttribute
       extends [mscorlib]System.Attribute
{
  .custom instance void [mscorlib]System.AttributeUsageAttribute::.ctor(valuetype [mscorlib]System.AttributeTargets) = ( 01 00 40 00 00 00 00 00 )                         // ..@.....
  .field public initonly string instantiation
  .field public initonly class [mscorlib]System.Type 'type'
  .field public initonly bool shouldFail
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(string instantiation,
                               class [mscorlib]System.Type 'type',
                               bool shouldFail) cil managed
  {
    // Code size       31 (0x1f)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Attribute::.ctor()
    IL_0006:  nop
    IL_0007:  nop
    IL_0008:  ldarg.0
    IL_0009:  ldarg.1
    IL_000a:  stfld      string TypeTestAttribute::instantiation
    IL_000f:  ldarg.0
    IL_0010:  ldarg.2
    IL_0011:  stfld      class [mscorlib]System.Type TypeTestAttribute::'type'
    IL_0016:  ldarg.0
    IL_0017:  ldarg.3
    IL_0018:  stfld      bool TypeTestAttribute::shouldFail
    IL_001d:  nop
    IL_001e:  ret
  } // end of method TypeTestAttribute::.ctor

} // end of class TypeTestAttribute

.class interface private abstract auto ansi I`1<T>
{
  .method public hidebysig newslot abstract virtual 
          instance void  M() cil managed
  {
  } // end of method I`1::M

} // end of class I`1

.class private auto ansi beforefieldinit A`2<T,U>
       extends [mscorlib]System.Object
       implements class I`1<!T>,
                  class I`1<!U>
{
  .method public hidebysig newslot virtual 
          instance void  M() cil managed
  {
    // Code size       33 (0x21)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "A<{0},{1}>.M()"
    IL_0006:  ldtoken    !T
    IL_000b:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_0010:  ldtoken    !U
    IL_0015:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_001a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_001f:  nop
    IL_0020:  ret
  } // end of method A`2::M

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method A`2::.ctor

} // end of class A`2

.class private auto ansi beforefieldinit B`2<T,U>
       extends [mscorlib]System.Object
       implements class I`1<class [mscorlib]System.Collections.Generic.List`1<!T>>,
                  class I`1<class [mscorlib]System.Collections.Generic.List`1<!U>>
{
  .method public hidebysig newslot virtual 
          instance void  M() cil managed
  {
    // Code size       33 (0x21)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "B<{0},{1}>.M()"
    IL_0006:  ldtoken    !T
    IL_000b:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_0010:  ldtoken    !U
    IL_0015:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_001a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_001f:  nop
    IL_0020:  ret
  } // end of method B`2::M

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method B`2::.ctor

} // end of class B`2

.class private auto ansi beforefieldinit C`2<T,U>
       extends [mscorlib]System.Object
       implements class I`1<class [mscorlib]System.Collections.Generic.List`1<!T>>,
                  class I`1<class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Collections.Generic.List`1<!U>>>
{
  .method public hidebysig newslot virtual 
          instance void  M() cil managed
  {
    // Code size       33 (0x21)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "C<{0},{1}>.M()"
    IL_0006:  ldtoken    !T
    IL_000b:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_0010:  ldtoken    !U
    IL_0015:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_001a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_001f:  nop
    IL_0020:  ret
  } // end of method C`2::M

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method C`2::.ctor

} // end of class C`2


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file HelloWorld.res
