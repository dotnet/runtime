// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly arrgetlen {}
.class private sequential ansi sealed beforefieldinit VT
       extends [mscorlib]System.ValueType
{
  .pack 0
  .size 1
} // end of class VT

.class private auto ansi beforefieldinit X<([mscorlib]System.Object) T>
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method X::.ctor

} // end of class X

.class public auto ansi beforefieldinit arrgetlen
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  2
    .locals init (class [mscorlib]System.Array V_0,
             int32 V_1,
             valuetype VT [] V_2,
             class arrgetlen[] V_3,
             class X<int32> [] V_4,
             class X<class X<int32>> [] V_5,
             class X<class X<int32>>[] [] V_6)
    IL_0000:  ldstr      "System.Int32"
    IL_0005:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetType(string)
    IL_000a:  ldc.i4.1
    IL_000b:  call       class [mscorlib]System.Array [mscorlib]System.Array::CreateInstance(class [mscorlib]System.Type,
                                                                                             int32)
    IL_0010:  stloc.0
    IL_0011:  ldloc.0
    IL_0012:  ldc.i4.0
    IL_0013:  callvirt   instance int32 [mscorlib]System.Array::GetLength(int32)
    IL_0018:  ldc.i4.1
    IL_0019:  beq.s      IL_0022

    IL_001b:  ldc.i4.1
    IL_001c:  stloc.1
    IL_001d:  br         IL_0135

    IL_0022:  ldtoken    VT
    IL_0027:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_002c:  ldc.i4.1
    IL_002d:  call       class [mscorlib]System.Array [mscorlib]System.Array::CreateInstance(class [mscorlib]System.Type,
                                                                                             int32)
    IL_0032:  stloc.2
    IL_0033:  ldloc.2
    IL_0034:  ldc.i4.0
    IL_0035:  callvirt   instance int32 [mscorlib]System.Array::GetLength(int32)
    IL_003a:  ldc.i4.1
    IL_003b:  beq.s      IL_0044

    IL_003d:  ldc.i4.1
    IL_003e:  stloc.1
    IL_003f:  br         IL_0135

    IL_0044:  ldtoken    arrgetlen
    IL_0049:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_004e:  ldc.i4.1
    IL_004f:  call       class [mscorlib]System.Array [mscorlib]System.Array::CreateInstance(class [mscorlib]System.Type,
                                                                                             int32)
    IL_0054:  stloc V_3
    IL_0055:  ldloc V_3
    IL_0056:  ldc.i4.0
    IL_0057:  callvirt   instance int32 [mscorlib]System.Array::GetLength(int32)
    IL_005c:  ldc.i4.1
    IL_005d:  beq.s      IL_0066

    IL_005f:  ldc.i4.1
    IL_0060:  stloc.1
    IL_0061:  br         IL_0135

    IL_0066:  ldtoken    class X<int32>
    IL_006b:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_0070:  ldc.i4.1
    IL_0071:  call       class [mscorlib]System.Array [mscorlib]System.Array::CreateInstance(class [mscorlib]System.Type,
                                                                                             int32)
    IL_0076:  stloc V_4
    IL_0077:  ldloc V_4
    IL_0078:  ldc.i4.0
    IL_0079:  callvirt   instance int32 [mscorlib]System.Array::GetLength(int32)
    IL_007e:  ldc.i4.1
    IL_007f:  beq.s      IL_0088

    IL_0081:  ldc.i4.1
    IL_0082:  stloc.1
    IL_0083:  br         IL_0135

    IL_0088:  ldtoken    class X<class X<int32>>
    IL_008d:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_0092:  ldc.i4.1
    IL_0093:  call       class [mscorlib]System.Array [mscorlib]System.Array::CreateInstance(class [mscorlib]System.Type,
                                                                                             int32)
    IL_0098:  stloc V_5
    IL_0099:  ldloc V_5
    IL_009a:  ldc.i4.0
    IL_009b:  callvirt   instance int32 [mscorlib]System.Array::GetLength(int32)
    IL_00a0:  ldc.i4.1
    IL_00a1:  beq.s      IL_00aa

    IL_00a3:  ldc.i4.1
    IL_00a4:  stloc.1
    IL_00a5:  br         IL_0135

    IL_00aa:  ldtoken    valuetype VT[]
    IL_00af:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_00b4:  ldc.i4.2
    IL_00b5:  call       class [mscorlib]System.Array [mscorlib]System.Array::CreateInstance(class [mscorlib]System.Type,
                                                                                             int32)
    IL_00ba:  stloc.0
    IL_00bb:  ldloc.0
    IL_00bc:  ldc.i4.0
    IL_00bd:  callvirt   instance int32 [mscorlib]System.Array::GetLength(int32)
    IL_00c2:  ldc.i4.2
    IL_00c3:  beq.s      IL_00c9

    IL_00c5:  ldc.i4.2
    IL_00c6:  stloc.1
    IL_00c7:  br.s       IL_0135

    IL_00c9:  ldtoken    class arrgetlen[]
    IL_00ce:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_00d3:  ldc.i4.2
    IL_00d4:  call       class [mscorlib]System.Array [mscorlib]System.Array::CreateInstance(class [mscorlib]System.Type,
                                                                                             int32)
    IL_00d9:  stloc.0
    IL_00da:  ldloc.0
    IL_00db:  ldc.i4.0
    IL_00dc:  callvirt   instance int32 [mscorlib]System.Array::GetLength(int32)
    IL_00e1:  ldc.i4.2
    IL_00e2:  beq.s      IL_00e8

    IL_00e4:  ldc.i4.2
    IL_00e5:  stloc.1
    IL_00e6:  br.s       IL_0135

    IL_00e8:  ldtoken    class X<int32>[]
    IL_00ed:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_00f2:  ldc.i4.2
    IL_00f3:  call       class [mscorlib]System.Array [mscorlib]System.Array::CreateInstance(class [mscorlib]System.Type,
                                                                                             int32)
    IL_00f8:  stloc.0
    IL_00f9:  ldloc.0
    IL_00fa:  ldc.i4.0
    IL_00fb:  callvirt   instance int32 [mscorlib]System.Array::GetLength(int32)
    IL_0100:  ldc.i4.2
    IL_0101:  beq.s      IL_0107

    IL_0103:  ldc.i4.2
    IL_0104:  stloc.1
    IL_0105:  br.s       IL_0135

    IL_0107:  ldtoken    class X<class X<int32>>[]
    IL_010c:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_0111:  ldc.i4.2
    IL_0112:  call       class [mscorlib]System.Array [mscorlib]System.Array::CreateInstance(class [mscorlib]System.Type,
                                                                                             int32)
    IL_0117:  stloc V_6
    IL_0118:  ldloc V_6
    IL_0119:  ldc.i4.0
    IL_011a:  callvirt   instance int32 [mscorlib]System.Array::GetLength(int32)
    IL_011f:  ldc.i4.2
    IL_0120:  beq.s      IL_0126

    IL_0122:  ldc.i4.2
    IL_0123:  stloc.1
    IL_0124:  br.s       IL_0135

    IL_0126:  ldstr      "PASSED"
    IL_012b:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0130:  ldc.i4.s   100
    IL_0132:  stloc.1
    IL_0133:  br.s       IL_0135

    IL_0135:  ldloc.1
    IL_0136:  ret
  } // end of method arrgetlen::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method arrgetlen::.ctor

}
