#!/usr/bin/env bash

saved=("$@")
while test $# -gt 0; do
  case "$1" in
    --help|-h)
      printf "Usage:\ngenerateversionscript.sh <filename> <prefix>\n"
      exit 1;;
    esac
    shift
done
set -- "${saved[@]}"

prefix="$2"

printf "V1.0 {\n    global:\n"

while read -r line; do
  # Skip empty lines and comment lines starting with semicolon
  if [[ "$line" =~ ^\;.*$|^[[:space:]]*$ ]]; then
    continue
  fi

  # Remove the CR character in case the sources are mapped from
  # a Windows share and contain CRLF line endings
  line="${line//$'\r'/}"

  # Only prefix the entries that start with "#"
  if [[ "$line" =~ ^#.*$ ]]; then
    printf "        %s%s;\n" "$prefix" "${line//#/}"
  else
    printf "        %s;\n" "$line"
  fi
done < "$1"

printf "    local: *;\n};\n"
