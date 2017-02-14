#!/usr/bin/env bash

function print_usage {
    echo ''
    echo 'CoreCLR perf test environment set up script on Linux.'
    echo ''
    echo 'Typical command line:'
    echo ''
    echo 'coreclr/tests/scripts/perf-perp.sh'
    echo '    --branch="dotnet_coreclr"'
    echo ''
    echo 'Required arguments:'
    echo '  --branch=<path>             : branch where coreclr/corefx/test bits are copied from (e.g. dotnet_coreclr).'
}

# Exit code constants
readonly EXIT_CODE_SUCCESS=0       # Script ran normally.

# Argument variables
perfArch="x64"
perfConfig="Release"
perfBranch=

for i in "$@"
do
    case $i in
        -h|--help)
            print_usage
            exit $EXIT_CODE_SUCCESS
            ;;
        --branch=*)
            perfBranch=${i#*=}
            ;;
        *)
            echo "Unknown switch: $i"
            print_usage
            exit $EXIT_CODE_SUCCESS
            ;;
    esac
done

perfBranch="dotnet_coreclr"
echo "branch = $perfBranch"
echo "architecture = $perfArch"
echo "configuration = $perfConfig"

# Since not all perf machines have Mono we cannot run nuget locally to get the Benchview tools
# Instead we curl the package feed and use grep and sed to find the newest package.
# We grep for content type and that returns us strings that contain the path to the nupkg
# Then we match only the last line using '$' and use the s command to replace the entire line
# with what we find inside of the quotes after src=.  We then jump to label x on a match and if 
# we don't match we delete the line.  This returns just the address of the last nupkg to curl.
curl "http://benchviewtestfeed.azurewebsites.net/nuget/FindPackagesById()?id='Microsoft.BenchView.JSONFormat'" | grep "content type" | sed "$ s/.*src=\"\([^\"]*\)\".*/\1/;tx;d;:x" | xargs curl -o benchview.zip
unzip -q -o benchview.zip -d ./tests/scripts/Microsoft.BenchView.JSONFormat

# Install python 3.5.2 to run machinedata.py for machine data collection
python3.5 --version
python3.5 ./tests/scripts/Microsoft.BenchView.JSONFormat/tools/machinedata.py

# Set up the copies
# Coreclr build containing the tests and mscorlib
curl https://ci.dot.net/job/$perfBranch/job/master/job/release_windows_nt/lastSuccessfulBuild/artifact/bin/tests/tests.zip -o tests.zip

# Corefx components.  We now have full stack builds on all distros we test here, so we can copy straight from CoreFX jobs.
mkdir corefx
curl https://ci.dot.net/job/dotnet_corefx/job/master/job/ubuntu14.04_release/lastSuccessfulBuild/artifact/bin/build.tar.gz -o ./corefx/build.tar.gz

# Unpack the corefx binaries
pushd corefx > /dev/null
tar -xf build.tar.gz
rm build.tar.gz
popd > /dev/null

# Unzip the tests first.  Exit with 0
mkdir bin
mkdir bin/tests
unzip -q -o tests.zip -d ./bin/tests/Windows_NT.$perfArch.$perfConfig || exit 0
echo "unzip tests to ./bin/tests/Windows_NT.$perfArch.$perfConfig"
