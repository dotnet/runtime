#! /bin/sh

TEST_NAME=$1

# Only 1 TEST_OP variable should be set.

TEST_OP1=$2
TEST_OP2=$3
TEST_OP3=$4
TEST_OP4=$5
TEST_OP5=$6

TEST_FILE=invalid_exception_branch_${TEST_NAME}_generated.cil
echo $TEST_FILE
sed -e "s/OPCODE1/${TEST_OP1}/g" -e "s/OPCODE2/${TEST_OP2}/g" -e "s/OPCODE3/${TEST_OP3}/g" -e "s/OPCODE4/${TEST_OP4}/g" -e "s/OPCODE5/${TEST_OP5}/g" > $TEST_FILE <<//EOF
// invalid CIL which breaks the ECMA-335 rules. 
// This CIL should fail verification by a conforming CLI verifier.

.assembly '${TEST_NAME}_generated'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}

.method public static int32 Main() cil managed
{
	.entrypoint
	.maxstack 2
	OPCODE1
	.try
	{
	branch_target1:
	OPCODE2
        }
        catch [mscorlib]System.Exception 
	{
	branch_target2:
	OPCODE3
        }
	.try
	{
	}
	finally
	{
	branch_target3:
	OPCODE4
	}
	.try 
	{
        }
	filter
	{
	ldc.i4.1
	endfilter
	}
	{
	branch_target4:
	OPCODE5
        }
	branch_target5:
	ldc.i4.0
	ret
}
//EOF