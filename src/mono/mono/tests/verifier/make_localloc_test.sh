#! /bin/sh

SED="sed"
if [ `which gsed 2> /dev/null` ] ; then 
	SED="gsed"
fi

TEST_NAME=$1
TEST_VALIDITY=$2
TEST_INIT=$3
TEST_BLOCK=$4

TRY_END="
		leave END
	} catch [mscorlib]System.NullReferenceException {
		leave END
	}"

if [ "$TEST_BLOCK" = "catch" ]; then
	TRY_MIDDLE="
		leave END
	} catch [mscorlib]System.NullReferenceException {"
	TRY_END="
		leave END
	}"
elif [ "$TEST_BLOCK" = "filter" ]; then
	TRY_MIDDLE="
		leave END
	} filter {"
	TRY_END="
		pop
		ldc.i4.0
		endfilter
	} {
		leave END
	}"
elif [ "$TEST_BLOCK" = "handler" ]; then
	TRY_MIDDLE="
		leave END
	} filter {
		pop
		ldc.i4.0
		endfilter
	} {"
	TRY_END="
		leave END
	}"
elif [ "$TEST_BLOCK" = "finally" ]; then
	TRY_MIDDLE="
		leave END
	} finally {"
	TRY_END="
		endfinally
	}"
elif [ "$TEST_BLOCK" = "fault" ]; then
	TRY_MIDDLE="
		leave END
	} fault {"
	TRY_END="
		endfault
	}"
fi


TEST_FILE=`echo ${TEST_VALIDITY}_${TEST_NAME} | $SED -e 's/ /_/g' -e 's/\./_/g' -e 's/&/mp/g' -e 's/\[/_/g' -e 's/\]/_/g'`_generated.il
echo $TEST_FILE
TEST_TYPE1=`echo $TEST_TYPE1 | $SED -s 's/&/\\\&/'`

$SED -e "s/OPCODE/${TEST_OP}/g" -e "s/VALIDITY/${TEST_VALIDITY}/g" -e "s/INIT/${TEST_INIT}/g" > $TEST_FILE <<//EOF

// VALIDITY CIL which breaks the ECMA-335 rules. 
// this CIL should fail verification by a conforming CLI verifier.

.assembly '${TEST_NAME}_generated'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}

.class public Template\`1<T>
  	extends [mscorlib]System.Object
{
}

.class public auto ansi sealed MyStruct
  	extends [mscorlib]System.ValueType
{
    .field public int32 valid
}

.method public static int32 Main() cil managed
{
	.entrypoint
	.maxstack 5
	.locals init (native int V_0, int32 V_1)

	.try {
		nop
${TRY_MIDDLE}
		INIT
		localloc
		stloc.0

${TRY_END}

END:
	ldc.i4.0
	ret
}
//EOF
