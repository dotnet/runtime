__VersionFolder="$(cd "$(dirname "$0")"; pwd -P)"
__RepoRoot="$(cd "$(dirname "$__VersionFolder")/../../"; pwd -P)"

# Use yes n and interactive cp instead of -n since -n is not supported on macOS.
yes n | cp -i "${__VersionFolder}/"*{.h,.c} "$__RepoRoot/artifacts/obj/"
