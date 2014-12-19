#!/bin/bash

 TESTFILE=$1

get_code_length () {
    ./mono -v $1 | grep 'code length' | sed 's/0x[0-9a-f]*/0x/g' | sed -E 's/Method (\(.*\) )?//g' | sort
}

sdiff_code_length () {
    sdiff -s -w 1000 <(echo "$(get_code_length $1)") <(echo "$(MONO_DEBUG=gen-compact-seq-points  get_code_length $1)")
}

DIFF_FILE=mktemp

echo "$(sdiff_code_length $TESTFILE)" > $DIFF_FILE

CHANGES=0

while read line; do
	if [ "$line" != "" ]; then
		CHANGES=$((CHANGES+1))
	    echo $line
	fi
done < $DIFF_FILE

if [ $CHANGES != 0 ]
then
	echo ''
	echo "Detected OP_IL_SEQ_POINT incompatibility on $TESTFILE"
	echo "  $CHANGES methods differ in size when sequence points are enabled."
	echo '  This is probably caused by a runtime optimization that is not handling OP_IL_SEQ_POINT'
	echo '  More details can be obtained by comparing the output of the following commands:'
	echo "    MONO_VERBOSE_METHOD=testname mono/mini/mono $TESTFILE"
	echo "    MONO_DEBUG=gen-compact-seq-points #MONO_VERBOSE_METHOD=testname mono/mini/mono $TESTFILE"
	exit 1
fi