#! /bin/sh
b=`dirname $0`
exec ${b}/dotnet.sh ${b}/../roslyn/artifacts/bin/csc/Release/netcoreapp3.1/csc.dll "$@"
