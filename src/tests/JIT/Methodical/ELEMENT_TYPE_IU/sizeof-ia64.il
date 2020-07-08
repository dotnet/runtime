// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.







.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'avg'// as "avg"
{
}
.module 'avg.exe'
// MVID: {BCA6096F-DF11-4FA3-BF16-EEDA01729535}
.namespace AvgTest
{
  .class private auto ansi Test
         extends [mscorlib]System.Object
  {
    .method private hidebysig static int32 Main() il managed
    {
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

} // end of namespace 'AvgTest'

//*********** DISASSEMBLY COMPLETE ***********************
