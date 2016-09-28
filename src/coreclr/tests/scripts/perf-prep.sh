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

# Install nuget to download benchview package, which includes the script machinedata.py for machine data collection
wget https://dist.nuget.org/win-x86-commandline/latest/nuget.exe
chmod u+x ./nuget.exe
./nuget.exe install Microsoft.BenchView.JSONFormat -Source http://benchviewtestfeed.azurewebsites.net/nuget -OutputDirectory ./tests/scripts -Prerelease -ExcludeVersion

# Install python 3.5.2 to run machinedata.py for machine data collection
sudo add-apt-repository ppa:fkrull/deadsnakes
sudo apt-get update
sudo apt-get --assume-yes install python3.5
python3.5 --version
python3.5 ./tests/scripts/Microsoft.BenchView.JSONFormat/tools/machinedata.py

# Set up the copies
# Coreclr build containing the tests and mscorlib
curl http://dotnet-ci.cloudapp.net/job/$perfBranch/job/master/job/release_windows_nt/lastSuccessfulBuild/artifact/bin/tests/tests.zip -o tests.zip

# Coreclr build we are trying to test
curl http://dotnet-ci.cloudapp.net/job/$perfBranch/job/master/job/release_ubuntu/lastSuccessfulBuild/artifact/*zip*/archive.zip -o bin.zip

# Corefx components.  We now have full stack builds on all distros we test here, so we can copy straight from CoreFX jobs.
curl http://dotnet-ci.cloudapp.net/job/dotnet_corefx/job/master/job/ubuntu14.04_release/lastSuccessfulBuild/artifact/bin/build.tar.gz -o build.tar.gz

# Unpack the corefx binaries
tar -xf build.tar.gz

# Unzip the coreclr binaries
unzip -q -o bin.zip

# Copy coreclr binaries to the right dir
cp -R ./archive/bin/obj ./bin
cp -R ./archive/bin/Product ./bin

# Unzip the tests first.  Exit with 0
mkdir ./bin/tests
unzip -q -o tests.zip -d ./bin/tests/Windows_NT.$perfArch.$perfConfig || exit 0
echo "unzip tests to ./bin/tests/Windows_NT.$perfArch.$perfConfig"
