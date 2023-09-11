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
.assembly extern Microsoft.DotNet.XUnitExtensions { .publickeytoken = (31 BF 38 56 AD 36 4E 35 ) }
.assembly extern TestLibrary {}
// MVID: {BCA6096F-DF11-4FA3-BF16-EEDA01729535}
.namespace AvgTest_sizeof_Target_64BIT
{
  .class public auto ansi Test
         extends [mscorlib]System.Object
  {
    .method public hidebysig static int32 Main() il managed
    {
        // [ConditionalFact(typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.Is64BitProcess))]
        .custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ConditionalFactAttribute::.ctor(class [System.Runtime]System.Type,
                                                                                                      string[]) = ( 01 00 61 54 65 73 74 4C 69 62 72 61 72 79 2E 50   // ..aTestLibrary.P
                                                                                                                    6C 61 74 66 6F 72 6D 44 65 74 65 63 74 69 6F 6E   // latformDetection
                                                                                                                    2C 20 54 65 73 74 4C 69 62 72 61 72 79 2C 20 56   // , TestLibrary, V
                                                                                                                    65 72 73 69 6F 6E 3D 30 2E 30 2E 30 2E 30 2C 20   // ersion=0.0.0.0, 
                                                                                                                    43 75 6C 74 75 72 65 3D 6E 65 75 74 72 61 6C 2C   // Culture=neutral,
                                                                                                                    20 50 75 62 6C 69 63 4B 65 79 54 6F 6B 65 6E 3D   //  PublicKeyToken=
                                                                                                                    6E 75 6C 6C 01 00 00 00 0E 49 73 36 34 42 69 74   // null.....Is64Bit
                                                                                                                    50 72 6F 63 65 73 73 00 00 )                      // Process..
		.entrypoint
		// Code size       48 (0x30)
		.maxstack  5
		ldc.i4.8
		ldc.i4.8
		ldc.i4.8
		ldc.i4.8
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

} // end of namespace 'AvgTest_sizeof_Target_64BIT'

//*********** DISASSEMBLY COMPLETE ***********************
