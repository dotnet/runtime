// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'try-fault-struct03'
{

  // --- The following custom attribute is added automatically, do not uncomment -------
  //  .custom instance void [mscorlib]System.Diagnostics.DebuggableAttribute::.ctor(bool,
  //                                                                                bool) = ( 01 00 00 01 00 00 ) 

  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
// MVID: {64D27B72-3BAB-4B06-AC5B-6ECC5A9F3FB9}
.imagebase 0x00400000
.subsystem 0x00000003
.file alignment 512
.corflags 0x00000001
// Image base: 0x02DB0000

// =============== CLASS MEMBERS DECLARATION ===================

.class public sequential ansi sealed beforefieldinit ValX0
       extends [mscorlib]System.ValueType
{
  .pack 0
  .size 1
} // end of class ValX0

.class public sequential ansi sealed beforefieldinit ValY0
       extends [mscorlib]System.ValueType
{
  .pack 0
  .size 1
} // end of class ValY0

.class public sequential ansi sealed beforefieldinit ValX1<([mscorlib]System.Object) T>
       extends [mscorlib]System.ValueType
{
  .pack 0
  .size 1
} // end of class ValX1

.class public sequential ansi sealed beforefieldinit ValY1<([mscorlib]System.Object) T>
       extends [mscorlib]System.ValueType
{
  .pack 0
  .size 1
} // end of class ValY1

.class public sequential ansi sealed beforefieldinit ValX2<([mscorlib]System.Object) T,([mscorlib]System.Object) U>
       extends [mscorlib]System.ValueType
{
  .pack 0
  .size 1
} // end of class ValX2

.class public sequential ansi sealed beforefieldinit ValY2<([mscorlib]System.Object) T,([mscorlib]System.Object) U>
       extends [mscorlib]System.ValueType
{
  .pack 0
  .size 1
} // end of class ValY2

.class public sequential ansi sealed beforefieldinit ValX3<([mscorlib]System.Object) T,([mscorlib]System.Object) U,([mscorlib]System.Object) V>
       extends [mscorlib]System.ValueType
{
  .pack 0
  .size 1
} // end of class ValX3

.class public sequential ansi sealed beforefieldinit ValY3<([mscorlib]System.Object) T,([mscorlib]System.Object) U,([mscorlib]System.Object) V>
       extends [mscorlib]System.ValueType
{
  .pack 0
  .size 1
} // end of class ValY3

.class public auto ansi beforefieldinit RefX0
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method RefX0::.ctor

} // end of class RefX0

.class public auto ansi beforefieldinit RefY0
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method RefY0::.ctor

} // end of class RefY0

.class public auto ansi beforefieldinit RefX1<([mscorlib]System.Object) T>
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method RefX1::.ctor

} // end of class RefX1

.class public auto ansi beforefieldinit RefY1<([mscorlib]System.Object) T>
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method RefY1::.ctor

} // end of class RefY1

.class public auto ansi beforefieldinit RefX2<([mscorlib]System.Object) T,([mscorlib]System.Object) U>
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method RefX2::.ctor

} // end of class RefX2

.class public auto ansi beforefieldinit RefY2<([mscorlib]System.Object) T,([mscorlib]System.Object) U>
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method RefY2::.ctor

} // end of class RefY2

.class public auto ansi beforefieldinit RefX3<([mscorlib]System.Object) T,([mscorlib]System.Object) U,([mscorlib]System.Object) V>
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method RefX3::.ctor

} // end of class RefX3

.class public auto ansi beforefieldinit RefY3<([mscorlib]System.Object) T,([mscorlib]System.Object) U,([mscorlib]System.Object) V>
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method RefY3::.ctor

} // end of class RefY3

.class public auto ansi beforefieldinit GenException<([mscorlib]System.Object) T>
       extends [mscorlib]System.Exception
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Exception::.ctor()
    IL_0006:  ret
  } // end of method GenException::.ctor

} // end of class GenException

.class public sequential ansi sealed beforefieldinit Gen<([mscorlib]System.Object) T>
       extends [mscorlib]System.ValueType
{
  .field public bool hit
  .method public hidebysig instance void 
          InternalExceptionTest(bool throwException) cil managed
  {
    // Code size       42 (0x2a)
    .maxstack  2
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.0
    IL_0002:  stfld      bool valuetype Gen<!0>::hit
    IL_0007:  ldarg.1
    IL_0008:  brfalse.s  IL_0010

    IL_000a:  newobj     instance void class GenException<!0>::.ctor()
    IL_000f:  throw

    IL_0010:  ldarg.1
    IL_0011:  ldc.i4.0
    IL_0012:  ceq
    IL_0014:  call       void Test_try_fault_struct03::Eval(bool)
    IL_0019:  leave.s    IL_0028

    IL_001b:  ldarg.0
    IL_001c:  ldc.i4.1
    IL_001d:  stfld      bool valuetype Gen<!0>::hit
    IL_0022:  newobj     instance void class GenException<class RefX1<!0>>::.ctor()
    IL_0027:  throw

    IL_0028:  br.s       IL_0028
    IL_002a:  
    // Exception count 1
    .try IL_0007 to IL_001b fault handler IL_001b to IL_0028
  } // end of method Gen::InternalExceptionTest

  .method public hidebysig instance void 
          ExceptionTest(bool throwException) cil managed
  {
    // Code size       53 (0x35)
    .maxstack  2
    .locals init (class [mscorlib]System.Exception V_0)
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  call       instance void valuetype Gen<!0>::InternalExceptionTest(bool)
    IL_0007:  ldarg.1
    IL_0008:  ldc.i4.0
    IL_0009:  ceq
    IL_000b:  call       void Test_try_fault_struct03::Eval(bool)
    IL_0010:  leave.s    IL_0034

    IL_0012:  stloc.0
    IL_0013:  ldarg.0
    IL_0014:  ldfld      bool valuetype Gen<!0>::hit
    IL_0019:  call       void Test_try_fault_struct03::Eval(bool)
    IL_001e:  ldarg.1
    IL_001f:  call       void Test_try_fault_struct03::Eval(bool)
    IL_0024:  ldloc.0
    IL_0025:  isinst     class GenException<class RefX1<!0>>
    IL_002a:  ldnull
    IL_002b:  cgt.un
    IL_002d:  call       void Test_try_fault_struct03::Eval(bool)
    IL_0032:  leave.s    IL_0034

    IL_0034:  ret
    IL_0035:  
    // Exception count 1
    .try IL_0000 to IL_0012 catch [mscorlib]System.Exception handler IL_0012 to IL_0034
  } // end of method Gen::ExceptionTest

} // end of valuetype Gen

.class public auto ansi beforefieldinit Test_try_fault_struct03
       extends [mscorlib]System.Object
{
  .field public static int32 counter
  .field public static bool result
  .method public hidebysig static void  Eval(bool exp) cil managed
  {
    // Code size       47 (0x2f)
    .maxstack  8
    IL_0000:  ldsfld     int32 Test_try_fault_struct03::counter
    IL_0005:  ldc.i4.1
    IL_0006:  add
    IL_0007:  stsfld     int32 Test_try_fault_struct03::counter
    IL_000c:  ldarg.0
    IL_000d:  brtrue.s   IL_002e

    IL_000f:  ldarg.0
    IL_0010:  stsfld     bool Test_try_fault_struct03::result
    IL_0015:  ldstr      "Test Failed at location: "
    IL_001a:  ldsfld     int32 Test_try_fault_struct03::counter
    IL_001f:  box        [mscorlib]System.Int32
    IL_0024:  call       string [mscorlib]System.String::Concat(object,
                                                                object)
    IL_0029:  call       void [System.Console]System.Console::WriteLine(string)
    IL_002e:  ret
  } // end of method Test::Eval

.method public hidebysig static int32  Main() cil managed
{
  .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
      01 00 00 00
  )
  .entrypoint
  // Code size       1032 (0x408)
  .maxstack  2
  .locals init (int32 V_0,
           valuetype Gen<int32> V_1,
           valuetype Gen<float64> V_2,
           valuetype Gen<string> V_3,
           valuetype Gen<object> V_4,
           valuetype Gen<valuetype [mscorlib]System.Guid> V_5,
           valuetype Gen<int32[]> V_6,
           valuetype Gen<float64[0...,0...]> V_7,
           valuetype Gen<string[][][]> V_8,
           valuetype Gen<object[0...,0...,0...,0...]> V_9,
           valuetype Gen<valuetype [mscorlib]System.Guid[][0...,0...,0...,0...][]> V_10,
           valuetype Gen<class RefX1<int32>[]> V_11,
           valuetype Gen<class RefX1<float64>[0...,0...]> V_12,
           valuetype Gen<class RefX1<string>[][][]> V_13,
           valuetype Gen<class RefX1<object>[0...,0...,0...,0...]> V_14,
           valuetype Gen<class RefX1<valuetype [mscorlib]System.Guid>[][0...,0...,0...,0...][]> V_15,
           valuetype Gen<class RefX2<int32,int32>[]> V_16,
           valuetype Gen<class RefX2<float64,float64>[0...,0...]> V_17,
           valuetype Gen<class RefX2<string,string>[][][]> V_18,
           valuetype Gen<class RefX2<object,object>[0...,0...,0...,0...]> V_19,
           valuetype Gen<class RefX2<valuetype [mscorlib]System.Guid,valuetype [mscorlib]System.Guid>[][0...,0...,0...,0...][]> V_20,
           valuetype Gen<valuetype ValX1<int32>[]> V_21,
           valuetype Gen<valuetype ValX1<float64>[0...,0...]> V_22,
           valuetype Gen<valuetype ValX1<string>[][][]> V_23,
           valuetype Gen<valuetype ValX1<object>[0...,0...,0...,0...]> V_24,
           valuetype Gen<valuetype ValX1<valuetype [mscorlib]System.Guid>[][0...,0...,0...,0...][]> V_25,
           valuetype Gen<valuetype ValX2<int32,int32>[]> V_26,
           valuetype Gen<valuetype ValX2<float64,float64>[0...,0...]> V_27,
           valuetype Gen<valuetype ValX2<string,string>[][][]> V_28,
           valuetype Gen<valuetype ValX2<object,object>[0...,0...,0...,0...]> V_29,
           valuetype Gen<valuetype ValX2<valuetype [mscorlib]System.Guid,valuetype [mscorlib]System.Guid>[][0...,0...,0...,0...][]> V_30,
           valuetype Gen<class RefX1<int32>> V_31,
           valuetype Gen<class RefX1<valuetype ValX1<int32>>> V_32,
           valuetype Gen<class RefX2<int32,string>> V_33,
           valuetype Gen<class RefX3<int32,string,valuetype [mscorlib]System.Guid>> V_34,
           valuetype Gen<class RefX1<class RefX1<int32>>> V_35,
           valuetype Gen<class RefX1<class RefX1<class RefX1<string>>>> V_36,
           valuetype Gen<class RefX1<class RefX1<class RefX1<class RefX1<valuetype [mscorlib]System.Guid>>>>> V_37,
           valuetype Gen<class RefX1<class RefX2<int32,string>>> V_38,
           valuetype Gen<class RefX2<class RefX2<class RefX1<int32>,class RefX3<int32,string,class RefX1<class RefX2<int32,string>>>>,class RefX2<class RefX1<int32>,class RefX3<int32,string,class RefX1<class RefX2<int32,string>>>>>> V_39,
           valuetype Gen<class RefX3<class RefX1<int32[0...,0...,0...,0...][]>,class RefX2<object[][][0...,0...,0...,0...],valuetype [mscorlib]System.Guid[][][]>,class RefX3<float64[0...,0...,0...,0...,0...,0...,0...,0...,0...,0...,0...],valuetype [mscorlib]System.Guid[][][][0...,0...,0...,0...,0...][0...,0...,0...,0...,0...][][][],string[][][][][][][][][][][]>>> V_40,
           valuetype Gen<valuetype ValX1<int32>> V_41,
           valuetype Gen<valuetype ValX1<class RefX1<int32>>> V_42,
           valuetype Gen<valuetype ValX2<int32,string>> V_43,
           valuetype Gen<valuetype ValX3<int32,string,valuetype [mscorlib]System.Guid>> V_44,
           valuetype Gen<valuetype ValX1<valuetype ValX1<int32>>> V_45,
           valuetype Gen<valuetype ValX1<valuetype ValX1<valuetype ValX1<string>>>> V_46,
           valuetype Gen<valuetype ValX1<valuetype ValX1<valuetype ValX1<valuetype ValX1<valuetype [mscorlib]System.Guid>>>>> V_47,
           valuetype Gen<valuetype ValX1<valuetype ValX2<int32,string>>> V_48,
           valuetype Gen<valuetype ValX2<valuetype ValX2<valuetype ValX1<int32>,valuetype ValX3<int32,string,valuetype ValX1<valuetype ValX2<int32,string>>>>,valuetype ValX2<valuetype ValX1<int32>,valuetype ValX3<int32,string,valuetype ValX1<valuetype ValX2<int32,string>>>>>> V_49,
           valuetype Gen<valuetype ValX3<valuetype ValX1<int32[0...,0...,0...,0...][]>,valuetype ValX2<object[][][0...,0...,0...,0...],valuetype [mscorlib]System.Guid[][][]>,valuetype ValX3<float64[0...,0...,0...,0...,0...,0...,0...,0...,0...,0...,0...],valuetype [mscorlib]System.Guid[][][][0...,0...,0...,0...,0...][0...,0...,0...,0...,0...][][][],string[][][][][][][][][][][]>>> V_50)
  IL_0000:  ldloca.s   V_1
  IL_0002:  initobj    valuetype Gen<int32>
  IL_0008:  ldloc.1
  IL_0009:  stloc.1
  IL_000a:  ldloca.s   V_1
  IL_000c:  ldc.i4.1
  IL_000d:  call       instance void valuetype Gen<int32>::ExceptionTest(bool)
  IL_0012:  ldloca.s   V_2
  IL_0014:  initobj    valuetype Gen<float64>
  IL_001a:  ldloc.2
  IL_001b:  stloc.2
  IL_001c:  ldloca.s   V_2
  IL_001e:  ldc.i4.1
  IL_001f:  call       instance void valuetype Gen<float64>::ExceptionTest(bool)
  IL_0024:  ldloca.s   V_3
  IL_0026:  initobj    valuetype Gen<string>
  IL_002c:  ldloc.3
  IL_002d:  stloc.3
  IL_002e:  ldloca.s   V_3
  IL_0030:  ldc.i4.1
  IL_0031:  call       instance void valuetype Gen<string>::ExceptionTest(bool)
  IL_0036:  ldloca.s   V_4
  IL_0038:  initobj    valuetype Gen<object>
  IL_003e:  ldloc.s    V_4
  IL_0040:  stloc.s    V_4
  IL_0042:  ldloca.s   V_4
  IL_0044:  ldc.i4.1
  IL_0045:  call       instance void valuetype Gen<object>::ExceptionTest(bool)
  IL_004a:  ldloca.s   V_5
  IL_004c:  initobj    valuetype Gen<valuetype [mscorlib]System.Guid>
  IL_0052:  ldloc.s    V_5
  IL_0054:  stloc.s    V_5
  IL_0056:  ldloca.s   V_5
  IL_0058:  ldc.i4.1
  IL_0059:  call       instance void valuetype Gen<valuetype [mscorlib]System.Guid>::ExceptionTest(bool)
  IL_005e:  ldloca.s   V_6
  IL_0060:  initobj    valuetype Gen<int32[]>
  IL_0066:  ldloc.s    V_6
  IL_0068:  stloc.s    V_6
  IL_006a:  ldloca.s   V_6
  IL_006c:  ldc.i4.1
  IL_006d:  call       instance void valuetype Gen<int32[]>::ExceptionTest(bool)
  IL_0072:  ldloca.s   V_7
  IL_0074:  initobj    valuetype Gen<float64[0...,0...]>
  IL_007a:  ldloc.s    V_7
  IL_007c:  stloc.s    V_7
  IL_007e:  ldloca.s   V_7
  IL_0080:  ldc.i4.1
  IL_0081:  call       instance void valuetype Gen<float64[0...,0...]>::ExceptionTest(bool)
  IL_0086:  ldloca.s   V_8
  IL_0088:  initobj    valuetype Gen<string[][][]>
  IL_008e:  ldloc.s    V_8
  IL_0090:  stloc.s    V_8
  IL_0092:  ldloca.s   V_8
  IL_0094:  ldc.i4.1
  IL_0095:  call       instance void valuetype Gen<string[][][]>::ExceptionTest(bool)
  IL_009a:  ldloca.s   V_9
  IL_009c:  initobj    valuetype Gen<object[0...,0...,0...,0...]>
  IL_00a2:  ldloc.s    V_9
  IL_00a4:  stloc.s    V_9
  IL_00a6:  ldloca.s   V_9
  IL_00a8:  ldc.i4.1
  IL_00a9:  call       instance void valuetype Gen<object[0...,0...,0...,0...]>::ExceptionTest(bool)
  IL_00ae:  ldloca.s   V_10
  IL_00b0:  initobj    valuetype Gen<valuetype [mscorlib]System.Guid[][0...,0...,0...,0...][]>
  IL_00b6:  ldloc.s    V_10
  IL_00b8:  stloc.s    V_10
  IL_00ba:  ldloca.s   V_10
  IL_00bc:  ldc.i4.1
  IL_00bd:  call       instance void valuetype Gen<valuetype [mscorlib]System.Guid[][0...,0...,0...,0...][]>::ExceptionTest(bool)
  IL_00c2:  ldloca.s   V_11
  IL_00c4:  initobj    valuetype Gen<class RefX1<int32>[]>
  IL_00ca:  ldloc.s    V_11
  IL_00cc:  stloc.s    V_11
  IL_00ce:  ldloca.s   V_11
  IL_00d0:  ldc.i4.1
  IL_00d1:  call       instance void valuetype Gen<class RefX1<int32>[]>::ExceptionTest(bool)
  IL_00d6:  ldloca.s   V_12
  IL_00d8:  initobj    valuetype Gen<class RefX1<float64>[0...,0...]>
  IL_00de:  ldloc.s    V_12
  IL_00e0:  stloc.s    V_12
  IL_00e2:  ldloca.s   V_12
  IL_00e4:  ldc.i4.1
  IL_00e5:  call       instance void valuetype Gen<class RefX1<float64>[0...,0...]>::ExceptionTest(bool)
  IL_00ea:  ldloca.s   V_13
  IL_00ec:  initobj    valuetype Gen<class RefX1<string>[][][]>
  IL_00f2:  ldloc.s    V_13
  IL_00f4:  stloc.s    V_13
  IL_00f6:  ldloca.s   V_13
  IL_00f8:  ldc.i4.1
  IL_00f9:  call       instance void valuetype Gen<class RefX1<string>[][][]>::ExceptionTest(bool)
  IL_00fe:  ldloca.s   V_14
  IL_0100:  initobj    valuetype Gen<class RefX1<object>[0...,0...,0...,0...]>
  IL_0106:  ldloc.s    V_14
  IL_0108:  stloc.s    V_14
  IL_010a:  ldloca.s   V_14
  IL_010c:  ldc.i4.1
  IL_010d:  call       instance void valuetype Gen<class RefX1<object>[0...,0...,0...,0...]>::ExceptionTest(bool)
  IL_0112:  ldloca.s   V_15
  IL_0114:  initobj    valuetype Gen<class RefX1<valuetype [mscorlib]System.Guid>[][0...,0...,0...,0...][]>
  IL_011a:  ldloc.s    V_15
  IL_011c:  stloc.s    V_15
  IL_011e:  ldloca.s   V_15
  IL_0120:  ldc.i4.1
  IL_0121:  call       instance void valuetype Gen<class RefX1<valuetype [mscorlib]System.Guid>[][0...,0...,0...,0...][]>::ExceptionTest(bool)
  IL_0126:  ldloca.s   V_16
  IL_0128:  initobj    valuetype Gen<class RefX2<int32,int32>[]>
  IL_012e:  ldloc.s    V_16
  IL_0130:  stloc.s    V_16
  IL_0132:  ldloca.s   V_16
  IL_0134:  ldc.i4.1
  IL_0135:  call       instance void valuetype Gen<class RefX2<int32,int32>[]>::ExceptionTest(bool)
  IL_013a:  ldloca.s   V_17
  IL_013c:  initobj    valuetype Gen<class RefX2<float64,float64>[0...,0...]>
  IL_0142:  ldloc.s    V_17
  IL_0144:  stloc.s    V_17
  IL_0146:  ldloca.s   V_17
  IL_0148:  ldc.i4.1
  IL_0149:  call       instance void valuetype Gen<class RefX2<float64,float64>[0...,0...]>::ExceptionTest(bool)
  IL_014e:  ldloca.s   V_18
  IL_0150:  initobj    valuetype Gen<class RefX2<string,string>[][][]>
  IL_0156:  ldloc.s    V_18
  IL_0158:  stloc.s    V_18
  IL_015a:  ldloca.s   V_18
  IL_015c:  ldc.i4.1
  IL_015d:  call       instance void valuetype Gen<class RefX2<string,string>[][][]>::ExceptionTest(bool)
  IL_0162:  ldloca.s   V_19
  IL_0164:  initobj    valuetype Gen<class RefX2<object,object>[0...,0...,0...,0...]>
  IL_016a:  ldloc.s    V_19
  IL_016c:  stloc.s    V_19
  IL_016e:  ldloca.s   V_19
  IL_0170:  ldc.i4.1
  IL_0171:  call       instance void valuetype Gen<class RefX2<object,object>[0...,0...,0...,0...]>::ExceptionTest(bool)
  IL_0176:  ldloca.s   V_20
  IL_0178:  initobj    valuetype Gen<class RefX2<valuetype [mscorlib]System.Guid,valuetype [mscorlib]System.Guid>[][0...,0...,0...,0...][]>
  IL_017e:  ldloc.s    V_20
  IL_0180:  stloc.s    V_20
  IL_0182:  ldloca.s   V_20
  IL_0184:  ldc.i4.1
  IL_0185:  call       instance void valuetype Gen<class RefX2<valuetype [mscorlib]System.Guid,valuetype [mscorlib]System.Guid>[][0...,0...,0...,0...][]>::ExceptionTest(bool)
  IL_018a:  ldloca.s   V_21
  IL_018c:  initobj    valuetype Gen<valuetype ValX1<int32>[]>
  IL_0192:  ldloc.s    V_21
  IL_0194:  stloc.s    V_21
  IL_0196:  ldloca.s   V_21
  IL_0198:  ldc.i4.1
  IL_0199:  call       instance void valuetype Gen<valuetype ValX1<int32>[]>::ExceptionTest(bool)
  IL_019e:  ldloca.s   V_22
  IL_01a0:  initobj    valuetype Gen<valuetype ValX1<float64>[0...,0...]>
  IL_01a6:  ldloc.s    V_22
  IL_01a8:  stloc.s    V_22
  IL_01aa:  ldloca.s   V_22
  IL_01ac:  ldc.i4.1
  IL_01ad:  call       instance void valuetype Gen<valuetype ValX1<float64>[0...,0...]>::ExceptionTest(bool)
  IL_01b2:  ldloca.s   V_23
  IL_01b4:  initobj    valuetype Gen<valuetype ValX1<string>[][][]>
  IL_01ba:  ldloc.s    V_23
  IL_01bc:  stloc.s    V_23
  IL_01be:  ldloca.s   V_23
  IL_01c0:  ldc.i4.1
  IL_01c1:  call       instance void valuetype Gen<valuetype ValX1<string>[][][]>::ExceptionTest(bool)
  IL_01c6:  ldloca.s   V_24
  IL_01c8:  initobj    valuetype Gen<valuetype ValX1<object>[0...,0...,0...,0...]>
  IL_01ce:  ldloc.s    V_24
  IL_01d0:  stloc.s    V_24
  IL_01d2:  ldloca.s   V_24
  IL_01d4:  ldc.i4.1
  IL_01d5:  call       instance void valuetype Gen<valuetype ValX1<object>[0...,0...,0...,0...]>::ExceptionTest(bool)
  IL_01da:  ldloca.s   V_25
  IL_01dc:  initobj    valuetype Gen<valuetype ValX1<valuetype [mscorlib]System.Guid>[][0...,0...,0...,0...][]>
  IL_01e2:  ldloc.s    V_25
  IL_01e4:  stloc.s    V_25
  IL_01e6:  ldloca.s   V_25
  IL_01e8:  ldc.i4.1
  IL_01e9:  call       instance void valuetype Gen<valuetype ValX1<valuetype [mscorlib]System.Guid>[][0...,0...,0...,0...][]>::ExceptionTest(bool)
  IL_01ee:  ldloca.s   V_26
  IL_01f0:  initobj    valuetype Gen<valuetype ValX2<int32,int32>[]>
  IL_01f6:  ldloc.s    V_26
  IL_01f8:  stloc.s    V_26
  IL_01fa:  ldloca.s   V_26
  IL_01fc:  ldc.i4.1
  IL_01fd:  call       instance void valuetype Gen<valuetype ValX2<int32,int32>[]>::ExceptionTest(bool)
  IL_0202:  ldloca.s   V_27
  IL_0204:  initobj    valuetype Gen<valuetype ValX2<float64,float64>[0...,0...]>
  IL_020a:  ldloc.s    V_27
  IL_020c:  stloc.s    V_27
  IL_020e:  ldloca.s   V_27
  IL_0210:  ldc.i4.1
  IL_0211:  call       instance void valuetype Gen<valuetype ValX2<float64,float64>[0...,0...]>::ExceptionTest(bool)
  IL_0216:  ldloca.s   V_28
  IL_0218:  initobj    valuetype Gen<valuetype ValX2<string,string>[][][]>
  IL_021e:  ldloc.s    V_28
  IL_0220:  stloc.s    V_28
  IL_0222:  ldloca.s   V_28
  IL_0224:  ldc.i4.1
  IL_0225:  call       instance void valuetype Gen<valuetype ValX2<string,string>[][][]>::ExceptionTest(bool)
  IL_022a:  ldloca.s   V_29
  IL_022c:  initobj    valuetype Gen<valuetype ValX2<object,object>[0...,0...,0...,0...]>
  IL_0232:  ldloc.s    V_29
  IL_0234:  stloc.s    V_29
  IL_0236:  ldloca.s   V_29
  IL_0238:  ldc.i4.1
  IL_0239:  call       instance void valuetype Gen<valuetype ValX2<object,object>[0...,0...,0...,0...]>::ExceptionTest(bool)
  IL_023e:  ldloca.s   V_30
  IL_0240:  initobj    valuetype Gen<valuetype ValX2<valuetype [mscorlib]System.Guid,valuetype [mscorlib]System.Guid>[][0...,0...,0...,0...][]>
  IL_0246:  ldloc.s    V_30
  IL_0248:  stloc.s    V_30
  IL_024a:  ldloca.s   V_30
  IL_024c:  ldc.i4.1
  IL_024d:  call       instance void valuetype Gen<valuetype ValX2<valuetype [mscorlib]System.Guid,valuetype [mscorlib]System.Guid>[][0...,0...,0...,0...][]>::ExceptionTest(bool)
  IL_0252:  ldloca.s   V_31
  IL_0254:  initobj    valuetype Gen<class RefX1<int32>>
  IL_025a:  ldloc.s    V_31
  IL_025c:  stloc.s    V_31
  IL_025e:  ldloca.s   V_31
  IL_0260:  ldc.i4.1
  IL_0261:  call       instance void valuetype Gen<class RefX1<int32>>::ExceptionTest(bool)
  IL_0266:  ldloca.s   V_32
  IL_0268:  initobj    valuetype Gen<class RefX1<valuetype ValX1<int32>>>
  IL_026e:  ldloc.s    V_32
  IL_0270:  stloc.s    V_32
  IL_0272:  ldloca.s   V_32
  IL_0274:  ldc.i4.1
  IL_0275:  call       instance void valuetype Gen<class RefX1<valuetype ValX1<int32>>>::ExceptionTest(bool)
  IL_027a:  ldloca.s   V_33
  IL_027c:  initobj    valuetype Gen<class RefX2<int32,string>>
  IL_0282:  ldloc.s    V_33
  IL_0284:  stloc.s    V_33
  IL_0286:  ldloca.s   V_33
  IL_0288:  ldc.i4.1
  IL_0289:  call       instance void valuetype Gen<class RefX2<int32,string>>::ExceptionTest(bool)
  IL_028e:  ldloca.s   V_34
  IL_0290:  initobj    valuetype Gen<class RefX3<int32,string,valuetype [mscorlib]System.Guid>>
  IL_0296:  ldloc.s    V_34
  IL_0298:  stloc.s    V_34
  IL_029a:  ldloca.s   V_34
  IL_029c:  ldc.i4.1
  IL_029d:  call       instance void valuetype Gen<class RefX3<int32,string,valuetype [mscorlib]System.Guid>>::ExceptionTest(bool)
  IL_02a2:  ldloca.s   V_35
  IL_02a4:  initobj    valuetype Gen<class RefX1<class RefX1<int32>>>
  IL_02aa:  ldloc.s    V_35
  IL_02ac:  stloc.s    V_35
  IL_02ae:  ldloca.s   V_35
  IL_02b0:  ldc.i4.1
  IL_02b1:  call       instance void valuetype Gen<class RefX1<class RefX1<int32>>>::ExceptionTest(bool)
  IL_02b6:  ldloca.s   V_36
  IL_02b8:  initobj    valuetype Gen<class RefX1<class RefX1<class RefX1<string>>>>
  IL_02be:  ldloc.s    V_36
  IL_02c0:  stloc.s    V_36
  IL_02c2:  ldloca.s   V_36
  IL_02c4:  ldc.i4.1
  IL_02c5:  call       instance void valuetype Gen<class RefX1<class RefX1<class RefX1<string>>>>::ExceptionTest(bool)
  IL_02ca:  ldloca.s   V_37
  IL_02cc:  initobj    valuetype Gen<class RefX1<class RefX1<class RefX1<class RefX1<valuetype [mscorlib]System.Guid>>>>>
  IL_02d2:  ldloc.s    V_37
  IL_02d4:  stloc.s    V_37
  IL_02d6:  ldloca.s   V_37
  IL_02d8:  ldc.i4.1
  IL_02d9:  call       instance void valuetype Gen<class RefX1<class RefX1<class RefX1<class RefX1<valuetype [mscorlib]System.Guid>>>>>::ExceptionTest(bool)
  IL_02de:  ldloca.s   V_38
  IL_02e0:  initobj    valuetype Gen<class RefX1<class RefX2<int32,string>>>
  IL_02e6:  ldloc.s    V_38
  IL_02e8:  stloc.s    V_38
  IL_02ea:  ldloca.s   V_38
  IL_02ec:  ldc.i4.1
  IL_02ed:  call       instance void valuetype Gen<class RefX1<class RefX2<int32,string>>>::ExceptionTest(bool)
  IL_02f2:  ldloca.s   V_39
  IL_02f4:  initobj    valuetype Gen<class RefX2<class RefX2<class RefX1<int32>,class RefX3<int32,string,class RefX1<class RefX2<int32,string>>>>,class RefX2<class RefX1<int32>,class RefX3<int32,string,class RefX1<class RefX2<int32,string>>>>>>
  IL_02fa:  ldloc.s    V_39
  IL_02fc:  stloc.s    V_39
  IL_02fe:  ldloca.s   V_39
  IL_0300:  ldc.i4.1
  IL_0301:  call       instance void valuetype Gen<class RefX2<class RefX2<class RefX1<int32>,class RefX3<int32,string,class RefX1<class RefX2<int32,string>>>>,class RefX2<class RefX1<int32>,class RefX3<int32,string,class RefX1<class RefX2<int32,string>>>>>>::ExceptionTest(bool)
  IL_0306:  ldloca.s   V_40
  IL_0308:  initobj    valuetype Gen<class RefX3<class RefX1<int32[0...,0...,0...,0...][]>,class RefX2<object[][][0...,0...,0...,0...],valuetype [mscorlib]System.Guid[][][]>,class RefX3<float64[0...,0...,0...,0...,0...,0...,0...,0...,0...,0...,0...],valuetype [mscorlib]System.Guid[][][][0...,0...,0...,0...,0...][0...,0...,0...,0...,0...][][][],string[][][][][][][][][][][]>>>
  IL_030e:  ldloc.s    V_40
  IL_0310:  stloc.s    V_40
  IL_0312:  ldloca.s   V_40
  IL_0314:  ldc.i4.1
  IL_0315:  call       instance void valuetype Gen<class RefX3<class RefX1<int32[0...,0...,0...,0...][]>,class RefX2<object[][][0...,0...,0...,0...],valuetype [mscorlib]System.Guid[][][]>,class RefX3<float64[0...,0...,0...,0...,0...,0...,0...,0...,0...,0...,0...],valuetype [mscorlib]System.Guid[][][][0...,0...,0...,0...,0...][0...,0...,0...,0...,0...][][][],string[][][][][][][][][][][]>>>::ExceptionTest(bool)
  IL_031a:  ldloca.s   V_41
  IL_031c:  initobj    valuetype Gen<valuetype ValX1<int32>>
  IL_0322:  ldloc.s    V_41
  IL_0324:  stloc.s    V_41
  IL_0326:  ldloca.s   V_41
  IL_0328:  ldc.i4.1
  IL_0329:  call       instance void valuetype Gen<valuetype ValX1<int32>>::ExceptionTest(bool)
  IL_032e:  ldloca.s   V_42
  IL_0330:  initobj    valuetype Gen<valuetype ValX1<class RefX1<int32>>>
  IL_0336:  ldloc.s    V_42
  IL_0338:  stloc.s    V_42
  IL_033a:  ldloca.s   V_42
  IL_033c:  ldc.i4.1
  IL_033d:  call       instance void valuetype Gen<valuetype ValX1<class RefX1<int32>>>::ExceptionTest(bool)
  IL_0342:  ldloca.s   V_43
  IL_0344:  initobj    valuetype Gen<valuetype ValX2<int32,string>>
  IL_034a:  ldloc.s    V_43
  IL_034c:  stloc.s    V_43
  IL_034e:  ldloca.s   V_43
  IL_0350:  ldc.i4.1
  IL_0351:  call       instance void valuetype Gen<valuetype ValX2<int32,string>>::ExceptionTest(bool)
  IL_0356:  ldloca.s   V_44
  IL_0358:  initobj    valuetype Gen<valuetype ValX3<int32,string,valuetype [mscorlib]System.Guid>>
  IL_035e:  ldloc.s    V_44
  IL_0360:  stloc.s    V_44
  IL_0362:  ldloca.s   V_44
  IL_0364:  ldc.i4.1
  IL_0365:  call       instance void valuetype Gen<valuetype ValX3<int32,string,valuetype [mscorlib]System.Guid>>::ExceptionTest(bool)
  IL_036a:  ldloca.s   V_45
  IL_036c:  initobj    valuetype Gen<valuetype ValX1<valuetype ValX1<int32>>>
  IL_0372:  ldloc.s    V_45
  IL_0374:  stloc.s    V_45
  IL_0376:  ldloca.s   V_45
  IL_0378:  ldc.i4.1
  IL_0379:  call       instance void valuetype Gen<valuetype ValX1<valuetype ValX1<int32>>>::ExceptionTest(bool)
  IL_037e:  ldloca.s   V_46
  IL_0380:  initobj    valuetype Gen<valuetype ValX1<valuetype ValX1<valuetype ValX1<string>>>>
  IL_0386:  ldloc.s    V_46
  IL_0388:  stloc.s    V_46
  IL_038a:  ldloca.s   V_46
  IL_038c:  ldc.i4.1
  IL_038d:  call       instance void valuetype Gen<valuetype ValX1<valuetype ValX1<valuetype ValX1<string>>>>::ExceptionTest(bool)
  IL_0392:  ldloca.s   V_47
  IL_0394:  initobj    valuetype Gen<valuetype ValX1<valuetype ValX1<valuetype ValX1<valuetype ValX1<valuetype [mscorlib]System.Guid>>>>>
  IL_039a:  ldloc.s    V_47
  IL_039c:  stloc.s    V_47
  IL_039e:  ldloca.s   V_47
  IL_03a0:  ldc.i4.1
  IL_03a1:  call       instance void valuetype Gen<valuetype ValX1<valuetype ValX1<valuetype ValX1<valuetype ValX1<valuetype [mscorlib]System.Guid>>>>>::ExceptionTest(bool)
  IL_03a6:  ldloca.s   V_48
  IL_03a8:  initobj    valuetype Gen<valuetype ValX1<valuetype ValX2<int32,string>>>
  IL_03ae:  ldloc.s    V_48
  IL_03b0:  stloc.s    V_48
  IL_03b2:  ldloca.s   V_48
  IL_03b4:  ldc.i4.1
  IL_03b5:  call       instance void valuetype Gen<valuetype ValX1<valuetype ValX2<int32,string>>>::ExceptionTest(bool)
  IL_03ba:  ldloca.s   V_49
  IL_03bc:  initobj    valuetype Gen<valuetype ValX2<valuetype ValX2<valuetype ValX1<int32>,valuetype ValX3<int32,string,valuetype ValX1<valuetype ValX2<int32,string>>>>,valuetype ValX2<valuetype ValX1<int32>,valuetype ValX3<int32,string,valuetype ValX1<valuetype ValX2<int32,string>>>>>>
  IL_03c2:  ldloc.s    V_49
  IL_03c4:  stloc.s    V_49
  IL_03c6:  ldloca.s   V_49
  IL_03c8:  ldc.i4.1
  IL_03c9:  call       instance void valuetype Gen<valuetype ValX2<valuetype ValX2<valuetype ValX1<int32>,valuetype ValX3<int32,string,valuetype ValX1<valuetype ValX2<int32,string>>>>,valuetype ValX2<valuetype ValX1<int32>,valuetype ValX3<int32,string,valuetype ValX1<valuetype ValX2<int32,string>>>>>>::ExceptionTest(bool)
  IL_03ce:  ldloca.s   V_50
  IL_03d0:  initobj    valuetype Gen<valuetype ValX3<valuetype ValX1<int32[0...,0...,0...,0...][]>,valuetype ValX2<object[][][0...,0...,0...,0...],valuetype [mscorlib]System.Guid[][][]>,valuetype ValX3<float64[0...,0...,0...,0...,0...,0...,0...,0...,0...,0...,0...],valuetype [mscorlib]System.Guid[][][][0...,0...,0...,0...,0...][0...,0...,0...,0...,0...][][][],string[][][][][][][][][][][]>>>
  IL_03d6:  ldloc.s    V_50
  IL_03d8:  stloc.s    V_50
  IL_03da:  ldloca.s   V_50
  IL_03dc:  ldc.i4.1
  IL_03dd:  call       instance void valuetype Gen<valuetype ValX3<valuetype ValX1<int32[0...,0...,0...,0...][]>,valuetype ValX2<object[][][0...,0...,0...,0...],valuetype [mscorlib]System.Guid[][][]>,valuetype ValX3<float64[0...,0...,0...,0...,0...,0...,0...,0...,0...,0...,0...],valuetype [mscorlib]System.Guid[][][][0...,0...,0...,0...,0...][0...,0...,0...,0...,0...][][][],string[][][][][][][][][][][]>>>::ExceptionTest(bool)
  IL_03e2:  ldsfld     bool Test_try_fault_struct03::result
  IL_03e7:  brfalse.s  IL_03f8
  IL_03e9:  ldstr      "Test Passed"
  IL_03ee:  call       void [System.Console]System.Console::WriteLine(string)
  IL_03f3:  ldc.i4.s   100
  IL_03f5:  stloc.0
  IL_03f6:  br.s       IL_0406
  IL_03f8:  ldstr      "Test Failed"
  IL_03fd:  call       void [System.Console]System.Console::WriteLine(string)
  IL_0402:  ldc.i4.1
  IL_0403:  stloc.0
  IL_0404:  br.s       IL_0406
  IL_0406:  ldloc.0
  IL_0407:  ret
} // end of method Test::Main



  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  stsfld     int32 Test_try_fault_struct03::counter
    IL_0006:  ldc.i4.1
    IL_0007:  stsfld     bool Test_try_fault_struct03::result
    IL_000c:  ret
  } // end of method Test::.cctor

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


// =============================================================


//*********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file try-fault03.res
