// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



 

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 2:0:0:0
}
.assembly ddb609988
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.assembly extern xunit.core {}
.module ddb609988.exe
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY



.class public sequential ansi sealed beforefieldinit OneString
       extends [mscorlib]System.ValueType
{
  .field public string isString
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(string i) cil managed noinlining
  {
    .maxstack  8
    IL_0007:  ldarg.0
    IL_0008:  ldfld      string OneString::isString
    IL_000d:  ldnull
    IL_000e:  ceq
    IL_0010:  stsfld     bool Program::IsInit
    IL_0015:  ldstr      "OneString: isString: {0}"
    IL_001a:  ldarg.0
    IL_001b:  ldfld      string OneString::isString
    IL_0020:  brfalse.s  IL_002a

    IL_0022:  ldarg.0
    IL_0023:  ldfld      string OneString::isString
    IL_0028:  br.s       IL_002f

    IL_002a:  ldstr      "<null>"
    IL_002f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_0034:  ret
  } // end of method OneString::.ctor

} // end of class OneString

.class public sequential ansi sealed beforefieldinit OneInt
       extends [mscorlib]System.ValueType
{
  .field public int32 isInt
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(int32 i) cil managed noinlining
  {
    .maxstack  8
    IL_0007:  ldarg.0
    IL_0008:  ldfld      int32 OneInt::isInt
    IL_000d:  ldc.i4.0
    IL_000e:  ceq
    IL_0010:  stsfld     bool Program::IsInit
    IL_0015:  ldstr      "OneInt: isInt: {0}"
    IL_001a:  ldarg.0
    IL_001b:  ldfld      int32 OneInt::isInt
    IL_0020:  box        [mscorlib]System.Int32
    IL_0025:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_002a:  ret
  } // end of method OneInt::.ctor

} // end of class OneInt

.class public sequential ansi sealed beforefieldinit OneBool
       extends [mscorlib]System.ValueType
{
  .field public bool isBool
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(bool i) cil managed noinlining
  {
    .maxstack  8
    IL_0007:  ldarg.0
    IL_0008:  ldfld      bool OneBool::isBool
    IL_000d:  ldc.i4.0
    IL_000e:  ceq
    IL_0010:  stsfld     bool Program::IsInit
    IL_0015:  ldstr      "OneBool: isBool: {0}"
    IL_001a:  ldarg.0
    IL_001b:  ldfld      bool OneBool::isBool
    IL_0020:  box        [mscorlib]System.Boolean
    IL_0025:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_002a:  ret
  } // end of method OneBool::.ctor

} // end of class OneBool

.class public sequential ansi sealed beforefieldinit OneRtTH
       extends [mscorlib]System.ValueType
{
  .field public valuetype [mscorlib]System.RuntimeTypeHandle isRtTH
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(valuetype [mscorlib]System.RuntimeTypeHandle i) cil managed noinlining
  {
    .maxstack  3
    IL_0007:  ldarg.0
    IL_0008:  ldflda     valuetype [mscorlib]System.RuntimeTypeHandle OneRtTH::isRtTH
    IL_000d:  call       instance native int [mscorlib]System.RuntimeTypeHandle::get_Value()
    IL_0012:  ldsfld     native int [mscorlib]System.IntPtr::Zero
    IL_0017:  call       bool [mscorlib]System.IntPtr::op_Equality(native int,
                                                                   native int)
    IL_001c:  stsfld     bool Program::IsInit
    IL_0021:  ldstr      "OneRtTH: isRuntimeTypeHandle: {0}.Value: {1}"
    IL_0026:  ldarg.0
    IL_0027:  ldfld      valuetype [mscorlib]System.RuntimeTypeHandle OneRtTH::isRtTH
    IL_002c:  ldnull
    IL_002d:  call       bool [mscorlib]System.RuntimeTypeHandle::op_Inequality(valuetype [mscorlib]System.RuntimeTypeHandle,
                                                                                object)
    IL_0032:  brtrue.s   IL_003b

    IL_0034:  ldstr      "<null>"
    IL_0039:  br.s       IL_0040

    IL_003b:  ldstr      "<not null>"
    IL_0040:  ldarg.0
    IL_0041:  ldflda     valuetype [mscorlib]System.RuntimeTypeHandle OneRtTH::isRtTH
    IL_0046:  call       instance native int [mscorlib]System.RuntimeTypeHandle::get_Value()
    IL_004b:  box        [mscorlib]System.IntPtr
    IL_0050:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0055:  ret
  } // end of method OneRtTH::.ctor

} // end of class OneRtTH

.class public sequential ansi sealed beforefieldinit OneStructOneString
       extends [mscorlib]System.ValueType
{
  .field public valuetype OneString isOneString
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(valuetype OneString i) cil managed noinlining
  {
    .maxstack  8
    IL_0007:  ldarg.0
    IL_0008:  ldflda     valuetype OneString OneStructOneString::isOneString
    IL_000d:  ldfld      string OneString::isString
    IL_0012:  ldnull
    IL_0013:  ceq
    IL_0015:  stsfld     bool Program::IsInit
    IL_001a:  ldstr      "OneStructOneString: isOneString.isString: {0}"
    IL_001f:  ldarg.0
    IL_0020:  ldflda     valuetype OneString OneStructOneString::isOneString
    IL_0025:  ldfld      string OneString::isString
    IL_002a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_002f:  ret
  } // end of method OneStructOneString::.ctor

} // end of class OneStructOneString

.class public sequential ansi sealed beforefieldinit OneStructOneInt
       extends [mscorlib]System.ValueType
{
  .field public valuetype OneInt isOneInt
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(valuetype OneInt i) cil managed noinlining
  {
    .maxstack  8
    IL_0007:  ldarg.0
    IL_0008:  ldflda     valuetype OneInt OneStructOneInt::isOneInt
    IL_000d:  ldfld      int32 OneInt::isInt
    IL_0012:  ldc.i4.0
    IL_0013:  ceq
    IL_0015:  stsfld     bool Program::IsInit
    IL_001a:  ldstr      "OneStructOneInt: isOneInt.isInt: {0}"
    IL_001f:  ldarg.0
    IL_0020:  ldflda     valuetype OneInt OneStructOneInt::isOneInt
    IL_0025:  ldfld      int32 OneInt::isInt
    IL_002a:  box        [mscorlib]System.Int32
    IL_002f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_0034:  ret
  } // end of method OneStructOneInt::.ctor

} // end of class OneStructOneInt

.class public sequential ansi sealed beforefieldinit OneStructOneRtTH
       extends [mscorlib]System.ValueType
{
  .field public valuetype OneRtTH isOneRtTH
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(valuetype OneRtTH i) cil managed noinlining
  {
    .maxstack  2
    IL_0007:  ldarg.0
    IL_0008:  ldflda     valuetype OneRtTH OneStructOneRtTH::isOneRtTH
    IL_000d:  ldflda     valuetype [mscorlib]System.RuntimeTypeHandle OneRtTH::isRtTH
    IL_0012:  call       instance native int [mscorlib]System.RuntimeTypeHandle::get_Value()
    IL_0017:  ldsfld     native int [mscorlib]System.IntPtr::Zero
    IL_001c:  call       bool [mscorlib]System.IntPtr::op_Equality(native int,
                                                                   native int)
    IL_0021:  stsfld     bool Program::IsInit
    IL_0026:  ldstr      "OneStructOneRtTH: isOneRtTH.isRtTH.Value: {0}"
    IL_002b:  ldarg.0
    IL_002c:  ldflda     valuetype OneRtTH OneStructOneRtTH::isOneRtTH
    IL_0031:  ldflda     valuetype [mscorlib]System.RuntimeTypeHandle OneRtTH::isRtTH
    IL_0036:  call       instance native int [mscorlib]System.RuntimeTypeHandle::get_Value()
    IL_003b:  box        [mscorlib]System.IntPtr
    IL_0040:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_0045:  ret
  } // end of method OneStructOneRtTH::.ctor

} // end of class OneStructOneRtTH

.class public sequential ansi sealed beforefieldinit TwoStringInt
       extends [mscorlib]System.ValueType
{
  .field public string isString
  .field public int32 isInt
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(string i) cil managed noinlining
  {
    .maxstack  3
    IL_000e:  ldarg.0
    IL_000f:  ldfld      string TwoStringInt::isString
    IL_0014:  brtrue.s   IL_0021

    IL_0016:  ldarg.0
    IL_0017:  ldfld      int32 TwoStringInt::isInt
    IL_001c:  ldc.i4.0
    IL_001d:  ceq
    IL_001f:  br.s       IL_0022

    IL_0021:  ldc.i4.0
    IL_0022:  stsfld     bool Program::IsInit
    IL_0027:  ldstr      "TwoStringInt: isString: {0} isInt: {1}"
    IL_002c:  ldarg.0
    IL_002d:  ldfld      string TwoStringInt::isString
    IL_0032:  brfalse.s  IL_003c

    IL_0034:  ldarg.0
    IL_0035:  ldfld      string TwoStringInt::isString
    IL_003a:  br.s       IL_0041

    IL_003c:  ldstr      "<null>"
    IL_0041:  ldarg.0
    IL_0042:  ldfld      int32 TwoStringInt::isInt
    IL_0047:  box        [mscorlib]System.Int32
    IL_004c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0051:  ret
  } // end of method TwoStringInt::.ctor

} // end of class TwoStringInt

.class public sequential ansi sealed beforefieldinit TwoIntBool
       extends [mscorlib]System.ValueType
{
  .field public int32 isInt
  .field public bool isBool
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(int32 i) cil managed noinlining
  {
    .maxstack  3
    IL_000e:  ldarg.0
    IL_000f:  ldfld      int32 TwoIntBool::isInt
    IL_0014:  brtrue.s   IL_0021

    IL_0016:  ldarg.0
    IL_0017:  ldfld      bool TwoIntBool::isBool
    IL_001c:  ldc.i4.0
    IL_001d:  ceq
    IL_001f:  br.s       IL_0022

    IL_0021:  ldc.i4.0
    IL_0022:  stsfld     bool Program::IsInit
    IL_0027:  ldstr      "TwoIntBool: isInt: {0} isBool: {1}"
    IL_002c:  ldarg.0
    IL_002d:  ldfld      int32 TwoIntBool::isInt
    IL_0032:  box        [mscorlib]System.Int32
    IL_0037:  ldarg.0
    IL_0038:  ldfld      bool TwoIntBool::isBool
    IL_003d:  box        [mscorlib]System.Boolean
    IL_0042:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0047:  ret
  } // end of method TwoIntBool::.ctor

} // end of class TwoIntBool

.class public sequential ansi sealed beforefieldinit TwoRTTHDouble
       extends [mscorlib]System.ValueType
{
  .field public float64 isDouble
  .field public valuetype [mscorlib]System.RuntimeTypeHandle isRtTH
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(bool i) cil managed noinlining
  {
    .maxstack  3
    IL_001b:  ldarg.0
    IL_001c:  ldfld      float64 TwoRTTHDouble::isDouble
    IL_0021:  ldc.r8     0.0
    IL_002a:  bne.un.s   IL_0043

    IL_002c:  ldarg.0
    IL_002d:  ldflda     valuetype [mscorlib]System.RuntimeTypeHandle TwoRTTHDouble::isRtTH
    IL_0032:  call       instance native int [mscorlib]System.RuntimeTypeHandle::get_Value()
    IL_0037:  ldsfld     native int [mscorlib]System.IntPtr::Zero
    IL_003c:  call       bool [mscorlib]System.IntPtr::op_Equality(native int,
                                                                   native int)
    IL_0041:  br.s       IL_0044

    IL_0043:  ldc.i4.0
    IL_0044:  stsfld     bool Program::IsInit
    IL_0049:  ldstr      "TwoRTTHDouble: isDouble: {0}, isRtTH.Value: {1}"
    IL_004e:  ldarg.0
    IL_004f:  ldfld      float64 TwoRTTHDouble::isDouble
    IL_0054:  box        [mscorlib]System.Double
    IL_0059:  ldarg.0
    IL_005a:  ldflda     valuetype [mscorlib]System.RuntimeTypeHandle TwoRTTHDouble::isRtTH
    IL_005f:  call       instance native int [mscorlib]System.RuntimeTypeHandle::get_Value()
    IL_0064:  box        [mscorlib]System.IntPtr
    IL_0069:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_006e:  ret
  } // end of method TwoRTTHDouble::.ctor

} // end of class TwoRTTHDouble

.class public sequential ansi sealed beforefieldinit AllTypes
       extends [mscorlib]System.ValueType
{
  .field public bool isBool
  .field public int32 isInt
  .field public int16 isShort
  .field public float64 isDouble
  .field public valuetype [mscorlib]System.RuntimeTypeHandle isRtTH
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(bool i) cil managed noinlining
  {
    .maxstack  4
    .locals init (object[] V_0)
    IL_0024:  ldarg.0
    IL_0025:  ldflda     valuetype [mscorlib]System.RuntimeTypeHandle AllTypes::isRtTH
    IL_002a:  initobj    [mscorlib]System.RuntimeTypeHandle
    IL_0030:  ldarg.0
    IL_0031:  ldfld      bool AllTypes::isBool
    IL_0036:  brtrue.s   IL_0070

    IL_0038:  ldarg.0
    IL_0039:  ldfld      int32 AllTypes::isInt
    IL_003e:  brtrue.s   IL_0070

    IL_0040:  ldarg.0
    IL_0041:  ldfld      int16 AllTypes::isShort
    IL_0046:  brtrue.s   IL_0070

    IL_0048:  ldarg.0
    IL_0049:  ldfld      float64 AllTypes::isDouble
    IL_004e:  ldc.r8     0.0
    IL_0057:  bne.un.s   IL_0070

    IL_0059:  ldarg.0
    IL_005a:  ldflda     valuetype [mscorlib]System.RuntimeTypeHandle AllTypes::isRtTH
    IL_005f:  call       instance native int [mscorlib]System.RuntimeTypeHandle::get_Value()
    IL_0064:  ldsfld     native int [mscorlib]System.IntPtr::Zero
    IL_0069:  call       bool [mscorlib]System.IntPtr::op_Equality(native int,
                                                                   native int)
    IL_006e:  br.s       IL_0071

    IL_0070:  ldc.i4.0
    IL_0071:  stsfld     bool Program::IsInit
    IL_0076:  ldstr      "AllTypes: isBool: {0} isInt: {1}, isShort: {2}, is"
    + "Double: {3}, isRtTH.Value: {4}"
    IL_007b:  ldc.i4.5
    IL_007c:  newarr     [mscorlib]System.Object
    IL_0081:  stloc.0
    IL_0082:  ldloc.0
    IL_0083:  ldc.i4.0
    IL_0084:  ldarg.0
    IL_0085:  ldfld      bool AllTypes::isBool
    IL_008a:  box        [mscorlib]System.Boolean
    IL_008f:  stelem.ref
    IL_0090:  ldloc.0
    IL_0091:  ldc.i4.1
    IL_0092:  ldarg.0
    IL_0093:  ldfld      int32 AllTypes::isInt
    IL_0098:  box        [mscorlib]System.Int32
    IL_009d:  stelem.ref
    IL_009e:  ldloc.0
    IL_009f:  ldc.i4.2
    IL_00a0:  ldarg.0
    IL_00a1:  ldfld      int16 AllTypes::isShort
    IL_00a6:  box        [mscorlib]System.Int16
    IL_00ab:  stelem.ref
    IL_00ac:  ldloc.0
    IL_00ad:  ldc.i4.3
    IL_00ae:  ldarg.0
    IL_00af:  ldfld      float64 AllTypes::isDouble
    IL_00b4:  box        [mscorlib]System.Double
    IL_00b9:  stelem.ref
    IL_00ba:  ldloc.0
    IL_00bb:  ldc.i4.4
    IL_00bc:  ldarg.0
    IL_00bd:  ldflda     valuetype [mscorlib]System.RuntimeTypeHandle AllTypes::isRtTH
    IL_00c2:  call       instance native int [mscorlib]System.RuntimeTypeHandle::get_Value()
    IL_00c7:  box        [mscorlib]System.IntPtr
    IL_00cc:  stelem.ref
    IL_00cd:  ldloc.0
    IL_00ce:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object[])
    IL_00d3:  ret
  } // end of method AllTypes::.ctor

} // end of class AllTypes

.class public sequential ansi sealed beforefieldinit AllTypesNullable
       extends [mscorlib]System.ValueType
{
  .field public valuetype [mscorlib]System.Nullable`1<bool> isBool
  .field public valuetype [mscorlib]System.Nullable`1<int32> isInt
  .field public valuetype [mscorlib]System.Nullable`1<int16> isShort
  .field public valuetype [mscorlib]System.Nullable`1<float64> isDouble
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(bool i) cil managed noinlining
  {
    .maxstack  4
    .locals init (valuetype [mscorlib]System.Nullable`1<bool> V_0,
             valuetype [mscorlib]System.Nullable`1<int32> V_1,
             valuetype [mscorlib]System.Nullable`1<int16> V_2,
             valuetype [mscorlib]System.Nullable`1<float64> V_3,
             object[] V_4)
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  newobj     instance void valuetype [mscorlib]System.Nullable`1<bool>::.ctor(!0)
    IL_0007:  stfld      valuetype [mscorlib]System.Nullable`1<bool> AllTypesNullable::isBool
    IL_000c:  ldarg.0
    IL_000d:  ldc.i4.0
    IL_000e:  newobj     instance void valuetype [mscorlib]System.Nullable`1<int32>::.ctor(!0)
    IL_0013:  stfld      valuetype [mscorlib]System.Nullable`1<int32> AllTypesNullable::isInt
    IL_0018:  ldarg.0
    IL_0019:  ldc.i4.0
    IL_001a:  newobj     instance void valuetype [mscorlib]System.Nullable`1<int16>::.ctor(!0)
    IL_001f:  stfld      valuetype [mscorlib]System.Nullable`1<int16> AllTypesNullable::isShort
    IL_0024:  ldarg.0
    IL_0025:  ldc.r8     0.0
    IL_002e:  newobj     instance void valuetype [mscorlib]System.Nullable`1<float64>::.ctor(!0)
    IL_0033:  stfld      valuetype [mscorlib]System.Nullable`1<float64> AllTypesNullable::isDouble
    IL_0038:  ldarg.0
    IL_0039:  ldfld      valuetype [mscorlib]System.Nullable`1<bool> AllTypesNullable::isBool
    IL_003e:  stloc.0
    IL_003f:  ldloca.s   V_0
    IL_0041:  call       instance !0 valuetype [mscorlib]System.Nullable`1<bool>::GetValueOrDefault()
    IL_0046:  brtrue.s   IL_0051

    IL_0048:  ldloca.s   V_0
    IL_004a:  call       instance bool valuetype [mscorlib]System.Nullable`1<bool>::get_HasValue()
    IL_004f:  br.s       IL_0052

    IL_0051:  ldc.i4.0
    IL_0052:  brfalse.s  IL_00b1

    IL_0054:  ldarg.0
    IL_0055:  ldfld      valuetype [mscorlib]System.Nullable`1<int32> AllTypesNullable::isInt
    IL_005a:  stloc.1
    IL_005b:  ldloca.s   V_1
    IL_005d:  call       instance !0 valuetype [mscorlib]System.Nullable`1<int32>::GetValueOrDefault()
    IL_0062:  brtrue.s   IL_006d

    IL_0064:  ldloca.s   V_1
    IL_0066:  call       instance bool valuetype [mscorlib]System.Nullable`1<int32>::get_HasValue()
    IL_006b:  br.s       IL_006e

    IL_006d:  ldc.i4.0
    IL_006e:  brfalse.s  IL_00b1

    IL_0070:  ldarg.0
    IL_0071:  ldfld      valuetype [mscorlib]System.Nullable`1<int16> AllTypesNullable::isShort
    IL_0076:  stloc.2
    IL_0077:  ldloca.s   V_2
    IL_0079:  call       instance !0 valuetype [mscorlib]System.Nullable`1<int16>::GetValueOrDefault()
    IL_007e:  brtrue.s   IL_0089

    IL_0080:  ldloca.s   V_2
    IL_0082:  call       instance bool valuetype [mscorlib]System.Nullable`1<int16>::get_HasValue()
    IL_0087:  br.s       IL_008a

    IL_0089:  ldc.i4.0
    IL_008a:  brfalse.s  IL_00b1

    IL_008c:  ldarg.0
    IL_008d:  ldfld      valuetype [mscorlib]System.Nullable`1<float64> AllTypesNullable::isDouble
    IL_0092:  stloc.3
    IL_0093:  ldloca.s   V_3
    IL_0095:  call       instance !0 valuetype [mscorlib]System.Nullable`1<float64>::GetValueOrDefault()
    IL_009a:  ldc.r8     0.0
    IL_00a3:  bne.un.s   IL_00ae

    IL_00a5:  ldloca.s   V_3
    IL_00a7:  call       instance bool valuetype [mscorlib]System.Nullable`1<float64>::get_HasValue()
    IL_00ac:  br.s       IL_00b2

    IL_00ae:  ldc.i4.0
    IL_00af:  br.s       IL_00b2

    IL_00b1:  ldc.i4.0
    IL_00b2:  stsfld     bool Program::IsInit
    IL_00b7:  ldstr      "AllTypesNullable: isBool: {0} isInt: {1}, isShort:"
    + " {2}, isDouble: {3}"
    IL_00bc:  ldc.i4.4
    IL_00bd:  newarr     [mscorlib]System.Object
    IL_00c2:  stloc.s    V_4
    IL_00c4:  ldloc.s    V_4
    IL_00c6:  ldc.i4.0
    IL_00c7:  ldarg.0
    IL_00c8:  ldfld      valuetype [mscorlib]System.Nullable`1<bool> AllTypesNullable::isBool
    IL_00cd:  box        valuetype [mscorlib]System.Nullable`1<bool>
    IL_00d2:  stelem.ref
    IL_00d3:  ldloc.s    V_4
    IL_00d5:  ldc.i4.1
    IL_00d6:  ldarg.0
    IL_00d7:  ldfld      valuetype [mscorlib]System.Nullable`1<int32> AllTypesNullable::isInt
    IL_00dc:  box        valuetype [mscorlib]System.Nullable`1<int32>
    IL_00e1:  stelem.ref
    IL_00e2:  ldloc.s    V_4
    IL_00e4:  ldc.i4.2
    IL_00e5:  ldarg.0
    IL_00e6:  ldfld      valuetype [mscorlib]System.Nullable`1<int16> AllTypesNullable::isShort
    IL_00eb:  box        valuetype [mscorlib]System.Nullable`1<int16>
    IL_00f0:  stelem.ref
    IL_00f1:  ldloc.s    V_4
    IL_00f3:  ldc.i4.3
    IL_00f4:  ldarg.0
    IL_00f5:  ldfld      valuetype [mscorlib]System.Nullable`1<float64> AllTypesNullable::isDouble
    IL_00fa:  box        valuetype [mscorlib]System.Nullable`1<float64>
    IL_00ff:  stelem.ref
    IL_0100:  ldloc.s    V_4
    IL_0102:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object[])
    IL_0107:  ret
  } // end of method AllTypesNullable::.ctor

} // end of class AllTypesNullable

.class public sequential ansi sealed beforefieldinit AllTypesNoExplicitConstructor
       extends [mscorlib]System.ValueType
{
  .field public bool isBool
  .field public int32 isInt
  .field public int16 isShort
  .field public float64 isDouble
  .field public valuetype [mscorlib]System.RuntimeTypeHandle isRtTH
} // end of class AllTypesNoExplicitConstructor

.class public auto ansi beforefieldinit Program
       extends [mscorlib]System.Object
{
  .field public static bool IsInit
  .field public static bool AllPassed
  .method public hidebysig static void  F(object o) cil managed noinlining
  {
    .maxstack  8
    IL_0000:  ret
  } // end of method Program::F

  .method private hidebysig static void  Test1() cil managed noinlining
  {
    .maxstack  8
    IL_0000:  ldstr      "4"
    IL_0005:  newobj     instance void OneString::.ctor(string)
    IL_000a:  box        OneString
    IL_000f:  call       void Program::F(object)
    IL_0014:  ldsfld     bool Program::IsInit
    IL_0019:  brtrue.s   IL_0021

    IL_001b:  ldc.i4.0
    IL_001c:  stsfld     bool Program::AllPassed
    IL_0021:  ret
  } // end of method Program::Test1

  .method private hidebysig static void  Test2() cil managed noinlining
  {
    .maxstack  8
    IL_0000:  ldc.i4.4
    IL_0001:  newobj     instance void OneInt::.ctor(int32)
    IL_0006:  box        OneInt
    IL_000b:  call       void Program::F(object)
    IL_0010:  ldsfld     bool Program::IsInit
    IL_0015:  brtrue.s   IL_001d

    IL_0017:  ldc.i4.0
    IL_0018:  stsfld     bool Program::AllPassed
    IL_001d:  ret
  } // end of method Program::Test2

  .method private hidebysig static void  Test3() cil managed noinlining
  {
    .maxstack  8
    IL_0000:  ldc.i4.1
    IL_0001:  newobj     instance void OneBool::.ctor(bool)
    IL_0006:  box        OneBool
    IL_000b:  call       void Program::F(object)
    IL_0010:  ldsfld     bool Program::IsInit
    IL_0015:  brtrue.s   IL_001d

    IL_0017:  ldc.i4.0
    IL_0018:  stsfld     bool Program::AllPassed
    IL_001d:  ret
  } // end of method Program::Test3

  .method private hidebysig static void  Test4() cil managed noinlining
  {
    .maxstack  2
    .locals init (valuetype [mscorlib]System.RuntimeTypeHandle V_0)
    IL_0000:  ldloca.s   V_0
    IL_0002:  initobj    [mscorlib]System.RuntimeTypeHandle
    IL_0008:  ldloc.0
    IL_0009:  newobj     instance void OneRtTH::.ctor(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_000e:  box        OneRtTH
    IL_0013:  call       void Program::F(object)
    IL_0018:  ldsfld     bool Program::IsInit
    IL_001d:  brtrue.s   IL_0025

    IL_001f:  ldc.i4.0
    IL_0020:  stsfld     bool Program::AllPassed
    IL_0025:  ret
  } // end of method Program::Test4

  .method private hidebysig static void  Test5() cil managed noinlining
  {
    .maxstack  8
    IL_0000:  ldstr      "4"
    IL_0005:  newobj     instance void OneString::.ctor(string)
    IL_000a:  newobj     instance void OneStructOneString::.ctor(valuetype OneString)
    IL_000f:  box        OneStructOneString
    IL_0014:  call       void Program::F(object)
    IL_0019:  ldsfld     bool Program::IsInit
    IL_001e:  brtrue.s   IL_0026

    IL_0020:  ldc.i4.0
    IL_0021:  stsfld     bool Program::AllPassed
    IL_0026:  ret
  } // end of method Program::Test5

  .method private hidebysig static void  Test6() cil managed noinlining
  {
    .maxstack  8
    IL_0000:  ldc.i4.4
    IL_0001:  newobj     instance void OneInt::.ctor(int32)
    IL_0006:  newobj     instance void OneStructOneInt::.ctor(valuetype OneInt)
    IL_000b:  box        OneStructOneInt
    IL_0010:  call       void Program::F(object)
    IL_0015:  ldsfld     bool Program::IsInit
    IL_001a:  brtrue.s   IL_0022

    IL_001c:  ldc.i4.0
    IL_001d:  stsfld     bool Program::AllPassed
    IL_0022:  ret
  } // end of method Program::Test6

  .method private hidebysig static void  Test7() cil managed noinlining
  {
    .maxstack  2
    .locals init (valuetype [mscorlib]System.RuntimeTypeHandle V_0)
    IL_0000:  ldloca.s   V_0
    IL_0002:  initobj    [mscorlib]System.RuntimeTypeHandle
    IL_0008:  ldloc.0
    IL_0009:  newobj     instance void OneRtTH::.ctor(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_000e:  newobj     instance void OneStructOneRtTH::.ctor(valuetype OneRtTH)
    IL_0013:  box        OneStructOneRtTH
    IL_0018:  call       void Program::F(object)
    IL_001d:  ldsfld     bool Program::IsInit
    IL_0022:  brtrue.s   IL_002a

    IL_0024:  ldc.i4.0
    IL_0025:  stsfld     bool Program::AllPassed
    IL_002a:  ret
  } // end of method Program::Test7

  .method private hidebysig static void  Test8() cil managed noinlining
  {
    .maxstack  8
    IL_0000:  ldstr      "1"
    IL_0005:  newobj     instance void TwoStringInt::.ctor(string)
    IL_000a:  box        TwoStringInt
    IL_000f:  call       void Program::F(object)
    IL_0014:  ldsfld     bool Program::IsInit
    IL_0019:  brtrue.s   IL_0021

    IL_001b:  ldc.i4.0
    IL_001c:  stsfld     bool Program::AllPassed
    IL_0021:  ret
  } // end of method Program::Test8

  .method private hidebysig static void  Test9() cil managed noinlining
  {
    .maxstack  8
    IL_0000:  ldc.i4.1
    IL_0001:  newobj     instance void TwoIntBool::.ctor(int32)
    IL_0006:  box        TwoIntBool
    IL_000b:  call       void Program::F(object)
    IL_0010:  ldsfld     bool Program::IsInit
    IL_0015:  brtrue.s   IL_001d

    IL_0017:  ldc.i4.0
    IL_0018:  stsfld     bool Program::AllPassed
    IL_001d:  ret
  } // end of method Program::Test9

  .method private hidebysig static void  Test10() cil managed noinlining
  {
    .maxstack  8
    IL_0000:  ldc.i4.1
    IL_0001:  newobj     instance void TwoRTTHDouble::.ctor(bool)
    IL_0006:  box        TwoRTTHDouble
    IL_000b:  call       void Program::F(object)
    IL_0010:  ldsfld     bool Program::IsInit
    IL_0015:  brtrue.s   IL_001d

    IL_0017:  ldc.i4.0
    IL_0018:  stsfld     bool Program::AllPassed
    IL_001d:  ret
  } // end of method Program::Test10

  .method private hidebysig static void  Test11() cil managed noinlining
  {
    .maxstack  8
    IL_0000:  ldc.i4.1
    IL_0001:  newobj     instance void AllTypes::.ctor(bool)
    IL_0006:  box        AllTypes
    IL_000b:  call       void Program::F(object)
    IL_0010:  ldsfld     bool Program::IsInit
    IL_0015:  brtrue.s   IL_001d

    IL_0017:  ldc.i4.0
    IL_0018:  stsfld     bool Program::AllPassed
    IL_001d:  ret
  } // end of method Program::Test11

  .method private hidebysig static void  Test12() cil managed noinlining
  {
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  newobj     instance void AllTypesNullable::.ctor(bool)
    IL_0006:  box        AllTypesNullable
    IL_000b:  call       void Program::F(object)
    IL_0010:  ldsfld     bool Program::IsInit
    IL_0015:  brtrue.s   IL_001d

    IL_0017:  ldc.i4.0
    IL_0018:  stsfld     bool Program::AllPassed
    IL_001d:  ret
  } // end of method Program::Test12

  .method private hidebysig static void  Test13() cil managed noinlining
  {
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  ldstr      "Calling OneString in a loop..."
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.0
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_0033

    IL_000e:  ldstr      "4"
    IL_0013:  newobj     instance void OneString::.ctor(string)
    IL_0018:  box        OneString
    IL_001d:  call       void Program::F(object)
    IL_0022:  ldsfld     bool Program::IsInit
    IL_0027:  brtrue.s   IL_002f

    IL_0029:  ldc.i4.0
    IL_002a:  stsfld     bool Program::AllPassed
    IL_002f:  ldloc.0
    IL_0030:  ldc.i4.1
    IL_0031:  add
    IL_0032:  stloc.0
    IL_0033:  ldloc.0
    IL_0034:  ldc.i4.s   10
    IL_0036:  blt.s      IL_000e

    IL_0038:  ret
  } // end of method Program::Test13

  .method private hidebysig static void  Test14() cil managed noinlining
  {
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  ldstr      "Calling OneInt in a loop..."
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.0
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_002f

    IL_000e:  ldc.i4.4
    IL_000f:  newobj     instance void OneInt::.ctor(int32)
    IL_0014:  box        OneInt
    IL_0019:  call       void Program::F(object)
    IL_001e:  ldsfld     bool Program::IsInit
    IL_0023:  brtrue.s   IL_002b

    IL_0025:  ldc.i4.0
    IL_0026:  stsfld     bool Program::AllPassed
    IL_002b:  ldloc.0
    IL_002c:  ldc.i4.1
    IL_002d:  add
    IL_002e:  stloc.0
    IL_002f:  ldloc.0
    IL_0030:  ldc.i4.s   10
    IL_0032:  blt.s      IL_000e

    IL_0034:  ret
  } // end of method Program::Test14

  .method private hidebysig static void  Test15() cil managed noinlining
  {
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  ldstr      "Calling OneBool in a loop..."
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.0
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_002f

    IL_000e:  ldc.i4.1
    IL_000f:  newobj     instance void OneBool::.ctor(bool)
    IL_0014:  box        OneBool
    IL_0019:  call       void Program::F(object)
    IL_001e:  ldsfld     bool Program::IsInit
    IL_0023:  brtrue.s   IL_002b

    IL_0025:  ldc.i4.0
    IL_0026:  stsfld     bool Program::AllPassed
    IL_002b:  ldloc.0
    IL_002c:  ldc.i4.1
    IL_002d:  add
    IL_002e:  stloc.0
    IL_002f:  ldloc.0
    IL_0030:  ldc.i4.s   10
    IL_0032:  blt.s      IL_000e

    IL_0034:  ret
  } // end of method Program::Test15

  .method private hidebysig static void  Test16() cil managed noinlining
  {
    .maxstack  2
    .locals init (int32 V_0,
             valuetype [mscorlib]System.RuntimeTypeHandle V_1)
    IL_0000:  ldstr      "Calling OneRtTH in a loop..."
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.0
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_0037

    IL_000e:  ldloca.s   V_1
    IL_0010:  initobj    [mscorlib]System.RuntimeTypeHandle
    IL_0016:  ldloc.1
    IL_0017:  newobj     instance void OneRtTH::.ctor(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_001c:  box        OneRtTH
    IL_0021:  call       void Program::F(object)
    IL_0026:  ldsfld     bool Program::IsInit
    IL_002b:  brtrue.s   IL_0033

    IL_002d:  ldc.i4.0
    IL_002e:  stsfld     bool Program::AllPassed
    IL_0033:  ldloc.0
    IL_0034:  ldc.i4.1
    IL_0035:  add
    IL_0036:  stloc.0
    IL_0037:  ldloc.0
    IL_0038:  ldc.i4.s   10
    IL_003a:  blt.s      IL_000e

    IL_003c:  ret
  } // end of method Program::Test16

  .method private hidebysig static void  Test17() cil managed noinlining
  {
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  ldstr      "Calling OneStructOneString in a loop..."
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.0
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_0038

    IL_000e:  ldstr      "4"
    IL_0013:  newobj     instance void OneString::.ctor(string)
    IL_0018:  newobj     instance void OneStructOneString::.ctor(valuetype OneString)
    IL_001d:  box        OneStructOneString
    IL_0022:  call       void Program::F(object)
    IL_0027:  ldsfld     bool Program::IsInit
    IL_002c:  brtrue.s   IL_0034

    IL_002e:  ldc.i4.0
    IL_002f:  stsfld     bool Program::AllPassed
    IL_0034:  ldloc.0
    IL_0035:  ldc.i4.1
    IL_0036:  add
    IL_0037:  stloc.0
    IL_0038:  ldloc.0
    IL_0039:  ldc.i4.s   10
    IL_003b:  blt.s      IL_000e

    IL_003d:  ret
  } // end of method Program::Test17

  .method private hidebysig static void  Test18() cil managed noinlining
  {
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  ldstr      "Calling OneStructOneInt in a loop..."
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.0
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_0034

    IL_000e:  ldc.i4.4
    IL_000f:  newobj     instance void OneInt::.ctor(int32)
    IL_0014:  newobj     instance void OneStructOneInt::.ctor(valuetype OneInt)
    IL_0019:  box        OneStructOneInt
    IL_001e:  call       void Program::F(object)
    IL_0023:  ldsfld     bool Program::IsInit
    IL_0028:  brtrue.s   IL_0030

    IL_002a:  ldc.i4.0
    IL_002b:  stsfld     bool Program::AllPassed
    IL_0030:  ldloc.0
    IL_0031:  ldc.i4.1
    IL_0032:  add
    IL_0033:  stloc.0
    IL_0034:  ldloc.0
    IL_0035:  ldc.i4.s   10
    IL_0037:  blt.s      IL_000e

    IL_0039:  ret
  } // end of method Program::Test18

  .method private hidebysig static void  Test19() cil managed noinlining
  {
    .maxstack  2
    .locals init (int32 V_0,
             valuetype [mscorlib]System.RuntimeTypeHandle V_1)
    IL_0000:  ldstr      "Calling OneStructOneRtTH in a loop..."
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.0
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_003c

    IL_000e:  ldloca.s   V_1
    IL_0010:  initobj    [mscorlib]System.RuntimeTypeHandle
    IL_0016:  ldloc.1
    IL_0017:  newobj     instance void OneRtTH::.ctor(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_001c:  newobj     instance void OneStructOneRtTH::.ctor(valuetype OneRtTH)
    IL_0021:  box        OneStructOneRtTH
    IL_0026:  call       void Program::F(object)
    IL_002b:  ldsfld     bool Program::IsInit
    IL_0030:  brtrue.s   IL_0038

    IL_0032:  ldc.i4.0
    IL_0033:  stsfld     bool Program::AllPassed
    IL_0038:  ldloc.0
    IL_0039:  ldc.i4.1
    IL_003a:  add
    IL_003b:  stloc.0
    IL_003c:  ldloc.0
    IL_003d:  ldc.i4.s   10
    IL_003f:  blt.s      IL_000e

    IL_0041:  ret
  } // end of method Program::Test19

  .method private hidebysig static void  Test20() cil managed noinlining
  {
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  ldstr      "Calling TwoStringInt in a loop..."
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.0
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_0033

    IL_000e:  ldstr      "1"
    IL_0013:  newobj     instance void TwoStringInt::.ctor(string)
    IL_0018:  box        TwoStringInt
    IL_001d:  call       void Program::F(object)
    IL_0022:  ldsfld     bool Program::IsInit
    IL_0027:  brtrue.s   IL_002f

    IL_0029:  ldc.i4.0
    IL_002a:  stsfld     bool Program::AllPassed
    IL_002f:  ldloc.0
    IL_0030:  ldc.i4.1
    IL_0031:  add
    IL_0032:  stloc.0
    IL_0033:  ldloc.0
    IL_0034:  ldc.i4.s   10
    IL_0036:  blt.s      IL_000e

    IL_0038:  ret
  } // end of method Program::Test20

  .method private hidebysig static void  Test21() cil managed noinlining
  {
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  ldstr      "Calling TwoIntBool in a loop..."
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.0
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_002f

    IL_000e:  ldc.i4.1
    IL_000f:  newobj     instance void TwoIntBool::.ctor(int32)
    IL_0014:  box        TwoIntBool
    IL_0019:  call       void Program::F(object)
    IL_001e:  ldsfld     bool Program::IsInit
    IL_0023:  brtrue.s   IL_002b

    IL_0025:  ldc.i4.0
    IL_0026:  stsfld     bool Program::AllPassed
    IL_002b:  ldloc.0
    IL_002c:  ldc.i4.1
    IL_002d:  add
    IL_002e:  stloc.0
    IL_002f:  ldloc.0
    IL_0030:  ldc.i4.s   10
    IL_0032:  blt.s      IL_000e

    IL_0034:  ret
  } // end of method Program::Test21

  .method private hidebysig static void  Test22() cil managed noinlining
  {
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  ldstr      "Calling TwoRTTHDouble in a loop..."
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.0
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_002f

    IL_000e:  ldc.i4.1
    IL_000f:  newobj     instance void TwoRTTHDouble::.ctor(bool)
    IL_0014:  box        TwoRTTHDouble
    IL_0019:  call       void Program::F(object)
    IL_001e:  ldsfld     bool Program::IsInit
    IL_0023:  brtrue.s   IL_002b

    IL_0025:  ldc.i4.0
    IL_0026:  stsfld     bool Program::AllPassed
    IL_002b:  ldloc.0
    IL_002c:  ldc.i4.1
    IL_002d:  add
    IL_002e:  stloc.0
    IL_002f:  ldloc.0
    IL_0030:  ldc.i4.s   10
    IL_0032:  blt.s      IL_000e

    IL_0034:  ret
  } // end of method Program::Test22

  .method private hidebysig static void  Test23() cil managed noinlining
  {
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  ldstr      "Calling AllTypes in a loop..."
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.0
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_002f

    IL_000e:  ldc.i4.1
    IL_000f:  newobj     instance void AllTypes::.ctor(bool)
    IL_0014:  box        AllTypes
    IL_0019:  call       void Program::F(object)
    IL_001e:  ldsfld     bool Program::IsInit
    IL_0023:  brtrue.s   IL_002b

    IL_0025:  ldc.i4.0
    IL_0026:  stsfld     bool Program::AllPassed
    IL_002b:  ldloc.0
    IL_002c:  ldc.i4.1
    IL_002d:  add
    IL_002e:  stloc.0
    IL_002f:  ldloc.0
    IL_0030:  ldc.i4.s   10
    IL_0032:  blt.s      IL_000e

    IL_0034:  ret
  } // end of method Program::Test23

  .method private hidebysig static void  Test24() cil managed noinlining
  {
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  ldstr      "Calling AllTypesNullable in a loop..."
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.0
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_002f

    IL_000e:  ldc.i4.0
    IL_000f:  newobj     instance void AllTypesNullable::.ctor(bool)
    IL_0014:  box        AllTypesNullable
    IL_0019:  call       void Program::F(object)
    IL_001e:  ldsfld     bool Program::IsInit
    IL_0023:  brtrue.s   IL_002b

    IL_0025:  ldc.i4.0
    IL_0026:  stsfld     bool Program::AllPassed
    IL_002b:  ldloc.0
    IL_002c:  ldc.i4.1
    IL_002d:  add
    IL_002e:  stloc.0
    IL_002f:  ldloc.0
    IL_0030:  ldc.i4.s   10
    IL_0032:  blt.s      IL_000e

    IL_0034:  ret
  } // end of method Program::Test24

  .method private hidebysig static void  Test25() cil managed noinlining
  {
    .maxstack  4
    .locals init (valuetype AllTypesNoExplicitConstructor V_0,
             object[] V_1)
    IL_0000:  ldloca.s   V_0
    IL_0002:  initobj    AllTypesNoExplicitConstructor
    IL_0008:  ldloca.s   V_0
    IL_000a:  ldfld      bool AllTypesNoExplicitConstructor::isBool
    IL_000f:  brtrue.s   IL_004d

    IL_0011:  ldloca.s   V_0
    IL_0013:  ldfld      int32 AllTypesNoExplicitConstructor::isInt
    IL_0018:  brtrue.s   IL_004d

    IL_001a:  ldloca.s   V_0
    IL_001c:  ldfld      int16 AllTypesNoExplicitConstructor::isShort
    IL_0021:  brtrue.s   IL_004d

    IL_0023:  ldloca.s   V_0
    IL_0025:  ldfld      float64 AllTypesNoExplicitConstructor::isDouble
    IL_002a:  ldc.r8     0.0
    IL_0033:  bne.un.s   IL_004d

    IL_0035:  ldloca.s   V_0
    IL_0037:  ldflda     valuetype [mscorlib]System.RuntimeTypeHandle AllTypesNoExplicitConstructor::isRtTH
    IL_003c:  call       instance native int [mscorlib]System.RuntimeTypeHandle::get_Value()
    IL_0041:  ldsfld     native int [mscorlib]System.IntPtr::Zero
    IL_0046:  call       bool [mscorlib]System.IntPtr::op_Equality(native int,
                                                                   native int)
    IL_004b:  br.s       IL_004e

    IL_004d:  ldc.i4.0
    IL_004e:  stsfld     bool Program::IsInit
    IL_0053:  ldstr      "AllTypesNoExplicitConstructor: isBool: {0} isInt: "
    + "{1}, isShort: {2}, isDouble: {3}, isRtTH.Value: {4}"
    IL_0058:  ldc.i4.5
    IL_0059:  newarr     [mscorlib]System.Object
    IL_005e:  stloc.1
    IL_005f:  ldloc.1
    IL_0060:  ldc.i4.0
    IL_0061:  ldloca.s   V_0
    IL_0063:  ldfld      bool AllTypesNoExplicitConstructor::isBool
    IL_0068:  box        [mscorlib]System.Boolean
    IL_006d:  stelem.ref
    IL_006e:  ldloc.1
    IL_006f:  ldc.i4.1
    IL_0070:  ldloca.s   V_0
    IL_0072:  ldfld      int32 AllTypesNoExplicitConstructor::isInt
    IL_0077:  box        [mscorlib]System.Int32
    IL_007c:  stelem.ref
    IL_007d:  ldloc.1
    IL_007e:  ldc.i4.2
    IL_007f:  ldloca.s   V_0
    IL_0081:  ldfld      int16 AllTypesNoExplicitConstructor::isShort
    IL_0086:  box        [mscorlib]System.Int16
    IL_008b:  stelem.ref
    IL_008c:  ldloc.1
    IL_008d:  ldc.i4.3
    IL_008e:  ldloca.s   V_0
    IL_0090:  ldfld      float64 AllTypesNoExplicitConstructor::isDouble
    IL_0095:  box        [mscorlib]System.Double
    IL_009a:  stelem.ref
    IL_009b:  ldloc.1
    IL_009c:  ldc.i4.4
    IL_009d:  ldloca.s   V_0
    IL_009f:  ldflda     valuetype [mscorlib]System.RuntimeTypeHandle AllTypesNoExplicitConstructor::isRtTH
    IL_00a4:  call       instance native int [mscorlib]System.RuntimeTypeHandle::get_Value()
    IL_00a9:  box        [mscorlib]System.IntPtr
    IL_00ae:  stelem.ref
    IL_00af:  ldloc.1
    IL_00b0:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object[])
    IL_00b5:  ldsfld     bool Program::IsInit
    IL_00ba:  brtrue.s   IL_00c2

    IL_00bc:  ldc.i4.0
    IL_00bd:  stsfld     bool Program::AllPassed
    IL_00c2:  ret
  } // end of method Program::Test25

  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  1
    IL_0000:  call       void Program::Test1()
    IL_0005:  call       void Program::Test2()
    IL_000a:  call       void Program::Test3()
    IL_000f:  call       void Program::Test4()
    IL_0014:  call       void Program::Test5()
    IL_0019:  call       void Program::Test6()
    IL_001e:  call       void Program::Test7()
    IL_0023:  call       void Program::Test8()
    IL_0028:  call       void Program::Test9()
    IL_002d:  call       void Program::Test10()
    IL_0032:  call       void Program::Test11()
    IL_0037:  call       void Program::Test12()
    IL_003c:  call       void Program::Test13()
    IL_0041:  call       void Program::Test14()
    IL_0046:  call       void Program::Test15()
    IL_004b:  call       void Program::Test16()
    IL_0050:  call       void Program::Test17()
    IL_0055:  call       void Program::Test18()
    IL_005a:  call       void Program::Test19()
    IL_005f:  call       void Program::Test20()
    IL_0064:  call       void Program::Test21()
    IL_0069:  call       void Program::Test22()
    IL_006e:  call       void Program::Test23()
    IL_0073:  call       void Program::Test24()
    IL_0078:  call       void Program::Test25()
    IL_007d:  ldsfld     bool Program::AllPassed
    IL_0082:  brfalse.s  IL_0091

    IL_0084:  ldstr      "!!!!!!!!!! TEST PASSED !!!!!!!!!!!!!"
    IL_0089:  call       void [System.Console]System.Console::WriteLine(string)
    IL_008e:  ldc.i4.s   100
    IL_0090:  ret

    IL_0091:  ldstr      "!!!!!!!!!! TEST FAILED !!!!!!!!!!!!!"
    IL_0096:  call       void [System.Console]System.Console::WriteLine(string)
    IL_009b:  ldc.i4.s   101
    IL_009d:  ret
  } // end of method Program::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Program::.ctor

  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldc.i4.1
    IL_0001:  stsfld     bool Program::IsInit
    IL_0006:  ldc.i4.1
    IL_0007:  stsfld     bool Program::AllPassed
    IL_000c:  ret
  } // end of method Program::.cctor

} // end of class Program



