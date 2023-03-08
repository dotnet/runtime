#!/usr/bin/env sh

docker run \
    -it \
    --mount 'type=bind,src=/home/svbomer/src/crossBuild,dst=/runtime' \
    -e 'ROOTFS_DIR=/crossrootfs/x64' \
    -w /runtime \
    mcr.microsoft.com/dotnet-buildtools/prereqs:cbl-mariner-2.0-cross-amd64local \
    ./build.sh -c Release --arch x64 --cross -bl --pgoinstrument


    # "/runtime/src/coreclr/build-runtime.sh" -x64 -release -cross -os linux -pgodatapath "/root/.nuget/packages/optimization.linux-x64.pgo.coreclr/1.0.0-prerelease.23068.4"


    # --mount 'type=volume,src=root_nuget,dst=/root/.nuget' \

#    ./build.sh -s clr+libs+clr.tools+packs.product --configuration Release --arch x64 --cross -bl

    # ./src/coreclr/build-runtime.sh -release -x64 -cross -pgoinstrument



