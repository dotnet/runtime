#!/bin/bash -e

DEFAULT_PROFILE=$1
TEST_FILE=$2
USE_AOT=$3

TMP_FILE_PREFIX=$(basename $0).tmp
BASEDIR=$(dirname $0)

case "$(uname -s)" in
	CYGWIN*)
		PLATFORM_PATH_SEPARATOR=';'
		PLATFORM_AOT_ARGUMENT=--aot=asmonly
		;;
	*)
		PLATFORM_PATH_SEPARATOR=':'
		PLATFORM_AOT_ARGUMENT=--aot
		;;
esac

MONO_PATH=$BASEDIR/../../mcs/class/lib/$DEFAULT_PROFILE$PLATFORM_PATH_SEPARATOR$BASEDIR
RUNTIME=$BASEDIR/../../runtime/mono-wrapper

trap "rm -rf ${TMP_FILE_PREFIX}*" EXIT

tmp_file () {
	mktemp ./${TMP_FILE_PREFIX}XXXXXX
}

clean_aot () {
	rm -rf *.exe.so *.exe.dylib *.exe.dylib.dSYM *.exe.dll *.exe.s
}

# The test compares the generated native code size between a compilation with and without seq points.
# In some architectures ie:amd64 when possible 32bit instructions and registers are used instead of 64bit ones.
# Using MONO_DEBUG=single-imm-size avoids 32bit optimizations thus mantaining the native code size between compilations.

get_methods () {
	if [ -z $4 ]; then
		MONO_PATH=$1 $2 -v --compile-all=1 $3 | grep '^Method .*code length' | sed 's/emitted[^()]*//' | sort
		return ${PIPESTATUS[0]}
	else
		clean_aot
		MONO_PATH=$1 $2 -v $PLATFORM_AOT_ARGUMENT $3 | grep '^Method .*code length' | sed 's/emitted[^()]*//' | sort
		return ${PIPESTATUS[0]}
	fi
}

get_method () {
	if [ -z $5 ]; then
		MONO_VERBOSE_METHOD="$4" MONO_PATH=$1 $2 --compile-all=1 $3 | sed 's/0x[0-9a-fA-F]*/0x0/g'
		return ${PIPESTATUS[0]}
	else
		clean_aot
		MONO_VERBOSE_METHOD="$4" MONO_PATH=$1 $2 $PLATFORM_AOT_ARGUMENT $3 | sed 's/0x[0-9a-fA-F]*/0x0/g'
		return ${PIPESTATUS[0]}
	fi
}

diff_methods () {
	TMP_FILE1=$(tmp_file)
	TMP_FILE2=$(tmp_file)
	echo "$(MONO_DEBUG=no-compact-seq-points,single-imm-size get_methods $1 $2 $3 $4 || echo Non-zero exit code for file1: $?)" >$TMP_FILE1
	echo "$(MONO_DEBUG=single-imm-size get_methods $1 $2 $3 $4 || echo Non-zero exit code for file2: $?)" >$TMP_FILE2
	diff $TMP_FILE1 $TMP_FILE2
}

diff_method () {
	TMP_FILE1=$(tmp_file)
	TMP_FILE2=$(tmp_file)
	echo "$(MONO_DEBUG=no-compact-seq-points,single-imm-size get_method $1 $2 $3 $4 $5)" >$TMP_FILE1
	echo "$(MONO_DEBUG=single-imm-size get_method $1 $2 $3 $4 $5 | grep -Ev il_seq_point)" >$TMP_FILE2
	sdiff -w 150 $TMP_FILE1 $TMP_FILE2
}

get_method_name () {
	echo $1 | sed -E 's/.*Method (\([^)]*\) )?([^ ]*).*/\2/g'
}

get_method_length () {
	echo $1 | sed 's/.*code length \([0-9]*\).*/\1/'
}

if [ -z $USE_AOT ]; then
	echo "Checking unintended native code changes in $TEST_FILE without AOT"
else
	echo "Checking unintended native code changes in $TEST_FILE with AOT"
fi

TMP_FILE=$(tmp_file)

echo "$(diff_methods $MONO_PATH $RUNTIME $TEST_FILE $USE_AOT)" > $TMP_FILE

CHANGES=0
FAIL=0
METHOD=""
MIN_SIZE=10000

while read line; do
	if [ "$line" != "" ]; then
		echo $line
		if [[ "$line" == *"Non-zero exit code"* ]]; then
			FAIL=1
		elif [[ ${line:0:1} == "<" ]]; then
			CHANGES=$((CHANGES+1))
			SIZE=$(get_method_length "$line")
			if [[ SIZE -lt MIN_SIZE ]]; then
				MIN_SIZE=$SIZE
				METHOD="$line"
			fi
		fi
	fi
done < $TMP_FILE

TESTRESULT_FILE=TestResult-op_il_seq_point.tmp

echo -n "              <test-case name=\"MonoTests.op_il_seq_point.${TEST_FILE}${USE_AOT}\" executed=\"True\" time=\"0\" asserts=\"0\" success=\"" >> $TESTRESULT_FILE

if [ $FAIL != 0 ]
then
	echo "False\">" >> $TESTRESULT_FILE
	echo "                <failure>" >> $TESTRESULT_FILE
	echo -n "                  <message><![CDATA[" >> $TESTRESULT_FILE
	echo "Mono failed on $TEST_FILE" >> $TESTRESULT_FILE
	echo "]]></message>" >> $TESTRESULT_FILE
	echo "                  <stack-trace>" >> $TESTRESULT_FILE
	echo "                  </stack-trace>" >> $TESTRESULT_FILE
	echo "                </failure>" >> $TESTRESULT_FILE
	echo "              </test-case>" >> $TESTRESULT_FILE
	exit 1
elif [ $CHANGES != 0 ]
then
	METHOD_NAME=$(get_method_name "$METHOD")

	echo "False\">" >> $TESTRESULT_FILE
	echo "                <failure>" >> $TESTRESULT_FILE
	echo -n "                  <message><![CDATA[" >> $TESTRESULT_FILE
	echo "Detected OP_IL_SEQ_POINT incompatibility on $TEST_FILE" >> $TESTRESULT_FILE
	echo "  $CHANGES methods differ when sequence points are enabled." >> $TESTRESULT_FILE
	echo '  This is probably caused by a runtime optimization that is not handling OP_IL_SEQ_POINT' >> $TESTRESULT_FILE
	echo '' >> $TESTRESULT_FILE
	echo "Diff $METHOD_NAME" >> $TESTRESULT_FILE
	echo "Without IL_OP_SEQ_POINT                                                         With IL_OP_SEQ_POINT" >> $TESTRESULT_FILE
	echo -n "$(diff_method $MONO_PATH $RUNTIME $TEST_FILE $METHOD_NAME $USE_AOT)" >> $TESTRESULT_FILE
	echo "]]></message>" >> $TESTRESULT_FILE
	echo "                  <stack-trace>" >> $TESTRESULT_FILE
	echo "                  </stack-trace>" >> $TESTRESULT_FILE
	echo "                </failure>" >> $TESTRESULT_FILE
	echo "              </test-case>" >> $TESTRESULT_FILE

	echo ''
	echo "Detected OP_IL_SEQ_POINT incompatibility on $TEST_FILE"
	echo "  $CHANGES methods differ when sequence points are enabled."
	echo '  This is probably caused by a runtime optimization that is not handling OP_IL_SEQ_POINT'

	echo ''
	echo "Diff $METHOD_NAME"
	echo "Without IL_OP_SEQ_POINT                                                         With IL_OP_SEQ_POINT"
	echo "$(diff_method $MONO_PATH $RUNTIME $TEST_FILE $METHOD_NAME $USE_AOT)"
	exit 1
else
	echo "True\" />" >> $TESTRESULT_FILE
fi
