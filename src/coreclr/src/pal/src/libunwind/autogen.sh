#!/bin/sh

test -n "$srcdir" || srcdir=`dirname "$0"`
test -n "$srcdir" || srcdir=.
(
  cd "$srcdir" &&
  autoreconf --force -v --install
) || exit
test -n "$NOCONFIGURE" || "$srcdir/configure" "$@"
