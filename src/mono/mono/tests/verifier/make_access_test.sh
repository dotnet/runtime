#! /bin/sh

SED="sed"
if [ `which gsed 2> /dev/null` ] ; then 
	SED="gsed"
fi

TEST_NAME=$1
TEST_VALIDITY=$2
TEST_OP=$3
TEST_CLASS_ACCESS=$4
TEST_MEMBER_ACCESS=$5
TEST_EXTENDS=$6
TEST_USE_SUB_CLASS=$7

if [ "$TEST_EXTENDS" = "yes" ]; then
	TEST_EXTENDS="extends Class"
	TEST_SUPER_TYPE="Class"
else
	TEST_EXTENDS="extends [mscorlib]System.Object"
	TEST_SUPER_TYPE="object"
fi

if [ "$TEST_USE_SUB_CLASS" = "yes" ]; then
	TEST_VAR_TYPE="ExampleClass"
else
	TEST_VAR_TYPE="Class"
fi

TEST_NAME=${TEST_VALIDITY}_${TEST_NAME}
TEST_FILE=${TEST_NAME}_generated.il
echo $TEST_FILE
$SED -e "s/SUPER_TYPE/${TEST_SUPER_TYPE}/g" -e "s/VALIDITY/${TEST_VALIDITY}/g" -e "s/OPCODE/${TEST_OP}/g" -e "s/CLASS_ACCESS/${TEST_CLASS_ACCESS}/g" -e "s/VAR_TYPE/${TEST_VAR_TYPE}/g" -e "s/MEMBER_ACCESS/${TEST_MEMBER_ACCESS}/g" -e "s/EXTENDS/${TEST_EXTENDS}/g" > $TEST_FILE <<//EOF

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

.class CLASS_ACCESS Class extends [mscorlib]System.Object
{
    .field MEMBER_ACCESS static int32 sfld
    .field MEMBER_ACCESS int32 fld

	.method public hidebysig specialname rtspecialname instance default void .ctor () cil managed
	{
		.maxstack 8
		ldarg.0 
		call instance void object::.ctor()
		ret
	}

    .method MEMBER_ACCESS int32 Method() {
    	ldc.i4.0
    	ret
    }
}

.class ExampleClass EXTENDS
{
	.method public hidebysig specialname rtspecialname instance default void .ctor () cil managed
	{
		.maxstack 8
		ldarg.0 
		call instance void SUPER_TYPE::.ctor()
		ret
	}

	.method public static int32 Main() cil managed
	{
		.entrypoint
		.maxstack 4
		newobj instance void class VAR_TYPE::.ctor()
		OPCODE // VALIDITY.
		pop
		ldc.i4.0
		ret
	}
}
//EOF
