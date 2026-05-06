#!/bin/sh

DYADIC="ADD SUB MUL NMUL DIV"
MONADIC="CPY ABS NEG SQRT CMP CMPE CMPZ CMPEZ CVT UITO SITO TOUI TOSI TOUIZ TOSIZ"

# $1: opcode list
# $2: template
gen() {
	for i in $1; do
		sed "s/<Op>/$i/g" $2.th
	done
}

echo -e "/* Macros for VFP ops, auto-generated from template */\n"

echo -e "\n/* dyadic */\n"
gen "$DYADIC" vfp_macros

echo -e "\n/* monadic */\n"
gen "$MONADIC" vfpm_macros

echo -e "\n\n"

echo -e "\n/* end generated */\n"
