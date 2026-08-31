#!/usr/bin/env bash

saved=("$@")
while test $# -gt 0; do
  case "$1" in
    --help|-h)
      printf "Usage:\ngenerateredefinesfile.sh <filename> <jump instruction for the platform> <prefix of what is being mapped from> <prefix of what is being mapped to>\n"
      exit 1;;
    esac
    shift
done
set -- "${saved[@]}"

jump="$2"
prefix1="$3"
prefix2="$4"

while read -r line; do
  # Skip empty lines and comment lines starting with semicolon
  if [[ "$line" =~ ^\;.*$|^[[:space:]]*$ ]]; then
    continue
  fi

  # Remove the CR character in case the sources are mapped from
  # a Windows share and contain CRLF line endings
  line="${line//$'\r'/}"

  # Only process the entries that begin with "#"
  if [[ "$line" =~ ^#.*$ ]]; then
    line="${line//#/}"
    printf "LEAF_ENTRY %s%s, _TEXT\n" "$prefix1" "$line"
    printf "    %s EXTERNAL_C_FUNC(%s%s)\n" "$jump" "$prefix2" "$line"
    printf "LEAF_END %s%s, _TEXT\n\n" "$prefix1" "$line"
  fi
done < "$1"
