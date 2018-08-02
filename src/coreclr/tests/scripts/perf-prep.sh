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
    echo 'Optional arguments:'
    echo '  --throughput                : if we are running setup for a throughput run.'
}

# Exit code constants
readonly EXIT_CODE_SUCCESS=0       # Script ran normally.

# Argument variables
perfArch="x64"
perfConfig="Release"
perfBranch=
throughput=0
nocorefx=0

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
        -t|--throughput)
            throughput=1
            ;;
        --nocorefx)
            nocorefx=1
            ;;
        --arch=*)
            perfArch=${i#*=}
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

if [ ! -d "./tests/scripts/Microsoft.Benchview.JSONFormat" ]; then
    curl "http://benchviewtestfeed.azurewebsites.net/nuget/FindPackagesById()?id='Microsoft.BenchView.JSONFormat'" | grep "content type" | sed "$ s/.*src=\"\([^\"]*\)\".*/\1/;tx;d;:x" | xargs curl -o benchview.zip
    unzip -q -o benchview.zip -d ./tests/scripts/Microsoft.BenchView.JSONFormat
fi

# Install python 3.5.2 to run machinedata.py for machine data collection
if [ $perfArch == "arm" ]; then
    python3.6 --version
    python3.6 ./tests/scripts/Microsoft.BenchView.JSONFormat/tools/machinedata.py --machine-manufacturer NVIDIA
else
    python3 --version
    python3 ./tests/scripts/Microsoft.BenchView.JSONFormat/tools/machinedata.py
fi

if [ $throughput -eq 1 ]; then
    # Download throughput benchmarks
    if [ ! -d "Microsoft.Benchview.ThroughputBenchmarks.x64.Windows_NT" ]; then
        mkdir Microsoft.Benchview.ThroughputBenchmarks.x64.Windows_NT
        cd Microsoft.Benchview.ThroughputBenchmarks.x64.Windows_NT

        curl -OL https://dotnet.myget.org/F/dotnet-core/api/v2/package/Microsoft.Benchview.ThroughputBenchmarks.x64.Windows_NT/1.0.0
        mv 1.0.0 1.0.0.zip
        unzip -q 1.0.0.zip
    fi

else
    if [ $nocorefx -eq 0 ]; then
        # Corefx components.  We now have full stack builds on all distros we test here, so we can copy straight from CoreFX jobs.
        echo "Downloading corefx"
        mkdir corefx		
        curl https://ci.dot.net/job/dotnet_corefx/job/master/job/ubuntu14.04_release/lastSuccessfulBuild/artifact/bin/build.tar.gz -o ./corefx/build.tar.gz		

        # Unpack the corefx binaries		
        pushd corefx > /dev/null		
        tar -xf build.tar.gz		
        rm build.tar.gz		
        popd > /dev/null
    fi

    # If the tests don't already exist, download them.
    if [ ! -d "bin" ]; then
        echo "Making bin dir"
        mkdir bin
    fi

    if [ ! -d "bin/tests" ]; then
        echo "Making bin/tests"
        mkdir bin/tests
    fi

    if [ ! -d "bin/tests/Windows_NT.$perfArch.$perfConfig" ]; then
        echo "Downloading tests"
        curl https://ci.dot.net/job/$perfBranch/job/master/job/release_windows_nt/lastSuccessfulBuild/artifact/bin/tests/tests.zip -o tests.zip
        echo "unzip tests to ./bin/tests/Windows_NT.$perfArch.$perfConfig"
        unzip -q -o tests.zip -d ./bin/tests/Windows_NT.$perfArch.$perfConfig || exit 0
    fi
fi
