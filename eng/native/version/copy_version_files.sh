__VersionFolder="$(cd "$(dirname "$0")"; pwd -P)"
__RepoRoot="$(cd "$(dirname "$__VersionFolder")/../../"; pwd -P)"

for path in "${__VersionFolder}/"*{.h,.c}; do
    if [[ "$(basename $path)" == _version.c ]]; then
        # For _version.c, update the commit ID if it has changed from the last build.
        # Set IFS to nothing to prevent the shell from combining all of the piped output into a single line in the script below
        IFS=
        # update commit
        commit="$(git rev-parse HEAD 2>/dev/null)"
        commit="${commit:-N/A}"
        substitute="$(printf 'static char sccsid[] __attribute__((used)) = "@(#)Version N/A @Commit: %s";\n' "$commit")"
        version_file_contents=$(cat $path | sed "s|^static.*|$substitute|")
        version_file_destination=$__RepoRoot/artifacts/obj/_version.c
        if [[ ! -e "$version_file_destination" || "$version_file_contents" != "$(<"$__RepoRoot/artifacts/obj/_version.c")" ]]; then
            echo $version_file_contents > "$version_file_destination"
        fi
    elif [[ ! -e "$__RepoRoot/artifacts/obj/$(basename "$path")" ]]; then
        cp "$path" "$__RepoRoot/artifacts/obj/"
    fi
done
