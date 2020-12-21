#!/usr/bin/env bash
#
# Generate module index header
#
set -euo pipefail

if [[ "$#" -lt 2 ]]; then
  echo "Usage: genmoduleindex.sh ModuleBinaryFile IndexHeaderFile"
  exit 1
fi

output=$2
printf "" > $output

function printIdAsBinary() {
  id="$1"

  # Print length in bytes
  bytesLength=${#id}
  printf "0x%02x, " "$((bytesLength/2))" > "$output"

  # Print each pair of hex digits with 0x prefix followed by a comma
  while [[ $id ]]; do
    printf '0x%s, ' "${id:0:2}"
    id=${id:2}
  done >> "$output"
}

OSName=$(uname -s)

case "$OSName" in
Darwin)
  array=($(dwarfdump -u $1))
  id="${array[1]}"
  id="${id//-/}"
  printIdAsBinary "$id"
  ;;
*)
  while read -r line; do
    if [[ "$line" =~ ^[[:space:]]*"Build ID:" ]]; then
      array=($line)
      printIdAsBinary "${array[2]}"
      break
    fi
  done < <(readelf -n $1)
  ;;
esac
