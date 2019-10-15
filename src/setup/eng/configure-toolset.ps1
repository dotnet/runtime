# Disable using the globally installed SDK. Using the global install can cause
# roll-forward to a newer SDK that may not work.
$script:useInstalledDotNetCli = $false

# Always use the local repo packages directory instead of the user's NuGet cache
# to keep the same between "ci" and non-"ci" builds. If the efficiency gain is
# required and it's worth maintaining the different types of build, this can be
# removed.
$script:useGlobalNuGetCache = $false
