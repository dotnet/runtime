#!/usr/bin/env bash

# Why is this a separate script? Why not just invoke 'cmake' and 'make' in the C# build scripts themselves?
# I really don't know, but it doesn't work when I do that. Something about SIGCHLD not getting from clang to cmake or something.
#       -anurse

set -e

SOURCE="${BASH_SOURCE[0]}"
while [ -h "$SOURCE" ]; do # resolve $SOURCE until the file is no longer a symlink
  DIR="$( cd -P "$( dirname "$SOURCE" )" && pwd )"
  SOURCE="$(readlink "$SOURCE")"
  [[ "$SOURCE" != /* ]] && SOURCE="$DIR/$SOURCE" # if $SOURCE was a relative symlink, we need to resolve it relative to the path where the symlink file was located
done
DIR="$( cd -P "$( dirname "$SOURCE" )" && pwd )"

echo "Building Corehost from $DIR to $(pwd)"
cmake "$DIR" -G "Unix Makefiles"
make
