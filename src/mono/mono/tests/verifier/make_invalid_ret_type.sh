#! /bin/sh

SED="sed"
if [ `which gsed 2> /dev/null` ] ; then 
	SED="gsed"
fi

TEST_NAME=$1
TEST_VALIDITY=$2
TEST_TYPE=$3


TEST_FILE=${TEST_VALIDITY}_${TEST_NAME}_generated.il
echo $TEST_FILE


$SED -e "s/VALIDITY/${TEST_VALIDITY}/g" -e "s/TYPE/${TEST_TYPE}/g" > $TEST_FILE <<//EOF

// VALIDITY CIL which breaks the ECMA-335 rules. 
// this CIL should fail verification by a conforming CLI verifier.

.assembly '${TEST_NAME}_generated'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}

.class public auto ansi beforefieldinit Driver extends [mscorlib]System.Object
{
	.method public static  hidebysig default TYPE Foo ()  cil managed 
    {
		.maxstack 8
		.locals init (TYPE V_0)
		ldloc.0 
		ret 
	}

	.method public static  hidebysig default int32 Main ()  cil managed 
	{
		.entrypoint
		.maxstack 2
		.locals init ()
		call TYPE class Driver::Foo()
		pop
		ldc.i4.0
		ret 
	}
}

//EOF
