#!/bin/sh

DYADIC="ADF MUF SUF RSF DVF RDF POW RPW RMF FML FDV FRD POL"
MONADIC="MVF MNF ABS RND SQT LOG EXP SIN COS TAN ASN ACS ATN URD NRM"

# $1: opcode list
# $2: template
gen() {
	for i in $1; do
		sed "s/<Op>/$i/g" $2.th
	done
}

echo -e "/* Macros for FPA ops, auto-generated from template */\n"

echo -e "\n/* dyadic */\n"
gen "$DYADIC" fpa_macros

echo -e "\n/* monadic */\n"
gen "$MONADIC" fpam_macros

echo -e "\n\n"

echo -e "\n/* end generated */\n"
