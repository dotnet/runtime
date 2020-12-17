#!/usr/bin/env bash

dllList=$(nm $1 | awk '/^[0-9a-fA-F]+ T \S+/ { if ($3 != "_init" && $3 != "_fini") print $3 }')
entriesList=$(awk 'match($0, /^\s+DllImportEntry\((\S+)\)/, m) { print m[1]}' $2)
diffList=$(echo -n $entriesList $dllList | tr " " "\n" | sort | uniq -u)
if [ -n "$diffList" ]; then
  echo "ERROR: $2 file did not match entries exported from $1" >&2
  echo "DIFFERENCES FOUND: " >&2 
  echo $diffList | tr " " "," >&2
  exit 2
fi
