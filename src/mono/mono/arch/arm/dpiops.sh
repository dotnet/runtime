#!/bin/bash

OPCODES="AND EOR SUB RSB ADD ADC SBC RSC ORR BIC"
CMP_OPCODES="TST TEQ CMP CMN"
MOV_OPCODES="MOV MVN"
OUTFILE=arm_dpimacros.h

# $1: opcode list
# $2: template
function gen() {
	for i in $1; do
		sed "s/<Op>/$i/g" $2.th >> $OUTFILE
	done
}



echo -e "/* Macros for DPI ops, auto-generated from template */\n" > $OUTFILE

echo -e "\n/* mov/mvn */\n" >>  $OUTFILE
gen "$MOV_OPCODES" mov_macros

echo -e "\n/* DPIs, arithmetic and logical */\n" >>  $OUTFILE
gen "$OPCODES" dpi_macros

echo -e "\n\n" >> $OUTFILE

echo -e "\n/* DPIs, comparison */\n" >>  $OUTFILE
gen "$CMP_OPCODES" cmp_macros

echo -e "/* end generated */\n\n" >> $OUTFILE

