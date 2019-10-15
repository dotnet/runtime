// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly test
{
}
.module test.exe
// MVID: {611FA85E-B08F-4BA6-A5ED-1EFCE48EC6D9}


.file alignment 512

// Image base: 0x03580000


.class private sequential ansi sealed beforefieldinit RangeTest
       extends [mscorlib]System.ValueType
{
  .pack 1
  .size 1

.method public hidebysig instance void  Dummy() cil managed
{
  // Code size       1 (0x1)
  .maxstack  8
  IL_0000:  ret
} // end of method RangeTest::Dummy
} // end of class RangeTest


.class private auto ansi beforefieldinit Test
       extends [mscorlib]System.Object
{
.method public hidebysig specialname rtspecialname 
        instance void  .ctor() cil managed
{
  // Code size       7 (0x7)
  .maxstack  8
  IL_0000:  ldarg.0
  IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
  IL_0006:  ret
} // end of method Test::.ctor


.method private hidebysig static int32 Main() cil managed
{
  .entrypoint
  // Code size       33 (0x21)
  .maxstack  2
  .locals (valuetype RangeTest[] V_0,
  				 native uint V_1,
  				 native uint V_2)
   .try {
  IL_0000:  ldnull
  IL_0001:  stloc.0
  
  // Loop start
  				  ldc.i4.0
  				  stloc.2
  				  
  				  
  // This should give an IndexOutOfRange or NullPointer exception
  IL_0002:  ldloc.0  
  IL_0003:  ldloc.2
  IL_0004:  ldelema    RangeTest
  // IL_0009:  call       instance void RangeTest::Dummy()
  					stloc.1
  					
  // If we get here we're in trouble
  IL_000e:  ldstr      "Failed"
  IL_0013:  call       void [System.Console]System.Console::WriteLine(string)
  
  
  // Loop end
  					ldloc.2
  					ldc.i4.1
  					add
  					stloc.2
  					ldloc.2
  					ldloc.0
  					ldlen
  					blt.s      IL_0002
  					
  IL_0018:  ldc.i4.s   10
  IL_001a:  newarr     RangeTest
  IL_001f:  stloc.0
  leave.s IL_FAIL
  } catch [mscorlib]System.NullReferenceException {
  leave.s IL_NULLREF
  }
  IL_FAIL:
  ldc.i4 101
  ret
  IL_NULLREF:
  ldc.i4 100
  ret
} // end of method Test::Main


} // end of class Test
