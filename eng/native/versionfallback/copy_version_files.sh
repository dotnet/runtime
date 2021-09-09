__VersionFallbackFolder="$(cd "$(dirname "$0")"; pwd -P)"
__RepoRoot="$(cd "$(dirname "$__VersionFallbackFolder")/../../"; pwd -P)"

cp -r -n "${__VersionFallbackFolder}/"*{.h,.c} "$__RepoRoot/artifacts/obj/"
