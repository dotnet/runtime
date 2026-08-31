// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b02043
{
}
.class public EvenOdd
{ .method static bool IsEven(int32 N)
  { .maxstack   2
    ldarg.0             // N
    ldc.i4.0
    bne.un      NonZero
    ldc.i4.1
    ret
NonZero:
    ldarg.0
    ldc.i4.1
    sub
    tail. call  bool EvenOdd::IsOdd(int32)
    ret
  } // end of method 'EvenOdd::IsEven'

  .method static bool IsOdd(int32 N)
  { .maxstack   2
    ldarg       N
    ldc.i4.0
    bne.un      NonZero
    ldc.i4.0
    ret
NonZero:
    ldarg       N
    ldc.i4.1
    sub
    tail. call  bool EvenOdd::IsEven(int32)
    ret
  } // end of method 'EvenOdd::IsOdd'

  .method static void Test(int32 N)
  { .maxstack   1
    ldarg       N
    call        void [System.Console]System.Console::Write(int32)
    ldstr       " is "
    call        void [System.Console]System.Console::Write(class [mscorlib]System.String)
    ldarg       N
    call        bool EvenOdd::IsEven(int32)
    brfalse     LoadOdd
    ldstr       "even"
Print:
    tail. call  void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
    ret
LoadOdd:
    ldstr       "odd"
    br          Print
  } // end of method 'EvenOdd::Test'  

//Global method

.method public static int32 main()
{ .entrypoint
  .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
      01 00 00 00
  )
  .maxstack     1
  ldc.i4.5
  call          void EvenOdd::Test(int32)
  ldc.i4.2
  call          void EvenOdd::Test(int32)
  ldc.i4        100
  call          void EvenOdd::Test(int32)
  ldc.i4        10001
  call          void EvenOdd::Test(int32)
ldc.i4 100
  ret
} // end of global method 'main'
} // end of class 'EvenOdd'
