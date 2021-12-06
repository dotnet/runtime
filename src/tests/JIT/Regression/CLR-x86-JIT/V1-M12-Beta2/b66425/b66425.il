// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly test_21_try {}
.assembly extern xunit.core {}

.class TestClass{

//+desc: leave from inner try{} to outside
.method public static void test_21_try(){
.locals(int32 locInt1)

//init locals
  ldc.i4.1
  stloc locInt1


// print a message
  ldstr      "Test is running"
  call       void [System.Console]System.Console::WriteLine(class System.String)  
//exception handler goes here
lbl_21_startTry:
	lbl_21_startTry1:

	  leave lbl_21_exitSEH	
	lbl_21_endTry1:
	lbl_21_startCatch1:

	  leave lbl_21_exitSEH1
	lbl_21_endCatch1:
	lbl_21_exitSEH1:

  leave lbl_21_exitSEH	
lbl_21_endTry:
lbl_21_startCatch:

  leave lbl_21_exitSEH
lbl_21_endCatch:
lbl_21_exitSEH:

.try   lbl_21_startTry1 to  lbl_21_endTry1 catch ['mscorlib']System.Exception handler lbl_21_startCatch1 to  lbl_21_endCatch1
.try   lbl_21_startTry to  lbl_21_endTry catch ['mscorlib']System.Exception handler lbl_21_startCatch to  lbl_21_endCatch

  ret


}




.method public static int32 Main(class ['mscorlib']System.String[] arg)
{
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    call void TestClass::test_21_try()
    .entrypoint
    ldc.i4 100
    ret
}


  .method public specialname rtspecialname instance void .ctor() il managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void ['mscorlib']System.Object::.ctor()
    IL_0006:  ret
  } // end of method 'TestClass::.ctor'

}
