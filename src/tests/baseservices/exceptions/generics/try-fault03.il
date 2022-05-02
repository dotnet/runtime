// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'try-fault03'
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

.class public auto ansi beforefieldinit Gen<([mscorlib]System.Object) T>
       extends [mscorlib]System.Object
{
  .field public bool hit
  .method public hidebysig instance void 
          InternalExceptionTest(bool throwException) cil managed
  {
    // Code size       42 (0x2a)
    .maxstack  2
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.0
    IL_0002:  stfld      bool class Gen<!0>::hit
    IL_0007:  ldarg.1
    IL_0008:  brfalse.s  IL_0010

    IL_000a:  newobj     instance void class GenException<!0>::.ctor()
    IL_000f:  throw

    IL_0010:  ldarg.1
    IL_0011:  ldc.i4.0
    IL_0012:  ceq
    IL_0014:  call       void Test_try_fault03::Eval(bool)
    IL_0019:  leave.s    IL_0028

    IL_001b:  ldarg.0
    IL_001c:  ldc.i4.1
    IL_001d:  stfld      bool class Gen<!0>::hit
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
    IL_0002:  call       instance void class Gen<!0>::InternalExceptionTest(bool)
    IL_0007:  ldarg.1
    IL_0008:  ldc.i4.0
    IL_0009:  ceq
    IL_000b:  call       void Test_try_fault03::Eval(bool)
    IL_0010:  leave.s    IL_0034

    IL_0012:  stloc.0
    IL_0013:  ldarg.0
    IL_0014:  ldfld      bool class Gen<!0>::hit
    IL_0019:  call       void Test_try_fault03::Eval(bool)
    IL_001e:  ldarg.1
    IL_001f:  call       void Test_try_fault03::Eval(bool)
    IL_0024:  ldloc.0
    IL_0025:  isinst     class GenException<class RefX1<!0>>
    IL_002a:  ldnull
    IL_002b:  cgt.un
    IL_002d:  call       void Test_try_fault03::Eval(bool)
    IL_0032:  leave.s    IL_0034

    IL_0034:  ret
    IL_0035:  
    // Exception count 1
    .try IL_0000 to IL_0012 catch [mscorlib]System.Exception handler IL_0012 to IL_0034
  } // end of method Gen::ExceptionTest

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Gen::.ctor

} // end of class Gen

.class public auto ansi beforefieldinit Test_try_fault03
       extends [mscorlib]System.Object
{
  .field public static int32 counter
  .field public static bool result
  .method public hidebysig static void  Eval(bool exp) cil managed
  {
    // Code size       47 (0x2f)
    .maxstack  8
    IL_0000:  ldsfld     int32 Test_try_fault03::counter
    IL_0005:  ldc.i4.1
    IL_0006:  add
    IL_0007:  stsfld     int32 Test_try_fault03::counter
    IL_000c:  ldarg.0
    IL_000d:  brtrue.s   IL_002e

    IL_000f:  ldarg.0
    IL_0010:  stsfld     bool Test_try_fault03::result
    IL_0015:  ldstr      "Test Failed at location: "
    IL_001a:  ldsfld     int32 Test_try_fault03::counter
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
    // Code size       588 (0x24c)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  newobj     instance void class Gen<int32>::.ctor()
    IL_0005:  ldc.i4.1
    IL_0006:  call       instance void class Gen<int32>::ExceptionTest(bool)
    IL_000b:  newobj     instance void class Gen<float64>::.ctor()
    IL_0010:  ldc.i4.1
    IL_0011:  call       instance void class Gen<float64>::ExceptionTest(bool)
    IL_0016:  newobj     instance void class Gen<string>::.ctor()
    IL_001b:  ldc.i4.1
    IL_001c:  call       instance void class Gen<string>::ExceptionTest(bool)
    IL_0021:  newobj     instance void class Gen<object>::.ctor()
    IL_0026:  ldc.i4.1
    IL_0027:  call       instance void class Gen<object>::ExceptionTest(bool)
    IL_002c:  newobj     instance void class Gen<valuetype [mscorlib]System.Guid>::.ctor()
    IL_0031:  ldc.i4.1
    IL_0032:  call       instance void class Gen<valuetype [mscorlib]System.Guid>::ExceptionTest(bool)
    IL_0037:  newobj     instance void class Gen<int32[]>::.ctor()
    IL_003c:  ldc.i4.1
    IL_003d:  call       instance void class Gen<int32[]>::ExceptionTest(bool)
    IL_0042:  newobj     instance void class Gen<float64[0...,0...]>::.ctor()
    IL_0047:  ldc.i4.1
    IL_0048:  call       instance void class Gen<float64[0...,0...]>::ExceptionTest(bool)
    IL_004d:  newobj     instance void class Gen<string[][][]>::.ctor()
    IL_0052:  ldc.i4.1
    IL_0053:  call       instance void class Gen<string[][][]>::ExceptionTest(bool)
    IL_0058:  newobj     instance void class Gen<object[0...,0...,0...,0...]>::.ctor()
    IL_005d:  ldc.i4.1
    IL_005e:  call       instance void class Gen<object[0...,0...,0...,0...]>::ExceptionTest(bool)
    IL_0063:  newobj     instance void class Gen<valuetype [mscorlib]System.Guid[][0...,0...,0...,0...][]>::.ctor()
    IL_0068:  ldc.i4.1
    IL_0069:  call       instance void class Gen<valuetype [mscorlib]System.Guid[][0...,0...,0...,0...][]>::ExceptionTest(bool)
    IL_006e:  newobj     instance void class Gen<class RefX1<int32>[]>::.ctor()
    IL_0073:  ldc.i4.1
    IL_0074:  call       instance void class Gen<class RefX1<int32>[]>::ExceptionTest(bool)
    IL_0079:  newobj     instance void class Gen<class RefX1<float64>[0...,0...]>::.ctor()
    IL_007e:  ldc.i4.1
    IL_007f:  call       instance void class Gen<class RefX1<float64>[0...,0...]>::ExceptionTest(bool)
    IL_0084:  newobj     instance void class Gen<class RefX1<string>[][][]>::.ctor()
    IL_0089:  ldc.i4.1
    IL_008a:  call       instance void class Gen<class RefX1<string>[][][]>::ExceptionTest(bool)
    IL_008f:  newobj     instance void class Gen<class RefX1<object>[0...,0...,0...,0...]>::.ctor()
    IL_0094:  ldc.i4.1
    IL_0095:  call       instance void class Gen<class RefX1<object>[0...,0...,0...,0...]>::ExceptionTest(bool)
    IL_009a:  newobj     instance void class Gen<class RefX1<valuetype [mscorlib]System.Guid>[][0...,0...,0...,0...][]>::.ctor()
    IL_009f:  ldc.i4.1
    IL_00a0:  call       instance void class Gen<class RefX1<valuetype [mscorlib]System.Guid>[][0...,0...,0...,0...][]>::ExceptionTest(bool)
    IL_00a5:  newobj     instance void class Gen<class RefX2<int32,int32>[]>::.ctor()
    IL_00aa:  ldc.i4.1
    IL_00ab:  call       instance void class Gen<class RefX2<int32,int32>[]>::ExceptionTest(bool)
    IL_00b0:  newobj     instance void class Gen<class RefX2<float64,float64>[0...,0...]>::.ctor()
    IL_00b5:  ldc.i4.1
    IL_00b6:  call       instance void class Gen<class RefX2<float64,float64>[0...,0...]>::ExceptionTest(bool)
    IL_00bb:  newobj     instance void class Gen<class RefX2<string,string>[][][]>::.ctor()
    IL_00c0:  ldc.i4.1
    IL_00c1:  call       instance void class Gen<class RefX2<string,string>[][][]>::ExceptionTest(bool)
    IL_00c6:  newobj     instance void class Gen<class RefX2<object,object>[0...,0...,0...,0...]>::.ctor()
    IL_00cb:  ldc.i4.1
    IL_00cc:  call       instance void class Gen<class RefX2<object,object>[0...,0...,0...,0...]>::ExceptionTest(bool)
    IL_00d1:  newobj     instance void class Gen<class RefX2<valuetype [mscorlib]System.Guid,valuetype [mscorlib]System.Guid>[][0...,0...,0...,0...][]>::.ctor()
    IL_00d6:  ldc.i4.1
    IL_00d7:  call       instance void class Gen<class RefX2<valuetype [mscorlib]System.Guid,valuetype [mscorlib]System.Guid>[][0...,0...,0...,0...][]>::ExceptionTest(bool)
    IL_00dc:  newobj     instance void class Gen<valuetype ValX1<int32>[]>::.ctor()
    IL_00e1:  ldc.i4.1
    IL_00e2:  call       instance void class Gen<valuetype ValX1<int32>[]>::ExceptionTest(bool)
    IL_00e7:  newobj     instance void class Gen<valuetype ValX1<float64>[0...,0...]>::.ctor()
    IL_00ec:  ldc.i4.1
    IL_00ed:  call       instance void class Gen<valuetype ValX1<float64>[0...,0...]>::ExceptionTest(bool)
    IL_00f2:  newobj     instance void class Gen<valuetype ValX1<string>[][][]>::.ctor()
    IL_00f7:  ldc.i4.1
    IL_00f8:  call       instance void class Gen<valuetype ValX1<string>[][][]>::ExceptionTest(bool)
    IL_00fd:  newobj     instance void class Gen<valuetype ValX1<object>[0...,0...,0...,0...]>::.ctor()
    IL_0102:  ldc.i4.1
    IL_0103:  call       instance void class Gen<valuetype ValX1<object>[0...,0...,0...,0...]>::ExceptionTest(bool)
    IL_0108:  newobj     instance void class Gen<valuetype ValX1<valuetype [mscorlib]System.Guid>[][0...,0...,0...,0...][]>::.ctor()
    IL_010d:  ldc.i4.1
    IL_010e:  call       instance void class Gen<valuetype ValX1<valuetype [mscorlib]System.Guid>[][0...,0...,0...,0...][]>::ExceptionTest(bool)
    IL_0113:  newobj     instance void class Gen<valuetype ValX2<int32,int32>[]>::.ctor()
    IL_0118:  ldc.i4.1
    IL_0119:  call       instance void class Gen<valuetype ValX2<int32,int32>[]>::ExceptionTest(bool)
    IL_011e:  newobj     instance void class Gen<valuetype ValX2<float64,float64>[0...,0...]>::.ctor()
    IL_0123:  ldc.i4.1
    IL_0124:  call       instance void class Gen<valuetype ValX2<float64,float64>[0...,0...]>::ExceptionTest(bool)
    IL_0129:  newobj     instance void class Gen<valuetype ValX2<string,string>[][][]>::.ctor()
    IL_012e:  ldc.i4.1
    IL_012f:  call       instance void class Gen<valuetype ValX2<string,string>[][][]>::ExceptionTest(bool)
    IL_0134:  newobj     instance void class Gen<valuetype ValX2<object,object>[0...,0...,0...,0...]>::.ctor()
    IL_0139:  ldc.i4.1
    IL_013a:  call       instance void class Gen<valuetype ValX2<object,object>[0...,0...,0...,0...]>::ExceptionTest(bool)
    IL_013f:  newobj     instance void class Gen<valuetype ValX2<valuetype [mscorlib]System.Guid,valuetype [mscorlib]System.Guid>[][0...,0...,0...,0...][]>::.ctor()
    IL_0144:  ldc.i4.1
    IL_0145:  call       instance void class Gen<valuetype ValX2<valuetype [mscorlib]System.Guid,valuetype [mscorlib]System.Guid>[][0...,0...,0...,0...][]>::ExceptionTest(bool)
    IL_014a:  newobj     instance void class Gen<class RefX1<int32>>::.ctor()
    IL_014f:  ldc.i4.1
    IL_0150:  call       instance void class Gen<class RefX1<int32>>::ExceptionTest(bool)
    IL_0155:  newobj     instance void class Gen<class RefX1<valuetype ValX1<int32>>>::.ctor()
    IL_015a:  ldc.i4.1
    IL_015b:  call       instance void class Gen<class RefX1<valuetype ValX1<int32>>>::ExceptionTest(bool)
    IL_0160:  newobj     instance void class Gen<class RefX2<int32,string>>::.ctor()
    IL_0165:  ldc.i4.1
    IL_0166:  call       instance void class Gen<class RefX2<int32,string>>::ExceptionTest(bool)
    IL_016b:  newobj     instance void class Gen<class RefX3<int32,string,valuetype [mscorlib]System.Guid>>::.ctor()
    IL_0170:  ldc.i4.1
    IL_0171:  call       instance void class Gen<class RefX3<int32,string,valuetype [mscorlib]System.Guid>>::ExceptionTest(bool)
    IL_0176:  newobj     instance void class Gen<class RefX1<class RefX1<int32>>>::.ctor()
    IL_017b:  ldc.i4.1
    IL_017c:  call       instance void class Gen<class RefX1<class RefX1<int32>>>::ExceptionTest(bool)
    IL_0181:  newobj     instance void class Gen<class RefX1<class RefX1<class RefX1<string>>>>::.ctor()
    IL_0186:  ldc.i4.1
    IL_0187:  call       instance void class Gen<class RefX1<class RefX1<class RefX1<string>>>>::ExceptionTest(bool)
    IL_018c:  newobj     instance void class Gen<class RefX1<class RefX1<class RefX1<class RefX1<valuetype [mscorlib]System.Guid>>>>>::.ctor()
    IL_0191:  ldc.i4.1
    IL_0192:  call       instance void class Gen<class RefX1<class RefX1<class RefX1<class RefX1<valuetype [mscorlib]System.Guid>>>>>::ExceptionTest(bool)
    IL_0197:  newobj     instance void class Gen<class RefX1<class RefX2<int32,string>>>::.ctor()
    IL_019c:  ldc.i4.1
    IL_019d:  call       instance void class Gen<class RefX1<class RefX2<int32,string>>>::ExceptionTest(bool)
    IL_01a2:  newobj     instance void class Gen<class RefX2<class RefX2<class RefX1<int32>,class RefX3<int32,string,class RefX1<class RefX2<int32,string>>>>,class RefX2<class RefX1<int32>,class RefX3<int32,string,class RefX1<class RefX2<int32,string>>>>>>::.ctor()
    IL_01a7:  ldc.i4.1
    IL_01a8:  call       instance void class Gen<class RefX2<class RefX2<class RefX1<int32>,class RefX3<int32,string,class RefX1<class RefX2<int32,string>>>>,class RefX2<class RefX1<int32>,class RefX3<int32,string,class RefX1<class RefX2<int32,string>>>>>>::ExceptionTest(bool)
    IL_01ad:  newobj     instance void class Gen<class RefX3<class RefX1<int32[0...,0...,0...,0...][]>,class RefX2<object[][][0...,0...,0...,0...],valuetype [mscorlib]System.Guid[][][]>,class RefX3<float64[0...,0...,0...,0...,0...,0...,0...,0...,0...,0...,0...],valuetype [mscorlib]System.Guid[][][][0...,0...,0...,0...,0...][0...,0...,0...,0...,0...][][][],string[][][][][][][][][][][]>>>::.ctor()
    IL_01b2:  ldc.i4.1
    IL_01b3:  call       instance void class Gen<class RefX3<class RefX1<int32[0...,0...,0...,0...][]>,class RefX2<object[][][0...,0...,0...,0...],valuetype [mscorlib]System.Guid[][][]>,class RefX3<float64[0...,0...,0...,0...,0...,0...,0...,0...,0...,0...,0...],valuetype [mscorlib]System.Guid[][][][0...,0...,0...,0...,0...][0...,0...,0...,0...,0...][][][],string[][][][][][][][][][][]>>>::ExceptionTest(bool)
    IL_01b8:  newobj     instance void class Gen<valuetype ValX1<int32>>::.ctor()
    IL_01bd:  ldc.i4.1
    IL_01be:  call       instance void class Gen<valuetype ValX1<int32>>::ExceptionTest(bool)
    IL_01c3:  newobj     instance void class Gen<valuetype ValX1<class RefX1<int32>>>::.ctor()
    IL_01c8:  ldc.i4.1
    IL_01c9:  call       instance void class Gen<valuetype ValX1<class RefX1<int32>>>::ExceptionTest(bool)
    IL_01ce:  newobj     instance void class Gen<valuetype ValX2<int32,string>>::.ctor()
    IL_01d3:  ldc.i4.1
    IL_01d4:  call       instance void class Gen<valuetype ValX2<int32,string>>::ExceptionTest(bool)
    IL_01d9:  newobj     instance void class Gen<valuetype ValX3<int32,string,valuetype [mscorlib]System.Guid>>::.ctor()
    IL_01de:  ldc.i4.1
    IL_01df:  call       instance void class Gen<valuetype ValX3<int32,string,valuetype [mscorlib]System.Guid>>::ExceptionTest(bool)
    IL_01e4:  newobj     instance void class Gen<valuetype ValX1<valuetype ValX1<int32>>>::.ctor()
    IL_01e9:  ldc.i4.1
    IL_01ea:  call       instance void class Gen<valuetype ValX1<valuetype ValX1<int32>>>::ExceptionTest(bool)
    IL_01ef:  newobj     instance void class Gen<valuetype ValX1<valuetype ValX1<valuetype ValX1<string>>>>::.ctor()
    IL_01f4:  ldc.i4.1
    IL_01f5:  call       instance void class Gen<valuetype ValX1<valuetype ValX1<valuetype ValX1<string>>>>::ExceptionTest(bool)
    IL_01fa:  newobj     instance void class Gen<valuetype ValX1<valuetype ValX1<valuetype ValX1<valuetype ValX1<valuetype [mscorlib]System.Guid>>>>>::.ctor()
    IL_01ff:  ldc.i4.1
    IL_0200:  call       instance void class Gen<valuetype ValX1<valuetype ValX1<valuetype ValX1<valuetype ValX1<valuetype [mscorlib]System.Guid>>>>>::ExceptionTest(bool)
    IL_0205:  newobj     instance void class Gen<valuetype ValX1<valuetype ValX2<int32,string>>>::.ctor()
    IL_020a:  ldc.i4.1
    IL_020b:  call       instance void class Gen<valuetype ValX1<valuetype ValX2<int32,string>>>::ExceptionTest(bool)
    IL_0210:  newobj     instance void class Gen<valuetype ValX2<valuetype ValX2<valuetype ValX1<int32>,valuetype ValX3<int32,string,valuetype ValX1<valuetype ValX2<int32,string>>>>,valuetype ValX2<valuetype ValX1<int32>,valuetype ValX3<int32,string,valuetype ValX1<valuetype ValX2<int32,string>>>>>>::.ctor()
    IL_0215:  ldc.i4.1
    IL_0216:  call       instance void class Gen<valuetype ValX2<valuetype ValX2<valuetype ValX1<int32>,valuetype ValX3<int32,string,valuetype ValX1<valuetype ValX2<int32,string>>>>,valuetype ValX2<valuetype ValX1<int32>,valuetype ValX3<int32,string,valuetype ValX1<valuetype ValX2<int32,string>>>>>>::ExceptionTest(bool)
    IL_021b:  newobj     instance void class Gen<valuetype ValX3<valuetype ValX1<int32[0...,0...,0...,0...][]>,valuetype ValX2<object[][][0...,0...,0...,0...],valuetype [mscorlib]System.Guid[][][]>,valuetype ValX3<float64[0...,0...,0...,0...,0...,0...,0...,0...,0...,0...,0...],valuetype [mscorlib]System.Guid[][][][0...,0...,0...,0...,0...][0...,0...,0...,0...,0...][][][],string[][][][][][][][][][][]>>>::.ctor()
    IL_0220:  ldc.i4.1
    IL_0221:  call       instance void class Gen<valuetype ValX3<valuetype ValX1<int32[0...,0...,0...,0...][]>,valuetype ValX2<object[][][0...,0...,0...,0...],valuetype [mscorlib]System.Guid[][][]>,valuetype ValX3<float64[0...,0...,0...,0...,0...,0...,0...,0...,0...,0...,0...],valuetype [mscorlib]System.Guid[][][][0...,0...,0...,0...,0...][0...,0...,0...,0...,0...][][][],string[][][][][][][][][][][]>>>::ExceptionTest(bool)
    IL_0226:  ldsfld     bool Test_try_fault03::result
    IL_022b:  brfalse.s  IL_023c

    IL_022d:  ldstr      "Test Passed"
    IL_0232:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0237:  ldc.i4.s   100
    IL_0239:  stloc.0
    IL_023a:  br.s       IL_024a

    IL_023c:  ldstr      "Test Failed"
    IL_0241:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0246:  ldc.i4.1
    IL_0247:  stloc.0
    IL_0248:  br.s       IL_024a

    IL_024a:  ldloc.0
    IL_024b:  ret
  } // end of method Test::Main

  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  stsfld     int32 Test_try_fault03::counter
    IL_0006:  ldc.i4.1
    IL_0007:  stsfld     bool Test_try_fault03::result
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
