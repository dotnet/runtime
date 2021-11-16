// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern System.Console { auto }
.assembly extern xunit.core {}
.assembly extern System.Runtime.Extensions { auto }
.assembly extern mscorlib { auto }

.assembly tailcall
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 )
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module tailcall.exe
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003
.corflags 0x00000001



.class interface private abstract auto ansi IFace1
{
  .method public hidebysig newslot abstract virtual
          instance int32  Recurse3(int32 depth) cil managed
  {
  }

  .method public hidebysig newslot abstract virtual
          instance int32  Recurse4(int32 depth,
                                   object o1,
                                   object o2) cil managed
  {
  }

}

.class public auto ansi beforefieldinit Class1
       extends [mscorlib]System.Object
       implements IFace1
{
  .field private static int32 MaxDepth
  .field private static int32 Expected
  .field private int32 'value'
  .method public hidebysig instance int32
          Recurse1(int32 depth) cil managed
  {
    .maxstack  5
    IL_0000:  ldarg.1
    IL_0001:  brtrue.s   IL_000a

    IL_0003:  ldarg.0
    IL_0004:  ldfld      int32 Class1::'value'
    IL_0009:  ret

    IL_000a:  ldarg.0
    IL_000b:  dup
    IL_000c:  ldfld      int32 Class1::'value'
    IL_0011:  ldarg.1
    IL_0012:  add
    IL_0013:  stfld      int32 Class1::'value'
    IL_0018:  ldarg.1
    IL_0019:  ldc.i4     0x80
    IL_001e:  rem
    IL_001f:  ldc.i4.s   43
    IL_0021:  bne.un.s   IL_002d

    IL_0023:  ldarg.0
    IL_0024:  ldarg.1
    IL_0025:  ldc.i4.1
    IL_0026:  sub
    IL_0027:  tail. callvirt   instance int32 Class1::Recurse3(int32)
    IL_002c:  ret

    IL_002d:  ldarg.0
    IL_002e:  ldarg.1
    IL_002f:  ldc.i4.1
    IL_0030:  sub
    IL_0031:  ldarg.1
    IL_0032:  box        [mscorlib]System.Int32
    IL_0037:  ldarg.1
    IL_0038:  ldc.i4.1
    IL_0039:  add
    IL_003a:  box        [mscorlib]System.Int32
    IL_003f:  tail. call       instance int32 Class1::Recurse2(int32,
                                                         object,
                                                         object)
    IL_0044:  ret
  }

  .method public hidebysig instance int32
          Recurse2(int32 depth,
                   object o1,
                   object o2) cil managed
  {
    .maxstack  4
    .locals init (string V_0)
    IL_0000:  ldarg.1
    IL_0001:  brtrue.s   IL_000a

    IL_0003:  ldarg.0
    IL_0004:  ldfld      int32 Class1::'value'
    IL_0009:  ret

    IL_000a:  ldarg.1
    IL_000b:  ldsfld     int32 Class1::MaxDepth
    IL_0010:  ldc.i4.s   10
    IL_0012:  div
    IL_0013:  rem
    IL_0014:  ldc.i4.s   100
    IL_0016:  bne.un.s   IL_0051

    IL_0018:  call       string [System.Runtime.Extensions]System.Environment::get_StackTrace()
    IL_001d:  stloc.0
    IL_001e:  ldloc.0
    IL_001f:  ldstr      "Main"
    IL_0024:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_0029:  ldc.i4.m1
    IL_002a:  bne.un.s   IL_004c

    IL_002c:  ldstr      "Unexpected stack trace: "
    IL_0031:  ldloc.0
    IL_0032:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0037:  call       void [System.Console]System.Console::WriteLine(string)
    IL_003c:  ldstr      "Test Failed"
    IL_0041:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0046:  ldc.i4.0
    IL_0047:  call       void [System.Runtime.Extensions]System.Environment::Exit(int32)
    IL_004c:  call       void [mscorlib]System.GC::Collect()
    IL_0051:  ldarg.0
    IL_0052:  dup
    IL_0053:  ldfld      int32 Class1::'value'
    IL_0058:  ldarg.2
    IL_0059:  unbox      [mscorlib]System.Int32
    IL_005e:  ldind.i4
    IL_005f:  ldarg.3
    IL_0060:  unbox      [mscorlib]System.Int32
    IL_0065:  ldind.i4
    IL_0066:  add
    IL_0067:  add
    IL_0068:  stfld      int32 Class1::'value'
    IL_006d:  ldarg.0
    IL_006e:  ldarg.1
    IL_006f:  ldc.i4.1
    IL_0070:  sub
    IL_0071:  tail. callvirt   instance int32 IFace1::Recurse3(int32)
    IL_0076:  ret
  }

  .method public hidebysig newslot virtual
          instance int32  Recurse3(int32 depth) cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.1
    IL_0001:  brtrue.s   IL_000a

    IL_0003:  ldarg.0
    IL_0004:  ldfld      int32 Class1::'value'
    IL_0009:  ret

    IL_000a:  ldarg.1
    IL_000b:  ldc.i4     0x80
    IL_0010:  rem
    IL_0011:  ldc.i4.s   21
    IL_0013:  bne.un.s   IL_001f

    IL_0015:  ldarg.0
    IL_0016:  ldarg.1
    IL_0017:  ldc.i4.1
    IL_0018:  sub
    IL_0019:  tail. call       instance int32 Class1::Recurse1(int32)
    IL_001e:  ret

    IL_001f:  ldarg.0
    IL_0020:  ldarg.1
    IL_0021:  ldc.i4.1
    IL_0022:  sub
    IL_0023:  ldarg.1
    IL_0024:  ldc.i4.1
    IL_0025:  add
    IL_0026:  box        [mscorlib]System.Int32
    IL_002b:  ldarg.1
    IL_002c:  ldc.i4.2
    IL_002d:  add
    IL_002e:  box        [mscorlib]System.Int32
    IL_0033:  tail. callvirt   instance int32 IFace1::Recurse4(int32,
                                                         object,
                                                         object)
    IL_0038:  ret
  }

  .method public hidebysig newslot virtual
          instance int32  Recurse4(int32 depth,
                                   object o1,
                                   object o2) cil managed
  {
    .maxstack  4
    .locals init (string V_0)
    IL_0000:  ldarg.1
    IL_0001:  brtrue.s   IL_000a

    IL_0003:  ldarg.0
    IL_0004:  ldfld      int32 Class1::'value'
    IL_0009:  ret

    IL_000a:  ldarg.1
    IL_000b:  ldsfld     int32 Class1::MaxDepth
    IL_0010:  ldc.i4.s   10
    IL_0012:  div
    IL_0013:  rem
    IL_0014:  ldc.i4     0xc8
    IL_0019:  bne.un.s   IL_0054

    IL_001b:  call       void [mscorlib]System.GC::Collect()
    IL_0020:  call       string [System.Runtime.Extensions]System.Environment::get_StackTrace()
    IL_0025:  stloc.0
    IL_0026:  ldloc.0
    IL_0027:  ldstr      "Main"
    IL_002c:  callvirt   instance int32 [mscorlib]System.String::IndexOf(string)
    IL_0031:  ldc.i4.m1
    IL_0032:  bne.un.s   IL_0054

    IL_0034:  ldstr      "Unexpected stack trace: "
    IL_0039:  ldloc.0
    IL_003a:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_003f:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0044:  ldstr      "Test Failed"
    IL_0049:  call       void [System.Console]System.Console::WriteLine(string)
    IL_004e:  ldc.i4.0
    IL_004f:  call       void [System.Runtime.Extensions]System.Environment::Exit(int32)
    IL_0054:  ldarg.0
    IL_0055:  dup
    IL_0056:  ldfld      int32 Class1::'value'
    IL_005b:  ldarg.2
    IL_005c:  unbox      [mscorlib]System.Int32
    IL_0061:  ldind.i4
    IL_0062:  ldarg.3
    IL_0063:  unbox      [mscorlib]System.Int32
    IL_0068:  ldind.i4
    IL_0069:  add
    IL_006a:  add
    IL_006b:  stfld      int32 Class1::'value'
    IL_0070:  ldarg.0
    IL_0071:  ldarg.1
    IL_0072:  ldc.i4.1
    IL_0073:  sub
    IL_0074:  tail. call       instance int32 Class1::Recurse1(int32)
    IL_0079:  ret
  }

  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  3
    .locals init (class Class1 V_0,
             object[] V_1)
    IL_0000:  ldstr      "Test Start"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  newobj     instance void Class1::.ctor()
    IL_000f:  stloc.0
    IL_0010:  ldloc.0
    IL_0011:  ldsfld     int32 Class1::MaxDepth
    IL_0016:  callvirt   instance int32 Class1::Recurse1(int32)
    IL_001b:  pop
    IL_001c:  ldloc.0
    IL_001d:  ldfld      int32 Class1::'value'
    IL_0022:  ldsfld     int32 Class1::Expected
    IL_0027:  beq.s      IL_006e

    IL_0029:  ldc.i4.4
    IL_002a:  newarr     [mscorlib]System.Object
    IL_002f:  stloc.1
    IL_0030:  ldloc.1
    IL_0031:  ldc.i4.0
    IL_0032:  ldstr      "Expected result: "
    IL_0037:  stelem.ref
    IL_0038:  ldloc.1
    IL_0039:  ldc.i4.1
    IL_003a:  ldc.i4.1
    IL_003b:  box        [mscorlib]System.Int32
    IL_0040:  stelem.ref
    IL_0041:  ldloc.1
    IL_0042:  ldc.i4.2
    IL_0043:  ldstr      " Actual result: "
    IL_0048:  stelem.ref
    IL_0049:  ldloc.1
    IL_004a:  ldc.i4.3
    IL_004b:  ldloc.0
    IL_004c:  ldfld      int32 Class1::'value'
    IL_0051:  box        [mscorlib]System.Int32
    IL_0056:  stelem.ref
    IL_0057:  ldloc.1
    IL_0058:  call       string [mscorlib]System.String::Concat(object[])
    IL_005d:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0062:  ldstr      "Test Failed"
    IL_0067:  call       void [System.Console]System.Console::WriteLine(string)
    IL_006c:  ldc.i4.0
    IL_006d:  ret

    IL_006e:  ldstr      "Test SUCCESS"
    IL_0073:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0078:  ldc.i4.s   100
    IL_007a:  ret
  }

  .method private hidebysig specialname rtspecialname static
          void  .cctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldc.i4     0x100000
    IL_0005:  stsfld     int32 Class1::MaxDepth
    IL_000a:  ldc.i4     0x280000
    IL_000f:  stsfld     int32 Class1::Expected
    IL_0014:  ret
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
