#!/bin/sh

BASEDIR=$(dirname $0)
$BASEDIR/../../.yamato/scripts/build_yamato.sh --test=embeddingmanaged "$@"