__VersionFolder="$(cd "$(dirname "$0")"; pwd -P)"
__RepoRoot="$(cd "$(dirname "$__VersionFolder")/../../"; pwd -P)"

cp -r -n "${__VersionFolder}/"*{.h,.c} "$__RepoRoot/artifacts/obj/"
