#!/usr/bin/env bash
# $1 contains full path to the .so to verify
# $2 contains message to print when the verification fails

count=0
while read -r line; do
  if [[ "$line" =~ ^.*"undefined symbol:" ]]; then
    array=($line)
    sym=${array[2]}

    # AddressSanitizer-instrumented shared libraries don't define the
    # symbols from the ASAN runtime. They are provided by the entry executable.
    # Therefore, we ignore them here as they're expected to not be present.
    if [[ "$sym" =~ ^"__asan" ]]; then
      continue
    fi

    if [[ "$count" -eq 0 ]]; then
      printf "Undefined symbol(s) found:\n"
    fi

    printf " %s\n" "${sym}"
    ((count++))
  fi
done < <(ldd -r $1 2>&1)

if [[ "$count" -gt 0 ]]; then
  exit 1
fi
