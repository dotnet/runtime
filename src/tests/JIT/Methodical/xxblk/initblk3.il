// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//transient, managed, unmanaged pointer
//"plain", volatile., unaligned. initblk
.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'initblk3' {}
.namespace Test_initblk3
{

.class public explicit ansi sealed $MultiByte$size$5000 extends [mscorlib]System.ValueType
{
  .pack 1
  .size 5000
} // end of class Test_initblk3.$MultiByte$size$5000

.class public initblk2 extends [mscorlib]System.Object
{
.method public static int32 main() 
{
  .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
      01 00 00 00
  )
  .entrypoint
  .maxstack  3
  .locals (int32 V_0,
           int32 V_1,
           int32 V_2,
           bool V_3,
           int8* V_4,
           int8& V_5,
           valuetype Test_initblk3.$MultiByte$size$5000 V_6)
  ldc.i4.1
  stloc.3

//*** "plain" initblk ***
  //test1: transient pointer
  ldloca.s   V_6
  ldc.i4.8
  ldc.i4     0x1388
  initblk
  ldc.i4.0
  stloc.2
  br.s       BL_0014

  BL_0010:  ldloc.2
  ldc.i4.1
  add
  stloc.2
  BL_0014:  ldloc.2
  ldc.i4     0x1388
  bge.s      BL_0028

  ldloca.s   V_6
  ldloc.2
  add
  ldind.i1
  ldc.i4.8
  beq.s      BL_0026

  ldc.i4.0
  stloc.3
  BL_0026:  br.s       BL_0010

  //test2: unmanaged pointer
  BL_0028:  ldloca.s   V_6
  stloc.s    V_4
  ldloc.s    V_4
  ldc.i4.s   112
  ldc.i4     0x1388
  initblk
  ldc.i4.0
  stloc.1
  br.s       BL_003f

  BL_003b:  ldloc.1
  ldc.i4.1
  add
  stloc.1
  BL_003f:  ldloc.1
  ldc.i4     0x1388
  bge.s      BL_0053

  ldloca.s   V_6
  ldloc.1
  add
  ldind.i1
  ldc.i4.s    112
  beq.s      BL_0051

  ldc.i4.0
  stloc.3
  BL_0051:  br.s       BL_003b

  //test3: managed pointer
  BL_0053:  ldloc.s    V_4
  stloc.s    V_5
  ldloc.s    V_5
  ldc.i4.s   114
  ldc.i4     0x1388
  initblk
  ldc.i4.0
  stloc.0
  br.s       BL_006a

  BL_0066:  ldloc.0
  ldc.i4.1
  add
  stloc.0
  BL_006a:  ldloc.0
  ldc.i4     0x1388
  bge      BL_007e

  ldloca.s   V_6
  ldloc.0
  add
  ldind.i1
  ldc.i4.s   114
  beq.s      BL_007c

  ldc.i4.0
  stloc.3
  BL_007c:  br.s       BL_0066

//*** volatile. initblk ***
  //test1: transient pointer
  ldloca.s   V_6
  ldc.i4.8
  ldc.i4     0x1388
  volatile. initblk
  ldc.i4.0
  stloc.2
  br.s       BL_0114

  BL_0110:  ldloc.2
  ldc.i4.1
  add
  stloc.2
  BL_0114:  ldloc.2
  ldc.i4     0x1388
  bge.s      BL_0128

  ldloca.s   V_6
  ldloc.2
  add
  ldind.i1
  ldc.i4.8
  beq.s      BL_0126

  ldc.i4.0
  stloc.3
  BL_0126:  br.s       BL_0110

  //test2: unmanaged pointer
  BL_0128:  ldloca.s   V_6
  stloc.s    V_4
  ldloc.s    V_4
  ldc.i4.s   112
  ldc.i4     0x1388
  volatile. initblk
  ldc.i4.0
  stloc.1
  br.s       BL_013f

  BL_013b:  ldloc.1
  ldc.i4.1
  add
  stloc.1
  BL_013f:  ldloc.1
  ldc.i4     0x1388
  bge.s      BL_0153

  ldloca.s   V_6
  ldloc.1
  add
  ldind.i1
  ldc.i4.s    112
  beq.s      BL_0151

  ldc.i4.0
  stloc.3
  BL_0151:  br.s       BL_013b

  //test3: managed pointer
  BL_0153:  ldloc.s    V_4
  stloc.s    V_5
  ldloc.s    V_5
  ldc.i4.s   114
  ldc.i4     0x1388
  volatile. initblk
  ldc.i4.0
  stloc.0
  br.s       BL_016a

  BL_0166:  ldloc.0
  ldc.i4.1
  add
  stloc.0
  BL_016a:  ldloc.0
  ldc.i4     0x1388
  bge      BL_007e

  ldloca.s   V_6
  ldloc.0
  add
  ldind.i1
  ldc.i4.s   114
  beq.s      BL_017c

  ldc.i4.0
  stloc.3
  BL_017c:  br.s       BL_0166

  //*** unaligned. 0x1 initblk ***
  //test1: transient pointer
  ldloca.s   V_6
  ldc.i4.8
  ldc.i4     0x1388
  unaligned. 0x1
  initblk
  ldc.i4.0
  stloc.2
  br.s       BL_0214

  BL_0210:  ldloc.2
  ldc.i4.1
  add
  stloc.2
  BL_0214:  ldloc.2
  ldc.i4     0x1388
  bge.s      BL_0228

  ldloca.s   V_6
  ldloc.2
  add
  ldind.i1
  ldc.i4.8
  beq.s      BL_0226

  ldc.i4.0
  stloc.3
  BL_0226:  br.s       BL_0210

  //test2: unmanaged pointer
  BL_0228:  ldloca.s   V_6
  stloc.s    V_4
  ldloc.s    V_4
  ldc.i4.s   112
  ldc.i4     0x1388
  unaligned. 0x1
  initblk
  ldc.i4.0
  stloc.1
  br.s       BL_023f

  BL_023b:  ldloc.1
  ldc.i4.1
  add
  stloc.1
  BL_023f:  ldloc.1
  ldc.i4     0x1388
  bge.s      BL_0253

  ldloca.s   V_6
  ldloc.1
  add
  ldind.i1
  ldc.i4.s    112
  beq.s      BL_0251

  ldc.i4.0
  stloc.3
  BL_0251:  br.s       BL_023b

  //test3: managed pointer
  BL_0253:  ldloc.s    V_4
  stloc.s    V_5
  ldloc.s    V_5
  ldc.i4.s   114
  ldc.i4     0x1388
  unaligned. 0x1
  initblk
  ldc.i4.0
  stloc.0
  br.s       BL_026a

  BL_0266:  ldloc.0
  ldc.i4.1
  add
  stloc.0
  BL_026a:  ldloc.0
  ldc.i4     0x1388
  bge      BL_007e

  ldloca.s   V_6
  ldloc.0
  add
  ldind.i1
  ldc.i4.s   114
  beq.s      BL_027c

  ldc.i4.0
  stloc.3
  BL_027c:  br.s       BL_0266

  //*** unaligned. 0x2 initblk ***
  //test1: transient pointer
  ldloca.s   V_6
  ldc.i4.8
  ldc.i4     0x1388
  unaligned. 0x2
  initblk
  ldc.i4.0
  stloc.2
  br.s       BL_0314

  BL_0310:  ldloc.2
  ldc.i4.1
  add
  stloc.2
  BL_0314:  ldloc.2
  ldc.i4     0x1388
  bge.s      BL_0328

  ldloca.s   V_6
  ldloc.2
  add
  ldind.i1
  ldc.i4.8
  beq.s      BL_0326

  ldc.i4.0
  stloc.3
  BL_0326:  br.s       BL_0310

  //test2: unmanaged pointer
  BL_0328:  ldloca.s   V_6
  stloc.s    V_4
  ldloc.s    V_4
  ldc.i4.s   112
  ldc.i4     0x1388
  unaligned. 0x2
  initblk
  ldc.i4.0
  stloc.1
  br.s       BL_033f

  BL_033b:  ldloc.1
  ldc.i4.1
  add
  stloc.1
  BL_033f:  ldloc.1
  ldc.i4     0x1388
  bge.s      BL_0353

  ldloca.s   V_6
  ldloc.1
  add
  ldind.i1
  ldc.i4.s    112
  beq.s      BL_0351

  ldc.i4.0
  stloc.3
  BL_0351:  br.s       BL_033b

  //test3: managed pointer
  BL_0353:  ldloc.s    V_4
  stloc.s    V_5
  ldloc.s    V_5
  ldc.i4.s   114
  ldc.i4     0x1388
  unaligned. 0x2
  initblk
  ldc.i4.0
  stloc.0
  br.s       BL_036a

  BL_0366:  ldloc.0
  ldc.i4.1
  add
  stloc.0
  BL_036a:  ldloc.0
  ldc.i4     0x1388
  bge      BL_007e

  ldloca.s   V_6
  ldloc.0
  add
  ldind.i1
  ldc.i4.s   114
  beq.s      BL_037c

  ldc.i4.0
  stloc.3
  BL_037c:  br.s       BL_0366

  //*** unaligned. 0x1 volatile. initblk ***
  //test1: transient pointer
  ldloca.s   V_6
  ldc.i4.8
  ldc.i4     0x1388
  unaligned. 0x1
  volatile. initblk
  ldc.i4.0
  stloc.2
  br.s       BL_0414

  BL_0410:  ldloc.2
  ldc.i4.1
  add
  stloc.2
  BL_0414:  ldloc.2
  ldc.i4     0x1388
  bge.s      BL_0428

  ldloca.s   V_6
  ldloc.2
  add
  ldind.i1
  ldc.i4.8
  beq.s      BL_0426

  ldc.i4.0
  stloc.3
  BL_0426:  br.s       BL_0410

  //test2: unmanaged pointer
  BL_0428:  ldloca.s   V_6
  stloc.s    V_4
  ldloc.s    V_4
  ldc.i4.s   112
  ldc.i4     0x1388
  unaligned. 0x1
  volatile. initblk
  ldc.i4.0
  stloc.1
  br.s       BL_043f

  BL_043b:  ldloc.1
  ldc.i4.1
  add
  stloc.1
  BL_043f:  ldloc.1
  ldc.i4     0x1388
  bge.s      BL_0453

  ldloca.s   V_6
  ldloc.1
  add
  ldind.i1
  ldc.i4.s    112
  beq.s      BL_0451

  ldc.i4.0
  stloc.3
  BL_0451:  br.s       BL_043b

  //test3: managed pointer
  BL_0453:  ldloc.s    V_4
  stloc.s    V_5
  ldloc.s    V_5
  ldc.i4.s   114
  ldc.i4     0x1388
  unaligned. 0x1
  volatile. initblk
  ldc.i4.0
  stloc.0
  br.s       BL_046a

  BL_0466:  ldloc.0
  ldc.i4.1
  add
  stloc.0
  BL_046a:  ldloc.0
  ldc.i4     0x1388
  bge      BL_007e

  ldloca.s   V_6
  ldloc.0
  add
  ldind.i1
  ldc.i4.s   114
  beq.s      BL_047c

  ldc.i4.0
  stloc.3
  BL_047c:  br.s       BL_0466

  //*** volatile. unaligned. 0x2 initblk ***
  //test1: transient pointer
  ldloca.s   V_6
  ldc.i4.8
  ldc.i4     0x1388
  volatile.
  unaligned. 0x2
  initblk
  ldc.i4.0
  stloc.2
  br.s       BL_0514

  BL_0510:  ldloc.2
  ldc.i4.1
  add
  stloc.2
  BL_0514:  ldloc.2
  ldc.i4     0x1388
  bge.s      BL_0528

  ldloca.s   V_6
  ldloc.2
  add
  ldind.i1
  ldc.i4.8
  beq.s      BL_0526

  ldc.i4.0
  stloc.3
  BL_0526:  br.s       BL_0510

  //test2: unmanaged pointer
  BL_0528:  ldloca.s   V_6
  stloc.s    V_4
  ldloc.s    V_4
  ldc.i4.s   112
  ldc.i4     0x1388
  volatile.
  unaligned. 0x2
  initblk
  ldc.i4.0
  stloc.1
  br.s       BL_053f

  BL_053b:  ldloc.1
  ldc.i4.1
  add
  stloc.1
  BL_053f:  ldloc.1
  ldc.i4     0x1388
  bge.s      BL_0553

  ldloca.s   V_6
  ldloc.1
  add
  ldind.i1
  ldc.i4.s    112
  beq.s      BL_0551

  ldc.i4.0
  stloc.3
  BL_0551:  br.s       BL_053b

  //test3: managed pointer
  BL_0553:  ldloc.s    V_4
  stloc.s    V_5
  ldloc.s    V_5
  ldc.i4.s   114
  ldc.i4     0x1388
  volatile.
  unaligned. 0x2
  initblk
  ldc.i4.0
  stloc.0
  br.s       BL_056a

  BL_0566:  ldloc.0
  ldc.i4.1
  add
  stloc.0
  BL_056a:  ldloc.0
  ldc.i4     0x1388
  bge      BL_007e

  ldloca.s   V_6
  ldloc.0
  add
  ldind.i1
  ldc.i4.s   114
  beq.s      BL_057c

  ldc.i4.0
  stloc.3
  BL_057c:  br.s       BL_0566


  //print out pass or fail
  BL_007e:  ldloc.3
  brfalse.s  BL_0096

  ldstr "PASSED"
  call       void [System.Console]System.Console::WriteLine(string)
  ldc.i4.s   100
  br.s       BL_00a9

  br.s       BL_00a8

  BL_0096:  ldstr      "FAILED"
  call       void [System.Console]System.Console::WriteLine(string)
  ldc.i4.1
  br.s       BL_00a9

  BL_00a8:  ldc.i4.0
  BL_00a9:  ret
}
}
}
