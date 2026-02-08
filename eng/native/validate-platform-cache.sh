#!/usr/bin/env bash
#
# Validates or regenerates the platform-specific CMake tryrun cache.
#
# Usage:
#   validate-platform-cache.sh [--regenerate] <platform>
#
# Platforms:
#   osx-arm64    - macOS ARM64 (Apple Silicon) native builds
#   osx-x64      - macOS x64 native builds
#   linux-x64    - Linux x64 native builds
#   linux-arm64  - Linux ARM64 native builds
#   browser      - WebAssembly browser builds (requires Emscripten)
#   ios          - iOS/tvOS cross-compilation
#
# Examples:
#   validate-platform-cache.sh osx-arm64              # Validate only
#   validate-platform-cache.sh --regenerate osx-arm64 # Regenerate if different
#   validate-platform-cache.sh browser                # Validate WASM cache
#

set -euo pipefail

scriptroot="$( cd -P "$( dirname "$0" )" && pwd )"
reporoot="$(cd "$scriptroot"/../..; pwd -P)"

regenerate=0
if [[ "${1:-}" == "--regenerate" ]]; then
    regenerate=1
    shift
fi

if [[ "$#" -lt 1 ]]; then
    echo "Usage: validate-platform-cache.sh [--regenerate] <platform>"
    echo ""
    echo "Options:"
    echo "  --regenerate  Update the cache file if differences are found"
    echo ""
    echo "Platforms:"
    echo "  osx-arm64    - macOS ARM64 (Apple Silicon) native builds"
    echo "  osx-x64      - macOS x64 native builds"
    echo "  linux-x64    - Linux x64 native builds"
    echo "  linux-arm64  - Linux ARM64 native builds"
    echo "  browser      - WebAssembly browser builds (requires Emscripten)"
    echo "  ios          - iOS/tvOS cross-compilation"
    exit 1
fi

platform="$1"

# Determine cache file and cmake configuration based on platform
cache_file=""
cmake_args=()

case "$platform" in
    osx-arm64)
        cache_file="$scriptroot/tryrun.osx-arm64.cmake"
        cmake_args=(
            -DCLR_CMAKE_HOST_UNIX=1
            -DCLR_CMAKE_HOST_OSX=1
            -DCLR_CMAKE_HOST_ARCH_ARM64=1
            -DCMAKE_OSX_ARCHITECTURES=arm64
        )
        ;;
    osx-x64)
        cache_file="$scriptroot/tryrun.osx-x64.cmake"
        cmake_args=(
            -DCLR_CMAKE_HOST_UNIX=1
            -DCLR_CMAKE_HOST_OSX=1
            -DCLR_CMAKE_HOST_ARCH_AMD64=1
            -DCMAKE_OSX_ARCHITECTURES=x86_64
        )
        ;;
    linux-x64)
        cache_file="$scriptroot/tryrun.linux-x64.cmake"
        cmake_args=(
            -DCLR_CMAKE_HOST_UNIX=1
            -DCLR_CMAKE_HOST_LINUX=1
            -DCLR_CMAKE_HOST_ARCH_AMD64=1
        )
        ;;
    linux-arm64)
        cache_file="$scriptroot/tryrun.linux-arm64.cmake"
        cmake_args=(
            -DCLR_CMAKE_HOST_UNIX=1
            -DCLR_CMAKE_HOST_LINUX=1
            -DCLR_CMAKE_HOST_ARCH_ARM64=1
        )
        ;;
    browser)
        cache_file="$scriptroot/tryrun.browser.cmake"
        if [[ -z "${EMSDK_PATH:-}" ]]; then
            if [[ -d "$reporoot/src/mono/browser/emsdk/" ]]; then
                export EMSDK_PATH="$reporoot/src/mono/browser/emsdk/"
            else
                echo "Error: EMSDK_PATH not set. Please set it or run ./src/mono/browser/emsdk/setup.sh first."
                exit 1
            fi
        fi
        export EMSDK_QUIET=1
        source "$EMSDK_PATH/emsdk_env.sh"
        cmake_args=(
            -DCLR_CMAKE_HOST_ARCH_WASM=1
            -DCLR_CMAKE_TARGET_OS=browser
        )
        ;;
    ios|tvos)
        cache_file="$scriptroot/tryrun_ios_tvos.cmake"
        cmake_args=(
            -DCLR_CMAKE_HOST_UNIX=1
            -DCLR_CMAKE_HOST_OSX=1
            -DCLR_CMAKE_HOST_ARCH_ARM64=1
            -DCMAKE_SYSTEM_NAME=iOS
        )
        ;;
    *)
        echo "Unknown platform: $platform"
        echo "Run with no arguments to see available platforms."
        exit 1
        ;;
esac

if [[ ! -f "$cache_file" ]]; then
    echo "Cache file does not exist: $cache_file"
    if [[ "$regenerate" == "1" ]]; then
        echo "Will generate new cache file."
    else
        echo "Nothing to validate."
        exit 0
    fi
fi

echo "============================================================"
echo "Platform Cache Validation"
echo "============================================================"
echo "Platform:   $platform"
echo "Cache file: $cache_file"
echo ""

# Create temporary directory for cmake configure
tmpdir=$(mktemp -d)
trap "rm -rf $tmpdir" EXIT

echo "Running CMake configure without cache (this may take a minute)..."
echo ""

# Run cmake configure without the platform cache
export CLR_CMAKE_SKIP_PLATFORM_CACHE=1

# Prepare cmake command
cmake_cmd="cmake"
if [[ "$platform" == "browser" ]]; then
    cmake_cmd="emcmake cmake"
fi

# Configure coreclr to get all the check results
if ! $cmake_cmd -S "$reporoot/src/coreclr" -B "$tmpdir" \
    -G Ninja \
    -DCMAKE_BUILD_TYPE=Debug \
    "${cmake_args[@]}" \
    > "$tmpdir/cmake-output.txt" 2>&1; then
    echo "CMake configure failed. Output:"
    tail -50 "$tmpdir/cmake-output.txt"
    # Don't delete tmpdir on failure
    trap - EXIT
    exit 1
fi

echo "CMake configure completed."
echo ""

# Extract relevant variables from CMakeCache.txt
cmake_cache="$tmpdir/CMakeCache.txt"

# Function to extract cache value
get_cache_value() {
    local var="$1"
    grep "^${var}:" "$cmake_cache" 2>/dev/null | cut -d= -f2 || echo ""
}

# Function to normalize values for comparison
normalize_value() {
    local val="$1"
    # Remove quotes, whitespace
    val=$(echo "$val" | sed 's/^"//;s/"$//;s/^[[:space:]]*//;s/[[:space:]]*$//')
    # Empty string normalization
    [[ "$val" == '""' || "$val" == "''" ]] && val=""
    echo "$val"
}

# Compare against cached values
echo "Comparing detected values against cached values..."
echo ""

differences=()
matched=0
total=0

# Extract all set() and set_cache_value() calls from the cache file and compare
while IFS= read -r line; do
    var_name=""
    cached_value=""

    # Match lines like: set(VARIABLE_NAME value CACHE ...)
    if [[ "$line" =~ ^set\(([A-Z_0-9]+)[[:space:]]+(.*)[[:space:]]+CACHE ]]; then
        var_name="${BASH_REMATCH[1]}"
        cached_value="${BASH_REMATCH[2]}"
    # Match lines like: set_cache_value(VARIABLE_NAME value)
    elif [[ "$line" =~ ^set_cache_value\(([A-Z_0-9]+)[[:space:]]+([0-9-]+)\) ]]; then
        var_name="${BASH_REMATCH[1]}"
        cached_value="${BASH_REMATCH[2]}"
    else
        continue
    fi

    # Skip helper macros and comments
    [[ -z "$var_name" ]] && continue

    cached_value=$(normalize_value "$cached_value")
    detected_value=$(normalize_value "$(get_cache_value "$var_name")")

    ((total++)) || true

    if [[ "$cached_value" != "$detected_value" ]]; then
        differences+=("$var_name: cached='$cached_value' detected='$detected_value'")
    else
        ((matched++)) || true
    fi
done < "$cache_file"

echo "============================================================"
echo "Results"
echo "============================================================"
echo "Total variables checked: $total"
echo "Matched: $matched"
echo "Differences: ${#differences[@]}"
echo ""

if [[ ${#differences[@]} -eq 0 ]]; then
    echo "✓ Platform cache is up to date!"
    exit 0
fi

echo "Differences found:"
echo ""
for diff in "${differences[@]}"; do
    echo "  - $diff"
done
echo ""

if [[ "$regenerate" == "1" ]]; then
    echo "Regenerating cache file..."

    # Generate new cache file from CMakeCache.txt
    new_cache="$tmpdir/new-cache.cmake"

    # Detect compiler version for macOS platforms
    compiler_version_line=""
    if [[ "$platform" == osx-* ]]; then
        clang_version=$(clang --version 2>/dev/null | head -1 | sed -n 's/.*clang version \([0-9]*\)\..*/\1/p')
        if [[ -n "$clang_version" ]]; then
            compiler_version_line="# AppleClang major version this cache was generated with
set(CLR_CMAKE_PLATFORM_CACHE_COMPILER_VERSION \"$clang_version\" CACHE STRING \"AppleClang version for this cache\")
"
        fi
    fi

    # Write header
    cat > "$new_cache" << HEADER
# CMake pre-configured cache for $platform builds
#
# This file caches the results of CMake feature detection checks to significantly
# speed up the CMake configure phase.
#
# AUTO-GENERATED by validate-platform-cache.sh --regenerate
# Generated on: $(date -u +"%Y-%m-%d %H:%M:%S UTC")
#
# HOW TO DISABLE:
#   If you encounter issues, disable with:
#     export CLR_CMAKE_SKIP_PLATFORM_CACHE=1
#

${compiler_version_line}# Helper macro for TRY_RUN results
macro(set_cache_value)
  set(\${ARGV0} \${ARGV1} CACHE STRING "Result from TRY_RUN" FORCE)
  set(\${ARGV0}__TRYRUN_OUTPUT "dummy output" CACHE STRING "Output from TRY_RUN" FORCE)
endmacro()

HEADER

    # Extract all relevant variables from CMakeCache.txt and write to new cache
    # Sort for consistent output
    grep -E "^(HAVE_|HAS_|COMPILER_|LINKER_|MMAP_|ONE_SHARED|PTHREAD_|REALPATH_|SEM_INIT|NEON_|FNO_|KEVENT_|IPV6MR_|INOTIFY_|LD_FLAG)" "$cmake_cache" | sort | \
    while IFS= read -r line; do
        var_name=$(echo "$line" | cut -d: -f1)
        var_value=$(echo "$line" | cut -d= -f2-)

        # Handle EXITCODE variables specially (use set_cache_value macro)
        if [[ "$var_name" == *_EXITCODE ]]; then
            echo "set_cache_value(${var_name} ${var_value})" >> "$new_cache"
        else
            # Normalize empty values
            if [[ -z "$var_value" ]]; then
                echo "set(${var_name} \"\" CACHE INTERNAL \"\")" >> "$new_cache"
            else
                echo "set(${var_name} ${var_value} CACHE INTERNAL \"\")" >> "$new_cache"
            fi
        fi
    done

    # Copy new cache to target location
    cp "$new_cache" "$cache_file"
    echo ""
    echo "✓ Cache file updated: $cache_file"
    echo ""
    echo "Next steps:"
    echo "  git diff $cache_file"
    echo "  git add $cache_file"
    echo "  git commit -m 'Regenerate platform cache for $platform'"
else
    echo "Run with --regenerate to update the cache file:"
    echo "  $0 --regenerate $platform"
    exit 1
fi
