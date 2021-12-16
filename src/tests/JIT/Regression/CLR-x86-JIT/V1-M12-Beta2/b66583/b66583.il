// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b66583'
{
}
.assembly extern xunit.core {}
// MVID: {BEFEC472-D5C8-4747-807D-713417E95FBD}

.file alignment 512

// Image base: 0x03280000
.namespace QSortTest
{
  .class private auto ansi Sorter
         extends [mscorlib]System.Object
  {
    .method private hidebysig static void 
            Sort(unsigned int32[] A,
                 unsigned int32 L,
                 unsigned int32 H) cil managed
      { 
      .maxstack 10 
      ret }
     // end of method Sorter::Sort

    .method private hidebysig static int32
            Main() cil managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      // Code size       134 (0x86)
      .maxstack  4
      .locals (unsigned int32 V_0,
               unsigned int32[] V_1,
               unsigned int32 V_2,
               unsigned int32 V_3,
               unsigned int32 V_4,
               unsigned int32 V_5)
      IL_0000:  ldc.i4     0x1f40
      IL_0005:  stloc.0
      IL_0006:  ldloc.0
      IL_0007:  ldc.i4.1
      IL_0008:  add
      IL_0009:  newarr     unsigned int32
      IL_000e:  stloc.1
      IL_000f:  ldloc.0
      IL_0010:  ldc.i4     0x7cf
      IL_0015:  mul
      IL_0016:  stloc.2
      IL_0017:  ldc.i4.1
      IL_0018:  stloc.3
      IL_0019:  br.s       IL_0031

      IL_001b:  ldloc.1
      IL_001c:  ldloc.3
      IL_001d:  ldloc.2
      IL_001e:  ldc.i4.s   19
      IL_0020:  add
      IL_0021:  ldc.i4.s   17
      IL_0023:  mul
      IL_0024:  ldc.i4     0x7cf
      IL_0029:  rem
      IL_002a:  dup
      IL_002b:  stloc.2
      IL_002c:  stelem.i4
      IL_002d:  ldloc.3
      			
      IL_002e:  ldc.i4.1
      IL_002f:  add.ovf.un
      IL_0030:  stloc.3
      IL_0031:  ldloc.3
      IL_0032:  ldloc.0
      IL_0033:  ble.s      IL_001b

      IL_0035:  ldloc.1
      IL_0036:  ldc.i4.1
      IL_0037:  ldloc.0
      IL_0038:  call       void QSortTest.Sorter::Sort(unsigned int32[],
                                                       unsigned int32,
                                                       unsigned int32)
      IL_003d:  ldc.i4.2
      IL_003e:  stloc.s    V_4
      IL_0040:  br.s       IL_006f

      IL_0042:  ldloc.1
      IL_0043:  ldloc.s    V_4
      IL_0045:  ldc.i4.1
      IL_0046:  sub
      IL_0047:  ldelem.i4
      IL_0048:  ldloc.1
      IL_0049:  ldloc.s    V_4
      IL_004b:  ldelem.i4
      IL_004c:  ble.s      IL_0069

      IL_004e:  ldstr      "Test passed index "
      IL_0053:  ldloca.s   V_4
      IL_0055:  call       instance string [mscorlib]System.UInt32::ToString()
      IL_005a:  call       string [mscorlib]System.String::Concat(string,
                                                                  string)
      IL_005f:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0064:  ldc.i4 100
      IL_0065:  stloc.s    V_5
      IL_0067:  br.s       IL_0083

      IL_0069:  ldloc.s    V_4
      IL_006b:  ldc.i4.1
      IL_006c:  add
      IL_006d:  stloc.s    V_4
      IL_006f:  ldloc.s    V_4
      IL_0071:  ldloc.0
      IL_0072:  ble.s      IL_0042

      IL_0074:  ldstr      "test failed"
      IL_0079:  call       void [System.Console]System.Console::WriteLine(string)
      IL_007e:  ldc.i4 101
      IL_007f:  stloc.s    V_5
      IL_0081:  br.s       IL_0083

      IL_0083:  ldloc.s    V_5
      
      IL_0085:  ret
    } // end of method Sorter::Main

    .method public hidebysig specialname rtspecialname 
            instance void .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method Sorter::.ctor

  } // end of class Sorter

} // end of namespace QSortTest

//*********** DISASSEMBLY COMPLETE ***********************
