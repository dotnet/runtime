#! /bin/sh

SED="sed"
if [ `which gsed 2> /dev/null` ] ; then 
	SED="gsed"
fi

TEST_NAME=$1
TEST_VALIDITY=$2
TEST_OP=$3
TEST_CLASS_ACCESS=$4
TEST_NESTED_ACCESS=$5
TEST_MEMBER_ACCESS=$6
TEST_BASE_EXTENDS=$7
TEST_NESTED_EXTENDS=$8
TEST_LOAD_BASE=$9


if [ "$TEST_BASE_EXTENDS" = "yes" ]; then
	TEST_BASE_EXTENDS="extends Root"
	TEST_BASE_CONSTRUCTOR="call instance void Root::.ctor()"
else
	TEST_BASE_EXTENDS="extends [mscorlib]System.Object"
	TEST_BASE_CONSTRUCTOR="call instance void object::.ctor()"
fi

if [ "$TEST_NESTED_EXTENDS" = "yes" ]; then
	TEST_NESTED_EXTENDS="extends Root\/Nested"
	TEST_NESTED_CONSTRUCTOR="call instance void Root\/Nested::.ctor()"
else
	TEST_NESTED_EXTENDS="extends [mscorlib]System.Object"
	TEST_NESTED_CONSTRUCTOR="call instance void object::.ctor()"
fi

if [ "$TEST_LOAD_BASE" = "yes" ]; then
	TEST_LOAD_REF="ldarg.0"
else
	TEST_LOAD_REF="call class Root\/Nested Root::Create ()"
fi

TEST_NAME=${TEST_VALIDITY}_${TEST_NAME}
TEST_FILE=${TEST_NAME}_generated.il
echo $TEST_FILE

$SED -e "s/CLASS_ACCESS/${TEST_CLASS_ACCESS}/g" -e "s/NESTED_ACCESS/${TEST_NESTED_ACCESS}/g" -e "s/MEMBER_ACCESS/${TEST_MEMBER_ACCESS}/g" -e "s/ROOT_EXTENDS/${TEST_BASE_EXTENDS}/g" -e "s/ROOT_CONSTRUCTOR/${TEST_BASE_CONSTRUCTOR}/g" -e "s/NESTED_EXTENDS/${TEST_NESTED_EXTENDS}/g" -e "s/NESTED_CONSTRUCTOR/${TEST_NESTED_CONSTRUCTOR}/g" -e "s/LOAD_REF/${TEST_LOAD_REF}/g" -e "s/OPCODE/${TEST_OP}/g" -e "s/VALIDITY/${TEST_VALIDITY}/g" > $TEST_FILE <<//EOF

.assembly '${TEST_NAME}_generated'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}

// VALIDITY CIL which breaks the ECMA-335 rules. 
// this CIL should fail verification by a conforming CLI verifier.

.assembly extern mscorlib
{
  .ver 1:0:5000:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}

.class CLASS_ACCESS Root extends [mscorlib]System.Object
{
	.method public static class Root/Nested Create ()
	{
		.maxstack 8
		newobj instance void class Root/Nested::.ctor()
		ret
	}

	.method public hidebysig specialname rtspecialname instance default void .ctor () cil managed
	{
		.maxstack 8
		ldarg.0 
		call instance void object::.ctor()
		ret
	}

	.class nested NESTED_ACCESS Nested extends [mscorlib]System.Object
	{
		.field MEMBER_ACCESS int32 fld
		.field MEMBER_ACCESS static int32 sfld

		.method public hidebysig specialname rtspecialname instance default void .ctor () cil managed
		{
			.maxstack 8
			ldarg.0 
			call instance void object::.ctor()
			ret
		}

		.method MEMBER_ACCESS virtual hidebysig newslot instance int32 Target ()
		{
			.maxstack 8
			ldc.i4.0
			ret 
		}		
	}
}


.class public Extension ROOT_EXTENDS
{
	.method public static void Execute ()
	{
		.maxstack 8
		newobj instance void class Extension/MyNested::.ctor()
		call instance void class Extension/MyNested::Method()
		ret
	}

	.method public hidebysig specialname rtspecialname instance default void .ctor () cil managed
	{
		.maxstack 8
		ldarg.0 
		ROOT_CONSTRUCTOR
		ret
	}

	.class nested MEMBER_ACCESS MyNested NESTED_EXTENDS
	{
		.method MEMBER_ACCESS virtual hidebysig instance int32 Target ()
		{
			.maxstack 8
			ldc.i4.0
			ret 
		}

		.method public hidebysig specialname rtspecialname instance default void .ctor () cil managed
		{
			.maxstack 8
			ldarg.0 
			NESTED_CONSTRUCTOR
			ret
		}

		.method public void Method ()
		{
			.maxstack 8
			LOAD_REF
			OPCODE // VALIDITY.
			pop
			ret
		}
	}
}

.method public static int32 Main() cil managed
{
	.entrypoint
	.maxstack 8
	call void class Extension::Execute ()
	ldc.i4.0
	ret
}

//EOF
