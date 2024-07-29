// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.





.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ASSEMBLY_NAME// as "native"
{
}
.assembly extern xunit.core {}
// MVID: {C18F1061-8239-420B-AC69-D4CB0BB38F2B}
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
		.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
		    01 00 00 00
		)
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
