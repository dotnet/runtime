#!/bin/bash
dotnet build src/native/managed/cdac/tests/UnitTests -c Debug
dotnet test src/native/managed/cdac/tests/UnitTests -c Debug --filter "MethodDefinitionsByAddress"
