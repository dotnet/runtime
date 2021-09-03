#!/bin/sh
while [[ -h "$source" ]]; do
  scriptroot="$( cd -P "$( dirname "$source" )" && pwd )"
  source="$(readlink "$source")"
  # if $source was a relative symlink, we need to resolve it relative to the path where the
  # symlink file was located
  [[ $source != /* ]] && source="$scriptroot/$source"
done
scriptroot="$( cd -P "$( dirname "$source" )" && pwd )"


hooksFolder="$(cd -P "$( dirname "$scriptroot" )" && pwd )"
gitFolder="$(cd -P "$( dirname "$hooksFolder" )" && pwd )"
repoRoot="$(cd -P "$( dirname "$gitFolder" )" && pwd )"

LC_ALL=C
# Select files to format
NATIVE_FILES=$(git diff --cached --name-only --diff-filter=ACM "*.h" "*.hpp" "*.c" "*.cpp" "*.inl" | sed 's| |\\ |g')
MANAGED_FILES=$(git diff --cached --name-only --diff-filter=ACM "*.cs" "*.vb" | sed 's| |\\ |g')

if [[ -n "$NATIVE_FILES" ]]; then
    # Format all selected files
    echo "$NATIVE_FILES" | cat | xargs | sed -e 's/ /,/g' | xargs "$repoRoot/artifacts/tools/clang-format" -style=file -i

    # Add back the modified files to staging
    echo "$NATIVE_FILES" | xargs git add
fi
if [[ -n "$MANAGED_FILES" ]]; then
    # Format all selected files
    echo "$MANAGED_FILES" | cat | xargs | sed -e 's/ /,/g' | dotnet format --include

    # Add back the modified files to staging
    echo "$MANAGED_FILES" | xargs git add
fi


exit 0
