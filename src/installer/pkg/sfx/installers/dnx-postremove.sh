#!/bin/sh
set -e

# Remove the public dnx entry points that dotnet-host generates and repairs, but only on the
# final removal of the host.
#
# This is shared by:
#   - the Debian 'postrm' maintainer script (invoked with 'remove' or 'purge'), and
#   - the RPM '%postun' scriptlet (invoked with the number of remaining package instances,
#     which is 0 on the final erase and >=1 during an upgrade).
#
# For RPM the public /usr/share/dotnet/dnx and /usr/bin/dnx paths are owned as "ghost" files
# (not shipped in the payload) and are materialized by the post-install/file-trigger repair,
# so rpm itself will not remove them; this scriptlet must. For Debian the same paths are
# shipped normally and dpkg removes them, so these calls are a harmless, idempotent backstop.
#
# dotnet-host is a hard dependency of the SDK packages, so it can only be removed once no SDK
# remains; cleaning these paths here therefore does not disturb a still-installed SDK.
case "$1" in
  0|remove|purge)
    rm -f /usr/bin/dnx
    rm -f /usr/share/dotnet/dnx
  ;;
esac
