#!/usr/bin/env bash
#
# Generate module index header
#
set -euo pipefail

if [[ "$#" -lt 2 ]]; then
  echo "Usage: genmoduleindex.sh ModuleBinaryFile IndexHeaderFile"
  exit 1
fi

OSName=$(uname -s)

case "$OSName" in
Darwin)
	# Extract the build id and prefix it with its length in bytes
	dwarfdump -u $1 |
	awk '/UUID:/ { gsub(/\-/,"", $2); printf("%02x", length($2)/2); print $2}' |
	# Convert each byte of the id to 0x prefixed constant followed by comma
	sed -E s/\(\.\.\)/0x\\1,\ /g > $2
	;;
*)
	# Extract the build id and prefix it with its length in bytes
	readelf -n $1 |
	awk '/Build ID:/ { printf("%02x", length($3)/2); print $3 }' |
	# Convert each byte of the id to 0x prefixed constant followed by comma
	sed -E s/\(\.\.\)/0x\\1,\ /g > $2
	;;
esac
