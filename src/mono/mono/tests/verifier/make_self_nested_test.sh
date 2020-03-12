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

TEST_NAME=${TEST_VALIDITY}_${TEST_NAME}
TEST_FILE=${TEST_NAME}_generated.il
echo $TEST_FILE
$SED -e "s/VALIDITY/${TEST_VALIDITY}/g" -e "s/OPCODE/${TEST_OP}/g" -e "s/CLASS_ACCESS/${TEST_CLASS_ACCESS}/g" -e "s/MEMBER_ACCESS/${TEST_MEMBER_ACCESS}/g" > $TEST_FILE <<//EOF

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

.class Outer extends [mscorlib]System.Object
{
	.method public hidebysig specialname rtspecialname instance default void .ctor () cil managed
	{
		.maxstack 8
		ldarg.0 
		call instance void object::.ctor()
		ret
	}

	.class nested CLASS_ACCESS Inner extends [mscorlib]System.Object
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

		.method MEMBER_ACCESS int32 Target ()
		{
			.maxstack 8
			ldc.i4.0
			ret 
		}		
	}

	.method public void Method ()
	{
		.maxstack 8

		newobj instance void class Outer/Inner::.ctor()
		OPCODE // VALIDITY.
		pop
		ret
	}
}

.method public static int32 Main() cil managed
{
	.entrypoint
	.maxstack 8
	newobj instance void class Outer::.ctor()
	call instance void class Outer::Method()
	ldc.i4.0
	ret
}
//EOF
