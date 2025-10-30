// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.







.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly ASSEMBLY_NAME// as "avg"
{
}
.assembly extern xunit.core {}
.assembly extern TestLibrary { .ver 0:0:0:0 }
// MVID: {BCA6096F-DF11-4FA3-BF16-EEDA01729535}
.namespace AvgTest_sizeof_Target_32BIT
{
  .class public auto ansi Test
         extends [mscorlib]System.Object
  {
    .method public hidebysig static int32 Main() il managed
    {
		.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
			01 00 00 00
		)
		.entrypoint
		.maxstack  1
		call       bool [TestLibrary]TestLibrary.PlatformDetection::get_Is32BitProcess()
		brtrue.s   DoTest
		ldc.i4     100
		ret

	DoTest:
		call       int32 AvgTest_sizeof_Target_32BIT::Test()
		ret
	}

    .method public hidebysig static int32 Test() il managed
    {
		.maxstack  5
		.locals (int32[0...], native int[0...], native int, native int)
		ldc.i4.4
		dup 
		dup
		dup
		sizeof native int
		ceq
		brfalse pop3
		sizeof native unsigned int
		ceq
		brfalse pop2
		sizeof [mscorlib]System.IntPtr
		ceq
		brfalse pop1
		sizeof [mscorlib]System.UIntPtr
		ceq
		brfalse pop0
		ldc.i4 0x64
		br.s return
	pop3:
		pop
	pop2:
		pop
	pop1:
		pop
	pop0:
		ldc.i4.1
	return:		
		ret
    } // end of method 'Test::Test'

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

} // end of namespace 'AvgTest_sizeof_Target_32BIT'

//*********** DISASSEMBLY COMPLETE ***********************
