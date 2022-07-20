// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//"plain", volatile., unaligned. cpblk
//transient, managed, unmanaged pointer
.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.namespace Test_cpblk3
{
.assembly 'cpblk3' {}
.class public explicit ansi sealed $MultiByte$size$34
       extends [mscorlib]System.ValueType
{
  .pack 1
  .size 34
//  .custom instance void .ctor() = ( 01 00 00 00 ) 
} // end of class Test_cpblk3.$MultiByte$size$34

.class public explicit sealed $MultiByte$size$7
       extends [mscorlib]System.ValueType
{
  .pack 1
  .size 7
//  .custom instance void .ctor() = ( 01 00 00 00 ) 
} // end of class Test_cpblk3.$MultiByte$size$7

//Global fields
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
.field static privatescope valuetype Test_cpblk3.$MultiByte$size$34 'unnamed-global-0$PST04000001' at D_00008030
.field static privatescope valuetype Test_cpblk3.$MultiByte$size$34 'unnamed-global-1$PST04000002' at D_00008054
//Global methods
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

.class public initblk2 extends [mscorlib]System.Object
{
.method public static int32
        main()
{
  .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
      01 00 00 00
  )
  .entrypoint
  // Code size       177 (0xb1)
  .maxstack  3
  .locals (bool V_0,
           string V_1,
           int8 * V_2,
           int8 & V_3,
           valuetype Test_cpblk3.$MultiByte$size$34 V_4,
           valuetype Test_cpblk3.$MultiByte$size$34 V_5,
           valuetype Test_cpblk3.$MultiByte$size$34 V_6,
           valuetype Test_cpblk3.$MultiByte$size$34 V_7)
  ldnull
  stloc.1
  ldc.i4.1
  stloc.0

  //*** "plain" cpblk ***
  //test1: transient pointer
  ldloca.s   V_4
  ldsflda    valuetype Test_cpblk3.$MultiByte$size$34 'unnamed-global-0$PST04000001'
  ldc.i4.s   34
  cpblk
  ldloc.s    V_4
  stloc.s    V_7
  ldsflda    valuetype Test_cpblk3.$MultiByte$size$34 'unnamed-global-1$PST04000002'
  newobj     instance void string::.ctor(int8*)
  stloc.1
  ldloc.1
  ldloca.s   V_7
  newobj     instance void string::.ctor(int8*)
  call       int32 string::Compare(string,
                                                              string)
  brfalse.s  BL_003b

  ldloca.s   V_7
  newobj     instance void string::.ctor(int8*)
  call       void [System.Console]System.Console::WriteLine(string)
  ldc.i4.0
  stloc.0
  BL_003b:  ldloca.s   V_6
  stloc.3

  //test2: managed pointer
  ldloc.3
  ldloca.s   V_4
  ldc.i4.s   34
  cpblk
  ldloc.1
  ldloc.3
  newobj     instance void string::.ctor(int8*)
  call       int32 string::Compare(string,
                                                              string)
  brfalse.s  BL_0060

  ldloc.3
  newobj     instance void string::.ctor(int8*)
  call       void [System.Console]System.Console::WriteLine(string)
  ldc.i4.0
  stloc.0
  BL_0060:  ldloca.s   V_5
  stloc.2

  //test3: unmanaged pointer
  ldloc.2
  ldloca.s   V_4
  ldc.i4.s   34
  cpblk
  ldloc.1
  ldloc.2
  newobj     instance void string::.ctor(int8*)
  call       int32 string::Compare(string,
                                                              string)
  brfalse.s  BL_0085

  ldloc.2
  newobj     instance void string::.ctor(int8*)
  call       void [System.Console]System.Console::WriteLine(string)
  ldc.i4.0
  stloc.0
  BL_0085:  ldloc.0
  brfalse  BL_009c

  //*** volatile. cpblk ***
  //test1: transient pointer
  ldloca.s   V_4
  ldsflda    valuetype Test_cpblk3.$MultiByte$size$34 'unnamed-global-0$PST04000001'
  ldc.i4.s   34
  volatile. cpblk
  ldloc.s    V_4
  stloc.s    V_7
  ldsflda    valuetype Test_cpblk3.$MultiByte$size$34 'unnamed-global-1$PST04000002'
  newobj     instance void string::.ctor(int8*)
  stloc.1
  ldloc.1
  ldloca.s   V_7
  newobj     instance void string::.ctor(int8*)
  call       int32 string::Compare(string,
                                                              string)
  brfalse.s  BL_013b

  ldloca.s   V_7
  newobj     instance void string::.ctor(int8*)
  call       void [System.Console]System.Console::WriteLine(string)
  ldc.i4.0
  stloc.0
  BL_013b:  ldloca.s   V_6
  stloc.3

  //test2: managed pointer
  ldloc.3
  ldloca.s   V_4
  ldc.i4.s   34
  volatile. cpblk
  ldloc.1
  ldloc.3
  newobj     instance void string::.ctor(int8*)
  call       int32 string::Compare(string,
                                                              string)
  brfalse.s  BL_0160

  ldloc.3
  newobj     instance void string::.ctor(int8*)
  call       void [System.Console]System.Console::WriteLine(string)
  ldc.i4.0
  stloc.0
  BL_0160:  ldloca.s   V_5
  stloc.2

  //test3: unmanaged pointer
  ldloc.2
  ldloca.s   V_4
  ldc.i4.s   34
  volatile. cpblk
  ldloc.1
  ldloc.2
  newobj     instance void string::.ctor(int8*)
  call       int32 string::Compare(string,
                                                              string)
  brfalse.s  BL_0185

  ldloc.2
  newobj     instance void string::.ctor(int8*)
  call       void [System.Console]System.Console::WriteLine(string)
  ldc.i4.0
  stloc.0
  BL_0185:  ldloc.0
  brfalse  BL_009c

  //*** unaligned. 0x1 volatile. cpblk ***
  //test1: transient pointer
  ldloca.s   V_4
  ldsflda    valuetype Test_cpblk3.$MultiByte$size$34 'unnamed-global-0$PST04000001'
  ldc.i4.s   34
  unaligned. 0x1
  volatile. cpblk
  ldloc.s    V_4
  stloc.s    V_7
  ldsflda    valuetype Test_cpblk3.$MultiByte$size$34 'unnamed-global-1$PST04000002'
  newobj     instance void string::.ctor(int8*)
  stloc.1
  ldloc.1
  ldloca.s   V_7
  newobj     instance void string::.ctor(int8*)
  call       int32 string::Compare(string,
                                                              string)
  brfalse.s  BL_023b

  ldloca.s   V_7
  newobj     instance void string::.ctor(int8*)
  call       void [System.Console]System.Console::WriteLine(string)
  ldc.i4.0
  stloc.0
  BL_023b:  ldloca.s   V_6
  stloc.3

  //test2: managed pointer
  ldloc.3
  ldloca.s   V_4
  ldc.i4.s   34
  unaligned. 0x1
  volatile. cpblk
  ldloc.1
  ldloc.3
  newobj     instance void string::.ctor(int8*)
  call       int32 string::Compare(string,
                                                              string)
  brfalse.s  BL_0260

  ldloc.3
  newobj     instance void string::.ctor(int8*)
  call       void [System.Console]System.Console::WriteLine(string)
  ldc.i4.0
  stloc.0
  BL_0260:  ldloca.s   V_5
  stloc.2

  //test3: unmanaged pointer
  ldloc.2
  ldloca.s   V_4
  ldc.i4.s   34
  unaligned. 0x1
  volatile. cpblk
  ldloc.1
  ldloc.2
  newobj     instance void string::.ctor(int8*)
  call       int32 string::Compare(string,
                                                              string)
  brfalse.s  BL_0285

  ldloc.2
  newobj     instance void string::.ctor(int8*)
  call       void [System.Console]System.Console::WriteLine(string)
  ldc.i4.0
  stloc.0
  BL_0285:  ldloc.0
  brfalse  BL_009c

  //*** unaligned. 0x2 volatile. cpblk ***
  //test1: transient pointer
  ldloca.s   V_4
  ldsflda    valuetype Test_cpblk3.$MultiByte$size$34 'unnamed-global-0$PST04000001'
  ldc.i4.s   34
  unaligned. 0x2
  volatile. cpblk
  ldloc.s    V_4
  stloc.s    V_7
  ldsflda    valuetype Test_cpblk3.$MultiByte$size$34 'unnamed-global-1$PST04000002'
  newobj     instance void string::.ctor(int8*)
  stloc.1
  ldloc.1
  ldloca.s   V_7
  newobj     instance void string::.ctor(int8*)
  call       int32 string::Compare(string,
                                                              string)
  brfalse.s  BL_033b

  ldloca.s   V_7
  newobj     instance void string::.ctor(int8*)
  call       void [System.Console]System.Console::WriteLine(string)
  ldc.i4.0
  stloc.0
  BL_033b:  ldloca.s   V_6
  stloc.3

  //test2: managed pointer
  ldloc.3
  ldloca.s   V_4
  ldc.i4.s   34
  unaligned. 0x2
  volatile. cpblk
  ldloc.1
  ldloc.3
  newobj     instance void string::.ctor(int8*)
  call       int32 string::Compare(string,
                                                              string)
  brfalse.s  BL_0360

  ldloc.3
  newobj     instance void string::.ctor(int8*)
  call       void [System.Console]System.Console::WriteLine(string)
  ldc.i4.0
  stloc.0
  BL_0360:  ldloca.s   V_5
  stloc.2

  //test3: unmanaged pointer
  ldloc.2
  ldloca.s   V_4
  ldc.i4.s   34
  unaligned. 0x2
  volatile. cpblk
  ldloc.1
  ldloc.2
  newobj     instance void string::.ctor(int8*)
  call       int32 string::Compare(string,
                                                              string)
  brfalse.s  BL_0385

  ldloc.2
  newobj     instance void string::.ctor(int8*)
  call       void [System.Console]System.Console::WriteLine(string)
  ldc.i4.0
  stloc.0
  BL_0385:  ldloc.0
  brfalse  BL_009c

  ldstr "PASSED"
  call       void [System.Console]System.Console::WriteLine(string)
  ldc.i4.s   100
  br.s       BL_00b0

  br.s       BL_00af

  BL_009c:  ldstr "FAILED"
  call       void [System.Console]System.Console::WriteLine(string)
  ldc.i4.s   1
  br.s       BL_00b0

  BL_00af:  ldc.i4.0
  BL_00b0:  ret
} // end of global method main
}

.data D_00008030 = bytearray (
                 57 65 6C 63 6F 6D 65 20 74 6F 20 74 68 65 20 2E  // Welcome to the .
                 4E 45 54 20 4D 61 6E 61 67 65 64 20 57 6F 72 6C  // NET Managed Worl
                 64 00 00 00)                                     // d...
.data D_00008054 = bytearray (
                 57 65 6C 63 6F 6D 65 20 74 6F 20 74 68 65 20 2E  // Welcome to the .
                 4E 45 54 20 4D 61 6E 61 67 65 64 20 57 6F 72 6C  // NET Managed Worl
                 64 00 00 00)                                     // d...
}
