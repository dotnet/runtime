#!/bin/sh
set -e

# Idempotently restore the robust dnx dispatcher shipped by dotnet-host and ensure the
# public /usr/bin/dnx entry point is a symlink to it.
#
# This is shared by:
#   - the Debian 'postinst' maintainer script (invoked on configure and on trigger activation),
#   - the RPM '%post' scriptlet (invoked on install and upgrade), and
#   - the RPM transaction file trigger that fires when an SDK changes its install tree.
#
# Background (https://github.com/dotnet/sdk/issues/55303): released dotnet-sdk-10.0 packages
# shipped /usr/bin/dnx as a regular file whose only content was the text "../share/dotnet/dnx"
# (so direct execution failed), and an older dispatcher at /usr/share/dotnet/dnx.
#
# /usr/share/dotnet/dnx cannot also be the repair source: it is one of the public paths that an
# SDK install or reinstall can overwrite before this script runs. The host package therefore keeps
# its robust dispatcher at /usr/share/dotnet/dnx.dispatcher, a private path that SDK packages do not
# own. That uncontested source survives the SDK transaction and lets this script deterministically
# restore both public entries afterward.

dispatcher_source="/usr/share/dotnet/dnx.dispatcher"
dispatcher_target="/usr/share/dotnet/dnx"
public_entry="/usr/bin/dnx"

if [ ! -f "$dispatcher_source" ]; then
  echo "dotnet-host cannot repair dnx because $dispatcher_source is missing." >&2
  exit 1
fi

install -m 0755 "$dispatcher_source" "$dispatcher_target"
ln -sfn ../share/dotnet/dnx "$public_entry"
