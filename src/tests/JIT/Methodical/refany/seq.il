// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.





.assembly extern mscorlib { }
.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ASSEMBLY_NAME// as "native"
{
}
.assembly extern xunit.core {}
.assembly extern Microsoft.DotNet.XUnitExtensions { .publickeytoken = (31 BF 38 56 AD 36 4E 35 ) }
.assembly extern TestLibrary {}
.namespace JitTest_seq_refany_il
{
  .class public auto ansi Test extends [mscorlib]System.Object
  {
  	.field private static unsigned int64 m_fld
    .method private hidebysig static vararg native unsigned int TestFunc(unsigned int64 FIXEDARG) il managed
    {
		.locals(value class [mscorlib]System.ArgIterator ARGS, unsigned int64 LOC, unsigned int64[] LOCARRAY)
		ldc.i4.0
		conv.i8
		stloc LOC
		ldloca LOC
		mkrefany unsigned int64
			ldloc.1 conv.i brnull D7
		refanyval unsigned int64
		mkrefany unsigned int64
			ldloc.1 conv.i brnull D6
		refanyval unsigned int64
		mkrefany unsigned int64
			ldloc.1 conv.i brnull D4
	D1:
		refanyval unsigned int64
	R1:
		mkrefany unsigned int64
			ldloc.1 conv.i brnull D2
		refanyval unsigned int64
			dup ldind.i8 conv.i brtrue R3
		mkrefany unsigned int64
		refanyval unsigned int64
			dup ldind.i8 conv.i brtrue R2
		mkrefany unsigned int64
	D3:
		refanyval unsigned int64
			dup ldind.i8 conv.i brtrue R3
		mkrefany unsigned int64
		refanyval unsigned int64
	R2:
		mkrefany unsigned int64
			ldloc.1 conv.i brnull D3
		refanyval unsigned int64
			dup ldind.i8 conv.i brtrue R4
	R3:
		mkrefany unsigned int64
		refanyval unsigned int64
		mkrefany unsigned int64
		refanyval unsigned int64
	R4:
		mkrefany unsigned int64
			ldloc.1 conv.i brnull D1
		refanyval unsigned int64
		mkrefany unsigned int64
	D2:
		refanyval unsigned int64
		mkrefany unsigned int64
			ldloc.1 conv.i brnull D2
		refanyval unsigned int64
			dup ldind.i8 conv.i brtrue R1
		mkrefany unsigned int64
	D4:
		refanyval unsigned int64
		mkrefany unsigned int64
			ldloc.1 conv.i brnull D3
	D5:
		refanyval unsigned int64
			dup ldind.i8 conv.i brtrue R5
		mkrefany unsigned int64
		refanyval unsigned int64
			dup ldind.i8 conv.i brtrue R6
		mkrefany unsigned int64
	D6:
		refanyval unsigned int64
	R5:
		mkrefany unsigned int64
		refanyval unsigned int64
			dup ldind.i8 conv.i brtrue R5
		mkrefany unsigned int64
		refanyval unsigned int64
		mkrefany unsigned int64
	D7:
		refanyval unsigned int64
		mkrefany unsigned int64
		refanyval unsigned int64
	R6:
		mkrefany unsigned int64
		refanyval unsigned int64
		mkrefany unsigned int64
		refanyval unsigned int64
		mkrefany unsigned int64
		refanyval unsigned int64
		ldind.i8
		conv.i4
		ldc.i4 100
		or
    	ret
    }

    .method public hidebysig static int32 Main() il managed
    {
        // [ConditionalFact(typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.IsVarArgSupported))]
        .custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ConditionalFactAttribute::.ctor(class [System.Runtime]System.Type,
                                                                                                      string[]) = ( 01 00 61 54 65 73 74 4C 69 62 72 61 72 79 2E 50   // ..aTestLibrary.P
                                                                                                                    6C 61 74 66 6F 72 6D 44 65 74 65 63 74 69 6F 6E   // latformDetection
                                                                                                                    2C 20 54 65 73 74 4C 69 62 72 61 72 79 2C 20 56   // , TestLibrary, V
                                                                                                                    65 72 73 69 6F 6E 3D 30 2E 30 2E 30 2E 30 2C 20   // ersion=0.0.0.0, 
                                                                                                                    43 75 6C 74 75 72 65 3D 6E 65 75 74 72 61 6C 2C   // Culture=neutral,
                                                                                                                    20 50 75 62 6C 69 63 4B 65 79 54 6F 6B 65 6E 3D   //  PublicKeyToken=
                                                                                                                    6E 75 6C 6C 01 00 00 00 11 49 73 56 61 72 41 72   // null.....IsVarAr
                                                                                                                    67 53 75 70 70 6F 72 74 65 64 00 00 )             // gSupported..
		.entrypoint
		.maxstack  8
		.locals (unsigned int64)
		ldc.i4.0
		conv.i8
		dup
		tail. call vararg native unsigned int JitTest_seq_refany_il.Test::TestFunc(unsigned int64, ..., unsigned int64)
		ret
    } // end of method Test::Main

    .method public hidebysig specialname rtspecialname instance void .ctor() il managed
    {
		.maxstack  8
		ldarg.0
		call       instance void [mscorlib]System.Object::.ctor()
		ret
    } // end of method Test::.ctor

  } // end of class Test

} // end of namespace JitTest_seq_refany_il

//*********** DISASSEMBLY COMPLETE ***********************
