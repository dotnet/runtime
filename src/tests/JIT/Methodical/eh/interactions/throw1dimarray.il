// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// throw a 1 dimensional array, and then print the array



.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern mscorlib
{
  .ver 0:0:0:0
}
.assembly extern System.Windows.Forms
{
  .ver 0:0:0:0
}
.assembly extern eh_common{}

.assembly 'throw1dimarray'
{
  .ver 0:0:0:0
}
.assembly extern xunit.core {}
.imagebase 0x00400000
.subsystem 0x00000003
.file alignment 512
.corflags 0x00000001








.namespace Test_throw1dimarray
{
.class public auto ansi beforefieldinit App
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint

  .maxstack  4
  .locals init ([0] int32[] a2,
           [1] int32 i,
           [2] int32 j,
           [3] int32 i,
           [4] int32 i,
           [5] int32 i,
           [6] int32[] b,
           [7] int32[] V_7,
           [8] object o,
			class [mscorlib]System.IO.StringWriter expectedOut,
			class [eh_common]TestUtil.TestLog testLog
		)

    newobj     instance void [mscorlib]System.IO.StringWriter::.ctor()
    stloc.s    expectedOut
    ldloc.s    expectedOut
    ldstr      "1"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "3"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "1"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "2"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "3"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "In finally"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "1"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "2"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "3"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "In catch"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "1"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "2"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "3"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "In finally (outer)"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "1"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "2"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    ldloc.s    expectedOut
    ldstr      "3"
    callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
    
    ldloc.s    expectedOut
    newobj     instance void [eh_common]TestUtil.TestLog::.ctor(object)
    stloc.s    testLog

    ldloc.s    testLog
    callvirt   instance void [eh_common]TestUtil.TestLog::StartRecording()
  IL_0000:  ldnull
  IL_0001:  stloc.0
  IL_0002:  ldnull
  IL_0003:  stloc.s    b
  IL_0005:  ldnull
  IL_0006:  stloc.s    o
  IL_0008:  ldc.i4.3
  IL_0009:  newarr     [mscorlib]System.Int32
  IL_000e:  stloc.s    V_7
  IL_0010:  ldloc.s    V_7
  IL_0012:  call       instance void [mscorlib]System.Array::Initialize()
  IL_0017:  ldloc.s    V_7
  IL_0019:  stloc.0
  IL_001a:  ldloc.0
  IL_001b:  ldc.i4.0
  IL_001c:  ldc.i4.1
  IL_001d:  stelem.i4
  IL_001e:  ldloc.0
  IL_001f:  ldc.i4.1
  IL_0020:  ldc.i4.2
  IL_0021:  stelem.i4
  IL_0022:  ldloc.0
  IL_0023:  ldc.i4.2
  IL_0024:  ldc.i4.3
  IL_0025:  stelem.i4
  IL_0026:  ldloc.0
  IL_0027:  call       instance int32 [mscorlib]System.Array::get_Rank()
  IL_002c:  call       void [System.Console]System.Console::WriteLine(int32)
  IL_0031:  ldloc.0
  IL_0032:  ldlen
  IL_0033:  call       void [System.Console]System.Console::WriteLine(int32)
  IL_0038:  ldc.i4.0
  IL_0039:  stloc.s    i
  IL_003b:  br.s       IL_0043
  IL_003d:  ldloc.s    i
  IL_003f:  ldc.i4.1
  IL_0040:  add
  IL_0041:  stloc.s    i
  IL_0043:  ldloc.s    i
  IL_0045:  ldc.i4.s   10
  IL_0047:  bge        IL_00fc
  IL_004c:  ldloc.s    i
  IL_004e:  brfalse.s  IL_0055
  IL_0050:  br         IL_00f7
  .try
  {
    .try
    {
      .try
      {
        IL_0055:  ldc.i4.0
        IL_0056:  stloc.s    i
        IL_0058:  br.s       IL_0060
        IL_005a:  ldloc.s    i
        IL_005c:  ldc.i4.1
        IL_005d:  add
        IL_005e:  stloc.s    i
        IL_0060:  ldloc.s    i
        IL_0062:  ldloc.0
        IL_0063:  ldlen
        IL_0064:  bge.s      IL_0071
        IL_0066:  ldloc.0
        IL_0067:  ldloc.s    i
        IL_0069:  ldelem.i4
        IL_006a:  call       void [System.Console]System.Console::WriteLine(int32)
        IL_006f:  br.s       IL_005a
        IL_0071:  ldloc.0
        IL_0072:  throw
        IL_0073:  leave.s    IL_009c
      }  
      finally
      {
        IL_0075:  ldstr      "In finally"
        IL_007f:  call       void [System.Console]System.Console::WriteLine(string)
        IL_0084:  ldc.i4.0
        IL_0085:  stloc.3
        IL_0086:  br.s       IL_008c
        IL_0088:  ldloc.3
        IL_0089:  ldc.i4.1
        IL_008a:  add
        IL_008b:  stloc.3
        IL_008c:  ldloc.3
        IL_008d:  ldloc.0
        IL_008e:  ldlen
        IL_008f:  bge.s      IL_009b
        IL_0091:  ldloc.0
        IL_0092:  ldloc.3
        IL_0093:  ldelem.i4
        IL_0094:  call       void [System.Console]System.Console::WriteLine(int32)
        IL_0099:  br.s       IL_0088
        IL_009b:  endfinally
      }  
      IL_009c:  leave.s    IL_00ce
    }  
    catch [mscorlib]System.Object 
    {
      IL_009e:  stloc.s    o
      IL_00a5:  ldstr      "In catch"
      IL_00aa:  call       void [System.Console]System.Console::WriteLine(string)
      IL_00af:  ldloc.s    o
      IL_00b1:  stloc.s    b
      IL_00b3:  ldc.i4.0
      IL_00b4:  stloc.2
      IL_00b5:  br.s       IL_00bb
      IL_00b7:  ldloc.2
      IL_00b8:  ldc.i4.1
      IL_00b9:  add
      IL_00ba:  stloc.2
      IL_00bb:  ldloc.2
      IL_00bc:  ldloc.s    b
      IL_00be:  ldlen
      IL_00bf:  bge.s      IL_00cc
      IL_00c1:  ldloc.s    b
      IL_00c3:  ldloc.2
      IL_00c4:  ldelem.i4
      IL_00c5:  call       void [System.Console]System.Console::WriteLine(int32)
      IL_00ca:  br.s       IL_00b7
      IL_00cc:  leave.s    IL_00ce
    }  
    IL_00ce:  leave.s    IL_00f7
  }  
  finally
  {
    IL_00d5:  ldstr "In finally (outer)"
    IL_00da:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00df:  ldc.i4.0
    IL_00e0:  stloc.1
    IL_00e1:  br.s       IL_00e7
    IL_00e3:  ldloc.1
    IL_00e4:  ldc.i4.1
    IL_00e5:  add
    IL_00e6:  stloc.1
    IL_00e7:  ldloc.1
    IL_00e8:  ldloc.0
    IL_00e9:  ldlen
    IL_00ea:  bge.s      IL_00f6
    IL_00ec:  ldloc.0
    IL_00ed:  ldloc.1
    IL_00ee:  ldelem.i4
    IL_00ef:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_00f4:  br.s       IL_00e3
    IL_00f6:  endfinally
  }  
  IL_00f7:  br         IL_003d
  IL_00fc:  
		ldloc.s    testLog
		callvirt   instance void [eh_common]TestUtil.TestLog::StopRecording()
		ldloc.s    testLog
		callvirt   instance int32 [eh_common]TestUtil.TestLog::VerifyOutput()
  
  IL_00fd:  ret

  } 

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } 

} 

.class private auto ansi beforefieldinit MyException
       extends [mscorlib]System.Exception
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(string msg) cil managed
  {
    .maxstack  2
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  call       instance void [mscorlib]System.Exception::.ctor(string)
    IL_0007:  ldarg.0
    IL_0008:  ldstr      "http://NotARealURL.Microsoft.com/help.html"
    IL_000d:  callvirt   instance void [mscorlib]System.Exception::set_HelpLink(string)
    IL_0012:  ret
  } 

} 
}
