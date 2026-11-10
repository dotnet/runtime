#!/usr/bin/env bash

if (( $# != 2 )); then
  echo "Usage:"
  echo "verify-no-swift-objc-classes.sh <path to library> <path to NM command>"
  exit 1
fi

library=$1
nmCommand=$2

if ! nmOutput=$("$nmCommand" -m "$library"); then
  echo "ERROR: failed to inspect $library for Swift ObjC class definitions." >&2
  exit 2
fi

swiftObjCClasses=()
while IFS= read -r line; do
  # Swift classes are registered as ObjC classes with process-global names. Local
  # definitions in this library can collide if another copy is loaded into the
  # same process, so keep Swift bindings limited to value types and functions.
  if [[ $line != *"(undefined)"* && $line =~ (__DATA__TtC|__METACLASS_DATA__TtC|__IVARS__TtC|_OBJC_CLASS_\$__TtC) ]]; then
    swiftObjCClasses+=("$line")
  fi
done <<< "$nmOutput"

if (( ${#swiftObjCClasses[@]} != 0 )); then
  echo "ERROR: $library contains Swift ObjC class definitions." >&2
  echo "Swift classes have process-global ObjC runtime names and must not be used in this native library." >&2
  printf '%s\n' "${swiftObjCClasses[@]}" >&2
  exit 2
fi
