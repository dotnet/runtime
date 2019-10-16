# We depend on a local cli for a number of our buildtool
# commands like init-tools so for now we need to disable
# using the globally installed dotnet

$script:useInstalledDotNetCli = $false

# Always use the local repo packages directory instead of
# the user's NuGet cache
$script:useGlobalNuGetCache = $false

# Working around issue https://github.com/dotnet/arcade/issues/2673
$script:DisableNativeToolsetInstalls = $true
