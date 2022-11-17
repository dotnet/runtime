#!/bin/sh

# Run this script from a VS command prompt

BASEDIR=$(dirname $0)
mcs /debug:full /out:$BASEDIR/LibraryWithMdb.dll /target:library $BASEDIR/LibraryWithMdb.cs
