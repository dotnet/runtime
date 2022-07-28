// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.





.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b40725'
{


  //                                                                                  bool) = ( 01 00 00 01 00 00 ) 
  
  
}
.assembly extern xunit.core {}
// MVID: {37F05BF1-E8CC-42FA-A268-D866F07CFFF8}
.namespace TestCase
{
  .class private auto ansi Test
         extends [mscorlib]System.Object
  {
    .method private hidebysig static int32 Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      // Code size       39 (0x27)
      .maxstack  5
		ldc.i4		0xfffffff2
		ldc.i4		0xffffff00
		and
		dup
		brtrue		story_about_bad
		pop
		ldc.i4		0x0000fff2
		ldc.i4		0xffffff00
		and
		dup
		brfalse		story_about_good
story_about_bad:
		ldc.i4		0xffffff00
		ceq
story_about_good:
		pop
		ldc.i4 100
		ret
    } // end of method 'Test::Main'

    .method public hidebysig specialname rtspecialname 
            instance void .ctor() il managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method 'Test::.ctor'

  } // end of class 'Test'

} // end of namespace 'TestCase'

//*********** DISASSEMBLY COMPLETE ***********************
