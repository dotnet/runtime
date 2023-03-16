#!/bin/sh

BASEDIR=$(dirname $0)
$BASEDIR/../../dotnet.sh run --project $BASEDIR/../../unity/CITools/BuildDriver/BuildDriver.csproj -- "$@"