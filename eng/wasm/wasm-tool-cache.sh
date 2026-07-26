#!/bin/sh
#
# Resolves a tool in the shared wasm tool cache described in eng/wasm/WasmToolCache.props.
# Kept in sync with that file: the cache root, the main-checkout anchoring, and the
# <tool>/<version>-<rid> layout are duplicated here because the native build scripts cannot
# evaluate MSBuild properties.
#
# POSIX sh only (no bashisms): this file is sourced both from bash (eng/native/gen-buildsys.sh)
# and, via $(shell . ...) in make, from /bin/sh, which is dash on many Linux distros.
#
# Usage: wasm_tool_cache_dir <tool name> <version file> <repo root>
# Prints the resolved directory and returns 0, or returns 1 if it is not provisioned.

# Anchor the cache at the *main* checkout so every git worktree shares one copy and it is removed
# with the repo. A worktree's <root>/.git is a FILE 'gitdir: <main>/.git/worktrees/<name>'; a
# normal clone's is a DIRECTORY. Only an ABSOLUTE gitdir is trusted as the main checkout; a
# relative gitdir (git relative-worktrees) or a normal clone falls back to the repo root, which is
# resolved to an absolute path so callers that pass a relative repo root still get a usable value.
# The anchoring rule matches eng/wasm/WasmToolCache.props exactly when a repo root is supplied,
# which every caller does.
wasm_tool_anchor_root()
{
    _repo_root="${1%/}"
    if [ -d "$_repo_root" ]; then
        _abs_repo="$(cd "$_repo_root" 2>/dev/null && pwd)"
        [ -n "$_abs_repo" ] && _repo_root="$_abs_repo"
    fi
    _git="$_repo_root/.git"
    if [ -f "$_git" ]; then
        _gitdir="$(sed -n 's/^gitdir: *//p' "$_git" | tr '\\' '/')"
        case "$_gitdir" in
            /*/.git/worktrees/*) printf '%s\n' "${_gitdir%%/.git/worktrees/*}"; return 0 ;;
        esac
    fi
    printf '%s\n' "$_repo_root"
}

wasm_tool_cache_root()
{
    # DOTNET_WASM_TOOL_CACHE_DIR wins; otherwise anchor under the repo's main checkout, exactly as
    # eng/wasm/WasmToolCache.props does. A repo root is always supplied by callers, so there is no
    # further fallback -- an unresolved root yields an empty result and the caller reports the
    # missing tool.
    if [ -n "${DOTNET_WASM_TOOL_CACHE_DIR:-}" ]; then
        printf '%s\n' "${DOTNET_WASM_TOOL_CACHE_DIR%/}"
    elif [ -n "${1:-}" ]; then
        printf '%s\n' "$(wasm_tool_anchor_root "$1")/.dotnet/wasm-tools"
    fi
}

wasm_tool_host_rid()
{
    _os=linux
    case "$(uname -s)" in
        Darwin) _os=osx ;;
        Linux)  _os=linux ;;
    esac
    # Normalize to the same names MSBuild's $(BuildArchitecture) uses, which comes from
    # RuntimeInformation.ProcessArchitecture (x86 / x64 / arm / arm64 / ...).
    _arch="$(uname -m)"
    case "$_arch" in
        arm64|aarch64)          _arch=arm64 ;;
        x86_64|amd64)           _arch=x64 ;;
        i[3456]86|x86)          _arch=x86 ;;
        armv*|arm)              _arch=arm ;;
    esac
    printf '%s-%s\n' "$_os" "$_arch"
}

wasm_tool_cache_dir()
{
    _tool="$1"
    _version_file="$2"
    _repo_root="$3"

    _root="$(wasm_tool_cache_root "$_repo_root")"
    if [ -z "$_root" ] || [ ! -f "$_version_file" ]; then
        return 1
    fi

    _version="$(tr -d '[:space:]' < "$_version_file")"
    if [ -z "$_version" ]; then
        return 1
    fi

    _rid="$(wasm_tool_host_rid)"
    _candidate="$_root/$_tool/$_version-$_rid"
    if [ -f "$_candidate.complete" ]; then
        printf '%s\n' "$_candidate"
        return 0
    fi

    return 1
}
