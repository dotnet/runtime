#!/usr/bin/env bash

# always ignore system dotnet
export use_installed_dotnet_cli=false
. "../../../eng/common/tools.sh"
InitializeDotNetCli true
which dotnet