#!/usr/bin/env bash

saved=("$@")
while test $# -gt 0; do
  case "$1" in
    --help|-h)
      printf "Usage:\ngenerateexportedsymbols.sh <filename>\n"
      exit 1;;
    esac
    shift
done
set -- "${saved[@]}"

while read -r line; do
  # Skip empty lines and comment lines starting with semicolon
  if [[ "$line" =~ ^\;.*$|^[[:space:]]*$ ]]; then
    continue
  fi

  # Remove the CR character in case the sources are mapped from
  # a Windows share and contain CRLF line endings
  line="${line//$'\r'/}"

  printf "_%s\n" "${line//#/}"
done < "$1"
