// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly b124443_il
{}

.module b124443_il.exe

.class private auto ansi sealed TestDelegate
       extends [mscorlib]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(object 'object',
                               native int 'method') runtime managed
  {
  } // end of method TestDelegate::.ctor

  .method public hidebysig newslot virtual 
          instance void  Invoke() runtime managed
  {
  } // end of method TestDelegate::Invoke

  .method public hidebysig newslot virtual 
          instance class [mscorlib]System.IAsyncResult 
          BeginInvoke(class [mscorlib]System.AsyncCallback callback,
                      object 'object') runtime managed
  {
  } // end of method TestDelegate::BeginInvoke

  .method public hidebysig newslot virtual 
          instance void  EndInvoke(class [mscorlib]System.IAsyncResult result) runtime managed
  {
  } // end of method TestDelegate::EndInvoke

} // end of class TestDelegate

.class private auto ansi beforefieldinit ArrayBounds
       extends [mscorlib]System.Object
{
  .method public hidebysig static void  f1a() cil managed
  {
    // Code size       33 (0x21)
    .maxstack  3
    .locals init (int32[] V_0,
             int32 V_1,
             bool V_2)
    IL_0000:  nop
    IL_0001:  ldc.i4.4
    IL_0002:  newarr     [mscorlib]System.Int32
    IL_0007:  stloc.0
    IL_0008:  ldc.i4.0
    IL_0009:  stloc.1
    IL_000a:  br.s       IL_0016

    IL_000c:  nop
    IL_000d:  ldloc.0
    IL_000e:  ldloc.1
    IL_000f:  ldc.i4.1
    IL_0010:  stelem.i4
    IL_0011:  nop
    IL_0012:  ldloc.1
    IL_0013:  ldc.i4.1
    IL_0014:  sub
    IL_0015:  stloc.1
    IL_0016:  ldloc.1
    IL_0017:  ldloc.0
    IL_0018:  ldlen
    IL_0019:  conv.i4
    IL_001a:  clt
    IL_001c:  stloc.2
    IL_001d:  ldloc.2
    IL_001e:  brtrue.s   IL_000c

    IL_0020:  ret
  } // end of method ArrayBounds::f1a

  .method public hidebysig static void  f2a() cil managed
  {
    // Code size       33 (0x21)
    .maxstack  2
    .locals init (int32[] V_0,
             int32 V_1,
             int32 V_2,
             bool V_3)
    IL_0000:  nop
    IL_0001:  ldc.i4.4
    IL_0002:  newarr     [mscorlib]System.Int32
    IL_0007:  stloc.0
    IL_0008:  ldc.i4.0
    IL_0009:  stloc.1
    IL_000a:  br.s       IL_0016

    IL_000c:  nop
    IL_000d:  ldloc.0
    IL_000e:  ldloc.1
    IL_000f:  ldelem.i4
    IL_0010:  stloc.2
    IL_0011:  nop
    IL_0012:  ldloc.1
    IL_0013:  ldc.i4.1
    IL_0014:  sub
    IL_0015:  stloc.1
    IL_0016:  ldloc.1
    IL_0017:  ldloc.0
    IL_0018:  ldlen
    IL_0019:  conv.i4
    IL_001a:  clt
    IL_001c:  stloc.3
    IL_001d:  ldloc.3
    IL_001e:  brtrue.s   IL_000c

    IL_0020:  ret
  } // end of method ArrayBounds::f2a

  .method public hidebysig static void  f3a() cil managed
  {
    // Code size       38 (0x26)
    .maxstack  2
    .locals init (int32[] V_0,
             int32 V_1,
             bool V_2)
    IL_0000:  nop
    IL_0001:  ldc.i4.4
    IL_0002:  newarr     [mscorlib]System.Int32
    IL_0007:  stloc.0
    IL_0008:  ldc.i4.0
    IL_0009:  stloc.1
    IL_000a:  br.s       IL_001b

    IL_000c:  nop
    IL_000d:  ldloc.0
    IL_000e:  ldloc.1
    IL_000f:  ldelem.i4
    IL_0010:  call       void [mscorlib]System.Console::WriteLine(int32)
    IL_0015:  nop
    IL_0016:  nop
    IL_0017:  ldloc.1
    IL_0018:  ldc.i4.1
    IL_0019:  sub
    IL_001a:  stloc.1
    IL_001b:  ldloc.1
    IL_001c:  ldloc.0
    IL_001d:  ldlen
    IL_001e:  conv.i4
    IL_001f:  clt
    IL_0021:  stloc.2
    IL_0022:  ldloc.2
    IL_0023:  brtrue.s   IL_000c

    IL_0025:  ret
  } // end of method ArrayBounds::f3a

  .method public hidebysig static void  f4a() cil managed
  {
    // Code size       33 (0x21)
    .maxstack  3
    .locals init (int32[] V_0,
             int32 V_1,
             bool V_2)
    IL_0000:  nop
    IL_0001:  ldc.i4.4
    IL_0002:  newarr     [mscorlib]System.Int32
    IL_0007:  stloc.0
    IL_0008:  ldc.i4.0
    IL_0009:  stloc.1
    IL_000a:  br.s       IL_0016

    IL_000c:  nop
    IL_000d:  nop
    IL_000e:  ldloc.0
    IL_000f:  ldloc.1
    IL_0010:  ldloc.1
    IL_0011:  stelem.i4
    IL_0012:  ldloc.1
    IL_0013:  ldc.i4.1
    IL_0014:  sub
    IL_0015:  stloc.1
    IL_0016:  ldloc.1
    IL_0017:  ldloc.0
    IL_0018:  ldlen
    IL_0019:  conv.i4
    IL_001a:  clt
    IL_001c:  stloc.2
    IL_001d:  ldloc.2
    IL_001e:  brtrue.s   IL_000c

    IL_0020:  ret
  } // end of method ArrayBounds::f4a

  .method public hidebysig static void  f1b() cil managed
  {
    // Code size       36 (0x24)
    .maxstack  3
    .locals init (int32[] V_0,
             int32 V_1,
             bool V_2)
    IL_0000:  nop
    IL_0001:  ldc.i4.4
    IL_0002:  newarr     [mscorlib]System.Int32
    IL_0007:  stloc.0
    IL_0008:  ldc.i4.0
    IL_0009:  stloc.1
    IL_000a:  br.s       IL_0016

    IL_000c:  nop
    IL_000d:  ldloc.0
    IL_000e:  ldloc.1
    IL_000f:  ldc.i4.1
    IL_0010:  stelem.i4
    IL_0011:  nop
    IL_0012:  ldloc.1
    IL_0013:  ldc.i4.1
    IL_0014:  add
    IL_0015:  stloc.1
    IL_0016:  ldloc.1
    IL_0017:  ldloc.0
    IL_0018:  ldlen
    IL_0019:  conv.i4
    IL_001a:  cgt
    IL_001c:  ldc.i4.0
    IL_001d:  ceq
    IL_001f:  stloc.2
    IL_0020:  ldloc.2
    IL_0021:  brtrue.s   IL_000c

    IL_0023:  ret
  } // end of method ArrayBounds::f1b

  .method public hidebysig static void  f2b() cil managed
  {
    // Code size       36 (0x24)
    .maxstack  2
    .locals init (int32[] V_0,
             int32 V_1,
             int32 V_2,
             bool V_3)
    IL_0000:  nop
    IL_0001:  ldc.i4.4
    IL_0002:  newarr     [mscorlib]System.Int32
    IL_0007:  stloc.0
    IL_0008:  ldc.i4.0
    IL_0009:  stloc.1
    IL_000a:  br.s       IL_0016

    IL_000c:  nop
    IL_000d:  ldloc.0
    IL_000e:  ldloc.1
    IL_000f:  ldelem.i4
    IL_0010:  stloc.2
    IL_0011:  nop
    IL_0012:  ldloc.1
    IL_0013:  ldc.i4.1
    IL_0014:  add
    IL_0015:  stloc.1
    IL_0016:  ldloc.1
    IL_0017:  ldloc.0
    IL_0018:  ldlen
    IL_0019:  conv.i4
    IL_001a:  cgt
    IL_001c:  ldc.i4.0
    IL_001d:  ceq
    IL_001f:  stloc.3
    IL_0020:  ldloc.3
    IL_0021:  brtrue.s   IL_000c

    IL_0023:  ret
  } // end of method ArrayBounds::f2b

  .method public hidebysig static void  f3b() cil managed
  {
    // Code size       41 (0x29)
    .maxstack  2
    .locals init (int32[] V_0,
             int32 V_1,
             bool V_2)
    IL_0000:  nop
    IL_0001:  ldc.i4.4
    IL_0002:  newarr     [mscorlib]System.Int32
    IL_0007:  stloc.0
    IL_0008:  ldc.i4.0
    IL_0009:  stloc.1
    IL_000a:  br.s       IL_001b

    IL_000c:  nop
    IL_000d:  ldloc.0
    IL_000e:  ldloc.1
    IL_000f:  ldelem.i4
    IL_0010:  call       void [mscorlib]System.Console::WriteLine(int32)
    IL_0015:  nop
    IL_0016:  nop
    IL_0017:  ldloc.1
    IL_0018:  ldc.i4.1
    IL_0019:  add
    IL_001a:  stloc.1
    IL_001b:  ldloc.1
    IL_001c:  ldloc.0
    IL_001d:  ldlen
    IL_001e:  conv.i4
    IL_001f:  cgt
    IL_0021:  ldc.i4.0
    IL_0022:  ceq
    IL_0024:  stloc.2
    IL_0025:  ldloc.2
    IL_0026:  brtrue.s   IL_000c

    IL_0028:  ret
  } // end of method ArrayBounds::f3b

  .method public hidebysig static void  f4b() cil managed
  {
    // Code size       36 (0x24)
    .maxstack  3
    .locals init (int32[] V_0,
             int32 V_1,
             bool V_2)
    IL_0000:  nop
    IL_0001:  ldc.i4.4
    IL_0002:  newarr     [mscorlib]System.Int32
    IL_0007:  stloc.0
    IL_0008:  ldc.i4.0
    IL_0009:  stloc.1
    IL_000a:  br.s       IL_0016

    IL_000c:  nop
    IL_000d:  nop
    IL_000e:  ldloc.0
    IL_000f:  ldloc.1
    IL_0010:  ldloc.1
    IL_0011:  stelem.i4
    IL_0012:  ldloc.1
    IL_0013:  ldc.i4.1
    IL_0014:  add
    IL_0015:  stloc.1
    IL_0016:  ldloc.1
    IL_0017:  ldloc.0
    IL_0018:  ldlen
    IL_0019:  conv.i4
    IL_001a:  cgt
    IL_001c:  ldc.i4.0
    IL_001d:  ceq
    IL_001f:  stloc.2
    IL_0020:  ldloc.2
    IL_0021:  brtrue.s   IL_000c

    IL_0023:  ret
  } // end of method ArrayBounds::f4b

  .method public hidebysig static void  f1c() cil managed
  {
    // Code size       44 (0x2c)
    .maxstack  3
    .locals init (bool[] V_0,
             int16 V_1,
             bool V_2)
    IL_0000:  nop
    IL_0001:  ldc.i4     0x7fff
    IL_0006:  newarr     [mscorlib]System.Boolean
    IL_000b:  stloc.0
    IL_000c:  ldc.i4     0x7ff0
    IL_0011:  stloc.1
    IL_0012:  br.s       IL_001f

    IL_0014:  nop
    IL_0015:  ldloc.0
    IL_0016:  ldloc.1
    IL_0017:  ldc.i4.1
    IL_0018:  stelem.i1
    IL_0019:  nop
    IL_001a:  ldloc.1
    IL_001b:  ldc.i4.1
    IL_001c:  add
    IL_001d:  conv.i2
    IL_001e:  stloc.1
    IL_001f:  ldloc.1
    IL_0020:  ldloc.0
    IL_0021:  ldlen
    IL_0022:  conv.i4
    IL_0023:  ldc.i4.1
    IL_0024:  add
    IL_0025:  clt
    IL_0027:  stloc.2
    IL_0028:  ldloc.2
    IL_0029:  brtrue.s   IL_0014

    IL_002b:  ret
  } // end of method ArrayBounds::f1c

  .method public hidebysig static void  f2c() cil managed
  {
    // Code size       44 (0x2c)
    .maxstack  3
    .locals init (bool[] V_0,
             int16 V_1,
             bool V_2,
             bool V_3)
    IL_0000:  nop
    IL_0001:  ldc.i4     0x7fff
    IL_0006:  newarr     [mscorlib]System.Boolean
    IL_000b:  stloc.0
    IL_000c:  ldc.i4     0x7ff0
    IL_0011:  stloc.1
    IL_0012:  br.s       IL_001f

    IL_0014:  nop
    IL_0015:  ldloc.0
    IL_0016:  ldloc.1
    IL_0017:  ldelem.i1
    IL_0018:  stloc.2
    IL_0019:  nop
    IL_001a:  ldloc.1
    IL_001b:  ldc.i4.1
    IL_001c:  add
    IL_001d:  conv.i2
    IL_001e:  stloc.1
    IL_001f:  ldloc.1
    IL_0020:  ldloc.0
    IL_0021:  ldlen
    IL_0022:  conv.i4
    IL_0023:  ldc.i4.1
    IL_0024:  add
    IL_0025:  clt
    IL_0027:  stloc.3
    IL_0028:  ldloc.3
    IL_0029:  brtrue.s   IL_0014

    IL_002b:  ret
  } // end of method ArrayBounds::f2c

  .method public hidebysig static void  f3c() cil managed
  {
    // Code size       49 (0x31)
    .maxstack  3
    .locals init (bool[] V_0,
             int16 V_1,
             bool V_2)
    IL_0000:  nop
    IL_0001:  ldc.i4     0x7fff
    IL_0006:  newarr     [mscorlib]System.Boolean
    IL_000b:  stloc.0
    IL_000c:  ldc.i4     0x7ffe
    IL_0011:  stloc.1
    IL_0012:  br.s       IL_0024

    IL_0014:  nop
    IL_0015:  ldloc.0
    IL_0016:  ldloc.1
    IL_0017:  ldelem.i1
    IL_0018:  call       void [mscorlib]System.Console::WriteLine(bool)
    IL_001d:  nop
    IL_001e:  nop
    IL_001f:  ldloc.1
    IL_0020:  ldc.i4.1
    IL_0021:  add
    IL_0022:  conv.i2
    IL_0023:  stloc.1
    IL_0024:  ldloc.1
    IL_0025:  ldloc.0
    IL_0026:  ldlen
    IL_0027:  conv.i4
    IL_0028:  ldc.i4.1
    IL_0029:  add
    IL_002a:  clt
    IL_002c:  stloc.2
    IL_002d:  ldloc.2
    IL_002e:  brtrue.s   IL_0014

    IL_0030:  ret
  } // end of method ArrayBounds::f3c

  .method public hidebysig static void  f4c() cil managed
  {
    // Code size       44 (0x2c)
    .maxstack  3
    .locals init (bool[] V_0,
             int16 V_1,
             bool V_2)
    IL_0000:  nop
    IL_0001:  ldc.i4     0x7fff
    IL_0006:  newarr     [mscorlib]System.Boolean
    IL_000b:  stloc.0
    IL_000c:  ldc.i4     0x7ff0
    IL_0011:  stloc.1
    IL_0012:  br.s       IL_001f

    IL_0014:  nop
    IL_0015:  ldloc.0
    IL_0016:  ldloc.1
    IL_0017:  ldc.i4.1
    IL_0018:  stelem.i1
    IL_0019:  nop
    IL_001a:  ldloc.1
    IL_001b:  ldc.i4.1
    IL_001c:  add
    IL_001d:  conv.i2
    IL_001e:  stloc.1
    IL_001f:  ldloc.1
    IL_0020:  ldloc.0
    IL_0021:  ldlen
    IL_0022:  conv.i4
    IL_0023:  ldc.i4.1
    IL_0024:  add
    IL_0025:  clt
    IL_0027:  stloc.2
    IL_0028:  ldloc.2
    IL_0029:  brtrue.s   IL_0014

    IL_002b:  ret
  } // end of method ArrayBounds::f4c

  .method public hidebysig static int32  RunTests(class TestDelegate d) cil managed
  {
    // Code size       101 (0x65)
    .maxstack  2
    .locals init (class [mscorlib]System.Exception V_0,
             int32 V_1)
    IL_0000:  nop
    .try
    {
      IL_0001:  nop
      IL_0002:  ldarg.0
      IL_0003:  callvirt   instance class [mscorlib]System.Reflection.MethodInfo [mscorlib]System.Delegate::get_Method()
      IL_0008:  callvirt   instance string [mscorlib]System.Reflection.MemberInfo::get_Name()
      IL_000d:  ldstr      ": "
      IL_0012:  call       string [mscorlib]System.String::Concat(string,
                                                                  string)
      IL_0017:  call       void [mscorlib]System.Console::Write(string)
      IL_001c:  nop
      IL_001d:  ldarg.0
      IL_001e:  callvirt   instance void TestDelegate::Invoke()
      IL_0023:  nop
      IL_0024:  nop
      IL_0025:  leave.s    IL_0051

    }  // end .try
    catch [mscorlib]System.IndexOutOfRangeException 
    {
      IL_0027:  pop
      IL_0028:  nop
      IL_0029:  ldstr      "IndexOutOfRangeException caught as expected"
      IL_002e:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0033:  nop
      IL_0034:  ldc.i4.s   100
      IL_0036:  stloc.1
      IL_0037:  leave.s    IL_0062

    }  // end handler
    catch [mscorlib]System.Exception 
    {
      IL_0039:  stloc.0
      IL_003a:  nop
      IL_003b:  ldstr      "FAILED"
      IL_0040:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0045:  nop
      IL_0046:  ldloc.0
      IL_0047:  call       void [mscorlib]System.Console::WriteLine(object)
      IL_004c:  nop
      IL_004d:  ldc.i4.1
      IL_004e:  stloc.1
      IL_004f:  leave.s    IL_0062

    }  // end handler
    IL_0051:  nop
    IL_0052:  ldstr      "PASSED"
    IL_0057:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_005c:  nop
    IL_005d:  ldc.i4.s   100
    IL_005f:  stloc.1
    IL_0060:  br.s       IL_0062

    IL_0062:  nop
    IL_0063:  ldloc.1
    IL_0064:  ret
  } // end of method ArrayBounds::RunTests

  .method public hidebysig static int32  Main() cil managed
  {
    .entrypoint
    // Code size       397 (0x18d)
    .maxstack  2
    .locals init (int32 V_0,
             bool V_1)
    IL_0000:  nop
    IL_0001:  ldnull
    IL_0002:  ldftn      void ArrayBounds::f1a()
    IL_0008:  newobj     instance void TestDelegate::.ctor(object,
                                                           native int)
    IL_000d:  call       int32 ArrayBounds::RunTests(class TestDelegate)
    IL_0012:  ldc.i4.s   100
    IL_0014:  ceq
    IL_0016:  stloc.1
    IL_0017:  ldloc.1
    IL_0018:  brtrue.s   IL_0021

    IL_001a:  ldc.i4.1
    IL_001b:  stloc.0
    IL_001c:  br         IL_018b

    IL_0021:  ldnull
    IL_0022:  ldftn      void ArrayBounds::f2a()
    IL_0028:  newobj     instance void TestDelegate::.ctor(object,
                                                           native int)
    IL_002d:  call       int32 ArrayBounds::RunTests(class TestDelegate)
    IL_0032:  ldc.i4.s   100
    IL_0034:  ceq
    IL_0036:  stloc.1
    IL_0037:  ldloc.1
    IL_0038:  brtrue.s   IL_0041

    IL_003a:  ldc.i4.1
    IL_003b:  stloc.0
    IL_003c:  br         IL_018b

    IL_0041:  ldnull
    IL_0042:  ldftn      void ArrayBounds::f3a()
    IL_0048:  newobj     instance void TestDelegate::.ctor(object,
                                                           native int)
    IL_004d:  call       int32 ArrayBounds::RunTests(class TestDelegate)
    IL_0052:  ldc.i4.s   100
    IL_0054:  ceq
    IL_0056:  stloc.1
    IL_0057:  ldloc.1
    IL_0058:  brtrue.s   IL_0061

    IL_005a:  ldc.i4.1
    IL_005b:  stloc.0
    IL_005c:  br         IL_018b

    IL_0061:  ldnull
    IL_0062:  ldftn      void ArrayBounds::f4a()
    IL_0068:  newobj     instance void TestDelegate::.ctor(object,
                                                           native int)
    IL_006d:  call       int32 ArrayBounds::RunTests(class TestDelegate)
    IL_0072:  ldc.i4.s   100
    IL_0074:  ceq
    IL_0076:  stloc.1
    IL_0077:  ldloc.1
    IL_0078:  brtrue.s   IL_0081

    IL_007a:  ldc.i4.1
    IL_007b:  stloc.0
    IL_007c:  br         IL_018b

    IL_0081:  ldnull
    IL_0082:  ldftn      void ArrayBounds::f1b()
    IL_0088:  newobj     instance void TestDelegate::.ctor(object,
                                                           native int)
    IL_008d:  call       int32 ArrayBounds::RunTests(class TestDelegate)
    IL_0092:  ldc.i4.s   100
    IL_0094:  ceq
    IL_0096:  stloc.1
    IL_0097:  ldloc.1
    IL_0098:  brtrue.s   IL_00a1

    IL_009a:  ldc.i4.1
    IL_009b:  stloc.0
    IL_009c:  br         IL_018b

    IL_00a1:  ldnull
    IL_00a2:  ldftn      void ArrayBounds::f2b()
    IL_00a8:  newobj     instance void TestDelegate::.ctor(object,
                                                           native int)
    IL_00ad:  call       int32 ArrayBounds::RunTests(class TestDelegate)
    IL_00b2:  ldc.i4.s   100
    IL_00b4:  ceq
    IL_00b6:  stloc.1
    IL_00b7:  ldloc.1
    IL_00b8:  brtrue.s   IL_00c1

    IL_00ba:  ldc.i4.1
    IL_00bb:  stloc.0
    IL_00bc:  br         IL_018b

    IL_00c1:  ldnull
    IL_00c2:  ldftn      void ArrayBounds::f3b()
    IL_00c8:  newobj     instance void TestDelegate::.ctor(object,
                                                           native int)
    IL_00cd:  call       int32 ArrayBounds::RunTests(class TestDelegate)
    IL_00d2:  ldc.i4.s   100
    IL_00d4:  ceq
    IL_00d6:  stloc.1
    IL_00d7:  ldloc.1
    IL_00d8:  brtrue.s   IL_00e1

    IL_00da:  ldc.i4.1
    IL_00db:  stloc.0
    IL_00dc:  br         IL_018b

    IL_00e1:  ldnull
    IL_00e2:  ldftn      void ArrayBounds::f4b()
    IL_00e8:  newobj     instance void TestDelegate::.ctor(object,
                                                           native int)
    IL_00ed:  call       int32 ArrayBounds::RunTests(class TestDelegate)
    IL_00f2:  ldc.i4.s   100
    IL_00f4:  ceq
    IL_00f6:  stloc.1
    IL_00f7:  ldloc.1
    IL_00f8:  brtrue.s   IL_0101

    IL_00fa:  ldc.i4.1
    IL_00fb:  stloc.0
    IL_00fc:  br         IL_018b

    IL_0101:  ldnull
    IL_0102:  ldftn      void ArrayBounds::f1c()
    IL_0108:  newobj     instance void TestDelegate::.ctor(object,
                                                           native int)
    IL_010d:  call       int32 ArrayBounds::RunTests(class TestDelegate)
    IL_0112:  ldc.i4.s   100
    IL_0114:  ceq
    IL_0116:  stloc.1
    IL_0117:  ldloc.1
    IL_0118:  brtrue.s   IL_011e

    IL_011a:  ldc.i4.1
    IL_011b:  stloc.0
    IL_011c:  br.s       IL_018b

    IL_011e:  ldnull
    IL_011f:  ldftn      void ArrayBounds::f2c()
    IL_0125:  newobj     instance void TestDelegate::.ctor(object,
                                                           native int)
    IL_012a:  call       int32 ArrayBounds::RunTests(class TestDelegate)
    IL_012f:  ldc.i4.s   100
    IL_0131:  ceq
    IL_0133:  stloc.1
    IL_0134:  ldloc.1
    IL_0135:  brtrue.s   IL_013b

    IL_0137:  ldc.i4.1
    IL_0138:  stloc.0
    IL_0139:  br.s       IL_018b

    IL_013b:  ldnull
    IL_013c:  ldftn      void ArrayBounds::f3c()
    IL_0142:  newobj     instance void TestDelegate::.ctor(object,
                                                           native int)
    IL_0147:  call       int32 ArrayBounds::RunTests(class TestDelegate)
    IL_014c:  ldc.i4.s   100
    IL_014e:  ceq
    IL_0150:  stloc.1
    IL_0151:  ldloc.1
    IL_0152:  brtrue.s   IL_0158

    IL_0154:  ldc.i4.1
    IL_0155:  stloc.0
    IL_0156:  br.s       IL_018b

    IL_0158:  ldnull
    IL_0159:  ldftn      void ArrayBounds::f4c()
    IL_015f:  newobj     instance void TestDelegate::.ctor(object,
                                                           native int)
    IL_0164:  call       int32 ArrayBounds::RunTests(class TestDelegate)
    IL_0169:  ldc.i4.s   100
    IL_016b:  ceq
    IL_016d:  stloc.1
    IL_016e:  ldloc.1
    IL_016f:  brtrue.s   IL_0175

    IL_0171:  ldc.i4.1
    IL_0172:  stloc.0
    IL_0173:  br.s       IL_018b

    IL_0175:  call       void [mscorlib]System.Console::WriteLine()
    IL_017a:  nop
    IL_017b:  ldstr      "PASSED"
    IL_0180:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0185:  nop
    IL_0186:  ldc.i4.s   100
    IL_0188:  stloc.0
    IL_0189:  br.s       IL_018b

    IL_018b:  ldloc.0
    IL_018c:  ret
  } // end of method ArrayBounds::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method ArrayBounds::.ctor

} // end of class ArrayBounds
