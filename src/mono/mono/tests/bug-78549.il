.assembly extern mscorlib
{
	.ver 2:0:0:0
	.publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'out'
{
	.custom instance void class
[mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32)
=  (01 00 08 00 00 00 00 00 ) // ........

	.hash algorithm 0x00008004
	.ver  0:0:0:0
}
.module 'out' // GUID = {4C258A12-8B71-4F2F-BBD4-D5D836DE4A15}


.class private auto ansi abstract sealed M
extends [mscorlib]System.Object
{

	// method line 1
	.method public static  hidebysig
	default int32 Main ()  cil managed
	{
		// Method begins at RVA 0x20ec
		.entrypoint
		// Code size 99 (0x63)
		.maxstack 10
		.locals init (
			string  V_0,
			int32   V_1,
			string  V_2,
			class [mscorlib]System.NullReferenceException   V_3,
			int32   V_4)
		.try { // 0
		ldstr "output"
		stloc.0
		ldnull
		callvirt instance int32 string::get_Length()
		stloc.1
		ldstr "!!"
		stloc.2
		ldstr "this "
		call void class [mscorlib]System.Console::Write(string)
		ldloc.0
		call void class [mscorlib]System.Console::Write(string)
		ldstr " will be "
		call void class [mscorlib]System.Console::Write(string)
		ldloc.1
		call void class [mscorlib]System.Console::Write(int32)
		ldstr " interrupted "
		call void class [mscorlib]System.Console::Write(string)
		ldloc.2
		call void class [mscorlib]System.Console::Write(string)
		ldstr "\n"
		call void class [mscorlib]System.Console::Write(string)
		ldc.i4 1
		stloc 4
		leave END

		} // end .try 0
		catch [mscorlib]System.NullReferenceException { // 0
		stloc.3
		ldstr "ok-caught\n"
		call void class [mscorlib]System.Console::Write(string)
		ldc.i4 0
		stloc 4
		leave END
		} // end handler 0
		END:  ldloc 4
		ret
	} // end of method M::default void Main ()

} // end of class M


