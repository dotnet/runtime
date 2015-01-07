#!/bin/bash

TEST_FILE=$1
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
	clean_aot
    MONO_PATH=$1 $2 --aot -v -v $3 | grep '^Method .*code length\|^000' | sed 's/emitted[^()]*//' | sed 's/0x[0-9a-fA-F]*/0x0/g' | awk -v RS='' '{gsub(/\n000/, "000"); print}' | sort
}

get_method () {
	clean_aot
	MONO_VERBOSE_METHOD="$4" MONO_PATH=$1 $2 --aot $3  | sed 's/0x[0-9a-fA-F]*/0x0/g'
}

diff_methods () {
	TMP_FILE=tmp_file
	echo "$(get_methods $1 $2 $3)" >$TMP_FILE
    sdiff -s -w 1000 <(cat $TMP_FILE) <(echo "$(MONO_DEBUG=gen-compact-seq-points get_methods $1 $2 $3)")
}

diff_method () {
	TMP_FILE=tmp_file
	echo "$(get_method $1 $2 $3 $4)" >$TMP_FILE
	sdiff -w 150 <(cat $TMP_FILE) <(echo "$(MONO_DEBUG=gen-compact-seq-points get_method $1 $2 $3 $4 | grep -Ev il_seq_point)")
}

get_method_name () {
	echo $1 | sed -E 's/Method (\([^)]*\) )?([^ ]*).*/\2/g'
}

get_method_length () {
	echo $1 | sed 's/.*code length \([0-9]*\).*/\1/'
}

echo "Checking unintended native code changes in $TEST_FILE"

TMP_FILE=tmp_file

echo "$(diff_methods $MONO_PATH $RUNTIME $TEST_FILE)" > $TMP_FILE

CHANGES=0
METHOD=""
MIN_SIZE=10000

while read line; do
	if [ "$line" != "" ]; then
		echo $line | sed 's/000.*//g'
		CHANGES=$((CHANGES+1))
		SIZE=$(get_method_length "$line")
		if [[ SIZE -lt MIN_SIZE ]]; then
			MIN_SIZE=$SIZE
			METHOD="$line"
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
	echo "$(diff_method $MONO_PATH $RUNTIME $TEST_FILE $METHOD_NAME)"
	exit 1
fi
