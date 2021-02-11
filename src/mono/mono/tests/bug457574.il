.assembly extern mscorlib
{
.ver 2:0:0:0
.publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
}

.assembly 'bug457574'
{

.hash algorithm 0x00008004
.ver  0:0:0:0
}
.module repro.exe


.class public auto ansi beforefieldinit bug457574_regression extends [mscorlib]System.Object
{

	.method public static hidebysig default bool test_01 () cil managed 
	{
	.maxstack 6
	.locals init (unsigned int32 V_0, unsigned int32 V_1)

	ldc.i4.0
	stloc.0 

	ldloc.0 
	switch (IL_0021, IL_0069)
	br IL_0075

IL_0021:
	br IL_0069

IL_0026:
	ldloc.0
	ldloc.1
	ceq
	brfalse IL_0051

	br IL_0075

IL_0051:
	ldloc.1
	stloc.0
	br IL_0077

IL_0069:
	br IL_0026

	ldc.i4.1
	stloc.1

IL_0075:
	ldc.i4.0 
	ret

IL_0077:
	ldc.i4.1 
	ret 
	}

	.method public static hidebysig default void test_02 () cil managed 
	{

		.maxstack 8
		.locals init (int32 V_0)

		ldloc.0
		switch(label_1, label_2)
		br end
	label_1:
		br label_2
	label_3:
		nop
		nop
		br end
	label_2:
		br label_3
		nop
		nop
	end:
		ret	
	}

.method public static hidebysig default void Main () cil managed 
{
	.entrypoint
	.maxstack 8

	call bool bug457574_regression::test_01 ()
	pop
	call void bug457574_regression::test_02 ()
	ret	
}

}
