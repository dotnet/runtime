# SPDX-License-Identifier: 0BSD

#############################################################################
#
# remove-ordinals.cmake
#
# Removes the ordinal numbers from a DEF file that has been created by
# GNU ld or LLVM lld option --output-def (when creating a Windows DLL).
# This should be equivalent: sed 's/ \+@ *[0-9]\+//'
#
# Usage:
#
#     cmake -DINPUT_FILE=infile.def.in \
#           -DOUTPUT_FILE=outfile.def \
#           -P remove-ordinals.cmake
#
#############################################################################
#
# Author: Lasse Collin
#
#############################################################################

file(READ "${INPUT_FILE}" STR)
string(REGEX REPLACE " +@ *[0-9]+" "" STR "${STR}")
file(WRITE "${OUTPUT_FILE}" "${STR}")
