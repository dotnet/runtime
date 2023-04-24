// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly '25param1b'{}
.assembly extern xunit.core {}
.namespace Test_25param1b_il
{
.class public auto ansi beforefieldinit test
       extends [mscorlib]System.Object
{
  .method private hidebysig static int32 
          f1(int32 a1,
             int32 a2,
             int32 a3,
             int32 a4,
             int32 a5,
             int32 a6,
             int32 a7,
             int32 a8,
             int32 a9,
             int32 a10,
             int32 a11,
             int32 a12,
             int32 a13,
             int32 a14,
             int32 a15,
             int32 a16,
             int32 a17,
             int32 a18,
             int32 a19,
             int32 a20,
             int32 a21,
             int32 a22,
             int32 a23,
             int32 a24,
             int32 a25) cil managed
  {
    // Code size       93 (0x5d)
    .maxstack  5
    .locals init (int32 V_0,
             int32 V_1)
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  add
    IL_0003:  ldarg.2
    IL_0004:  add
    IL_0005:  ldarg.3
    IL_0006:  add
    IL_0007:  ldarg.s    a5
    IL_0009:  add
    IL_000a:  ldarg.s    a6
    IL_000c:  add
    IL_000d:  ldarg.s    a7
    IL_000f:  add
    IL_0010:  ldarg.s    a8
    IL_0012:  add
    IL_0013:  ldarg.s    a9
    IL_0015:  add
    IL_0016:  ldarg.s    a10
    IL_0018:  add
    IL_0019:  ldarg.s    a11
    IL_001b:  add
    IL_001c:  ldarg.s    a12
    IL_001e:  add
    IL_001f:  ldarg.s    a13
    IL_0021:  add
    IL_0022:  ldarg.s    a14
    IL_0024:  add
    IL_0025:  ldarg.s    a15
    IL_0027:  add
    IL_0028:  ldarg.s    a16
    IL_002a:  add
    IL_002b:  ldarg.s    a17
    IL_002d:  add
    IL_002e:  ldarg.s    a18
    IL_0030:  add
    IL_0031:  ldarg.s    a19
    IL_0033:  add
    IL_0034:  ldarg.s    a20
    IL_0036:  add
    IL_0037:  ldarg.s    a21
    IL_0039:  add
    IL_003a:  ldarg.s    a22
    IL_003c:  add
    IL_003d:  ldarg.s    a23
    IL_003f:  add
    IL_0040:  ldarg.s    a24
    IL_0042:  add
    IL_0043:  ldarg.s    a25
    IL_0045:  add
    IL_0046:  stloc.0
    IL_0047:  ldstr      "The sum is {0}"
		ldloc.0
		box        [mscorlib]System.Int32
		call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_0057:  ldloc.0
    IL_0058:  stloc.1
    IL_0059:  br.s       IL_005b

    IL_005b:  ldloc.1
    IL_005c:  ret
  } // end of method Test_25param1b_il.test::f1

  .method private hidebysig static int32 
          f(int32 a1,
            int32 a2,
            int32 a3,
            int32 a4,
            int32 a5,
            int32 a6,
            int32 a7,
            int32 a8,
            int32 a9,
            int32 a10,
            int32 a11,
            int32 a12,
            int32 a13,
            int32 a14,
            int32 a15,
            int32 a16,
            int32 a17,
            int32 a18,
            int32 a19,
            int32 a20,
            int32 a21,
            int32 a22,
            int32 a23,
            int32 a24,
            int32 a25) cil managed
  {
    // Code size       229 (0xe5)
    .maxstack  25
    .locals init (int32 V_0,
             int32 V_1)
    IL_0000:  ldarg.0
    IL_0001:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_0006:  ldarg.1
    IL_0007:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_000c:  ldarg.2
    IL_000d:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_0012:  ldarg.3
    IL_0013:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_0018:  ldarg.s    a5
    IL_001a:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_001f:  ldarg.s    a6
    IL_0021:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_0026:  ldarg.s    a7
    IL_0028:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_002d:  ldarg.s    a8
    IL_002f:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_0034:  ldarg.s    a9
    IL_0036:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_003b:  ldarg.s    a10
    IL_003d:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_0042:  ldarg.s    a11
    IL_0044:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_0049:  ldarg.s    a12
    IL_004b:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_0050:  ldarg.s    a13
    IL_0052:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_0057:  ldarg.s    a14
    IL_0059:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_005e:  ldarg.s    a15
    IL_0060:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_0065:  ldarg.s    a16
    IL_0067:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_006c:  ldarg.s    a17
    IL_006e:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_0073:  ldarg.s    a18
    IL_0075:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_007a:  ldarg.s    a19
    IL_007c:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_0081:  ldarg.s    a20
    IL_0083:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_0088:  ldarg.s    a21
    IL_008a:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_008f:  ldarg.s    a22
    IL_0091:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_0096:  ldarg.s    a23
    IL_0098:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_009d:  ldarg.s    a24
    IL_009f:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_00a4:  ldarg.s    a25
    IL_00a6:  call       void [System.Console]System.Console::WriteLine(int32)
    jmp       int32 Test_25param1b_il.test::f1(int32,
                                        int32,
                                        int32,
                                        int32,
                                        int32,
                                        int32,
                                        int32,
                                        int32,
                                        int32,
                                        int32,
                                        int32,
                                        int32,
                                        int32,
                                        int32,
                                        int32,
                                        int32,
                                        int32,
                                        int32,
                                        int32,
                                        int32,
                                        int32,
                                        int32,
                                        int32,
                                        int32,
                                        int32)
  } // end of method Test_25param1b_il.test::f

  .method public hidebysig static int32 
          Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       67 (0x43)
    .maxstack  25
    .locals init (int32 V_0,
             int32 V_1)
ldstr "Testing method of 25 parameters, all of int data type, jmp"
call void [System.Console]System.Console::WriteLine(string)
    IL_0000:  ldc.i4.1
    IL_0001:  ldc.i4.2
    IL_0002:  ldc.i4.3
    IL_0003:  ldc.i4.4
    IL_0004:  ldc.i4.5
    IL_0005:  ldc.i4.6
    IL_0006:  ldc.i4.7
    IL_0007:  ldc.i4.8
    IL_0008:  ldc.i4.s   9
    IL_000a:  ldc.i4.s   10
    IL_000c:  ldc.i4.s   11
    IL_000e:  ldc.i4.s   12
    IL_0010:  ldc.i4.s   13
    IL_0012:  ldc.i4.s   14
    IL_0014:  ldc.i4.s   15
    IL_0016:  ldc.i4.s   16
    IL_0018:  ldc.i4.s   17
    IL_001a:  ldc.i4.s   18
    IL_001c:  ldc.i4.s   19
    IL_001e:  ldc.i4.s   20
    IL_0020:  ldc.i4.s   21
    IL_0022:  ldc.i4.s   22
    IL_0024:  ldc.i4.s   23
    IL_0026:  ldc.i4.s   24
    IL_0028:  ldc.i4.s   25
    IL_002a:  call       int32 Test_25param1b_il.test::f(int32,
                                       int32,
                                       int32,
                                       int32,
                                       int32,
                                       int32,
                                       int32,
                                       int32,
                                       int32,
                                       int32,
                                       int32,
                                       int32,
                                       int32,
                                       int32,
                                       int32,
                                       int32,
                                       int32,
                                       int32,
                                       int32,
                                       int32,
                                       int32,
                                       int32,
                                       int32,
                                       int32,
                                       int32)
    IL_002f:  stloc.0
    IL_0030:  ldloc.0
    IL_0031:  ldc.i4     0x145
    IL_0036:  bne.un.s   IL_003d

    IL_0038:  ldc.i4.s   100
    IL_003a:  stloc.1
    IL_003b:  br.s       IL_0041

    IL_003d:  ldc.i4.1
    IL_003e:  stloc.1
    IL_003f:  br.s       IL_0041

    IL_0041:  ldloc.1
    IL_0042:  ret
  } // end of method Test_25param1b_il.test::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Test_25param1b_il.test::.ctor

}
}
