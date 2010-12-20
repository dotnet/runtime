#!/bin/bash
# Copyright (c) 2009 The Native Client Authors. All rights reserved.
# Use of this source code is governed by a BSD-style license that be
# found in the LICENSE file.
#

# normal-mono.sh
#
# usage:  normal-mono.sh
#
# this script builds normal x86 mono
# (installed in ./normal folder)
#

readonly MONO_TRUNK_NACL=$(pwd)

readonly PACKAGE_NAME=mono-normal-build

source common.sh


CustomConfigureStep() {
  Banner "Configuring ${PACKAGE_NAME}"
  set +e
  if [ -f ${PACKAGE_NAME}/Makefile ]
  then
    cd ${PACKAGE_NAME}
    make distclean
  fi
  cd ${MONO_TRUNK_NACL}
  set -e
  Remove ${PACKAGE_NAME}
  MakeDir ${PACKAGE_NAME}
  cd ${PACKAGE_NAME}
  ../../configure \
    --prefix=${MONO_TRUNK_NACL}/normal-mono \
    --disable-parallel-mark \
    --with-tls=pthread 
}

CustomPackageInstall() {
  CustomConfigureStep
  DefaultBuildStep
  DefaultInstallStep
}


CustomPackageInstall
exit 0
