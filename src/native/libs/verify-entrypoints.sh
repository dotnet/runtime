#!/usr/bin/env bash

if (( $# != 3 )); then
  echo "Usage:"
  echo "verify-entrypoints.sh <path to shared library> <path to entries.c file> <path to NM command>"
  exit 1
fi

nmCommand=$3

IFS=$'\n'
dllList=()
for line in $($nmCommand $1); do
  pattern='^[[:xdigit:]]+ T _?([[:alnum:]_]+)'
  if [[ $line =~ $pattern ]]; then
    # skip symbols that we don't want to consider
    case ${BASH_REMATCH[1]} in
      init) ;;
      fini) ;;
      etext) ;;
      # _chk_fail & _stack_chk_fail are present in Haiku builds
      _chk_fail) ;;
      _stack_chk_fail) ;;
      PROCEDURE_LINKAGE_TABLE_) ;;
      *)    dllList+=(${BASH_REMATCH[1]});;
    esac
  fi
done

entriesList=()
for line in $(<$2); do
  pattern='^[[:space:]]+DllImportEntry\(([[:alnum:]_]+)\)'
  if [[ $line =~ $pattern ]]; then
    entriesList+=(${BASH_REMATCH[1]})
  fi
done

diffList=$(echo -n ${entriesList[@]} ${dllList[@]} | tr " " "\n" | sort | uniq -u)

if [ -n "$diffList" ]; then
  echo "ERROR: $2 file did not match entries exported from $1" >&2
  echo "DIFFERENCES FOUND: " >&2
  echo $diffList | tr " " "," >&2
  exit 2
fi
