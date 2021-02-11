// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// The test was reproducing a JIT bug in `STORE_OBJ` logic when `src` was a small `FIELD`
// in a special format that could not have been transformed as `LCL_FLD` and survived global morph
// as `IND small type` instead of `IND dstType`. A lowering optimization was incorrectly read that
// field and extended it to `dstType` instead of reading the requested dst size starting from that field
// address.


.assembly extern mscorlib {}
.assembly extern System.Runtime {}
.assembly extern System.Runtime.Extensions {}
.assembly extern System.Diagnostics.Debug {}

.assembly WPF_3226 {}

// =============== CLASS MEMBERS DECLARATION ===================

.class private auto ansi beforefieldinit Test
       extends [System.Runtime]System.Object
{

  // the struct needs many fields to prevent struct promotion.
  .class sequential ansi sealed nested private beforefieldinit B
         extends [System.Runtime]System.ValueType
  {
    .field public int8 a
    .field public int8 b
    .field public int8 c
    .field public int8 d
    .field public int8 e
    .field public int8 f
    .field public int8 g
    .field public int8 h
  } // end of class B
  
  .method public hidebysig static valuetype Test/B 
        CopyAStructUsingAddressOfASmallField(valuetype Test/B** s1) cil managed noinlining
{
  // Code size       14 (0xe)
  .maxstack  10
  .locals init (valuetype Test/B V_0,
           valuetype Test/B V_1)
  IL_0000:  nop
            ldloca.s   V_1
  IL_0001:  ldarg.0
            ldind.i
  IL_0026:  ldflda int8 Test/B::a
  IL_0028:  ldc.i4.8
            cpblk
  IL_000c:  ldloc.1
  IL_000d:  ret
} // end of method Test::CopyAStructUsingAddressOfASmallField

.method public hidebysig static int32  Main() cil managed
{
  .entrypoint
  // Code size       56 (0x38)
  .maxstack  2
  .locals init (valuetype Test/B V_0,
           valuetype Test/B* V_1,
           valuetype Test/B** V_2,
           valuetype Test/B V_3,
           int32 V_4)
  IL_0000:  nop
  IL_0001:  ldloca.s   V_0
  IL_0003:  initobj    Test/B
  IL_0009:  ldloca.s   V_0
  IL_000b:  ldc.i4.2
  IL_000c:  stfld      int8 Test/B::b
  IL_0011:  ldloca.s   V_0
  IL_0013:  conv.u
  IL_0014:  stloc.1
  IL_0015:  ldloca.s   V_1
  IL_0017:  conv.u
  IL_0018:  stloc.2
  IL_0019:  ldloc.2
  IL_001a:  call       valuetype Test/B Test::CopyAStructUsingAddressOfASmallField(valuetype Test/B**)
  IL_001f:  stloc.3
  IL_0020:  ldloc.3
  IL_0021:  ldfld      int8 Test/B::b
  IL_0026:  ldc.i4.2
  IL_0027:  ceq
  IL_0029:  call       void [System.Diagnostics.Debug]System.Diagnostics.Debug::Assert(bool)
  IL_002e:  nop
  IL_002f:  ldc.i4.s   100
  IL_0031:  stloc.s    V_4
  IL_0033:  br.s       IL_0035
  IL_0035:  ldloc.s    V_4
  IL_0037:  ret
} // end of method Test::Main

} // end of class Test
