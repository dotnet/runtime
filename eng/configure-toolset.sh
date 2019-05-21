# We depend on a local cli for a number of our buildtool
# commands like init-tools so for now we need to disable
# using the globally installed dotnet

use_installed_dotnet_cli=false

# Always use the local repo packages directory instead of
# the user's NuGet cache
use_global_nuget_cache=false