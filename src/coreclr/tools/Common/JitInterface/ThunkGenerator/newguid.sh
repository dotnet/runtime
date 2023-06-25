#!/usr/bin/env bash
cd "$(dirname ${BASH_SOURCE[0]})"
../../../../../../dotnet.sh run -- NewJITEEVersion ../../../../inc/jiteeversionguid.h
