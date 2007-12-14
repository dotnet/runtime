#! /bin/sh

TEST_NAME=$1
TEST_VALIDITY=$2
TEST_POS=$3
TEST_FILTER_EXTRAS=$4

for I in {2..5};
do
	declare LEAVE_${I}="leave END"
done

declare OPCODE_${TEST_POS}="endfilter"
declare LEAVE_${TEST_POS}="endfilter"
declare END_FILTER_${TEST_POS}="endfilter"
declare EXTRAS_${TEST_POS}="ldc.i4.0"



TEST_NAME=${TEST_VALIDITY}_${TEST_NAME}
TEST_FILE=${TEST_NAME}_generated.il
echo $TEST_FILE
sed -e "s/VALIDITY/${TEST_VALIDITY}/g" -e "s/FILTER_EXTRAS/${TEST_FILTER_EXTRAS}/g"   > $TEST_FILE <<//EOF
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
		newobj instance void class [mscorlib]System.Exception::.ctor()
		throw 

		${LEAVE_2}
        }
        catch [mscorlib]System.Exception 
	{
		pop
		${LEAVE_3}
        }

	.try {
		.try
		{
			newobj instance void class [mscorlib]System.Exception::.ctor()
			throw 

			${LEAVE_4}
		}
		finally
		{
			nop
			${OPCODE_6}
			endfinally
		}

		leave END

	} catch [mscorlib]System.Exception 
	{
		pop
		leave END
        }

	.try 
	{
		newobj instance void class [mscorlib]System.Exception::.ctor()
		throw 


 		leave END
	}
	filter
	{
		${EXTRAS_7}
		${END_FILTER_7}

		pop

		nop
		${EXTRAS_8}
		${END_FILTER_8}

		${EXTRAS_9}
		FILTER_EXTRAS
		${END_FILTER_9}
	}
	{
		nop
		${LEAVE_5}
	}

END:
	ldc.i4.0
	ret
}
//EOF
