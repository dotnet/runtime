#! /bin/sh

SED="sed"
if [ `which gsed 2> /dev/null` ] ; then 
	SED="gsed"
fi

TEST_NAME=$1
TEST_VALIDITY=$2
TEST_POS=$3
TEST_LEAVE_CODE=$4
TEST_EXTRA=$5


declare OPCODE_${TEST_POS}="$TEST_LEAVE_CODE"
declare OPCODE_EXTRA_${TEST_POS}="${TEST_EXTRA}"

TEST_NAME=${TEST_VALIDITY}_${TEST_NAME}
TEST_FILE=${TEST_NAME}_generated.il
echo $TEST_FILE
$SED -e "s/VALIDITY/${TEST_VALIDITY}/g" -e "s/OPCODE_EXTRA_1/${OPCODE_EXTRA_1}/g"  -e "s/OPCODE_EXTRA_2/${OPCODE_EXTRA_2}/g" -e "s/OPCODE_EXTRA_3/${OPCODE_EXTRA_3}/g" -e "s/OPCODE_EXTRA_4/${OPCODE_EXTRA_4}/g" > $TEST_FILE <<//EOF
// VALIDITY

.assembly '${TEST_NAME}_generated'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}

.method public static int32 Main() cil managed
{
	.entrypoint
	.maxstack 8
	.locals init (int32 V0)

	OPCODE_EXTRA_1
	${OPCODE_1}

	.try
	{
		newobj instance void class [mscorlib]System.Exception::.ctor()
		throw
		leave END
        }
        catch [mscorlib]System.Exception 
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
	finally
	{
		nop
		OPCODE_EXTRA_2
		${OPCODE_2}
END_2:
		endfinally
	}

	.try 
	{
		newobj instance void class [mscorlib]System.Exception::.ctor()
		throw 
 		leave END
	}
	filter
	{
		pop
		OPCODE_EXTRA_3
		${OPCODE_3}
END_3:
		ldc.i4.0
		endfilter
	}
	{
		nop
		leave END
	}

END:
	ldc.i4.0
	ret
}
//EOF
