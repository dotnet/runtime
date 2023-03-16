#!/usr/bin/env bash
# $1 contains full path to the .so to verify
# $2 contains message to print when the verification fails

count=0
while read -r line; do
  # AddressSanitizer-instrumented shared libraries don't define the
  # symbols from the ASAN runtime. They are provided by the entry executable.
  # Therefore, we ignore them here as they're expected to not be present.
  if [[ "$line" =~ ^.*"undefined symbol: (?!__asan)" ]]; then
    if [[ "$count" -eq 0 ]]; then
      printf "Undefined symbol(s) found:\n"
    fi
    array=($line)
    printf " %s\n" "${array[2]}"
    ((count++))
  fi
done < <(ldd -r $1 2>&1)

if [[ "$count" -gt 0 ]]; then
  exit 1
fi
