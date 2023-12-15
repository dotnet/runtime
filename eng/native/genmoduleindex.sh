#!/usr/bin/env bash
#
# Generate module index header
#
set -euo pipefail

if [[ "$#" -lt 2 ]]; then
  echo "Usage: genmoduleindex.sh ModuleBinaryFile IndexHeaderFile readelfBinaryPath"
  exit 1
fi

function printIdAsBinary() {
  id="$1"

  # Print length in bytes
  bytesLength="${#id}"
  printf "0x%02x, " "$((bytesLength/2))"

  # Print each pair of hex digits with 0x prefix followed by a comma
  while [[ "$id" ]]; do
    printf '0x%s, ' "${id:0:2}"
    id=${id:2}
  done
}

case "$(uname -s)" in
Darwin)
  cmd="dwarfdump"
  arg="-u"
  pattern='^UUID: ([0-9A-Fa-f\-]+)';;
*)
  cmd="$3"
  arg="-n"
  pattern='^[[:space:]]*Build ID: ([0-9A-Fa-f\-]+)';;
esac

"$cmd" "$arg" "$1" | while read -r line; do
  if [[ "$line" =~ $pattern ]]; then
    printIdAsBinary "${BASH_REMATCH[1]//-/}"
    break
  fi
done > "$2"
