#!/usr/bin/env bash

# Requires git, GNU bash and GNU m4 to run.

DATE=`date +%D`;
COMMIT=`git rev-parse --short HEAD`

if [ ! -r "$1" -o -z "$2" ]; then
	echo "usage: $0 <path to clrconfigvalues.h> <output file>"
	exit 1;
fi

read -r -d '' M4SCRIPT << "EOF"
changequote(`"', `"')
define("CONFIG_DWORD_INFO", "`$2` | $4 | DWORD | patsubst("$1", "_.*", "") | $3 | ")dnl
define("RETAIL_CONFIG_DWORD_INFO", "`$2` | $4 | DWORD | patsubst("$1", "_.*", "") | $3 | ")dnl
define("CONFIG_DWORD_INFO_DIRECT_ACCESS", "`$2` | $3 | DWORD | patsubst("$1", "_.*", "") | | ")dnl
define("RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS", "`$2` | $3 | DWORD | patsubst("$1", "_.*", "") | | ")dnl
define("CONFIG_STRING_INFO", "`$2` | $3 | STRING | patsubst("$1", "_.*", "") | | ")dnl
define("RETAIL_CONFIG_STRING_INFO", "`$2` | $3 | STRING | patsubst("$1", "_.*", "") | | ")dnl
define("CONFIG_STRING_INFO_DIRECT_ACCESS", "`$2` | $3 | STRING | patsubst("$1", "_.*", "") | | ")dnl
define("RETAIL_CONFIG_STRING_INFO_DIRECT_ACCESS", "`$2` | $3 | STRING | patsubst("$1", "_.*", "") | | ")dnl
define("CONFIG_DWORD_INFO_EX", "`$2` | $4 | DWORD | patsubst("$1", "_.*", "") | $3 | patsubst(patsubst("$5", "CLRConfig::", ""), "|", "/")")dnl
define("RETAIL_CONFIG_DWORD_INFO_EX", "`$2` | $4 | DWORD | patsubst("$1", "_.*", "") | $3 | patsubst(patsubst("$5", "CLRConfig::", ""), "|", "/")")dnl
define("CONFIG_STRING_INFO_EX", "`$2` | $3 | STRING | patsubst("$1", "_.*", "") | | patsubst(patsubst("$4", "CLRConfig::", ""), "|", "/")")dnl
define("RETAIL_CONFIG_STRING_INFO_EX", "`$2` | $3 | STRING | patsubst("$1", "_.*", "") | | patsubst(patsubst("$4", "CLRConfig::", ""), "|", "/")")dnl
define("W", "$1")dnl
dnl
#CLR Configuration Knobs

EOF

INFO="This Document is machine-generated from commit $COMMIT on ${DATE}. It might be out of date."

read -r -d '' HEADER << "EOF"
When using these configurations from environment variables, the variable need to have `COMPlus_` prefix in its name. e.g. To set DumpJittedMethods to 1, add `COMPlus_DumpJittedMethods=1` to envvars.

See also [Dumps and Other Tools](../botr/ryujit-overview.md#dumps-and-other-tools) for more information.

Name | Description | Type | Class | Default Value | Flags 
-----|-------------|------|-------|---------------|-------
EOF


cat <(echo "$M4SCRIPT") <(echo) \
	<(echo "$INFO") <(echo) \
	<(echo "$HEADER") <(cat "$1" | sed "/^\/\//d" | sed "/^#/d" | sed "s/\\\\\"/'/g" | sed "/^$/d"  ) \
	| m4 | sed "s/;$//" > "$2";
