#!/usr/bin/env bash
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

# CoreCLR-WASI smoke test runner. Builds + runs managed test assemblies
# under corerun.wasm on wasmtime. Exit-code 100 = pass.
#
# Usage:
#   ./src/coreclr/wasi/tests/run-tests.sh            # all smoke tests
#   ./src/coreclr/wasi/tests/run-tests.sh HelloWorld # one test by name
#   ./src/coreclr/wasi/tests/run-tests.sh --list     # enumerate
#
# Env overrides:
#   CONFIG        - Release (default) | Debug | Checked
#   WASMTIME      - explicit path to a wasmtime binary
#   COREROOT      - explicit path to a CORE_ROOT layout (override staging)
#   KEEP_STAGING  - if set, leave the per-test staging dir on disk

set -euo pipefail

script_dir="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
repo_root="$( cd "$script_dir/../../../.." && pwd )"
config="${CONFIG:-Release}"
smoke_dir="$script_dir/smoke"
known_broken_dir="$script_dir/known-broken"

# --- Locate the build outputs we need ----------------------------------------

clr_artifacts="$repo_root/artifacts/bin/coreclr/wasi.wasm.$config"
corerun="$clr_artifacts/corerun"
corelib="$clr_artifacts/IL/System.Private.CoreLib.dll"
runtime_pack="$repo_root/artifacts/bin/microsoft.netcore.app.runtime.wasi-wasm/$config/runtimes/wasi-wasm/lib/net11.0"

if [[ ! -f "$corerun" ]]; then
    echo "error: corerun.wasm not found at $corerun" >&2
    echo "       run: ./build.sh -s clr+libs -c $config -os wasi /p:TestAssemblies=false" >&2
    exit 2
fi
if [[ ! -f "$corelib" ]]; then
    echo "error: System.Private.CoreLib.dll not found at $corelib" >&2
    exit 2
fi
if [[ ! -d "$runtime_pack" ]]; then
    echo "error: runtime pack not found at $runtime_pack" >&2
    echo "       run: ./build.sh -s clr+libs -c $config -os wasi /p:TestAssemblies=false" >&2
    exit 2
fi

# --- Enumerate tests (before provisioning so --list is instant) --------------

# Portable: macOS default bash 3.2 has no `mapfile`.
# Default suite = smoke/ only (green baseline).
# Named lookups also consider known-broken/ so individual debugging works.
default_tests=()
while IFS= read -r -d '' csproj; do
    default_tests+=("$csproj")
done < <(find "$smoke_dir" -mindepth 2 -maxdepth 2 -name '*.csproj' -print0 | sort -z)

all_tests=("${default_tests[@]}")
if [[ -d "$known_broken_dir" ]]; then
    while IFS= read -r -d '' csproj; do
        all_tests+=("$csproj")
    done < <(find "$known_broken_dir" -mindepth 2 -maxdepth 2 -name '*.csproj' -print0 | sort -z)
fi

list_tests() {
    echo "smoke (default):"
    for csproj in "${default_tests[@]}"; do
        echo "  $(basename "$csproj" .csproj)"
    done
    if [[ ${#all_tests[@]} -gt ${#default_tests[@]} ]]; then
        echo "known-broken (run by name only):"
        for csproj in "${all_tests[@]}"; do
            case " ${default_tests[*]} " in
                *" $csproj "*) continue ;;
            esac
            echo "  $(basename "$csproj" .csproj)"
        done
    fi
}

if [[ $# -ge 1 && "${1}" == "--list" ]]; then
    list_tests
    exit 0
fi

# --- Locate or provision wasmtime --------------------------------------------

provisioned_dir="$repo_root/artifacts/obj/wasmtime"
candidate_wasmtimes=(
    "${WASMTIME:-}"
    "$provisioned_dir/wasmtime"
    "$HOME/.wasmtime/bin/wasmtime"
)

wasmtime_bin=""
for cand in "${candidate_wasmtimes[@]}"; do
    if [[ -n "$cand" && -x "$cand" ]]; then
        # Probe: a wrong-arch binary will fail --version with a Bad CPU /
        # Exec format / cannot execute error. Skip those quietly.
        if "$cand" --version >/dev/null 2>&1; then
            wasmtime_bin="$cand"
            break
        fi
    fi
done

if [[ -z "$wasmtime_bin" ]]; then
    echo "Provisioning wasmtime via $script_dir/provision-wasmtime.proj ..." >&2
    "$repo_root/.dotnet/dotnet" build "$script_dir/provision-wasmtime.proj" \
        /p:Configuration="$config" \
        -v:m --nologo
    if [[ -x "$provisioned_dir/wasmtime" ]] && "$provisioned_dir/wasmtime" --version >/dev/null 2>&1; then
        wasmtime_bin="$provisioned_dir/wasmtime"
    else
        echo "error: no usable wasmtime found." >&2
        echo "  tried: ${candidate_wasmtimes[*]}" >&2
        echo "  install wasmtime (e.g. curl https://wasmtime.dev/install.sh -sSf | bash)" >&2
        echo "  and re-run, or set WASMTIME=/path/to/wasmtime" >&2
        exit 2
    fi
fi
echo "wasmtime: $("$wasmtime_bin" --version)"

# --- Pick selected tests ------------------------------------------------------

# Selected = args (or default suite if no args)
declare -a selected
if [[ $# -eq 0 ]]; then
    selected=("${default_tests[@]}")
else
    for name in "$@"; do
        match=""
        for csproj in "${all_tests[@]}"; do
            if [[ "$(basename "$csproj" .csproj)" == "$name" ]]; then
                match="$csproj"
                break
            fi
        done
        if [[ -z "$match" ]]; then
            echo "error: no such test '$name'" >&2
            echo "available:" >&2
            list_tests >&2
            exit 2
        fi
        selected+=("$match")
    done
fi

# --- Run each test -----------------------------------------------------------

pass=0
fail=0
declare -a failures

for csproj in "${selected[@]}"; do
    name="$(basename "$csproj" .csproj)"
    echo
    echo "=== $name ==="

    # Build and discover the output dll via msbuild's TargetPath property
    # (the repo's Directory.Build.props redirects bin/ globally).
    "$repo_root/.dotnet/dotnet" build "$csproj" -c "$config" -v:q --nologo \
        || { echo "  BUILD FAILED"; fail=$((fail+1)); failures+=("$name: build"); continue; }

    test_dll="$("$repo_root/.dotnet/dotnet" build "$csproj" -c "$config" -v:q --nologo \
        -getProperty:TargetPath 2>/dev/null | tail -1)"
    if [[ -z "$test_dll" || ! -f "$test_dll" ]]; then
        echo "  error: built dll not found (TargetPath='$test_dll')"
        fail=$((fail+1)); failures+=("$name: missing dll"); continue
    fi

    # Stage CORE_ROOT.
    staging="$(mktemp -d -t "wasi-smoke-$name.XXXXXX")"
    trap '[[ -n "${KEEP_STAGING:-}" ]] || rm -rf "$staging"' RETURN
    # Order matters: runtime pack first, then overlay the freshly-built
    # CoreLib + corerun + test on top so newer artifacts win.
    cp "$runtime_pack"/*.dll "$staging/"
    cp "$corerun" "$corelib" "$test_dll" "$staging/"

    # Run. Capture stdout to scan for the sentinel; tee to terminal so the
    # user still sees test output live. corerun on WASI currently does not
    # propagate the managed Main return code (latched_exit_code from
    # coreclr_shutdown_2 overrides it; see corerun.cpp:672), so the test
    # must emit "WASI-SMOKE-PASS:<name>" on stdout to signal success.
    out_file="$staging/_stdout.log"
    set +e
    "$wasmtime_bin" run -W exceptions=y \
        --dir "$staging::/" \
        --env CORE_ROOT=/ \
        --env DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true \
        "$staging/corerun" "$name.dll" 2>&1 | tee "$out_file"
    rc="${PIPESTATUS[0]}"
    set -e

    sentinel="WASI-SMOKE-PASS:$name"
    if grep -qx "$sentinel" "$out_file"; then
        echo "  PASS (wasmtime exit=$rc, sentinel matched)"
        pass=$((pass+1))
    else
        echo "  FAIL (wasmtime exit=$rc, missing sentinel '$sentinel')"
        fail=$((fail+1)); failures+=("$name: no sentinel (exit=$rc)")
    fi

    [[ -n "${KEEP_STAGING:-}" ]] && echo "  staging: $staging"
done

# --- Summary -----------------------------------------------------------------

echo
echo "=== summary: $pass passed, $fail failed ==="
if [[ $fail -gt 0 ]]; then
    for f in "${failures[@]}"; do
        echo "  - $f"
    done
    exit 1
fi
