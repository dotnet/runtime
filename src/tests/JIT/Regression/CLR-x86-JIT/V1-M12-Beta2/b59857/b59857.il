// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b59857' {}


//=================================================

// Generated Method Number 20907
// Check the type soundness of assigning into local variables
// The verifier should REJECT this method
//-test_20907_stloc_ByReftoI4_over_ByReftoI2
.class public C20907 {
.method public static void test_20907_stloc_ByReftoI4_over_ByReftoI2(class C,int32, int32 &) {
// Local 0 - an initialized int32 (not used in all tests)
// Local 1 - an initialized int64 (not used in all tests)
// Local 2 - an initially uninitialized ['mscorlib']System.DateTime (not used in all tests)
// Local 3 - an uninitialized Object handle (not used in all tests)
// Local local_int8 - an initialized int8 (not used in all tests)
// Local 5 - used to test stloc
.maxstack 20
.locals(int32,int64,value class ['mscorlib']System.DateTime,class ['mscorlib']System.Object,int8 local_int8,int16 &)
  ldc.i4 0 //initialize local 0 (not used in all tests)
  stloc 0
  ldc.i8 0 //initialize local 1 (not used in all tests)
  stloc 1
  ldc.i4 0 //initialize local 4 (not used in all tests)
  stloc local_int8
 ldstr "running test_20907_stloc_ByReftoI4_over_ByReftoI2\n"
call       void [System.Console]System.Console::WriteLine(class System.String)

  // Generate a byref to an I4 on the stack

  // by taking the address of an initialized local.

  ldloca 0

  // Now try to overwrite local 5 with this value
  // The verifier should reject this, because the base types are
  // incompatible, even after any implicit conversions on the store.
  stloc 5
    ret
}
}

.class public auto ansi C  extends ['mscorlib']System.Object {
  .field public static value class ['mscorlib']System.DateTime static_unboxed_dt
  .field public static class ['mscorlib']System.DateTime static_boxed_dt
  .field public static int32 i4
  .field public static class ['mscorlib']System.Object obj
  .field public static class ['mscorlib']System.IComparable obj2
   .method  public specialname rtspecialname instance void .ctor() { ldarg 0
 call instance void ['mscorlib']System.Object::.ctor()
 ret
 } 
  .method public static int32 main() {
  .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
      01 00 00 00
  )
  .entrypoint
  .maxstack 10

ldnull
ldc.i4 10
ldsflda int32 C::i4
call void C20907::test_20907_stloc_ByReftoI4_over_ByReftoI2(class C,int32, int32 &)
ldc.i4 100
ret
}
}
