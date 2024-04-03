#!/bin/sh

# initDistroRidGlobal
#
# Input:
#   os: (str)
#   arch: (str)
#   rootfsDir?: (nullable:string)
#
# Return:
#   None
#
# Notes:
#   It is important to note that the function does not return anything, but it
#   exports the following variables on success:
#     __PortableTargetOS  : OS-part of the portable rid that corresponds to the target platform.
initDistroRidGlobal()
{
    targetOs="$1"
    targetArch="$2"
    rootfsDir=""
    if [ $# -ge 3 ]; then
        rootfsDir="$3"
    fi

    if [ -n "${rootfsDir}" ]; then
        # We may have a cross build. Check for the existence of the rootfsDir
        if [ ! -e "${rootfsDir}" ]; then
            echo "Error: rootfsDir has been passed, but the location is not valid."
            exit 1
        fi
    fi

    if [ -z "${__PortableTargetOS:-}" ]; then
        __PortableTargetOS="$targetOs"

        STRINGS="$(command -v strings || true)"
        if [ -z "$STRINGS" ]; then
            STRINGS="$(command -v llvm-strings || true)"
        fi

        # Check for musl-based distros (e.g. Alpine Linux, Void Linux).
        if "${rootfsDir}/usr/bin/ldd" --version 2>&1 | grep -q musl ||
                ( [ -n "$STRINGS" ] && "$STRINGS" "${rootfsDir}/usr/bin/ldd" 2>&1 | grep -q musl ); then
            __PortableTargetOS="linux-musl"
        fi
    fi

    export __PortableTargetOS
}
