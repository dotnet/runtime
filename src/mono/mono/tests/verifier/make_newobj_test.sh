#! /bin/sh

SED="sed"
if [ `which gsed 2> /dev/null` ] ; then 
	SED="gsed"
fi

TEST_NAME=$1
TEST_VALIDITY=$2
TEST_OP=$3
TEST1_TYPE=$4
TEST2_TYPE=$5

TEST_NAME=${TEST_VALIDITY}_${TEST_NAME}
TEST_FILE=${TEST_NAME}_generated.il
echo $TEST_FILE
$SED -e "s/VALIDITY/${TEST_VALIDITY}/g" -e "s/TYPE1/${TEST1_TYPE}/g" -e "s/TYPE2/${TEST2_TYPE}/g" -e "s/OPCODE/${TEST_OP}/g" > $TEST_FILE <<//EOF

// VALIDITY CIL which breaks the ECMA-335 rules. 
// this CIL should fail verification by a conforming CLI verifier.

.assembly '${TEST_NAME}_generated'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}

.assembly extern mscorlib
{
  .ver 1:0:5000:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}

.class ClassA extends [mscorlib]System.Object
{
	.method public hidebysig  specialname  rtspecialname instance default void .ctor ()  cil managed 
	{
		.maxstack 8
		ldarg.0 
		call instance void object::.ctor()
		ret 
	}

	.method public hidebysig  specialname  rtspecialname instance default void .ctor (TYPE2 tp)  cil managed 
	{
		.maxstack 8
		ldarg.0 
		call instance void object::.ctor()
		ret 
	}

	.method public hidebysig  specialname  rtspecialname instance default void .ctor (TYPE2 a1, TYPE2 a2)  cil managed 
	{
		.maxstack 8
		ldarg.0 
		call instance void object::.ctor()
		ret 
	}

	.method public instance void ctor(TYPE2 tp)
	{
		.maxstack 8
		ret
	}

	.method public static void sctor(TYPE2 tp)
	{
		.maxstack 8
		ret
	}

	.method public hidebysig  specialname  rtspecialname static default void .cctor ()  cil managed 
	{
		.maxstack 8
		ret 
	}

	.method public instance void Method1()
	{
		.maxstack 8
		ret
	}

	.method public instance void Method2(int32 arg)
	{
		.maxstack 8
		ret
	}
}

.class interface abstract InterfaceA
{
}

.class abstract AbsClass extends [mscorlib]System.Object
{
	.method public hidebysig  specialname  rtspecialname instance default void .ctor ()  cil managed 
	{
		.maxstack 8
		ldarg.0 
		call instance void object::.ctor()
		ret 
	}
}

.class sealed ValueType extends [mscorlib]System.ValueType
{
	.field private int32 v

	.method public hidebysig specialname rtspecialname instance default void .ctor ()  cil managed 
	{
		.maxstack 8
		ret 
	}
}

.method public static void Main() cil managed
{
	.entrypoint
	.maxstack 4
	.locals init (
	    TYPE1 V_0
	)
	ldloc.0
	OPCODE //VALIDITY
	pop
	ret
}
//EOF
