
// unverifiable CIL which breaks the ECMA-335 rules. 
// this CIL should fail verification by a conforming CLI verifier.

.assembly 'unverifiable_assign_compat_9_1_generated'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}

.assembly extern mscorlib
{
  .ver 1:0:5000:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}

.class sealed MyValueType extends [mscorlib]System.ValueType
{
	.field private int32 fld
}

.class public GoodClass {
	.field initonly valuetype MyValueType x

.method public specialname rtspecialname default void .ctor ()
{
    .maxstack 8
    ldarg.0
    ldarg.0
	ldflda valuetype MyValueType GoodClass::x
	initobj MyValueType
    ldarg.0
	ldfld valuetype MyValueType GoodClass::x
	stfld valuetype MyValueType GoodClass::x
	ldarg.0 
	call instance void object::.ctor()
	ret 
}


}

.method public static void Main() cil managed
{
	.maxstack 2
	.entrypoint
	ret
}



