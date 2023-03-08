#!/usr/bin/env sh

docker run \
    -it \
    --mount 'type=bind,src=/home/svbomer/src/crossBuild,dst=/runtime' \
    --mount 'type=volume,src=root_nuget,dst=/root/.nuget' \
    -e 'ROOTFS_DIR=/crossrootfs/x64' \
    -w /runtime \
    mcr.microsoft.com/dotnet-buildtools/prereqs:cbl-mariner-2.0-cross-amd64-alpinelocal \
    ./build.sh -s clr+libs+clr.tools+packs.product --configuration Release --arch x64 --cross -bl
