## This is a helper script for non-containerized local build and test execution.
## It downloads and uses the daily SDK which contains the compatible AspNetCore bits.
## Usage:
## ./build-local.ps1 [StressConfiguration] [LibrariesConfiguration]

$RepoRoot="$(git rev-parse --show-toplevel)"
&$RepoRoot/src/libraries/Common/tests/System/Net/StressTests/build-local.ps1 $PSScriptRoot @args
