#!/usr/bin/env bash

set -euo pipefail

project_dir=""
configuration="Debug"
framework=""
rid="iossimulator-arm64"
assembly="aot-instances.dll"
compiler=""
expect_exit_code=0

usage() {
    cat <<'EOF'
Usage: issue129613-sourceonly-run-mono-aot.sh --project-dir <dir> --framework <tfm> [options]

Required:
  --project-dir <dir>   Source-only app project directory
  --framework <tfm>     Target framework (for example net11.0-ios)

Optional:
  --configuration <cfg> Build configuration (default: Debug)
  --rid <rid>           Runtime identifier (default: iossimulator-arm64)
  --assembly <name>     Assembly entry from _AssembliesToAOT.items (default: aot-instances.dll)
  --compiler <path>     mono-aot-cross to run; defaults to the published compiler path file
  --expect-exit-code N  Expected compiler exit code (default: 0)
EOF
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        --project-dir)
            project_dir="$2"
            shift 2
            ;;
        --configuration)
            configuration="$2"
            shift 2
            ;;
        --framework)
            framework="$2"
            shift 2
            ;;
        --rid)
            rid="$2"
            shift 2
            ;;
        --assembly)
            assembly="$2"
            shift 2
            ;;
        --compiler)
            compiler="$2"
            shift 2
            ;;
        --expect-exit-code)
            expect_exit_code="$2"
            shift 2
            ;;
        -h|--help)
            usage
            exit 0
            ;;
        *)
            echo "Unknown argument: $1" >&2
            usage >&2
            exit 2
            ;;
    esac
done

if [[ -z "$project_dir" || -z "$framework" ]]; then
    usage >&2
    exit 2
fi

project_dir="$(cd "$project_dir" && pwd)"
base_dir="$project_dir/obj/$configuration/$framework/$rid"
items_file="$base_dir/linker-items/_AssembliesToAOT.items"

if [[ ! -f "$items_file" ]]; then
    echo "Missing _AssembliesToAOT.items at $items_file" >&2
    exit 3
fi

if [[ -z "$compiler" ]]; then
    compiler_path_file="$(find "$base_dir" -maxdepth 1 -name 'aot-compiler-path-*.txt' | head -n 1)"
    if [[ -z "$compiler_path_file" ]]; then
        echo "No compiler override supplied and no published compiler path file was found in $base_dir" >&2
        exit 4
    fi

    compiler="$(<"$compiler_path_file")"
fi

if [[ ! -x "$compiler" ]]; then
    echo "Compiler is not executable: $compiler" >&2
    exit 5
fi

command_json="$(python3 - "$items_file" "$assembly" "$compiler" "$project_dir" <<'PY'
import json
import os
import shlex
import sys
import xml.etree.ElementTree as ET

items_file, assembly_name, compiler_path, project_dir = sys.argv[1:]
ns = {"msbuild": "http://schemas.microsoft.com/developer/msbuild/2003"}
root = ET.parse(items_file).getroot()
items = []
target = None

for item in root.findall(".//msbuild:_AssembliesToAOT", ns):
    metadata = {}
    for child in item:
        tag = child.tag.split("}", 1)[-1]
        metadata[tag] = child.text or ""

    row = {"Include": item.attrib["Include"], **metadata}
    items.append(row)

    include = row["Include"].replace("\\", "/")
    if include.endswith("/" + assembly_name) or include.endswith(assembly_name):
        target = row

if target is None:
    raise SystemExit(f"Assembly '{assembly_name}' was not found in {items_file}")

inputs = [target["Include"]]
if target.get("IsDedupAssembly", "").lower() == "true":
    inputs.extend(item["Include"] for item in items if item["Include"] != target["Include"])

linked_dir = os.path.join(project_dir, os.path.dirname(target["Include"]))
aot_argument = ",".join(shlex.split(target["Arguments"]))
command = [compiler_path]
command.append(f"--path={linked_dir}")
command.append(aot_argument)
command.extend(shlex.split(target.get("ProcessArguments", "")))
command.extend(os.path.join(project_dir, path) for path in inputs)

print(json.dumps(command))
PY
)"

command=()
while IFS= read -r part; do
    command+=("$part")
done < <(python3 - "$command_json" <<'PY'
import json
import sys

for part in json.loads(sys.argv[1]):
    print(part)
PY
)

printf 'Running:'
for part in "${command[@]}"; do
    printf ' %q' "$part"
done
printf '\n'

set +e
(
    cd "$project_dir"
    "${command[@]}"
)
actual_exit_code=$?
set -e

if [[ "$actual_exit_code" -ne "$expect_exit_code" ]]; then
    echo "mono-aot-cross exit code $actual_exit_code did not match expected $expect_exit_code" >&2
    exit 6
fi
