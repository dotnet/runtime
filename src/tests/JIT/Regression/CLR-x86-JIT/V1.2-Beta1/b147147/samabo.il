// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly samabo
{
}
.assembly extern xunit.core {}


.class public sequential ansi sealed beforefieldinit MyStruct
       extends [mscorlib]System.ValueType
{
  .class sequential ansi sealed nested public beforefieldinit buffer__FixedBufferImpl
         extends [mscorlib]System.ValueType
  {
    .pack 0
    .size 10
    .field public char FixedElementField
  } // end of class buffer__FixedBufferImpl

  .field private valuetype MyStruct/buffer__FixedBufferImpl buffer

  .method private hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       19 (0x13)
    .maxstack  2
    .locals ([0] int32 i)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.0
    IL_0002:  br.s       IL_000d

    IL_0004:  call       void MyStruct::RunTest()
    IL_0009:  ldloc.0
    IL_000a:  ldc.i4.1
    IL_000b:  add
    IL_000c:  stloc.0
    IL_000d:  ldloc.0
    IL_000e:  ldc.i4.s   10
    IL_0010:  blt.s      IL_0004

    IL_0012:  ldc.i4 100
    		ldstr "PASSED"
    	call       void [System.Console]System.Console::WriteLine(string)
    		ret
  } // end of method MyStruct::Main

  .method private hidebysig static void  RunTest() cil managed
  {
    // Code size       137 (0x89)
    .maxstack  3
    .locals ([0] valuetype MyStruct ms,
             [1] int32 i)
    IL_0000:  ldloca.s   ms
    IL_0002:  initobj    MyStruct
    IL_0008:  ldloca.s   ms
    IL_000a:  ldflda     valuetype MyStruct/buffer__FixedBufferImpl MyStruct::buffer
    IL_000f:  ldflda     char MyStruct/buffer__FixedBufferImpl::FixedElementField
    IL_0014:  ldc.i4.s   72
    IL_0016:  stind.i2
    IL_0017:  ldloca.s   ms
    IL_0019:  ldflda     valuetype MyStruct/buffer__FixedBufferImpl MyStruct::buffer
    IL_001e:  ldflda     char MyStruct/buffer__FixedBufferImpl::FixedElementField
    IL_0023:  ldc.i4.2
    IL_0024:  conv.i
    IL_0025:  add
    IL_0026:  ldc.i4.s   69
    IL_0028:  stind.i2
    IL_0029:  ldloca.s   ms
    IL_002b:  ldflda     valuetype MyStruct/buffer__FixedBufferImpl MyStruct::buffer
    IL_0030:  ldflda     char MyStruct/buffer__FixedBufferImpl::FixedElementField
    IL_0035:  ldc.i4.4
    IL_0036:  conv.i
    IL_0037:  add
    IL_0038:  ldc.i4.s   76
    IL_003a:  stind.i2
    IL_003b:  ldloca.s   ms
    IL_003d:  ldflda     valuetype MyStruct/buffer__FixedBufferImpl MyStruct::buffer
    IL_0042:  ldflda     char MyStruct/buffer__FixedBufferImpl::FixedElementField
    IL_0047:  ldc.i4.6
    IL_0048:  conv.i
    IL_0049:  add
    IL_004a:  ldc.i4.s   76
    IL_004c:  stind.i2
    IL_004d:  ldloca.s   ms
    IL_004f:  ldflda     valuetype MyStruct/buffer__FixedBufferImpl MyStruct::buffer
    IL_0054:  ldflda     char MyStruct/buffer__FixedBufferImpl::FixedElementField
    IL_0059:  ldc.i4.8
    IL_005a:  conv.i
    IL_005b:  add
    IL_005c:  ldc.i4.s   79
    IL_005e:  stind.i2
    IL_005f:  ldc.i4.0
    IL_0060:  stloc.1
    IL_0061:  br.s       IL_007e

    IL_0063:  ldloca.s   ms
    IL_0065:  ldflda     valuetype MyStruct/buffer__FixedBufferImpl MyStruct::buffer
    IL_006a:  ldflda     char MyStruct/buffer__FixedBufferImpl::FixedElementField
    IL_006f:  ldloc.1
    IL_0070:  conv.i
    IL_0071:  ldc.i4.2
    IL_0072:  mul
    IL_0073:  add
    IL_0074:  ldind.u2
    IL_0075:  call       void [System.Console]System.Console::Write(char)
    IL_007a:  ldloc.1
    IL_007b:  ldc.i4.1
    IL_007c:  add
    IL_007d:  stloc.1
    IL_007e:  ldloc.1
    IL_007f:  ldc.i4.4
    IL_0080:  ble.s      IL_0063

    IL_0082:  ldnull
    	call       void [System.Console]System.Console::WriteLine(string)
    IL_0087:  //pop
    IL_0088:  ret
  } // end of method MyStruct::RunTest

} // end of class MyStruct


