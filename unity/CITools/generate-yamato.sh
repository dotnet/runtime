#!/bin/sh

BASEDIR=$(dirname $0)
# Using system install because the one in .dotnet gives a weird exception
dotnet run --project $BASEDIR/Unity.Cookbook/Unity.Cookbook.csproj "$@"