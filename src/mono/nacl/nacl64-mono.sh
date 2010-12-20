#!/bin/bash

# nacl64-mono.sh
#
# usage:  nacl64-mono.sh
#
# this script builds a compiler for 64-bit NaCl code
# (installed in ./compiler folder)
#

readonly MONO_TRUNK_NACL=$(pwd)

readonly PACKAGE_NAME=nacl64-mono-build

readonly INSTALL_PATH=${MONO_TRUNK_NACL}/compiler

source common.sh
source nacl-common.sh


CustomConfigureStep() {
  Banner "Configuring ${PACKAGE_NAME}"
  set +e
  cd ${PACKAGE_NAME}
  make distclean
  cd ${MONO_TRUNK_NACL}
  set -e
  Remove ${PACKAGE_NAME}
  MakeDir ${PACKAGE_NAME}
  cd ${PACKAGE_NAME}
  cp ../nacl64-mono-config-cache ../nacl64-mono-config-cache.temp
  if [ $HOST_BITSIZE = "64" ]; then
    ../../configure \
      CFLAGS="-O0" CXXFLAGS="-O0" CC='cc -m32' CXX='g++ -m32' \
      --host=i386-pc-linux \
      --build=amd64-pc-linux \
      --target=nacl64 \
      --prefix=${INSTALL_PATH} \
      --with-tls=pthread \
      --enable-nacl-codegen \
      --disable-mono-debugger \
      --disable-mcs-build \
      --with-sigaltstack=no \
      --cache-file=../nacl-mono-config-cache.temp
  else
    ../../configure \
      --target=nacl64 \
      --prefix=${INSTALL_PATH} \
      --with-tls=pthread \
      --enable-nacl-codegen \
      --disable-mono-debugger \
      --disable-mcs-build \
      --with-sigaltstack=no \
      --cache-file=../nacl-mono-config-cache.temp
  fi
  

  rm ../nacl-mono-config-cache.temp
}

CustomBuildStep() {
  MONO_NACL_ALIGN_MASK_OFF=1 make -j4
}

CustomInstallStep() {
  MONO_NACL_ALIGN_MASK_OFF=1 make install
}

CustomPackageInstall() {
  CustomConfigureStep
  #CustomBuildStep
  #CustomInstallStep
  DefaultBuildStep
  DefaultInstallStep
}


CustomPackageInstall
exit 0
