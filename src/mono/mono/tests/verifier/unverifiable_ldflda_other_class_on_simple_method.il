
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
	.field static initonly valuetype MyValueType x

}

.class public BadClass
{

.method void ordinaryName ()
{
    .maxstack 8
    .locals (
    	GoodClass V_1
   	)
   	
   	ldloc.0
	ldflda valuetype MyValueType GoodClass::x
	pop
	ret 
}

.method public hidebysig specialname rtspecialname instance default void .ctor () cil managed
{
	.maxstack 8
	ldarg.0 
	call instance void object::.ctor()
	ret
}


}

.method public static void Main() cil managed
{
	.maxstack 2
	.entrypoint
	newobj instance void BadClass::.ctor()
	call instance void BadClass::ordinaryName()
	ret
}



