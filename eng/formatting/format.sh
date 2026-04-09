#!/bin/sh

LC_ALL=C
# Select files to format
NATIVE_FILES=$(git diff --cached --name-only --diff-filter=ACM "*.h" "*.hpp" "*.c" "*.cpp" "*.inl" | sed 's| |\\ |g')
MANAGED_FILES=$(git diff --cached --name-only --diff-filter=ACM "*.cs" "*.vb" | sed 's| |\\ |g')

exec 1>&2

if [ -n "$NATIVE_FILES" ]; then
    # Format all selected files
    echo "$NATIVE_FILES" | cat | xargs | sed -e 's/ /,/g' | xargs "./artifacts/tools/clang-format" -style=file -i

    # Add back the modified files to staging
    echo "$NATIVE_FILES" | xargs git add
fi

if [ -n "$MANAGED_FILES" ]; then
    # Format all selected files
    echo "$MANAGED_FILES" | cat | xargs | sed -e 's/ /,/g' | dotnet format whitespace --include - --folder

    # Add back the modified files to staging
    echo "$MANAGED_FILES" | xargs git add
fi

exit 0
