#! /bin/sh

SED="sed"
if [ `which gsed 2> /dev/null` ] ; then 
	SED="gsed"
fi

TEST_NAME=$1
TEST_VALIDITY=$2

# Only 1 TEST_OP variable should be set.
TEST_OP1=$3
TEST_OP2=$4
TEST_OP3=$5
TEST_OP4=$6
TEST_OP5=$7

TEST_FILE=${TEST_VALIDITY}_exception_branch_${TEST_NAME}_generated.il
echo $TEST_FILE
$SED -e "s/OPCODE1/${TEST_OP1}/g" -e "s/OPCODE2/${TEST_OP2}/g" -e "s/OPCODE3/${TEST_OP3}/g" -e "s/OPCODE4/${TEST_OP4}/g" -e "s/OPCODE5/${TEST_OP5}/g" > $TEST_FILE <<//EOF
// ${TEST_VALIDITY} CIL which breaks the ECMA-335 rules. 
// This CIL should fail verification by a conforming CLI verifier.

.assembly '${TEST_NAME}_generated'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}

.method public static int32 Main() cil managed
{
	.entrypoint
	.maxstack 8
	.locals init (int32 V0,
		int32 V1)

	OPCODE1
	.try
	{
		nop
branch_target1:
		OPCODE2
		leave branch_target5
        }
        catch [mscorlib]System.Exception 
	{
		pop
branch_target2:
		OPCODE3
		leave branch_target5
        }
	.try
	{
		nop
		leave branch_target5
	}
	finally
	{
		nop
branch_target3:
		OPCODE4
		endfinally
	}
	.try 
	{
		nop
   		leave branch_target5
	}
	filter
	{
		pop
		ldc.i4.1
		endfilter
	}

	{
		pop
		nop
branch_target4:
		OPCODE5
		nop
		leave branch_target5
	}
	nop
	branch_target5:
	ldc.i4.0
	ret
}
//EOF
