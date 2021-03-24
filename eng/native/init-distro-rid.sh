#!/usr/bin/env bash

# initNonPortableDistroRid
#
# Input:
#   targetOs: (str)
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
# It is important to note that the function does not return anything, but it
# exports __DistroRid, if there is a non-portable distro rid to be used.
#
initNonPortableDistroRid()
{
    # Make sure out parameter is cleared.
    __DistroRid=

    local targetOs="$1"
    local buildArch="$2"
    local isPortable="$3"
    local rootfsDir="$4"
    local nonPortableBuildID=""

    if [ "$targetOs" = "Linux" ]; then
        if [ -e "${rootfsDir}/etc/os-release" ]; then
            source "${rootfsDir}/etc/os-release"

            # We have forced __PortableBuild=0. This is because -portablebuld
            # has been passed as false.
            if (( isPortable == 0 )); then
                if [ "${ID}" = "rhel" ]; then
                    # remove the last version digit
                    VERSION_ID="${VERSION_ID%.*}"
                fi

                if [ -z "${VERSION_ID+x}" ]; then
                        # Rolling release distros do not set VERSION_ID, so omit
                        # it here to be consistent with everything else.
                        nonPortableBuildID="${ID}-${buildArch}"
                else
                        nonPortableBuildID="${ID}.${VERSION_ID}-${buildArch}"
                fi
            fi

        elif [ -e "${rootfsDir}/android_platform" ]; then
            source "$rootfsDir"/android_platform
            nonPortableBuildID="$RID"
        fi
    fi

    if [ "$targetOs" = "FreeBSD" ]; then
        if (( isPortable == 0 )); then
            # $rootfsDir can be empty. freebsd-version is shell script and it should always work.
            __freebsd_major_version=$($rootfsDir/bin/freebsd-version | { read v; echo "${v%%.*}"; })
            nonPortableBuildID="freebsd.$__freebsd_major_version-${buildArch}"
        fi
    elif command -v getprop && getprop ro.product.system.model 2>&1 | grep -qi android; then
        __android_sdk_version=$(getprop ro.build.version.sdk)
        nonPortableBuildID="android.$__android_sdk_version-${buildArch}"
    elif [ "$targetOs" = "illumos" ]; then
        __uname_version=$(uname -v)
        case "$__uname_version" in
            omnios-*)
                __omnios_major_version=$(echo "${__uname_version:8:2}")
                nonPortableBuildID=omnios."$__omnios_major_version"-"$buildArch"
            ;;
            joyent_*)
                __smartos_major_version=$(echo "${__uname_version:7:4}")
                nonPortableBuildID=smartos."$__smartos_major_version"-"$buildArch"
            ;;
            illumos_*)
                nonPortableBuildID=openindiana-"$buildArch"
            ;;
        esac
    elif [ "$targetOs" = "Solaris" ]; then
        __uname_version=$(uname -v)
        __solaris_major_version=$(echo "${__uname_version%.*}")
        nonPortableBuildID=solaris."$__solaris_major_version"-"$buildArch"
    fi

    if [ -n "${nonPortableBuildID}" ]; then
        __DistroRid="${nonPortableBuildID}"

        # We are using a non-portable build rid. Force __PortableBuild to false.
        __PortableBuild=0

        export __DistroRid __PortableBuild
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
# It is important to note that the function does not return anything, but it
# exports the following variables on success:
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

    local targetOs="$1"
    local buildArch="$2"
    local isPortable="$3"
    local rootfsDir=""
    if [ "$#" -ge 4 ]; then
        rootfsDir="$4"
    fi

    if [ -n "${rootfsDir}" ]; then
        # We may have a cross build. Check for the existance of the rootfsDir
        if [ ! -e "${rootfsDir}" ]; then
            echo "Error rootfsDir has been passed, but the location is not valid."
            exit 1
        fi
    fi

    initNonPortableDistroRid "${targetOs}" "${buildArch}" "${isPortable}" "${rootfsDir}"

    if [ "$buildArch" = "wasm" ]; then
        __DistroRid=browser-wasm
        export __DistroRid
    fi

    if [ -z "${__DistroRid}" ]; then
        # The non-portable build rid was not set. Set the portable rid.

        __PortableBuild=1
        export __PortableBuild
        local distroRid=""

        # Check for musl-based distros (e.g Alpine Linux, Void Linux).
        if "${rootfsDir}/usr/bin/ldd" --version 2>&1 | grep -q musl ||
                strings "${rootfsDir}/usr/bin/ldd" 2>&1 | grep -q musl; then
            distroRid="linux-musl-${buildArch}"
        fi

        if [ -z "${distroRid}" ]; then
            if [ "$targetOs" = "Linux" ]; then
                distroRid="linux-$buildArch"
            elif [ "$targetOs" = "OSX" ]; then
                distroRid="osx-$buildArch"
            elif [ "$targetOs" = "MacCatalyst" ]; then
                distroRid="maccatalyst-$buildArch"
            elif [ "$targetOs" = "tvOS" ]; then
                distroRid="tvos-$buildArch"
            elif [ "$targetOs" = "tvOSSimulator" ]; then
                distroRid="tvossimulator-$buildArch"
            elif [ "$targetOs" = "iOS" ]; then
                distroRid="ios-$buildArch"
            elif [ "$targetOs" = "iOSSimulator" ]; then
                distroRid="iossimulator-$buildArch"
            elif [ "$targetOs" = "Android" ]; then
                distroRid="android-$buildArch"
            elif [ "$targetOs" = "Browser" ]; then
                distroRid="browser-$buildArch"
            elif [ "$targetOs" = "FreeBSD" ]; then
                distroRid="freebsd-$buildArch"
            elif [ "$targetOs" = "illumos" ]; then
                distroRid="illumos-$buildArch"
            elif [ "$targetOs" = "Solaris" ]; then
                distroRid="solaris-$buildArch"
            fi
        fi

        __DistroRid="${distroRid}"
        export __DistroRid
    fi

    if [ -z "$__DistroRid" ]; then
        echo "DistroRid is not set. This is almost certainly an error"
        exit 1
    fi

    echo "__DistroRid: ${__DistroRid}"
}
