// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


.assembly extern System.Console { auto }
.assembly extern System.Runtime.Extensions { auto }
.assembly extern mscorlib { auto }

.assembly template
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 )
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}

.class public auto ansi beforefieldinit Test
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32  Main(string[] args) cil managed noinlining
  {
    .entrypoint
    .maxstack  4
    .locals init (bool V_0,
             string[] V_1,
             int32 V_2,
             string V_3,
             int32 V_4,
             bool V_5,
             string[] V_6,
             int32 V_7)
    IL_0000:  ldc.i4.1
    IL_0001:  stloc.0
    IL_0002:  ldftn       string[] [System.Runtime.Extensions]System.Environment::GetCommandLineArgs()
    IL_0003:  calli       string[]()
    IL_0007:  stloc.1
    IL_0008:  ldloc.1
    IL_0009:  ldlen
    IL_000a:  conv.i4
    IL_000b:  ldc.i4.1
    IL_000c:  sub
    IL_000d:  ldarg.0
    IL_000e:  ldlen
    IL_000f:  conv.i4
    IL_0010:  ceq
    IL_0012:  ldc.i4.0
    IL_0013:  ceq
    IL_0015:  stloc.s    V_5
    IL_0017:  ldloc.s    V_5
    IL_0019:  brtrue.s   IL_0048

    IL_001b:  ldc.i4.0
    IL_001c:  stloc.2
    IL_001d:  br.s       IL_0039

    IL_001f:  ldarg.0
    IL_0020:  ldloc.2
    IL_0021:  ldelem.ref
    IL_0022:  ldloc.1
    IL_0023:  ldloc.2
    IL_0024:  ldc.i4.1
    IL_0025:  add
    IL_0026:  ldelem.ref
    IL_0027:  callvirt   instance bool [mscorlib]System.String::Equals(string)
    IL_002c:  stloc.s    V_5
    IL_002e:  ldloc.s    V_5
    IL_0030:  brtrue.s   IL_0034

    IL_0032:  ldc.i4.0
    IL_0033:  stloc.0
    IL_0034:  nop
    IL_0035:  ldloc.2
    IL_0036:  ldc.i4.1
    IL_0037:  add
    IL_0038:  stloc.2
    IL_0039:  ldloc.2
    IL_003a:  ldarg.0
    IL_003b:  ldlen
    IL_003c:  conv.i4
    IL_003d:  clt
    IL_003f:  stloc.s    V_5
    IL_0041:  ldloc.s    V_5
    IL_0043:  brtrue.s   IL_001f

    IL_0045:  nop
    IL_0046:  br.s       IL_0056

    IL_0048:  ldstr      "Lengths not equal"
    IL_004d:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0052:  nop
    IL_0053:  ldc.i4.0
    IL_0054:  stloc.0
    IL_0055:  nop
    IL_0056:  ldloc.0
    IL_0057:  ldc.i4.0
    IL_0058:  ceq
    IL_005a:  stloc.s    V_5
    IL_005c:  ldloc.s    V_5
    IL_005e:  brtrue.s   IL_0071

    IL_0060:  ldstr      "Passed"
    IL_0065:  call       void [System.Console]System.Console::WriteLine(string)
    IL_006a:  nop
    IL_006b:  ldc.i4.s   100
    IL_006d:  stloc.s    V_4
    IL_006f:  br.s       IL_00e9

    IL_0071:  ldstr      "Failed"
    IL_0076:  call       void [System.Console]System.Console::WriteLine(string)
    IL_007b:  nop
    IL_007c:  ldstr      "args:"
    IL_0081:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0086:  nop
    IL_0087:  ldarg.0
    IL_0088:  stloc.s    V_6
    IL_008a:  ldc.i4.0
    IL_008b:  stloc.s    V_7
    IL_008d:  br.s       IL_00a2

    IL_008f:  ldloc.s    V_6
    IL_0091:  ldloc.s    V_7
    IL_0093:  ldelem.ref
    IL_0094:  stloc.3
    IL_0095:  ldloc.3
    IL_0096:  call       void [System.Console]System.Console::WriteLine(string)
    IL_009b:  nop
    IL_009c:  ldloc.s    V_7
    IL_009e:  ldc.i4.1
    IL_009f:  add
    IL_00a0:  stloc.s    V_7
    IL_00a2:  ldloc.s    V_7
    IL_00a4:  ldloc.s    V_6
    IL_00a6:  ldlen
    IL_00a7:  conv.i4
    IL_00a8:  clt
    IL_00aa:  stloc.s    V_5
    IL_00ac:  ldloc.s    V_5
    IL_00ae:  brtrue.s   IL_008f

    IL_00b0:  ldstr      "cmds:"
    IL_00b5:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00ba:  nop
    IL_00bb:  ldloc.1
    IL_00bc:  stloc.s    V_6
    IL_00be:  ldc.i4.0
    IL_00bf:  stloc.s    V_7
    IL_00c1:  br.s       IL_00d6

    IL_00c3:  ldloc.s    V_6
    IL_00c5:  ldloc.s    V_7
    IL_00c7:  ldelem.ref
    IL_00c8:  stloc.3
    IL_00c9:  ldloc.3
    IL_00ca:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00cf:  nop
    IL_00d0:  ldloc.s    V_7
    IL_00d2:  ldc.i4.1
    IL_00d3:  add
    IL_00d4:  stloc.s    V_7
    IL_00d6:  ldloc.s    V_7
    IL_00d8:  ldloc.s    V_6
    IL_00da:  ldlen
    IL_00db:  conv.i4
    IL_00dc:  clt
    IL_00de:  stloc.s    V_5
    IL_00e0:  ldloc.s    V_5
    IL_00e2:  brtrue.s   IL_00c3

    IL_00e4:  ldc.i4.1
    IL_00e5:  stloc.s    V_4
    IL_00e7:  br.s       IL_00e9

    IL_00e9:  ldloc.s    V_4
    IL_00eb:  ret
  }

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  }

}
