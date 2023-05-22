// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}
.assembly ASSEMBLY_NAME { }
.method public static class System.String 
        rems(int32& n,
             int32& m) il managed
{
  // Code size       93 (0x5d)
  .maxstack  8
  .locals (int32[] V_0)
  IL_0000:  ldarg.1
  IL_0001:  ldind.i4
  IL_0002:  ldc.i4.1
  IL_0003:  bne.un.s   IL_0007

  IL_0005:  ldnull
  IL_0006:  ret

  IL_0007:  ldc.i4.1
  IL_0008:  newarr     int32
  IL_000d:  stloc.0
  IL_000e:  ldloc.0
  IL_000f:  ldc.i4.0
  IL_0010:  ldarg.1
  IL_0011:  ldind.i4
  IL_0012:  ldc.i4.1
  IL_0013:  sub
  IL_0014:  stelem.i4
  IL_0015:  ldarg.0
  IL_0016:  ldind.i4
  IL_0017:  ldarg.1
  IL_0018:  ldind.i4
  IL_0019:  rem
  IL_001a:  brtrue.s   IL_0047

  IL_001c:  ldarg.0
  IL_001d:  ldloc.0
  IL_001e:  ldc.i4.0
  IL_001f:  ldelema    int32
  IL_0024:  call       class System.String rems(int32&,
                                                int32&)
  IL_0029:  ldstr      " "
  IL_002e:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                           class System.String)
  IL_0033:  ldarg.1
  IL_0034:  call       instance class System.String [mscorlib]System.Int32::ToString()
  IL_0039:  ldftn      class System.String [mscorlib]System.String::Concat(class System.String,
                                                                           class System.String)
  IL_003f:  tail.
  IL_0041:  calli      class System.String(class System.String,class System.String)
  IL_0046:  ret

  IL_0047:  ldarg.0
  IL_0048:  ldloc.0
  IL_0049:  ldc.i4.0
  IL_004a:  ldelema    int32
  IL_004f:  ldftn      class System.String rems(int32&,
                                                int32&)
  IL_0055:  tail.
  IL_0057:  calli      class System.String(int32&,int32&)
  IL_005c:  ret
} // end of global method rems

.class public auto ansi beforefieldinit Test_reference_i extends [mscorlib]System.Object
{
.method public static int32 main() il managed
{
  .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
      01 00 00 00
  )
  .entrypoint
  // Code size       66 (0x42)
  .maxstack  2
  .locals (int32 V_0,
           int32 V_1)
  IL_0000:  ldc.i4.2
  IL_0001:  stloc.0
  IL_0002:  br.s       IL_0008

  IL_0004:  ldloc.0
  IL_0005:  ldc.i4.1
  IL_0006:  add
  IL_0007:  stloc.0
  IL_0008:  ldloc.0
  IL_0009:  ldc.i4     0x3e8
  IL_000e:  bge.s      IL_0037

  IL_0010:  ldloc.0
  IL_0011:  ldc.i4.1
  IL_0012:  sub
  IL_0013:  stloc.1
  IL_0014:  ldloca.s   V_0
  IL_0016:  ldloca.s   V_1
  IL_0018:  call       class System.String rems(int32&,
                                                int32&)
  IL_001d:  brtrue.s   IL_0035

  IL_001f:  ldloca.s   V_0
  IL_0021:  call       instance class System.String [mscorlib]System.Int32::ToString()
  IL_0026:  call       void [System.Console]System.Console::Write(class System.String)
  IL_002b:  ldstr      " "
  IL_0030:  call       void [System.Console]System.Console::Write(class System.String)
  IL_0035:  br.s       IL_0004

  IL_0037:  call       void [System.Console]System.Console::WriteLine()
  IL_003c:  ldc.i4     0x64
  IL_0041:  ret
} // end of method main


.method public hidebysig  specialname rtspecialname
        instance void .ctor() il managed
{
  .maxstack  8
  IL_0000:  ldarg.0
  IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
  IL_0006:  ret
}
}

//*********** DISASSEMBLY COMPLETE ***********************
