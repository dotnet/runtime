#!/bin/bash

DYADIC="ADF MUF SUF RSF DVF RDF POW RPW RMF FML FDV FRD POL"
MONADIC="MVF MNF ABS RND SQT LOG EXP SIN COS TAN ASN ACS ATN URD NRM"
OUTFILE=arm_fpamacros.h

# $1: opcode list
# $2: template
function gen() {
	for i in $1; do
		sed "s/<Op>/$i/g" $2.th >> $OUTFILE
	done
}

echo -e "/* Macros for FPA ops, auto-generated from template */\n" > $OUTFILE

echo -e "\n/* dyadic */\n" >>  $OUTFILE
gen "$DYADIC" fpa_macros

echo -e "\n/* monadic */\n" >>  $OUTFILE
gen "$MONADIC" fpam_macros

echo -e "\n\n" >> $OUTFILE

echo -e "\n/* end generated */\n" >> $OUTFILE

