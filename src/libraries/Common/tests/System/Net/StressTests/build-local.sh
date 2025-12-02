#!/usr/bin/env bash

## This is a helper script for non-containerized local build and test execution.
## It downloads and uses the daily SDK which contains the compatible AspNetCore bits.
## Usage:
## ./build-local.sh [TestProjectDir] [StressConfiguration] [LibrariesConfiguration]

if [ "$1" == "" ]; then
    echo "Usage:"
    echo "./build-local.sh [TestProjectDir] [StressConfiguration] [LibrariesConfiguration]"
    echo "StressConfiguration and LibrariesConfiguration default to Release!"
    exit 1
fi

projectdir=$1
projectname=$(basename $1)
daily_dotnet_root=$projectdir/.dotnet-daily

repo_root=$(git -C "$projectdir" rev-parse --show-toplevel)
major_version=$(grep -oP '(?<=<MajorVersion>).*?(?=</MajorVersion>)' "$repo_root/eng/Versions.props")
minor_version=$(grep -oP '(?<=<MinorVersion>).*?(?=</MinorVersion>)' "$repo_root/eng/Versions.props")
version="$major_version.$minor_version"

stress_configuration="Release"
if [ "$2" != "" ]; then
    stress_configuration=${2,,}                   # Lowercase all characters in $1
    stress_configuration=${stress_configuration^} # Uppercase first character
fi

libraries_configuration="Release"
if [ "$3" != "" ]; then
    libraries_configuration=${3,,}                      # Lowercase all characters in $1
    libraries_configuration=${libraries_configuration^} # Uppercase first character
fi

testhost_root=$repo_root/artifacts/bin/testhost/net$version-linux-$libraries_configuration-x64
echo "StressConfiguration: $stress_configuration, LibrariesConfiguration: $libraries_configuration, testhost: $testhost_root"

if [[ ! -d $testhost_root ]]; then
    echo "Cannot find testhost in: $testhost_root"
    echo "Make sure libraries with the requested configuration are built!"
    echo "Usage:"
    echo "./build-local.sh [TestProjectDir] [StressConfiguration] [LibrariesConfiguration]"
    echo "StressConfiguration and LibrariesConfiguration default to Release!"
    exit 1
fi

if [[ ! -d $daily_dotnet_root ]]; then
    echo "Downloading daily SDK to $daily_dotnet_root"
    mkdir $daily_dotnet_root
    wget https://builds.dotnet.microsoft.com/dotnet/scripts/v1/dotnet-install.sh -O $daily_dotnet_root/dotnet-install.sh
    bash $daily_dotnet_root/dotnet-install.sh --no-path --channel $version --quality daily --install-dir $daily_dotnet_root
else
    echo "Daily SDK found in $daily_dotnet_root"
fi

export DOTNET_ROOT=$daily_dotnet_root
export PATH=$DOTNET_ROOT:$PATH
export DOTNET_MULTILEVEL_LOOKUP=0

if [[ ! -d "$testhost_root/shared/Microsoft.AspNetCore.App" ]]; then
    echo "Copying Microsoft.AspNetCore.App bits from daily SDK to testhost: $testhost_root"
    cp -r $daily_dotnet_root/shared/Microsoft.AspNetCore.App $testhost_root/shared/Microsoft.AspNetCore.App
else
    echo "Microsoft.AspNetCore.App found in testhost: $testhost_root"
fi

echo "Building solution."
dotnet build -c $stress_configuration

runscript=./run-stress-${stress_configuration,,}-${libraries_configuration,,}.sh
if [[ ! -f $runscript ]]; then
    echo "Generating runscript."
    cat >$runscript <<EOF
#!/usr/bin/env bash
$testhost_root/dotnet exec --roll-forward Major ./bin/$stress_configuration/net$version/$projectname.dll \$@
EOF

    chmod +x $runscript
fi

echo "To run tests type:"
echo "$runscript [stress test args]"
