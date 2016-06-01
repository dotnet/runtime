#!/usr/bin/env bash

# This script generates documentation about the various configuration options that
# are available and how to use them.

# Requires git, GNU bash and GNU m4 to run.

#################################
# Print script usage
#################################

if [ ! -r "$1" -o -z "$2" ]; then
	echo "usage: $0 <path to clrconfigvalues.h> <output file>"
	exit 1;
fi

#################################
# Intro section of the document
#################################

read -r -d '' INTROSECTION << "EOF"

There are two primary ways to configure runtime behavior: CoreCLR hosts can pass in key-value string pairs during runtime initialization, or users can set special variables in the environment or registry. Today, the set of configuration options that can be set via the former method is relatively small, but moving forward, we expect to add more options there. Each set of options is described below.

EOF

#################################
# Host configuration knobs section of the document
# 
# Contains information about the key-value pairs that can be
# passed in by a host during CoreCLR initialization.
#################################

read -r -d '' HOSTCONFIGURATIONKNOBSSECTION << "EOF"

## Host Configuration Knobs

These can be passed in by a host during initialization. Note that the values are all passed in as strings, so if the type is boolean, the value would be the string "true" or "false", and if it's a numeric value, it would be in the form "123".

Name | Description | Type
-----|-------------|------
System.GC.Concurrent|Enable concurrent GC|boolean
System.GC.Server|Enable server GC|boolean
System.GC.RetainVM|Put segments that should be deleted on a standby list for future use instead of releasing them back to the OS|boolean
System.Threading.ThreadPool.MinThreads|Override MinThreads for the ThreadPool worker pool|numeric
System.Threading.ThreadPool.MaxThreads|Override MaxThreads for the ThreadPool worker pool|numeric

EOF

#################################
# CLRConfig section of the document
# 
# This section contains a table of COMPlus configurations that's 
# generated based on the contents of the clrconfigvalues.h header.
#################################

CLRCONFIGSECTIONTITLE="## Environment/Registry Configuration Knobs"
DATE=`date +%D`;
COMMIT=`git rev-parse --short HEAD`
GENERATEDTABLEINFO="This table is machine-generated from commit $COMMIT on ${DATE}. It might be out of date."

read -r -d '' CLRCONFIGSECTIONCONTENTS << "EOF"
When using these configurations from environment variables, the variables need to have the `COMPlus_` prefix in their names. e.g. To set DumpJittedMethods to 1, add the environment variable `COMPlus_DumpJittedMethods=1`.

See also [Setting configuration variables](../building/viewing-jit-dumps.md#setting-configuration-variables) for more information.

Name | Description | Type | Class | Default Value | Flags 
-----|-------------|------|-------|---------------|-------
EOF

#################################
# M4 script for processing macros
#################################

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

EOF

#################################
# Write contents to file
#################################

cat <(echo "$INTROSECTION") <(echo)\
    <(echo "$HOSTCONFIGURATIONKNOBSSECTION") <(echo)\
    <(echo "$CLRCONFIGSECTIONTITLE") <(echo)\
    <(echo "$GENERATEDTABLEINFO") > "$2";

cat <(echo "$M4SCRIPT") \
    <(echo "$CLRCONFIGSECTIONCONTENTS") <(cat "$1" | sed "/^\/\//d" | sed "/^#/d" | sed "s/\\\\\"/'/g" | sed "/^$/d"  ) \
    | m4 | sed "s/;$//" >> "$2";