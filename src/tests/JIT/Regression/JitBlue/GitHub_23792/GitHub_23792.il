// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This tests that 
// ((x+icon)+y) => ((x+y)+icon)
// and
// ((x+icon1)+(y+icon2)) => ((x+y)+(icon1+icon2))
// transformations in morph don't create byrefs pointing outside of the ref object.
// Method Run1 calculates (theRef - 8 + a -8 + b).
// Before the fix morph transformed this to ((theRef + a) + b -16).
// theRef + a may point outside of the object.
// Method Run2 calculates ((theRef - 8) + (a - 8) + b).
// Before the fix morph transformed this to ((theRef + a) + b -16).
// theRef + a may point outside of the object.

.assembly extern mscorlib{}
.assembly extern System.Console{}
.assembly a {}
.assembly extern xunit.core {}

.class private auto ansi beforefieldinit Test_GitHub_23792
       extends [mscorlib]System.Object
{

  //public static int Main()
  //{
  //    byte size = 9;
  //    byte[] arr = new byte[9];
  //    for (byte i = 0; i < size; ++i)
  //    {
  //         arr[i] = i;
  //    }
  //    ref byte newRef1 = ref Run1(ref arr[8], (IntPtr)8, (IntPtr)8);
  //    ref byte newRef2 = ref Run2(ref arr[8], (IntPtr)8, (IntPtr)8);
  //    int result = 84 + newRef1 + newRef2;
  //    if (result == 100)
  //    {
  //        Console.WriteLine("PASS");
  //    }
  //    else
  //    {
  //        Console.WriteLine("FAIL");
  //    }            
  //    return result;
  //}

  .method public hidebysig static int32  Main() cil managed
  {
        .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
            01 00 00 00
        )
        .entrypoint
  // Code size       119 (0x77)
  .maxstack  3
  .locals init (uint8 V_0,
           uint8[] V_1,
           uint8& V_2,
           uint8& V_3,
           uint8 V_4)
  IL_0000:  ldc.i4.s   9
  IL_0002:  stloc.0
  IL_0003:  ldc.i4.s   9
  IL_0005:  newarr     [mscorlib]System.Byte
  IL_000a:  stloc.1
  IL_000b:  ldc.i4.0
  IL_000c:  stloc.s    V_4
  IL_000e:  br.s       IL_001d
  IL_0010:  ldloc.1
  IL_0011:  ldloc.s    V_4
  IL_0013:  ldloc.s    V_4
  IL_0015:  stelem.i1
  IL_0016:  ldloc.s    V_4
  IL_0018:  ldc.i4.1
  IL_0019:  add
  IL_001a:  conv.u1
  IL_001b:  stloc.s    V_4
  IL_001d:  ldloc.s    V_4
  IL_001f:  ldloc.0
  IL_0020:  blt.s      IL_0010
  IL_0022:  ldloc.1
  IL_0023:  ldc.i4.8
  IL_0024:  ldelema    [mscorlib]System.Byte
  IL_0029:  ldc.i4.8
  IL_002a:  call       native int [mscorlib]System.IntPtr::op_Explicit(int32)
  IL_002f:  ldc.i4.8
  IL_0030:  call       native int [mscorlib]System.IntPtr::op_Explicit(int32)
  IL_0035:  call       uint8& Test_GitHub_23792::Run1(uint8&,
                                        native int,
                                        native int)
  IL_003a:  stloc.2
  IL_003b:  ldloc.1
  IL_003c:  ldc.i4.8
  IL_003d:  ldelema    [mscorlib]System.Byte
  IL_0042:  ldc.i4.8
  IL_0043:  call       native int [mscorlib]System.IntPtr::op_Explicit(int32)
  IL_0048:  ldc.i4.8
  IL_0049:  call       native int [mscorlib]System.IntPtr::op_Explicit(int32)
  IL_004e:  call       uint8& Test_GitHub_23792::Run2(uint8&,
                                        native int,
                                        native int)
  IL_0053:  stloc.3
  IL_0054:  ldc.i4.s   84
  IL_0056:  ldloc.2
  IL_0057:  ldind.u1
  IL_0058:  add
  IL_0059:  ldloc.3
  IL_005a:  ldind.u1
  IL_005b:  add
  IL_005c:  dup
  IL_005d:  ldc.i4.s   100
  IL_005f:  bne.un.s   IL_006c
  IL_0061:  ldstr      "PASS"
  IL_0066:  call       void [System.Console]System.Console::WriteLine(string)
  IL_006b:  ret
  IL_006c:  ldstr      "FAIL"
  IL_0071:  call       void [System.Console]System.Console::WriteLine(string)
  IL_0076:  ret
} // end of method Test::Main


  .method private hidebysig static uint8& 
          Run1(uint8& theRef,
              native int a,
              native int b) cil managed noinlining
  {
    ldarg.0
    ldc.i4    8
    sub
    ldarg.1
    ldc.i4    8
    sub
    add
    ldc.i4    8
    add
    ret
  } // end of method Test::Run1

    .method private hidebysig static uint8& 
          Run2(uint8& theRef,
              native int a,
              native int b) cil managed noinlining
  {
    ldarg.0
    ldc.i4    -8
    add
    ldarg.1
    ldc.i4    -8
    add
    add
    ldarg.2
    add
    ret
  } // end of method Test::Run2

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Test::.ctor

} // end of class Test
