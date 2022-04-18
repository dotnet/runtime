// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.







.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'i_flood'// as "avg"
{
}
.assembly extern xunit.core {}
// MVID: {BCA6096F-DF11-4FA3-BF16-EEDA01729535}
.namespace AvgTest_i_flood {
  .class public auto ansi Test extends [mscorlib]System.Object {
    .method public hidebysig static int32 Main() il managed {
	.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
	    01 00 00 00
	)
	.entrypoint
	.maxstack  8
	.locals(native int, native int)
	ldc.i4.0
	stloc.0
	ldloc.0
	ldloc.0
	ldloc.0
	ldloc.0
	ldloc.0
	ldloc.0
	ldloc.0
	ldloc.0
	br8: brtrue pop7
	br7: brfalse pop6
	br6: brfalse pop5
	br5: bge pop3
	br3: brtrue pop2
	br2: brtrue pop1
	br1: brfalse pop0
	br0: br pop0
	pop7: brtrue br6
	pop6: brfalse br5
	pop5: pop
	pop4: brtrue br3
	pop3: brfalse br2
	pop2: brfalse br1
	pop1: brtrue br0
	pop0: ldc.i4 0x64
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

} // end of namespace 'AvgTest_i_flood'

//*********** DISASSEMBLY COMPLETE ***********************
