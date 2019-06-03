#!/usr/bin/env bash

# initNonPortableDistroRid
#
# Input:
#   buildOs: (str)
#   buildArch: (str)
#   isPortable: (int)
#   rootfsDir: (str)
#
# Return:
#   None
#
# Notes:
#
# initNonPortableDistroRid will attempt to initialize a non portable rid. These
# rids are specific to distros need to build the product/package and consume
# them on the same platform.
#
# If -portablebuild=false is passed a non-portable rid will be created for any
# distro.
#
# Below is the list of current non-portable platforms.
#
# Builds from the following *must* be non-portable:
#
#   |    OS     |           Expected RID            |
#   -------------------------------------------------
#   |   rhel6   |           rhel.6-x64              |
#   |  alpine*  |        linux-musl-(arch)          |
#   |  freeBSD  |        freebsd.(version)-x64      |
#
# It is important to note that the function does not return anything, but it 
# will set __DistroRid if there is a non-portable distro rid to be used.
#
initNonPortableDistroRid()
{
    # Make sure out parameter is cleared.
    __DistroRid=

    local buildOs=$1
    local buildArch=$2
    local isPortable=$3
    local rootfsDir=$4

    if [ "$buildOs" = "Linux" ]; then
        # RHEL 6 is the only distro we will check redHat release for.
        if [ -e "${rootfsDir}/etc/os-release" ]; then
            source "${rootfsDir}/etc/os-release"

            # We have forced __PortableBuild=0. This is because -portablebuld
            # has been passed as false.
            if (( ${isPortable} == 0 )); then
                if [ "${ID}" == "rhel" ]; then
                    # remove the last version digit	
                    VERSION_ID=${VERSION_ID%.*}
                fi

                nonPortableBuildID="${ID}.${VERSION_ID}-${buildArch}"
            fi
            
        elif [ -e "${rootfsDir}/etc/redhat-release" ]; then
            local redhatRelease=$(<${rootfsDir}/etc/redhat-release)

            if [[ "${redhatRelease}" == "CentOS release 6."* || "$redhatRelease" == "Red Hat Enterprise Linux Server release 6."* ]]; then
                nonPortableBuildID="rhel.6-${buildArch}"
            fi
        elif [ -e "${rootfsDir}/android_platform" ]; then
            source $rootfsDir/android_platform
            nonPortableBuildID="$RID"
        fi
    fi

    if [ "$buildOs" = "FreeBSD" ]; then
        __freebsd_version=`sysctl -n kern.osrelease | cut -f1 -d'.'`
        nonPortableBuildID="freebsd.$__freebsd_version-${buildArch}"
    fi

    if [ "${nonPortableBuildID}" != "" ]; then
        export __DistroRid=${nonPortableBuildID}

        # We are using a non-portable build rid. Force __PortableBuild to false.
        export __PortableBuild=0
    fi
}


# initDistroRidGlobal
#
# Input:
#   os: (str)
#   arch: (str)
#   isPortable: (int)
#   rootfsDir?: (nullable:string)
#
# Return:
#   None
#
# Notes:
#
# The following out parameters are returned
#
#   __DistroRid
#   __PortableBuild
#
initDistroRidGlobal()
{
    # __DistroRid must be set at the end of the function.
    # Previously we would create a variable __HostDistroRid and/or __DistroRid.
    #
    # __HostDistroRid was used in the case of a non-portable build, it has been
    # deprecated. Now only __DistroRid is supported. It will be used for both
    # portable and non-portable rids and will be used in build-packages.sh

    local buildOs=$1
    local buildArch=$2
    local isPortable=$3
    local rootfsDir=$4

    # Setup whether this is a crossbuild. We can find this out if rootfsDir
    # is set. 
    local isCrossBuild=0

    if [ -z "${rootfsDir}" ]; then
        isCrossBuild=0
    else
        # We may have a cross build. Check for the existance of the rootfsDir
        if [ -e ${rootfsDir} ]; then
            isCrossBuild=1
        else
            echo "Error rootfsDir has been passed, but the location is not valid."
            exit 1
        fi
    fi

    if [ "$buildArch" = "armel" ]; then
        # Armel cross build is Tizen specific and does not support Portable RID build
        export __PortableBuild=0
        isPortable=0
    fi

    initNonPortableDistroRid ${buildOs} ${buildArch} ${isPortable} ${rootfsDir}

    if [ -z "${__DistroRid}" ]; then
        # The non-portable build rid was not set. Set the portable rid.

        export __PortableBuild=1
        local distroRid=""

        # Check for alpine. It is the only portable build that will will have
        # its name in the portable build rid.
        if [ -e "${rootfsDir}/etc/os-release" ]; then
            source "${rootfsDir}/etc/os-release"
            if [ "${ID}" = "alpine" ]; then
                distroRid="linux-musl-${buildArch}"
            fi
        fi

        if [ "${distroRid}" == "" ]; then
            if [ "$buildOs" = "Linux" ]; then
                distroRid="linux-$buildArch"
            elif [ "$buildOs" = "OSX" ]; then
                distroRid="osx-$buildArch"
            elif [ "$buildOs" = "FreeBSD" ]; then
                distroRid="freebsd-$buildArch"
            fi
        fi

        export __DistroRid=${distroRid}
    fi

    if [ -z "$__DistroRid" ]; then
        echo "DistroRid is not set. This is almost certainly an error"

        exit 1
    else
        echo "__DistroRid: ${__DistroRid}"
        echo "__RuntimeId: ${__DistroRid}"
        
        export __RuntimeId=${__DistroRid}
    fi
}