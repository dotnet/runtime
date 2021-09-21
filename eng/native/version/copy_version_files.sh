__VersionFolder="$(cd "$(dirname "$0")"; pwd -P)"
__RepoRoot="$(cd "$(dirname "$__VersionFolder")/../../"; pwd -P)"

for path in "${__VersionFolder}/"*{.h,.c}; do
    # -s <filename> checks if file has nonzero size (we don't need any more guarantee than that)
    if [ ! -s "$__RepoRoot/artifacts/obj/$(basename "$path")" ]; then
        cp "$path" "$__RepoRoot/artifacts/obj/"
    fi
done
