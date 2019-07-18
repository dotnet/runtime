#!/usr/bin/env bash
#
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

# WARNING: This utility is not used by infra and very likely to be out of date.

# This is a simple dev utility to easily perform clean builds for Debian and RPM
# package development. It emulates the official build, first producing a
# portable build using some other image, then using Deb/RPM build images to
# package up the bits.
#
# Run this script from the root of the repository.

set -uex

skipPortable=
makeDeb=
makeRpm=

while [[ $# > 0 ]]; do
    opt="$(echo "$1" | awk '{print tolower($0)}')"
    case "$opt" in
        --skip-portable)
            skipPortable=true
            ;;
        --deb)
            makeDeb=true
            ;;
        --rpm)
            makeRpm=true
            ;;
        *)
            echo "Invalid argument: $1"
            exit 1
            ;;
    esac
    shift
done

containerized() {
    image=$1
    shift
    docker run -it --rm \
        -u="$(id -u):$(id -g)" \
        -e HOME=/work/.container-home \
        -v "$(pwd):/work:z" \
        -w "/work" \
        "$image" \
        "$@"
}

package() {
    name=$1
    shift
    image=$1
    shift
    type=$1
    shift
    queryCommand=$1
    shift

    containerized "$image" bash -c "
        eng/common/msbuild.sh \
            tools-local/tasks/core-setup.tasks.csproj \
            /t:Restore /t:Build /t:CreateHostMachineInfoFile \
            /p:Configuration=Release \
            /p:OSGroup=Linux \
            /p:PortableBuild=false \
            /p:TargetArchitecture=x64 \
            /bl:artifacts/msbuild.$name.traversaldependencies.binlog;
        ./build.sh \
            --ci \
            /p:OfficialBuildId=20190101.1 \
            /p:Subset=Installer \
            /p:UsePrebuiltPortableBinariesForInstallers=true \
            /p:SharedFrameworkPublishDir=/work/artifacts/obj/linux-x64.Release/sharedFrameworkPublish/ \
            /p:InstallerSourceOSPlatformConfig=linux-x64.Release \
            /p:GenerateProjectInstallers=true \
            /p:Configuration=Release \
            /p:OSGroup=Linux \
            /p:PortableBuild=false \
            /p:TargetArchitecture=x64 \
            /bl:artifacts/msbuild.$name.installers.binlog"

    containerized "$image" \
        find artifacts/packages/Release/ \
        -iname "*.$type" \
        -exec printf "\n{}\n========\n" \; \
        -exec $queryCommand '{}' \; \
        > "info-$type.txt"
}

[ "$skipPortable" ] || containerized microsoft/dotnet-buildtools-prereqs:centos-7-b46d863-20180719033416 \
    ./build.sh \
    -c Release \
    /p:PortableBuild=true \
    /p:StripSymbols=true \
    /p:TargetArchitecture=x64 \
    /bl:artifacts/msbuild.portable.binlog

ubuntu=microsoft/dotnet-buildtools-prereqs:ubuntu-14.04-debpkg-e5cf912-20175003025046
rhel=microsoft/dotnet-buildtools-prereqs:rhel-7-rpmpkg-c982313-20174116044113

[ "$makeRpm" ] && package rhel $rhel rpm "rpm -qpiR"
[ "$makeDeb" ] && package ubuntu $ubuntu deb "dpkg-deb -I"
