#! /bin/sh

SED="sed"
if [ `which gsed 2> /dev/null` ] ; then 
	SED="gsed"
fi

TEST_NAME=$1
TEST_VALIDITY=$2
TEST_POS=$3
# Only 1 TEST_OP variable should be set.


declare OPCODE_${TEST_POS}="rethrow"


TEST_NAME=${TEST_VALIDITY}_${TEST_NAME}
TEST_FILE=${TEST_NAME}_generated.il
echo $TEST_FILE
$SED -e "s/VALIDITY/${TEST_VALIDITY}/g" -e "s/LOCAL/${TEST_LOCAL}/g" > $TEST_FILE <<//EOF
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
		.try
		{
			nop
			${OPCODE_4}
			leave END
		}
		catch [mscorlib]System.NullReferenceException 
		{
			pop
			${OPCODE_5}
			leave END
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

		leave END
        }
	.try
	{
		nop
		leave END
	}
	finally
	{
		nop
		${OPCODE_8}
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
		${OPCODE_9}
		ldc.i4.0
		endfilter
	}
	{
		nop
		${OPCODE_10}
		leave END
	}

END:
	ldc.i4.0
	ret
}
//EOF
