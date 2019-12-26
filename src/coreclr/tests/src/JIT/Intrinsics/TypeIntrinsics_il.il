// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly repro_32
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
}

.class private auto ansi beforefieldinit Test
       extends [mscorlib]System.Object
{
  .method private hidebysig static int32 Main() cil managed
  {
    .entrypoint
    // Code size       257 (0x101)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  ldc.i4.s   100
    IL_0002:  stloc.0
    IL_0003:  ldtoken    [System.Runtime]System.String&
    IL_0008:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_000d:  call       instance bool [System.Runtime]System.Type::get_IsPrimitive()
    IL_0012:  brfalse.s  IL_0018

    IL_0014:  ldloc.0
    IL_0015:  ldc.i4.1
    IL_0016:  add
    IL_0017:  stloc.0
    IL_0018:  ldtoken    [System.Runtime]System.String&
    IL_001d:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_0022:  call       instance bool [System.Runtime]System.Type::get_IsValueType()
    IL_0027:  brfalse.s  IL_002d

    IL_0029:  ldloc.0
    IL_002a:  ldc.i4.1
    IL_002b:  add
    IL_002c:  stloc.0
    IL_002d:  ldtoken    [System.Runtime]System.String&
    IL_0032:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_0037:  call       instance bool [System.Runtime]System.Type::get_IsClass()
    IL_003c:  brtrue.s   IL_0042

    IL_003e:  ldloc.0
    IL_003f:  ldc.i4.1
    IL_0040:  add
    IL_0041:  stloc.0
    IL_0042:  ldtoken    [System.Runtime]System.Byte&
    IL_0047:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_004c:  call       instance bool [System.Runtime]System.Type::get_IsPrimitive()
    IL_0051:  brtrue.s   IL_0057

    IL_0053:  ldloc.0
    IL_0054:  ldc.i4.1
    IL_0055:  add
    IL_0056:  stloc.0
    IL_0057:  ldtoken    [System.Runtime]System.Int16&
    IL_005c:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_0061:  call       instance bool [System.Runtime]System.Type::get_IsValueType()
    IL_0066:  brtrue.s   IL_006c

    IL_0068:  ldloc.0
    IL_0069:  ldc.i4.1
    IL_006a:  add
    IL_006b:  stloc.0
    IL_006c:  ldtoken    [System.Runtime]System.UInt32&
    IL_0071:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_0076:  call       instance bool [System.Runtime]System.Type::get_IsClass()
    IL_007b:  brfalse.s  IL_0081

    IL_007d:  ldloc.0
    IL_007e:  ldc.i4.1
    IL_007f:  add
    IL_0080:  stloc.0
    IL_0081:  ldtoken    [System.Runtime]System.Object&
    IL_0086:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_008b:  call       instance bool [System.Runtime]System.Type::get_IsPrimitive()
    IL_0090:  brfalse.s  IL_0096

    IL_0092:  ldloc.0
    IL_0093:  ldc.i4.1
    IL_0094:  add
    IL_0095:  stloc.0
    IL_0096:  ldtoken    [System.Runtime]System.Object&
    IL_009b:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_00a0:  call       instance bool [System.Runtime]System.Type::get_IsValueType()
    IL_00a5:  brfalse.s  IL_00ab

    IL_00a7:  ldloc.0
    IL_00a8:  ldc.i4.1
    IL_00a9:  add
    IL_00aa:  stloc.0
    IL_00ab:  ldtoken    [System.Runtime]System.Object&
    IL_00b0:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_00b5:  call       instance bool [System.Runtime]System.Type::get_IsClass()
    IL_00ba:  brtrue.s   IL_00c0

    IL_00bc:  ldloc.0
    IL_00bd:  ldc.i4.1
    IL_00be:  add
    IL_00bf:  stloc.0
    IL_00c0:  ldtoken    [System.Runtime]System.Decimal&
    IL_00c5:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_00ca:  call       instance bool [System.Runtime]System.Type::get_IsPrimitive()
    IL_00cf:  brfalse.s  IL_00d5

    IL_00d1:  ldloc.0
    IL_00d2:  ldc.i4.1
    IL_00d3:  add
    IL_00d4:  stloc.0
    IL_00d5:  ldtoken    [System.Runtime]System.Decimal&
    IL_00da:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_00df:  call       instance bool [System.Runtime]System.Type::get_IsValueType()
    IL_00e4:  brtrue.s   IL_00ea

    IL_00e6:  ldloc.0
    IL_00e7:  ldc.i4.1
    IL_00e8:  add
    IL_00e9:  stloc.0
    IL_00ea:  ldtoken    [System.Runtime]System.Decimal&
    IL_00ef:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    IL_00f4:  call       instance bool [System.Runtime]System.Type::get_IsClass()
    IL_00f9:  brfalse.s  IL_00ff

    IL_00fb:  ldloc.0
    IL_00fc:  ldc.i4.1
    IL_00fd:  add
    IL_00fe:  stloc.0
    IL_00ff:  ldloc.0
    IL_0100:  ret
  }
}