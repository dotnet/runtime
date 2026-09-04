#!/usr/bin/env bash

set -euo pipefail

source_dir="$1"
destination_dir="$2"
expected_version="$3"
shift 3

mkdir -p "$destination_dir"

cohort_version=
for package_pattern in "$@"; do
  package_count="$(
    find "$source_dir" -maxdepth 1 -type f \
      -name "$package_pattern" -not -name '*.symbols.nupkg' -print |
      wc -l | tr -d '[:space:]'
  )"

  if [ "$package_count" -ne 1 ]; then
    echo "Expected exactly one non-symbol $package_pattern in $source_dir; found $package_count." >&2
    find "$source_dir" -maxdepth 1 -type f \
      -name "$package_pattern" -not -name '*.symbols.nupkg' -print >&2
    exit 1
  fi

  package="$(
    find "$source_dir" -maxdepth 1 -type f \
      -name "$package_pattern" -not -name '*.symbols.nupkg' -print -quit
  )"

  if [ -z "$cohort_version" ]; then
    package_prefix="${package_pattern%%\**}"
    cohort_version="$(basename "$package")"
    cohort_version="${cohort_version#"$package_prefix"}"
    cohort_version="${cohort_version%.nupkg}"

    if [ -n "$expected_version" ] && [ "$cohort_version" != "$expected_version" ]; then
      echo "Expected $(basename "$package") to match local package cohort $expected_version." >&2
      exit 1
    fi
  elif [[ "$(basename "$package")" != *".$cohort_version.nupkg" ]]; then
    echo "Expected $(basename "$package") to match local package cohort $cohort_version." >&2
    exit 1
  fi

  cp "$package" "$destination_dir/"
done
