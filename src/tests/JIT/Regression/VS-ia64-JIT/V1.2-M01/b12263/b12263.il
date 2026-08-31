// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib{}
.assembly 'b12263'{}
.assembly extern xunit.core {}

.class private auto ansi beforefieldinit MyTest
       extends [mscorlib]System.Object
{
  .field public int32 intI
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       14 (0xe)
    .maxstack  2
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.1
    IL_0002:  stfld      int32 MyTest::intI
    IL_0007:  ldarg.0
    IL_0008:  call       instance void [mscorlib]System.Object::.ctor()
    IL_000d:  ret
  } // end of method MyTest::.ctor

} // end of class MyTest

.class public auto ansi beforefieldinit MyClass
       extends [mscorlib]System.Object
{
  .field private class MyTest tc
  .method public static int32 RealMain()
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
	.entrypoint
	.try
	{
		call void MyClass::Main()
		leave FAIL
	}
	catch [mscorlib]System.NullReferenceException
	{
		leave PASS
	}
    FAIL:
       ldc.i4.1
	ret
    PASS:
	ldc.i4 100
	ret  
  }

  .method public hidebysig static void Main() cil managed
  {
    .locals init (int32 V_0,
             class MyClass V_1,
             int32 V_2,
             class [mscorlib]System.NullReferenceException V_3,
             int32 V_4)

    IL_0000:  ldc.i4.1
    IL_0001:  stloc.0
    IL_0002:  newobj     instance void MyClass::.ctor()
    IL_0007:  stloc.1

      IL_0008:  ldloc.1
      IL_0009:  ldfld      class MyTest MyClass::tc
      IL_000e:  ldfld      int32 MyTest::intI	// <-- NullRef Exception should be thrown here
	        pop
    
    IL_0022:  ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method MyClass::.ctor

}
