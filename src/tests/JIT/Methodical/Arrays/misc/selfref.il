// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.







.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ASSEMBLY_NAME
{


  //                                                                                  bool) = ( 01 00 00 01 00 00 ) 
  
  
}
.assembly extern xunit.core {}
// MVID: {57800D71-6F51-4ACF-BC86-8F3D2FBAC8E1}
.namespace GCTest_selfref_il
{
  .class public auto ansi Test
         extends [mscorlib]System.Object
  {
    .method public hidebysig static int32
            Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      // Code size       157 (0x9d)
      .maxstack  3
      .locals (class [mscorlib]System.Object &V_0,
               class [mscorlib]System.Object[] V_1,
               int32 V_2,
               class [mscorlib]System.Array V_3,
               int32 V_4,
               int32 V_5,
               class [mscorlib]System.Object dummy)
      IL_0000:  ldloca dummy
      IL_0001:  stloc.0
      IL_0002:  ldc.i4.s   16
      IL_0004:  newarr     [mscorlib]System.Object
      IL_0009:  stloc.1
      IL_000a:  ldloc.1
      IL_000b:  ldc.i4.0
      IL_000c:  call       instance int32 [mscorlib]System.Array::GetLowerBound(int32)
      IL_0011:  stloc.2
      IL_0012:  br.s       IL_001c

      IL_0014:  ldloc.1
      IL_0015:  ldloc.2
      IL_0016:  ldloc.1
      IL_0017:  stelem.ref
      IL_0018:  ldloc.2
      IL_0019:  ldc.i4.1
      IL_001a:  add
      IL_001b:  stloc.2
      IL_001c:  ldloc.2
      IL_001d:  ldloc.1
      IL_001e:  ldc.i4.0
      IL_001f:  call       instance int32 [mscorlib]System.Array::GetUpperBound(int32)
      IL_0024:  ble.s      IL_0014

      IL_0026:  ldloc.1
      IL_0027:  ldc.i4.s   11
      IL_0029:  ldelema		[mscorlib]System.Object
      IL_002a:  stloc.0
      IL_002b:  ldnull
      IL_002c:  stloc.1
      			call		void [mscorlib]System.GC::Collect()
      IL_002d:  ldloc.0
      			ldind.ref
      IL_002e:  castclass  [mscorlib]System.Array
      IL_0033:  stloc.3
      IL_0034:  ldloc.3
      IL_0035:  ldc.i4.0
      IL_0036:  call       instance int32 [mscorlib]System.Array::GetLowerBound(int32)
      IL_003b:  stloc.s    V_4
      IL_003d:  br.s       IL_0080

      IL_003f:  ldloc.3
      IL_0040:  ldloc.s    V_4
      IL_0042:  call       instance class [mscorlib]System.Object [mscorlib]System.Array::GetValue(int32)
      IL_0047:  castclass  [mscorlib]System.Array
      IL_004c:  ldc.i4.0
      IL_004d:  call       instance int32 [mscorlib]System.Array::GetLowerBound(int32)
      IL_0052:  brtrue.s   IL_006b

      IL_0054:  ldloc.3
      IL_0055:  ldloc.s    V_4
      IL_0057:  call       instance class [mscorlib]System.Object [mscorlib]System.Array::GetValue(int32)
      IL_005c:  castclass  [mscorlib]System.Array
      IL_0061:  ldc.i4.0
      IL_0062:  call       instance int32 [mscorlib]System.Array::GetUpperBound(int32)
      IL_0067:  ldc.i4.s   15
      IL_0069:  beq.s      IL_007a

      IL_006b:  ldstr      "TEST FAILED!"
      IL_0070:  call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
      IL_0075:  ldc.i4.1
      IL_0076:  stloc.s    V_5
      IL_0078:  br.s       IL_009a

      IL_007a:  ldloc.s    V_4
      IL_007c:  ldc.i4.1
      IL_007d:  add
      IL_007e:  stloc.s    V_4
      IL_0080:  ldloc.s    V_4
      IL_0082:  ldloc.3
      IL_0083:  ldc.i4.0
      IL_0084:  call       instance int32 [mscorlib]System.Array::GetUpperBound(int32)
      IL_0089:  ble.s      IL_003f

      IL_008b:  ldstr      "TEST PASSED!"
      IL_0090:  call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
      IL_0095:  ldc.i4    0x64
      IL_0096:  stloc.s    V_5
      IL_0098:  br.s       IL_009a

      IL_009a:  ldloc.s    V_5
      IL_009c:  ret
    } // end of method 'Test::Main'

    .method public hidebysig specialname rtspecialname 
            instance void .ctor() il managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method 'Test::.ctor'

  } // end of class 'Test'

} // end of namespace 'GCTest_selfref_il'

//*********** DISASSEMBLY COMPLETE ***********************
