#!/usr/bin/env bash

function print_usage {
    echo ''
    echo 'Build ILLINKer for CoreCLR testing'
    echo ''
    echo 'Optional arguments:'
    echo '  -?|-h|--help       : Show usage information.'
    echo '  --clone            : Clone the repository https://github.com/mono/linker'
    echo '  --arch             : The architecture to build (default X64)'
    echo '  --os               : The os/runtime to build x64 (ubuntu.16.04)'
    echo ''
}

# Argument variables
clone=
setenv=
os='ubuntu'
arch='x64'

for i in "$@"
do
    case $i in
        -?|-h|--help)
            print_usage
            exit $EXIT_CODE_SUCCESS
            ;;
	
        --clone)
            clone=1
            ;;
	
        --arch=*)
            arch=${i#*=}
            ;;
	
        --os=*)
            os=${i#*=}
            ;;
	
        *)
            echo "Unknown switch: $i"
            print_usage
            exit $EXIT_CODE_SUCCESS
            ;;
    esac
done

rid="$os-$arch"

if [ ! -z "$clone" ]; then
    git clone --recursive https://github.com/mono/linker
fi

pushd linker/corebuild
./restore.sh -r $rid
cd ../linker
../corebuild/dotnet.sh publish -r $rid -c netcore_Release
popd

dir=$(pwd)
output="$dir/linker/linker/artifacts/netcore_Release/netcoreapp2.0/$rid/publish/illink"
echo Built $output

exit $EXIT_CODE_SUCCESS
