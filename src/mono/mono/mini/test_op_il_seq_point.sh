#!/bin/bash

 TESTFILE=$1

get_methods () {
    ./mono -v -v $1 | grep '^Method \|^000' | sed 's/emitted[^()]*//' | sed 's/0x[0-9a-fA-F]*/0x0/g' | awk -v RS='' '{gsub(/\n000/, "000"); print}' | sort
}

get_method () {
	MONO_VERBOSE_METHOD="$2" ./mono $1 | grep '^Method \|^000' | sed 's/emitted[^()]*//' | sed 's/0x[0-9a-fA-F]*/0x0/g'
}

sdiff_methods () {
    sdiff -s -w 1000 <(echo "$(get_methods $1)") <(echo "$(MONO_DEBUG=gen-compact-seq-points  get_methods $1)")
}

diff_method () {
	diff <(echo "$(get_method $1 $2)") <(echo "$(MONO_DEBUG=gen-compact-seq-points  get_method $1 $2)")
}

get_method_name () {
	echo $1 | sed -E 's/Method (\([^)]*\) )?([^ ]*).*/\2/g'
}

get_method_length () {
	echo $1 | sed 's/.*code length \([0-9]*\).*/\1/'
}

DIFF_FILE=mktemp

echo "$(sdiff_methods $TESTFILE)" > $DIFF_FILE

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
done < $DIFF_FILE

if [ $CHANGES != 0 ]
then
	METHOD_NAME=$(get_method_name "$METHOD")

	echo "$(diff_method $TESTFILE $METHOD_NAME)"

	echo ''
	echo "Detected OP_IL_SEQ_POINT incompatibility on $TESTFILE"
	echo "  $CHANGES methods differ when sequence points are enabled."
	echo '  This is probably caused by a runtime optimization that is not handling OP_IL_SEQ_POINT'
	echo '  Differences can be obtained by comparing the output of the following commands:'
	echo "    MONO_VERBOSE_METHOD=\"$METHOD_NAME\" mono/mini/mono mono/mini/$TESTFILE"
	echo "    MONO_DEBUG=gen-compact-seq-points MONO_VERBOSE_METHOD=\"$METHOD_NAME\" mono/mini/mono mono/mini/$TESTFILE"
	exit 1
fi