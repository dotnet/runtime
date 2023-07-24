// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly '_Arrayssimple3'
{
}

.class public auto ansi Simple2_Array_Test
{
  .method public static int32 test(int32[,,,] Odd_Variable) il managed
  {
    // Method begins at RVA 0x1090
    // Code size       30 (0x1e)
    .maxstack  3
    .locals (int32 V_0)

    IL_0000:  ldstr      "Rank is :"
    IL_0005:  ldarg.0
    IL_0006:  callvirt   instance int32 [mscorlib]System.Array::get_Rank()
    IL_000b:  stloc.0
    IL_000c:  ldloca.s   V_0
    IL_000e:  call       instance class [mscorlib]System.String [mscorlib]System.Int32::ToString()
    IL_0013:  call class [mscorlib]System.String [mscorlib]System.String::Concat(class [mscorlib]System.String,class [mscorlib]System.String)
    IL_0018:  call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
              ldloc 0
    IL_001d:  ret
  }

  .method public static int32 Main() il managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Method begins at RVA 0x1090
    // Code size       230 (0xe6)
    .maxstack  7
    .locals (int32 SIZE,
             int32[0...,0...,0...,0...] foo,
             int32 i,
             int32 j,
             int32 k,
             int32 l,
             int32 m,
             int64 sum)

    IL_0000:  ldstr      "Starting..."
    IL_0005:  call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
    IL_000a:  ldc.i4.s   10
    IL_000c:  stloc.0
    IL_000d:  ldloc.0
    IL_000e:  ldloc.0
    IL_000f:  ldloc.0
    IL_0010:  ldloc.0
    IL_0011:  newobj     instance void int32[,,,]::.ctor(int32,int32,int32,int32)
    IL_0016:  stloc.1
ldloc 1
call int32 Simple2_Array_Test::test(int32[,,,])
ldc.i4 4
bne.un IL_00da
    IL_0017:  ldc.i4.0
    IL_0018:  conv.i8
    IL_0019:  stloc.s    sum
    IL_001b:  ldc.i4.0
    IL_001c:  stloc.2
    IL_001d:  br.s       IL_0064

    IL_001f:  ldc.i4.0
    IL_0020:  stloc.3
    IL_0021:  br.s       IL_005c

    IL_0023:  ldc.i4.0
    IL_0024:  stloc.s    k
    IL_0026:  br.s       IL_0053

    IL_0028:  ldc.i4.0
    IL_0029:  stloc.s    l
    IL_002b:  br.s       IL_0048

    IL_002d:  ldloc.1
    IL_002e:  ldloc.2
    IL_002f:  ldloc.3
    IL_0030:  ldloc.s    k
    IL_0032:  ldloc.s    l
    IL_0034:  ldloc.2
    IL_0035:  ldloc.3
    IL_0036:  mul
    IL_0037:  ldloc.s    k
    IL_0039:  mul
    IL_003a:  ldloc.s    l
    IL_003c:  mul
    IL_003d:  call       instance void int32[,,,]::Set(int32,int32,int32,int32,int32)
    IL_0042:  ldloc.s    l
    IL_0044:  ldc.i4.1
    IL_0045:  add
    IL_0046:  stloc.s    l
    IL_0048:  ldloc.s    l
    IL_004a:  ldloc.0
    IL_004b:  blt.s      IL_002d

    IL_004d:  ldloc.s    k
    IL_004f:  ldc.i4.1
    IL_0050:  add
    IL_0051:  stloc.s    k
    IL_0053:  ldloc.s    k
    IL_0055:  ldloc.0
    IL_0056:  blt.s      IL_0028

    IL_0058:  ldloc.3
    IL_0059:  ldc.i4.1
    IL_005a:  add
    IL_005b:  stloc.3
    IL_005c:  ldloc.3
    IL_005d:  ldloc.0
    IL_005e:  blt.s      IL_0023

    IL_0060:  ldloc.2
    IL_0061:  ldc.i4.1
    IL_0062:  add
    IL_0063:  stloc.2
    IL_0064:  ldloc.2
    IL_0065:  ldloc.0
    IL_0066:  blt.s      IL_001f

    IL_0068:  ldc.i4.0
    IL_0069:  stloc.2
    IL_006a:  br.s       IL_00c0

    IL_006c:  ldc.i4.0
    IL_006d:  stloc.3
    IL_006e:  br.s       IL_00b8

    IL_0070:  ldc.i4.0
    IL_0071:  stloc.s    k
    IL_0073:  br.s       IL_00af

    IL_0075:  ldc.i4.0
    IL_0076:  stloc.s    l
    IL_0078:  br.s       IL_00a3

    IL_007a:  ldc.i4.0
    IL_007b:  stloc.s    m
    IL_007d:  br.s       IL_0097


    IL_007f:  ldloc.s    sum
    IL_0081:  ldloc.1
    IL_0082:  ldloc.2
    IL_0083:  ldloc.3
    IL_0084:  ldloc.s    k
    IL_0086:  ldloc.s    l
    IL_0088:  call       instance int32 int32[,,,]::Get(int32,int32,int32,int32)
    IL_008d:  conv.i8
    IL_008e:  add
    IL_008f:  stloc.s    sum
    IL_0091:  ldloc.s    m
    IL_0093:  ldc.i4.1
    IL_0094:  add
    IL_0095:  stloc.s    m
    IL_0097:  ldloc.s    m
    IL_0099:  ldloc.s    l
    IL_009b:  blt.s      IL_007f

    IL_009d:  ldloc.s    l
    IL_009f:  ldc.i4.1
    IL_00a0:  add
    IL_00a1:  stloc.s    l
    IL_00a3:  ldloc.s    l
    IL_00a5:  ldloc.s    k
    IL_00a7:  blt.s      IL_007a

    IL_00a9:  ldloc.s    k
    IL_00ab:  ldc.i4.1
    IL_00ac:  add
    IL_00ad:  stloc.s    k
    IL_00af:  ldloc.s    k
    IL_00b1:  ldloc.3
    IL_00b2:  blt.s      IL_0075

    IL_00b4:  ldloc.3
    IL_00b5:  ldc.i4.1
    IL_00b6:  add
    IL_00b7:  stloc.3
    IL_00b8:  ldloc.3
    IL_00b9:  ldloc.2
    IL_00ba:  blt.s      IL_0070

    IL_00bc:  ldloc.2
    IL_00bd:  ldc.i4.1
    IL_00be:  add
    IL_00bf:  stloc.2
    IL_00c0:  ldloc.2
    IL_00c1:  ldloc.0
    IL_00c2:  blt.s      IL_006c

ldloc 1
call int32 Simple2_Array_Test::test(int32[,,,])
ldc.i4 4
bne.un IL_00da

    IL_00c4:  ldloc.s    sum
    IL_00c6:  ldc.i4     0x3022b
    IL_00cb:  conv.i8
    IL_00cc:  bne.un.s   IL_00da

    IL_00ce:  ldstr      "Everything Worked!"
    IL_00d3:  call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
    IL_00d8:  ldc.i4    0x64
    IL_00d9:  ret

    IL_00da:  ldstr      "Something is broken!"
    IL_00df:  call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
    IL_00e4:  ldc.i4.1
    IL_00e5:  ret
  } // end of method 'Simple2_Array_Test::Main'

  .method public specialname rtspecialname instance void .ctor() il managed
  {
    // Method begins at RVA 0x1182
    // Code size       7 (0x7)
    .maxstack  8

    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method 'Simple2_Array_Test::.ctor'

} // end of class 'Simple2_Array_Test'
