.assembly EvenOdd { }
.class EvenOdd
{ .method private static bool IsEven(int32 N) il managed
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
    tail.
    call bool EvenOdd::IsOdd(int32)
    ret
  } // end of method `EvenOdd::IsEven'

  .method private static bool IsOdd(int32 N) il managed
  { .maxstack   2
    // Demonstrates use of argument names and labels
    // Notice that the assembler does not covert these 
    // automatically to their short versions
    ldarg       N
    ldc.i4.0
    bne.un      NonZero
    ldc.i4.0
    ret
NonZero:
    ldarg       N
    ldc.i4.1
    sub
    tail.
    call bool EvenOdd::IsEven(int32)
    ret
  } // end of method `EvenOdd::IsOdd'

  .method public static void Test(int32 N) il managed
  { .maxstack   1
    ldarg       N
    call        void [mscorlib]System.Console::Write(int32)
    ldstr       " is "
    call        void [mscorlib]System.Console::Write(class System.String)
    ldarg       N
    call        bool EvenOdd::IsEven(int32)
    brfalse     LoadOdd
    ldstr       "even"
Print:
    call        void [mscorlib]System.Console::WriteLine(class System.String)
    ret
LoadOdd:
    ldstr       "odd"
    br          Print
  } // end of method `EvenOdd::Test'  
} // end of class `EvenOdd'

//Global method

.method public static void main() il managed
{ .entrypoint
  .maxstack     1
  ldc.i4        1001
  call          void EvenOdd::Test(int32)
  ret
} // end of global method `main'





