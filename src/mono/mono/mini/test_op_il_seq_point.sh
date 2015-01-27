#!/bin/bash

TEST_FILE=$1
USE_AOT=$2

TMP_FILE_PREFIX=$(basename $0).tmp
BASEDIR=$(dirname $0)

MONO_PATH=$BASEDIR/../../mcs/class/lib/net_4_5:$BASEDIR
RUNTIME=$BASEDIR/../../runtime/mono-wrapper

trap "rm -rf ${TMP_FILE_PREFIX}*" EXIT

tmp_file () {
	mktemp ./${TMP_FILE_PREFIX}XXXX
}

clean_aot () {
	rm -rf *.exe..so *.exe.dylib *.exe.dylib.dSYM
}

get_methods () {
	if [ -z $4 ]; then
		MONO_PATH=$1 $2 -v --compile-all=1 $3 | grep '^Method .*code length' | sed 's/emitted[^()]*//' | sort
	else
		clean_aot
		MONO_PATH=$1 $2 -v --aot $3 | grep '^Method .*code length' | sed 's/emitted[^()]*//' | sort
	fi
}

get_method () {
	if [ -z $5 ]; then
		MONO_VERBOSE_METHOD="$4" MONO_PATH=$1 $2 --compile-all=1 $3 | sed 's/0x[0-9a-fA-F]*/0x0/g'
	else
		clean_aot
		MONO_VERBOSE_METHOD="$4" MONO_PATH=$1 $2 --aot $3 | sed 's/0x[0-9a-fA-F]*/0x0/g'
	fi
}

diff_methods () {
	TMP_FILE=$(tmp_file)
	echo "$(get_methods $1 $2 $3 $4)" >$TMP_FILE
	diff <(cat $TMP_FILE) <(echo "$(MONO_DEBUG=gen-compact-seq-points get_methods $1 $2 $3 $4)")
}

diff_method () {
	TMP_FILE=$(tmp_file)
	echo "$(get_method $1 $2 $3 $4 $5)" >$TMP_FILE
	sdiff -w 150 <(cat $TMP_FILE) <(echo "$(MONO_DEBUG=gen-compact-seq-points get_method $1 $2 $3 $4 $5 | grep -Ev il_seq_point)")
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
METHOD=""
MIN_SIZE=10000

while read line; do
	if [ "$line" != "" ]; then
		echo $line
		if [[ ${line:0:1} == "<" ]]; then
			CHANGES=$((CHANGES+1))
			SIZE=$(get_method_length "$line")
			if [[ SIZE -lt MIN_SIZE ]]; then
				MIN_SIZE=$SIZE
				METHOD="$line"
			fi
		fi
	fi
done < $TMP_FILE

if [ $CHANGES != 0 ]
then
	METHOD_NAME=$(get_method_name "$METHOD")

	echo ''
	echo "Detected OP_IL_SEQ_POINT incompatibility on $TEST_FILE"
	echo "  $CHANGES methods differ when sequence points are enabled."
	echo '  This is probably caused by a runtime optimization that is not handling OP_IL_SEQ_POINT'

	echo ''
	echo "Diff $METHOD_NAME"
	echo "Without IL_OP_SEQ_POINT                                                         With IL_OP_SEQ_POINT"
	echo "$(diff_method $MONO_PATH $RUNTIME $TEST_FILE $METHOD_NAME $USE_AOT)"
	exit 1
fi
