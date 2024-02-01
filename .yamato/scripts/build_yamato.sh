#!/bin/sh

BASEDIR=$(dirname $0)
$BASEDIR/../../dotnet.sh restore $BASEDIR/../../unity/CITools/BuildDriver/BuildDriver.csproj --configfile $BASEDIR/../../NuGet.config
$BASEDIR/../../dotnet.sh run --project $BASEDIR/../../unity/CITools/BuildDriver/BuildDriver.csproj --no-restore -- "$@"