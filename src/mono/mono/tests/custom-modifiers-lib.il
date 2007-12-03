.assembly extern mscorlib
{
	.ver 2:0:0:0
	.publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'custom-modifiers-lib'
{
	.custom instance void class
[mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32)
=  (01 00 08 00 00 00 00 00 ) // ........

	.hash algorithm 0x00008004
	.ver  0:0:0:0
}
.module 'custom-modifiers-lib'


.class public auto ansi abstract sealed CustomModifiers
extends [mscorlib]System.Object
{
	.field public int32 modreq([mscorlib]System.Runtime.CompilerServices.IsBoxed) field_1
	.field public int32 modopt([mscorlib]System.Runtime.CompilerServices.IsVolatile) field_2
} // end of class CustomModifiers


