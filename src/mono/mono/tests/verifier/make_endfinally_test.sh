#! /bin/sh

SED="sed"
if [ `which gsed 2> /dev/null` ] ; then 
	SED="gsed"
fi

TEST_NAME=$1
TEST_VALIDITY=$2
TEST_BLOCK=$3
TEST_POS=$4
TEST_EXTRA_OPS=$5
# Only 1 TEST_OP variable should be set.


declare OPCODE_${TEST_POS}="endfinally"


TEST_NAME=${TEST_VALIDITY}_${TEST_NAME}
TEST_FILE=${TEST_NAME}_generated.il
echo $TEST_FILE
$SED -e "s/VALIDITY/${TEST_VALIDITY}/g" -e "s/LOCAL/${TEST_LOCAL}/g"  -e "s/BLOCK/${TEST_BLOCK}/g" -e "s/EXTRA_OPS/${TEST_EXTRA_OPS}/g"  > $TEST_FILE <<//EOF
// VALIDITY

.assembly '${TEST_NAME}_generated'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}

.method public static int32 Main() cil managed
{
	.entrypoint
	.maxstack 2
	${OPCODE_1}
	.try
	{
		nop
		${OPCODE_2}
		leave TRY_2
        }
        catch [mscorlib]System.NullReferenceException 
	{
		pop
		${OPCODE_3}
		leave END
	}

TRY_2:

	.try
	{
		nop
		${OPCODE_4}
		leave TRY_3
	}

	BLOCK
	{
		nop
		EXTRA_OPS
		${OPCODE_8}
		endfinally
	}

TRY_3:
	.try {
		.try
		{
			nop
			leave TRY_4
		}
		catch [mscorlib]System.NullReferenceException 
		{
			pop
			${OPCODE_5}
			leave END
		}
	}
	BLOCK
	{
		nop
		${OPCODE_9}
		endfinally
	}

TRY_4:

	.try 
	{
		nop
   		leave END
	}
	filter
	{
		pop
		${OPCODE_6}
		ldc.i4.0
		endfilter
	}
	{
		nop
		${OPCODE_7}
		leave END
	}

END:
	ldc.i4.0
	ret
}
//EOF
