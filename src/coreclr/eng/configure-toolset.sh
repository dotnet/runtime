# We depend on a local cli for a number of our buildtool
# commands like init-tools so for now we need to disable
# using the globally installed dotnet

use_installed_dotnet_cli=false

# Working around issue https://github.com/dotnet/arcade/issues/2673
DisableNativeToolsetInstalls=true
