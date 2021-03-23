#!/usr/bin/env bash

if [ "$1" = "--dynamic" ]; then
  __DynamicSymbolsOption="-D"
  shift
else
  __DynamicSymbolsOption=""
fi

while read -r line; do
  if [[ "$line" =~ g_dacTable ]]; then

    # Parse line for DAC relative address, if length of value is:
    # * shorter than 16, zero pad.
    # * longer than 16, capture last 16 characters.
    #
    array=($line)
    value="$(printf "%016s\n" ${array[2]:(${#array[2]} > 16 ? -16 : 0)})"

    # Write line to file and exit
    printf "#define DAC_TABLE_RVA 0x%s\n" "$value" > "$2"
    break
  fi
done < <(${NM:-nm} $__DynamicSymbolsOption -P -t x $1)
