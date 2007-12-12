#! /bin/sh

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
sed -e "s/VALIDITY/${TEST_VALIDITY}/g" -e "s/LOCAL/${TEST_LOCAL}/g"  -e "s/BLOCK/${TEST_BLOCK}/g" -e "s/EXTRA_OPS/${TEST_EXTRA_OPS}/g"  > $TEST_FILE <<//EOF
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
		leave END
        }
        catch [mscorlib]System.NullReferenceException 
	{
		pop
		${OPCODE_3}
		leave END
        }

	.try
	{
		nop
		${OPCODE_4}
		leave END
	}

	BLOCK
	{
		nop
		EXTRA_OPS
		${OPCODE_8}
		endfinally
	}

	.try {
		.try
		{
			nop
			leave END
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
