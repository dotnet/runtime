#!/usr/bin/env bash

bash -c "echo $*"
bash -c "echo $@"

.dotnet-daily/dotnet build $*
