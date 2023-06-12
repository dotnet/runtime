// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 2:0:0:0
}
.assembly '607586'
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003
.corflags 0x00000001

.class public auto ansi beforefieldinit Class extends [mscorlib]System.Object
{
    .method public hidebysig static int32 Main() cil managed
    {
        .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
            01 00 00 00
        )
        .entrypoint
        .maxstack  5
        .locals init ([0] int32 CS$1$0000)
        IL_0000:  nop
        ldc.i4.0
        ldc.i4.2
        ldc.i4.3
        ldc.i4.4
        newarr     [mscorlib]System.Int32
        ldc.i4.5
        call       void Class::ConvertView(int32, int32, int32, int32[], int32)
        IL_0011:  ldc.i4.s 100
        IL_0012:  stloc.0
        IL_0013:  br.s       IL_0015

        IL_0015:  ldloc.0
        IL_0016:  ret
    } 

    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
        .maxstack  8
        ldarg.0
        call       instance void [mscorlib]System.Object::.ctor()
        ret
    }


    .method private hidebysig static void  dosomething(int32 a, string b, string c) cil managed
    {
        .maxstack  8
        nop
        ret
    }

    .method assembly hidebysig static void ConvertView(int32 a, int32 b, int32 c, int32[] d, int32 e) cil managed
    {
        .maxstack  3
        .locals init (
            [0] int32 dmember,
            [1] bool CS$4$0000,
            [2] int32[] CS$6$0001,
            [3] int32 CS$7$0002)

        IL_0001:  ldarg.0
        IL_0002:  ldc.i4.0
        IL_0003:  ceq
        IL_0007:  brfalse afterfinally
        br begintry
        afterfinally:
        IL_0009:  nop
        IL_000a:  ldarg.0
        IL_000b:  ldarg.s    e
        IL_000d:  brtrue.s   IL_0016
        IL_000f:  ldsfld     string [mscorlib]System.String::Empty
        IL_0014:  br      joinpoint
        IL_0016:  ldarga.s   a
        IL_0018:  call       instance string [mscorlib]System.Int32::ToString()
        br joinpoint

        begintry:
        .try
        {
            IL_002b:  ldarg.3
            IL_002c:  stloc.2
            IL_002d:  ldc.i4.0
            IL_002e:  stloc.3
            IL_002f:  br.s       IL_0080
            IL_0031: 
            ldloc.2
            IL_0032:  ldloc.3
            IL_0033:  ldelem.i4
            IL_0034:  stloc.0
            IL_0035:  nop
            IL_007a:  nop
            IL_007b:  nop
            IL_007c:  ldloc.3
            IL_007d:  ldc.i4.1
            IL_007e:  add
            IL_007f:  stloc.3
            IL_0080:  ldloc.3
            IL_0081:  ldloc.2
            IL_0082:  ldlen
            IL_0083:  conv.i4
            IL_0084:  clt
            IL_0086:  stloc.1
            IL_0087:  ldloc.1
            IL_0088:  brtrue.s   IL_0031
            IL_008a:  nop
            IL_008b:  leave.s    IL_00c3
        }
        finally
        {
            IL_008d:  nop
            IL_008e:  ldstr      "in outer finally"
            IL_0093:  call       void [System.Console]System.Console::WriteLine(string)
            IL_00c2:  endfinally
        }

        joinpoint:
        IL_001d:  ldstr      "the bad string"
        IL_0022:  call       void Class::dosomething(int32, string, string)
        ldloc 0
        brfalse begintry 
        IL_00c3:  nop
        IL_00c4:  ret
      }
}
